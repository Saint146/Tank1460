using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
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

        // Когда не можем поворачиваться, думаем проще.
        if (Tank.IsImmobile)
        {
            _order = ThinkWhenImmobile();
            return _order;
        }

        // Осматриваемся по всем сторонам и оцениваем возможность выстрела, наличие опасности или врага.
        var shotPriorities = new Dictionary<ObjectDirection, ShotPriority>();
        ObjectDirection? enemyDirection = null;
        LevelObject enemy = null;
        double distanceToEnemy = 0;
        foreach (var direction in ObjectDirectionExtensions.AllDirections)
        {
            var (priority, target) = CheckLine(direction);
            switch (priority)
            {
                case ShotPriority.Danger:
                {
                    _order = ReactToDanger(target, direction);
                    return _order;
                }

                case ShotPriority.Enemy:
                    var distanceToTarget = Tank.BoundingRectangle.Center.DistanceTo(target.BoundingRectangle.Center);
                    if (enemy is not null && distanceToEnemy < distanceToTarget)
                        break;

                    enemyDirection = direction;
                    enemy = target;
                    distanceToEnemy = distanceToTarget;

                    break;
            }

            shotPriorities[direction] = priority;
        }

        if (enemyDirection.HasValue)
        {
            _order = ReactToEnemy(enemy, enemyDirection.Value);
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

    /// <summary>
    /// Принять приказ, ориентируясь на цель в указанном направлении.
    /// </summary>
    private TankOrder ReactToEnemy(LevelObject enemy, ObjectDirection enemyDirection)
    {
        // Если цель спереди или сзади, наступаем и стреляем.
        if (!Tank.Direction.Has90DegreesDifference(enemyDirection))
            return enemyDirection.ToTankOrder() | TankOrder.Shoot;

        // Если мы к цели под 90 градусов, то поворачиваемся, только если мы достаточно близко к клетке, которая действительно пересекается с целью.
        // Иначе из-за округления при повороте (см. Tank.TurnTo) окажемся не там и начнём дергаться.
        var positionAfterTurn = CalcPositionAfter90Turn();
        if (IsTargetShootableFromPosition(positionAfterTurn, enemy))
            return enemyDirection.ToTankOrder() | TankOrder.Shoot;

        // TODO: Ехать в направлении врага
        return Tank.Direction.ToTankOrder();
    }

    /// <summary>
    /// Принять приказ, ориентируясь на опасность в указанном направлении.
    /// </summary>
    private TankOrder ReactToDanger(LevelObject danger, ObjectDirection dangerDirection)
    {
        // Если опасность спереди или сзади, поворачиваемся, но стараемся не ехать, и стреляем.
        // TODO: На самом деле тут бы учесть, есть ли снаряды и какое расстояние до опасности, чтобы успеть её перебить.
        if (!Tank.Direction.Has90DegreesDifference(dangerDirection))
        {
            if (dangerDirection == Tank.Direction)
                return TankOrder.Shoot;

            return dangerDirection.ToTankOrder() | TankOrder.Shoot;
        }

        // Если мы к цели под 90 градусов, то поворачиваемся, только если мы достаточно близко к клетке, которая действительно пересекается с целью.
        // Иначе из-за округления при повороте (см. Tank.TurnTo) окажемся не там и начнём дергаться.
        var positionAfterTurn = CalcPositionAfter90Turn();
        if (IsTargetShootableFromPosition(positionAfterTurn, danger))
            return dangerDirection.ToTankOrder() | TankOrder.Shoot;

        // TODO: Уворачиваться, т.е. ехать в обратном направлении, если оно доступно.
        return Tank.Direction.ToTankOrder();
    }

    private bool IsTargetShootableFromPosition(Point position, LevelObject enemy)
    {
        return (position.Y < enemy.Position.Y + enemy.BoundingRectangle.Height &&
                position.Y + Tank.BoundingRectangle.Height > enemy.Position.Y)

               || (position.X < enemy.Position.X + enemy.BoundingRectangle.Width &&
                   position.X + Tank.BoundingRectangle.Width > enemy.Position.X);
    }

    private TankOrder ThinkWhenImmobile()
    {
        var (priority, _) = CheckLine(Tank.Direction);

        // Можем только стрелять или не стрелять.
        return priority switch
        {
            ShotPriority.Danger => TankOrder.Shoot,
            ShotPriority.Enemy => TankOrder.Shoot,
            ShotPriority.Forbidden => TankOrder.None,
            _ => Rng.OneIn(16) ? TankOrder.Shoot : TankOrder.None
        };
    }

    private (ShotPriority Priority, LevelObject Target) CheckLine(ObjectDirection direction)
    {
        // Получаем прямоугольник перед танком и продвигаемся в том же направлении, пока не найдём что-то интересное или конец карты.
        var testTileRect = PlayerTank.TileRectangle.NearestTiles(direction);
        var step = direction.ToStep();

        while (_level.TileBounds.Contains(testTileRect))
        {
            var levelObjects = _level.GetLevelObjectsInTiles(testTileRect);

            LevelObject enemy = null;
            LevelObject danger = null;
            var foundSomeDestructibles = false;
            foreach (var levelObject in levelObjects)
            {
                // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
                switch (levelObject)
                {
                    case ConcreteTile:
                        if (!PlayerTank.ShellProperties.HasFlag(ShellProperties.ArmorPiercing))
                            return (ShotPriority.Forbidden, null);

                        foundSomeDestructibles = true;
                        break;

                    case BrickTile:
                        foundSomeDestructibles = true;
                        break;

                    case Falcon:
                    case LevelObjects.Tanks.PlayerTank when !foundSomeDestructibles:
                        return (ShotPriority.Forbidden, null);

                    case BotTank:
                        enemy = levelObject;
                        break;

                    case Shell { ShotBy: BotTank }:
                        danger = levelObject;
                        break;
                }
            }

            if (danger is not null)
                return (ShotPriority.Danger, danger);

            if (enemy is not null)
                return (ShotPriority.Enemy, enemy);

            // Продолжаем смотреть дальше.
            testTileRect.Location += step;
        }

        return (ShotPriority.None, null);
    }

    private enum ShotPriority
    {
        None,
        Forbidden,
        Enemy,
        Danger
    }

    /// <summary>
    /// Посчитать, где окажется танк при повороте на 90 градусов (см. <see cref="Tank.TurnTo"/>)
    /// </summary>
    private Point CalcPositionAfter90Turn() => new((int)Math.Round((double)Tank.Position.X / Tile.DefaultWidth) * Tile.DefaultWidth,
                                                   (int)Math.Round((double)Tank.Position.Y / Tile.DefaultHeight) * Tile.DefaultHeight);

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