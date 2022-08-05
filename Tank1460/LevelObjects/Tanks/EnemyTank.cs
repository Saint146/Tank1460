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
    private readonly Dictionary<ObjectDirection, IAnimation> _animations = new();

    protected override int[] SpawnAnimationTimesInFrames() => new[] { 4, 4, 4, 6, 4, 4, 6, 4, 4, 6, 4, 4, 2 };

    protected override IReadOnlyDictionary<ObjectDirection, IAnimation> Animations() => _animations;

    protected override ShootingProperties ShootingProperties() => ShootingPropertiesProvider.GetEnemyProperties();

    //private EnemyCommand _activeCommand = EnemyCommand.Idle;

    private bool _skipThink = false;
    private readonly int _index;

    private int _bonusCount;
    private readonly TankType _type;
    private ObjectDirection? _previousMovingDirection;

    public int PeriodIndex { get; set; }

#if DEBUG
    public double Lifetime = 0.0;
#endif

    public EnemyTank(Level level, TankType type, int bonusCount, int index, int periodIndex) : base(level)
    {
        _type = type;
        _bonusCount = bonusCount;
#if !DEBUG
        _index = index;
#else
        _index = 1;
#endif
        PeriodIndex = periodIndex;

        
    }

#if DEBUG
    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.Draw(gameTime, spriteBatch);
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
    }
#endif

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
        Explode();
    }

    protected override void Think(GameTime gameTime, KeyboardState keyboardState)
    {
#if DEBUG
        Lifetime += gameTime.ElapsedGameTime.TotalSeconds;
#endif

        // По умолчанию движемся туда же, куда и двигались.
        MovingDirection = _previousMovingDirection;

        // Думаем только в каждом втором такте.
        // TODO: Тут бы тоже время считать по-хорошему как везде, чтобы в случае какого-то лага это все равно срабатывало верно.
        _skipThink = !_skipThink;
        if (_skipThink)
            return;

        CheckTileReach();

        _previousMovingDirection = MovingDirection;

        if (Rng.Next(16) == 0)
            IssueShootOrder(gameTime);
    }

    private void DecideNewTarget()
    {
        switch (PeriodIndex)
        {
            case 0:
                RoamAround();
                break;
            case 1:
                Hunt(Level.GetTargetPlayerForEnemy(_index));
                break;
            default:
                Hunt(Level.Falcon);
                break;
        }
    }

    private void RoamAround()
    {
        SetRandomDirection();
    }

    private void ChangeDirection()
    {
        if (Rng.Next(2) == 0)
        {
            DecideNewTarget();
        }
        else
        {
            MoveTo(Rng.Next(2) == 0 ? Direction.Clockwise() : Direction.CounterClockwise());
        }
    }

    private void CheckTileReach()
    {
        if (IsTankCenteredOnTile() && Rng.Next(16) == 0)
        {
            DecideNewTarget();
        }
        else
        {
            if (IsFrontTileBlocked && Rng.Next(4) == 0)
            {
                if (IsTankCenteredOnTile())
                    ChangeDirection();
                else
                    MoveTo(Direction.Invert());
            }
        }
    }

    private void SetRandomDirection()
    {
        var allDirections = Enum.GetValues(typeof(ObjectDirection));
        MoveTo((ObjectDirection)allDirections.GetValue(Rng.Next(allDirections.Length))!);
    }

    private void Hunt(LevelObject target)
    {
        if (target is null || (!IsTankCenteredOnTile() && Rng.Next(2) == 0))
        {
            CheckTileReach();
            return;
        }

        var deltaX = target.Position.X / Tile.DefaultWidth - Position.X / Tile.DefaultWidth;
        var deltaY = target.Position.Y / Tile.DefaultWidth - Position.Y / Tile.DefaultWidth;

        if (deltaX != 0 && deltaY != 0)
        {
            if (Rng.Next(2) == 0)
                OrderMoveByDeltaX(deltaX);
            else
                OrderMoveByDeltaY(deltaY);

            return;
        }

        if (!OrderMoveByDeltaX(deltaX) && !OrderMoveByDeltaY(deltaY))
            RoamAround();
    }

    private bool OrderMoveByDeltaX(int deltaX)
    {
        switch (deltaX)
        {
            case < 0:
                MoveTo(ObjectDirection.Left);
                return true;
            case > 0:
                MoveTo(ObjectDirection.Right);
                return true;
            default:
                return false;
        }
    }
    private bool OrderMoveByDeltaY(int deltaY)
    {
        switch (deltaY)
        {
            case < 0:
                MoveTo(ObjectDirection.Up);
                return true;
            case > 0:
                MoveTo(ObjectDirection.Down);
                return true;
            default:
                return false;
        }
    }
}