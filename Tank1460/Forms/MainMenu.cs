using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using MonoGame.Extended;
using Tank1460.Common.Extensions;
using Tank1460.Common.Level.Object.Tank;
using Tank1460.Globals;
using Tank1460.Input;
using Tank1460.LevelObjects.Tanks;
using Tank1460.LevelObjects.Tiles;

namespace Tank1460.Forms;

internal class MainMenu : Form
{
    public int PlayerCount { get; private set; }

    private readonly Range<int> _playerCountRange;

    private const string TitleText = "TANK\n1460";
    private const string MenuItem1PlayerText = "1 PLAYER";
    private const string MenuItemMultiPlayersText = "{0} PLAYERS";

    private const int MenuItemsX = 11 * Tile.DefaultWidth;
    private const int MenuItem1Y = 15 * Tile.DefaultHeight;
    private const int MenuItemsYStep = 2 * Tile.DefaultHeight;

    private readonly Dictionary<int, FormButton> _playerButtons = new();

    private FormButton _cursor;
    private TankType _cursorTankType;
    private TankColor _cursorTankColor;
    private static readonly TankType[] AllPossibleCursorTankTypes = Enum.GetValues<TankType>().ToArray();
    private static readonly TankColor[] AllPossibleCursorTankColors = Enum.GetValues<TankColor>()
                                                                    .Where(color => color != TankColor.Red)
                                                                    .Concat(EnumExtensions.GetCombinedFlagValues<TankColor>(2))
                                                                    .ToArray();


    public MainMenu(GameServiceContainer serviceProvider, int playerCount, Range<int> playerCountRange) : base(serviceProvider)
    {
        _playerCountRange = playerCountRange;
        PlayerCount = playerCount;

        CreateMenuItems();
        CreateTitle();
        CreateCursor();
    }

    protected override void OnClick(FormItem item)
    {
        if (item == _cursor)
        {
            ChangeCursor();
            return;
        }

        if (item is not FormButton button || !_playerButtons.ContainsValue(button)) return;

        PlayerCount = _playerButtons.Keys.Single(i => _playerButtons[i] == button);
        Exit();
    }

    protected override void OnInputPressed(PlayerIndex playerIndex, PlayerInputCommands input)
    {
        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch (input)
        {
            case PlayerInputCommands.Up:
                PlayerCount = _playerCountRange.PrevLooping(PlayerCount);
                UpdateCursorPosition();
                break;

            case PlayerInputCommands.Down:
                PlayerCount = _playerCountRange.NextLooping(PlayerCount);
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
        var topButton = _playerButtons.First().Value;

        var titleFont = Content.LoadOrCreateCustomFont("TitleFont", () =>
        {
            var commonFont = Content.LoadFont(@"Sprites/Font/Pixel8");
            var titlePatternTexture = Content.Load<Texture2D>(@"Sprites/Hud/Pattern1");
            return commonFont.CreateFontUsingTextureAsPattern(titlePatternTexture);
        });

        var title = CreateTextImage(TitleText, titleFont);
        AddItem(title, new Point(x: topButton.Position.X + topButton.Bounds.Width / 2 - title.Bounds.Width / 2,
                                 y: topButton.Position.Y - Tile.DefaultHeight * 2 - title.Bounds.Height));
    }

    private void CreateMenuItems()
    {
        var first = _playerCountRange.Min;
        foreach (var i in _playerCountRange)
        {
            var text = i == 1 ? MenuItem1PlayerText : string.Format(MenuItemMultiPlayersText, i);

            var button = _playerButtons[i] = CreateTextButton(text, GameColors.White, GameColors.Curtain);

            AddItem(button, new Point(MenuItemsX, MenuItem1Y + (i - first) * MenuItemsYStep));
        }
    }

    private void CreateCursor()
    {
        _cursorTankType = TankType.P0;
        _cursorTankColor = TankColor.Yellow;
        _cursor = new FormButton(
            normalTexture: Content.LoadRecoloredTexture($"Sprites/Tank/{_cursorTankType}/Right", $"Sprites/_R/Tank/{_cursorTankColor}"),
            hoverTexture: Content.LoadRecoloredTexture($"Sprites/Tank/{_cursorTankType}/Right", $"Sprites/_R/Tank/{_cursorTankColor}"),
            pressedTexture: Content.LoadRecoloredTexture($"Sprites/Tank/{_cursorTankType}/Right", @"Sprites/_R/Tank/Red"),
            frameTime: GameRules.TimeInFrames(4));

        AddItem(_cursor);
        UpdateCursorPosition();
    }

    private void UpdateCursorPosition()
    {
        var activeButton = _playerButtons[PlayerCount];

        var x = MenuItemsX - Tile.DefaultWidth - _cursor.Bounds.Width;
        var y = activeButton.Bounds.Center.Y - _cursor.Bounds.Height / 2f - 1;
        _cursor.Position = new(x, (int)y);
    }

    /// <summary>
    /// Выбрать для курсора новый случайный цвет и тип танка.
    /// </summary>
    private void ChangeCursor()
    {
        _cursorTankType = AllPossibleCursorTankTypes.Where(type => type != _cursorTankType).ToArray().GetRandom();
        _cursorTankColor = AllPossibleCursorTankColors.Where(color => color != _cursorTankColor).ToArray().GetRandom();

        _cursor.ChangeTexture(FormItemVisualStatus.Normal,
                              Content.LoadRecoloredTexture($"Sprites/Tank/{_cursorTankType}/Right", $"Sprites/_R/Tank/{_cursorTankColor}"));
        _cursor.ChangeTexture(FormItemVisualStatus.Hover,
                              Content.LoadRecoloredTexture($"Sprites/Tank/{_cursorTankType}/Right", $"Sprites/_R/Tank/{_cursorTankColor}"));
        _cursor.ChangeTexture(FormItemVisualStatus.Pressed,
                              Content.LoadRecoloredTexture($"Sprites/Tank/{_cursorTankType}/Right", @"Sprites/_R/Tank/Red"));
    }
}