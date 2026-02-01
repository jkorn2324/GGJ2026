using System.Collections.Generic;
using UnityEngine;

namespace GGJ2026.Painting
{
    public class ToolSelectButtonGroup : MonoBehaviour
    {
        [SerializeField, Tooltip("The painter reference.")]
        private Painter painterRef;
        [SerializeField, Tooltip("The tool buttons.")]
        private List<ToolSelectButton> toolButtons;
        
        public void SetToolSelected(ToolSelectButton activeTool)
        {
            if (painterRef)
            {
                painterRef.SelectTool(activeTool);
            }
            if (toolButtons == null)
            {
                return;
            }
            for (var index = 0; index < toolButtons.Count; index++)
            {
                var tool = toolButtons[index];
                if (!tool)
                {
                    continue;
                }
                if (activeTool != tool)
                {
                    tool.SetInactiveTool();
                }
            }
        }
    }
}

