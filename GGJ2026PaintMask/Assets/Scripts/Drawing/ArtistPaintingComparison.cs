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
            // TODO: Compare by order of comparison
            // TODO: Compare by pixels?
            return 0.0f;
        }
    }
}