using System;
using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GGJ2026.Painting
{
    public class GameFlow : MonoBehaviour
    {
        [System.Serializable]
        private struct TimerRef
        {
            [SerializeField]
            private GameObject gameObjectRef;
            [SerializeField]
            private TMP_Text textRef;

            public void SetVisible(bool isVisible)
            {
                if (gameObjectRef)
                {
                    gameObjectRef.SetActive(isVisible);
                }
            }

            public void SetText(string text)
            {
                if (textRef)
                {
                    textRef.text = text;
                }
            }
        }

        [System.Serializable]
        private struct ArtistsRefPainting
        {
            [SerializeField, Tooltip("The group reference.")]
            private GameObject groupRef;
            [SerializeField, Tooltip("The canvs reference.")]
            private ArtistCanvasComponent canvasRef;

            public void SetVisible(bool isVisible)
            {
                if (groupRef)
                {
                    groupRef.SetActive(isVisible);
                }
            }

            public void SetPainting(ArtistPainting painting)
            {
                if (canvasRef)
                {
                    canvasRef.SetPainting(painting);
                }
            }
        }
        
        [Header("Painting Reference")]
        [SerializeField, Tooltip("The painting width.")]
        private int paintingWidth;
        [SerializeField, Tooltip("The painting height")]
        private int paintingHeight = 600;
        [Space]
        [SerializeField, Tooltip("The artist canvas component.")]
        private ArtistCanvasComponent canvasComponent;
        [Space]
        public Painter painter;
        public ToolSelectButtonGroup toolSelectButtons;
        [SerializeField, Tooltip("The scoring settings.")]
        private ArtistPaintingScoring.ScoringSettings scoringSettingsRef;
        [Space]
        public GameplayBanner banner;
        [Space] 
        [SerializeField, Tooltip("The reveal results view.")]
        private RevealResultsView revealResultsView;
        [SerializeField, Tooltip("The forger artist painting reference.")]
        private ArtistsRefPainting forgerArtistPaintingRef;
        [Space] 
        [SerializeField, Tooltip("The button reference.")]
        private Button exitButton;

        [SerializeField, Tooltip("The timer reference.")]
        private TimerRef timerRef;
        public AudioManager audio;

        //timer for countdown to round end
        public float timeRemaining;

        //time at the start of the round, before round gameplay starts
        public float roundPrepTime = 3f;

        //text to display in banner
        public string roundStartText;
        public string gameEndTextLeft;
        public string gameEndTextRight;

        //is the game during a round?
        public bool roundActive;

        private Round _round;
        private Round.Listener _roundListener;

        private UnityAction _exitPressed;

        /// <summary>
        /// The round.
        /// </summary>
        public Round Round => _round;

        private void Awake()
        {
            _round = Round.New(
                new Round.InitParams(
                    paintingSize: new Vector2(paintingWidth, paintingHeight),
                    totalArtistPaintingTimeSecs: 120.0f, 
                    forgerPaintingTimeSecs: 180.0f, forgersCount: 1,
                    inScoringSettings: scoringSettingsRef));
            _roundListener.OnRoundFinished = OnRoundFinished;
            _exitPressed = OnExitButtonPressed;
        }

        private void OnEnable()
        {
            _roundListener.Initialize(_round);
            if (exitButton)
            {
                exitButton.onClick?.AddListener(_exitPressed);
            }
        }

        private void OnDisable()
        {
            _roundListener.DeInitialize(_round);
            if (exitButton)
            {
                exitButton.onClick?.RemoveListener(_exitPressed);
            }
        }

        private void OnDestroy()
        {
            Round.Release(ref _round);
        }

        #region GAME_START
        
        private void Start()
        {
            PrepareRound();
        }

        #endregion
        
        #region exit_game
        
        private void OnExitButtonPressed()
        {
            // Loadss the main menu scene asynchronously.
            SceneManager.LoadSceneAsync("MainMenu");
        }
        
        #endregion

        #region ROUND_START

        private void PrepareRound()
        {
            if (_round == null)
            {
                return;
            }
            if (!_round.IsPlaying)
            {
                _round.StartRound();
            }
            else
            {
                _round.SetCurrentPlayer(_round.CurrentPlayerIndex + 1);
            }

            // Updates the painting so that the forgers can reference it.
            var currentPlayer = _round.CurrentPlayer;
            var shouldRevealArtistRef = currentPlayer != null && currentPlayer.PlayerType == Player.Type.Forger;
            forgerArtistPaintingRef.SetPainting(shouldRevealArtistRef ? _round.ArtistPainting : null);
            forgerArtistPaintingRef.SetVisible(shouldRevealArtistRef);
            
            if (canvasComponent)
            {
                canvasComponent.SetPainting(_round.CurrentPainting);
            }
            var player = _round.CurrentPlayer;
            Debug.Log($"preparing round with the player: {player?.PlayerName}");

            //bring up banner to announce round start
            banner.PullUpBanner(roundStartText, player?.PlayerName);


            //StartRoundGameplay();
            StartCoroutine(WaitToStartRoundGameplay());
        }

        IEnumerator WaitToStartRoundGameplay()
        {
            yield return new WaitForSeconds(roundPrepTime);
            StartRoundGameplay();
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

            //bring down banner
            banner.BringDownBanner();

            var player = _round?.CurrentPlayer;
            Debug.Log("ROUND START! GO " + player?.PlayerName);
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
            timerRef.SetVisible(true);
            var currentPlayerTimeLimit = _round?.CurrentPlayerTimeLimit ?? 0.0f;
            timeRemaining = currentPlayerTimeLimit;
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
            timerRef.SetText(timerInteger.ToString());
        }

        #endregion

        #region ROUND_END

        void PrepareEndRound()
        {
            Debug.Log("round over!");
            audio.PlayTimerSFX();
            roundActive = false;
            DisableToolButtons();
            DisablePaintInput();
            DisableTimer();

            //mayube add time for animations and feedback before ending the round
            EndRound();
        }

        void EndRound()
        {
            if (_round == null)
            {
                return;
            }
            var idx = _round.CurrentPlayerIndex + 1;
            if (idx < _round.TotalPlayers)
            {
                //prepare the round with the next player team
                PrepareRound();
            }
            else
            {
                //start ending the game
                PrepareEndgame();
            }
        }

        void DisableTimer()
        {
            timerRef.SetVisible(false);
        }

        void DisableToolButtons()
        {
            Debug.Log("tool buttons are disabled");
            toolSelectButtons.SetToolSelected(null);
            painter.ResetCurrentTool();
            toolSelectButtons.gameObject.SetActive(false);
        }

        void DisablePaintInput()
        {
            Debug.Log("input is disabled");
            painter.SetPaintInputMode(Painter.PaintInputMode.DISABLED);
        }

        #endregion

        #region GAME_END
        
        private void OnRoundFinished(Round round)
        {
            Debug.Log("Round has finished.");
            // Gets called when the round has finished calculating the result.
            StartCoroutine(RevealResults(0.5f, round));
        }

        private IEnumerator RevealResults(float waitTime, Round round)
        {
            yield return new WaitForSeconds(waitTime);
            if (revealResultsView && round != null)
            {
                var result = round.GameResult;
                if (result != null)
                {
                    var castedResult = (Round.Result)result;
                    var leftView = new RevealResultsView.PaintingReference()
                    {
                        DidWin = !castedResult.DidForgersWin,
                        Painting = round.ArtistPainting
                    };
                    var rightView = new RevealResultsView.PaintingReference()
                    {
                        DidWin = castedResult.DidForgersWin,
                        Painting = round.ForgerPainting
                    };
                    revealResultsView.SetPaintings(leftView, rightView);
                    revealResultsView.SetVisible(true, animate: true);
                }
            }
        }

        private void PrepareEndgame()
        {
            if (!_round.IsPlaying)
            {
                return;
            }
            _round.SetCurrentPlayer(_round.CurrentPlayerIndex + 1);
            //bring up banner to announce round start
            banner.PullUpBanner(gameEndTextLeft, gameEndTextRight);
        }

        #endregion
    }
}
