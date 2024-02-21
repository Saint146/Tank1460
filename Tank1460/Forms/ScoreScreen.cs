using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Tank1460.Audio;
using Tank1460.Common.Extensions;
using Tank1460.Common.Level.Object;
using Tank1460.Common.Level.Object.Tank;
using Tank1460.Globals;
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
    private readonly PlayerIndex[] _players;

    private const int StartingPositionX = 0 * Tile.DefaultWidth;
    private const int StartingPositionY = 1 * Tile.DefaultHeight;
    private const int BonusPoints = 1460;

    private static readonly TankType[] TankTypes = { TankType.B0, TankType.B1, TankType.B2, TankType.B3 };

    private TimedActionsQueue _actionsQueue;

    private bool _bonusHasEarnedOneUp;

    // Лейблы.
    private readonly Dictionary<PlayerIndex, Dictionary<TankType, FormTextLabel>> _playersTypedFragsLabels = new();
    private readonly Dictionary<PlayerIndex, Dictionary<TankType, FormTextLabel>> _playersTypedScoreLabels = new();
    private readonly Dictionary<PlayerIndex, (FormTextLabel FragsLabel, FormTextLabel ScoreLabel)> _playersTotalScoreLabels = new();

    private FormTextLabel _bonusLabel1;
    private FormTextLabel _bonusLabel2;

    public ScoreScreen(GameServiceContainer serviceProvider, int levelNumber, int highscore, GameState gameState, LevelStats levelStats, bool showBonus) : base(serviceProvider)
    {
        _levelNumber = levelNumber;
        _highscore = highscore;
        _gameState = gameState;
        _levelStats = levelStats;
        _showBonus = showBonus;
        _players = _gameState.PlayersStates.Keys.ToArray();

        CreateTextAndLabels();
        CreateTankImages();
        _actionsQueue = new TimedActionsQueue(CreateTimedActions());

        SoundPlayer.StopAll();
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        if (_actionsQueue is null)
            return;

        _actionsQueue.Update(gameTime);
        if (!_actionsQueue.IsFinished)
            return;

        _actionsQueue = null;
        Exit();
    }

    protected override void OnClick(FormItem item)
    {
        // Последнее действие - пустое ожидание
        if (_actionsQueue.Actions.Count > 0)
            return;

        Exit();
    }

    protected override void OnInputPressed(PlayerIndex playerIndex, PlayerInputCommands input)
    {
        // Последнее действие - пустое ожидание
        if (_actionsQueue.Actions.Count > 0)
            return;

        if (input.HasOneOfFlags(PlayerInputCommands.Shoot, PlayerInputCommands.ShootTurbo, PlayerInputCommands.Start))
            Exit();
    }

    private void CreateTankImages()
    {
        var isBasicMode = _gameState.PlayersStates.Count <= 2;

        var x = (isBasicMode ? 15 : 16) * Tile.DefaultWidth;
        var y = (int)(10.5 * Tile.DefaultHeight);

        foreach (var tankType in TankTypes)
        {
            var tankTexture = Content.LoadRecoloredTexture($"Sprites/Tank/{tankType}/{ObjectDirection.Up}",
                                                           $"Sprites/_R/Tank/{TankColor.Gray}");

            var animation = new Animation(tankTexture, false);
            AddItem(new FormImage(animation), new Point(x, y));
            y += 3 * Tile.DefaultHeight;
        }
    }

    // TODO: Не придумал, как нормально одженерить. + сверстать на трёх и четырёх
    private void CreateTextAndLabels()
    {
        var whiteFont = Content.LoadFont(@"Sprites/Font/Pixel8", GameColors.White);
        var yellowFont = Content.LoadFont(@"Sprites/Font/Pixel8", GameColors.Yellow);
        var redFont = Content.LoadFont(@"Sprites/Font/Pixel8", GameColors.Red);

        var isBasicMode = _gameState.PlayersStates.Count <= 2;

        var isSecondPlayerPresent = _gameState.PlayersStates.Count > 1;
        var isFourthPlayerPresent = _gameState.PlayersStates.Count > 3;

        var redText = @"
        HI-SCORE



";

        if (isBasicMode)
            redText += $"   Ⅰ-PLAYER{(isSecondPlayerPresent ? "          Ⅱ-PLAYER" : "")}";
        else
            redText += $"   Ⅰ-P     Ⅱ-P     Ⅲ-P{(isFourthPlayerPresent ? "     Ⅳ-P" : "")}";

        redText += @"

";

        var yellowText = @$"
                 {_highscore,7}";

        var text =
            $@"


            STAGE {_levelNumber,2}



";

        var currentPosition = new Point(StartingPositionX, StartingPositionY + (text.CountLines() - 1) * Tile.DefaultHeight);

        // Лейблы с количеством фрагов и очков за каждый из типов танков.
        foreach (var player in _players)
        {
            _playersTypedFragsLabels[player] = new();
            _playersTypedScoreLabels[player] = new();
        }

        foreach (var tankType in TankTypes)
        {
            text += @"


";

            if (isBasicMode)
                text += $"        PTS   ←{(isSecondPlayerPresent ? "  →        PTS" : "")}";
            else
                text += @"               ←  →";

            currentPosition.Y += 3 * Tile.DefaultHeight;

            foreach (var player in _players)
            {
                var xInTiles = isBasicMode
                    ? player == PlayerIndex.One ? 12 : 18
                    : 5 + 8 * (int)player;

                var fragsLabel = new FormTextLabel(whiteFont, 2, 1)
                {
                    Position = currentPosition with
                    {
                        X = currentPosition.X + xInTiles * Tile.DefaultWidth
                    },
                    Visible = false
                };
                AddItem(fragsLabel);
                _playersTypedFragsLabels[player][tankType] = fragsLabel;

                if (!isBasicMode)
                    continue;

                var scoreLabel = new FormTextLabel(whiteFont, 4, 1)
                {
                    Position = currentPosition with
                    {
                        X = currentPosition.X + (player == PlayerIndex.One ? 3 : 21) * Tile.DefaultWidth
                    },
                    Visible = false
                };
                AddItem(scoreLabel);
                _playersTypedScoreLabels[player][tankType] = scoreLabel;
            }
        }

        if (isBasicMode)
            text +=
                @"
            ________
      TOTAL";
        else
            text +=
                @"
     __________________________
TOTAL";

        currentPosition.Y += 2 * Tile.DefaultHeight;

        var totalPlayerFrags = new Dictionary<PlayerIndex, int>();
        // Лейблы с текущим количеством очков и суммарным числом фрагов.
        foreach (var player in _players)
        {
            var scoreLabelXInTiles = isBasicMode
                ? player == PlayerIndex.One ? 4 : 22
                : 8 * (int)player;

            var totalScoreLabel = new FormTextLabel(yellowFont, 7, 1)
            {
                Text = $"{_gameState.PlayersStates[player].Score,7}",
                Position = new Point(x: StartingPositionX + scoreLabelXInTiles * Tile.DefaultWidth,
                                     y: StartingPositionY + (redText.CountLines() - 1) * Tile.DefaultHeight),
            };
            AddItem(totalScoreLabel);


            var totalFrags = _levelStats.PlayerStats[player].BotsDefeated.Values.Sum();
            totalPlayerFrags[player] = totalFrags;

            var fragsLabelXInTiles = isBasicMode
                ? player == PlayerIndex.One ? 12 : 18
                : 5 + 8 * (int)player;

            var totalFragsLabel = new FormTextLabel(whiteFont, 2, 1)
            {
                Text = $"{totalFrags,2}",
                Position = currentPosition with
                {
                    X = currentPosition.X + fragsLabelXInTiles * Tile.DefaultWidth
                },
                Visible = false
            };
            AddItem(totalFragsLabel);

            _playersTotalScoreLabels[player] = (totalFragsLabel, totalScoreLabel);
        }

        if (_showBonus && totalPlayerFrags.Count > 1)
        {
            var playersWithMaxFrags = totalPlayerFrags.Where(x => x.Value == totalPlayerFrags.Values.Max()).ToArray();
            if (playersWithMaxFrags.Length == 1)
            {
                var player = playersWithMaxFrags[0].Key;

                RewardPlayerWithPoints(player, BonusPoints);

                var xInTiles = isBasicMode
                    ? player == PlayerIndex.One ? 3 : 21
                    : 8 * (int)player + (player == PlayerIndex.Four ? 0 : 2);
                var x = StartingPositionX + xInTiles * Tile.DefaultWidth;

                _bonusLabel1 = new FormTextLabel(redFont, 6, 1)
                {
                    Text = @"BONUS‼",
                    Position = new Point(x, currentPosition.Y + 2 * Tile.DefaultHeight),
                    Visible = false
                };
                AddItem(_bonusLabel1);

                _bonusLabel2 = new FormTextLabel(whiteFont, 8, 1)
                {
                    Text = @$"{BonusPoints,4} PTS",
                    Position = new Point(x, currentPosition.Y + 3 * Tile.DefaultHeight),
                    Visible = false
                };
                AddItem(_bonusLabel2);
            }
        }

        // TODO: Создавать текстуру с болванкой всего текста, который не меняется + кэшировать эту картинку
        AddItem(CreateTextImage(text, whiteFont), new Point(StartingPositionX, StartingPositionY));
        AddItem(CreateTextImage(yellowText, yellowFont), new Point(StartingPositionX, StartingPositionY));
        AddItem(CreateTextImage(redText, redFont), new Point(StartingPositionX, StartingPositionY));
    }

    private void RewardPlayerWithPoints(PlayerIndex playerIndex, int points)
    {
        Debug.Assert(points > 0);

        var oneUpsGained = GameRules.GetOneUpsGained(_gameState.PlayersStates[playerIndex].Score, points);
        _gameState.PlayersStates[playerIndex].Score += points;

        if (oneUpsGained > 0)
        {
            _gameState.PlayersStates[playerIndex].LivesRemaining += oneUpsGained;
            _bonusHasEarnedOneUp = true;
        }
    }

    private IEnumerable<(Action Action, double ActionDelay)> CreateTimedActions()
    {
        yield return (() => { }, GameRules.TimeInFrames(27));

        foreach (var tankType in TankTypes)
        {
            var ticksCount = _levelStats.PlayerStats.Max(stats => stats.Value.BotsDefeated.GetValueOrDefault(tankType));
            if (ticksCount == 0)
                ticksCount = 1;

            var scoreMultiplier = GameRules.TankScoreByType[tankType];

            for (var i = 1; i <= ticksCount; i++)
            {
                var tick = i;
                yield return (() =>
                {
                    var needsTickSound = false;
                    foreach (var player in _players)
                    {
                        var fragsLabel = _playersTypedFragsLabels[player][tankType];
                        var scoreLabel = _playersTypedScoreLabels.GetValueOrDefault(player)?.GetValueOrDefault(tankType);

                        var frags = Math.Min(tick, _levelStats.PlayerStats[player].BotsDefeated.GetValueOrDefault(tankType));
                        if (frags > 0)
                            needsTickSound = true;

                        fragsLabel.Text = $"{frags,2}";
                        fragsLabel.Visible = true;

                        if (scoreLabel is null)
                            continue;
                        scoreLabel.Text = $"{frags * scoreMultiplier,4}";
                        scoreLabel.Visible = true;
                    }

                    if (needsTickSound)
                        SoundPlayer.Play(Sound.Tick);
                }, GameRules.TimeInFrames(9));
            }

            yield return (() => { }, GameRules.TimeInFrames(27));
        }

        yield return (() =>
        {
            foreach (var player in _players)
            {
                var (fragsLabel, scoreLabel) = _playersTotalScoreLabels[player];
                fragsLabel.Visible = true;
                scoreLabel.Visible = true;
            }
        }, GameRules.TimeInFrames(36));

        if (_bonusLabel1 is not null && _bonusLabel2 is not null)
            yield return (() =>
            {
                _bonusLabel1.Visible = true;
                _bonusLabel2.Visible = true;

                foreach (var player in _players)
                    _playersTotalScoreLabels[player].ScoreLabel.Text =
                        string.Format($"{{0,{_playersTotalScoreLabels[player].ScoreLabel.SizeInChars.X}}}", _gameState.PlayersStates[player].Score);

                SoundPlayer.Play(_bonusHasEarnedOneUp ? Sound.OneUp : Sound.Reward);
            }, GameRules.TimeInFrames(9));

        // Последнее ожидание можно пропустить.
        yield return (() => { }, GameRules.TimeInFrames(36));
        yield return (() => { }, GameRules.TimeInFrames(108));
    }
}