using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace GGJ2026.Painting
{
    /// <summary>
    /// The artist canvas component.
    /// </summary>
    [DisallowMultipleComponent]
    public class ArtistCanvasComponent : MonoBehaviour
    {
        [Header("Variables")]
        [SerializeField, Tooltip("The background color.")]
        private Color backgroundColor = Color.clear;

        [Header("References")] 
        [SerializeField, Tooltip("The raw image.")]
        private RawImage rawImage;
        
        private ArtistCanvasDrawer _drawer;
        private RectTransform _rawImageRect;

        private List<ArtistPainting.Stroke> _strokes;

        /// <summary>
        /// The canvas image rect.
        /// </summary>
        public RectTransform CanvasImageRect
        {
            get
            {
                if (!_rawImageRect)
                {
                    _rawImageRect = rawImage ? rawImage.rectTransform : transform as RectTransform;
                }
                return _rawImageRect;
            }
        }

        public ArtistPainting Painting => _drawer?.Painting;

        private void Awake()
        {
            _drawer = ArtistCanvasDrawer.New(background: backgroundColor);
            if (rawImage)
            {
                rawImage.texture = _drawer.TargetRT;
            }
        }

        private void OnDestroy()
        {
            ArtistCanvasDrawer.Release(ref _drawer);
            if (_strokes != null)
            {
                ListPool<ArtistPainting.Stroke>.Release(_strokes);
                _strokes = null;
            }
        }

        /// <summary>
        /// Gets the mouse position relative to this canvas.
        /// </summary>
        /// <param name="mouseScreenPosition">The mouse screen position.</param>
        /// <param name="clampToCanvas">Determines whether or not to clamp to canvas.</param>
        /// <returns>The canvas.</returns>
        public Vector2? GetRelativeMousePositionInArtistCanvas(Vector2 mouseScreenPosition, bool clampToCanvas = false)
        {
            var canvasImageRect = CanvasImageRect;
            var normalized = !canvasImageRect ? null : 
                MouseUtil.TryGetNormalizedLocalPointInRect(canvasImageRect, mouseScreenPosition, Camera.main, clampToCanvas);
            var painting = Painting;
            if (normalized == null || painting == null)
            {
                return null;
            }
            var paintingSize = painting.PaintingSize;
            return paintingSize * (Vector2)normalized;
        }

        /// <summary>
        /// Sets the painting.
        /// </summary>
        /// <param name="painting">The painting.</param>
        public void SetPainting(ArtistPainting painting)
        {
            _drawer?.SetPainting(painting);
        }

        private void LateUpdate()
        {
            if (rawImage && rawImage.texture != _drawer.TargetRT)
            {
                rawImage.texture = _drawer.TargetRT;
            }
            // Renders the painting here.
            _drawer?.Render();
        }
    }
}