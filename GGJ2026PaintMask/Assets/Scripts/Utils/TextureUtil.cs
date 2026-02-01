using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace GGJ2026.Utils
{
    public static class TextureUtil
    {
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