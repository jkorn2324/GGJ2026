using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace GGJ2026.Utils.Internals
{
    internal static class RenderTextureCompare
    {
        public static async Awaitable<float> CompareRenderTextures(
            RenderTexture srcA,
            RenderTexture srcB,
            int comparisonWidth,
            int comparisonHeight,
            RenderTexture differenceTextureCopied = null,
            bool useLuma = true,
            bool ignoreAlpha = true)
        {
            if (!srcA || !srcB || comparisonWidth <= 0 || comparisonHeight <= 0)
            {
                return 0f;
            }

            var reductionShaderMaterial = GetOrCreateMaterial();
            if (!reductionShaderMaterial)
            {
                return 0f;
            }

            reductionShaderMaterial.SetFloat(ShaderIDs.UseLuma, useLuma ? 1f : 0f);
            reductionShaderMaterial.SetFloat(ShaderIDs.IgnoreAlpha, ignoreAlpha ? 1f : 0f);

            RenderTexture aTextureNormalizedSize = null;
            RenderTexture bTextureNormalizedSize = null;
            RenderTexture differenceTexture = null;
            RenderTexture reductionChainTexture = null;
            RenderTexture outputReadbackTexture = null;
            var normalizedTextureFormat = GetBestSingleChannelOrColorFormat();
#if TEST_READBACK
            aTextureNormalizedSize = CreateTemporaryRenderTexture(comparisonWidth, comparisonHeight,
                normalizedTextureFormat, "RTSim_A");
            Graphics.Blit(Texture2D.blackTexture, aTextureNormalizedSize);
            var result = await ReadOnePixelRAsync(aTextureNormalizedSize);
            result = Mathf.Clamp01(result);
            // Calculates the similarity here.
            var similarity = 1f - result;
            return Mathf.Clamp01(similarity);
#else
            try
            {
                aTextureNormalizedSize = CreateTemporaryRenderTexture(comparisonWidth, comparisonHeight,
                    normalizedTextureFormat, "RTSim_A");
                bTextureNormalizedSize = CreateTemporaryRenderTexture(comparisonWidth, comparisonHeight,
                    normalizedTextureFormat, "RTSim_B");
                // Normalize/resample sources to comparison size (also resolves MSAA if present).
                {
                    TextureUtil.SetRenderTextureColor(aTextureNormalizedSize, Color.clear);
                    TextureUtil.SetRenderTextureColor(bTextureNormalizedSize, Color.clear);
                    Graphics.Blit(srcA, aTextureNormalizedSize);
                    Graphics.Blit(srcB, bTextureNormalizedSize);
                }
                // Diff into a single-channel error RT if available; else use ARGB32 and read R.
                {
                    differenceTexture = CreateTemporaryRenderTexture(comparisonWidth, comparisonHeight,
                        GraphicsFormat.R8G8B8A8_UNorm, "RTSim_Diff");
                    differenceTexture.filterMode = FilterMode.Point;
                    TextureUtil.SetRenderTextureColor(differenceTexture, Color.clear);
                    reductionShaderMaterial.SetTexture(ShaderIDs.TexA, aTextureNormalizedSize);
                    reductionShaderMaterial.SetTexture(ShaderIDs.TexB, bTextureNormalizedSize);
                    Graphics.Blit(null, differenceTexture, reductionShaderMaterial, pass: 0);
                    if (differenceTextureCopied)
                    {
                        Graphics.Blit(differenceTexture, differenceTextureCopied);
                    }
                }
                // Downsample chain until 1x1 average error.
                {
                    reductionChainTexture = differenceTexture;
                    var w = comparisonWidth;
                    var h = comparisonHeight;
                    while (w > 1 || h > 1)
                    {
                        var nw = Mathf.Max(1, (w + 1) / 2);
                        var nh = Mathf.Max(1, (h + 1) / 2);
                        var next = CreateTemporaryRenderTexture(nw, nh, reductionChainTexture.graphicsFormat,
                            "RTSim_Reduce");
                        next.filterMode = FilterMode.Point;
                        reductionShaderMaterial.SetVector(ShaderIDs.InvSrcSize, new Vector4(1f / w, 1f / h, w, h));
                        reductionShaderMaterial.SetTexture(ShaderIDs.TexReduction, reductionChainTexture);
                        Graphics.Blit(null, next, reductionShaderMaterial, pass: 1);
                        if (reductionChainTexture != differenceTexture)
                        {
                            RenderTexture.ReleaseTemporary(reductionChainTexture);
                        }

                        reductionChainTexture = next;
                        w = nw;
                        h = nh;
                    }
                    outputReadbackTexture = reductionChainTexture;
                    reductionChainTexture = null;
                }
                var avgError = await ReadOnePixelRAsync(outputReadbackTexture);
                avgError = Mathf.Clamp01(avgError);
                // Calculates the similarity here.
                var similarity = 1f - avgError;
                return Mathf.Clamp01(similarity);
            }
            finally
            {
                if (reductionChainTexture && reductionChainTexture != differenceTexture)
                {
                    RenderTexture.ReleaseTemporary(reductionChainTexture);
                }

                if (outputReadbackTexture && outputReadbackTexture != differenceTexture)
                {
                    RenderTexture.ReleaseTemporary(outputReadbackTexture);
                }

                if (differenceTexture)
                {
                    RenderTexture.ReleaseTemporary(differenceTexture);
                }

                if (aTextureNormalizedSize)
                {
                    RenderTexture.ReleaseTemporary(aTextureNormalizedSize);
                }

                if (bTextureNormalizedSize)
                {
                    RenderTexture.ReleaseTemporary(bTextureNormalizedSize);
                }
            }
#endif
        }

        private static readonly Shader ReduceShader = Shader.Find("GGJ2026/S_DiffRTReduceShader");
        private static Material _mat;

        private static Material GetOrCreateMaterial()
        {
            if (_mat)
            {
                return _mat;
            }

            if (!ReduceShader)
            {
                return null;
            }

            _mat = new Material(ReduceShader) { hideFlags = HideFlags.HideAndDontSave };
            return _mat;
        }

        private static RenderTexture CreateTemporaryRenderTexture(int width, int height, GraphicsFormat format,
            string name)
        {
            var desc = new RenderTextureDescriptor(width, height)
            {
                depthBufferBits = 0,
                msaaSamples = 1,
                mipCount = 1,
                useMipMap = false,
                autoGenerateMips = false,
                enableRandomWrite = false,
                sRGB = false,
                graphicsFormat = format
            };
            var rt = RenderTexture.GetTemporary(desc);
            rt.name = name;
            rt.filterMode = FilterMode.Bilinear;
            rt.wrapMode = TextureWrapMode.Clamp;
            return rt;
        }

        private static GraphicsFormat GetBestSingleChannelOrColorFormat()
        {
            if (SystemInfo.IsFormatSupported(GraphicsFormat.R8G8B8A8_UNorm, GraphicsFormatUsage.Render))
            {
                return GraphicsFormat.R8G8B8A8_UNorm;
            }
            return SystemInfo.GetGraphicsFormat(DefaultFormat.LDR);
        }

        private static async Awaitable<float> ReadOnePixelRAsync(RenderTexture rt1X1)
        {
            if (!SystemInfo.supportsAsyncGPUReadback || Application.platform == RuntimePlatform.WebGLPlayer)
            {
                return ReadOnePixelRSync(rt1X1);
            }
            var request = AsyncGPUReadback.Request(rt1X1);
            while (!request.done)
            {
                await Awaitable.NextFrameAsync();
            }
            if (request.hasError)
            {
                return ReadOnePixelRSync(rt1X1);
            }
            // Because we are assuming that the output texture is RGBA Unorm.
            var data = request.GetData<Color32>();
            var value = (float)data[0].r / 255f;
            return Mathf.Clamp01(value);
        }

        private static float ReadOnePixelRSync(RenderTexture rt1X1)
        {
            var prev = RenderTexture.active;
            RenderTexture.active = rt1X1;
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, mipChain: false, linear: true);
            tex.ReadPixels(new Rect(0, 0, 1, 1), 0, 0, recalculateMipMaps: false);
            tex.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            var pixels = tex.GetRawTextureData<Color32>();
            var v = pixels[0].r;
            Object.Destroy(tex);
            RenderTexture.active = prev;
            return Mathf.Clamp01(v);
        }

        private static class ShaderIDs
        {
            public static readonly int TexA = Shader.PropertyToID("_TexA");
            public static readonly int TexB = Shader.PropertyToID("_TexB");
            public static readonly int TexReduction = Shader.PropertyToID("_TexReduction");
            public static readonly int UseLuma = Shader.PropertyToID("_UseLuma");
            public static readonly int IgnoreAlpha = Shader.PropertyToID("_IgnoreAlpha");
            public static readonly int InvSrcSize = Shader.PropertyToID("_InvSrcSize");
        }
    }
}