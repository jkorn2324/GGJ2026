using UnityEngine;
using UnityEngine.UI;

public class Painter : MonoBehaviour
{
    public int currentLayerNum = 0;
    public Color currentColor;
    public ToolSelectButton.ToolType currentTool;
    public Painting currentPainting;
    public RectTransform paintingCanvas;

    public enum PaintInputMode { DISABLED, OPEN, STARTING, MOVING, HELD, ENDING }
    public PaintInputMode paintInputMode;

    public Image paintLayer;
    private RectTransform paintLayerRect;
    public Image paintLayerFab;

    public float startPaintMovementThreshold = .02f;

    public float paintWidth = 0.2f;
    public float paintInitialHeight = 0.05f;

    public float tapeWidth = 0.05f;
    public float tapeInitialHeight = 0.02f;

    private float paintStrokeAngle;
    private Vector3 paintStrokeStartPos;
    private Vector3 paintStrokeCurrentPos;
    private Vector3 paintStrokeDistance;

    public bool allowDirectionChangeMidStroke;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        paintInputMode = PaintInputMode.OPEN;
        SetPaintTapeDimensions();
    }

    void SetPaintTapeDimensions()
    {
        paintWidth = SetRelativeToCanvasHeight(paintWidth);
        paintInitialHeight = SetRelativeToCanvasHeight(paintInitialHeight);
        tapeWidth = SetRelativeToCanvasHeight(tapeWidth);
        tapeInitialHeight = SetRelativeToCanvasHeight(tapeInitialHeight);
    }

    float SetRelativeToCanvasHeight(float multiplier)
    {
        float relativeValue;
        relativeValue = multiplier * paintingCanvas.rect.height;
        return relativeValue;
    }

    // Update is called once per frame
    void Update()
    {
        CheckInput();
    }

    void CheckInput()
    {
        if (Input.GetMouseButtonDown(0)){
            if(paintInputMode == PaintInputMode.OPEN && (currentTool == ToolSelectButton.ToolType.PAINT || currentTool == ToolSelectButton.ToolType.TAPE))
            {
                StartPaintStrokeInput();
            }
        }
        if (Input.GetMouseButtonUp(0)){
            if(paintInputMode == PaintInputMode.STARTING)
            {
                EndPaintStrokeInput();
            }
            if (paintInputMode == PaintInputMode.MOVING || paintInputMode == PaintInputMode.HELD)
            {
                EndPaintStroke();
            }
        }
        if (Input.GetMouseButton(0))
        {
            if(paintInputMode == PaintInputMode.STARTING)
            {
                CheckForPaintStrokeMovement();
            }
            if(paintInputMode == PaintInputMode.MOVING)
            {
                MovePaintStroke();
            }
        }
    }

    void StartPaintStrokeInput()
    {
        Debug.Log("starting paint input");
        paintInputMode = PaintInputMode.STARTING;
        paintStrokeStartPos = Input.mousePosition;


    }

    void CheckForPaintStrokeMovement()
    {
        //the user has to move the press enough to actually start painting, just holding the press in a single spot doesn't paint
        paintStrokeDistance = Input.mousePosition - paintStrokeStartPos;
        Debug.Log("initial input has distance of: " + paintStrokeDistance.magnitude);
        if (paintStrokeDistance.magnitude >= Screen.width * startPaintMovementThreshold) 
        {
            StartPaintStroke();
        }
    }

    void StartPaintStroke()
    {
        Debug.Log("enough input movement to start paintstroke");

        paintInputMode = PaintInputMode.MOVING;

        SetPaintStrokeVector();

        paintLayer = GameObject.Instantiate(paintLayerFab);
        paintLayer.color = currentColor;
        paintLayerRect = paintLayer.GetComponent<RectTransform>();
        paintLayerRect.position = paintStrokeStartPos;

        if (currentTool == ToolSelectButton.ToolType.PAINT)
        {
            paintLayerRect.sizeDelta = new Vector2(paintWidth, paintInitialHeight);
        }
        else if (currentTool == ToolSelectButton.ToolType.TAPE)
        {
            paintLayerRect.sizeDelta = new Vector2(tapeWidth, tapeInitialHeight);
        }

        //paintStrokeStartPos = Input.mousePosition;




        paintLayerRect.rotation = Quaternion.Euler(new Vector3(0f,0f,paintStrokeAngle));
        paintLayer.transform.SetParent(currentPainting.transform);
    }

    void EndPaintStrokeInput()
    {
        //if user lifted the press without actually starting a paintstroke (not enough movement)
        //revert to open state without making a paintstroke
        Debug.Log("not enough movement to start paintstroke");
        paintInputMode = PaintInputMode.OPEN;
    }

    void EndPaintStroke()
    {
        Debug.Log("ending paintstroke");
        paintInputMode = PaintInputMode.OPEN;
        //paintInputMode = PaintInputMode.ENDING;
    }

    void MovePaintStroke()
    {
        SetPaintStrokeVector();

        SetPaintStrokeTransform();
    }

    void SetPaintStrokeVector()
    {
        
        paintStrokeDistance = Input.mousePosition - paintStrokeStartPos;
        paintStrokeAngle = Mathf.Atan2(-paintStrokeDistance.x, paintStrokeDistance.y) * Mathf.Rad2Deg;
    }

    void SetPaintStrokeTransform()
    {

        //the length of paint/tape CANNOT decrease, only increase
        //movement of input along the axis of the paint/tape line increases the length
        //movement of input in any other direction (perpendicular to axis, or opposite direction) has no effect on length
        float newPaintLength;


        if (allowDirectionChangeMidStroke)
        {
            newPaintLength = paintStrokeDistance.magnitude;
        }
        else
        {
            if (paintStrokeDistance.magnitude > paintLayerRect.rect.height)
            {
                newPaintLength = paintStrokeDistance.magnitude;
            }
            else
            {
                newPaintLength = paintLayerRect.rect.height;
            }

            
        }

        float width = 0;
        if (currentTool == ToolSelectButton.ToolType.PAINT)
        {
            width = paintWidth;
        }
        else if (currentTool == ToolSelectButton.ToolType.TAPE)
        {
            width = tapeWidth;
        }
        paintLayerRect.sizeDelta = new Vector2(width, newPaintLength);

        if (allowDirectionChangeMidStroke)
        {
            paintLayerRect.rotation = Quaternion.Euler(new Vector3(0f, 0f, paintStrokeAngle));
        }
    }

    public void SelectTool (ToolSelectButton tool)
    {
        currentTool = tool.type;
        currentColor = tool.buttonColor;

        Debug.Log("setting tool to: " + tool.type + " with color: " + tool.buttonColor);
        
    }
}
