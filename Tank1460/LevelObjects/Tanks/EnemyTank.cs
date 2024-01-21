using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Diagnostics;
using Microsoft.Xna.Framework.Graphics;
using Tank1460.Extensions;
using MonoGame.Extended;
using System.Collections.Generic;
using Tank1460.LevelObjects.Tiles;

namespace Tank1460.LevelObjects.Tanks;

public class EnemyTank : Tank
{
    public int PeriodIndex { get; set; }

    protected override int[] SpawnAnimationTimesInFrames() => new[] { 4, 4, 4, 6, 4, 4, 6, 4, 4, 6, 4, 4, 2 };

    protected override IReadOnlyDictionary<ObjectDirection, IAnimation> Animations() => _animations;

    protected override ShootingProperties ShootingProperties() => ShootingPropertiesProvider.GetEnemyProperties();

    private readonly Dictionary<ObjectDirection, IAnimation> _animations = new();

    private bool _skipThink = false;
    private readonly int _index;
    private int _bonusCount;
    private readonly TankType _type;
    private TankOrder _order;

    public EnemyTank(Level level, TankType type, int bonusCount, int index, int periodIndex) : base(level)
    {
        _type = type;
        _bonusCount = bonusCount;
        _index = index;
        PeriodIndex = periodIndex;
        _order = ObjectDirectionExtensions.GetRandomDirection().ToTankOrder();
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.Draw(gameTime, spriteBatch);
#if DEBUG
        if (!Tank1460Game.ShowEnemiesPeriods)
            return;

        switch (PeriodIndex)
        {
            case 0:
                break;
            case 1:
                spriteBatch.DrawEllipse(BoundingRectangle.Center.ToVector2(), new Vector2(2), 4, Color.Yellow);
                break;
            default:
                spriteBatch.DrawEllipse(BoundingRectangle.Center.ToVector2(), new Vector2(2), 4, Color.Red);
                break;
        }
#endif
    }

    protected override void LoadContent()
    {
        base.LoadContent();

        foreach (ObjectDirection direction in Enum.GetValues(typeof(ObjectDirection)))
        {
            // Загружаем обычные текстуры в любом случае.
            var plainTexture = Level.Content.LoadRecoloredTexture($"Sprites/Tank/Type{(int)_type}/{direction}", $"Sprites/_R/Tank/Gray");
            IAnimation animation;

            if (_bonusCount <= 0)
            {
                animation = new Animation(plainTexture, true);
            }
            else
            {
                // Но только в случае бонуса подгружаем нужную текстуру и подключаем двумерную анимацию.
                var bonusTexture = Level.Content.LoadRecoloredTexture($"Sprites/Tank/Type{(int)_type}/{direction}", $"Sprites/_R/Tank/Red");

                animation = new ShiftingAnimation(new[] { bonusTexture, plainTexture },
                    double.MaxValue, true, 8 * Tank1460Game.OneFrameSpan);
            }

            _animations.Add(direction, animation);
        }

        TurnTo(ObjectDirection.Down);
    }

    public override void HandleShot(Shell shell)
    {
        Debug.Assert(shell.ShotBy is PlayerTank);

        if (IsInvulnerable())
            return;

        if (State != TankState.Normal)
            return;

        if (_bonusCount > 0)
        {
            _bonusCount--;
            Level.BonusManager.Spawn();
        }
        Explode(shell.ShotBy);
    }

    protected override TankOrder Think(GameTime gameTime, KeyboardState keyboardState)
    {
        // По умолчанию движемся туда же, куда и двигались, даже когда не думаем.
        var newOrder = _order.GetMovementOnly();

        // Думаем только в каждом втором такте (логика оригинала).
        // TODO: Тут бы тоже время считать по-хорошему как везде, чтобы в случае какого-то лага это все равно срабатывало верно.
        _skipThink = !_skipThink;
        if (_skipThink)
            return newOrder;

        var newThoughtDirection = CheckTileReach();
        if (newThoughtDirection is not null)
            newOrder = newThoughtDirection.Value.ToTankOrder();

        // Стреляй, Глеб Егорыч!
        if (Rng.Next(16) == 0)
            newOrder |= TankOrder.Shoot;

        _order = newOrder;
        return newOrder;
    }

    private ObjectDirection? DecideNewTarget()
    {
        return PeriodIndex switch
        {
            0 => ObjectDirectionExtensions.GetRandomDirection(),
            1 => Hunt(Level.GetTargetPlayerForEnemy(_index)),
            _ => Hunt(Level.Falcon)
        };
    }

    private ObjectDirection? ChangeDirection()
    {
        if (Rng.Next(2) == 0)
            return DecideNewTarget();

        return Rng.Next(2) == 0 ? Direction.Clockwise() : Direction.CounterClockwise();
    }

    private ObjectDirection? CheckTileReach()
    {
        if (IsTankCenteredOnTile() && Rng.Next(16) == 0)
            return DecideNewTarget();

        if (IsFrontTileBlocked && Rng.Next(4) == 0)
        {
            return IsTankCenteredOnTile() ? ChangeDirection() : Direction.Invert();
        }

        return null;
    }

    private ObjectDirection? Hunt(LevelObject target)
    {
        if (target is null || (!IsTankCenteredOnTile() && Rng.Next(2) == 0))
        {
            return CheckTileReach();
        }

        var deltaX = target.Position.X / Tile.DefaultWidth - Position.X / Tile.DefaultWidth;
        var deltaY = target.Position.Y / Tile.DefaultWidth - Position.Y / Tile.DefaultWidth;

        if (deltaX != 0 && deltaY != 0)
        {
            if (Rng.Next(2) == 0)
                DeltaXToDirection(deltaX);
            else
                DeltaYToDirection(deltaY);

            return null;
        }

        if (deltaX != 0)
            return DeltaXToDirection(deltaX);

        if (deltaY != 0)
            return DeltaYToDirection(deltaY);

        return ObjectDirectionExtensions.GetRandomDirection();
    }

    private static ObjectDirection DeltaXToDirection(int deltaX)
    {
        Debug.Assert(deltaX != 0);
        return deltaX < 0 ? ObjectDirection.Left : ObjectDirection.Right;
    }

    private static ObjectDirection DeltaYToDirection(int deltaY)
    {
        Debug.Assert(deltaY != 0);
        return deltaY < 0 ? ObjectDirection.Up : ObjectDirection.Down;
    }
}