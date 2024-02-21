using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Tank1460.Common;
using Tank1460.Common.Extensions;
using Tank1460.Common.Level.Object;
using Tank1460.Extensions;
using Tank1460.LevelObjects;
using Tank1460.LevelObjects.Tanks;
using Tank1460.LevelObjects.Tiles;
using ObjectDirectionExtensions = Tank1460.Common.Extensions.ObjectDirectionExtensions;

namespace Tank1460.AI;

internal class CommonPlayerTankAi : PlayerTankAi
{
    private readonly Level _level;
    private TankOrder _order;
    private bool _skipThink;
    private LevelObject _target;

    public CommonPlayerTankAi(PlayerTank tank, Level level) : base(tank)
    {
        _level = level;
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

        var shotPriorities = new Dictionary<ObjectDirection, ShotPriority>();
        ObjectDirection? targetDirection = null;
        foreach (var direction in ObjectDirectionExtensions.AllDirections)
        {
            var priority = CheckLine(direction);
            switch (priority)
            {
                case ShotPriority.Danger:
                {
                    _order = direction.ToTankOrder() | TankOrder.Shoot;
                    return _order;
                }

                case ShotPriority.Target:
                    targetDirection = direction;
                    break;
            }

            shotPriorities[direction] = priority;
        }

        if (targetDirection.HasValue)
        {
            _order = targetDirection.Value.ToTankOrder() | TankOrder.Shoot;
            return _order;
        }
        
        var newThoughtDirection = CheckTileReach();
        if (newThoughtDirection is not null)
            newOrder = newThoughtDirection.Value.ToTankOrder();

        // Стреляй, Глеб Егорыч!
        if (Rng.OneIn(16) && shotPriorities[newThoughtDirection ?? Tank.Direction] != ShotPriority.Forbidden)
            newOrder |= TankOrder.Shoot;

        _order = newOrder;
        return newOrder;
    }

    private ShotPriority CheckLine(ObjectDirection direction)
    {
        // Получаем прямоугольник перед танком и продвигаемся в том же направлении, пока не найдём что-то интересное или конец карты.
        var testTileRect = PlayerTank.TileRectangle.NearestTiles(direction);
        var step = direction.ToStep();

        while (_level.TileBounds.Contains(testTileRect))
        {
            var levelObjects = _level.GetLevelObjectsInTiles(testTileRect);

            var foundPreferredTarget = false;
            var foundDanger = false;
            var foundSomeDestructibles = false;
            foreach (var targetKind in levelObjects.Select(CheckTarget))
            {
                switch (targetKind)
                {
                    case ShotTarget.Indestructible:
                    case ShotTarget.ForbiddenForClearing:
                    case ShotTarget.ForbiddenForShooting when !foundSomeDestructibles:
                        return ShotPriority.Forbidden;

                    case ShotTarget.Destructible:
                        foundSomeDestructibles = true;
                        break;

                    case ShotTarget.Preferred:
                        foundPreferredTarget = true;
                        break;

                    case ShotTarget.Danger:
                        foundDanger = true;
                        break;
                }
            }

            if (foundDanger)
                return ShotPriority.Danger;

            if (foundPreferredTarget)
                return ShotPriority.Target;

            // Продолжаем смотреть дальше.
            testTileRect.Location += step;
        }

        return ShotPriority.None;
    }

    private ShotTarget CheckTarget(LevelObject levelObject) =>
        levelObject switch
        {
            BrickTile => ShotTarget.Destructible,
            Falcon => ShotTarget.ForbiddenForClearing,
            LevelObjects.Tanks.PlayerTank => ShotTarget.ForbiddenForShooting,
            ConcreteTile => PlayerTank.ShellProperties.HasFlag(ShellProperties.ArmorPiercing) ? ShotTarget.Destructible : ShotTarget.Indestructible,
            BotTank => ShotTarget.Preferred,
            Shell { ShotBy: BotTank } => ShotTarget.Danger,
            _ => ShotTarget.None
        };

    private enum ShotTarget
    {
        None,
        Destructible,
        Indestructible,
        ForbiddenForShooting,
        ForbiddenForClearing,
        Preferred,
        Danger
    }

    private enum ShotPriority
    {
        None,
        Forbidden,
        Target,
        Danger
    }

    private ObjectDirection? CheckTileReach()
    {
        if (PlayerTank.Position.IsCenteredOnTile() && Rng.Next(16) == 0)
            return DecideNewTarget();

        if (PlayerTank.IsFrontTileBlocked && Rng.Next(4) == 0)
            return PlayerTank.Position.IsCenteredOnTile() ? ChangeDirection() : PlayerTank.Direction.Invert();

        return null;
    }

    private ObjectDirection? DecideNewTarget()
    {
        if (_target is { ToRemove: false })
            return Hunt(_target);

        if (_level.BonusManager.Bonuses.Count != 0)
            return Hunt(_level.BonusManager.Bonuses.ToArray().GetRandom());

        if (_level.BotManager.BotTanks.Count != 0)
            return Hunt(_level.BotManager.BotTanks.ToArray().GetRandom());

        return ObjectDirectionExtensions.GetRandomDirection();
    }

    private ObjectDirection? ChangeDirection()
    {
        if (Rng.Next(2) == 0)
            return DecideNewTarget();

        return Rng.Next(2) == 0 ? PlayerTank.Direction.Clockwise() : PlayerTank.Direction.CounterClockwise();
    }

    private ObjectDirection? Hunt(LevelObject target)
    {
        if (target is null)
            return CheckTileReach();

        _target = target;

        var deltaX = target.Position.X - PlayerTank.Position.X;
        var deltaY = target.Position.Y - PlayerTank.Position.Y;

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