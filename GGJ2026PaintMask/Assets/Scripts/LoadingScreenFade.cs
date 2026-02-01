using UnityEngine;
using UnityEngine.UI;

public class LoadingScreenFade : MonoBehaviour
{
    private Image img;
    public float initialWaitTime = 0.5f;
    public float fadeTime = 1f;
    private enum FadeState {TRANSPARENT, OPAQUE, WAIT_TO_FADE_IN, FADING_IN, WAIT_TO_FADE_OUT, FADING_OUT}
    private FadeState fadeState;
    private float startTime;
    private float startAlpha;
    private float endAlpha;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetupScreen();
    }

    void SetupScreen()
    {
        img = this.GetComponent<Image>();
        img.enabled = true;
        img.color = Color.black;
        fadeState = FadeState.WAIT_TO_FADE_OUT;
        Invoke("StartFadeOut", initialWaitTime);
    }

    // Update is called once per frame
    void Update()
    {
        if(fadeState == FadeState.FADING_IN || fadeState == FadeState.FADING_OUT)
        {
            Fade();
        }
    }

    public void StartFadeOut()
    {
        img.enabled = true;
        fadeState = FadeState.FADING_OUT;
        startTime = Time.time;
        startAlpha = 1f;
        endAlpha = 0f;
    }

    public void StartFadeIn()
    {
        img.enabled = true;
        fadeState = FadeState.FADING_IN;
        startTime = Time.time;
        startAlpha = 0f;
        endAlpha = 1f;
    }

    void FinishFadeIn()
    {
        fadeState = FadeState.OPAQUE;
    }

    void FinishFadeOut()
    {
        img.enabled = false;
        fadeState = FadeState.TRANSPARENT;
    }

    

    void Fade()
    {
        float u = (Time.time - startTime) / fadeTime;
        

        

        float alpha = Mathf.Lerp(startAlpha, endAlpha, u);
        img.color = new Color(0f,0f,0f, alpha);

        if (u >= 1)
        {
            if (fadeState == FadeState.FADING_OUT)
            {
                FinishFadeOut();
            }
            else if (fadeState == FadeState.FADING_IN)
            {
                FinishFadeIn();
            }
        }
    }
}
