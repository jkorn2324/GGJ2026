using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class GameFlow : MonoBehaviour
{
    //each team is a ScriptableObject of type PlayerTeam
    public List<PlayerTeam> playerTeams;

    //who the current team is
    public int currentPlayerTeam;
    private const int ARTIST = 0;
    private const int FORGER = 1;

    public Painter painter;
    public ToolSelectButtonGroup toolSelectButtons;
    public GameObject timer;


    //timer for countdown to round end
    public float timeRemaining;

    //is the game during a round?
    public bool roundActive;



    #region GAME_START
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //start round (Artist)
        PrepareRound(ARTIST);
    }
    #endregion

    

    #region ROUND_START

    void PrepareRound(int newPlayer)
    {
        Debug.Log("preparing round with the player: " + playerTeams[newPlayer].teamName);
        //set which team is playing
        SetPlayer(newPlayer);

        //mayber add animations or feedback before the round starts
        StartRoundGameplay();
    }

    void SetPlayer(int newPlayer)
    {
        currentPlayerTeam = newPlayer;
    }

    void StartRoundGameplay()
    {

        
        //start timer
        StartTimer();

        //enable tool buttons
        EnableToolButtons();

        //enable input for gaming
        EnablePaintInput();


        roundActive = true;
        Debug.Log("ROUND START! GO " + playerTeams[currentPlayerTeam].teamName);
    }

    void EnableToolButtons()
    {
        Debug.Log("tool buttons are enabled!");
        toolSelectButtons.gameObject.SetActive(true);
    }

    

    void EnablePaintInput()
    {
        Debug.Log("input enabled!");
        painter.SetPaintInputMode(Painter.PaintInputMode.OPEN);
    }

    

    void StartTimer()
    {
        timer.SetActive(true);
        timeRemaining = playerTeams[currentPlayerTeam].timeLimit;
        Debug.Log("clock has been started! This many seconds on the clock: " + timeRemaining);
    }
    #endregion

    #region DURING_ROUND
    // Update is called once per frame
    void Update()
    {
        if (roundActive)
        {
            CheckTimeRemaining();
        }
    }

    void CheckTimeRemaining()
    {
        timeRemaining -= Time.deltaTime;
        //Debug.Log("time remaining: " + timeRemaining);

        if (timeRemaining <= 0)
        {
            PrepareEndRound();
        }
        else
        {
            SetTimerText();
        }
    }

    void SetTimerText()
    {
        int timerInteger = (int)timeRemaining;
        timer.transform.Find("TimerText").GetComponent<TMP_Text>().text = timerInteger.ToString();
    }
    #endregion

    #region ROUND_END

    void PrepareEndRound()
    {
        Debug.Log("round over!");
        roundActive = false;
        DisableToolButtons();
        DisablePaintInput();
        DisableTimer();

        //mayube add time for animations and feedback before ending the round
        EndRound();
    }

    void EndRound()
    {
        //if this isn't the last player team
        if (currentPlayerTeam < playerTeams.Count - 1)
        {
            //prepare the round with the next player team
            PrepareRound(currentPlayerTeam + 1);
        }
        else
        {
            //start ending the game
            PrepareEndgame();
        }
    }

    void DisableTimer()
    {
        timer.SetActive(false);
    }

    void DisableToolButtons()
    {
        toolSelectButtons.DeactivateOtherToolButtons(null);
        Debug.Log("tool buttons are disabled");
        toolSelectButtons.gameObject.SetActive(false);

    }

    void DisablePaintInput()
    {
        Debug.Log("input is disabled");
        painter.SetPaintInputMode(Painter.PaintInputMode.DISABLED);
    }
    #endregion

    #region GAME_END
    void PrepareEndgame()
    {
        Debug.Log("game over!");
    }

    #endregion
}
