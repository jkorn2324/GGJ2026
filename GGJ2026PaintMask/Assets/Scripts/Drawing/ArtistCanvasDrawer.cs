using GGJ2026.Utils;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Pool;
using Half = Unity.Mathematics.half;

namespace GGJ2026.Painting
{
    /// <summary>
    /// Single-pass drawer:
    /// - Uploads stroke segments + tape segments into 1D RGBAHalf textures
    /// - Fullscreen blit runs a single shader that does the double loop (strokes x tapes) w/ history
    /// - Outputs directly to TargetRT
    /// </summary>
    public sealed class ArtistCanvasDrawer
    {
        #region constants

        public const int MaxStrokes = 256;
        public const int StrokeTexelsPerStroke = 3;
        public const int StrokeTexelCount = MaxStrokes * StrokeTexelsPerStroke;

        public const int MaxTapes = 128;
        public const int TapeTexelsPerTape = 4;
        public const int TapeTexelCount = MaxTapes * TapeTexelsPerTape;

        private static readonly int StrokeTexId   = Shader.PropertyToID("_StrokeTex");
        private static readonly int StrokeCountId = Shader.PropertyToID("_StrokeCount");

        private static readonly int TapeTexId     = Shader.PropertyToID("_TapeTex");
        private static readonly int TapeCountId   = Shader.PropertyToID("_TapeCount");

        private static readonly int TargetSizeId  = Shader.PropertyToID("_TargetSize");
        private static readonly int BackgroundId  = Shader.PropertyToID("_Background");

        private static readonly Shader DrawingShader = Shader.Find("GGJ2026/S_DrawingSinglePass");

        #endregion

        #region statics

        private static readonly ObjectPool<ArtistCanvasDrawer> Pool =
            new ObjectPool<ArtistCanvasDrawer>(() => new ArtistCanvasDrawer());

        public static ArtistCanvasDrawer New(
            int width,
            int height,
            ArtistPainting painting,
            RenderTextureFormat format = RenderTextureFormat.ARGB32,
            Color? background = null)
        {
            if (painting == null) return null;

            var drawer = Pool.Get();
            drawer.Initialize(painting, width, height, format, background);
            return drawer;
        }

        public static void Release(ref ArtistCanvasDrawer drawer)
        {
            if (drawer != null)
            {
                drawer.DeInitialize();
                Pool.Release(drawer);
            }
            drawer = null;
        }

        #endregion

        private ArtistPainting _painting;

        private Material _material;
        private RenderTexture _targetRT;

        private Texture2D _strokeTex;
        private Texture2D _tapeTex;

        private Color _background;

        private bool _dataDirty = true;
        private bool _renderDirty = true;

        public RenderTexture TargetRT => _targetRT;
        public bool IsInitialized { get; private set; }

        private ArtistCanvasDrawer() { }

        #region lifecycle

        private void Initialize(
            ArtistPainting painting,
            int width,
            int height,
            RenderTextureFormat format,
            Color? background)
        {
            if (IsInitialized) return;

            _painting = painting;
            _background = background ?? new Color(0, 0, 0, 0);

            _material = new Material(DrawingShader) { hideFlags = HideFlags.HideAndDontSave };
            _targetRT = TextureUtil.CreateRenderTexture(width, height, format);

            AllocateDataTextures();

            IsInitialized = true;
        }

        private void DeInitialize()
        {
            if (!IsInitialized) return;

            _painting = null;

            TextureUtil.ReleaseRenderTexture(ref _targetRT);

            ObjectUtil.ReleaseObject(ref _strokeTex);
            ObjectUtil.ReleaseObject(ref _tapeTex);
            ObjectUtil.ReleaseObject(ref _material);

            IsInitialized = false;
        }

        private void AllocateDataTextures()
        {
            _strokeTex = new Texture2D(StrokeTexelCount, 1, TextureFormat.RGBAHalf, mipChain: false, linear: true)
            {
                name = "GGJ2026_StrokeDataTex",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point
            };
            TextureUtil.ClearPixels<Half>(_strokeTex);
            _strokeTex.Apply(false, false);

            _tapeTex = new Texture2D(TapeTexelCount, 1, TextureFormat.RGBAHalf, mipChain: false, linear: true)
            {
                name = "GGJ2026_TapeDataTex",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point
            };
            TextureUtil.ClearPixels<Half>(_tapeTex);
            _tapeTex.Apply(false, false);
        }

        #endregion

        #region public api

        public void Resize(int width, int height, RenderTextureFormat format)
        {
            if (!IsInitialized) return;

            TextureUtil.ReleaseRenderTexture(ref _targetRT);
            _targetRT = TextureUtil.CreateRenderTexture(width, height, format);

            _renderDirty = true;
        }

        public void SetBackground(Color color)
        {
            _background = color;
            _renderDirty = true;
        }

        public void MarkDirty()
        {
            _dataDirty = true;
            _renderDirty = true;
        }

        /// <summary>
        /// Full redraw into TargetRT. Does nothing unless dirty (or force=true).
        /// </summary>
        public void Render(bool force = false)
        {
            if (!IsInitialized || _painting == null || !_painting.IsInitialized)
            {
                return;
            }
            if (!force && !_renderDirty)
            {
                return;
            }
            if (_dataDirty)
            {
                UploadDataTextures();
                _dataDirty = false;
            }
            BindMaterialUniforms();
            Graphics.Blit(null, _targetRT, _material);
            _renderDirty = false;
        }

