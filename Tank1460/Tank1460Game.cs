using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Tank1460.Extensions;
using Tank1460.LevelObjects.Explosions;
using Tank1460.LevelObjects.Tiles;
using Tank1460.Input;
using Tank1460.SaveLoad;
using Tank1460.SaveLoad.Settings;

namespace Tank1460;

public class Tank1460Game : Game
{
    private const int Fps = 60;

    /// <summary>
    /// Длительность одного кадра в секундах.
    /// TODO: Убрать необходимость всем указывать время или скорость с использованием этой константы. Лучше, чтобы всё было в кадрах и пикселях на кадр.
    /// </summary>
    public const double OneFrameSpan = 1.0 / Fps;

#if DEBUG
    public static bool ShowObjectsBoundaries;
    public static bool ShowBotsPeriods = true;
#endif

    internal GameState State { get; private set; }

    private Rectangle GetLevelBounds() =>
        _level?.Bounds ?? new Rectangle(0, 0, 26 * Tile.DefaultWidth, 26 * Tile.DefaultHeight);

    private static Point PreLevelIndent { get; } = new(2 * Tile.DefaultWidth, Tile.DefaultHeight);
    private static Point PostLevelIndent { get; } = new(Tile.DefaultWidth, Tile.DefaultHeight);
    private static Point PostHudIndent { get; } = new(Tile.DefaultWidth, 0);

    private Point BaseScreenSize =>
        // Отступ слева и сверху
        PreLevelIndent +
        // Сам уровень
        GetLevelBounds().Size +
        // Отступ справа и снизу
        PostLevelIndent +
        // Ширина худа
        new Point(LevelHud.HudWidth, 0) +
        // Отступ после худа
        PostHudIndent;

    private static Color LevelBackColor { get; } = new(0x7f, 0x7f, 0x7f);

    private static Color CurtainColor { get; }= LevelBackColor;

    private static Color GameBackColor { get; } = Color.Black;

    private bool _customCursorEnabled;

    internal bool CustomCursorEnabled
    {
        get => _customCursorEnabled;
        private set
        {
            _customCursorEnabled = value;
            IsMouseVisible = !value;
        }
    }

    private bool _isScalingPixelPerfect = true;

    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private readonly SaveLoadManager _saveLoadManager = new();
    private Level _level;
    private Menu _menu;
    private LevelHud _levelHud;
    private Cursor _cursor;

    private readonly Matrix _levelTransformation = Matrix.CreateTranslation(PostLevelIndent.X, PostLevelIndent.Y, 0);
    private Matrix _globalTransformation;
    private float _scale = 1.0f;
    private const int DefaultScale = 3;

    private int _backbufferWidth, _backbufferHeight;
    private bool _isCustomCursorVisible;

    private const int OpenedCurtainPosition = 0;
    private const int ClosedCurtainPosition = 30;
    private int _curtainPosition = OpenedCurtainPosition;
    private double _curtainTime;
    private const double _curtainTickTime = OneFrameSpan;

    private readonly PlayerInputHandler _playerInputHandler;
    private MouseState _mouseState;
    private KeyboardState _keyboardState;
    private Dictionary<int, GamePadState> _gamePadStates;

    private Dictionary<PlayerIndex, int> _playersPoints;
    private Point _windowPosition;
    private Point _windowSize;

    private PlayerIndex[] AllPlayers { get; } = { PlayerIndex.One, PlayerIndex.Two };

    private PlayerIndex[] PlayersInGame { get; set; } = { PlayerIndex.One };

    private int LevelNumber { get; set; } = 1;

    public Tank1460Game()
    {
        Window.Title = "Tank 1460";
        Window.AllowUserResizing = true;
        Content.RootDirectory = "Content";
        IsFixedTimeStep = true;

        _graphics = new GraphicsDeviceManager(this);

        _playerInputHandler = new PlayerInputHandler(AllPlayers);

        ResetPlayersPoints();

        State = GameState.Initializing;

#pragma warning disable CS0162
        if (Fps != 60)
        {
            _graphics.SynchronizeWithVerticalRetrace = false;
            //IsFixedTimeStep = false; // Вроде бы и без этого работает
            TargetElapsedTime = TimeSpan.FromMilliseconds(1000.0 / Fps);
        }
#pragma warning restore CS0162
    }

