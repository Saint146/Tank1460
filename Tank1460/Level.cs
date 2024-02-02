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

    public bool IsLoaded => Status != LevelStatus.Loading;

    public PlayerTank GetPlayerTank(PlayerIndex playerIndex) => PlayerSpawners[playerIndex]?.Tank;

    public IEnumerable<PlayerTank> GetAllPlayerTanks() => PlayersInGame.Select(GetPlayerTank).Where(tank => tank is not null);

    public List<Falcon> Falcons { get; } = new();

    internal PlayerIndex[] PlayersInGame { get; }

    internal ISoundPlayer SoundPlayer { get; }

    internal LevelStructure Structure { get; }

    internal bool ClassicRules => false;

    private readonly List<Tile> _tiles = new();
    //private Texture2D[] layers;
    private List<LevelObject>[,] _tileObjectMap;

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

    private LevelStatus _status = LevelStatus.Loading;
    private double _delayTime;
    private double _delayEffectTime;

    internal LevelStatus Status
    {
        get => _status;
        private set
        {
            if (value == LevelStatus.Paused && _status != LevelStatus.Paused)
            {
                SoundPlayer.PauseAndPushState();
                SoundPlayer.Play(Sound.Pause);
            }
            else if (value != LevelStatus.Paused && _status == LevelStatus.Paused)
                SoundPlayer.ResumeAndPopState();

            _status = value;
        }
    }

    public Level(IServiceProvider serviceProvider, LevelStructure levelStructure, int levelNumber, GameState startingGameState)
    {
        Structure = levelStructure;
        LevelNumber = levelNumber;
        PlayersInGame = startingGameState.PlayersStates.Keys.ToArray();

        Content = new ContentManagerEx(serviceProvider, "Content");

        SoundPlayer = new SoundPlayer(Content);
        BotManager = new BotManager(this, TotalBots, MaxAliveBots());
        BonusManager = new BonusManager(this, MaxBonusesOnScreen);
        LoadTiles(levelStructure.Tiles);

        // Load starting game state.
        foreach (var (playerIndex, playerState) in startingGameState.PlayersStates)
        {
            PlayersScore[playerIndex] = playerState.Score;
            PlayerSpawners[playerIndex].LivesRemaining = playerState.LivesRemaining;
            PlayerSpawners[playerIndex].SetNextSpawnSettings(playerState.TankType, playerState.TankHasShip);
        }

        SoundPlayer.Play(Sound.Intro);
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
            case LevelStatus.WinScoreScreen:
            case LevelStatus.LostScoreScreen:
            case LevelStatus.GameOverScreen:
            case LevelStatus.Win:
            case LevelStatus.Lost:
                playersInputs.ClearInputs();
                break;

            case LevelStatus.Running:
            case LevelStatus.Paused:
            case LevelStatus.WinDelay:
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }

        foreach (var playerIndex in PlayersInGame)
        {
            var tank = GetPlayerTank(playerIndex);
            if (tank is null)
            {
                if (PlayerLivesRemaining(playerIndex) == 0)
                {
                    // Обрабатываем нажатия клавиш игрока без жизней.
                    if(playersInputs[playerIndex].Pressed.HasFlag(PlayerInputCommands.Shoot))
                        TryReceiveDonatedLife(playerIndex);
                }
            }
            else
            {
                var playerInput = playersInputs[playerIndex];

                if (playerInput.Pressed.HasFlag(PlayerInputCommands.Start))
                    Status = Status == LevelStatus.Running ? LevelStatus.Paused : LevelStatus.Running;
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

        if (KeyboardEx.HasBeenPressed(Keys.Enter))
            GetAllPlayerTanks().ForEach(tank => tank.Explode(tank));
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

            _tiles.RemoveAll(t => t.ToRemove);
            foreach (var tile in _tiles)
                tile.Update(gameTime);

            foreach (var playerSpawner in PlayerSpawners.Values)
                playerSpawner.Update(gameTime);

            BonusManager.Update(gameTime);

            _levelEffects.Update(gameTime);
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
            StartGameOverSequence();
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

        foreach (var playerTank in GetAllPlayerTanks())
            playerTank.Draw(gameTime, spriteBatch);

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
        _levelEffects.Draw(spriteBatch, Bounds.Location.ToVector2());
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
        Status = LevelStatus.WinDelay;
        _delayTime = 0.0;
        _delayEffectTime = 300 * Tank1460Game.OneFrameSpan;
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

    internal void RewardPlayerWithPoints(PlayerIndex playerIndex, int points)
    {
        Debug.Assert(points > 0);

        // TODO: Проверить логику оригинала.
        const int pointsForOneUp = 20000;

        var nearestOneUpPoints = (PlayersScore[playerIndex] + 1).CeilingByBase(pointsForOneUp);
        var newPoints = PlayersScore[playerIndex] += points;

        while (nearestOneUpPoints <= newPoints)
        {
            PlayerSpawners[playerIndex].AddOneUp();
            nearestOneUpPoints = (nearestOneUpPoints + 1).CeilingByBase(pointsForOneUp);
        }
    }

    internal void HandlePlayerLostAllLives(PlayerIndex playerIndex)
    {
        if (PlayersInGame.All(player => PlayerLivesRemaining(player) == 0))
            StartGameOverSequence();
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
            case LevelStatus.Lost:
            case LevelStatus.Win:
                return false;

            case LevelStatus.Running:
                return true;

            case LevelStatus.WinDelay:
                _delayTime += gameTime.ElapsedGameTime.TotalSeconds;
                if (_delayTime <= _delayEffectTime)
                    return true;

                Status = LevelStatus.Win;
                LevelComplete?.Invoke(this);

                return true;

            case LevelStatus.LostDelay:
                _delayTime += gameTime.ElapsedGameTime.TotalSeconds;
                if (_delayTime <= _delayEffectTime)
                    return true;

                Status = LevelStatus.Lost;
                SoundPlayer.Unmute();
                GameOver?.Invoke(this);

                return true;

            case LevelStatus.WinScoreScreen:
            case LevelStatus.LostScoreScreen:
            case LevelStatus.GameOverScreen:
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
                var tile = LoadTile(tileTypes[x, y], x, y);
                if (tile is null)
                    continue;

                AddTile(tile, x, y);
            }
        }

        TileBounds = new Rectangle(0, 0, width, height);
        Bounds = TileBounds.Multiply(new Point(Tile.DefaultWidth, Tile.DefaultHeight));

        // TODO: Почему-то срабатывает когда не должно.
        //var playersWithoutSpawners = PlayersInGame.Where(playerIndex => !PlayerSpawners.ContainsKey(playerIndex)).ToList();
        //if (PlayerSpawners.Count != 0)
        //    throw new NotSupportedException($"No player spawner for {string.Join(", ", playersWithoutSpawners)} found in the level.");

        if (Falcons.Count == 0)
            throw new NotSupportedException("A level must have at least one falcon.");
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

    private void TryReceiveDonatedLife(PlayerIndex playerIndex)
    {
        if (!PlayerSpawners.Values.TryGetFirst(out var spawnerDonator, spawner => spawner.CanDonateLife()))
            return;

        spawnerDonator.DonateLife(PlayerSpawners[playerIndex]);
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
        if (!PlayersInGame.Contains(playerIndex))
            return null;

        if (PlayerSpawners.ContainsKey(playerIndex))
            throw new NotSupportedException($"A level may only have one player {playerIndex} spawn.");

        var playerSpawner = new PlayerSpawner(this, x, y, playerIndex);
        PlayerSpawners.Add(playerIndex, playerSpawner);

        return null;
    }

    private void StartGameOverSequence()
    {
        Status = LevelStatus.LostDelay;
        SoundPlayer.Mute();
        _delayTime = 0.0;
        _delayEffectTime = 288 * Tank1460Game.OneFrameSpan;
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
    public delegate void LevelEvent<in T>(Level level, T args);

    public event LevelEvent GameOver;
    public event LevelEvent LevelComplete;
}