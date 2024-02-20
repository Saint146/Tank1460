using Microsoft.Xna.Framework;
using MonoGame.Extended;
using System;
using System.Diagnostics;
using Tank1460.Audio;
using Tank1460.Common.Extensions;
using Tank1460.Globals;
using Tank1460.Input;
using Tank1460.LevelObjects.Tiles;

namespace Tank1460.Forms;

internal class LevelSelectScreen : Form
{
    public int LevelNumber
    {
        get => _levelNumber;
        set
        {
            _levelNumber = value;
            RefreshLabelText();
        }
    }

    private readonly Range<int> _levelRange;

    private bool _closing;

    private TimedActionsQueue _closingTimer;
    private int _levelNumber;
    private FormTextLabel _levelLabel;
    private FormButton _leftButton;
    private FormButton _rightButton;

    private const string LabelFormat = @"STAGE {0,2}";
    private static readonly Point LabelPosition = new(x: 12 * Tile.DefaultWidth,
                                                      y: 13 * Tile.DefaultHeight);

    public LevelSelectScreen(GameServiceContainer serviceProvider, int levelNumber, Range<int> levelRange, bool skipSelecting) : base(serviceProvider)
    {
        if (!levelRange.Contains(levelNumber))
            throw new ArgumentOutOfRangeException(nameof(levelNumber));

        BackColor = GameColors.Curtain;
        CreateControls();

        _levelRange = levelRange;
        LevelNumber = levelNumber;

        if (skipSelecting)
            StartClosing();
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        if (_closing)
            _closingTimer.Update(gameTime);
    }

    protected override void OnClick(FormItem item)
    {
        if (_closing)
            return;

        if (item == _leftButton)
            LevelNumber = _levelRange.PrevLooping(LevelNumber);

        if (item == _rightButton)
            LevelNumber = _levelRange.NextLooping(LevelNumber);

        if (item == _levelLabel)
            StartClosing();
    }

    protected override void OnInputPressed(PlayerIndex playerIndex, PlayerInputCommands input)
    {
        if (_closing)
            return;

        if (input.HasFlag(PlayerInputCommands.Left))
            LevelNumber = _levelRange.PrevLooping(LevelNumber);

        if (input.HasFlag(PlayerInputCommands.Right))
            LevelNumber = _levelRange.NextLooping(LevelNumber);

        if (input.HasOneOfFlags(PlayerInputCommands.Shoot, PlayerInputCommands.ShootTurbo, PlayerInputCommands.Start))
            StartClosing();
    }

    private void CreateControls()
    {
        var font = Content.LoadFont(@"Sprites/Font/Pixel8", GameColors.Black);

        _levelLabel = new FormTextLabel(font,
                                        new Point(x: string.Format(LabelFormat, 0).Length,
                                                  y: 1));
        AddItem(_levelLabel, LabelPosition);

        _leftButton = CreateTextButton(text: "←",
                                       normalColor: GameColors.Black,
                                       shadowColor: GameColors.BlackTextShadow,
                                       margins: new Point(font.CharWidth * 3, font.CharHeight * 3));
        AddItem(_leftButton,
                new Point(x: _levelLabel.Bounds.X - _leftButton.Bounds.Width,
                          y: _levelLabel.Bounds.Center.Y - _leftButton.Bounds.Height / 2));

        _rightButton = CreateTextButton(text: "→",
                                        normalColor: GameColors.Black,
                                        shadowColor: GameColors.BlackTextShadow,
                                        margins: new Point(font.CharWidth * 3, font.CharHeight * 3));
        AddItem(_rightButton,
                new Point(x: _levelLabel.Bounds.Right + 1,
                          y: _levelLabel.Bounds.Center.Y - _rightButton.Bounds.Height / 2));
    }

    private void RefreshLabelText()
    {
        _levelLabel.Text = string.Format(LabelFormat, LevelNumber);
    }

    private void StartClosing()
    {
        Debug.Assert(!_closing);

#if !DEBUG
        SoundPlayer.Play(Sound.Intro);
#endif

        _leftButton.Visible = _rightButton.Visible = false;

        _closing = true;
        _closingTimer = new TimedActionsQueue((Exit, GameRules.TimeInFrames(64)));
    }
}