    protected override void Initialize()
    {
        base.Initialize();
        LoadSettings();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _levelHud = new LevelHud(Content);
        _cursor = new Cursor(Content);

        State = GameState.Ready;
    }

    protected override void Update(GameTime gameTime)
    {
        // Если размер экрана изменился.
        if (_backbufferHeight != GraphicsDevice.PresentationParameters.BackBufferHeight ||
            _backbufferWidth != GraphicsDevice.PresentationParameters.BackBufferWidth)
            ScalePresentationArea();

        var inputs = HandleInput();

        if (_isCustomCursorVisible)
            _cursor.Update(gameTime, _mouseState, _scale);

        ProcessState(gameTime);

        _menu?.HandleInput(inputs);
        _menu?.Update(gameTime);

        _level?.HandleInput(inputs);
        _level?.Update(gameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(_level is null ? GameBackColor : LevelBackColor);

        _spriteBatch.Begin(SpriteSortMode.Immediate, null, SamplerState.PointClamp, null, null, null, _globalTransformation);

        if (_level is not null)
        {
            _level.Draw(gameTime, _spriteBatch);

            var hudPosition = /*PreLevelIndent +*/new Point(GetLevelBounds().Width + PostLevelIndent.X - 1, 0); // ну вот так в оригинале, на один пиксель сдвинуто левее сетки
            _levelHud.Draw(_level, _spriteBatch, hudPosition);
        }

        _menu?.Draw(gameTime, _spriteBatch);

        DrawCurtain(_spriteBatch);

        _spriteBatch.End();

        if (_isCustomCursorVisible)
        {
            _spriteBatch.Begin(SpriteSortMode.Immediate, null, SamplerState.PointClamp);
            _cursor.Draw(gameTime, _spriteBatch);
            _spriteBatch.End();
        }

        base.Draw(gameTime);
    }

    protected override void OnExiting(object sender, EventArgs args)
    {
        try
        {
            SaveSettings();
        }
        catch (Exception ex)
        {
            Debug.Fail(ex.ToString());
        }
        finally
        {
            base.OnExiting(sender, args);
        }
    }

    protected override void UnloadContent()
    {
        UnloadLevel();
        UnloadMenu();
        base.UnloadContent();
    }

    private void ProcessState(GameTime gameTime)
    {
        switch (State)
        {
            case GameState.Initializing:
                // Ждём загрузки. По идее, всё однопоточно, конечно, но LoadContent вызывается базовым классом, и мы не знаем, когда это произойдет.
                break;

            case GameState.Ready:
                State = GameState.InMenu;
                UnloadLevel();
                LoadMenu();
                break;

            case GameState.InMenu:
            case GameState.InLevel:
                break;

            case GameState.CurtainOpening:
                ProcessCurtain(gameTime, -1);
                if (_curtainPosition >= OpenedCurtainPosition)
                    break;

                State = GameState.InLevel;
                _level.Start();
                break;

            case GameState.CurtainClosing:
                ProcessCurtain(gameTime, 1);
                if (_curtainPosition <= ClosedCurtainPosition)
                    break;

                UnloadMenu();
                LoadLevel(LevelNumber);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(State));
        }
    }

    private void ProcessCurtain(GameTime gameTime, int step)
    {
        _curtainTime += gameTime.ElapsedGameTime.TotalSeconds;

        while (_curtainTime > _curtainTickTime)
        {
            _curtainTime -= _curtainTime;
            _curtainPosition += step;
        }
    }

    private void ResetPlayersPoints()
    {
        _playersPoints = PlayersInGame.ToDictionary(playerIndex => playerIndex, _ => 0);
    }

