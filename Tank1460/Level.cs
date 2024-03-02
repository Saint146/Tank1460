using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Tank1460.AI.Algo;
using Tank1460.Audio;
using Tank1460.Common.Extensions;
using Tank1460.Common.Level;
using Tank1460.Common.Level.Object;
using Tank1460.Common.Level.Object.Tile;
using Tank1460.Globals;
using Tank1460.Input;
using Tank1460.LevelObjects;
using Tank1460.LevelObjects.Bonuses;
using Tank1460.LevelObjects.Explosions;
using Tank1460.LevelObjects.Tanks;
using Tank1460.LevelObjects.Tiles;

namespace Tank1460;

public class Level : IDisposable
{
    // TODO: Паблик Морозов во все поля и попахивает год-обжектом :(

    public int PlayerLivesRemaining(PlayerIndex playerIndex) => PlayerSpawners[playerIndex].LivesRemaining;

    public int BotSpawnsRemaining => BotManager?.SpawnsRemaining ?? 0;

    public bool BotsCanGrabBonuses { get; } = true;

    public int PlayerCount => PlayerSpawners.Count;

    public PlayerSpawner GetPlayerSpawner(PlayerIndex playerIndex) => PlayerSpawners[playerIndex];

    public BotManager BotManager { get; }

    public BonusManager BonusManager { get; }

    public List<Shell> Shells { get; } = new();

    public ContentManagerEx Content { get; }

    public Color BackColor => GameColors.Curtain;

    public string ShortName { get; }

    public int LevelNumber { get; }

    public Rectangle TileBounds { get; private set; }

    public Rectangle Bounds { get; private set; }

    public bool IsLoaded => Status != LevelStatus.Loading;

    public PlayerTank GetPlayerTank(PlayerIndex playerIndex) => PlayerSpawners[playerIndex]?.Tank;

    public IEnumerable<PlayerTank> GetAllPlayerTanks() => PlayersInGame.Select(GetPlayerTank).Where(tank => tank is not null);

    public List<Falcon> Falcons { get; } = new();

    internal PlayerIndex[] PlayersInGame { get; }

    internal ISoundPlayer SoundPlayer { get; }

    internal LevelModel Model { get; }

    internal LevelStats Stats { get; }

    internal bool ClassicRules => Tank1460Game.ClassicRules;

    internal IReadOnlyCollection<Point> ObstructedTiles => _obstructedTiles;

    internal ICollection<LevelObject> GetLevelObjectsInTile(int x, int y) => _tileObjectMap[x, y];

    internal HashSet<LevelObject> GetLevelObjectsInTiles(Rectangle tileRectangle)
    {
        var objects = new HashSet<LevelObject>();
        tileRectangle.EnumerateArray(_tileObjectMap, objects.UnionWith);
        return objects;
    }

    private LevelStatus Status { get; set; }

    private readonly List<Tile> _tiles = new();
    //private Texture2D[] layers;
    private List<LevelObject>[,] _tileObjectMap;

    /// <summary>
    /// Список заблокированных точек для алгоритма A*. Включает в себя границы экрана. Меняется при добавлении или удалении тайлов.
    /// </summary>
    // TODO: Добавить вес, чтобы кирпичи считались тяжелее пустых точек.
    private readonly HashSet<Point> _obstructedTiles = new();

    private readonly LevelEffects _levelEffects = new();

    // В оригинале именно так: зависит лишь от режима,
    // а не от того, жив ли второй игрок. Даже если уровень стартует, когда один уже без жизней, всё равно будет шесть.
    private int MaxAliveBots() => (PlayersInGame.Length + 1) * 2;

    private Dictionary<PlayerIndex, PlayerSpawner> PlayerSpawners { get; } = new();
    private Dictionary<PlayerIndex, int> PlayersScore { get; } = new();
    private readonly List<Explosion> _explosions = new();
    private const int TotalBots = 20;
    private const int MaxBonusesOnScreen = 1;
#if DEBUG
    private bool _cheatGodMode;
#endif

