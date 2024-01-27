using System.Diagnostics;
using Microsoft.Xna.Framework;
using Tank1460.Audio;
using Tank1460.LevelObjects.Tanks;

namespace Tank1460;

public class PlayerSpawner
{
    public PlayerIndex PlayerIndex { get; }

    public PlayerTank Tank { get; private set; }

    private readonly Level _level;
    private readonly int _x;
    private readonly int _y;
    private const double RespawnInterval = 64 * Tank1460Game.OneFrameSpan;

    private double _timeToSpawnRemaining;
    private bool _spawnIsDue;

    public int LivesRemaining { get; private set; } = 3;

    private bool _enabled = true;

    public PlayerSpawner(Level level, int x, int y, PlayerIndex playerIndex)
    {
        PlayerIndex = playerIndex;
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

    public void HandleTankDestroyed(PlayerTank playerTank)
    {
        if (Tank != playerTank)
        {
            Debug.Fail($"Player spawner {PlayerIndex} cannot handle a death of player tank with a different number {playerTank.PlayerIndex}");
            return;
        }

        Tank = null;

        LivesRemaining--;
        if (LivesRemaining == 0)
            _level.HandlePlayerLostAllLives(PlayerIndex);

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

    public bool CanDonateLife() => LivesRemaining > 1;

    public void DonateLife(PlayerSpawner receiverSpawner)
    {
        LivesRemaining--;
        receiverSpawner.LivesRemaining++;
    }

    public void Update(GameTime gameTime)
    {
        if (!_enabled)
            return;

        if (Tank is not null)
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
        if (Tank is not null)
            return;

        var bounds = Level.GetTileBounds(_x, _y);
        Tank = new PlayerTank(_level, PlayerIndex);
        Tank.Spawn(new Point(bounds.Left, bounds.Top));
    }
}