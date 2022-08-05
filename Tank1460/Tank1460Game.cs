﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Diagnostics;
using System.Linq;
using Tank1460.Extensions;
using Tank1460.LevelObjects.Explosions;
using Tank1460.LevelObjects.Tiles;
using Tank1460.SaveLoad;

namespace Tank1460;

/// <summary>
/// This is the main type for your game.
/// </summary>
public class Tank1460Game : Game
{
    private const int FPS = 60;

    /// <summary>
    /// Длительность одного кадра в секундах.
    /// </summary>
    public const double OneFrameSpan = 1.0 / FPS;

#if DEBUG
    public static bool ShowObjectsBoundaries;
    public static bool ShowBotsPeriods = true;
#endif

    private const int LevelLeftInTiles = 1;
    private const int LevelTopInTiles = 1;
    const int LevelWidthInTiles = 26;
    const int LevelHeightInTiles = 26;

    const int ScreenWidthInTiles = LevelLeftInTiles + LevelWidthInTiles + 4;
    const int ScreenHeightInTiles = LevelTopInTiles + LevelHeightInTiles + 1;

    private readonly Color HUDBackColor = new(0x7f7f7f);

    private LevelHud _levelHud;

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

    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private SpriteBatch _unscalableSpriteBatch;

    readonly Vector2 _baseScreenSize = new(ScreenWidthInTiles * Tile.DefaultWidth, ScreenHeightInTiles * Tile.DefaultHeight);

    private readonly Matrix _levelTransformation = Matrix.CreateTranslation(LevelLeftInTiles * Tile.DefaultWidth, LevelTopInTiles * Tile.DefaultHeight, 0);
    private Matrix _globalTransformation;
    private float _scale = 1.0f;

    private int _backbufferWidth, _backbufferHeight;
    private bool _isCustomCursorVisible;

    private const int NumberOfLevels = 2;

    private int _levelNumber = 0;
    private Level _level;

    private KeyboardState _keyboardState;
    private Cursor _cursor;

    private readonly SaveLoadManager _saveLoadManager = new();

    public Tank1460Game()
    {
        Window.Title = "Tank 1460";

        _graphics = new GraphicsDeviceManager(this);

        if (FPS != 60)
        {
#pragma warning disable CS0162 // Unreachable code detected
            _graphics.SynchronizeWithVerticalRetrace = false;
            IsFixedTimeStep = false;
            TargetElapsedTime = TimeSpan.FromMilliseconds(1000.0 / FPS);
#pragma warning restore CS0162 // Unreachable code detected
        }

        Content.RootDirectory = "Content";

        const int defaultScale = 3;
        _graphics.PreferredBackBufferWidth = defaultScale * (int)_baseScreenSize.X;
        _graphics.PreferredBackBufferHeight = defaultScale * (int)_baseScreenSize.Y;

        Window.AllowUserResizing = true;

        IsFixedTimeStep = true;
        CustomCursorEnabled = true;
    }

    private void LoadSettings()
    {
        var settings = _saveLoadManager.LoadSettings();

        var position = settings?.Screen?.Position;
        var size = settings?.Screen?.Size;
        var isMaximized = settings?.Screen?.IsMaximized;

        // Позиция окна.
        if (position.HasValue)
            Window.Position = position.Value.ToPoint();

        // Размер окна.
        if (size.HasValue && size.Value.X >= _baseScreenSize.X && size.Value.Y >= _baseScreenSize.Y)
        {
            _graphics.PreferredBackBufferWidth = size.Value.X;
            _graphics.PreferredBackBufferHeight = size.Value.Y;
        }

        // Развернутое окно.
        if (isMaximized is true)
            Window.Maximize();

        _graphics.HardwareModeSwitch = false;

        // Режим экрана.
        switch (settings?.Screen?.Mode)
        {
            case ScreenMode.Borderless:
                _graphics.IsFullScreen = true;
                break;

            case ScreenMode.Window:
            default:
                _graphics.IsFullScreen = false;
                break;
        }

        _graphics.ApplyChanges();
    }

    private void SaveSettings()
    {
        var settings = new SettingsData
        {
            Screen = new ScreenSettingsData
            {
                Mode = _graphics.IsFullScreen ? ScreenMode.Borderless : ScreenMode.Window,
                Position = ScreenPoint.FromPoint(Window.Position),
                Size = ScreenPoint.FromPoint(Window.ClientBounds.Size),
                IsMaximized = Window.IsMaximized()
            }
        };

        _saveLoadManager.SaveSettings(settings);
    }

