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
using Tank1460.Input;
using Tank1460.LevelObjects;
using Tank1460.LevelObjects.Bonuses;
using Tank1460.LevelObjects.Explosions;
using Tank1460.LevelObjects.Tanks;
using Tank1460.LevelObjects.Tiles;

namespace Tank1460;

public class Level : IDisposable
{
    // TODO: Паблик Морозов во все поля

    public int PlayerLivesRemaining(PlayerIndex playerIndex) => PlayerSpawners[playerIndex].LivesRemaining;

    public int BotSpawnsRemaining => BotManager?.SpawnsRemaining ?? 0;

    public bool BotsCanGrabBonuses { get; } = true;

    public int PlayerCount => PlayerSpawners.Count;

    public PlayerSpawner GetPlayerSpawner(PlayerIndex playerIndex) => PlayerSpawners[playerIndex];

    public BotManager BotManager { get; }

    public BonusManager BonusManager { get; }

    public List<Shell> Shells { get; } = new();

    public ContentManagerEx Content { get; }

    public int LevelNumber { get; }

    public Rectangle TileBounds { get; private set; }

    public Rectangle Bounds { get; private set; }

    public bool IsLoaded => State != LevelState.Loading;

    public IReadOnlyList<PlayerTank> PlayerTanks => _playerTanks;

    public List<Falcon> Falcons { get; } = new();

    internal PlayerIndex[] PlayersInGame { get; }

    internal ISoundPlayer SoundPlayer { get; }

    internal LevelStructure Structure { get; }

    private readonly List<Tile> _tiles = new();
    //private Texture2D[] layers;
    private List<LevelObject>[,] _tileObjectMap;

    // TODO: возможно, одженерить.
    private readonly List<LevelEffect> _levelEffects = new();

    // В оригинале именно так: зависит лишь от режима,
    // а не от того, жив ли второй игрок. Даже если уровень стартует, когда один уже без жизней, всё равно будет шесть.
    private int MaxAliveBots() => (PlayersInGame.Length + 1) * 2;

    private readonly List<PlayerTank> _playerTanks = new();

    private Dictionary<PlayerIndex, PlayerSpawner> PlayerSpawners { get; } = new();
    private const int MaxPlayerCount = 2;
    private readonly List<Explosion> _explosions = new();
    private const int TotalBots = 20;
    private const int MaxBonusesOnScreen = 1;
#if DEBUG
    private bool _cheatGodMode;
#endif

    private LevelState _state = LevelState.Loading;
    internal LevelState State
    {
        get => _state;
        private set
        {
            if (value == LevelState.Paused && _state != LevelState.Paused)
            {
                SoundPlayer.PauseAndPushState();
                SoundPlayer.Play(Sound.Pause);
            }
            else if (value != LevelState.Paused && _state == LevelState.Paused)
                SoundPlayer.ResumeAndPopState();

            _state = value;
        }
    }

    public Level(IServiceProvider serviceProvider, LevelStructure levelStructure, int levelNumber, PlayerIndex[] playersInGame)
    {
        Structure = levelStructure;
        LevelNumber = levelNumber;
        PlayersInGame = playersInGame;

        Content = new ContentManagerEx(serviceProvider, "Content");

        SoundPlayer = new SoundPlayer(Content);
        BotManager = new BotManager(this, TotalBots, MaxAliveBots());
        BonusManager = new BonusManager(this, MaxBonusesOnScreen);
        LoadTiles(levelStructure.Tiles);

        SoundPlayer.Play(Sound.Intro);
        State = LevelState.Intro;
    }

    public void AddExplosion(Explosion explosion) => _explosions.Add(explosion);

    public void AddPlayer(PlayerTank playerTank) => _playerTanks.Add(playerTank);