    private PlayerInputCollection HandleInput()
    {
        _keyboardState = KeyboardEx.GetState();
        _mouseState = Mouse.GetState();
        _gamePadStates = _playerInputHandler.GetActiveGamePadIndexes().ToDictionary(index => index, GamePad.GetState);

        var inputs = _playerInputHandler.HandleInput(_keyboardState, _gamePadStates);

        if (KeyboardEx.HasBeenPressed(Keys.F11))
        {
            _graphics.IsFullScreen = !_graphics.IsFullScreen;
            _graphics.ApplyChanges();
        }

#if DEBUG
        if (KeyboardEx.HasBeenPressed(Keys.F5))
            ShowObjectsBoundaries = !ShowObjectsBoundaries;

        if (KeyboardEx.HasBeenPressed(Keys.F6))
            ShowBotsPeriods = !ShowBotsPeriods;

        if (KeyboardEx.HasBeenPressed(Keys.F7))
            CustomCursorEnabled = !CustomCursorEnabled;

        if (KeyboardEx.HasBeenPressed(Keys.F8))
        {
            _isScalingPixelPerfect = !_isScalingPixelPerfect;
            ScalePresentationArea();
        }

        if (_keyboardState.IsKeyDown(Keys.LeftShift))
        {
            foreach (var digit in Enumerable.Range(0, 10))
            {
                var key = Keys.D0 + digit;

                if (KeyboardEx.HasBeenPressed(key))
                {
                    LoadLevel(digit);
                    break;
                }
            }
        }

        if (_keyboardState.IsKeyDown(Keys.J))
        {
            var x = Rng.Next(_level.TileBounds.Left, _level.TileBounds.Right);
            var y = Rng.Next(_level.TileBounds.Top, _level.TileBounds.Bottom);

            var explosion = new CommonExplosion(_level);
            explosion.Spawn(Level.GetTileBounds(x, y).Location);
        }
#endif

        if (_customCursorEnabled)
            // Курсор не обновляет свое состояние, если он отключен или вне экрана.
            _isCustomCursorVisible = _mouseState.X >= 0 && _mouseState.Y >= 0 && _mouseState.X < _backbufferWidth && _mouseState.Y < _backbufferHeight;
        else
            _isCustomCursorVisible = false;

        return inputs;
    }

    private void UnloadMenu()
    {
        if (_menu is null)
            return;

        _menu.MenuExited -= Menu_MenuExited;
        _menu.Dispose();
        _menu = null;
    }

    private void LoadMenu()
    {
        _menu = new Menu(Services, 1, 1);
        _menu.MenuExited += Menu_MenuExited;
    }

    private void Menu_MenuExited()
    {
        LevelNumber = _menu.LevelNumber;
        PlayersInGame = AllPlayers.Take(_menu.PlayerCount).ToArray();

        _curtainTime = 0.0;
        State = GameState.CurtainClosing;
    }

    private void UnloadLevel()
    {
        if (_level is null)
            return;

        _level.PlayerRewarded -= Level_PlayerRewarded;
        _level.LevelComplete -= Level_LevelComplete;
        _level.GameOver -= Level_GameOver;

        _level.Dispose();
        _level = null;
    }

    private void LoadLevel(int levelNumber)
    {
        Debug.WriteLine($"Loading level {levelNumber}...");

        UnloadLevel();

        LevelNumber = levelNumber;
        var levelStructure = new LevelStructure($"Content/Levels/{levelNumber}.lvl");
        _level = new Level(Services, levelStructure, levelNumber, PlayersInGame);

        _level.PlayerRewarded += Level_PlayerRewarded;
        _level.LevelComplete += Level_LevelComplete;
        _level.GameOver += Level_GameOver;

        ScalePresentationArea();

        // TODO: Вынести в сеттер State
        State = GameState.CurtainOpening;
        _curtainPosition = ClosedCurtainPosition;
        _curtainTime = 0.0;

        Debug.WriteLine($"Level {levelNumber} loaded.");
    }

    private void Level_LevelComplete(Level level)
    {
        LevelNumber++;

        _curtainTime = 0.0;
        State = GameState.CurtainClosing;
    }

    private void Level_GameOver(Level level)
    {
        State = GameState.Ready;
        ResetPlayersPoints();
    }

    private void Level_PlayerRewarded(Level level, (PlayerIndex PlayerIndex, int PointsReward) args)
    {
        // TODO: Проверить логику оригинала.
        const int pointsForOneUp = 20000;

        var nearestOneUpPoints = (_playersPoints[args.PlayerIndex] + 1).CeilingByBase(pointsForOneUp);
        var newPoints = _playersPoints[args.PlayerIndex] += args.PointsReward;

        while (nearestOneUpPoints <= newPoints)
        {
            level.GetPlayerSpawner(args.PlayerIndex).AddOneUp();
            nearestOneUpPoints = (nearestOneUpPoints + 1).CeilingByBase(pointsForOneUp);
        }
    }

