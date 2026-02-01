using System.Threading.Tasks;
using GGJ2026.Utils.Internals;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace GGJ2026.Utils
{
    public static class TextureUtil
    {
        #region clear_render_texture
        
        private static readonly int ClearColorId   = Shader.PropertyToID("_ClearColor");
        private static readonly Shader ClearShader = Shader.Find("GGJ2026/S_ClearTexturePass");

        private static Material _clearMaterial = null;

        private static Material GetOrCreateClearMaterial()
        {
            if (_clearMaterial)
            {
                return _clearMaterial;
            }
            if (!ClearShader)
            {
                return null;
            }
            return _clearMaterial = new Material(ClearShader) { hideFlags = HideFlags.HideAndDontSave };
        }
        
        /// <summary>
        /// Sets the render texture color.
        /// </summary>
        /// <param name="renderTexture">The render texture.</param>
        /// <param name="color">The color.</param>
        /// <returns>True if we set the render texture color.</returns>
        public static bool SetRenderTextureColor(RenderTexture renderTexture, Color color)
        {
            if (!renderTexture)
            {
                return false;
            }
            var clearMaterial = GetOrCreateClearMaterial();
            if (!clearMaterial)
            {
                return false;
            }
            clearMaterial.SetColor(ClearColorId, color);
            Graphics.Blit(null, renderTexture, clearMaterial);
            return true;
        }
        
        #endregion
        
        public static Vector2Int GetCommonSize(Texture textureA, Texture textureB)
        {
            if (!textureA || !textureB)
            {
                return Vector2Int.zero;
            }
            return new Vector2Int(Mathf.Max(textureA.width, textureB.width), 
                Mathf.Max(textureA.height, textureB.height));
        }

        /// <summary>
        /// Compares render textures.
        /// </summary>
        /// <param name="srcA">The first render texture.</param>
        /// <param name="srcB">The second render texture.</param>
        /// <param name="comparisonSize">The comparison size.</param>
        /// <param name="useLuma">Determines whether to use luma.</param>
        /// <param name="ignoreAlpha">Determines whether to ignore alpha.</param>
        /// <param name="differenceTextureCopied">The difference texture copied.</param>
        /// <returns>Task that returns render textures comparison (0 = not similar, 1 = 100% similar)</returns>
        public static Awaitable<float> CompareRenderTextures(RenderTexture srcA, RenderTexture srcB, Vector2Int comparisonSize,
            bool useLuma = true, bool ignoreAlpha = true, RenderTexture differenceTextureCopied = null)
        {
            return RenderTextureCompare.CompareRenderTextures(srcA, srcB, comparisonSize.x, comparisonSize.y, 
                useLuma: useLuma, ignoreAlpha: ignoreAlpha, differenceTextureCopied: differenceTextureCopied);
        }
        
        public static RenderTexture CreateRenderTexture(int width, int height, RenderTextureFormat format)
        {
            return new RenderTexture(width, height, 0, format)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                useMipMap = false,
                autoGenerateMips = false
            };
        }
        
    	public static void ReleaseRenderTexture(ref RenderTexture renderTexture)
        {
            if (!renderTexture)
            {
				renderTexture = null;
                return;
            }
            renderTexture.Release();
            UnityEngine.Object.Destroy(renderTexture);
			renderTexture = null;
        }

        public static unsafe void ClearPixels<T>(Texture2D texture)
            where T : unmanaged
        {
            if (!texture)
            {
                return;
            }
            var nativeTexture = texture.GetRawTextureData<T>();
            UnsafeUtility.MemSet(nativeTexture.GetUnsafePtr(), 0, nativeTexture.Length * sizeof(T));
        }
    }
}