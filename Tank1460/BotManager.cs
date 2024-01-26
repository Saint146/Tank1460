using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Tank1460.Extensions;
using Tank1460.LevelObjects.Tanks;

namespace Tank1460;

public class BotManager
{
    public int SpawnsRemaining { get; private set; }
    public IReadOnlyList<BotTank> BotTanks => _botTanks;

    private readonly List<BotTank> _botTanks = new();

    private readonly Level _level;
    private readonly List<(int x, int y)> _points = new();
    private int _pointIndex = 0;
    private readonly double _respawnInterval;
    private double _timeToSpawnRemaining;
    private bool _spawnIsDue;

    private readonly int _totalSpawns;
    private readonly Queue<TankType> _tankTypes;
    private int _botsAlive = 0;
    private readonly int _maxAliveBots;
    private int _periodIndex = 0;
    private double _periodTime = 0.0;

    private bool _paralyzeIsActive = false;
    private double _paralyzeTime;
    private double _paralyzeEffectTime;

#if !DEBUG
    private readonly double _periodLength;
    private const double PeriodResetTime = 16384.0 * Tank1460Game.OneFrameSpan;
#else
    private double _periodLength;
    private double PeriodResetTime = 4320.0 * Tank1460Game.OneFrameSpan;

#endif

    public BotManager(Level level, int totalBots, int maxAliveBots)
    {
        _level = level;
        SpawnsRemaining = _totalSpawns = totalBots;
        _maxAliveBots = maxAliveBots;
        _tankTypes = ComposeTankTypeQueue(_level.Structure?.BotTypes);

        _respawnInterval = (190 - level.LevelNumber * 4 - (level.PlayerCount - 1) * 20) * Tank1460Game.OneFrameSpan;
        _periodLength = _respawnInterval * 8;
        SpawnIsReady();
    }
    public void AddSpawnPoint(int x, int y)
    {
        _points.Add((x, y));
    }

    public void AddOneUp()
    {
        SpawnsRemaining++;
    }

    public void Update(GameTime gameTime)
    {
#if DEBUG
        if (KeyboardEx.IsPressed(Keys.LeftControl))
        {
            foreach (var digit in Enumerable.Range(0, 10))
            {
                var key = Keys.D0 + digit;

                if (KeyboardEx.HasBeenPressed(key))
                {
                    PeriodResetTime = double.MaxValue;
                    _periodLength = 1000000.0;
                    _periodTime = _periodLength * digit;
                    break;
                }
            }
        }
#endif

        if (_paralyzeIsActive)
        {
            _paralyzeTime += gameTime.ElapsedGameTime.TotalSeconds;
            if (_paralyzeTime > _paralyzeEffectTime)
            {
                _paralyzeIsActive = false;
                _botTanks.ForEach(tank => tank.IsImmobile = tank.IsPacifist = false);
            }
        }

        _botTanks.FindAll(e => e.ToRemove).ForEach(HandleBotDestroyed);
        foreach (var bot in _botTanks)
            bot.Update(gameTime);

        var elapsedSeconds = gameTime.ElapsedGameTime.TotalSeconds;
        _periodTime += elapsedSeconds;
        if (_periodTime > PeriodResetTime)
            _periodTime = 0.0;
        SetPeriodIndex((int)(_periodTime / _periodLength));

        if (!_spawnIsDue)
        {
            _timeToSpawnRemaining -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_timeToSpawnRemaining > 0.0)
                return;

            SpawnIsReady();
        }

        TrySpawnBot();
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch) => _botTanks.ForEach(e => e.Draw(gameTime, spriteBatch));

    public void ExplodeAll(PlayerTank playerTank)
    {
        _botTanks.ForEach(botTank => botTank.Explode(playerTank));
    }

    public void AddParalyze(double effectTime)
    {
        _paralyzeIsActive = true;
        _paralyzeTime = 0.0;
        _paralyzeEffectTime = effectTime;
        _botTanks.ForEach(tank => tank.IsImmobile = tank.IsPacifist = true);
    }

    private void ResetSpawnTimer()
    {
        _timeToSpawnRemaining = _respawnInterval;
        _spawnIsDue = false;
    }

    private void SpawnIsReady()
    {
        _spawnIsDue = true;
    }

    private static Queue<TankType> ComposeTankTypeQueue(IReadOnlyList<(TankType, int)> structureBotTypes)
    {
        Queue<TankType> result = new();
        if (structureBotTypes is null)
            return result;

        foreach (var (type, count) in structureBotTypes)
            Enumerable.Range(1, count).ForEach(_ => result.Enqueue(type));

        return result;
    }

    private void SetPeriodIndex(int periodIndex)
    {
        if (_periodIndex != periodIndex)
        {
            _periodIndex = periodIndex;
            _botTanks.ForEach(e => e.PeriodIndex = periodIndex);
        }
    }

    private void TrySpawnBot()
    {
        if (SpawnsRemaining <= 0 || _botsAlive >= _maxAliveBots)
            return;

        Debug.Assert(_points.Count > 0);

        SpawnsRemaining--;

        var (x, y) = GetNextSpot();

        //if (_level.CanSpawnEnemyOnTile(x, y))
        //    return;

        var position = Level.GetTileBounds(x, y).Location;

        if (_tankTypes?.TryDequeue(out var type) is not true)
            type = GetRandomType();

        var botNumber = _totalSpawns - SpawnsRemaining;

        bool hasBonus;
        if (_level.Structure?.BotBonusNumbers is null)
            hasBonus = Rng.Next(4) == 0;
        else
            hasBonus = _level.Structure.BotBonusNumbers.Contains(botNumber);

        var bot = new BotTank(_level, type, type == TankType.Type7 ? 4 : 1, hasBonus ? 1 : 0, SpawnsRemaining, _periodIndex);
        if (_paralyzeIsActive)
            bot.IsImmobile = bot.IsPacifist = true;

        bot.Spawn(position);
        _botTanks.Add(bot);
        _botsAlive++;

        ResetSpawnTimer();
    }

    private static TankType GetRandomType()
    {
        return (TankType)Rng.Next(4, 8);
    }

    private (int x, int y) GetNextSpot()
    {
        if (++_pointIndex >= _points.Count)
            _pointIndex = 0;

        return _points[_pointIndex];
    }

    private void HandleBotDestroyed(BotTank botTank)
    {
        _botTanks.Remove(botTank);
        _botsAlive--;
        if (_botsAlive <= 0 && SpawnsRemaining <= 0)
            _level.HandleAllBotsDestroyed();
    }
}