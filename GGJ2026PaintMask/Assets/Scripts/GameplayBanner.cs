using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameplayBanner : MonoBehaviour
{

    public Image graphic;
    public TMP_Text textLeft;
    public TMP_Text textRight;
    public float moveSpeed = 5f;
    private bool active;
    private Vector3 startPosition;

    void Start()
    {
        startPosition = this.transform.position;
    }

    void Update()
    {
        if (active)
        {
            MoveBanner();
        }
    }

    void MoveBanner()
    {
        float moveAmount = moveSpeed * Time.deltaTime;
        this.transform.position = new Vector3(this.transform.position.x + moveAmount, this.transform.position.y, this.transform.position.z);
    }


    public void PullUpBanner(string left, string right)
    {
        active = true;
        graphic.enabled = true;
        textLeft.enabled = true;
        textRight.enabled = true;
        textLeft.text = left;
        textRight.text = right;
    }

    public void BringDownBanner()
    {
        active = false;
        graphic.enabled = false;
        textLeft.enabled = false;
        textRight.enabled = false;
        this.transform.position = startPosition;
    }
}
