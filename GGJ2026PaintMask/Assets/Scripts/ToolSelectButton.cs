using UnityEngine;
using UnityEngine.UI;

public class ToolSelectButton : MonoBehaviour
{

    public enum ToolType {PAINT, TAPE, REMOVE_TAPE, UNDO, RESET}
    public ToolType type;
    public ColorSwatch colorSwatch;
    public Color buttonColor;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //SetupButton();
    }

    public void SetupButton(ToolType toolType, Color color)
    {
        

        switch (type)
        {
            case ToolType.PAINT:
                if (color != null) {
                    this.GetComponent<Image>().color = color;
                    buttonColor = color;
                }
                    break;
            case ToolType.TAPE:
                //buttonColor = colorSwatch.TAPE;
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
