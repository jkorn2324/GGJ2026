using GGJ2026.Utils;
using UnityEngine;

namespace GGJ2026.Painting
{
    public struct ArtistLineInfo
    {
        public const float CDefaultEdgeSmoothness = 3.0f;
        public const float CDefaultEdgeRoundness = 1.0f;
        
        public readonly Color Color;
        public readonly Vector2 StartPosition;
        public Vector2 EndPosition;
        public readonly float Width;
        public readonly float EdgeSmoothness;
        public readonly float EdgeRoundness;
        
        public readonly Vector2 Direction => (EndPosition - StartPosition).normalized;

        public ArtistLineInfo(Color color, in Vector2 startPos,
            float inWidth, 
            Vector2? endPos = null,
            float? inEdgeSmoothness = null, 
            float? inEdgeRoundness = null)
        {
            Color = color;
            StartPosition = startPos;
            EndPosition = endPos ?? startPos;
            Width = inWidth;
            EdgeSmoothness = inEdgeSmoothness ?? CDefaultEdgeSmoothness;
            EdgeRoundness = inEdgeRoundness ?? CDefaultEdgeRoundness;
        }
        
        public readonly bool IsPositionInStroke(in Vector2 position, float radiusLeniency,
            out float distanceSqr)
        {
            var halfStrokeWidth = (Width * 0.5f) + radiusLeniency;
            var additionalPosition = Direction * radiusLeniency;
            var newStartPosition = StartPosition + (-additionalPosition);
            var newEndPosition = EndPosition + additionalPosition;
            var closestPointOnSegment =
                (Vector2)MathUtil.GetClosestPointOnLineSegment(position, newStartPosition, newEndPosition);
            distanceSqr = Vector2.SqrMagnitude(closestPointOnSegment - position);
            // The distance 
            if (distanceSqr > (halfStrokeWidth * halfStrokeWidth))
            {
                return false;
            }
            if (closestPointOnSegment == newStartPosition)
            {
                var closestPointToPosition = position - closestPointOnSegment;
                var dotProduct = Vector2.Dot(Direction, closestPointToPosition.normalized);
                // This means that the dot product is in the same direction and we are now good.
                return dotProduct >= 0.0f;
            }
            if (closestPointOnSegment == newEndPosition)
            {
                var closestPointToPosition = position - closestPointOnSegment;
                var dotProduct = Vector2.Dot(-Direction, closestPointToPosition.normalized);
                // This means that the dot product is in the same direction and we are now good.
                return dotProduct >= 0.0f;
            }
            return true;
        }
    }
}