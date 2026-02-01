using UnityEngine;
using TMPro;

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
        
        [SerializeField, Tooltip("The painting width.")]
        private int paintingWidth;
        [SerializeField, Tooltip("The painting height")]
        private int paintingHeight = 600;
        [Space]
        [SerializeField, Tooltip("The artist canvas component.")]
        private ArtistCanvasComponent canvasComponent;

        public Painter painter;
        public ToolSelectButtonGroup toolSelectButtons;

        [SerializeField, Tooltip("The timer reference.")]
        private TimerRef timerRef;
        public AudioManager audio;

        //timer for countdown to round end
        public float timeRemaining;

        //is the game during a round?
        public bool roundActive;

        private Round _round;

        /// <summary>
        /// The round.
        /// </summary>
        public Round Round => _round;

        private void Awake()
        {
            _round = Round.New(
                new Round.InitParams(
                    paintingSize: new Vector2(paintingWidth, paintingHeight),
                    totalArtistPaintingTimeSecs: 60.0f, 
                    forgerPaintingTimeSecs: 60.0f, forgersCount: 1));
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

            if (canvasComponent)
            {
                canvasComponent.SetPainting(_round.CurrentPainting);
            }
            var player = _round.CurrentPlayer;
            Debug.Log($"preparing round with the player: {player?.PlayerName}");
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
}
