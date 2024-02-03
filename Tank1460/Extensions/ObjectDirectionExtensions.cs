using System.Collections.Generic;
using Tank1460.Common.Level.Object;

namespace Tank1460.Extensions;

internal static class ObjectDirectionExtensions
{
    public static TankOrder ToTankOrder(this ObjectDirection direction) => ToTankOrderMap[direction];
    
    private static readonly Dictionary<ObjectDirection, TankOrder> ToTankOrderMap = new()
    {
        {0, TankOrder.None},
        { ObjectDirection.Up, TankOrder.MoveUp },
        { ObjectDirection.Down, TankOrder.MoveDown },
        { ObjectDirection.Left, TankOrder.MoveLeft },
        { ObjectDirection.Right, TankOrder.MoveRight }
    };
}