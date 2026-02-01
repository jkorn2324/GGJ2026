using UnityEngine;

namespace GGJ2026.Utils
{
    public static class MathUtil
    {
        public static float Cross(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
        }

        public static Vector2 ToUv(Vector2 point, Vector2 size)
        {
            if (size.x <= 0 || size.y <= 0)
            {
                return Vector2.zero;
            }
            return point / size;
        }
        
        public static bool IsPointOnLineSegment(Vector3 point, Vector3 lineStart, Vector3 lineEnd, float lineWidth)
        {
            var halfWidth = lineWidth * 0.5f;
            var sqrDistance = GetSqrDistanceToLineSegment(point, lineStart, lineEnd);
            return sqrDistance <= (halfWidth * halfWidth);
        }
        public static float GetSqrDistanceToLineSegment(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
        {
            var closestPointOnLineSegment = GetClosestPointOnLineSegment(point, lineStart, lineEnd);
            return Vector3.SqrMagnitude(closestPointOnLineSegment - point);
        }
        
        public static Vector3 GetClosestPointOnLineSegment(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
        {
            var line = lineEnd - lineStart;
            var lineLength = line.magnitude;
            if (lineLength < Mathf.Epsilon)
            {
                return lineStart;
            }
            var t = Mathf.Clamp01(Vector2.Dot(point - lineStart, line) / (lineLength * lineLength));
            return lineStart + t * line;
        }
    }
}