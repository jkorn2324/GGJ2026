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
        [SerializeField, Tooltip("The width.")]
        private int width = 1080;
        [SerializeField, Tooltip("The height.")]
        private int height = 720;

        [Header("References")] 
        [SerializeField, Tooltip("The raw image.")]
        private RawImage rawImage;
        
        private ArtistCanvasDrawer _drawer;
        private ArtistPainting _painting;

        private List<ArtistPainting.Stroke> _strokes;

        private void Awake()
        {
            _painting = ArtistPainting.New();
            _drawer = ArtistCanvasDrawer.New(width, height, painting: _painting);
            if (rawImage)
            {
                rawImage.texture = _drawer.TargetRT;
            }
        }

        private void OnEnable()
        {
            _painting.BeginLine(
                new ArtistLineInfo(
                    startPos: Vector2.zero, 
                    color: Color.red, 
                    inWidth: 10.0f), false);
            _painting.UpdateLine(new Vector2(200.0f, height * 0.9f));
            _painting.EndLine();

            _painting.BeginLine(new ArtistLineInfo(
                startPos: Vector2.zero, 
                color: Color.yellow, 
                inWidth: 50.0f), false);
            _painting.UpdateLine(new Vector2(500.0f, height * 0.5f));
            _painting.EndLine();
            
            // Create Tape.
            _painting.BeginLine(new ArtistLineInfo(
                startPos: Vector2.zero,
                color: new Color(1.0f, 1.0f, 1.0f, 0.5f),
                inWidth: 50.0f,
                inEdgeRoundness: 0.0f,
                inEdgeSmoothness: 0.0f), inIsTape: true);
            _painting.UpdateLine(new Vector2(300, height * 0.25f));
            var tapeInfo = _painting.EndLine();
            
            _painting.BeginLine(new ArtistLineInfo(
                startPos: Vector2.zero,
                color: Color.green,
                inWidth: 50.0f), inIsTape: false);
            _painting.UpdateLine(new Vector2(500.0f, height * 0.9f));
            _painting.EndLine();
            
            _painting.BeginLine(new ArtistLineInfo(
                startPos: Vector2.zero,
                color: Color.grey,
                inWidth: 50.0f), inIsTape: false);
            _painting.UpdateLine(new Vector2(300, height * 0.25f));
            _painting.EndLine();

            _painting.BeginLine(new ArtistLineInfo(
                startPos: new Vector2(0.0f, 100.0f),
                color: Color.magenta,
                inWidth: 10.0f), inIsTape: false);
            _painting.UpdateLine(new Vector2(600.0f, 100.0f));
            _painting.EndLine();
            
            _painting.TryRemoveTape(tapeInfo.TapeIndex);
            
            _painting.BeginLine(new ArtistLineInfo(
                startPos: new Vector2(10.0f, 50.0f),
                color: Color.magenta,
                inWidth: 10.0f), inIsTape: false);
            _painting.UpdateLine(new Vector2(500.0f, 50.0f));
            _painting.EndLine();
        }

        private void OnDestroy()
        {
            ArtistCanvasDrawer.Release(ref _drawer);
            ArtistPainting.Release(ref _painting);
            if (_strokes != null)
            {
                ListPool<ArtistPainting.Stroke>.Release(_strokes);
                _strokes = null;
            }
        }

        private void LateUpdate()
        {
            _drawer.Render();
        }
    }
}