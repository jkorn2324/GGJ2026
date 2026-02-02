using UnityEngine;
using UnityEngine.SceneManagement;


public class MainMenu : MonoBehaviour
{
    public float waitTime = 1.5f;
    public LoadingScreenFade fadeScreen;
    public AudioManager audio;


    public void PrepareToStartGame()
    {
        Debug.Log("preparing to load game scene");
        fadeScreen.StartFadeIn();
        audio.PlayClickSFX();
        Invoke(nameof(LoadGameScene), waitTime);
    }

    public void PrepareToExitGame()
    {
        Debug.Log("preparing to exit game");
        fadeScreen.StartFadeIn();
        audio.PlayClickSFX();
        Invoke("ExitGame", waitTime);
    }

    void LoadGameScene()
    {
        Debug.Log("loading game scene");
        SceneManager.LoadScene("GameScene");
    }

    void ExitGame()
    {
        Debug.Log("QUIT");
        Application.Quit();
    }

    
}
