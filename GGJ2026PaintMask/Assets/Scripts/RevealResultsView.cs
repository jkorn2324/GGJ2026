using System.Collections;
using UnityEngine;

namespace GGJ2026.Painting
{
    public class RevealResultsView : MonoBehaviour
    {
        [Header("References")] 
        [SerializeField, Tooltip("The canvas group reference.")]
        private GameObject parentRef;
        [SerializeField, Tooltip("The canvas group reference.")]
        private CanvasGroup revealCanvasGroupRef;
        
        [SerializeField, Tooltip("The left painting reference.")]
        private ArtistCanvasComponent leftPaintingRef;
        [SerializeField, Tooltip("The right painting reference.")]
        private ArtistCanvasComponent rightPaintingRef;

        [Header("Variables")] 
        [SerializeField, Tooltip("The visibility animation time.")]
        private float visibilityAnimationTime = 1.0f;
        
        public void SetVisible(bool isVisible, bool animate)
        {
            
        }

        private IEnumerable AnimateVisibility(float time)
        {
            
        }
        
        public void SetLeftPainting(ArtistPainting left, ArtistPainting right)
        {
            
        }
    }
}