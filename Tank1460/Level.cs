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
    public int PlayerLivesRemaining(int playerNumber) => _playerSpawners[playerNumber].LivesRemaining;

    public bool IsPlayerInGame(int playerNumber) => _playersInGame.Contains(playerNumber);

    private readonly int[] _playersInGame = { 1, 2 };
    
    public int BotSpawnsRemaining => BotManager?.SpawnsRemaining ?? 0;

    public bool BotsCanGrabBonuses = true;

    public int PlayerCount => _playerSpawners.Count;

    internal LevelStructure Structure { get; }

    private readonly List<Tile> _tiles = new();
    //private Texture2D[] layers;

    private List<LevelObject>[,] _tileObjectMap;

    private readonly List<PlayerTank> _players = new();

    private readonly Dictionary<int, PlayerSpawner> _playerSpawners = new();

    internal ISoundPlayer SoundPlayer;

    public PlayerSpawner GetPlayerSpawner(int playerNumber) => _playerSpawners[playerNumber];

    private const int MaxPlayerCount = 2;

    public Falcon Falcon { get; private set; }

    private readonly List<Explosion> _explosions = new();

    public BotManager BotManager { get; }
    public BonusManager BonusManager { get; }

    public List<Shell> Shells = new();

    public ContentManagerEx Content { get; }

    private const int TotalBots = 20;
    private const int MaxAliveBots = 4;

    private const int MaxBonusesOnScreen = 1;

    public int LevelNumber { get; }

    public Rectangle TileBounds;
    public Rectangle Bounds;

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
        BotManager = new BotManager(this, TotalBots, MaxAliveBots);
        BonusManager = new BonusManager(this, MaxBonusesOnScreen);
        LoadTiles(levelStructure.Tiles);

        SoundPlayer.Play(Sound.Intro);
    }

    public void AddExplosion(Explosion explosion) => _explosions.Add(explosion);

    public void AddPlayer(PlayerTank playerTank) => _players.Add(playerTank);

    private void RemovePlayer(PlayerTank playerTank)
    {
        _players.Remove(playerTank);
        var playerSpawner = _playerSpawners[playerTank.PlayerNumber];

        if (playerSpawner is null)
        {
            Debug.Fail($"Cannot find player spawner for player {playerTank.PlayerNumber}.");
            return;
        }

        playerSpawner.HandlePlayerDeath(playerTank);
    }

    public PlayerTank GetTargetPlayerForBot(int botIndex)
    {
        return _players.Count switch
        {
            0 => null,
            1 => botIndex % 2 == 0 ? null : _players[0],
            _ => _players[botIndex % _players.Count]
        };
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
                var tile = CreateTile(tileTypes[x, y], x, y);
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

        if (Falcon == null)
            throw new NotSupportedException("A level must have the falcon.");

        BotManager.ForceSpawn();
    }

    private Tile CreateTile(TileType tileType, int x, int y)
    {
        return tileType switch
        {
            // Blank space
            TileType.Empty => null,

            // Brick
            TileType.Brick => new BrickTile(this),

            // Concrete
            TileType.Concrete => new ConcreteTile(this),

            // Water
            TileType.Water => new WaterTile(this),

            // Forest
            TileType.Forest => new ForestTile(this),

            // Ice
            TileType.Ice => new IceTile(this),

            // Player 1 start point
            TileType.Player1Spawn => CreatePlayerSpawn(x, y, 1),

            // Player 2 start point
            TileType.Player2Spawn => CreatePlayerSpawn(x, y, 2),

            // Bot spawn point
            TileType.BotSpawn => CreateBotSpawn(x, y),

            // Falcon
            TileType.Falcon => CreateFalcon(x, y),

            // Unknown tile type character
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
        if(_playerSpawners.ContainsKey(playerNumber))
            throw new NotSupportedException($"A level may only have one player {playerNumber} spawn.");

        var playerSpawner = new PlayerSpawner(this, x, y, playerNumber);
        _playerSpawners.Add(playerNumber, playerSpawner);

        if (!IsPlayerInGame(playerNumber))
            playerSpawner.Disable();

        return null;
    }

    private Tile CreateFalcon(int x, int y)
    {
        if (Falcon != null)
            throw new NotSupportedException("A level may only have one falcon.");

        Falcon = new Falcon(this);
        Falcon.Spawn(new Point(x * Tile.DefaultWidth, y * Tile.DefaultHeight));

        return null;
    }

    /// <summary>
    /// Unloads the level content.
    /// </summary>
    public void Dispose()
    {
        Content.Unload();
    }

    /// <summary>
    /// Draw everything in the level from background to foreground.
    /// </summary>
    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        spriteBatch.FillRectangle(Bounds, Color.Black);

        //for (int i = 0; i <= EntityLayer; ++i)
        //    spriteBatch.Draw(layers[i], Vector2.Zero, Color.White);

        // TODO: перейти на слои
        foreach (var tile in _tiles.Where(tile => tile.TileView == TileView.Default))
            tile.Draw(gameTime, spriteBatch);

        foreach (var player in _players)
            player.Draw(gameTime, spriteBatch);

        Falcon.Draw(gameTime, spriteBatch);

        foreach (var explosion in _explosions)
        {
            explosion.Draw(gameTime, spriteBatch);
        }

        foreach (var shell in Shells)
        {
            shell.Draw(gameTime, spriteBatch);
        }

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

    private IReadOnlyCollection<(LevelObject levelObject, Vector2 depth)> GetAllCollisions(LevelObject levelObject,
        IReadOnlyCollection<LevelObject> excludedObjects = null)
    {
        var allCollisions = new List<(LevelObject levelObject, Vector2 depth)>();

        var outOfBoundsCollisionDepth = DetectOutOfBoundsCollision(levelObject.BoundingRectangle, levelObject.TileRectangle);

        if (outOfBoundsCollisionDepth != Vector2.Zero)
        {
            allCollisions.Add((null, outOfBoundsCollisionDepth));
        }
        else
        {
            var tilesObjects = new HashSet<LevelObject>();

            levelObject.TileRectangle.EnumerateArray(_tileObjectMap, tilesObjects.UnionWith);
            tilesObjects.Remove(levelObject);
            if (excludedObjects is not null)
                tilesObjects.RemoveWhere(excludedObjects.Contains);

            foreach (var collidingObject in tilesObjects)
            {
                var depth = levelObject.BoundingRectangle.GetIntersectionDepth(
                    collidingObject.BoundingRectangle);
                if (depth != Vector2.Zero)
                    allCollisions.Add((collidingObject, depth));
            }
        }

        return allCollisions;
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

    private Vector2 DetectOutOfBoundsCollision(Rectangle rect, Rectangle tileRect)
    {
        int? outOfBoundsXTile = null;
        int? outOfBoundsYTile = null;

        if (tileRect.Top < TileBounds.Top)
            outOfBoundsYTile = tileRect.Top;

        if (tileRect.Bottom > TileBounds.Bottom)
            outOfBoundsYTile = tileRect.Bottom;

        if (tileRect.Left < TileBounds.Left)
            outOfBoundsXTile = tileRect.Left;

        if (tileRect.Right > TileBounds.Right)
            outOfBoundsXTile = tileRect.Right;

        if (outOfBoundsXTile.HasValue || outOfBoundsYTile.HasValue)
        {
            outOfBoundsXTile ??= tileRect.Left;
            outOfBoundsYTile ??= tileRect.Top;
            var outOfBoundsTile = GetTileBounds(outOfBoundsXTile.Value, outOfBoundsYTile.Value);
            return rect.GetIntersectionDepth(outOfBoundsTile);
        }

        return Vector2.Zero;
    }

    public void Update(GameTime gameTime, KeyboardState keyboardState)
    {
        HandleInput();

        if (!IsGamePaused)
        {
            _players.FindAll(p => p.ToRemove).ForEach(RemovePlayer);
            foreach (var player in _players)
                player.Update(gameTime, keyboardState);

            Falcon.Update(gameTime, keyboardState);

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

        //Debug.WriteLine($"Point: {tilePoint} Objects: {tileObjects?.Count ?? -1}");

        return tileObjects?.All(o => !o.CollisionType.HasFlag(CollisionType.Impassable)) == true;
    }

    public void HandleFalconDestroyed(Falcon falcon)
    {
        GameOver();
    }

    private void GameOver()
    {

    }

    public void HandleAllBotsDestroyed()
    {
    }
}