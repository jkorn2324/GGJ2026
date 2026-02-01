using UnityEngine;
using UnityEngine.UI;

namespace GGJ2026.Painting
{
    public class Painter : MonoBehaviour
    {
        //NOTE: these states are used for multiple types of input: applying paint, applying tape, removing tape
        public enum PaintInputMode
        {
            DISABLED,
            OPEN,
            STARTING,
            MOVING,
            HELD,
            ENDING
        }


        [SerializeField]
        private GameFlow gameFlow;
        [SerializeField]
        private ArtistCanvasComponent artistCanvasRef;
        
        [Header("Variables")]
        public float paintWidth = 50.0f;
        public float tapeWidth = 10.0f;

        public Vector3 paintStrokeStartPos;
        public Color currentColor;
        public ToolSelectButton.ToolType currentTool;

        public PaintInputMode paintInputMode;

        public float startPaintMovementThreshold = .02f;
        private int _currentRemovedTapeIndex = -1;

        /// <summary>
        /// The current painting.
        /// </summary>
        public ArtistPainting CurrentPainting => gameFlow ? gameFlow.Round?.CurrentPainting : null;

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
            var painting = CurrentPainting;
            if (painting == null || !artistCanvasRef)
            {
                return;
            }
            var mousePos = Input.mousePosition;
            var position = artistCanvasRef.GetRelativeMousePositionInArtistCanvas(Input.mousePosition);
            if (position == null)
            {
                return;
            }
            paintStrokeStartPos = mousePos;
            paintInputMode = PaintInputMode.STARTING;
        }

        //starting the input for removing tape (plays the frame that mouse is down)
        void StartRemoveTapeInput()
        {
            paintInputMode = PaintInputMode.STARTING;
            var painting = CurrentPainting;
            if (painting == null || !artistCanvasRef)
            {
                return;
            }
            paintStrokeStartPos = Input.mousePosition;
        }

        //the user has to move the press enough to actually start painting, just holding the press in a single spot doesn't paint
        void CheckForPaintStrokeMovement()
        {
            var paintStrokeDistance = Input.mousePosition - paintStrokeStartPos;
            // Debug.Log("initial input for paintstroke has distance of: " + paintStrokeDistance.magnitude);
            if (paintStrokeDistance.magnitude >= Screen.width * startPaintMovementThreshold)
            {
                //start movement detection for applying paint or tape
                StartPaintStroke();
            }
        }

        //the user has to move the press enough to actually start removing tape, just holding the press in a single spot doesn't cut it
        void CheckForRemoveTapeMovement()
        {
            var paintStrokeDistance = Input.mousePosition - paintStrokeStartPos;
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
            var painting = CurrentPainting;
            if (painting == null || !painting.IsInitialized || !artistCanvasRef)
            {
                return;
            }
            var position = artistCanvasRef.GetRelativeMousePositionInArtistCanvas(paintStrokeStartPos);
            if (position == null)
            {
                EndPaintStroke();
                return;
            }
            var castedPosition = (Vector2)position;
            if (painting.BeginLine(
                    new ArtistLineInfo(color: currentColor,
                        startPos: castedPosition,
                        inWidth: currentTool == ToolSelectButton.ToolType.PAINT ? paintWidth : tapeWidth,
                        inEdgeRoundness: paintWidth * 0.5f),
                    inIsTape: currentTool == ToolSelectButton.ToolType.TAPE))
            {
                paintInputMode = PaintInputMode.MOVING;
            }
        }

        //Begin removing tape
        //this plays as soon as the amount of input movement is enough to be considered a swipe
        void StartRemoveTape()
        {
            var painting = CurrentPainting;
            if (!artistCanvasRef)
            {
                return;
            }
            var artistCanvasPosition = artistCanvasRef.GetRelativeMousePositionInArtistCanvas(Input.mousePosition);
            if (artistCanvasPosition == null)
            {
                return;
            }
            var tapeAt = painting.GetTapeIndexAt((Vector2)artistCanvasPosition, 1.0f);
            if (tapeAt == -1)
            {
                return;
            }
            _currentRemovedTapeIndex = tapeAt;
            Debug.Log("enough input movement to start removing tape");
            paintInputMode = PaintInputMode.MOVING;
        }

        void EndInputWithoutAction()
        {
            //if user lifted the press without actually starting an acftion(not enough movement)
            //revert to open state without applying paint, tape, or tape removal
            Debug.Log("not enough movement to start action");
            _currentRemovedTapeIndex = -1;
            paintInputMode = PaintInputMode.OPEN;
        }

        //End Stroke(for when player releases, etc)
        //Release Tape(for when player releases the tape)
        void EndPaintStroke()
        {
            Debug.Log("ending paintstroke");
            paintInputMode = PaintInputMode.OPEN;
            
            var painting = CurrentPainting;
            if (painting == null || !painting.IsInitialized || !artistCanvasRef)
            {
                return;
            }
            painting.EndLine();
        }

        //Update Stroke (for when player is setting the direction of the stroke)
        void MovePaintStroke()
        {
            var currentPainting = CurrentPainting;
            if (currentPainting == null || !currentPainting.IsInitialized || !artistCanvasRef)
            {
                return;
            }
            Debug.Log("actively applying paint/tape with movement");
            var currentPosition = Input.mousePosition;
            var relativeMousePosition = artistCanvasRef.GetRelativeMousePositionInArtistCanvas(currentPosition);
            if (relativeMousePosition == null)
            {
                return;
            }
            currentPainting.UpdateLine((Vector2)relativeMousePosition);
        }

        //calls every frame that the user is moving input to remove tape
        void MoveRemovedTape()
        {
            Debug.Log("actively removing tape");
        }

        void EndRemoveTape()
        {
            var currentPainting = CurrentPainting;
            if (currentPainting == null || !currentPainting.IsInitialized)
            {
                return;
            }
            currentPainting.TryRemoveTape(_currentRemovedTapeIndex);
            _currentRemovedTapeIndex = -1;
            Debug.Log("ending input for tape removal");
        }

        public void SelectTool(ToolSelectButton tool)
        {
            if (!tool)
            {
                return;
            }
            currentTool = tool.type;
            currentColor = tool.buttonColor;

            Debug.Log("setting tool to: " + tool.type + " with color: " + tool.buttonColor);
        }
    }
}