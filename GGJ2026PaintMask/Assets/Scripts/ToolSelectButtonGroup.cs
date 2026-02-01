using UnityEngine;

public class ToolSelectButtonGroup : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void DeactivateOtherToolButtons(Transform activeTool)
    {
        foreach(Transform toolButton in transform)
        {
            if(toolButton != activeTool)
            {
                toolButton.GetComponent<ToolSelectButton>().SetInactiveTool();
            }
        }
    }
}
