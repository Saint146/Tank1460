using Microsoft.Xna.Framework;
using Tank1460.LevelObjects.Tiles;

namespace Tank1460.Menu;

internal class MainMenu : Form
{
    public int PlayerCount { get; private set; }

    public int LevelNumber { get; private set; }

    private const string MenuItem1PlayerText = "1 PLAYER";
    private const string MenuItem2PlayersText = "2 PLAYERS";
    private const int MenuItemsX = 12 * Tile.DefaultWidth;
    private const int MenuItem1Y = 14 * Tile.DefaultHeight;
    private const int MenuItem2Y = 16 * Tile.DefaultHeight;
    private static readonly Point MenuItem1StartingPosition = new(MenuItemsX, MenuItem1Y);
    private static readonly Point MenuItem2StartingPosition = new(MenuItemsX, MenuItem2Y);

    private FormItem _player1Item;
    private FormItem _player2Item;
    private FormItem _cursorItem;

    public MainMenu(ContentManagerEx content, int defaultPlayerCount, int defaultLevelNumber) : base(content, defaultPlayerCount, defaultLevelNumber)
    {
        PlayerCount = defaultPlayerCount;
        LevelNumber = defaultLevelNumber;

        CreateItems();
    }

    protected override void CursorUp()
    {
        PlayerCount = 3 - PlayerCount;
        UpdateCursorPosition();
    }

    protected override void CursorDown()
    {
        PlayerCount = 3 - PlayerCount;
        UpdateCursorPosition();
    }

    protected override void Enter()
    {
        Exit();
    }

    protected override void HandleClick(FormItem item)
    {
        if (item == _cursorItem)
            return;

        PlayerCount = item == _player1Item ? 1 : 2;
        Exit();
    }

    private void CreateItems()
    {
        _player1Item = CreateTextItem(MenuItem1PlayerText);
        _player2Item = CreateTextItem(MenuItem2PlayersText);
        _cursorItem = new FormItem(
            normalTexture: Content.LoadRecoloredTexture(@"Sprites/Tank/Type0/Right", @"Sprites/_R/Tank/Yellow"),
            hoverTexture: Content.LoadRecoloredTexture(@"Sprites/Tank/Type0/Right", @"Sprites/_R/Tank/Yellow"),
            pressedTexture: Content.LoadRecoloredTexture(@"Sprites/Tank/Type3/Right", @"Sprites/_R/Tank/Yellow"),
            frameTime: 4 * Tank1460Game.OneFrameSpan);

        AddItem(_player1Item, MenuItem1StartingPosition);
        AddItem(_player2Item, MenuItem2StartingPosition);
        AddItem(_cursorItem);

        UpdateCursorPosition();
    }

    private void UpdateCursorPosition()
    {
        var x = MenuItemsX - Tile.DefaultWidth - _cursorItem.Bounds.Width;
        var y = (PlayerCount == 1 ? _player1Item.Bounds : _player2Item.Bounds).Center.Y - _cursorItem.Bounds.Height / 2f - 1;
        _cursorItem.Position = new(x, (int)y);
    }
}