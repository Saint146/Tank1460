using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Tank1460.Common.Extensions;
using Tank1460.Globals;
using Tank1460.LevelObjects.Tanks;

namespace Tank1460.Managers
{
    /// <summary>
    /// Manages player-related operations including spawners, scoring, and lives.
    /// Extracted from the god object Level class to handle player logic separately.
    /// </summary>
    internal class LevelPlayerManager
    {
        private readonly Dictionary<PlayerIndex, PlayerSpawner> _playerSpawners;
        private readonly Dictionary<PlayerIndex, int> _playersScore;
        private readonly LevelEffects _levelEffects;
        private readonly Level _level;

        public LevelPlayerManager(LevelEffects levelEffects, Level level)
        {
            _playerSpawners = new Dictionary<PlayerIndex, PlayerSpawner>();
            _playersScore = new Dictionary<PlayerIndex, int>();
            _levelEffects = levelEffects;
            _level = level;
        }

        public void AddPlayerSpawner(PlayerIndex playerIndex, PlayerSpawner spawner)
        {
            _playerSpawners[playerIndex] = spawner;
        }

        public PlayerSpawner GetPlayerSpawner(PlayerIndex playerIndex)
        {
            return _playerSpawners.TryGetValue(playerIndex, out var spawner) ? spawner : null;
        }

        public IEnumerable<PlayerSpawner> GetAllPlayerSpawners() => _playerSpawners.Values;

        public int GetPlayerLivesRemaining(PlayerIndex playerIndex)
        {
            return _playerSpawners[playerIndex].LivesRemaining;
        }

        public PlayerTank GetPlayerTank(PlayerIndex playerIndex)
        {
            return _playerSpawners[playerIndex]?.Tank;
        }

        public int GetPlayerScore(PlayerIndex playerIndex)
        {
            return _playersScore[playerIndex];
        }

        public void SetPlayerScore(PlayerIndex playerIndex, int score)
        {
            _playersScore[playerIndex] = score;
        }

        public void HandlePlayerTankDestroyed(PlayerTank playerTank)
        {
            var playerSpawner = _playerSpawners[playerTank.PlayerIndex];
            if (playerSpawner is null)
            {
                Debug.Fail($"Cannot find player spawner for player {playerTank.PlayerIndex}.");
                return;
            }

            playerSpawner.HandleTankDestroyed(playerTank);
        }

        public void TrySnatchLife(PlayerIndex playerIndex)
        {
            if (!_playerSpawners.Values.TryGetFirst(out var spawnerDonator, spawner => spawner.CanDonateLife()))
                return;

            spawnerDonator.DonateLife(_playerSpawners[playerIndex]);
            _levelEffects.RemoveAll<GameOverLevelEffect>(effect => effect.PlayerIndex == playerIndex);
        }

        public void RewardPlayerWithPoints(PlayerIndex playerIndex, int points)
        {
            Debug.Assert(points > 0);

            var oneUpsGained = GameRules.GetOneUpsGained(_playersScore[playerIndex], points);
            _playersScore[playerIndex] += points;

            if (oneUpsGained > 0)
                _playerSpawners[playerIndex].AddOneUps(oneUpsGained);
        }

        public void AddPlayerStatsForDefeatingTank(PlayerIndex playerIndex, Tank tank)
        {
            var tankType = tank.Type;

            var playerBotStats = _level.Stats.PlayerStats[playerIndex].BotsDefeated;
            if (playerBotStats.TryGetValue(tankType, out var value))
                playerBotStats[tankType] = value + 1;
            else
                playerBotStats[tankType] = 1;

            RewardPlayerWithPoints(playerIndex, GameRules.TankScoreByType[tankType]);
        }

        public void HandlePlayerLostAllLives(PlayerIndex playerIndex)
        {
            if (_playerSpawners.Values.All(spawner => spawner.ControlledByAi || spawner.LivesRemaining == 0))
            {
                _level.HandleAllPlayersLostLives();
                return;
            }

            _levelEffects.Add(new GameOverLevelEffect(_level, playerIndex));
        }

        public bool ContainsPlayerSpawner(PlayerIndex playerIndex)
        {
            return _playerSpawners.ContainsKey(playerIndex);
        }

        public void Clear()
        {
            _playerSpawners.Clear();
            _playersScore.Clear();
        }
    }
}