    /// <summary>
    /// Allows the game to perform any initialization it needs to before starting to run.
    /// This is where it can query for any required services and load any non-graphic
    /// related content.  Calling base.Initialize will enumerate through any components
    /// and initialize them as well.
    /// </summary>
    protected override void Initialize()
    {
        //var form = (Form)Form.FromHandle(Window.Handle);
        //form.WindowState = FormWindowState.Maximized;
        //
        //Window.Position = new Point(0, 0);
        //SDL_MaximizeWindow(Window.Handle);

        LoadSettings();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _unscalableSpriteBatch = new SpriteBatch(GraphicsDevice);

        _levelHud = new LevelHud(Content);
        _cursor = new Cursor(Content);

        ScalePresentationArea();

        LoadNextLevel();
    }

    private void ScalePresentationArea()
    {
        _backbufferWidth = GraphicsDevice.PresentationParameters.BackBufferWidth;
        _backbufferHeight = GraphicsDevice.PresentationParameters.BackBufferHeight;

        var horScaling = _backbufferWidth / _baseScreenSize.X;
        var verScaling = _backbufferHeight / _baseScreenSize.Y;
        _scale = MathHelper.Min(horScaling, verScaling);
        var screenScalingFactor = new Vector3(_scale, _scale, 1);
        var scaleTransformation = Matrix.CreateScale(screenScalingFactor);

        var xShift = (horScaling - _scale) * _baseScreenSize.X / 2;
        var yShift = (verScaling - _scale) * _baseScreenSize.Y / 2;
        var shiftTransformation = Matrix.CreateTranslation(xShift, yShift, 0);

        _globalTransformation = _levelTransformation * scaleTransformation * shiftTransformation;
    }

    /// <summary>
    /// UnloadContent will be called once per game and is the place to unload
    /// game-specific content.
    /// </summary>
    protected override void UnloadContent()
    {
        _level?.Dispose();
        base.UnloadContent();
    }

    /// <summary>
    /// Allows the game to run logic such as updating the world,
    /// checking for collisions, gathering input, and playing audio.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    protected override void Update(GameTime gameTime)
    {
        // Если размер экрана изменился.
        if (_backbufferHeight != GraphicsDevice.PresentationParameters.BackBufferHeight ||
            _backbufferWidth != GraphicsDevice.PresentationParameters.BackBufferWidth)
            ScalePresentationArea();

        HandleInput();

        if (_customCursorEnabled)
        {
            // Курсор не обновляет свое состояние, если он отключен или вне экрана.
            var mouseState = Mouse.GetState();
            _isCustomCursorVisible = mouseState.X >= 0 && mouseState.Y >= 0 && mouseState.X < _backbufferWidth &&
                                     mouseState.Y < _backbufferHeight;

            if (_isCustomCursorVisible)
                _cursor.Update(gameTime, mouseState, _scale);
        }
        else
        {
            _isCustomCursorVisible = false;
        }

        _level?.Update(gameTime, _keyboardState);

        base.Update(gameTime);
    }

    private void HandleInput()
    {
        _keyboardState = KeyboardEx.GetState();

        if (_keyboardState.IsKeyDown(Keys.Escape))
            Exit();

        if (_keyboardState.IsKeyDown(Keys.LeftAlt) || _keyboardState.IsKeyDown(Keys.RightAlt))
        {
            if (KeyboardEx.HasBeenPressed(Keys.Enter))
            {
                _graphics.IsFullScreen = !_graphics.IsFullScreen;
                _graphics.ApplyChanges();
            }
        }

#if DEBUG
        if (KeyboardEx.HasBeenPressed(Keys.F5))
            ShowObjectsBoundaries = !ShowObjectsBoundaries;

        if (KeyboardEx.HasBeenPressed(Keys.F6))
            ShowBotsPeriods = !ShowBotsPeriods;

        if (KeyboardEx.HasBeenPressed(Keys.F7))
            CustomCursorEnabled = !CustomCursorEnabled;

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
    }

    private void LoadNextLevel()
    {
        LoadLevel(_levelNumber % NumberOfLevels + 1);
    }

    private void LoadLevel(int levelNumber)
    {
        _level?.Dispose();
        var levelStructure = new LevelStructure($"Content/Levels/{levelNumber}.lvl");

        _level = new Level(Services, levelStructure, levelNumber);
        _levelNumber = levelNumber;
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

    /// <summary>
    /// This is called when the game should draw itself.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(HUDBackColor);

        _spriteBatch.Begin(SpriteSortMode.Immediate, null, SamplerState.PointClamp, null, null, null, _globalTransformation);
        _level.Draw(gameTime, _spriteBatch);

        var hudPosition = Level.GetTileBounds(LevelLeftInTiles + LevelWidthInTiles, LevelTopInTiles).Location.ToVector2();
        hudPosition.X -= 1;
        _levelHud.Draw(_level, _spriteBatch, hudPosition);
        _spriteBatch.End();

        if (_isCustomCursorVisible)
        {
            _unscalableSpriteBatch.Begin(SpriteSortMode.Immediate, null, SamplerState.PointClamp);
            _cursor.Draw(gameTime, _unscalableSpriteBatch);
            _unscalableSpriteBatch.End();
        }

        base.Draw(gameTime);
    }
}