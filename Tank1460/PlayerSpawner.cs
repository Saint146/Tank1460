using System.Diagnostics;
using Microsoft.Xna.Framework;
using Tank1460.Audio;
using Tank1460.Common.Extensions;
using Tank1460.Common.Level.Object.Tank;
using Tank1460.LevelObjects.Tanks;
using Tank1460.LevelObjects.Tiles;

namespace Tank1460;

public class PlayerSpawner
{
    public PlayerIndex PlayerIndex { get; }

    public PlayerTank Tank { get; private set; }

    public int LivesRemaining { get; set; } = 3;

    public Rectangle TileBounds { get; }

    public Rectangle Bounds { get; }

    public bool ControlledByAi { get; set; }

    public bool HasInfiniteLives { get; set; }

    private readonly Level _level;
    private const double RespawnInterval = 0.0;

    private double _timeToSpawnRemaining;
    private bool _spawnIsDue;

    // Задаёт тип для первого респауна на случай перехода между уровнями.
    private TankType? _nextSpawnType;
    private bool _nextSpawnHasShip;

    public PlayerSpawner(Level level, Rectangle tileBounds, PlayerIndex playerIndex)
    {
        PlayerIndex = playerIndex;
        _level = level;

        TileBounds = tileBounds;
        Bounds = TileBounds.Multiply(Tile.DefaultSize);

        SpawnIsReady();
    }

    public void HandleTankDestroyed(PlayerTank playerTank)
    {
        if (Tank != playerTank)
        {
            Debug.Fail($"Player spawner {PlayerIndex} cannot handle a death of player tank with a different number {playerTank.PlayerIndex}");
            return;
        }

        Tank = null;

        switch (LivesRemaining)
        {
            case 1 when HasInfiniteLives:
                break;

            case 1:
                LivesRemaining = 0;
                _level.HandlePlayerLostAllLives(PlayerIndex);
                break;

            default:
                LivesRemaining--;
                break;
        }

        StartSpawnTimer();
    }

    public void AddOneUps(int count = 1)
    {
        Debug.Assert(count > 0);

        _level.SoundPlayer.Play(Sound.OneUp);
        LivesRemaining += count;
    }

    public bool CanDonateLife() => !HasInfiniteLives && LivesRemaining > 1;

    public void DonateLife(PlayerSpawner receiverSpawner)
    {
        LivesRemaining--;
        receiverSpawner.LivesRemaining++;
    }

    public void SetNextSpawnSettings(TankType? type, bool hasShip)
    {
        _nextSpawnType = type;
        _nextSpawnHasShip = hasShip;
    }

    public void Update(GameTime gameTime)
    {
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

    private void StartSpawnTimer()
    {
        _timeToSpawnRemaining = RespawnInterval;
        _spawnIsDue = false;
    }

    private void SpawnIsReady()
    {
        _spawnIsDue = true;
    }

    private void SpawnPlayer()
    {
        if (Tank is not null)
            return;

        var type = _nextSpawnType ?? (_level.ClassicRules ? TankType.P0 : TankType.P1);
        _nextSpawnType = null;

        Tank = new PlayerTank(_level, PlayerIndex, type, ControlledByAi);
        Tank.Spawn(Bounds.Location);

        if (!_nextSpawnHasShip)
            return;

        Tank.AddShip();
        _nextSpawnHasShip = false;
    }
}