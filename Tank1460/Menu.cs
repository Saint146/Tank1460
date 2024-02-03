using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Linq;
using Tank1460.Input;
using Tank1460.LevelObjects.Tiles;

namespace Tank1460;

internal class Menu
{
    public int PlayerCount { get; private set; }

    public int LevelNumber { get; }

    private MenuStatus Status { get; set; }

    private Font _font, _shadowFont, _pressedFont;
    private TimedAnimationPlayer _menuCursorSprite;

    /// <summary>
    /// Пункт меню, на котором левую кнопку мыши последний раз зажали.
    /// </summary>
    private int? _lastPressedMenuItemIndex;

    /// <summary>
    /// Пункт меню, над которым находится мышь.
    /// </summary>
    private int? _hoveringMenuItemIndex;

    private Rectangle _menuItem1Bounds;
    private Rectangle _menuItem2Bounds;

    private const string MenuItem1PlayerText = "1 PLAYER";
    private const string MenuItem2PlayersText = "2 PLAYERS";
    private const int MenuItemsX = 12 * Tile.DefaultWidth;
    private const int MenuItem1Y = 14 * Tile.DefaultHeight;
    private const int MenuItem2Y = 16 * Tile.DefaultHeight;
    private static readonly Point MenuItem1StartingPosition = new(MenuItemsX, MenuItem1Y);
    private static readonly Point MenuItem2StartingPosition = new(MenuItemsX, MenuItem2Y);
    private static readonly Point MenuItem1StartingShadowPosition = MenuItem1StartingPosition + new Point(1, 1);
    private static readonly Point MenuItem2StartingShadowPosition = MenuItem2StartingPosition + new Point(1, 1);

    public Menu(ContentManagerEx content, int defaultPlayerCount, int defaultLevelNumber)
    {
        PlayerCount = defaultPlayerCount;
        LevelNumber = defaultLevelNumber;

        Status = MenuStatus.Running;

        LoadContent(content);
    }

    public void HandleInput(PlayerInputCollection playersInputs, MouseState mouseState)
    {
        if (Status is not MenuStatus.Running)
            return;

        if (playersInputs.Values.Any(inputs => inputs.Pressed.HasFlag(PlayerInputCommands.Down)))
            PlayerCount = 3 - PlayerCount;
        else if (playersInputs.Values.Any(inputs => inputs.Pressed.HasFlag(PlayerInputCommands.Up)))
            PlayerCount = 3 - PlayerCount;
        else if (playersInputs.Values.Any(inputs => inputs.Pressed.HasFlag(PlayerInputCommands.Start)))
            Exit();

        _hoveringMenuItemIndex = HitTest(mouseState.Position);

        var isMouseDown = mouseState.LeftButton == ButtonState.Pressed;
        if (_lastPressedMenuItemIndex is null && isMouseDown)
        {
            // Клавишу только что нажали.
            _lastPressedMenuItemIndex = _hoveringMenuItemIndex;
        }
        else if (_lastPressedMenuItemIndex is not null && !isMouseDown)
        {
            // Клавишу только что отпустили.
            if (_hoveringMenuItemIndex == _lastPressedMenuItemIndex)
                HandleClick(_lastPressedMenuItemIndex.Value);

            _lastPressedMenuItemIndex = null;
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        // TODO: Ну давно пора уже окончательно одженерить для N пунктов. Ломает пока)
        var itemIndex = 0;
        if (_hoveringMenuItemIndex == itemIndex || _lastPressedMenuItemIndex == itemIndex)
            _shadowFont.Draw(MenuItem1PlayerText, spriteBatch, MenuItem1StartingShadowPosition);

        var font = _lastPressedMenuItemIndex == itemIndex && _hoveringMenuItemIndex == itemIndex ? _pressedFont : _font;
        font.Draw(MenuItem1PlayerText, spriteBatch, MenuItem1StartingPosition);

        itemIndex = 1;
        if (_hoveringMenuItemIndex == itemIndex || _lastPressedMenuItemIndex == itemIndex)
            _shadowFont.Draw(MenuItem2PlayersText, spriteBatch, MenuItem2StartingShadowPosition);

        font = _lastPressedMenuItemIndex == itemIndex && _hoveringMenuItemIndex == itemIndex ? _pressedFont : _font;
        font.Draw(MenuItem2PlayersText, spriteBatch, MenuItem2StartingPosition);

        var pointerX = MenuItemsX - Tile.DefaultWidth - _menuCursorSprite.VisibleRect.Width;
        var pointerY = (PlayerCount == 1 ? MenuItem1Y : MenuItem2Y) - _menuCursorSprite.VisibleRect.Height / 3.5f;

        _menuCursorSprite.Draw(spriteBatch, new Vector2(pointerX, pointerY));
    }

    public void Update(GameTime gameTime)
    {
        _menuItem1Bounds = _font.GetTextRectangle(MenuItem1PlayerText, MenuItem1StartingPosition);
        _menuItem1Bounds.Inflate(4, 4);
        _menuItem2Bounds = _font.GetTextRectangle(MenuItem2PlayersText, MenuItem2StartingPosition);
        _menuItem2Bounds.Inflate(4, 4);

        _menuCursorSprite.ProcessAnimation(gameTime);
    }

    private void LoadContent(ContentManagerEx content)
    {
        _font = content.LoadFont(@"Sprites/Font/Pixel8", Color.White);
        _shadowFont = content.LoadFont(@"Sprites/Font/Pixel8", new Color(0xff7f7f7f));
        _pressedFont = content.LoadFont(@"Sprites/Font/Pixel8", new Color(0x775000e0));

        var pointerTexture = content.LoadRecoloredTexture(@"Sprites/Tank/Type0/Right", @"Sprites/_R/Tank/Yellow");
        var pointerAnimation = new Animation(pointerTexture, 4 * Tank1460Game.OneFrameSpan, true);
        _menuCursorSprite = new TimedAnimationPlayer();
        _menuCursorSprite.PlayAnimation(pointerAnimation);
    }

    private void HandleClick(int menuItemIndex)
    {
        PlayerCount = menuItemIndex + 1;
        Exit();
    }

    private int? HitTest(Point mousePosition)
    {
        if (_menuItem1Bounds.Contains(mousePosition))
            return 0;

        if (_menuItem2Bounds.Contains(mousePosition))
            return 1;

        return null;
    }

    private void Exit()
    {
        Status = MenuStatus.Exited;
        MenuExited?.Invoke();
    }

    public delegate void MenuEvent();

    public event MenuEvent MenuExited;

    private enum MenuStatus
    {
        Running,
        Exited
    }
}