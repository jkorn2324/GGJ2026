using UnityEngine.Pool;

namespace GGJ2026.Painting
{
    public class Player
    {
        #region defines
        
        public enum Type
        {
            Artist,
            Forger,
            Unknown
        }

        #endregion
        
        #region initialization
        
        private static readonly ObjectPool<Player> Pool = new ObjectPool<Player>(() => new Player());

        /// <summary>
        /// Use this to create a new player.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="type">The type.</param>
        /// <param name="round">The round.</param>
        /// <returns>The player.</returns>
        public static Player New(string name, Type type, Round round)
        {
            var newPlayer = Pool.Get();
            newPlayer.Initialize(name, type, round);
            return newPlayer;
        }

        /// <summary>
        /// Use this to destroy the player.
        /// </summary>
        /// <param name="player">The player.</param>
        public static void Release(ref Player player)
        {
            if (player != null)
            {
                player.DeInitialize();
                Pool.Release(player);
            }
            player = null;
        }
        
        #endregion

        public string PlayerName { get; private set; }

        public Type PlayerType { get; private set; } = Type.Unknown;

        /// <summary>
        /// The current round.
        /// </summary>
        public Round CurrentRound { get; private set; }

        public bool IsInitialized { get; private set; } = false;
        
        private Player() { }

        private void Initialize(string name, Type type, Round round)
        {
            if (IsInitialized)
            {
                return;
            }
            PlayerName = name;
            PlayerType = type;
            CurrentRound = round;
            IsInitialized = true;
        }

        private void DeInitialize()
        {
            if (!IsInitialized)
            {
                return;
            }
            PlayerName = null;
            PlayerType = Type.Unknown;
            CurrentRound = null;
            IsInitialized = false;
        }
    }
}