using System;
using GGJ2026.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace GGJ2026.Painting.Tests
{
    public class RenderTextureCompareTest : MonoBehaviour
    {
        [SerializeField]
        private Texture2D firstTexture;
        [SerializeField]
        private Texture2D secondTexture;
        [Space] 
        [SerializeField] 
        private bool ignoreAlpha;
        [SerializeField]
        private bool useLuma;

        [SerializeField]
        private RawImage firstImage;
        [SerializeField]
        private RawImage secondImage;
        [SerializeField]
        private RawImage differenceImage;

        private async Awaitable Start()
        {
            var commonSize = TextureUtil.GetCommonSize(firstTexture, secondTexture);
            var firstRenderTexture = RenderTexture.GetTemporary(firstTexture.width, firstTexture.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(firstTexture, firstRenderTexture);
            var secondRenderTexture = RenderTexture.GetTemporary(secondTexture.width, secondTexture.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(secondTexture, secondRenderTexture);
            firstImage.texture = firstRenderTexture;
            secondImage.texture = secondRenderTexture;
            
            var differenceTextureCopy = RenderTexture.GetTemporary(commonSize.x, commonSize.y, 0, RenderTextureFormat.ARGB32);
            var result = await TextureUtil.CompareRenderTextures(firstRenderTexture, secondRenderTexture,
                commonSize, useLuma: useLuma, ignoreAlpha: ignoreAlpha, differenceTextureCopied: differenceTextureCopy);
            differenceImage.texture = differenceTextureCopy;
            
            // Comparison Result.
            Debug.Log($"Result: {result}");
            var totalFramesDelayed = 60 * 60;
            while (totalFramesDelayed > 0)
            {
                await Awaitable.NextFrameAsync();
                totalFramesDelayed--;
            }
            RenderTexture.ReleaseTemporary(firstRenderTexture);
            RenderTexture.ReleaseTemporary(secondRenderTexture);
            RenderTexture.ReleaseTemporary(differenceTextureCopy);
        }
    }
}