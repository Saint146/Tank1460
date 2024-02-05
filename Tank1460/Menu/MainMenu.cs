using Microsoft.Xna.Framework;
using System;
using System.Linq;
using Tank1460.Common.Extensions;
using Tank1460.Common.Level.Object.Tank;
using Tank1460.LevelObjects.Tanks;
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

    private TankType _cursorTankType;
    private TankColor _cursorTankColor;

    public MainMenu(ContentManagerEx content, int playerCount, int levelNumber) : base(content)
    {
        PlayerCount = playerCount;
        LevelNumber = levelNumber;

        CreateItems();
        CreateCursor();
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
        {
            ChangeCursor();
            return;
        }

        PlayerCount = item == _player1Item ? 1 : 2;
        Exit();
    }

    private void CreateItems()
    {
        _player1Item = CreateTextItem(MenuItem1PlayerText);
        _player2Item = CreateTextItem(MenuItem2PlayersText);

        AddItem(_player1Item, MenuItem1StartingPosition);
        AddItem(_player2Item, MenuItem2StartingPosition);
    }

    private void CreateCursor()
    {
        _cursorTankType = TankType.Type0;
        _cursorTankColor = TankColor.Yellow;
        _cursorItem = new FormItem(
            normalTexture: Content.LoadRecoloredTexture($"Sprites/Tank/{_cursorTankType}/Right", $"Sprites/_R/Tank/{_cursorTankColor}"),
            hoverTexture: Content.LoadRecoloredTexture($"Sprites/Tank/{_cursorTankType}/Right", $"Sprites/_R/Tank/{_cursorTankColor}"),
            pressedTexture: Content.LoadRecoloredTexture($"Sprites/Tank/{_cursorTankType}/Right", @"Sprites/_R/Tank/Red"),
            frameTime: 4 * Tank1460Game.OneFrameSpan);

        AddItem(_cursorItem);
        UpdateCursorPosition();
    }

    private void UpdateCursorPosition()
    {
        var x = MenuItemsX - Tile.DefaultWidth - _cursorItem.Bounds.Width;
        var y = (PlayerCount == 1 ? _player1Item.Bounds : _player2Item.Bounds).Center.Y - _cursorItem.Bounds.Height / 2f - 1;
        _cursorItem.Position = new(x, (int)y);
    }

    private void ChangeCursor(TankType? newType = null, TankColor? newColor = null)
    {
        if (newType is null)
        {
            do
                newType = Enum.GetValues<TankType>().GetRandom();
            while (newType == _cursorTankType);
        }

        if (newColor is null)
        {
            do
                newColor = Enum.GetValues<TankColor>().Concat(EnumExtensions.GetCombinedFlagValues<TankColor>(2)).ToArray().GetRandom();
            while (newColor == _cursorTankColor);
        }

        _cursorTankType = newType.Value;
        _cursorTankColor = newColor.Value;

        _cursorItem.ChangeTexture(FormItemVisualStatus.Normal, Content.LoadRecoloredTexture($"Sprites/Tank/{_cursorTankType}/Right", $"Sprites/_R/Tank/{newColor}"));
        _cursorItem.ChangeTexture(FormItemVisualStatus.Hover, Content.LoadRecoloredTexture($"Sprites/Tank/{_cursorTankType}/Right", $"Sprites/_R/Tank/{newColor}"));
        _cursorItem.ChangeTexture(FormItemVisualStatus.Pressed, Content.LoadRecoloredTexture($"Sprites/Tank/{_cursorTankType}/Right", @"Sprites/_R/Tank/Red"));
    }
}