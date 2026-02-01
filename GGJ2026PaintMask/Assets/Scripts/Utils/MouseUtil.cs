using UnityEngine;

namespace GGJ2026.Painting
{
    public static class MouseUtil
    {
        public static Vector2? TryGetLocalPointInRect(
            RectTransform rectTransform,
            Vector2 screenPoint,
            Camera eventCamera,
            bool clampToCanvas)
        {
            if (!rectTransform)
            {
                return null;
            }
            var result = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, screenPoint, eventCamera, out var localPoint);
            var rect = rectTransform.rect;
            localPoint += rect.size;
            if (!result && !clampToCanvas)
            {
                return null;
            }
            if (clampToCanvas)
            {
                localPoint.x = Mathf.Clamp(localPoint.x, rect.xMin, rect.xMax);
                localPoint.y = Mathf.Clamp(localPoint.y, rect.yMin, rect.yMax);
            }
            return localPoint;
        }
        
        public static Vector2? TryGetNormalizedLocalPointInRect(
            RectTransform rectTransform,
            Vector2 screenPoint,
            Camera eventCamera,
            bool clampToCanvas)
        {
            var localPoint = TryGetLocalPointInRect(rectTransform, screenPoint, eventCamera, clampToCanvas);
            if (localPoint == null)
            {
                return null;
            }
            var bounds = rectTransform.rect;
            var localPointVector = (Vector2)localPoint;
            Debug.Log(localPointVector);
            return localPointVector / bounds.size;
        }
    }
}