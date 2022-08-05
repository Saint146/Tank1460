using System.Collections.Generic;

namespace Tank1460.Extensions;

public static class ObjectDirectionExtension
{
    private static readonly Dictionary<ObjectDirection, ObjectDirection> ClockwiseDictionary = new()
    {
        { 0, 0 },
        { ObjectDirection.Up, ObjectDirection.Right },
        { ObjectDirection.Right, ObjectDirection.Down },
        { ObjectDirection.Down, ObjectDirection.Left },
        { ObjectDirection.Left, ObjectDirection.Up }
    };

    private static readonly Dictionary<ObjectDirection, ObjectDirection> CounterClockwiseDictionary = new()
    {
        { 0, 0 },
        { ObjectDirection.Up, ObjectDirection.Left },
        { ObjectDirection.Right, ObjectDirection.Up },
        { ObjectDirection.Down, ObjectDirection.Right },
        { ObjectDirection.Left, ObjectDirection.Down }
    };

    public static bool Has90DegreesDifference(this ObjectDirection direction, ObjectDirection anotherDirection)
    {
        return ClockwiseDictionary[direction] == anotherDirection ||
               CounterClockwiseDictionary[direction] == anotherDirection;
    }

    public static ObjectDirection Invert(this ObjectDirection direction)
    {
        return Clockwise(Clockwise(direction));
    }

    public static ObjectDirection Clockwise(this ObjectDirection direction)
    {
        return ClockwiseDictionary[direction];
    }

    public static ObjectDirection CounterClockwise(this ObjectDirection direction)
    {
        return CounterClockwiseDictionary[direction];
    }
}