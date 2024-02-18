using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Tank1460.Common.Extensions;
using Tank1460.Common.Level.Object.Tank;
using Tank1460.Input;
using Tank1460.LevelObjects.Tanks;
using Tank1460.LevelObjects.Tiles;

namespace Tank1460.Forms;

internal class MainMenu : Form
{
    public int PlayerCount { get; private set; }

    public int LevelNumber { get; private set; }

    private const string TitleText = "TANK\n1460";
    private const string MenuItem1PlayerText = "1 PLAYER";
    private const string MenuItem2PlayersText = "2 PLAYERS";
    private const int MenuItemsX = 11 * Tile.DefaultWidth;
    private const int MenuItem1Y = 15 * Tile.DefaultHeight;
    private const int MenuItem2Y = 17 * Tile.DefaultHeight;
    private static readonly Point MenuItem1StartingPosition = new(MenuItemsX, MenuItem1Y);
    private static readonly Point MenuItem2StartingPosition = new(MenuItemsX, MenuItem2Y);

    private FormButton _player1Button;
    private FormButton _player2Button;
    private FormButton _cursor;

    private TankType _cursorTankType;
    private TankColor _cursorTankColor;

    public MainMenu(ContentManagerEx content, int playerCount, int levelNumber) : base(content)
    {
        PlayerCount = playerCount;
        LevelNumber = levelNumber;

        CreateMenuItems();
        CreateTitle();
        CreateCursor();
    }

    protected override void OnUpdate(GameTime gameTime)
    {
    }

    protected override void OnClick(FormItem item)
    {
        if (item == _cursor)
        {
            ChangeCursor();
            return;
        }

        if (item != _player1Button && item != _player2Button) return;

        PlayerCount = item == _player1Button ? 1 : 2;
        Exit();
    }

    protected override void OnPress(PlayerIndex playerIndex, PlayerInputCommands input)
    {
        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch (input)
        {
            case PlayerInputCommands.Up:
            case PlayerInputCommands.Down:
                PlayerCount = 3 - PlayerCount;
                UpdateCursorPosition();
                break;

            case PlayerInputCommands.ShootTurbo:
            case PlayerInputCommands.Shoot:
            case PlayerInputCommands.Start:
                Exit();
                break;

        }
    }

    private void CreateTitle()
    {
        var titleFont = Content.LoadOrCreateCustomFont("TitleFont", () =>
        {
            var commonFont = Content.LoadFont(@"Sprites/Font/Pixel8");
            var titlePatternTexture = Content.Load<Texture2D>(@"Sprites/Hud/Pattern1");
            return commonFont.CreateFontUsingTextureAsPattern(titlePatternTexture);
        });

        var title = CreateTextImage(TitleText, titleFont);
        AddItem(title);
        title.Position = new Point(x: _player1Button.Position.X + _player1Button.Bounds.Width / 2 - title.Bounds.Width / 2,
                                   y: _player1Button.Position.Y - Tile.DefaultHeight * 2 - title.Bounds.Height);
    }

    private void CreateMenuItems()
    {
        _player1Button = CreateTextButton(MenuItem1PlayerText);
        _player2Button = CreateTextButton(MenuItem2PlayersText);

        AddItem(_player1Button, MenuItem1StartingPosition);
        AddItem(_player2Button, MenuItem2StartingPosition);
    }

    private void CreateCursor()
    {
        _cursorTankType = TankType.Type0;
        _cursorTankColor = TankColor.Yellow;
        _cursor = new FormButton(
            normalTexture: Content.LoadRecoloredTexture($"Sprites/Tank/{_cursorTankType}/Right", $"Sprites/_R/Tank/{_cursorTankColor}"),
            hoverTexture: Content.LoadRecoloredTexture($"Sprites/Tank/{_cursorTankType}/Right", $"Sprites/_R/Tank/{_cursorTankColor}"),
            pressedTexture: Content.LoadRecoloredTexture($"Sprites/Tank/{_cursorTankType}/Right", @"Sprites/_R/Tank/Red"),
            frameTime: 4 * Tank1460Game.OneFrameSpan);

        AddItem(_cursor);
        UpdateCursorPosition();
    }

    private void UpdateCursorPosition()
    {
        var x = MenuItemsX - Tile.DefaultWidth - _cursor.Bounds.Width;
        var y = (PlayerCount == 1 ? _player1Button.Bounds : _player2Button.Bounds).Center.Y - _cursor.Bounds.Height / 2f - 1;
        _cursor.Position = new(x, (int)y);
    }

    /// <summary>
    /// Выбрать для курсора новый случайный цвет и тип танка.
    /// </summary>
    private void ChangeCursor()
    {
        TankType newType;
        do
            newType = Enum.GetValues<TankType>().GetRandom();
        while (newType == _cursorTankType);

        TankColor newColor;
        do
            newColor = Enum.GetValues<TankColor>().Concat(EnumExtensions.GetCombinedFlagValues<TankColor>(2)).ToArray().GetRandom();
        while (newColor == _cursorTankColor);

        _cursorTankType = newType;
        _cursorTankColor = newColor;

        _cursor.ChangeTexture(FormItemVisualStatus.Normal, Content.LoadRecoloredTexture($"Sprites/Tank/{_cursorTankType}/Right", $"Sprites/_R/Tank/{_cursorTankColor}"));
        _cursor.ChangeTexture(FormItemVisualStatus.Hover, Content.LoadRecoloredTexture($"Sprites/Tank/{_cursorTankType}/Right", $"Sprites/_R/Tank/{_cursorTankColor}"));
        _cursor.ChangeTexture(FormItemVisualStatus.Pressed, Content.LoadRecoloredTexture($"Sprites/Tank/{_cursorTankType}/Right", @"Sprites/_R/Tank/Red"));
    }
}