        #endregion

        #region gpu upload

        private void UploadDataTextures()
        {
            if (_painting == null || !_painting.IsInitialized)
            {
                return;
            }
            if (!_strokeTex || !_tapeTex || !_targetRT)
            {
                return;
            }
            var texSize = new Vector2(_targetRT.width, _targetRT.height);
            UploadStrokes(texSize);
            UploadTapes(texSize);
        }

        /// <summary>
        /// Stroke packing must match shader:
        /// - texel0: startUV.xy, endUV.xy
        /// - texel1: color.rgb, widthPixels
        /// - texel2: color.a, featherPixels, roundCaps01, unused
        /// </summary>
        private void UploadStrokes(Vector2 targetSizePixels)
        {
            var raw = _strokeTex.GetRawTextureData<Half>();
            TextureUtil.ClearPixels<Half>(_strokeTex);

            var strokeCount = Mathf.Min(_painting.StrokeCount, MaxStrokes);
            for (var strokeIndex = 0; strokeIndex < strokeCount; strokeIndex++)
            {
                var stroke = _painting.GetStroke(strokeIndex);
                if (!stroke.IsValid)
                {
                    continue;
                }
                var seg = _painting.GetLine(stroke.LineIndex);
                var baseTexel = strokeIndex * StrokeTexelsPerStroke;
                var startUv = MathUtil.ToUv(seg.StartPosition, targetSizePixels);
                var endUv   = MathUtil.ToUv(seg.EndPosition, targetSizePixels);
                var widthPixels   = seg.Width;
                var featherPixels = seg.EdgeSmoothness;
                var roundCaps01   = seg.EdgeRoundness;
                WriteTexelRgbaHalf(raw, baseTexel + 0, startUv.x, startUv.y, endUv.x, endUv.y);
                WriteTexelRgbaHalf(raw, baseTexel + 1, seg.Color.r, seg.Color.g, seg.Color.b, widthPixels);
                WriteTexelRgbaHalf(raw, baseTexel + 2, seg.Color.a, featherPixels, roundCaps01, 0f);
            }
            _strokeTex.Apply(false, false);
        }

        /// <summary>
        /// Tape packing must match shader (4 texels per tape):
        /// - T0: startUV.xy, endUV.xy
        /// - T1: widthPixels, startStrokeIndex, finishedStrokeIndex, reserved
        /// - T2: color.rgba
        /// - T3: featherPixels, roundCaps01, reserved, reserved
        /// </summary>
        private void UploadTapes(Vector2 targetSizePixels)
        {
            var raw = _tapeTex.GetRawTextureData<Half>();
            TextureUtil.ClearPixels<Half>(_tapeTex);

            var tapeCount = Mathf.Min(_painting.TapeCount, MaxTapes);
            for (var tapeIndex = 0; tapeIndex < tapeCount; tapeIndex++)
            {
                var tape = _painting.GetTape(tapeIndex);
                if (!tape.IsValid)
                {
                    continue;
                }
                var tapeLine = _painting.GetLine(tape.LineIndex);
                var baseTexel = tapeIndex * TapeTexelsPerTape;
                var startUv = MathUtil.ToUv(tapeLine.StartPosition, targetSizePixels);
                var endUv   = MathUtil.ToUv(tapeLine.EndPosition, targetSizePixels);
                var startStrokeIndex = tape.StartStrokeIndex;
                var finishedStrokeIndex = tape.FinishedStrokeIndex; 
                var tapeColor = tapeLine.Color;
                WriteTexelRgbaHalf(raw, baseTexel + 0, startUv.x, startUv.y, endUv.x, endUv.y);
                WriteTexelRgbaHalf(raw, baseTexel + 1, tapeLine.Width, startStrokeIndex, finishedStrokeIndex, 0f);
                WriteTexelRgbaHalf(raw, baseTexel + 2, tapeColor.r, tapeColor.g, tapeColor.b, tapeColor.a);
                WriteTexelRgbaHalf(raw, baseTexel + 3, tapeLine.EdgeSmoothness, tapeLine.EdgeRoundness, 0f, 0f);
            }

            _tapeTex.Apply(false, false);
        }

        private void BindMaterialUniforms()
        {
            var strokeCount = Mathf.Min(_painting.StrokeCount, MaxStrokes);
            var tapeCount = Mathf.Min(_painting.TapeCount, MaxTapes);
            _material.SetTexture(StrokeTexId, _strokeTex);
            _material.SetFloat(StrokeCountId, strokeCount);
            _material.SetTexture(TapeTexId, _tapeTex);
            _material.SetFloat(TapeCountId, tapeCount);
            _material.SetVector(TargetSizeId,
                new Vector4(
                    _targetRT.width,
                    _targetRT.height,
                    1f / _targetRT.width,
                    1f / _targetRT.height
                ));
            _material.SetColor(BackgroundId, _background);
        }

        #endregion

        #region utilities

        private static void WriteTexelRgbaHalf(NativeArray<Half> raw, int texelIndex, float r, float g, float b, float a)
        {
            var o = texelIndex * 4;
            raw[o + 0] = (Half)r;
            raw[o + 1] = (Half)g;
            raw[o + 2] = (Half)b;
            raw[o + 3] = (Half)a;
        }

        #endregion
    }
}
