using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Tank1460.Audio;
using Tank1460.Extensions;
using Tank1460.LevelObjects;
using Tank1460.LevelObjects.Bonuses;
using Tank1460.LevelObjects.Explosions;
using Tank1460.LevelObjects.Tanks;
using Tank1460.LevelObjects.Tiles;

namespace Tank1460;

public class Level : IDisposable
{
    // TODO: Паблик Морозов во все поля

    public int PlayerLivesRemaining(int playerNumber) => _playerSpawners[playerNumber].LivesRemaining;

    public bool IsPlayerInGame(int playerNumber) => _playersInGame.Contains(playerNumber);

    public int BotSpawnsRemaining => BotManager?.SpawnsRemaining ?? 0;

    public bool BotsCanGrabBonuses { get; } = true;

    public int PlayerCount => _playerSpawners.Count;

    public PlayerSpawner GetPlayerSpawner(int playerNumber) => _playerSpawners[playerNumber];

    public BotManager BotManager { get; }

    public BonusManager BonusManager { get; }

    public List<Shell> Shells = new();

    public ContentManagerEx Content { get; }

    public int LevelNumber { get; }

    public Rectangle TileBounds { get; private set; }

    public Rectangle Bounds { get; private set; }

    internal ISoundPlayer SoundPlayer;

    internal LevelStructure Structure { get; }

    private readonly int[] _playersInGame = { 1, 2 };
    private readonly List<Tile> _tiles = new();
    //private Texture2D[] layers;
    private List<LevelObject>[,] _tileObjectMap;

    // В оригинале именно так: зависит лишь от режима,
    // а не от того, жив ли второй игрок. Даже если уровень стартует, когда один уже без жизней, всё равно будет шесть.
    private int MaxAliveBots() => (_playersInGame.Length + 1) * 2;

    private List<Falcon> Falcons { get; } = new();

    private List<PlayerTank> PlayerTanks { get; } = new();
    private readonly Dictionary<int, PlayerSpawner> _playerSpawners = new();
    private const int MaxPlayerCount = 2;
    private readonly List<Explosion> _explosions = new();
    private const int TotalBots = 20;
    private const int MaxBonusesOnScreen = 1;

    // TODO: State
    private bool _isGamePaused;
    public bool IsGamePaused
    {
        get => _isGamePaused;
        private set
        {
            if (_isGamePaused != value)
            {
                if (value)
                    SoundPlayer.PauseAndPushState();
                else
                    SoundPlayer.ResumeAndPopState();
            }

            _isGamePaused = value;
            if (value)
                SoundPlayer.Play(Sound.Pause);
        }
    }

    public Level(IServiceProvider serviceProvider, LevelStructure levelStructure, int levelNumber)
    {
        Structure = levelStructure;
        LevelNumber = levelNumber;

        Content = new ContentManagerEx(serviceProvider, "Content");

        SoundPlayer = new SoundPlayer(Content);
        BotManager = new BotManager(this, TotalBots, MaxAliveBots());
        BonusManager = new BonusManager(this, MaxBonusesOnScreen);
        LoadTiles(levelStructure.Tiles);

        SoundPlayer.Play(Sound.Intro);
    }

    public void AddExplosion(Explosion explosion) => _explosions.Add(explosion);

    public void AddPlayer(PlayerTank playerTank) => PlayerTanks.Add(playerTank);

    public PlayerTank GetTargetPlayerForBot(int botIndex)
    {
        return PlayerTanks.Count switch
        {
            0 => null,
            1 => botIndex % 2 == 0 ? null : PlayerTanks[0],
            _ => PlayerTanks[botIndex % PlayerTanks.Count]
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
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Content.Unload();
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        spriteBatch.FillRectangle(Bounds, Color.Black);

        //for (int i = 0; i <= EntityLayer; ++i)
        //    spriteBatch.Draw(layers[i], Vector2.Zero, Color.White);

        // TODO: перейти на слои
        foreach (var tile in _tiles.Where(tile => tile.TileView == TileView.Default))
            tile.Draw(gameTime, spriteBatch);

        foreach (var player in PlayerTanks)
            player.Draw(gameTime, spriteBatch);

        foreach (var falcon in Falcons)
            falcon.Draw(gameTime, spriteBatch);

        foreach (var explosion in _explosions)
            explosion.Draw(gameTime, spriteBatch);

        foreach (var shell in Shells)
            shell.Draw(gameTime, spriteBatch);

        BotManager.Draw(gameTime, spriteBatch);

        foreach (var tile in _tiles.Where(tile => tile.TileView == TileView.Foreground))
            tile.Draw(gameTime, spriteBatch);

        BonusManager.Draw(gameTime, spriteBatch);
    }

    public static Rectangle GetTileBounds(int tileX, int tileY)
    {
        return new Rectangle(tileX * Tile.DefaultWidth, tileY * Tile.DefaultHeight, Tile.DefaultWidth,
            Tile.DefaultHeight);
    }

    public void HandleChangeTileBounds(LevelObject levelObject, Rectangle oldTileBounds, Rectangle? newTileBounds)
    {
        oldTileBounds.EnumerateArray(_tileObjectMap, tileLevelObjects => { tileLevelObjects.Remove(levelObject); });

        newTileBounds?.EnumerateArray(_tileObjectMap, tileLevelObjects => { tileLevelObjects.Add(levelObject); });
    }

    public void HandleObjectRemoved(LevelObject levelObject)
    {
        HandleChangeTileBounds(levelObject, levelObject.TileRectangle, null);
    }

    public ICollection<LevelObject> GetAllCollisionsSimple(LevelObject levelObject,
        IReadOnlyCollection<LevelObject> excludedObjects = null)
    {
        if (DetectOutOfBoundsCollisionSimple(levelObject.TileRectangle))
            return new LevelObject[] { null };

        var closeObjects = new HashSet<LevelObject>();
        levelObject.TileRectangle.EnumerateArray(_tileObjectMap, closeObjects.UnionWith);

        closeObjects.Remove(levelObject);
        if (excludedObjects is not null)
            closeObjects.RemoveWhere(excludedObjects.Contains);

        closeObjects.RemoveWhere(closeObject =>
            levelObject.BoundingRectangle.GetIntersectionDepth(closeObject.BoundingRectangle) == Vector2.Zero);

        return closeObjects;
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
                var tile = LoadTile(tileTypes[x, y], x, y);
                if (tile is null)
                    continue;

                _tiles.Add(tile);
                tile.Spawn(new Point(x * Tile.DefaultWidth, y * Tile.DefaultHeight));
            }
        }

