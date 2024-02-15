using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Tank1460.Common.Extensions;
using Tank1460.LevelObjects.Tiles;

namespace Tank1460;

public class GameOverLevelEffect : LevelEffect
{
    internal PlayerIndex? PlayerIndex { get; }

    protected readonly AnimationPlayer Sprite = new();

    private const double TickTime = Tank1460Game.OneFrameSpan;

    /// <summary>
    /// За сколько надпись доходит до конца при геймовере для игрока.
    /// </summary>
    private const int PlayerTickLength = 50;

    /// <summary>
    /// За сколько надпись доходит до конца при полном геймовере.
    /// </summary>
    private const int GlobalTickLength = 130;

    private const double EffectTime = Tank1460Game.OneFrameSpan * 288;

    private readonly Vector2 _targetPosition;
    private readonly Vector2 _step;
    private Vector2 _position;
    private double _moveTime;
    private double _time;
    private bool _isOnTarget;
    private static readonly Color RedColor = new (0xff0027d1);

    private const string Text = "GAME\nOVER";

    public override bool CanUpdateWhenGameIsPaused => true;

    /// <summary>
    /// Создать летящую надпись GAME OVER. Если передать игрока, то она вылетит для него, если же null, то по центру экрана.
    /// </summary>
    public GameOverLevelEffect(Level level, PlayerIndex? playerIndex = null) : base(level)
    {
        LoadContent(level.Content);
        PlayerIndex = playerIndex;

        // Так как эффект рисуется относительно уровня, то координаты можно не перерассчитывать полностью в процессе.
        int tickLength;
        switch (playerIndex)
        {
            case null:
                // Надпись летит снизу до центра экрана.
                _targetPosition = new Vector2(level.Bounds.Center.X - Sprite.VisibleRect.Width / 2, level.Bounds.Center.Y - Sprite.VisibleRect.Height / 2);
                _position = new Vector2(_targetPosition.X, level.Bounds.Bottom + Tile.DefaultHeight * 3);
                tickLength = GlobalTickLength;
                break;

            // Для всех игроков надпись летит до центра их спавнера.

            case Microsoft.Xna.Framework.PlayerIndex.One:
            case Microsoft.Xna.Framework.PlayerIndex.Three:
                // Для первого и третьего игроков надпись вылетает слева.
                var playerSpawnerPositionCenter = level.GetPlayerSpawner(playerIndex.Value).Bounds.Center;
                _targetPosition = new Vector2(playerSpawnerPositionCenter.X - Sprite.VisibleRect.Width / 2, playerSpawnerPositionCenter.Y - Sprite.VisibleRect.Height / 2);
                _position = new Vector2(1 * Tile.DefaultWidth, _targetPosition.Y);
                tickLength = PlayerTickLength;
                break;

            case Microsoft.Xna.Framework.PlayerIndex.Two:
            case Microsoft.Xna.Framework.PlayerIndex.Four:
                // Для второго и четвертого игроков надпись вылетает справа.
                playerSpawnerPositionCenter = level.GetPlayerSpawner(playerIndex.Value).Bounds.Center;
                _targetPosition = new Vector2(playerSpawnerPositionCenter.X - Sprite.VisibleRect.Width / 2, playerSpawnerPositionCenter.Y - Sprite.VisibleRect.Height / 2);
                _position = new Vector2(level.Bounds.Right - 1 * Tile.DefaultWidth - Sprite.VisibleRect.Width, _targetPosition.Y);
                tickLength = PlayerTickLength;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(playerIndex), playerIndex, null);
        }

        _step = (_targetPosition - _position) / tickLength;
    }

    public override void Update(GameTime gameTime)
    {
        _time += gameTime.ElapsedGameTime.TotalSeconds;
        if (_time >= EffectTime)
            Remove();

        if (_isOnTarget)
            return;

        _moveTime += gameTime.ElapsedGameTime.TotalSeconds;

        while (_moveTime > TickTime)
        {
            _moveTime -= TickTime;

            if (_position.TryStep(_step, _targetPosition))
                continue;

            _isOnTarget = true;
            _position = _targetPosition;
            return;
        }
    }

    public override void Draw(SpriteBatch spriteBatch, Rectangle levelBounds)
    {
        Sprite.Draw(spriteBatch, _position);
    }

    private void LoadContent(ContentManagerEx content)
    {
        var font = content.LoadFont(@"Sprites/Font/Pixel8", RedColor);
        var textTexture = font.CreateTexture(Text);

        var animation = new Animation(textTexture, new[] { double.MaxValue }, false);
        Sprite.PlayAnimation(animation);
    }
}