    private double _delayTime;
    private double _delayEffectTime;
    private LevelStatus _statusBeforePause;

    public Level(GameServiceContainer serviceProvider, LevelModel levelModel, GameState startingGameState)
    {
        Model = levelModel;
        ShortName = levelModel.ShortName;

        // TODO: Попробовать выкинуть этот номер всё-таки.
        if (!int.TryParse(ShortName, out var levelNumber))
            levelNumber = -1;
        LevelNumber = levelNumber;

        PlayersInGame = startingGameState.PlayersStates.Keys.ToArray();
        Stats = new LevelStats(PlayersInGame);

        Content = serviceProvider.GetService<ContentManagerEx>();
        SoundPlayer = serviceProvider.GetService<ISoundPlayer>();

        BotManager = new BotManager(this, TotalBots, MaxAliveBots());
        BonusManager = new BonusManager(this, MaxBonusesOnScreen);
        LoadTiles(levelModel.Tiles);
        LoadObjects(levelModel.Objects);

        // Load starting game state.
        foreach (var (playerIndex, playerState) in startingGameState.PlayersStates)
        {
            PlayersScore[playerIndex] = playerState.Score;
            PlayerSpawners[playerIndex].LivesRemaining = playerState.LivesRemaining;
            PlayerSpawners[playerIndex].SetNextSpawnSettings(playerState.TankType, playerState.TankHasShip);
        }

        Status = LevelStatus.Intro;
    }

    public void AddExplosion(Explosion explosion) => _explosions.Add(explosion);

    public PlayerTank GetTargetPlayerForBot(int botIndex)
    {
        return PlayersInGame.Length switch
        {
            0 => null,
            1 => botIndex % 2 == 0 ? null : GetPlayerTank(PlayersInGame[0]),
            _ => GetPlayerTank(PlayersInGame[botIndex % PlayersInGame.Length])
        };
    }

    public Falcon GetTargetFalconForBot(int botIndex)
    {
        var aliveFalcons = Falcons.Where(falcon => falcon.IsAlive).ToList();

        return aliveFalcons.Count switch
        {
            0 => null,
            1 => aliveFalcons[0],
            _ => aliveFalcons[botIndex % aliveFalcons.Count]
        };
    }

    public void HandleInput(PlayerInputCollection playersInputs)
    {
        switch (Status)
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
                throw new ArgumentOutOfRangeException(nameof(Status));
        }

        foreach (var playerIndex in PlayersInGame)
        {
            var playerInput = playersInputs[playerIndex];

            if (playerInput.Pressed.HasFlag(PlayerInputCommands.Start))
                TogglePause();

            var tank = GetPlayerTank(playerIndex);
            if (tank is null)
            {
                if (PlayerLivesRemaining(playerIndex) != 0)
                    continue;

                // Обрабатываем нажатия клавиш игрока без жизней.
                if (playerInput.Pressed.HasFlag(PlayerInputCommands.ShootTurbo) || playerInput.Pressed.HasFlag(PlayerInputCommands.Shoot))
                    TrySnatchLife(playerIndex);
            }
            else
            {
                tank.HandleInput(playerInput);
            }
        }

#if DEBUG
        if (KeyboardEx.HasBeenPressed(Keys.F10))
        {
            _cheatGodMode = !_cheatGodMode;
            GetAllPlayerTanks().ForEach(tank => tank.GodMode = _cheatGodMode);
        }

        if (KeyboardEx.HasBeenPressed(Keys.PageUp))
            GetAllPlayerTanks().ForEach(tank => tank.UpgradeUp());

        if (KeyboardEx.HasBeenPressed(Keys.PageDown))
            GetAllPlayerTanks().ForEach(tank => tank.UpgradeDown());