    public PlayerTank GetTargetPlayerForBot(int botIndex)
    {
        return _playerTanks.Count switch
        {
            0 => null,
            1 => botIndex % 2 == 0 ? null : _playerTanks[0],
            _ => _playerTanks[botIndex % _playerTanks.Count]
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
        // TODO: Вообще говоря, не всегда правда.
        if (State is not LevelState.Running and not LevelState.Paused)
            return;

        foreach (var playerTank in _playerTanks)
            playerTank.HandleInput(playersInputs[playerTank.PlayerIndex]);

        if (KeyboardEx.HasBeenPressed(Keys.Space) || KeyboardEx.HasBeenPressed(Keys.X))
        {
            State = State == LevelState.Running ? LevelState.Paused : LevelState.Running;
        }

#if DEBUG
        if (KeyboardEx.HasBeenPressed(Keys.F10))
        {
            _cheatGodMode = !_cheatGodMode;
            _playerTanks.ForEach(tank => tank.GodMode = _cheatGodMode);
        }

        if (KeyboardEx.HasBeenPressed(Keys.PageUp))
            _playerTanks.ForEach(tank => tank.UpgradeUp());

        if (KeyboardEx.HasBeenPressed(Keys.PageDown))
            _playerTanks.ForEach(tank => tank.UpgradeDown());

        if (KeyboardEx.HasBeenPressed(Keys.Enter))
            _playerTanks.ForEach(tank => tank.Explode(tank));
#endif
    }

    public void Update(GameTime gameTime)
    {
        if (State is not LevelState.Loading and not LevelState.Intro and not LevelState.Paused)
        {
            _playerTanks.FindAll(p => p.ToRemove).ForEach(HandlePlayerTankDestroyed);
            foreach (var player in _playerTanks)
                player.Update(gameTime);

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

            foreach (var playerSpawner in PlayerSpawners.Values)
                playerSpawner.Update(gameTime);

            BonusManager.Update(gameTime);

            _levelEffects.RemoveAll(effect => effect.ToRemove);
            _levelEffects.ForEach(effect => effect.Update(gameTime));
        }

        SoundPlayer.Perform(gameTime);
    }

    public bool CanTankPassThroughTile(Tank tank, Point tilePoint)
    {
        var tileObjects = _tileObjectMap.ElementAtOrDefault(tilePoint.X, tilePoint.Y);
        return tileObjects?.All(o => !o.CollisionType.HasFlag(CollisionType.Impassable) && (!o.CollisionType.HasFlag(CollisionType.PassablyOnlyByShip) || tank.HasShip)) == true;
    }

    public void HandleFalconDestroyed(Falcon falcon)
    {
        if (!Falcons.Any(f => f.IsAlive))
            AllFalconsDestroyed();
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

    public void HandleAllBotsDestroyed()
    {
    }

    public void Start()
    {
        Debug.Assert(State == LevelState.Intro);
        State = LevelState.Running;
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
        var closeObjects = new HashSet<LevelObject>();
        levelObject.TileRectangle.EnumerateArray(_tileObjectMap, closeObjects.UnionWith);

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
    }

    internal Tile GetTile(int x, int y)
    {
        return (Tile)_tileObjectMap[x, y].SingleOrDefault(o => o is Tile);
    }

    internal void TryRemoveTileAt(int x, int y)
    {
        GetTile(x, y)?.Remove();
    }

    internal void AddExclusiveEffect(LevelEffect levelEffect)
    {
        _levelEffects.Where(e => e.GetType().IsInstanceOfType(levelEffect))
                     .ForEach(e => e.Remove());

        _levelEffects.Add(levelEffect);
    }

    internal void RemoveAllEffects<T>() where T : LevelEffect
    {
        _levelEffects.RemoveAll(e => e is T);
    }

    internal void RewardPlayerWithPoints(PlayerIndex playerIndex, int points)
    {
        PlayerRewarded?.Invoke(this, (playerIndex, points));
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

                AddTile(tile, x, y);
            }
        }

        TileBounds = new Rectangle(0, 0, width, height);
        Bounds = TileBounds.Multiply(new Point(Tile.DefaultWidth, Tile.DefaultHeight));

        if (PlayerSpawners.Count is < 1 or > MaxPlayerCount)
            throw new NotSupportedException($"A level must have 1 to {MaxPlayerCount} starting point(s).");

        if (Falcons.Count == 0)
            throw new NotSupportedException("A level must have at least one falcon.");
    }

    private void HandlePlayerTankDestroyed(PlayerTank playerTank)
    {
        _playerTanks.Remove(playerTank);
        var playerSpawner = PlayerSpawners[playerTank.PlayerIndex];

        if (playerSpawner is null)
        {
            Debug.Fail($"Cannot find player spawner for player {playerTank.PlayerIndex}.");
            return;
        }

        playerSpawner.HandleTankDestroyed(playerTank);
    }

    private Tile LoadTile(TileType tileType, int x, int y)
    {
        // TODO: Убрать объекты из типов тайлов и заменить на фабричный метод.
        return tileType switch
        {
            TileType.Empty => null,

            TileType.Brick => new BrickTile(this),

            TileType.Concrete => new ConcreteTile(this),

            TileType.Water => new WaterTile(this),

            TileType.Forest => new ForestTile(this),

            TileType.Ice => new IceTile(this),

            TileType.Player1Spawn => CreatePlayerSpawn(x, y, PlayerIndex.One),

            TileType.Player2Spawn => CreatePlayerSpawn(x, y, PlayerIndex.Two),

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

    private Tile CreatePlayerSpawn(int x, int y, PlayerIndex playerIndex)
    {
        if (PlayerSpawners.ContainsKey(playerIndex))
            throw new NotSupportedException($"A level may only have one player {playerIndex} spawn.");

        var playerSpawner = new PlayerSpawner(this, x, y, playerIndex);
        PlayerSpawners.Add(playerIndex, playerSpawner);

        if (!PlayersInGame.Contains(playerIndex))
            playerSpawner.Disable();

        return null;
    }

    private void AllFalconsDestroyed()
    {
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

    public delegate void LevelEvent(Level level);
    public delegate void LevelEvent<T>(Level level, T args);

    public event LevelEvent GameOver;
    public event LevelEvent<(PlayerIndex PlayerIndex, int PointsReward)> PlayerRewarded;
}