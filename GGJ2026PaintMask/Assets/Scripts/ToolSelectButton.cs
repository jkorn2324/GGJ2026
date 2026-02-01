using UnityEngine;
using UnityEngine.UI;

public class ToolSelectButton : MonoBehaviour
{

    public enum ToolType {PAINT, TAPE, REMOVE_TAPE, UNDO, RESET}
    public ToolType type;
    public ColorSwatch colorSwatch;
    public Color buttonColor;

    public float hoverDistance;
    public float hoverLoopTime;
    private float hoverTimer;
    public bool activeTool;
    public Vector3 startPosition;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetupButton();
    }

    public void SetupButton()
    {
        startPosition = this.transform.position;   

        /*switch (type)
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
        }*/
    }

    // Update is called once per frame
    void Update()
    {
        if (activeTool)
        {
            HoverActiveTool();
        }
    }

    void HoverActiveTool()
    {
        float yPos = Mathf.Sin(hoverTimer * hoverLoopTime * Mathf.PI * 2f) * hoverDistance;
        this.transform.position = new Vector3(this.transform.position.x, startPosition.y + yPos, this.transform.position.z);
        hoverTimer += Time.deltaTime;
    }

    public void SetActiveTool()
    {
        hoverTimer = 0;
        this.activeTool = true;
        this.transform.parent.GetComponent<ToolSelectButtonGroup>().DeactivateOtherToolButtons(this.transform);
    }

    public void SetInactiveTool()
    {
        this.activeTool = false;
        this.transform.position = startPosition;
    }
}
