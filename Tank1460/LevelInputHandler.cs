using Microsoft.Xna.Framework.Input;
using Tank1460.Input;
using System.Linq;
using Microsoft.Xna.Framework;
using Tank1460.Common.Extensions;

namespace Tank1460.Managers
{
    /// <summary>
    /// Handles player input processing for the level.
    /// Extracted from the god object Level class to manage input separately.
    /// </summary>
    internal class LevelInputHandler
    {
        private readonly Level _level;
        private readonly LevelStateManager _stateManager;
        private readonly LevelPlayerManager _playerManager;
#if DEBUG
        private bool _cheatGodMode;
#endif

        public LevelInputHandler(Level level, LevelStateManager stateManager, LevelPlayerManager playerManager)
        {
            _level = level;
            _stateManager = stateManager;
            _playerManager = playerManager;
        }

        public void HandleInput(PlayerInputCollection playersInputs)
        {
            switch (_stateManager.Status)
            {
                case LevelStatus.Loading:
                case LevelStatus.Intro:
                case LevelStatus.LostDelay:
                case LevelStatus.Win:
                case LevelStatus.GameOver:
                    playersInputs.ClearInputs();
                    break;

                case LevelStatus.Running:
                case LevelStatus.Paused:
                case LevelStatus.WinDelay:
                case LevelStatus.LostPreDelay:
                    break;

                default:
                    throw new System.ArgumentOutOfRangeException();
            }

            foreach (var playerIndex in _level.PlayersInGame)
            {
                var playerInput = playersInputs[playerIndex];

                if (playerInput.Pressed.HasFlag(PlayerInputCommands.Start))
                    _stateManager.TogglePause();

                var tank = _playerManager.GetPlayerTank(playerIndex);
                if (tank is null)
                {
                    if (_playerManager.GetPlayerLivesRemaining(playerIndex) != 0)
                        continue;

                    if (playerInput.Active.HasFlag(PlayerInputCommands.ShootTurbo) || playerInput.Pressed.HasFlag(PlayerInputCommands.Shoot))
                        _playerManager.TrySnatchLife(playerIndex);
                }
                else
                {
                    tank.HandleInput(playerInput);
                }
            }

#if DEBUG
            HandleDebugInput();
#endif
        }

#if DEBUG
        private void HandleDebugInput()
        {
            if (KeyboardEx.HasBeenPressed(Keys.F10))
            {
                _cheatGodMode = !_cheatGodMode;
                _level.GetAllPlayerTanks().ForEach(tank => tank.GodMode = _cheatGodMode);
            }

            if (KeyboardEx.HasBeenPressed(Keys.PageUp))
                _level.GetAllPlayerTanks().ForEach(tank => tank.UpgradeUp());

            if (KeyboardEx.HasBeenPressed(Keys.PageDown))
                _level.GetAllPlayerTanks().ForEach(tank => tank.UpgradeDown());

            if (KeyboardEx.IsPressed(Keys.LeftAlt))
            {
                foreach (var digit in System.Linq.Enumerable.Range(1, 4))
                {
                    var key = Keys.NumPad0 + digit;
                    if (!KeyboardEx.HasBeenPressed(key))
                        continue;

                    var player = (PlayerIndex)(digit - 1);
                    if (!_level.PlayersInGame.Contains(player))
                        continue;

                    _level.BotManager.ExplodeAll(_playerManager.GetPlayerTank(player));

                    break;
                }
            }
        }
#endif
    }
}