    private void DrawCurtain(SpriteBatch spriteBatch)
    {
        if (State != GameState.CurtainOpening && State != GameState.CurtainClosing)
            return;

        var screenHeight = BaseScreenSize.Y;

        var curtainHeight = screenHeight * (_curtainPosition - OpenedCurtainPosition) /
                            (ClosedCurtainPosition - OpenedCurtainPosition) / 2;
        var curtainWidth = BaseScreenSize.X;

        spriteBatch.FillRectangle(0, 0, curtainWidth, curtainHeight, CurtainColor);
        spriteBatch.FillRectangle(0, screenHeight - curtainHeight, curtainWidth, curtainHeight, CurtainColor);
    }

    private void LoadSettings()
    {
        var settings = _saveLoadManager.LoadSettings();

        // Настройки управления.
        _playerInputHandler.LoadControlSettings(settings?.Controls);

        // Кастомный курсор.
        CustomCursorEnabled = settings?.Controls?.CustomCursor ?? true;

        // Масштабирование.
        _isScalingPixelPerfect = settings?.Graphics?.PixelPerfectScaling ?? true;

        // Позиция окна.
        var position = settings?.Screen?.Position;
        if (position.HasValue)
            Window.Position = position.Value.ToPoint();
        _windowPosition = Window.Position;

        // Размер окна.
        var size = settings?.Screen?.Size ?? ScreenPoint.FromPoint(BaseScreenSize.Multiply(DefaultScale));
        _graphics.PreferredBackBufferWidth = size.X;
        _graphics.PreferredBackBufferHeight = size.Y;
        _windowSize = size.ToPoint();

        // Режим экрана.
        _graphics.HardwareModeSwitch = false;
        _graphics.IsFullScreen = settings?.Screen?.Mode switch
        {
            ScreenMode.Window => false,
            ScreenMode.Borderless => true,
            _ => true
        };

        _graphics.ApplyChanges();

        // Развернутое окно.
        var isMaximized = settings?.Screen?.IsMaximized;
        if (isMaximized is true)
            Window.Maximize();

        ScalePresentationArea();
    }

    private void SaveSettings()
    {
        var settings = new UserSettings
        {
            Controls = new ControlsSettings
            {
                CustomCursor = CustomCursorEnabled,
                PlayerControls = _playerInputHandler.SaveSettings()
            },
            Graphics = new GraphicsSettings
            {
                PixelPerfectScaling = _isScalingPixelPerfect
            },
            Screen = new ScreenSettings
            {
                Mode = _graphics.IsFullScreen ? ScreenMode.Borderless : ScreenMode.Window,
                Position = ScreenPoint.FromPoint(_windowPosition),
                Size = ScreenPoint.FromPoint(_windowSize),
                IsMaximized = Window.IsMaximized()
            }
        };

        _saveLoadManager.SaveSettings(settings);
    }

    private void ScalePresentationArea()
    {
        // Запоминаем положение и размер окна, только если находимся в окне.
        if (!_graphics.IsFullScreen && !Window.IsMaximized())
        {
            _windowPosition = Window.Position;
            _windowSize = Window.ClientBounds.Size;
        }

        _backbufferWidth = GraphicsDevice.PresentationParameters.BackBufferWidth;
        _backbufferHeight = GraphicsDevice.PresentationParameters.BackBufferHeight;

        var horScaling = (float)_backbufferWidth / BaseScreenSize.X;
        var verScaling = (float)_backbufferHeight / BaseScreenSize.Y;
        _scale = MathHelper.Min(horScaling, verScaling);
        if (_isScalingPixelPerfect)
            _scale = (int)_scale;
        var screenScalingFactor = new Vector3(_scale, _scale, 1);
        var scaleTransformation = Matrix.CreateScale(screenScalingFactor);

        var xShift = (horScaling - _scale) * BaseScreenSize.X / 2;
        var yShift =(verScaling - _scale) * BaseScreenSize.Y / 2;
        if (_isScalingPixelPerfect)
        {
            xShift = MathF.Round(xShift);
            yShift = MathF.Round(yShift);
        }
        var shiftTransformation = Matrix.CreateTranslation(xShift, yShift, 0);

        _globalTransformation = _levelTransformation * scaleTransformation * shiftTransformation;
    }
}