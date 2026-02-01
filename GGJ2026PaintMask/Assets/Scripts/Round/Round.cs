using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace GGJ2026.Painting
{
    /// <summary>
    /// The round.
    /// </summary>
    public class Round
    {
        #region structures

        public struct InitParams
        {
            public readonly float TotalArtistPaintingTimeSecs;
            public readonly float ForgerPaintingTimeSecs;
            public readonly int ForgersCount;
            public readonly Vector2 PaintingSize;
            
            public InitParams(Vector2 paintingSize, float totalArtistPaintingTimeSecs, float forgerPaintingTimeSecs, int forgersCount)
            {
                TotalArtistPaintingTimeSecs = totalArtistPaintingTimeSecs;
                ForgerPaintingTimeSecs = forgerPaintingTimeSecs;
                ForgersCount = forgersCount;
                PaintingSize = paintingSize;
            }
            
            public static readonly InitParams Default = new InitParams(
                new Vector2(Screen.width, Screen.height), 60.0f, 60.0f, 1);
        }
        
        #endregion
        #region pool
        
        private static readonly ObjectPool<Round> Pool = new ObjectPool<Round>(() => new Round());

        public static Round New(InitParams initParams)
        {
            var round = Pool.Get();
            round.Initialize(initParams);
            return round;
        }

        public static void Release(ref Round round)
        {
            if (round != null)
            {
                round.DeInitialize();
                Pool.Release(round);
            }
            round = null;
        }
        
        #endregion

        private ArtistPainting _artistPainting, _forgerPainting;
        private List<Player> _currentPlayers;
        
        /// <summary>
        /// The current player painting.
        /// </summary>
        public int CurrentPlayerIndex { get; private set; } = -1;

        public bool IsInitialized { get; private set; } = false;

        public bool IsPlaying { get; private set; } = false;
        
        /// <summary>
        /// The artist painting.
        /// </summary>
        public ArtistPainting ArtistPainting => _artistPainting;
        
        /// <summary>
        /// The forger painting.
        /// </summary>
        public ArtistPainting ForgerPainting => _forgerPainting;

        /// <summary>
        /// The current player.
        /// </summary>
        public Player CurrentPlayer => GetPlayer(CurrentPlayerIndex);
        
        public ArtistPainting CurrentPainting => GetPainting(CurrentPlayer?.PlayerType ?? Player.Type.Unknown);
        
        public float CurrentPlayerTimeLimit => GetPlayerTimeLimit(CurrentPlayer?.PlayerType ?? Player.Type.Unknown);

        public int TotalPlayers => _currentPlayers?.Count ?? 0;

        public InitParams Params { get; private set; } = default;
        
        private void Initialize(InitParams initParams)
        {
            if (IsInitialized)
            {
                return;
            }
            _currentPlayers = ListPool<Player>.Get();
            {
                // Creates a new artist.
                var newArtist = Player.New("Artist", Player.Type.Artist, this);
                _currentPlayers.Add(newArtist);
                for (var index = 0; index < initParams.ForgersCount; index++)
                {
                    var newPlayer = Player.New($"Forger_{index + 1}", 
                        Player.Type.Forger, this);
                    _currentPlayers.Add(newPlayer);
                }
            }
            Params = initParams;
            _artistPainting = ArtistPainting.New(initParams.PaintingSize);
            _forgerPainting = ArtistPainting.New(initParams.PaintingSize);
            IsPlaying = false;
            IsInitialized = true;
        }

        private void DeInitialize()
        {
            if (!IsInitialized)
            {
                return;
            }
            if (_currentPlayers != null)
            {
                for (var index = 0; index < _currentPlayers.Count; index++)
                {
                    var player = _currentPlayers[index];
                    Player.Release(ref player);
                }
                ListPool<Player>.Release(_currentPlayers);
                _currentPlayers = null;
            }
            Params = default;
            ArtistPainting.Release(ref _artistPainting);
            ArtistPainting.Release(ref _forgerPainting);
            IsPlaying = IsInitialized = false;
        }

        /// <summary>
        /// Starts the round.
        /// </summary>
        /// <returns>The round.</returns>
        public bool StartRound()
        {
            if (!IsInitialized || IsPlaying)
            {
                return false;
            }
            CurrentPlayerIndex = 0;
            _artistPainting.Clear();
            _forgerPainting.Clear();
            IsPlaying = true;
            return true;
        }

        public bool SetCurrentPlayer(int newPlayerIndex)
        {
            if (!IsInitialized)
            {
                return false;
            }
            CurrentPlayerIndex = newPlayerIndex;
            // TODO: 
            return true;
        }

        /// <summary>
        /// Gets the player.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The player.</returns>
        public Player GetPlayer(int index)
        {
            return (_currentPlayers != null && index >= 0 && index < _currentPlayers.Count) 
                ? _currentPlayers[index] : null;
        }

        public float GetPlayerTimeLimit(Player.Type playerType)
        {
            switch (playerType)
            {
                case Player.Type.Artist: return Params.TotalArtistPaintingTimeSecs;
                case Player.Type.Forger: return Params.ForgerPaintingTimeSecs;
            }
            return 0.0f;
        }

        /// <summary>
        /// Get the painting.
        /// </summary>
        /// <param name="playerType">The player type.</param>
        /// <returns>The artist painting.</returns>
        public ArtistPainting GetPainting(Player.Type playerType)
        {
            switch (playerType)
            {
                case Player.Type.Artist: return _artistPainting;
                case Player.Type.Forger: return _forgerPainting;
            }
            return null;
        }
    }
}
