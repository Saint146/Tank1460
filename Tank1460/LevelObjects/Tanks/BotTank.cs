using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using System.Collections.Generic;
using System.Diagnostics;
using Tank1460.Audio;
using Tank1460.Extensions;
using Tank1460.LevelObjects.Tiles;

namespace Tank1460.LevelObjects.Tanks;

public class BotTank : Tank
{
    public int PeriodIndex { get; set; }

    protected override int[] SpawnAnimationTimesInFrames() => new[] { 4, 4, 4, 6, 4, 4, 6, 4, 4, 6, 4, 4, 2 };

    private bool _skipThink;
    private readonly int _index;
    private TankOrder _botOrder;
    private int _hp;

    /// <summary>
    /// Цвет танка в зависимости от хп.
    /// </summary>
    private static readonly Dictionary<int, TankColor> HpToColorMap = new()
    {
        { 1, TankColor.Gray },
        { 2, TankColor.Yellow | TankColor.Green },
        { 3, TankColor.Gray | TankColor.Yellow },
        { 4, TankColor.Gray | TankColor.Green }
    };
    
    public BotTank(Level level, TankType type, int hp, int bonusCount, int index, int periodIndex) : base(level, type, HpToTankColor(hp), bonusCount)
    {
        _hp = hp;
        _index = index;
        PeriodIndex = periodIndex;

        // Понеслись!
        _botOrder = ObjectDirectionExtensions.GetRandomDirection().ToTankOrder();
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.Draw(gameTime, spriteBatch);
#if DEBUG
        if (!Tank1460Game.ShowBotsPeriods)
            return;

        switch (PeriodIndex)
        {
            case 0:
                break;
            case 1:
                spriteBatch.DrawEllipse(BoundingRectangle.Center.ToVector2(), new Vector2(2), 4, Microsoft.Xna.Framework.Color.Yellow);
                break;
            default:
                spriteBatch.DrawEllipse(BoundingRectangle.Center.ToVector2(), new Vector2(2), 4, Microsoft.Xna.Framework.Color.Red);
                break;
        }
#endif
    }

    protected override TankOrder Think(GameTime gameTime, KeyboardState keyboardState)
    {
        // По умолчанию движемся туда же, куда и двигались, даже когда не думаем.
        var newOrder = _botOrder.GetMovementOnly();

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

        _botOrder = newOrder;
        return newOrder;
    }

    protected override void HandleDamaged(Tank damagedBy)
    {
        Debug.Assert(damagedBy is PlayerTank, "Боты не могут бить друг друга.");

        if (_hp <= 1)
        {
            Explode(damagedBy);
            return;
        }

        Level.SoundPlayer.Play(Sound.HitHurt);
        SetHp(_hp - 1);
    }

    private static TankColor HpToTankColor(int hp)
    {
        Debug.Assert(hp is > 0 and <= 4);
        return HpToColorMap[hp];
    }

    private void SetHp(int newHp)
    {
        Debug.Assert(newHp is > 0 and <= 4);
        _hp = newHp;

        SetColor(HpToTankColor(newHp));
    }

    #region --- AI ---

    private ObjectDirection? DecideNewTarget()
    {
        return PeriodIndex switch
        {
            0 => ObjectDirectionExtensions.GetRandomDirection(),
            1 => Hunt(Level.GetTargetPlayerForBot(_index)),
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

    #endregion
}