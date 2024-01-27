using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tank1460.Input;
using Tank1460.LevelObjects.Tiles;

namespace Tank1460;

internal class Menu : IDisposable
{
    public Rectangle Bounds { get; private set; }

    public int PlayerCount { get; private set; }

    public int LevelNumber { get; private set; }

    private MenuState State { get; set; }

    private ContentManagerEx Content { get; }

    private Font _font;
    private TimedAnimationPlayer _pointerSprite;

    public Menu(IServiceProvider serviceProvider, int defaultPlayerCount, int defaultLevelNumber)
    {
        Content = new ContentManagerEx(serviceProvider, "Content");
        PlayerCount = defaultPlayerCount;
        LevelNumber = defaultLevelNumber;

        State = MenuState.Running;

        LoadContent();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Content.Unload();
    }

    public void HandleInput(PlayerInputCollection playersInputs)
    {
        if (State is not MenuState.Running)
            return;

        if (playersInputs.Values.Any(inputs => inputs.Pressed.HasFlag(PlayerInputCommands.Down)))
            PlayerCount = 3 - PlayerCount;
        else if (playersInputs.Values.Any(inputs => inputs.Pressed.HasFlag(PlayerInputCommands.Up)))
            PlayerCount = 3 - PlayerCount;
        else if (playersInputs.Values.Any(inputs => inputs.Pressed.HasFlag(PlayerInputCommands.Start)))
            Exit();
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        const float itemsX = 12 * Tile.DefaultWidth;
        const float item1Y = 14 * Tile.DefaultHeight;
        const float item2Y = 16 * Tile.DefaultHeight;

        _font.Draw("1 PLAYER", spriteBatch, new Vector2(itemsX, item1Y));
        _font.Draw("2 PLAYERS", spriteBatch, new Vector2(itemsX, item2Y));

        var pointerX = itemsX - Tile.DefaultWidth - _pointerSprite.VisibleRect.Width;
        var pointerY = (PlayerCount == 1 ? item1Y : item2Y) - _pointerSprite.VisibleRect.Height / 4.0f;

        _pointerSprite.Draw(spriteBatch, new Vector2(pointerX, pointerY));
    }

    public void Update(GameTime gameTime)
    {
        _pointerSprite.ProcessAnimation(gameTime);
    }

    private void LoadContent()
    {
        _font = new Font(Content, Color.White);

        var pointerTexture = Content.LoadRecoloredTexture(@"Sprites/Tank/Type0/Right", @"Sprites/_R/Tank/Yellow");
        var pointerAnimation = new Animation(pointerTexture, 4 * Tank1460Game.OneFrameSpan, true);
        _pointerSprite = new TimedAnimationPlayer();
        _pointerSprite.PlayAnimation(pointerAnimation);
    }

    private void Exit()
    {
        State = MenuState.Exited;
        MenuExited?.Invoke();
    }

    public delegate void MenuEvent();

    public event MenuEvent MenuExited;
}

internal enum MenuState
{
    Running,
    Exited
}