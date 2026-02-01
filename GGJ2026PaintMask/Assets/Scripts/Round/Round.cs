using System.Collections.Generic;
using UnityEngine.Pool;

namespace GGJ2026.Painting
{
    /// <summary>
    /// The round.
    /// </summary>
    public class Round
    {
        #region initialization
        
        private static readonly ObjectPool<Round> Pool = new ObjectPool<Round>(() => new Round());

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
        
        private List<Player> _currentPlayers;

        private void Initialize()
        {
            _currentPlayers = ListPool<Player>.Get();
        }

        private void DeInitialize()
        {
            if (_currentPlayers != null)
            {
                ListPool<Player>.Release(_currentPlayers);
            }
        }
    }
}
