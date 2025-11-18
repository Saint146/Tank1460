using Microsoft.Xna.Framework;
using System;
using Tank1460.Audio;
using Tank1460.Globals;

namespace Tank1460.Managers
{
    /// <summary>
    /// Manages the state transitions and timing logic of a level.
    /// Extracted from the god object Level class to handle status management separately.
    /// </summary>
    internal class LevelStateManager
    {
        public Action _onLevelComplete;
        public Action _onGameOver;

        private LevelStatus _status;
        private LevelStatus _statusBeforePause;
        private double _delayTime;
        private double _delayEffectTime;

        private readonly Level _level;
        private readonly ISoundPlayer _soundPlayer;
        private readonly LevelEffects _levelEffects;

        public LevelStatus Status
        {
            get => _status;
            set => _status = value;
        }

        public LevelStateManager(Level level, ISoundPlayer soundPlayer, LevelEffects levelEffects, Action onLevelComplete, Action onGameOver)
        {
            _level = level;
            _soundPlayer = soundPlayer;
            _levelEffects = levelEffects;
            _onLevelComplete = onLevelComplete;  // Callback instead of direct event
            _onGameOver = onGameOver;             // Callback instead of direct event
            _status = LevelStatus.Intro;
        }
        public bool ProcessStatus(GameTime gameTime)
        {
            switch (_status)
            {
                case LevelStatus.Loading:
                case LevelStatus.Intro:
                case LevelStatus.Paused:
                case LevelStatus.GameOver:
                case LevelStatus.Win:
                    return false;

                case LevelStatus.Running:
                    return true;

                case LevelStatus.WinDelay:
                    _delayTime += gameTime.ElapsedGameTime.TotalSeconds;
                    if (_delayTime <= _delayEffectTime)
                        return true;

                    _status = LevelStatus.Win;
                    _soundPlayer.StopAll();
                    _onLevelComplete?.Invoke();
                    return false;

                case LevelStatus.LostPreDelay:
                    _delayTime += gameTime.ElapsedGameTime.TotalSeconds;
                    if (_delayTime <= _delayEffectTime)
                        return true;

                    _levelEffects.AddExclusive(new GameOverLevelEffect(_level));
                    _soundPlayer.MuteAllWithLessPriorityThan(Sound.ExplosionBig);
                    _status = LevelStatus.LostDelay;
                    _delayTime = 0.0;
                    _delayEffectTime = GameRules.TimeInFrames(252);
                    return true;

                case LevelStatus.LostDelay:
                    _delayTime += gameTime.ElapsedGameTime.TotalSeconds;
                    if (_delayTime <= _delayEffectTime)
                        return true;

                    _status = LevelStatus.GameOver;
                    _levelEffects.RemoveAll();
                    _soundPlayer.StopAll();
                    _soundPlayer.Unmute();
                    _onGameOver?.Invoke();
                    return false;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void TogglePause()
        {
            switch (_status)
            {
                case LevelStatus.Loading:
                case LevelStatus.WinDelay:
                case LevelStatus.LostDelay:
                    return;

                case LevelStatus.Intro:
                case LevelStatus.LostPreDelay:
                case LevelStatus.Running:
                    _statusBeforePause = _status;
                    _status = LevelStatus.Paused;
                    _soundPlayer.PauseAndPushState();
                    _soundPlayer.Play(Sound.Pause);
                    _levelEffects.AddExclusive(new PauseLevelEffect(_level));
                    break;

                case LevelStatus.Paused:
                    _levelEffects.RemoveAll<PauseLevelEffect>();
                    _status = _statusBeforePause;
                    _soundPlayer.ResumeAndPopState();
                    break;

                case LevelStatus.Win:
                case LevelStatus.GameOver:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void StartGameOverSequence()
        {
            if (_status is LevelStatus.LostPreDelay or LevelStatus.LostDelay)
                return;

            _status = LevelStatus.LostPreDelay;
            _delayEffectTime = GameRules.TimeInFrames(36);
        }

        public void HandleAllBotsDestroyed()
        {
            if (_status is LevelStatus.LostPreDelay or LevelStatus.LostDelay)
                return;

            _status = LevelStatus.WinDelay;
            _delayTime = 0.0;
            _delayEffectTime = GameRules.TimeInFrames(144);
        }

        public void Start()
        {
            System.Diagnostics.Debug.Assert(_status == LevelStatus.Intro);
            _status = LevelStatus.Running;
        }
    }
}