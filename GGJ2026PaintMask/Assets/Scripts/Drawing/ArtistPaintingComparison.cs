using GGJ2026.Utils;
using UnityEngine;

namespace GGJ2026.Painting
{
    /// <summary>
    /// The artist painting comparison.
    /// </summary>
    public static class ArtistPaintingComparison
    {
        /// <summary>
        /// Calculates the comparison score.
        /// </summary>
        /// <param name="original">The original painting.</param>
        /// <param name="compared">The compared painting.</param>
        /// <returns>An awaitable for score.</returns>
        public static async Awaitable<float> CalculatePaintingPixelsComparison(ArtistPainting original, ArtistPainting compared)
        {
            if (original == null 
                || compared == null
                || !original.IsInitialized
                || !compared.IsInitialized)
            {
                return 0.0f;
            }
            // Initialize the original vs compared.
            var originalPainting = ArtistCanvasDrawer.New();
            originalPainting.SetPainting(original);
            originalPainting.Render();

            var comparedPainting = ArtistCanvasDrawer.New();
            comparedPainting.SetPainting(compared);
            comparedPainting.Render();
            
            var commonSize = TextureUtil.GetCommonSize(
                originalPainting.TargetRT, comparedPainting.TargetRT);
            var result = await TextureUtil.CompareRenderTextures(
                originalPainting.TargetRT, comparedPainting.TargetRT, commonSize,
                ignoreAlpha: false, useLuma: false);
            ArtistCanvasDrawer.Release(ref originalPainting);
            ArtistCanvasDrawer.Release(ref comparedPainting);
            return result;
        }
    }
}