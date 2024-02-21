using System.Diagnostics;
using Tank1460.Common;
using Tank1460.Common.Extensions;
using Tank1460.Common.Level.Object;
using Tank1460.Extensions;
using Tank1460.LevelObjects;
using Tank1460.LevelObjects.Tanks;
using ObjectDirectionExtensions = Tank1460.Common.Extensions.ObjectDirectionExtensions;

namespace Tank1460.AI;

internal class ClassicBotTankAi : BotTankAi
{
    private readonly Level _level;

    private TankOrder _order;
    private bool _skipThink;

    public ClassicBotTankAi(BotTank tank, Level level):base(tank)
    {
        _level = level;

        // Понеслись!
        _order = ObjectDirectionExtensions.GetRandomDirection().ToTankOrder();
    }

    public override TankOrder Think()
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
            if (Rng.OneIn(16))
                newOrder |= TankOrder.Shoot;

            _order = newOrder;
            return newOrder;
    }

    private ObjectDirection? CheckTileReach()
    {
        if (BotTank.Position.IsCenteredOnTile() && Rng.Next(16) == 0)
            return DecideNewTarget();

        if (BotTank.IsFrontTileBlocked && Rng.Next(4) == 0)
            return BotTank.Position.IsCenteredOnTile() ? ChangeDirection() : BotTank.Direction.Invert();

        return null;
    }

    private ObjectDirection? DecideNewTarget()
    {
        return BotTank.PeriodIndex switch
        {
            0 => ObjectDirectionExtensions.GetRandomDirection(),
            1 => Hunt(_level.GetTargetPlayerForBot(BotTank.Index)),
            _ => Hunt(_level.GetTargetFalconForBot(BotTank.Index))
        };
    }

    private ObjectDirection? ChangeDirection()
    {
        if (Rng.Next(2) == 0)
            return DecideNewTarget();

        return Rng.Next(2) == 0 ? BotTank.Direction.Clockwise() : BotTank.Direction.CounterClockwise();
    }

    private ObjectDirection? Hunt(LevelObject target)
    {
        if (target is null)
            return CheckTileReach();

        var deltaX = target.Position.X - BotTank.Position.X;
        var deltaY = target.Position.Y - BotTank.Position.Y;

        if (deltaX != 0 && deltaY != 0)
            return Rng.Next(2) == 0 ? DeltaXToDirection(deltaX) : DeltaYToDirection(deltaY);

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