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

    public bool AiEnabled { get; private set; }

    public bool ExitSelected { get; private set; }

    private readonly Range<int> _playerCountRange;

    private const string TitleText = "TANK\n1460";
    private const string MenuItem1PlayerText = "1 PLAYER";
    private const string MenuItemMultiPlayersTextFormat = "{0} PLAYERS";
    private const string AiEnabledTextFormat = "{0} AI ENABLED";
    private const string ExitButtonText = "QUIT    ";

    private const int MenuItemsX = 11 * Tile.DefaultWidth;
    private const int MenuItem1Y = 11 * Tile.DefaultHeight;
    private const int MenuItemsYStep = 2 * Tile.DefaultHeight;

    private readonly Dictionary<int, FormButton> _playerButtons = new();
    private FormTextLabel _aiEnabledLabel;
    private FormButton _exitButton;

    private int _cursorItemIndex;
    private readonly List<FormItem> _cursorItems = new();

    private FormItem CursorItem => _cursorItems[_cursorItemIndex];

    private FormButton _cursor;
    private TankType _cursorTankType;
    private TankColor _cursorTankColor;
    private static readonly TankType[] AllPossibleCursorTankTypes = Enum.GetValues<TankType>().ToArray();
    private static readonly TankColor[] AllPossibleCursorTankColors = Enum.GetValues<TankColor>()
                                                                    .Where(color => color != TankColor.Red)
                                                                    .Concat(EnumExtensions.GetCombinedFlagValues<TankColor>(2))
                                                                    .ToArray();

    public MainMenu(GameServiceContainer serviceProvider, int playerCount, Range<int> playerCountRange, bool aiEnabled) : base(serviceProvider)
    {
        _playerCountRange = playerCountRange;
        PlayerCount = playerCount;
        AiEnabled = aiEnabled;

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

        if (item == _aiEnabledLabel)
        {
            AiEnabled = !AiEnabled;
            UpdateAiEnabledText();
            return;
        }

        if (item == _exitButton)
        {
            ExitSelected = true;
            Exit();
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
                _cursorItemIndex--;
                if (_cursorItemIndex < 0)
                    _cursorItemIndex = _cursorItems.Count - 1;

                UpdateCursorPosition();
                break;

            case PlayerInputCommands.Down:
                _cursorItemIndex++;
                if (_cursorItemIndex >= _cursorItems.Count)
                    _cursorItemIndex = 0;

                UpdateCursorPosition();
                break;

            case PlayerInputCommands.ShootTurbo:
            case PlayerInputCommands.Shoot:
            case PlayerInputCommands.Start:
                OnClick(CursorItem);
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
            var text = i == 1 ? MenuItem1PlayerText : string.Format(MenuItemMultiPlayersTextFormat, i);

            var button = _playerButtons[i] = CreateTextButton(text, GameColors.White, GameColors.Curtain);

            AddItem(button,
                    new Point(x: MenuItemsX,
                              y: MenuItem1Y + (i - first) * MenuItemsYStep));
            _cursorItems.Add(button);

            if (i == PlayerCount)
                _cursorItemIndex = _cursorItems.Count - 1;
        }

        var font = Content.LoadFont(@"Sprites/Font/Pixel8", GameColors.White);
        _aiEnabledLabel = new FormTextLabel(font, string.Format(AiEnabledTextFormat, ' ').Length, 1);
        AddItem(_aiEnabledLabel,
                new Point(x: MenuItemsX + font.CharHeight / 2,
                          y: MenuItem1Y + (_playerCountRange.Length() + 1) * MenuItemsYStep + font.CharWidth / 2));
        UpdateAiEnabledText();
        _cursorItems.Add(_aiEnabledLabel);

        _exitButton = CreateTextButton(ExitButtonText, GameColors.White, GameColors.Curtain);
        AddItem(_exitButton,
                new Point(x: MenuItemsX,
                          y: MenuItem1Y + (_playerCountRange.Length() + 3) * MenuItemsYStep));
        _cursorItems.Add(_exitButton);
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
        // TODO: Убрать всё это в сеттер CursorItem.
        var (newPlayerCount, playerButton) = _playerButtons.SingleOrDefault(x => x.Value == CursorItem);
        if (playerButton is not null)
            PlayerCount = newPlayerCount;

        var x = CursorItem.Position.X - Tile.DefaultWidth - _cursor.Bounds.Width;
        var y = CursorItem.Bounds.Center.Y - _cursor.Bounds.Height / 2f - 1;
        _cursor.Position = new Point(x, (int)y);
    }

    private void UpdateAiEnabledText()
    {
        _aiEnabledLabel.Text = string.Format(AiEnabledTextFormat, AiEnabled ? "☑" : "☐");
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