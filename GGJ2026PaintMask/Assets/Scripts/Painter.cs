using UnityEngine;
using UnityEngine.UI;

public class Painter : MonoBehaviour
{
    public int currentLayerNum = 0;
    public Color currentColor;
    public ToolSelectButton.ToolType currentTool;
    public Painting currentPainting;
    public RectTransform paintingCanvas;

    //NOTE: these states are used for multiple types of input: applying paint, applying tape, removing tape
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
        //paintInputMode = PaintInputMode.OPEN;
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

    public void SetPaintInputMode(PaintInputMode newPaintInputMode)
    {
        paintInputMode = newPaintInputMode;
    }

    // Update is called once per frame
    void Update()
    {
        CheckInput();
    }

    void CheckInput()
    {

        
        if (Input.GetMouseButtonDown(0))
        {
            OnInputDown();
        }
        if (Input.GetMouseButtonUp(0))
        {
            OnInputUp();
        }
        if (Input.GetMouseButton(0))
        {
            OnInputHeld();
        }
    }

    //plays first frame when mouse/touch is down
    void OnInputDown()
    {
        if (paintInputMode == PaintInputMode.OPEN)
        {

            if (currentTool == ToolSelectButton.ToolType.PAINT || currentTool == ToolSelectButton.ToolType.TAPE)
            {
                //TO DO: add condition to confirm input happens in the appropriate play area
                StartPaintStrokeInput();
            }
            else if (currentTool == ToolSelectButton.ToolType.REMOVE_TAPE)
            {
                //TO DO: add condition to confirm input happens close enough to the end of a piece of tape
                StartRemoveTapeInput();
            }

        }
    }

    //plays on any frame when mouse/touch is down
    void OnInputHeld()
    {
        //if input state is down, but BEFORE it is confirmed to have enough movement to trigger an action (paint apply, tape apply, tape removal)
        if (paintInputMode == PaintInputMode.STARTING)
        {

            //for applying paint and tape
            if (currentTool == ToolSelectButton.ToolType.PAINT || currentTool == ToolSelectButton.ToolType.TAPE)
            {
                CheckForPaintStrokeMovement();
            }

            //for removing tape
            else if (currentTool == ToolSelectButton.ToolType.REMOVE_TAPE)
            {
                CheckForRemoveTapeMovement();
            }

        }

        //if input state is actively moving, AND has already been confirmed to be an action (paint apply, tape apply, tape removal)
        else if (paintInputMode == PaintInputMode.MOVING)
        {
            //for applying paint and tape
            if (currentTool == ToolSelectButton.ToolType.PAINT || currentTool == ToolSelectButton.ToolType.TAPE)
            {
                //move the paint / tape line
                MovePaintStroke();
            }

            //for removing tape
            else if (currentTool == ToolSelectButton.ToolType.REMOVE_TAPE)
            {
                //adjust the position of the tape being removed
                MoveRemovedTape();
            }

        }
    }

    //plays on frame when mouse/touch is released
    void OnInputUp()
    {
        //if input did NOT have enough movement to confirm as an action (paint apply, tape apply, tape removal)
        if (paintInputMode == PaintInputMode.STARTING)
        {
            EndInputWithoutAction();
        }
        if (paintInputMode == PaintInputMode.MOVING || paintInputMode == PaintInputMode.HELD)
        {
            if (currentTool == ToolSelectButton.ToolType.PAINT || currentTool == ToolSelectButton.ToolType.TAPE)
            {
                EndPaintStroke();
            }
            else if (currentTool == ToolSelectButton.ToolType.REMOVE_TAPE)
            {
                EndRemoveTape();
            }
        }
    }

    //starting the input for applying paint or tape (plays the frame that mouse is down)
    void StartPaintStrokeInput()
    {
        Debug.Log("starting paint input");
        paintInputMode = PaintInputMode.STARTING;
        paintStrokeStartPos = Input.mousePosition;
    }

    //starting the input for removing tape (plays the frame that mouse is down)
    void StartRemoveTapeInput()
    {
        Debug.Log("starting input for removing tape");
        paintInputMode = PaintInputMode.STARTING;
        paintStrokeStartPos = Input.mousePosition;
    }

    //the user has to move the press enough to actually start painting, just holding the press in a single spot doesn't paint
    void CheckForPaintStrokeMovement()
    {
        paintStrokeDistance = Input.mousePosition - paintStrokeStartPos;
        //Debug.Log("initial input for paintstroke has distance of: " + paintStrokeDistance.magnitude);
        if (paintStrokeDistance.magnitude >= Screen.width * startPaintMovementThreshold) 
        {
            //start movement detection for applying paint or tape
            StartPaintStroke();
        }
    }

    //the user has to move the press enough to actually start removing tape, just holding the press in a single spot doesn't cut it
    void CheckForRemoveTapeMovement()
    {
        paintStrokeDistance = Input.mousePosition - paintStrokeStartPos;
        Debug.Log("initial input for tape removal has distance of: " + paintStrokeDistance.magnitude);
        if (paintStrokeDistance.magnitude >= Screen.width * startPaintMovementThreshold)
        {
            //start movement detection for applying paint or tape
            StartRemoveTape();
        }
    }

    //Begin Stroke (for both tape & regular strokes)
    //this plays as soon as the amount of input movement is enough to be considered a swipe
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

    //Begin removing tape
    //this plays as soon as the amount of input movement is enough to be considered a swipe
    void StartRemoveTape()
    {
        Debug.Log("enough input movement to start removing tape");

        paintInputMode = PaintInputMode.MOVING;

        
    }

    void EndInputWithoutAction()
    {
        //if user lifted the press without actually starting an acftion(not enough movement)
        //revert to open state without applying paint, tape, or tape removal
        Debug.Log("not enough movement to start action");
        paintInputMode = PaintInputMode.OPEN;
    }


    //End Stroke(for when player releases, etc)
    //Release Tape(for when player releases the tape)
    void EndPaintStroke()
    {
        Debug.Log("ending paintstroke");
        paintInputMode = PaintInputMode.OPEN;
        //paintInputMode = PaintInputMode.ENDING;
    }

    //Update Stroke (for when player is setting the direction of the stroke)
    void MovePaintStroke()
    {
        Debug.Log("actively applying paint/tape with movement");
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

    //calls every frame that the user is moving input to remove tape
    void MoveRemovedTape()
    {
            Debug.Log("actively removing tape");
    }

    void EndRemoveTape()
    {
        Debug.Log("ending input for tape removal");
    }

    public void SelectTool (ToolSelectButton tool)
    {
        currentTool = tool.type;
        currentColor = tool.buttonColor;

        Debug.Log("setting tool to: " + tool.type + " with color: " + tool.buttonColor);
        
    }
}
