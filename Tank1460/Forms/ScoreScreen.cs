using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Tank1460.Common.Extensions;
using Tank1460.Common.Level.Object;
using Tank1460.Common.Level.Object.Tank;
using Tank1460.Input;
using Tank1460.LevelObjects.Tanks;
using Tank1460.LevelObjects.Tiles;

namespace Tank1460.Forms;

internal class ScoreScreen : Form
{
    private readonly int _levelNumber;
    private readonly GameState _gameState;
    private readonly LevelStats _levelStats;
    private readonly bool _showBonus;
    private const int StartingPositionX = 3 * Tile.DefaultWidth;
    private const int StartingPositionY = 1 * Tile.DefaultHeight;

    private static readonly Color WhiteColor = Color.White;
    private static readonly Color YellowColor = new(0xff3898fc);
    private static readonly Color RedColor = new(0xff0027d1);
    private static readonly TankType[] TankTypes = { TankType.Type4, TankType.Type5, TankType.Type6, TankType.Type7 };

    public ScoreScreen(ContentManagerEx content, int levelNumber, GameState gameState, LevelStats levelStats, bool showBonus) : base(content)
    {
        _levelNumber = levelNumber;
        _gameState = gameState;
        _levelStats = levelStats;
        _showBonus = showBonus;

        if (_gameState.PlayersStates.Count == 1)
            CreateOnePlayerText();
        else
            CreateTwoPlayersText();

        CreateTankImages();
    }

    protected override void OnClick(FormItem item)
    {
        Exit();
    }

    protected override void OnPress(PlayerIndex playerIndex, PlayerInputCommands input)
    {
        if (input.HasOneOfFlags(PlayerInputCommands.Shoot, PlayerInputCommands.ShootTurbo, PlayerInputCommands.Start))
            Exit();
    }

    private void CreateTankImages()
    {
        // TODO: Разобраться, почему так.
        var x = (int)(13.5 * Tile.DefaultWidth);
        var y = 10 * Tile.DefaultHeight;

        foreach (var tankType in TankTypes)
        {
            var tankTexture = Content.LoadRecoloredTexture($"Sprites/Tank/Type{(int)tankType}/{ObjectDirection.Up}",
                                                           $"Sprites/_R/Tank/{TankColor.Gray}");

            var animation = new Animation(tankTexture, false);
            AddItem(new FormImage(animation), new Point(x, y));
            y += 3 * Tile.DefaultHeight;
        }
    }

    // TODO: Не придумал, как одженерить. + сверстать на трёх и четырёх
    private void CreateOnePlayerText()
    {
        var redText = @"
     HI-SCORE



│-PLAYER
";

        var yellowText = @$"
                20000





{_gameState.PlayersStates[PlayerIndex.One].Score,8}";

        var text =
            $@"


         STAGE {_levelNumber,2}



";

        var p1TotalFrags = 0;
        foreach (var tankType in TankTypes)
        {
            var p1Frags = _levelStats.PlayerStats[PlayerIndex.One].BotsDefeated.GetValueOrDefault(tankType);
            var p1Score = p1Frags * GameRules.TankScoreByType[tankType];

            p1TotalFrags += p1Frags;

            text += $@"


{p1Score,4} PTS {p1Frags,2}←";
        }

        text +=
            $@"
         ________
   TOTAL {p1TotalFrags,2}";

        var whiteFont = Content.LoadFont(@"Sprites/Font/Pixel8", WhiteColor);
        var yellowFont = Content.LoadFont(@"Sprites/Font/Pixel8", YellowColor);
        var redFont = Content.LoadFont(@"Sprites/Font/Pixel8", RedColor);

        // TODO: Создавать текстуру с болванкой всего текста, который не меняется + кэшировать эту картинку
        AddItem(CreateTextLabel(text, whiteFont, new Point(StartingPositionX, StartingPositionY)));
        AddItem(CreateTextLabel(yellowText, yellowFont, new Point(StartingPositionX, StartingPositionY)));
        AddItem(CreateTextLabel(redText, redFont, new Point(StartingPositionX, StartingPositionY)));
    }

    private void CreateTwoPlayersText()
    {
        var redText = @"
     HI-SCORE



│-PLAYER          ║-PLAYER
";

        var yellowText = @$"
                20000





{_gameState.PlayersStates[PlayerIndex.One].Score,8}          {_gameState.PlayersStates[PlayerIndex.Two].Score,8}";

        var text =
            $@"


         STAGE {_levelNumber,2}



";

        var p1TotalFrags = 0;
        var p2TotalFrags = 0;
        foreach (var tankType in TankTypes)
        {
            var p1Frags = _levelStats.PlayerStats[PlayerIndex.One].BotsDefeated.GetValueOrDefault(tankType);
            var p2Frags = _levelStats.PlayerStats[PlayerIndex.Two].BotsDefeated.GetValueOrDefault(tankType);
            var p1Score = p1Frags * GameRules.TankScoreByType[tankType];
            var p2Score = p2Frags * GameRules.TankScoreByType[tankType];

            p1TotalFrags += p1Frags;
            p2TotalFrags += p2Frags;

            text += $@"


{p1Score,4} PTS {p1Frags,2}←  →{p2Frags,2} {p2Score,4} PTS";
        }

        text +=
            $@"
         ________
   TOTAL {p1TotalFrags,2}    {p2TotalFrags,2}";

        if (_showBonus && p1TotalFrags != p2TotalFrags)
        {
            var indent = p1TotalFrags > p2TotalFrags ? string.Empty : new string(' ', 18);

            text += $@"


{indent}1460 PTS";

            redText += new string('\n', 18) + $"{indent}BONUS‼";
        }

        var whiteFont = Content.LoadFont(@"Sprites/Font/Pixel8", WhiteColor);
        var yellowFont = Content.LoadFont(@"Sprites/Font/Pixel8", YellowColor);
        var redFont = Content.LoadFont(@"Sprites/Font/Pixel8", RedColor);

        // TODO: Создавать текстуру с болванкой всего текста, который не меняется + кэшировать эту картинку
        AddItem(CreateTextLabel(text, whiteFont, new Point(StartingPositionX, StartingPositionY)));
        AddItem(CreateTextLabel(yellowText, yellowFont, new Point(StartingPositionX, StartingPositionY)));
        AddItem(CreateTextLabel(redText, redFont, new Point(StartingPositionX, StartingPositionY)));
    }
}