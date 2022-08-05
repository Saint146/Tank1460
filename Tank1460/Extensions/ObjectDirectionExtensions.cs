using System;
using System.Collections.Generic;

namespace Tank1460.Extensions;

public static class ObjectDirectionExtensions
{
    public static bool Has90DegreesDifference(this ObjectDirection direction, ObjectDirection anotherDirection) =>
        ClockwiseMap[direction] == anotherDirection || CounterClockwiseMap[direction] == anotherDirection;

    public static ObjectDirection Invert(this ObjectDirection direction) => Clockwise(Clockwise(direction));

    public static ObjectDirection Clockwise(this ObjectDirection direction) => ClockwiseMap[direction];

    public static ObjectDirection CounterClockwise(this ObjectDirection direction) => CounterClockwiseMap[direction];

    public static TankOrder ToTankOrder(this ObjectDirection direction) => ToTankOrderMap[direction];

    public static ObjectDirection GetRandomDirection() => AllDirections.GetRandom();

    private static readonly ObjectDirection[] AllDirections = Enum.GetValues<ObjectDirection>();

    private static readonly Dictionary<ObjectDirection, ObjectDirection> ClockwiseMap = new()
    {
        { 0, 0 },
        { ObjectDirection.Up, ObjectDirection.Right },
        { ObjectDirection.Right, ObjectDirection.Down },
        { ObjectDirection.Down, ObjectDirection.Left },
        { ObjectDirection.Left, ObjectDirection.Up }
    };

    private static readonly Dictionary<ObjectDirection, ObjectDirection> CounterClockwiseMap = new()
    {
        { 0, 0 },
        { ObjectDirection.Up, ObjectDirection.Left },
        { ObjectDirection.Right, ObjectDirection.Up },
        { ObjectDirection.Down, ObjectDirection.Right },
        { ObjectDirection.Left, ObjectDirection.Down }
    };

    private static readonly Dictionary<ObjectDirection, TankOrder> ToTankOrderMap = new()
    {
        {0, TankOrder.None},
        { ObjectDirection.Up, TankOrder.MoveUp },
        { ObjectDirection.Down, TankOrder.MoveDown },
        { ObjectDirection.Left, TankOrder.MoveLeft },
        { ObjectDirection.Right, TankOrder.MoveRight }
    };
}