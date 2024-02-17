using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
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
    private readonly int _highscore;
    private readonly GameState _gameState;
    private readonly LevelStats _levelStats;
    private readonly bool _showBonus;
    private const int StartingPositionX = 3 * Tile.DefaultWidth;
    private const int StartingPositionY = 1 * Tile.DefaultHeight;

    private static readonly Color WhiteColor = Color.White;
    private static readonly Color YellowColor = new(0xff3898fc);
    private static readonly Color RedColor = new(0xff0027d1);
    private static readonly TankType[] TankTypes = { TankType.Type4, TankType.Type5, TankType.Type6, TankType.Type7 };

    public ScoreScreen(ContentManagerEx content, int levelNumber, int highscore, GameState gameState, LevelStats levelStats, bool showBonus) : base(content)
    {
        _levelNumber = levelNumber;
        _highscore = highscore;
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
        var x = 15 * Tile.DefaultWidth;
        var y = (int)(10.5 * Tile.DefaultHeight);

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
        var whiteFont = Content.LoadFont(@"Sprites/Font/Pixel8", WhiteColor);
        var yellowFont = Content.LoadFont(@"Sprites/Font/Pixel8", YellowColor);
        var redFont = Content.LoadFont(@"Sprites/Font/Pixel8", RedColor);

        var redText = @"
     HI-SCORE



│-PLAYER

";

        var yellowText = @$"
              {_highscore}";

        var p1ScoreLabel = new FormTextLabel(yellowFont, 8, 1)
        {
            Text = $"{_gameState.PlayersStates[PlayerIndex.One].Score,8}",
            Position = new Point(StartingPositionX, StartingPositionY + (redText.CountLines() - 1) * Tile.DefaultHeight)
        };
        AddItem(p1ScoreLabel);

        var text =
            $@"


         STAGE {_levelNumber,2}



";

        var p1Frags = TankTypes.ToDictionary(type => type, type => _levelStats.PlayerStats[PlayerIndex.One].BotsDefeated.GetValueOrDefault(type));
        var p1Scores = p1Frags.ToDictionary(x => x.Key, x => x.Value * GameRules.TankScoreByType[x.Key]);
        var p1TotalFrags = p1Frags.Values.Sum();

        var currentPosition = new Point(StartingPositionX, StartingPositionY + (text.CountLines() - 1) * Tile.DefaultHeight);
        foreach (var tankType in TankTypes)
        {
            text += @"


     PTS   ←";
            currentPosition.Y += 3 * Tile.DefaultHeight;

            AddItem(new FormTextLabel(whiteFont, 4, 1)
            {
                Text = $"{p1Scores[tankType],4}",
                Position = currentPosition
            });

            AddItem(new FormTextLabel(whiteFont, 2, 1)
            {
                Text = $"{p1Frags[tankType],2}",
                Position = currentPosition with { X = currentPosition.X + 9 * Tile.DefaultWidth }
            });
        }

        text +=
            @"
         ________
   TOTAL";
        currentPosition.Y += 2 * Tile.DefaultHeight;

        AddItem(new FormTextLabel(whiteFont, 2, 1)
        {
            Text = $"{p1TotalFrags,2}",
            Position = currentPosition with { X = currentPosition.X + 9 * Tile.DefaultWidth }
        });

        // TODO: Создавать текстуру с болванкой всего текста, который не меняется + кэшировать эту картинку
        AddItem(CreateTextImage(text, whiteFont), new Point(StartingPositionX, StartingPositionY));
        AddItem(CreateTextImage(yellowText, yellowFont), new Point(StartingPositionX, StartingPositionY));
        AddItem(CreateTextImage(redText, redFont), new Point(StartingPositionX, StartingPositionY));
    }

    private void CreateTwoPlayersText()
    {
        var whiteFont = Content.LoadFont(@"Sprites/Font/Pixel8", WhiteColor);
        var yellowFont = Content.LoadFont(@"Sprites/Font/Pixel8", YellowColor);
        var redFont = Content.LoadFont(@"Sprites/Font/Pixel8", RedColor);

        var redText = @"
     HI-SCORE



│-PLAYER          ║-PLAYER

";

        var yellowText = @$"
              {_highscore}";

        var p1ScoreLabel = new FormTextLabel(yellowFont, 8, 1)
        {
            Text = $"{_gameState.PlayersStates[PlayerIndex.One].Score,8}",
            Position = new Point(StartingPositionX, StartingPositionY + (redText.CountLines() - 1) * Tile.DefaultHeight)
        };
        AddItem(p1ScoreLabel);

        var p2ScoreLabel = new FormTextLabel(yellowFont, 8, 1)
        {
            Text = $"{_gameState.PlayersStates[PlayerIndex.Two].Score,8}",
            Position = new Point(StartingPositionX + 18 * Tile.DefaultWidth, StartingPositionY + (redText.CountLines() - 1) * Tile.DefaultHeight)
        };
        AddItem(p1ScoreLabel);

        var text =
            $@"


         STAGE {_levelNumber,2}



";

        var p1Frags = TankTypes.ToDictionary(type => type, type => _levelStats.PlayerStats[PlayerIndex.One].BotsDefeated.GetValueOrDefault(type));
        var p1Scores = p1Frags.ToDictionary(x => x.Key, x => x.Value * GameRules.TankScoreByType[x.Key]);
        var p1TotalFrags = p1Frags.Values.Sum();

        var p2Frags = TankTypes.ToDictionary(type => type, type => _levelStats.PlayerStats[PlayerIndex.Two].BotsDefeated.GetValueOrDefault(type));
        var p2Scores = p2Frags.ToDictionary(x => x.Key, x => x.Value * GameRules.TankScoreByType[x.Key]);
        var p2TotalFrags = p2Frags.Values.Sum();

        var currentPosition = new Point(StartingPositionX, StartingPositionY + (text.CountLines() - 1) * Tile.DefaultHeight);
        foreach (var tankType in TankTypes)
        {
            text += @"


     PTS   ←  →         PTS";
            currentPosition.Y += 3 * Tile.DefaultHeight;

            AddItem(new FormTextLabel(whiteFont, 4, 1)
            {
                Text = $"{p1Scores[tankType],4}",
                Position = currentPosition
            });

            AddItem(new FormTextLabel(whiteFont, 2, 1)
            {
                Text = $"{p1Frags[tankType],2}",
                Position = currentPosition with { X = currentPosition.X + 9 * Tile.DefaultWidth }
            });

            AddItem(new FormTextLabel(whiteFont, 4, 1)
            {
                Text = $"{p2Scores[tankType],4}",
                Position = currentPosition with { X = currentPosition.X + 18 * Tile.DefaultWidth }
            });

            AddItem(new FormTextLabel(whiteFont, 2, 1)
            {
                Text = $"{p2Frags[tankType],2}",
                Position = currentPosition with { X = currentPosition.X + 15 * Tile.DefaultWidth }
            });
        }

        text +=
            @"
         ________
   TOTAL";
        currentPosition.Y += 2 * Tile.DefaultHeight;

        AddItem(new FormTextLabel(whiteFont, 2, 1)
        {
            Text = $"{p1TotalFrags,2}",
            Position = currentPosition with { X = currentPosition.X + 9 * Tile.DefaultWidth }
        });

        AddItem(new FormTextLabel(whiteFont, 2, 1)
        {
            Text = $"{p1TotalFrags,2}",
            Position = currentPosition with { X = currentPosition.X + 15 * Tile.DefaultWidth }
        });

        if (_showBonus && p1TotalFrags != p2TotalFrags)
        {
            var x = currentPosition.X + (p1TotalFrags > p2TotalFrags ? 0 : 18 * Tile.DefaultWidth);

            AddItem(new FormTextLabel(redFont, 6, 1)
            {
                Text = @"BONUS‼",
                Position = new Point(x, currentPosition.Y + 2 * Tile.DefaultHeight)
            });

            AddItem(new FormTextLabel(whiteFont, 8, 1)
            {
                Text = @"1460 PTS",
                Position = new Point(x, currentPosition.Y + 3 * Tile.DefaultHeight)
            });
        }

        // TODO: Создавать текстуру с болванкой всего текста, который не меняется + кэшировать эту картинку
        AddItem(CreateTextImage(text, whiteFont), new Point(StartingPositionX, StartingPositionY));
        AddItem(CreateTextImage(yellowText, yellowFont), new Point(StartingPositionX, StartingPositionY));
        AddItem(CreateTextImage(redText, redFont), new Point(StartingPositionX, StartingPositionY));
    }
}