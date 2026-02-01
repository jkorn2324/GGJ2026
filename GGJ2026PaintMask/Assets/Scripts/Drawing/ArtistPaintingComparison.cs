namespace GGJ2026.Painting
{
    public static class ArtistPaintingComparison
    {
        
        public static float CalculateComparisonScore(ArtistPainting original, ArtistPainting compared)
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
            
            // TODO: Implementation - Compare the pixels here.
            ArtistCanvasDrawer.Release(ref originalPainting);
            ArtistCanvasDrawer.Release(ref comparedPainting);
            return 0.0f;
        }
    }
}