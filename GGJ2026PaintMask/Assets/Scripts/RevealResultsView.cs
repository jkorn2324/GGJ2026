using System;
using System.Collections;
using UnityEngine;

namespace GGJ2026.Painting
{
    public class RevealResultsView : MonoBehaviour
    {
        [Header("References")] 
        [SerializeField, Tooltip("The canvas group reference.")]
        private CanvasGroup revealCanvasGroupRef;
        
        [SerializeField, Tooltip("The left painting reference.")]
        private ArtistCanvasComponent leftPaintingRef;
        [SerializeField, Tooltip("The right painting reference.")]
        private ArtistCanvasComponent rightPaintingRef;

        [Header("Variables")] 
        [SerializeField, Tooltip("The visibility animation time.")]
        private float visibilityAnimationTime = 1.0f;

        private void Start()
        {
            SetVisible(false, false);
        }

        public void SetVisible(bool isVisible, bool animate)
        {
            if (animate)
            {
                AnimateVisibility(visibilityAnimationTime, isVisible);
                return;
            }
            if (revealCanvasGroupRef)
            {
                revealCanvasGroupRef.alpha = isVisible ? 1.0f : 0.0f;
                revealCanvasGroupRef.gameObject.SetActive(isVisible);
            }
        }

        private async void AnimateVisibility(float time, bool visible)
        {
            if (visible)
            {
                if (revealCanvasGroupRef)
                {
                    revealCanvasGroupRef.alpha = 0.0f;
                    revealCanvasGroupRef.gameObject.SetActive(true);
                }
            }
            var currentTime = time;
            while (currentTime > 0.0f)
            {
                await Awaitable.NextFrameAsync();
                currentTime -= Time.deltaTime;
                var currentAlpha = Mathf.Clamp01(time - currentTime);
                if (revealCanvasGroupRef)
                {
                    revealCanvasGroupRef.alpha = visible ? currentAlpha : (1.0f - currentAlpha);
                }
            }
            if (revealCanvasGroupRef)
            {
                revealCanvasGroupRef.alpha = visible ? 1.0f : 0.0f;
                if (!visible)
                {
                    revealCanvasGroupRef.gameObject.SetActive(false);
                }
            }
        }
        
        public void SetPaintings(ArtistPainting left, ArtistPainting right)
        {
            if (leftPaintingRef)
            {
                leftPaintingRef.SetPainting(left);
            }
            if (rightPaintingRef)
            {
                rightPaintingRef.SetPainting(right);
            }
        }
    }
}