        if (KeyboardEx.IsPressed(Keys.LeftAlt))
        {
            foreach (var digit in Enumerable.Range(1, 4))
            {
                var key = Keys.NumPad0 + digit;
                if (!KeyboardEx.HasBeenPressed(key))
                    continue;

                var player = (PlayerIndex)(digit - 1);
                if (!PlayersInGame.Contains(player))
                    continue;

                BotManager.ExplodeAll(GetPlayerTank(player));

                break;
            }
        }

        if (KeyboardEx.IsPressed(Keys.Home))
        {
            foreach (var tank in BotManager.BotTanks)
            {
                tank.IsPacifist = true;
            }
        }
#endif
    }

    public void Update(GameTime gameTime)
    {
        var proceedWithUpdate = ProcessStatus(gameTime);

        if (proceedWithUpdate)
        {
            var playerTanks = GetAllPlayerTanks().ToList();
            playerTanks.FindAll(p => p.ToRemove).ForEach(HandlePlayerTankDestroyed);
            foreach (var playerTank in playerTanks)
                playerTank.Update(gameTime);

            foreach (var falcon in Falcons)
                falcon.Update(gameTime);

            BotManager.Update(gameTime);

            foreach (var shell in Shells)
                shell.Update(gameTime);
            Shells.RemoveAll(s => s.ToRemove);

            foreach (var explosion in _explosions)
                explosion.Update(gameTime);
            _explosions.RemoveAll(e => e.ToRemove);

            // TODO: Написать нормально
            _tiles.Where(t => t.ToRemove).ForEach(HandleTileRemoved);
            _tiles.RemoveAll(t => t.ToRemove);

            foreach (var tile in _tiles)
                tile.Update(gameTime);

            foreach (var playerSpawner in PlayerSpawners.Values)
                playerSpawner.Update(gameTime);

            BonusManager.Update(gameTime);
        }

        _levelEffects.Update(gameTime, Status == LevelStatus.Paused);
    }

    public bool CanTankPassThroughTile(Tank tank, Point tilePoint)
    {
        var tileObjects = _tileObjectMap.ElementAtOrDefault(tilePoint.X, tilePoint.Y);
        return tileObjects?.All(o => !o.CollisionType.HasFlag(CollisionType.Impassable) && (!o.CollisionType.HasFlag(CollisionType.PassableOnlyByShip) || tank.HasShip)) == true;
    }

    public void HandleFalconDestroyed(Falcon falcon)
    {
        if (!Falcons.Any(f => f.IsAlive))
            StartGameOverSequence();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        spriteBatch.FillRectangle(Bounds, Color.Black);

        //for (int i = 0; i <= EntityLayer; ++i)
        //    spriteBatch.Draw(layers[i], Vector2.Zero, Color.White);

        // TODO: перейти на слои
        foreach (var tile in _tiles.Where(tile => tile.TileLayer == TileLayer.Default))
            tile.Draw(gameTime, spriteBatch);

#if DEBUG
        if (GameRules.ShowObstructedTiles)
        {
            foreach (var point in _obstructedTiles)
            {
                var obstructedRect = GetTileBounds(point.X, point.Y);
                spriteBatch.DrawRectangle(obstructedRect, new Color(0x220000ff));
            }
        }
#endif

        foreach (var playerTank in GetAllPlayerTanks())
            playerTank.Draw(gameTime, spriteBatch);

        foreach (var falcon in Falcons)
            falcon.Draw(gameTime, spriteBatch);

        foreach (var explosion in _explosions)
            explosion.Draw(gameTime, spriteBatch);

        foreach (var shell in Shells)
            shell.Draw(gameTime, spriteBatch);

        BotManager.Draw(gameTime, spriteBatch);

        foreach (var tile in _tiles.Where(tile => tile.TileLayer == TileLayer.Foreground))
            tile.Draw(gameTime, spriteBatch);

        BonusManager.Draw(gameTime, spriteBatch);
        _levelEffects.Draw(spriteBatch, Bounds);
    }

    public static Rectangle GetTileBounds(int tileX, int tileY) =>
        new(tileX * Tile.DefaultWidth, tileY * Tile.DefaultHeight, Tile.DefaultWidth, Tile.DefaultHeight);

    public void HandleChangeTileBounds(LevelObject levelObject, Rectangle oldTileBounds, Rectangle? newTileBounds)
    {
        oldTileBounds.EnumerateArray(_tileObjectMap, tileLevelObjects => { tileLevelObjects.Remove(levelObject); });

        newTileBounds?.EnumerateArray(_tileObjectMap, tileLevelObjects => { tileLevelObjects.Add(levelObject); });
    }

    public void HandleAllBotsDestroyed()
    {
        if (Status is LevelStatus.LostPreDelay or LevelStatus.LostDelay)
            return;

        Status = LevelStatus.WinDelay;
        _delayTime = 0.0;
        _delayEffectTime = GameRules.TimeInFrames(144);
    }

    public void Start()
    {
        Debug.Assert(Status == LevelStatus.Intro);
        Status = LevelStatus.Running;
    }

    public void HandleObjectRemoved(UpdateableObject o)
    {
        if (o is not LevelObject levelObject)
            return;

        HandleChangeTileBounds(levelObject, levelObject.TileRectangle, null);
    }

    public ICollection<LevelObject> GetAllCollisionsSimple(LevelObject levelObject,
        IReadOnlyCollection<LevelObject> excludedObjects = null)
    {
        if (DetectOutOfBoundsCollisionSimple(levelObject.TileRectangle))
            return new LevelObject[] { null };

        // Ограничиваем список всех объектов на уровне теми, что находятся в тех же тайлах.
        var closeObjects = GetLevelObjectsInTiles(levelObject.TileRectangle);

        // Выкидываем из него лишнее - сам объект и то, что было указано как лишнее.
        closeObjects.Remove(levelObject);
        if (excludedObjects is not null)
            closeObjects.RemoveWhere(excludedObjects.Contains);

        // Вот здесь уже настоящая проверка коллизий.
        // TODO: Использовать более простой метод, ведь нам тут не надо считать глубину.
        closeObjects.RemoveWhere(closeObject =>
            levelObject.BoundingRectangle.GetIntersectionDepth(closeObject.BoundingRectangle) == Vector2.Zero);

        return closeObjects;
    }

    internal void AddTile(Tile tile, int x, int y)
    {
        _tiles.Add(tile);
        tile.Spawn(new Point(x * Tile.DefaultWidth, y * Tile.DefaultHeight));

        AddObstructionForTile(tile);
    }

    private void RemoveTile(Tile tile)
    {
        tile.Remove();
    }

    private void HandleTileRemoved(Tile tile)
    {
        if (!IsTileObstructing(tile))
            return;

        var tilePosition = tile.TileRectangle.Location;

        // Пересчитываем заново занятость клеток для квадрата 3х3 вокруг нужной клетки.
        var allPointsToRecalcObstruction = new Rectangle(tilePosition.X - 1, tilePosition.Y - 1, 3, 3)
                                           .Clip(TileBounds)
                                           .GetAllPoints()
                                           .ToHashSet();

        foreach (var tilePoint in allPointsToRecalcObstruction)
            _obstructedTiles.Remove(tilePoint);

        // Сама удаленная клетка ещё на этот момент остаётся в коллекции.
        allPointsToRecalcObstruction.Remove(tilePosition);
        foreach (var t in allPointsToRecalcObstruction.Select(GetTile).Where(t => t is not null))
            AddObstructionForTile(t);
    }

    private void AddObstructionForTile(Tile tile)
    {
        if (!IsTileObstructing(tile))
            return;

        var tilePosition = tile.TileRectangle.Location;
        _obstructedTiles.Add(tilePosition);
        _obstructedTiles.Add(tilePosition with { X = tilePosition.X - 1 });
        _obstructedTiles.Add(tilePosition with { Y = tilePosition.Y - 1 });
        _obstructedTiles.Add(tilePosition with { X = tilePosition.X - 1, Y = tilePosition.Y - 1 });
    }

    private bool IsTileObstructing(Tile tile) => tile.Type is TileType.Concrete or TileType.Water;

    internal Tile GetTile(int x, int y)
    {
        return (Tile)_tileObjectMap[x, y].SingleOrDefault(o => o is Tile);
    }

    internal Tile GetTile(Point point) => GetTile(point.X, point.Y);

    internal void TryRemoveTileAt(int x, int y)
    {
        var tile = GetTile(x, y);
        if (tile != null)
            RemoveTile(tile);
    }

    internal void ArmorFalcons(double effectTime)
    {
        _levelEffects.AddExclusive(new ArmoredFalconEffect(this, effectTime));
    }

    internal void LeaveFalconsUnprotected()
    {
        _levelEffects.AddExclusive(new UnprotectedFalconEffect(this));
    }

    internal void RemoveAllEffects<T>() where T : LevelEffect
    {
        _levelEffects.RemoveAll(e => e is T);
    }

    internal void AddPlayerStatsForDefeatingTank(PlayerIndex playerIndex, Tank tank)
    {
        var tankType = tank.Type;

        var playerBotStats = Stats.PlayerStats[playerIndex].BotsDefeated;
        if (playerBotStats.TryGetValue(tankType, out var value))
            playerBotStats[tankType] = value + 1;
        else
            playerBotStats[tankType] = 1;

        RewardPlayerWithPoints(playerIndex, GameRules.TankScoreByType[tankType]);
    }

    internal void RewardPlayerWithPoints(PlayerIndex playerIndex, int points)
    {
        Debug.Assert(points > 0);

        var oneUpsGained = GameRules.GetOneUpsGained(PlayersScore[playerIndex], points);
        PlayersScore[playerIndex] += points;

        if (oneUpsGained > 0)
            PlayerSpawners[playerIndex].AddOneUps(oneUpsGained);
    }

    internal void CreateFloatingText(Point centerPosition, string text, double effectTime)
    {
        _levelEffects.Add(new FloatingText(this, text, centerPosition, effectTime));
    }

    internal void HandlePlayerLostAllLives(PlayerIndex playerIndex)
    {
        if (PlayersInGame.All(player => PlayerSpawners[player].ControlledByAi || PlayerLivesRemaining(player) == 0))
        {
            StartGameOverSequence();
            return;
        }

        _levelEffects.Add(new GameOverLevelEffect(this, playerIndex));
    }

    internal GameState GetGameState()
    {
        var gameState = new GameState(PlayersInGame);
        foreach (var (playerIndex, state) in gameState.PlayersStates)
        {
            var tank = GetPlayerTank(playerIndex);

            state.LivesRemaining = PlayerLivesRemaining(playerIndex);
            state.Score = PlayersScore[playerIndex];
            state.TankType = tank?.Type;
            state.TankHasShip = tank?.HasShip is true;
        }

        return gameState;
    }

    private bool ProcessStatus(GameTime gameTime)
    {
        switch (Status)
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

                Status = LevelStatus.Win;
                SoundPlayer.StopAll();
                LevelComplete?.Invoke(this);

                return false;

            case LevelStatus.LostPreDelay:
                _delayTime += gameTime.ElapsedGameTime.TotalSeconds;
                if (_delayTime <= _delayEffectTime)
                    return true;

                _levelEffects.AddExclusive(new GameOverLevelEffect(this));
                SoundPlayer.MuteAllWithLessPriorityThan(Sound.ExplosionBig);
                Status = LevelStatus.LostDelay;

                _delayTime = 0.0;
                _delayEffectTime = GameRules.TimeInFrames(252);
                return true;

            case LevelStatus.LostDelay:
                _delayTime += gameTime.ElapsedGameTime.TotalSeconds;
                if (_delayTime <= _delayEffectTime)
                    return true;

                Status = LevelStatus.GameOver;
                _levelEffects.RemoveAll();
                SoundPlayer.StopAll();
                SoundPlayer.Unmute();
                GameOver?.Invoke(this);

                return false;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void LoadTiles(TileType[,] tileTypes)
    {
        var width = tileTypes.GetLength(0);
        var height = tileTypes.GetLength(1);

        _tileObjectMap = new List<LevelObject>[width, height];
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                _tileObjectMap[x, y] = new List<LevelObject>();

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var tile = LoadTile(tileTypes[x, y]);
                if (tile is null)
                    continue;

                AddTile(tile, x, y);
            }
        }

        TileBounds = new Rectangle(0, 0, width, height);
        Bounds = TileBounds.Multiply(new Point(Tile.DefaultWidth, Tile.DefaultHeight));

        var levelBoundsRect = TileBounds;
        levelBoundsRect.Inflate(1, 1);
        _obstructedTiles.UnionWith(levelBoundsRect.GetOutlinePoints());
    }

    private Tile LoadTile(TileType tileType)
    {
        // TODO: Заменить на фабричный метод.
        return tileType switch
        {
            TileType.Empty => null,

            TileType.Brick => new BrickTile(this),

            TileType.Concrete => new ConcreteTile(this),

            TileType.Water => new WaterTile(this),

            TileType.Forest => new ForestTile(this),

            TileType.Ice => new IceTile(this),

            _ => throw new NotSupportedException()
        };
    }

    private void LoadObjects(LevelObjectModel[] objectModels)
    {
        foreach (var objectModel in objectModels.EmptyIfNull())
            switch (objectModel)
            {
                case BotSpawnerModel botSpawnerModel:
                    CreateBotSpawn(botSpawnerModel.Bounds);
                    break;

                case FalconModel falcon:
                    CreateFalcon(falcon.Bounds);
                    break;

                case PlayerSpawnerModel playerSpawnerModel:
                    CreatePlayerSpawn(playerSpawnerModel.Bounds, playerSpawnerModel.Player);
                    break;

                default:
                    throw new NotSupportedException();
            }

        CreateMissingRequiredObjects();

        ClearTilesUnderObjects();
    }

    private void CreatePlayerSpawn(Rectangle tileBounds, PlayerIndex playerIndex)
    {
        if (!PlayersInGame.Contains(playerIndex))
            return;

        if (PlayerSpawners.ContainsKey(playerIndex))
            throw new NotSupportedException($"A level may only have one player {playerIndex} spawn.");

        var playerSpawner = new PlayerSpawner(this, tileBounds, playerIndex);
        PlayerSpawners.Add(playerIndex, playerSpawner);

        if (playerIndex == PlayerIndex.One || !GameRules.AiEnabled)
            return;

        playerSpawner.ControlledByAi = true;
        playerSpawner.HasInfiniteLives = GameRules.AiHasInfiniteLives;
    }

    private void CreateFalcon(Rectangle tileBounds)
    {
        var falcon = new Falcon(this, tileBounds.Size);
        Falcons.Add(falcon);

        falcon.Spawn(tileBounds.Location * Tile.DefaultSize);
    }

    private void CreateBotSpawn(Rectangle tileBounds)
    {
        BotManager.AddSpawnPoint(tileBounds);
    }

    private void CreateMissingRequiredObjects()
    {
        var playersWithoutSpawners = PlayersInGame.Where(playerIndex => !PlayerSpawners.ContainsKey(playerIndex)).ToList();

        // TODO: Временный хардкод, пока не дойдут руки переделать сами файлы уровней.
        if (Falcons.Count == 0)
            CreateFalcon(new Rectangle(12, 24, 2, 2));

        if (BotManager.SpawnAreas.Count == 0)
        {
            CreateBotSpawn(new Rectangle(0, 0, 2, 2));
            CreateBotSpawn(new Rectangle(12, 0, 2, 2));
            CreateBotSpawn(new Rectangle(24, 0, 2, 2));
        }

        if (playersWithoutSpawners.Contains(PlayerIndex.One))
            CreatePlayerSpawn(new Rectangle(8, 24, 2, 2), PlayerIndex.One);

        if (playersWithoutSpawners.Contains(PlayerIndex.Two))
            CreatePlayerSpawn(new Rectangle(16, 24, 2, 2), PlayerIndex.Two);

        if (playersWithoutSpawners.Contains(PlayerIndex.Three))
            CreatePlayerSpawn(new Rectangle(5, 24, 2, 2), PlayerIndex.Three);

        if (playersWithoutSpawners.Contains(PlayerIndex.Four))
            CreatePlayerSpawn(new Rectangle(19, 24, 2, 2), PlayerIndex.Four);
    }

    private void ClearTilesUnderObjects()
    {
        var pointsToClear = new HashSet<Point>();

        foreach (var spawner in PlayerSpawners.Values)
            spawner.TileBounds.GetAllPoints().ForEach(point => pointsToClear.Add(point));

        foreach (var falcon in Falcons)
            falcon.TileRectangle.GetAllPoints().ForEach(point => pointsToClear.Add(point));

        foreach (var point in pointsToClear)
        {
            var tile = GetTile(point.X, point.Y);
            if (tile is { CollisionType: not CollisionType.None })
                RemoveTile(tile);
        }
    }

    private void TogglePause()
    {
        switch (Status)
        {
            case LevelStatus.Loading:
            case LevelStatus.WinDelay:
            case LevelStatus.LostDelay:
                return;

            case LevelStatus.Intro:
            case LevelStatus.LostPreDelay:
            case LevelStatus.Running:
                _statusBeforePause = Status;
                Status = LevelStatus.Paused;
                SoundPlayer.PauseAndPushState();
                SoundPlayer.Play(Sound.Pause);
                _levelEffects.AddExclusive(new PauseLevelEffect(this));
                break;

            case LevelStatus.Paused:
                _levelEffects.RemoveAll<PauseLevelEffect>();
                Status = _statusBeforePause;
                SoundPlayer.ResumeAndPopState();
                break;

            case LevelStatus.Win:
            case LevelStatus.GameOver:
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void HandlePlayerTankDestroyed(PlayerTank playerTank)
    {
        var playerSpawner = PlayerSpawners[playerTank.PlayerIndex];
        if (playerSpawner is null)
        {
            Debug.Fail($"Cannot find player spawner for player {playerTank.PlayerIndex}.");
            return;
        }

        playerSpawner.HandleTankDestroyed(playerTank);
    }

    private void TrySnatchLife(PlayerIndex playerIndex)
    {
        if (!PlayerSpawners.Values.TryGetFirst(out var spawnerDonator, spawner => spawner.CanDonateLife()))
            return;

        spawnerDonator.DonateLife(PlayerSpawners[playerIndex]);
        _levelEffects.RemoveAll<GameOverLevelEffect>(effect => effect.PlayerIndex == playerIndex);
    }

    private void StartGameOverSequence()
    {
        if (Status is LevelStatus.LostPreDelay or LevelStatus.LostDelay)
            return;

        Status = LevelStatus.LostPreDelay;
        _delayEffectTime = GameRules.TimeInFrames(36);
    }

    private bool DetectOutOfBoundsCollisionSimple(Rectangle tileRect)
    {
        if (tileRect.Top < TileBounds.Top)
            return true;

        if (tileRect.Bottom > TileBounds.Bottom)
            return true;

        if (tileRect.Left < TileBounds.Left)
            return true;

        if (tileRect.Right > TileBounds.Right)
            return true;

        return false;
    }

    public delegate void LevelEvent(Level level);
    public delegate void LevelEvent<in T>(Level level, T args);

    public event LevelEvent GameOver;
    public event LevelEvent LevelComplete;
}