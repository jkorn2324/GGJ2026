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
        
        private static readonly int ClearColorId   = Shader.PropertyToID("_ClearColor");
        private static readonly Shader ClearShader = Shader.Find("GGJ2026/S_ClearTexturePass");

        #endregion

        #region statics

        private static readonly ObjectPool<ArtistCanvasDrawer> Pool =
            new ObjectPool<ArtistCanvasDrawer>(() => new ArtistCanvasDrawer());

        public static ArtistCanvasDrawer New(
            RenderTextureFormat format = RenderTextureFormat.ARGB32,
            Color? background = null)
        {
            var drawer = Pool.Get();
            drawer.Initialize(format, background);
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
        private ArtistPainting.Listener _paintingListener;

        private Material _renderPassMaterial;
        private Material _clearMaterial;
        private RenderTexture _targetRT;

        private Texture2D _strokeTex;
        private Texture2D _tapeTex;

        private Color _background;

        private bool _dataDirty = true;
        private bool _renderDirty = true;

        public RenderTexture TargetRT => _targetRT;
        
        public ArtistPainting Painting => _painting;
        
        public bool IsInitialized { get; private set; }

        private ArtistCanvasDrawer()
        {
            _paintingListener.OnChanged = OnPaintingChanged;
        }

        #region lifecycle

        private void Initialize(
            RenderTextureFormat format,
            Color? background)
        {
            if (IsInitialized) return;

            _background = background ?? new Color(0, 0, 0, 0);

            _renderPassMaterial = new Material(DrawingShader) { hideFlags = HideFlags.HideAndDontSave };
            _clearMaterial = new Material(ClearShader) { hideFlags = HideFlags.HideAndDontSave };
            _targetRT = TextureUtil.CreateRenderTexture(16, 16, format);

            AllocateDataTextures();
            IsInitialized = true;
            SetPainting(null);
        }

        private void DeInitialize()
        {
            if (!IsInitialized) return;

            SetPainting(null);
            TextureUtil.ReleaseRenderTexture(ref _targetRT);
            ObjectUtil.ReleaseObject(ref _strokeTex);
            ObjectUtil.ReleaseObject(ref _tapeTex);
            ObjectUtil.ReleaseObject(ref _renderPassMaterial);
            ObjectUtil.ReleaseObject(ref _clearMaterial);

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
        
        #region internals
        
        private void OnPaintingChanged(ArtistPainting obj)
        {
            if (!IsInitialized)
            {
                return;
            }
            // Resizes the render texture.
            if (_targetRT)
            {
                var prevWidth = _targetRT.width;
                var prevHeight = _targetRT.height;
                var paintingSize = obj.PaintingSize;
                if (!Mathf.Approximately(paintingSize.x, prevWidth)
                    || !Mathf.Approximately(paintingSize.y, prevHeight))
                {
                    Resize(Mathf.RoundToInt(paintingSize.x), 
                        Mathf.RoundToInt(paintingSize.y), _targetRT.format);
                }
            }
            else
            {
                var paintingSize = obj.PaintingSize;
                Resize(Mathf.RoundToInt(paintingSize.x), 
                    Mathf.RoundToInt(paintingSize.y), RenderTextureFormat.ARGB32);
            }
            MarkDirty();
        }
        
        #endregion

        #region public api

        /// <summary>
        /// Sets the painting.
        /// </summary>
        /// <param name="painting">The painting.</param>
        public void SetPainting(ArtistPainting painting)
        {
            if (!IsInitialized)
            {
                return;
            }
            var prev = _painting;
            _painting = painting;
            if (prev != painting)
            {
                _paintingListener.DeInitialize(prev);
                _paintingListener.Initialize(painting, invokeFunctions: true);
            }
        }

        private void Resize(int width, int height, RenderTextureFormat format)
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
            if (!IsInitialized || (!force && !_renderDirty))
            {
                return;
            }
            if (_dataDirty)
            {
                UploadDataTextures();
                _dataDirty = false;
            }
            if (_painting != null && _painting.IsInitialized)
            {
                BindMaterialUniforms();
                Graphics.Blit(null, _targetRT, _renderPassMaterial);
                _renderDirty = false;
                return;
            }
            _clearMaterial.SetColor(ClearColorId, Color.clear);
            Graphics.Blit(null, _targetRT, _clearMaterial);
            _renderDirty = false;
        }

        #endregion

        #region gpu upload

        private void UploadDataTextures()
        {
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

            if (_painting == null || !_painting.IsInitialized)
            {
                _strokeTex.Apply(false, false);
                return;
            }
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
            // Sets the current line.
            if (_painting.CurrentLine != null)
            {
                var currentLine = (ArtistPainting.ActiveLineInfo)_painting.CurrentLine;
                if (!currentLine.IsTape)
                {
                    var baseTexel = strokeCount * StrokeTexelsPerStroke;
                    var seg = currentLine.Line;
                    var startUv = MathUtil.ToUv(seg.StartPosition, targetSizePixels);
                    var endUv   = MathUtil.ToUv(seg.EndPosition, targetSizePixels);
                    var widthPixels   = seg.Width;
                    var featherPixels = seg.EdgeSmoothness;
                    var roundCaps01   = seg.EdgeRoundness;
                    WriteTexelRgbaHalf(raw, baseTexel + 0, startUv.x, startUv.y, endUv.x, endUv.y);
                    WriteTexelRgbaHalf(raw, baseTexel + 1, seg.Color.r, seg.Color.g, seg.Color.b, widthPixels);
                    WriteTexelRgbaHalf(raw, baseTexel + 2, seg.Color.a, featherPixels, roundCaps01, 0f);
                }
            }
            _strokeTex.Apply(false, false);
        }

        /// <summary>
        /// Tape packing must match shader (4 texels per tape):
        /// - T0: startUV.xy, endUV.xy
        /// - T1: widthPixels, startStrokeIndex, affectedStrokeCount, isFinished
        /// - T2: color.rgba
        /// - T3: featherPixels, roundCaps01, reserved, reserved
        /// </summary>
        private void UploadTapes(Vector2 targetSizePixels)
        {
            var raw = _tapeTex.GetRawTextureData<Half>();
            TextureUtil.ClearPixels<Half>(_tapeTex);

            if (_painting == null || !_painting.IsInitialized)
            {
                _tapeTex.Apply(false, false);
                return;
            }
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
                var strokeCount = tape.AffectedStrokeCount;
                var isFinished = tape.IsFinished;
                var tapeColor = tapeLine.Color;
                WriteTexelRgbaHalf(raw, baseTexel + 0, startUv.x, startUv.y, endUv.x, endUv.y);
                WriteTexelRgbaHalf(raw, baseTexel + 1, tapeLine.Width, startStrokeIndex, strokeCount, isFinished ? 1f : 0f);
                WriteTexelRgbaHalf(raw, baseTexel + 2, tapeColor.r, tapeColor.g, tapeColor.b, tapeColor.a);
                WriteTexelRgbaHalf(raw, baseTexel + 3, tapeLine.EdgeSmoothness, tapeLine.EdgeRoundness, 0f, 0f);
            }
            // Sets the current line.
            if (_painting.CurrentLine != null)
            {
                var currentLine = (ArtistPainting.ActiveLineInfo)_painting.CurrentLine;
                if (currentLine.IsTape)
                {
                    var baseTexel = tapeCount * StrokeTexelsPerStroke;
                    var tapeLine = currentLine.Line;
                    var startUv = MathUtil.ToUv(tapeLine.StartPosition, targetSizePixels);
                    var endUv   = MathUtil.ToUv(tapeLine.EndPosition, targetSizePixels);
                    var startStrokeIndex = _painting.StrokeCount;
                    var tapeColor = tapeLine.Color;
                    WriteTexelRgbaHalf(raw, baseTexel + 0, startUv.x, startUv.y, endUv.x, endUv.y);
                    WriteTexelRgbaHalf(raw, baseTexel + 1, tapeLine.Width, startStrokeIndex, 0f, 0f);
                    WriteTexelRgbaHalf(raw, baseTexel + 2, tapeColor.r, tapeColor.g, tapeColor.b, tapeColor.a);
                    WriteTexelRgbaHalf(raw, baseTexel + 3, tapeLine.EdgeSmoothness, tapeLine.EdgeRoundness, 0f, 0f);
                }
            }
            _tapeTex.Apply(false, false);
        }

        private void BindMaterialUniforms()
        {
            if (_painting == null || !_painting.IsInitialized)
            {
                return;
            }

            var additionalStrokeCount = 0;
            var additionalTapeCount = 0;
            if (_painting.CurrentLine != null)
            {
                var casted = (ArtistPainting.ActiveLineInfo)_painting.CurrentLine;
                additionalTapeCount += casted.IsTape ? 1 : 0;
                additionalStrokeCount += !casted.IsTape ? 1 : 0;
            }
            var strokeCount = Mathf.Min(_painting.StrokeCount + additionalStrokeCount, MaxStrokes);
            var tapeCount = Mathf.Min(_painting.TapeCount + additionalTapeCount, MaxTapes);
            _renderPassMaterial.SetTexture(StrokeTexId, _strokeTex);
            _renderPassMaterial.SetFloat(StrokeCountId, strokeCount);
            _renderPassMaterial.SetTexture(TapeTexId, _tapeTex);
            _renderPassMaterial.SetFloat(TapeCountId, tapeCount);
            _renderPassMaterial.SetVector(TargetSizeId,
                new Vector4(
                    _targetRT.width,
                    _targetRT.height,
                    1f / _targetRT.width,
                    1f / _targetRT.height));
            _renderPassMaterial.SetColor(BackgroundId, _background);
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
