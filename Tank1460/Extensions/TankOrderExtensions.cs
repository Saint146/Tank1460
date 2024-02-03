using Tank1460.Common.Level.Object;

namespace Tank1460.Extensions;

public static class TankOrderExtensions
{
    public static ObjectDirection? ToDirection(this TankOrder order)
    {
        // Кажется, что это шизофрения, но это просто логика оригинала.

        var hasLeft = order.HasFlag(TankOrder.MoveLeft);
        var hasRight = order.HasFlag(TankOrder.MoveRight);

        if (hasLeft || hasRight)
        {
            if (!hasLeft) return ObjectDirection.Right;

            if (!hasRight)
                return ObjectDirection.Left;

            return null;
        }

        var hasUp = order.HasFlag(TankOrder.MoveUp);
        var hasDown = order.HasFlag(TankOrder.MoveDown);

        if (hasUp || hasDown)
        {
            if (!hasUp) return ObjectDirection.Down;

            if (!hasDown)
                return ObjectDirection.Up;
        }

        return null;
    }

    public static TankOrder GetMovementOnly(this TankOrder order)
    {
        const TankOrder allMovementOrders = TankOrder.MoveDown | TankOrder.MoveUp | TankOrder.MoveLeft | TankOrder.MoveRight;
        return order & allMovementOrders;
    }
}