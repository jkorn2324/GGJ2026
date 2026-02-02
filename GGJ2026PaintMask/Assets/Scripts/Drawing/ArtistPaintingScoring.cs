using System.Threading.Tasks;
using GGJ2026.Utils;
using UnityEngine;
using UnityEngine.Serialization;

namespace GGJ2026.Painting
{
    public static class ArtistPaintingScoring
    {
        /// <summary>
        /// The comparison settings.
        /// </summary>
        [System.Serializable]
        public struct ScoringSettings
        {
            [SerializeField, Tooltip("The total points for image comparison."), Min(0.0f)]
            private float highestPossibleImageComparePoints;
            [SerializeField, Tooltip("The total points for tape."), Min(0.0f)]
            private float highestPossibleTapePoints;
            [Space]
            [SerializeField, Tooltip("The minimum score for the forgers to win."), Min(0.0f)]
            private float minimumPointsForForgerToWin;
            [Space]
            [SerializeField, Tooltip("The total points deducted from tape."), Min(0.0f)]
            private float pointsDeductedForEachTape;
            
            public readonly float MinScoreForgersToWin => minimumPointsForForgerToWin;

            public readonly float PointsDeductedPerTape => pointsDeductedForEachTape;

            public readonly float MaxTapePoints => highestPossibleTapePoints;

            public readonly float MaxImageComparePoints => highestPossibleImageComparePoints;

            public ScoringSettings(float inHighestPossibleImageComparePoints, float inHighestPossibleTapePoints,
                float inMinimumPointsForForgersToWin, float inPointsDeducted)
            {
                highestPossibleImageComparePoints = inHighestPossibleImageComparePoints;
                highestPossibleTapePoints = inHighestPossibleTapePoints;
                minimumPointsForForgerToWin = inMinimumPointsForForgersToWin;
                pointsDeductedForEachTape = inPointsDeducted;
            }

            /// <summary>
            /// Determines whether the forgers won.
            /// </summary>
            /// <param name="forgerScore">The forger score.</param>
            /// <returns>True if the forgers won or not.</returns>
            public readonly bool DidForgerWin(float forgerScore)
            {
                return forgerScore >= MinScoreForgersToWin;
            }
        }

        /// <summary>
        /// Calculates the tape score.
        /// </summary>
        /// <param name="original">The original points.</param>
        /// <param name="maximumTapePoints">The maximum tape points.</param>
        /// <param name="perTapeDeduction">The per tape deduction.</param>
        /// <returns>The calculated tape score.</returns>
        public static float CalculateTapeScore(ArtistPainting original,
            float maximumTapePoints, float perTapeDeduction)
        {
            return original == null ? 0.0f : Mathf.Max(0.0f, maximumTapePoints - (perTapeDeduction * original.ActiveTapeCount));
        }

        /// <summary>
        /// Calculates the forger score.
        /// </summary>
        /// <param name="scoringSettings">The scoring settings.</param>
        /// <param name="originalPainting">The original painting.</param>
        /// <param name="forgerPainting">The forger painting.</param>
        /// <returns>True if we calculated the score.</returns>
        public static async Awaitable<float> CalculateForgerScore(ScoringSettings scoringSettings,
            ArtistPainting originalPainting, ArtistPainting forgerPainting)
        {
            if (originalPainting == null || forgerPainting == null)
            {
                return 0.0f;
            }
            var comparison = await ArtistPaintingComparison.CalculatePaintingPixelsComparison(
                originalPainting, forgerPainting);
            var originalTapeScore = CalculateTapeScore(originalPainting,
                scoringSettings.MaxTapePoints,
                scoringSettings.PointsDeductedPerTape);
            return comparison + originalTapeScore;
        }
    }
}