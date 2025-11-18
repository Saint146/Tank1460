using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
using Tank1460.Managers;

namespace Tank1460;

public class Level : IDisposable
{
    // Public Properties
    public int PlayerLivesRemaining(PlayerIndex playerIndex) => _playerManager.GetPlayerLivesRemaining(playerIndex);

    public int BotSpawnsRemaining => BotManager?.SpawnsRemaining ?? 0;

    public bool BotsCanGrabBonuses { get; } = true;

    public int PlayerCount => _playerManager.GetAllPlayerSpawners().Count();

    public PlayerSpawner GetPlayerSpawner(PlayerIndex playerIndex) => _playerManager.GetPlayerSpawner(playerIndex);

    public BotManager BotManager { get; }

    public BonusManager BonusManager { get; }

    public List<Shell> Shells { get; } = new();

    public ContentManagerEx Content { get; }

    public Color BackColor => GameColors.Curtain;

    public string ShortName { get; }

    public int LevelNumber { get; }

    public Rectangle TileBounds { get; private set; }

    public Rectangle Bounds { get; private set; }

    public bool IsLoaded => _stateManager.Status != LevelStatus.Loading;

    public PlayerTank GetPlayerTank(PlayerIndex playerIndex) => _playerManager.GetPlayerTank(playerIndex);

    public IEnumerable<PlayerTank> GetAllPlayerTanks() => PlayersInGame.Select(GetPlayerTank).Where(tank => tank is not null);

    public List<Falcon> Falcons { get; } = new();

    internal PlayerIndex[] PlayersInGame { get; }

    internal ISoundPlayer SoundPlayer { get; }

    internal LevelModel Model { get; }

    internal LevelStats Stats { get; }

    internal bool ClassicRules => Tank1460Game.ClassicRules;

    public ICollection<LevelObject> GetLevelObjectsInTile(int x, int y) => _collisionDetector.GetLevelObjectsInTile(x, y);

    public HashSet<LevelObject> GetLevelObjectsInTiles(Rectangle tileRectangle) => _collisionDetector.GetLevelObjectsInTiles(tileRectangle);

    // Private Managers and Fields
    private readonly LevelStateManager _stateManager;
    private LevelCollisionDetector _collisionDetector;
    private readonly LevelPlayerManager _playerManager;
    private readonly LevelInputHandler _inputHandler;

    private readonly List<Tile> _tiles = new();
    private List<LevelObject>[,] _tileObjectMap;

    private readonly LevelEffects _levelEffects = new();
    private readonly List<Explosion> _explosions = new();

    private const int TotalBots = 20;
    private const int MaxBonusesOnScreen = 1;

    private int MaxAliveBots() => (PlayersInGame.Length + 1) * 2;

    public Level(GameServiceContainer serviceProvider, LevelModel levelModel, GameState startingGameState)
    {
        Model = levelModel;
        ShortName = levelModel.ShortName;

        if (!int.TryParse(ShortName, out var levelNumber))
            levelNumber = -1;
        LevelNumber = levelNumber;

        PlayersInGame = startingGameState.PlayersStates.Keys.ToArray();
        Stats = new LevelStats(PlayersInGame);

        Content = serviceProvider.GetService<ContentManagerEx>();
        SoundPlayer = serviceProvider.GetService<ISoundPlayer>();

        // Initialize managers
        _playerManager = new LevelPlayerManager(_levelEffects, this);
        _stateManager = new LevelStateManager(
            this, SoundPlayer,
            _levelEffects,
            () => LevelComplete?.Invoke(this),
            () => GameOver?.Invoke(this));
        _collisionDetector = new LevelCollisionDetector(_tileObjectMap, new Rectangle(0, 0, 26, 26));
        _inputHandler = new LevelInputHandler(this, _stateManager, _playerManager);
        BotManager = new BotManager(this, TotalBots, MaxAliveBots());
        BonusManager = new BonusManager(this, MaxBonusesOnScreen);


        LoadTiles(levelModel.Tiles);
        LoadObjects(levelModel.Objects);

        // Load starting game state
        foreach (var (playerIndex, playerState) in startingGameState.PlayersStates)
        {
            _playerManager.SetPlayerScore(playerIndex, playerState.Score);
            GetPlayerSpawner(playerIndex).LivesRemaining = playerState.LivesRemaining;
            GetPlayerSpawner(playerIndex).SetNextSpawnSettings(playerState.TankType, playerState.TankHasShip);
        }

        _stateManager.Status = LevelStatus.Intro;
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
        _inputHandler.HandleInput(playersInputs);
    }

    public void Update(GameTime gameTime)
    {
        var proceedWithUpdate = _stateManager.ProcessStatus(gameTime);

        if (proceedWithUpdate)
        {
            var playerTanks = GetAllPlayerTanks().ToList();
            playerTanks.FindAll(p => p.ToRemove).ForEach(_playerManager.HandlePlayerTankDestroyed);
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

            _tiles.RemoveAll(t => t.ToRemove);
            foreach (var tile in _tiles)
                tile.Update(gameTime);

            foreach (var playerSpawner in _playerManager.GetAllPlayerSpawners())
                playerSpawner.Update(gameTime);

            BonusManager.Update(gameTime);
        }

        _levelEffects.Update(gameTime, _stateManager.Status == LevelStatus.Paused);
    }

