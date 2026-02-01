using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GGJ2026.Painting
{
    public class ToolSelectButton : MonoBehaviour
    {
        public enum ToolType
        {
            PAINT,
            TAPE,
            REMOVE_TAPE,
            UNDO,
            RESET
        }

        [SerializeField, Tooltip("The audio.")]
        private new AudioManager audio;
        [SerializeField, Tooltip("The button group reference.")]
        private ToolSelectButtonGroup buttonGroupRef;

        [SerializeField, Tooltip("The button reference.")]
        private Button buttonRef;

        public ToolType type;
        public Color buttonColor;

        public float hoverDistance;
        public float hoverLoopTime;
        private float hoverTimer;
        public bool activeTool;
        public Vector3 startPosition;
        
        private UnityAction _onClick;

        private void Awake()
        {
            _onClick = OnButtonClicked;
            if (!buttonGroupRef)
            {
                buttonGroupRef = GetComponentInParent<ToolSelectButtonGroup>();
            }
        }

        private void Start()
        {
            SetupButton();
        }

        private void OnEnable()
        {
            if (buttonRef)
            {
                buttonRef.onClick?.AddListener(_onClick);
            }
        }

        private void OnDisable()
        {
            if (buttonRef)
            {
                buttonRef.onClick?.RemoveListener(_onClick);
            }
        }

        private void OnButtonClicked()
        {
            SetActiveTool();
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
            activeTool = true;
            if (buttonGroupRef)
            {
                buttonGroupRef.SetToolSelected(this);
            }
            if (audio)
            {
                audio.PlayClickSFX();
            }
        }

        public void SetInactiveTool()
        {
            this.activeTool = false;
            this.transform.position = startPosition;
        }
    }
}
