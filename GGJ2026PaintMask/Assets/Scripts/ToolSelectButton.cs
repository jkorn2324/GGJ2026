using UnityEngine;

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

    /*public void SetupButton(ToolType = null, color = null)
    {

        switch (type)
        {
            case ToolType.PAINT:
                break;
            case ToolType.TAPE:
                buttonColor = colorSwatch.TAPE;
                break;
        }
    }*/

    // Update is called once per frame
    void Update()
    {
        
    }
}