    public bool CanTankPassThroughTile(Tank tank, Point tilePoint)
    {
        return _collisionDetector.CanTankPassThroughTile(tank, tilePoint);
    }

    public void HandleFalconDestroyed(Falcon falcon)
    {
        if (!Falcons.Any(f => f.IsAlive))
            HandleAllPlayersLostLives();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        spriteBatch.FillRectangle(Bounds, Color.Black);

        foreach (var tile in _tiles.Where(tile => tile.TileLayer == TileLayer.Default))
            tile.Draw(gameTime, spriteBatch);

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
        _collisionDetector.HandleChangeTileBounds(levelObject, oldTileBounds, newTileBounds);
    }

    public void HandleAllBotsDestroyed()
    {
        _stateManager.HandleAllBotsDestroyed();
    }

    public void Start()
    {
        _stateManager.Start();
    }

    public void HandleObjectRemoved(UpdateableObject o)
    {
        if (o is not LevelObject levelObject)
            return;

        HandleChangeTileBounds(levelObject, levelObject.TileRectangle, null);
    }

    public ICollection<LevelObject> GetAllCollisionsSimple(LevelObject levelObject, IReadOnlyCollection<LevelObject> excludedObjects = null)
    {
        return _collisionDetector.GetAllCollisionsSimple(levelObject, excludedObjects);
    }

    internal void AddTile(Tile tile, int x, int y)
    {
        _tiles.Add(tile);
        tile.Spawn(new Point(x * Tile.DefaultWidth, y * Tile.DefaultHeight));
    }

    internal Tile GetTile(int x, int y)
    {
        return (Tile)_collisionDetector.GetLevelObjectsInTile(x, y).SingleOrDefault(o => o is Tile);
    }

    internal void TryRemoveTileAt(int x, int y)
    {
        GetTile(x, y)?.Remove();
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
        _playerManager.AddPlayerStatsForDefeatingTank(playerIndex, tank);
    }

    internal void RewardPlayerWithPoints(PlayerIndex playerIndex, int points)
    {
        _playerManager.RewardPlayerWithPoints(playerIndex, points);
    }

    internal void CreateFloatingText(Point centerPosition, string text, double effectTime)
    {
        _levelEffects.Add(new FloatingText(this, text, centerPosition, effectTime));
    }

    internal void HandlePlayerLostAllLives(PlayerIndex playerIndex)
    {
        _playerManager.HandlePlayerLostAllLives(playerIndex);
    }

    internal void HandleAllPlayersLostLives()
    {
        _stateManager.StartGameOverSequence();
    }

    internal GameState GetGameState()
    {
        var gameState = new GameState(PlayersInGame);
        foreach (var (playerIndex, state) in gameState.PlayersStates)
        {
            var tank = GetPlayerTank(playerIndex);

            state.LivesRemaining = PlayerLivesRemaining(playerIndex);
            state.Score = _playerManager.GetPlayerScore(playerIndex);
            state.TankType = tank?.Type;
            state.TankHasShip = tank?.HasShip is true;
        }

        return gameState;
    }

    private void LoadTiles(TileType[,] tileTypes)
    {
        var width = tileTypes.GetLength(0);
        var height = tileTypes.GetLength(1);

        _tileObjectMap = new List<LevelObject>[width, height];
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                _tileObjectMap[x, y] = new List<LevelObject>();

        // Update collision detector with the new tile object map
        _collisionDetector = new LevelCollisionDetector(_tileObjectMap, new Rectangle(0, 0, width, height));

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
    }

    private Tile LoadTile(TileType tileType)
    {
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

        if (_playerManager.ContainsPlayerSpawner(playerIndex))
            throw new NotSupportedException($"A level may only have one player {playerIndex} spawn.");

        var playerSpawner = new PlayerSpawner(this, tileBounds, playerIndex);
        _playerManager.AddPlayerSpawner(playerIndex, playerSpawner);

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
        var playersWithoutSpawners = PlayersInGame.Where(playerIndex => !_playerManager.ContainsPlayerSpawner(playerIndex)).ToList();

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

        foreach (var spawner in _playerManager.GetAllPlayerSpawners())
            spawner.TileBounds.GetAllPoints().ForEach(point => pointsToClear.Add(point));

        foreach (var falcon in Falcons)
            falcon.TileRectangle.GetAllPoints().ForEach(point => pointsToClear.Add(point));

        foreach (var point in pointsToClear)
        {
            var tile = GetTile(point.X, point.Y);
            if (tile is { CollisionType: not CollisionType.None })
            {
                tile.Remove();
            }
        }
    }

    public delegate void LevelEvent(Level level);
    public delegate void LevelEvent<in T>(Level level, T args);

    public event LevelEvent GameOver;
    public event LevelEvent LevelComplete;
}