        TileBounds = new Rectangle(0, 0, width, height);
        Bounds = TileBounds.Multiply(new Point(Tile.DefaultWidth, Tile.DefaultHeight));

        if (_playerSpawners.Count is < 1 or > MaxPlayerCount)
            throw new NotSupportedException($"A level must have 1 to {MaxPlayerCount} starting point(s).");

        if (Falcons.Count == 0)
            throw new NotSupportedException("A level must have the falcon.");
    }

    private void HandlePlayerTankDestroyed(PlayerTank playerTank)
    {
        PlayerTanks.Remove(playerTank);
        var playerSpawner = _playerSpawners[playerTank.PlayerNumber];

        if (playerSpawner is null)
        {
            Debug.Fail($"Cannot find player spawner for player {playerTank.PlayerNumber}.");
            return;
        }

        playerSpawner.HandleTankDestroyed(playerTank);
    }

    private Tile LoadTile(TileType tileType, int x, int y)
    {
        return tileType switch
        {
            TileType.Empty => null,

            TileType.Brick => new BrickTile(this),

            TileType.Concrete => new ConcreteTile(this),

            TileType.Water => new WaterTile(this),

            TileType.Forest => new ForestTile(this),

            TileType.Ice => new IceTile(this),

            TileType.Player1Spawn => CreatePlayerSpawn(x, y, 1),

            TileType.Player2Spawn => CreatePlayerSpawn(x, y, 2),

            TileType.BotSpawn => CreateBotSpawn(x, y),

            TileType.Falcon => CreateFalcon(x, y),

            _ => throw new NotSupportedException()
        };
    }

    private Tile CreateBotSpawn(int x, int y)
    {
        BotManager.AddSpawnPoint(x, y);
        return null;
    }

    private Tile CreatePlayerSpawn(int x, int y, int playerNumber)
    {
        if (_playerSpawners.ContainsKey(playerNumber))
            throw new NotSupportedException($"A level may only have one player {playerNumber} spawn.");

        var playerSpawner = new PlayerSpawner(this, x, y, playerNumber);
        _playerSpawners.Add(playerNumber, playerSpawner);

        if (!IsPlayerInGame(playerNumber))
            playerSpawner.Disable();

        return null;
    }

    private Tile CreateFalcon(int x, int y)
    {
        var falcon = new Falcon(this);
        Falcons.Add(falcon);

        falcon.Spawn(new Point(x * Tile.DefaultWidth, y * Tile.DefaultHeight));

        return null;
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

    public void Update(GameTime gameTime, KeyboardState keyboardState)
    {
        HandleInput();

        if (!IsGamePaused)
        {
            PlayerTanks.FindAll(p => p.ToRemove).ForEach(HandlePlayerTankDestroyed);
            foreach (var player in PlayerTanks)
                player.Update(gameTime, keyboardState);

            foreach (var falcon in Falcons)
                falcon.Update(gameTime, keyboardState);

            BotManager.Update(gameTime, keyboardState);

            foreach (var shell in Shells)
                shell.Update(gameTime, keyboardState);
            Shells.RemoveAll(s => s.ToRemove);

            foreach (var explosion in _explosions)
                explosion.Update(gameTime, keyboardState);
            _explosions.RemoveAll(e => e.ToRemove);

            _tiles.RemoveAll(t => t.ToRemove);
            foreach (var tile in _tiles)
                tile.Update(gameTime, keyboardState);

            foreach (var playerSpawner in _playerSpawners.Values)
                playerSpawner.Update(gameTime);

            BonusManager.Update(gameTime, keyboardState);
        }

        SoundPlayer.Perform(gameTime);
    }

    private void HandleInput()
    {
        if (KeyboardEx.HasBeenPressed(Keys.Space))
            IsGamePaused = !IsGamePaused;
    }

    public bool IsTileFree(Point tilePoint)
    {
        var tileObjects = _tileObjectMap.ElementAtOrDefault(tilePoint.X, tilePoint.Y);
        return tileObjects?.All(o => !o.CollisionType.HasFlag(CollisionType.Impassable)) == true;
    }

    public void HandleFalconDestroyed(Falcon falcon)
    {
        if (!Falcons.Any(f => f.IsAlive))
            GameOver();
    }

    private void GameOver()
    {

    }

    public void HandleAllBotsDestroyed()
    {
    }
}