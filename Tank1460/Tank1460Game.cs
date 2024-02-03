using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Tank1460.Common;
using Tank1460.Common.Extensions;
using Tank1460.Common.Level;
using Tank1460.Input;
using Tank1460.LevelObjects.Explosions;
using Tank1460.LevelObjects.Tiles;
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

    internal GameStatus Status { get; private set; }

    private new ContentManagerEx Content { get; }

    private Rectangle GetLevelBounds() =>
        _level?.Bounds ?? new Rectangle(0, 0, 26 * Tile.DefaultWidth, 26 * Tile.DefaultHeight);

    private static Point PreLevelIndent { get; } = new(2 * Tile.DefaultWidth, Tile.DefaultHeight);
    private static Point PostLevelIndent { get; } = new(Tile.DefaultWidth, Tile.DefaultHeight);
    private static Point PostHudIndent { get; } = new(Tile.DefaultWidth, 0);

    private readonly Matrix _levelIndentTransformation = Matrix.CreateTranslation(PreLevelIndent.X, PreLevelIndent.Y, 0);
    private readonly Matrix _menuIndentTransformation = Matrix.Identity;

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

    private static Color GameBackColor { get; } = Color.Black;

    private bool _customCursorEnabled;
    private bool CustomCursorEnabled
    {
        get => _customCursorEnabled;
        set
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
    private Curtain _curtain;

    private Matrix _levelTransformation;
    private Matrix _menuTransformation;
    private float _scale = 1.0f;
    private const int DefaultScale = 3;

    private int _backbufferWidth, _backbufferHeight;
    private bool _isCustomCursorVisible;

    private readonly PlayerInputHandler _playerInputHandler;
    private MouseState _mouseState;
    private KeyboardState _keyboardState;
    private Dictionary<int, GamePadState> _gamePadStates;

    private GameState _gameState;
    private Point _windowPosition;
    private Point _windowSize;

    private PlayerIndex[] AllPlayers { get; } = { PlayerIndex.One, PlayerIndex.Two };

    private PlayerIndex[] PlayersInGame { get; set; } = { PlayerIndex.One };

    private string LevelFolder { get; set; } = "Modern";

    private int LevelNumber { get; set; } = 1;

    public Tank1460Game()
    {
        Window.Title = "Tank 1460";
        Window.AllowUserResizing = true;
        Content = new ContentManagerEx(Services, "Content");
        IsFixedTimeStep = true;

        _graphics = new GraphicsDeviceManager(this);

        _playerInputHandler = new PlayerInputHandler(AllPlayers);

        ResetGameState();

        Status = GameStatus.Initializing;

#pragma warning disable CS0162
        if (Fps == 60) return;

        _graphics.SynchronizeWithVerticalRetrace = false;
        //IsFixedTimeStep = false; // Вроде бы и без этого работает
        TargetElapsedTime = TimeSpan.FromMilliseconds(1000.0 / Fps);
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

        PreloadLevelsContent();

        Status = GameStatus.Ready;
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

        ProcessStatus();

        _menu?.HandleInput(inputs, _mouseState.CopyWithPosition(_mouseState.Position.ApplyReversedTransformation(_menuTransformation)));
        _menu?.Update(gameTime);

        _level?.HandleInput(inputs);
        _level?.Update(gameTime);

        _curtain?.Update(gameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(_menu is not null || _level is null ? GameBackColor : LevelBackColor);

        if (_level is not null)
        {
            _spriteBatch.Begin(SpriteSortMode.Immediate, null, SamplerState.PointClamp, null, null, null, _levelTransformation);

            _level.Draw(gameTime, _spriteBatch);
            var hudPosition = new Point(GetLevelBounds().Width + PostLevelIndent.X - 1, 0); // ну вот так в оригинале, на один пиксель сдвинуто левее сетки
            _levelHud.Draw(_level, _spriteBatch, hudPosition);

            _spriteBatch.End();
        }

        if (_menu is not null)
        {
            _spriteBatch.Begin(SpriteSortMode.Immediate, null, SamplerState.PointClamp, null, null, null, _menuTransformation);

            _menu.Draw(gameTime, _spriteBatch);

            _spriteBatch.End();
        }

        if (_curtain is not null)
        {
            _spriteBatch.Begin(SpriteSortMode.Immediate, null, SamplerState.PointClamp);

            _curtain.Draw(_spriteBatch, new Rectangle(0, 0, _backbufferWidth, _backbufferHeight));

            _spriteBatch.End();
        }

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

    private void ProcessStatus()
    {
        switch (Status)
        {
            case GameStatus.Initializing:
                // Ждём загрузки. По идее, всё однопоточно, конечно, но LoadContent вызывается базовым классом, и мы не знаем, когда это произойдет.
                break;

            case GameStatus.Ready:
                Status = GameStatus.InMenu;
                UnloadLevel();
                LoadMenu();
                break;

            case GameStatus.InMenu:
            case GameStatus.InLevel:
                break;

            case GameStatus.CurtainOpening:
                if (!_curtain.IsFinished)
                    break;

                _curtain = null;
                Status = GameStatus.InLevel;
                _level.Start();
                break;

            case GameStatus.CurtainClosing:
                if (!_curtain.IsFinished)
                    break;

                _curtain = null;
                UnloadMenu();
                LoadLevel();
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(Status));
        }
    }

    protected override void EndRun()
    {
        Content.Unload();
        base.EndRun();
    }

    private void PreloadLevelsContent()
    {
        // TODO: Перенести внутрь левела и прочих вложенных объектов (чтобы каждый объект сам говорил, что ему предзагружать).
        Content.MassLoadContent<Texture2D>("Sprites", "*.*", recurse: true);
        Content.MassLoadContent<SoundEffect>("Sounds", "*.*", recurse: true);
        //Content.MassLoadContent<LevelStructure>("Levels", "*.*", recurse: true);
    }

    private void ResetGameState()
    {
        _gameState = new GameState(PlayersInGame);
    }

    private PlayerInputCollection HandleInput()
    {
        // Клавиатура.
        _keyboardState = KeyboardEx.GetState();

        // Мышь.
        _mouseState = Mouse.GetState();
        // Курсор не обновляет свое состояние, если он отключен или вне экрана.
        if (_customCursorEnabled) 
            _isCustomCursorVisible = _mouseState.X >= 0 && _mouseState.Y >= 0 && _mouseState.X < _backbufferWidth && _mouseState.Y < _backbufferHeight;
        else
            _isCustomCursorVisible = false;

        // Геймпады.
        _gamePadStates = _playerInputHandler.GetActiveGamePadIndices().ToDictionary(index => index, GamePad.GetState);

        var inputs = _playerInputHandler.HandleInput(_keyboardState, _gamePadStates);

        // Глобальные бинды.
        if (KeyboardEx.HasBeenPressed(Keys.F11) || (KeyboardEx.IsPressed(Keys.LeftAlt) || KeyboardEx.IsPressed(Keys.RightAlt)) && KeyboardEx.HasBeenPressed(Keys.Enter))
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
                if (!KeyboardEx.HasBeenPressed(key))
                    continue;

                StartLoadingLevelSequence("Test", digit);
                break;
            }

            if (KeyboardEx.HasBeenPressed(Keys.OemPlus))
                StartLoadingLevelSequence(levelNumber: LevelNumber + 1);

            if (KeyboardEx.HasBeenPressed(Keys.OemMinus))
                StartLoadingLevelSequence(levelNumber: LevelNumber + 1);
        }

        if (_keyboardState.IsKeyDown(Keys.J))
        {
            var x = Rng.Next(_level.TileBounds.Left, _level.TileBounds.Right);
            var y = Rng.Next(_level.TileBounds.Top, _level.TileBounds.Bottom);

            var explosion = new CommonExplosion(_level);
            explosion.Spawn(Level.GetTileBounds(x, y).Location);
        }
#endif

        return inputs;
    }

    private void UnloadMenu()
    {
        if (_menu is null)
            return;

        _menu.MenuExited -= Menu_MenuExited;
        _menu = null;
    }

    private void LoadMenu()
    {
        _menu = new Menu(Content, 1, 1);
        _menu.MenuExited += Menu_MenuExited;
    }

    private void Menu_MenuExited()
    {
        LevelNumber = _menu.LevelNumber;
        PlayersInGame = AllPlayers.Take(_menu.PlayerCount).ToArray();
        ResetGameState();
        StartLoadingLevelSequence();
    }

    /// <summary>
    /// Начать процесс загрузки уровня (закрыть шторку и открыть её уже с уровнем).
    /// </summary>
    /// <param name="levelFolder">Папка с уровнями. Если не указать, останется от предыдущего уровня.</param>
    /// <param name="levelNumber">Номер уровня. Если не указать, останется от предыдущего уровня.</param>
    private void StartLoadingLevelSequence(string levelFolder = null, int? levelNumber = null)
    {
        if (levelFolder is not null)
            LevelFolder = levelFolder;

        if (levelNumber is not null)
            LevelNumber = levelNumber.Value;

        _curtain = new Curtain(LevelBackColor, CurtainAction.Close);
        Status = GameStatus.CurtainClosing;
    }

    private void UnloadLevel()
    {
        if (_level is null)
            return;

        _level.LevelComplete -= Level_LevelComplete;
        _level.GameOver -= Level_GameOver;

        _level.Dispose();
        _level = null;
    }

    private void LoadLevel()
    {
        Debug.WriteLine($"Loading level {LevelFolder}/{LevelNumber}...");

        UnloadLevel();

        var levelStructure = Content.Load<LevelStructure>($"Levels/{LevelFolder}/{LevelNumber}");
        _level = new Level(Services, levelStructure, LevelNumber, _gameState);

        _level.LevelComplete += Level_LevelComplete;
        _level.GameOver += Level_GameOver;

        ScalePresentationArea();

        // TODO: Вынести в сеттер Status
        Status = GameStatus.CurtainOpening;
        _curtain = new Curtain(LevelBackColor, CurtainAction.Open);

        Debug.WriteLine($"Level {LevelFolder}/{LevelNumber} loaded.");
    }

    private void Level_LevelComplete(Level level)
    {
        _gameState = level.GetGameState();
        StartLoadingLevelSequence(levelNumber: LevelNumber + 1);
    }

    private void Level_GameOver(Level level)
    {
        Status = GameStatus.Ready;
        ResetGameState();
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
        // Запоминаем положение и размер окна, только если находимся в окне, чтобы потом хранить именно его.
        if (!_graphics.IsFullScreen && !Window.IsMaximized())
        {
            _windowPosition = Window.Position;
            _windowSize = Window.ClientBounds.Size;
        }

        _backbufferWidth = GraphicsDevice.PresentationParameters.BackBufferWidth;
        _backbufferHeight = GraphicsDevice.PresentationParameters.BackBufferHeight;

        // Считаем соотношения реальных размеров окна и виртуального экрана.
        var horScaling = (float)_backbufferWidth / BaseScreenSize.X;
        var verScaling = (float)_backbufferHeight / BaseScreenSize.Y;

        // Берем минимальный из них как масштаб и создаём матрицу для масштабирования.
        _scale = MathHelper.Min(horScaling, verScaling);
        if (_isScalingPixelPerfect)
            _scale = (int)_scale;
        var screenScalingFactor = new Vector3(_scale, _scale, 1);
        var scaleTransformation = Matrix.CreateScale(screenScalingFactor);

        // Подсчитываем сдвиги по координатам и создаём матрицу для сдвига.
        var xShift = (horScaling - _scale) * BaseScreenSize.X / 2;
        var yShift = (verScaling - _scale) * BaseScreenSize.Y / 2;
        if (_isScalingPixelPerfect)
        {
            xShift = MathF.Round(xShift);
            yShift = MathF.Round(yShift);
        }
        var shiftTransformation = Matrix.CreateTranslation(xShift, yShift, 0);

        // Итоговая трансформация.
        var globalTransformation = scaleTransformation * shiftTransformation;

        // Для разных сущностей рассчитываем от их изначальных сдвигов.
        _levelTransformation = _levelIndentTransformation * globalTransformation;
        _menuTransformation = _menuIndentTransformation * globalTransformation;
    }
}