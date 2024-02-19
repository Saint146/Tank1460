using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Tank1460.Audio;
using Tank1460.Common;
using Tank1460.Common.Extensions;
using Tank1460.Common.Level;
using Tank1460.Forms;
using Tank1460.Input;
using Tank1460.LevelObjects.Explosions;
using Tank1460.LevelObjects.Tiles;
using Tank1460.SaveLoad;
using Tank1460.SaveLoad.Settings;

namespace Tank1460;

public class Tank1460Game : Game
{
    internal GameStatus Status { get; private set; }

    private new ContentManagerEx Content { get; }

    private Rectangle GetLevelBounds() =>
        _level?.Bounds ?? new Rectangle(0, 0, 26 * Tile.DefaultWidth, 26 * Tile.DefaultHeight);

    private static Point PreLevelIndent { get; } = new(2 * Tile.DefaultWidth, Tile.DefaultHeight);
    private static Point PostLevelIndent { get; } = new(Tile.DefaultWidth, Tile.DefaultHeight);
    private static Point PostHudIndent { get; } = new(Tile.DefaultWidth, 0);

    private readonly Matrix _levelIndentTransformation = Matrix.CreateTranslation(PreLevelIndent.X, PreLevelIndent.Y, 0);
    private readonly Matrix _formIndentTransformation = Matrix.Identity;

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

    private static Color LevelBackColor { get; } = new(0xff7f7f7f);

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
    private Form _form;
    private LevelHud _levelHud;
    private Cursor _cursor;
    private Curtain _curtain;

    private Matrix _levelTransformation;
    private Matrix _formTransformation;
    private float _scale = 1.0f;
    private const int DefaultScale = 3;
    internal const bool ClassicRules = false;

    private int _backbufferWidth, _backbufferHeight;
    private bool _isMouseInsideWindow;
    private bool _isCustomCursorVisible;

    private readonly PlayerInputHandler _playerInputHandler;
    private MouseState _mouseState;
    private KeyboardState _keyboardState;
    private Dictionary<int, GamePadState> _gamePadStates;

    private GameState _gameState;
    private Point _windowPosition;
    private Point _windowSize;
    private ISoundPlayer _soundPlayer;

    private PlayerIndex[] AllPlayers { get; } = { PlayerIndex.One, PlayerIndex.Two };

    private PlayerIndex[] PlayersInGame { get; set; } = { PlayerIndex.One };

    private string LevelFolder { get; set; } = ClassicRules ? "Classic" : "Modern";

    private int LevelNumber { get; set; } =
#if DEBUG
        Rng.Next(1, 36);
#else
    1;
#endif

    public Tank1460Game()
    {
        Window.AllowUserResizing = true;
        Content = new ContentManagerEx(Services, "Content");
        Services.AddService(Content);
        Services.AddService(typeof(ISoundPlayer), new SoundPlayer(Content));

        IsFixedTimeStep = true;

        _graphics = new GraphicsDeviceManager(this);

        _playerInputHandler = new PlayerInputHandler(AllPlayers);

        ResetGameState();

        Status = GameStatus.Initializing;

        _graphics.SynchronizeWithVerticalRetrace = false;
        //IsFixedTimeStep = false; // Вроде бы и без этого работает
        TargetElapsedTime = TimeSpan.FromSeconds(GameRules.TimeInFrames(1));
    }

