using UnityEngine;
using System.Collections.Generic;

namespace GGJ2026.Painting
{
    public class ColorSwatch : MonoBehaviour
    {
        public List<Color> paintColors;
        public Color TAPE;
        public Color BLACK;
        public Color WHITE;

        public bool randomRGBColorScheme;

        public ToolSelectButton randomPaintButtonRed;
        public ToolSelectButton randomPaintButtonBlue;
        public ToolSelectButton randomPaintButtonGreen;

        public float hueRedMin;
        public float hueRedMax;
        public float hueGreenMin;
        public float hueGreenMax;
        public float hueBlueMin;
        public float hueBlueMax;
        public float valueMin;
        public float valueMax;
        public float saturationMin;
        public float saturationMax;

        public Color randomRed;
        public Color randomBlue;
        public Color randomGreen;



        void Start()
        {
            if (randomRGBColorScheme)
            {
                SetRandomRGBColorScheme();
                SetRandomButtonColors();
            }
        }

        void SetRandomRGBColorScheme()
        {
            float hueRed = Random.Range(hueRedMin, hueRedMax);
            float hueBlue = Random.Range(hueBlueMin, hueBlueMax);
            float hueGreen = Random.Range(hueGreenMin, hueGreenMax);

            float saturation = Random.Range(saturationMin, saturationMax);
            float value = Random.Range(valueMin, valueMax);

            randomRed = Color.HSVToRGB(hueRed, saturation, value);
            randomBlue = Color.HSVToRGB(hueBlue, saturation, value);
            randomGreen = Color.HSVToRGB(hueGreen, saturation, value);
        }

        void SetRandomButtonColors()
        {
            randomPaintButtonRed.SetColor(randomRed);
            randomPaintButtonBlue.SetColor(randomBlue);
            randomPaintButtonGreen.SetColor(randomGreen);  
        }

        

        
    }
}
