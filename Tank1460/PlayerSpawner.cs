using System.Diagnostics;
using Microsoft.Xna.Framework;
using Tank1460.Audio;
using Tank1460.LevelObjects.Tanks;

namespace Tank1460;

public class PlayerSpawner
{
    public int PlayerNumber { get; }
    private readonly Level _level;
    private readonly int _x;
    private readonly int _y;
    private const double RespawnInterval = 64 * Tank1460Game.OneFrameSpan;

    private double _timeToSpawnRemaining;
    private bool _spawnIsDue;

    public int LivesRemaining { get; private set; } = 3;
    private PlayerTank _playerTank;
    private bool _enabled = true;

    public PlayerSpawner(Level level, int x, int y, int playerNumber)
    {
        PlayerNumber = playerNumber;
        _level = level;
        _x = x;
        _y = y;
        SpawnIsReady();
    }

    private void StartSpawnTimer()
    {
        _timeToSpawnRemaining = RespawnInterval;
        _spawnIsDue = false;
    }

    private void SpawnIsReady()
    {
        _spawnIsDue = true;
    }

    public void HandlePlayerDeath(PlayerTank playerTank)
    {
        if (_playerTank != playerTank)
        {
            Debug.Fail($"Player spawner #{PlayerNumber} cannot handle a death of player tank with a different number #{playerTank.PlayerNumber}");
            return;
        }

        _playerTank = null;
        LivesRemaining--;

        StartSpawnTimer();
    }

    public void Disable()
    {
        _enabled = false;
    }

    public void AddOneUp()
    {
        _level.SoundPlayer.Play(Sound.OneUp);
        LivesRemaining++;
    }

    public void Update(GameTime gameTime)
    {
        if (!_enabled)
            return;

        if (_playerTank is not null)
            return;

        if (!_spawnIsDue)
        {
            _timeToSpawnRemaining -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_timeToSpawnRemaining > 0.0)
                return;

            _spawnIsDue = true;
        }

        if (LivesRemaining > 0)
            SpawnPlayer();
    }

    private void SpawnPlayer()
    {
        if (_playerTank is not null)
            return;

        var bounds = Level.GetTileBounds(_x, _y);
        _playerTank = new PlayerTank(_level, PlayerNumber);
        _playerTank.Spawn(new Point(bounds.Left, bounds.Top));
        _level.AddPlayer(_playerTank);
    }
}