    protected override void Initialize()
    {
        base.Initialize();
        Window.Title = "Tank 1460";
        LoadSettings();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _levelHud = new LevelHud(Content);
        _cursor = new Cursor(Content);
        _soundPlayer = Services.GetService<ISoundPlayer>();

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

        if (_form is not null)
        {
            _form.HandleInput(inputs, _mouseState.CopyWithPosition(_mouseState.Position.ApplyReversedTransformation(_formTransformation)));
            _form.Update(gameTime);
        }
        else if (_level is not null)
        {
            _level.HandleInput(inputs);
            _level.Update(gameTime);
        }

        _curtain?.Update(gameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(_form is not null || _level is null ? GameBackColor : LevelBackColor);

        if (_form is not null)
        {
            _spriteBatch.Begin(SpriteSortMode.Immediate, null, SamplerState.PointClamp, null, null, null, _formTransformation);

            _form.Draw(_spriteBatch);

            _spriteBatch.End();
        }
        else if (_level is not null)
        {
            _spriteBatch.Begin(SpriteSortMode.Immediate, null, SamplerState.PointClamp, null, null, null, _levelTransformation);

            _level.Draw(gameTime, _spriteBatch);
            var hudPosition = new Point(GetLevelBounds().Width + PostLevelIndent.X - 1, 0); // ну вот так в оригинале, на один пиксель сдвинуто левее сетки
            _levelHud.Draw(_level, _spriteBatch, hudPosition);

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

            _cursor.Draw(_spriteBatch);

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
        UnloadForm();
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
                Status = GameStatus.InMainMenu;
                UnloadLevel();
                LoadMainMenu();
                break;

            case GameStatus.InMainMenu:
                if (_form.Status != FormStatus.Exited)
                    break;

                var mainMenu = (MainMenu)_form;
                LevelNumber = mainMenu.LevelNumber;
                PlayersInGame = AllPlayers.Take(mainMenu.PlayerCount).ToArray();
                ResetGameState();
                StartLoadingLevelSequence();
                break;

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
                UnloadLevel();
                UnloadForm();
                LoadLevel();
                break;

            case GameStatus.InWinScoreScreen:
                if (_form.Status != FormStatus.Exited)
                    break;

                UnloadForm();
                StartLoadingLevelSequence(levelNumber: LevelNumber + 1);
                break;

            case GameStatus.InLostScoreScreen:
                if (_form.Status != FormStatus.Exited)
                    break;

                UnloadForm();
                UnloadLevel();
                Status = GameStatus.GameOverScreen;
                LoadGameOverScreen();
                break;

            case GameStatus.GameOverScreen:
                if (_form.Status != FormStatus.Exited)
                    break;

                UnloadForm();
                Status = GameStatus.Ready;
                ResetGameState();
                break;

            case GameStatus.HighscoreScreen:
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
        _isMouseInsideWindow = _mouseState.X >= 0 && _mouseState.Y >= 0 && _mouseState.X < _backbufferWidth && _mouseState.Y < _backbufferHeight;

        // Не учитываем нажатия мыши, если окно неактивно или курсор за экраном (самый простой случай — клик по заголовку окна в оконном режиме)
        if (!_isMouseInsideWindow || !IsActive)
            _mouseState = _mouseState.CopyWithAllButtonsReleased();

        // Курсор не обновляет свое состояние, если он отключен или вне экрана.
        _isCustomCursorVisible = _customCursorEnabled && _isMouseInsideWindow;

        // Геймпады.
        _gamePadStates = _playerInputHandler.GetActiveGamePadIndices().ToDictionary(index => index, GamePad.GetState);

        var inputs = _playerInputHandler.HandleInput(_keyboardState, _gamePadStates);

        // Глобальные бинды, включая отладочные.

        // F11 / Alt+Enter — Включить/отключить полный экран.
        if (KeyboardEx.HasBeenPressed(Keys.F11) || (KeyboardEx.IsPressed(Keys.LeftAlt) || KeyboardEx.IsPressed(Keys.RightAlt)) && KeyboardEx.HasBeenPressed(Keys.Enter))
        {
            _graphics.IsFullScreen = !_graphics.IsFullScreen;
            _graphics.ApplyChanges();
        }

#if DEBUG
        // F5 — Включить/отключить отображение границ объектов и занимаемых тайлов
        if (KeyboardEx.HasBeenPressed(Keys.F5)) GameRules.ShowObjectsBoundaries = !GameRules.ShowObjectsBoundaries;

        // F6 — Включить/отключить отображение "периодов" стандартного ИИ ботов
        if (KeyboardEx.HasBeenPressed(Keys.F6)) GameRules.ShowBotsPeriods = !GameRules.ShowBotsPeriods;

        // F7 — Включить/отключить системный курсор
        if (KeyboardEx.HasBeenPressed(Keys.F7))
            CustomCursorEnabled = !CustomCursorEnabled;

        // F8 — Включить/отключить масштабирование с точностью до пикселя
        if (KeyboardEx.HasBeenPressed(Keys.F8))
        {
            _isScalingPixelPerfect = !_isScalingPixelPerfect;
            ScalePresentationArea();
        }

        // Del — взорвать все танки игроков
        if (KeyboardEx.HasBeenPressed(Keys.Delete))
        {
            _level?.GetAllPlayerTanks().EmptyIfNull().ForEach(tank => tank.Explode(null));
        }

        // Shift+0..Shift+9 — загрузить уровень Test/%d
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

        // Ctrl+Numpad1..Ctrl+Numpad3 — отмасштабировать графику строго к масштабу 1:1..1:3
        if (_keyboardState.IsKeyDown(Keys.LeftControl))
        {
            foreach (var digit in Enumerable.Range(1, 3))
            {
                var key = Keys.NumPad0 + digit;
                if (!KeyboardEx.HasBeenPressed(key))
                    continue;

                _graphics.IsFullScreen = false;
                if (Window.IsMaximized())
                    Window.Restore();

                var size = ScreenPoint.FromPoint(BaseScreenSize.Multiply(digit));
                _graphics.PreferredBackBufferWidth = size.X;
                _graphics.PreferredBackBufferHeight = size.Y;

                _graphics.ApplyChanges();

                break;
            }
        }

        // J — Показать взрывы в случайных точках экрана.
        if (_keyboardState.IsKeyDown(Keys.J) && _level is not null)
        {
            var x = Rng.Next(_level.TileBounds.Left, _level.TileBounds.Right);
            var y = Rng.Next(_level.TileBounds.Top, _level.TileBounds.Bottom);

            var explosion = new CommonExplosion(_level);
            explosion.Spawn(Level.GetTileBounds(x, y).Location);
        }
#endif

        return inputs;
    }

    private void UnloadForm()
    {
        if (_form is null)
            return;

        _form = null;
    }

    private void LoadMainMenu()
    {
        Debug.Assert(_level is null);
        Debug.Assert(_form is null);

        _form = new MainMenu(Content, PlayersInGame.Length, LevelNumber);
    }

    private void LoadScoreScreen(bool showBonus)
    {
        Debug.Assert(_level is not null);
        Debug.Assert(_form is null);

        var levelStats = _level.Stats;
        _form = new ScoreScreen(Content, LevelNumber, 20000, _gameState, levelStats, showBonus);
    }

    private void LoadGameOverScreen()
    {
        Debug.Assert(_level is null);
        Debug.Assert(_form is null);

        _form = new GameOverScreen(Content);
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

        ScalePresentationArea();
    }

    private void LoadLevel()
    {
        Debug.WriteLine($"Loading level {LevelFolder}/{LevelNumber}...");
        Debug.Assert(_level is null);
        Debug.Assert(_form is null);

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

        _gameState.PlayersStates[PlayerIndex.One].Score = 19000;

        Status = GameStatus.InWinScoreScreen;
        LoadScoreScreen(showBonus: true);
    }

    private void Level_GameOver(Level level)
    {
        _gameState = level.GetGameState();

        Status = GameStatus.InLostScoreScreen;
        LoadScoreScreen(showBonus: false);
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
        _formTransformation = _formIndentTransformation * globalTransformation;
    }
}