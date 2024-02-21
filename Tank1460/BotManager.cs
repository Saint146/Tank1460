using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Tank1460.Common;
using Tank1460.Common.Extensions;
using Tank1460.Common.Level.Object.Tank;
using Tank1460.LevelObjects.Tanks;

namespace Tank1460;

public class BotManager
{
    public int SpawnsRemaining { get; private set; }

    public IReadOnlyList<BotTank> BotTanks => _botTanks;

    private readonly List<BotTank> _botTanks = new();

    private readonly Level _level;
    private readonly List<(int x, int y)> _points = new();
    private int _pointIndex;
    private readonly double _respawnInterval;
    private double _timeToSpawnRemaining;
    private bool _spawnIsDue;

    private readonly int _totalSpawns;
    private readonly Queue<TankType> _tankTypes;
    private int _botsAlive;
    private readonly int _maxAliveBots;
    private int _periodIndex;
    private double _periodTime;

    private bool _paralyzeIsActive;
    private double _paralyzeTime;
    private double _paralyzeEffectTime;
    private static readonly int[] ClassicBotBonusNumbers = { 4, 11, 18 };
    private static readonly TankType[] AllBotTankTypes = { TankType.TypeB0, TankType.TypeB1, TankType.TypeB2, TankType.TypeB3 };

#if !DEBUG
    private readonly double _periodLength;
    private readonly double _periodResetTime = GameRules.TimeInFrames(16384);
#else
    private double _periodLength;
    private double _periodResetTime = GameRules.TimeInFrames(4320);
#endif

    public BotManager(Level level, int totalBots, int maxAliveBots)
    {
        _level = level;
        SpawnsRemaining = _totalSpawns = totalBots;
        _maxAliveBots = maxAliveBots;
        _tankTypes = ComposeTankTypeQueue(_level.Structure?.BotTypes);

        _respawnInterval = GameRules.TimeInFrames(190 - level.LevelNumber * 4 - (level.PlayerCount - 1) * 20);
        _periodLength = _respawnInterval * 8;

        if (!_level.ClassicRules)
            _respawnInterval = GameRules.TimeInFrames(8);

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
                    _periodResetTime = double.MaxValue;
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
        if (_periodTime > _periodResetTime)
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
        if (_periodIndex == periodIndex)
            return;

        _periodIndex = periodIndex;
        _botTanks.ForEach(e => e.PeriodIndex = periodIndex);
    }

    private void TrySpawnBot()
    {
        if (SpawnsRemaining <= 0 || _botsAlive >= _maxAliveBots)
            return;

        Debug.Assert(_points.Count > 0);

        SpawnsRemaining--;

        var (x, y) = GetNextSpot();

        var position = Level.GetTileBounds(x, y).Location;

        if (_tankTypes?.TryDequeue(out var type) is not true)
            type = GetRandomType();

        var botNumber = _totalSpawns - SpawnsRemaining;

        int hp, bonusCount;
        if (_level.ClassicRules)
        {
            hp = type == TankType.TypeB3 ? 4 : 1;
            bonusCount = ClassicBotBonusNumbers.Contains(botNumber) ? 1 : 0;
        }
        else
        {
            hp = Rng.Next(1, 4);
            if (Rng.OneIn(3))
            {
                bonusCount = Rng.Next(1, 4);
                hp += bonusCount - 1;
            }
            else
            {
                bonusCount = 0;
            }
        }

        var bot = new BotTank(_level, type, hp, bonusCount, SpawnsRemaining, _periodIndex);
        if (_paralyzeIsActive)
            bot.IsImmobile = bot.IsPacifist = true;

        bot.Spawn(position);
        _botTanks.Add(bot);
        _botsAlive++;

        ResetSpawnTimer();
    }

    private static TankType GetRandomType()
    {
        return AllBotTankTypes.GetRandom();
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