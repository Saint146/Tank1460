using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Tank1460.Common.Level.Object;

namespace Tank1460.Common.Extensions;

public static class ObjectDirectionExtensions
{
    public static bool Has90DegreesDifference(this ObjectDirection direction, ObjectDirection anotherDirection) =>
        ClockwiseMap[direction] == anotherDirection || CounterClockwiseMap[direction] == anotherDirection;

    public static ObjectDirection Invert(this ObjectDirection direction) => Clockwise(Clockwise(direction));

    public static ObjectDirection Clockwise(this ObjectDirection direction) => ClockwiseMap[direction];

    public static ObjectDirection CounterClockwise(this ObjectDirection direction) => CounterClockwiseMap[direction];

    public static ObjectDirection GetRandomDirection() => _allDirections.GetRandom();

    public static Point ToStep(this ObjectDirection direction) => StepMap[direction];

    public static IReadOnlyCollection<ObjectDirection> AllDirections => _allDirections;

    private static readonly ObjectDirection[] _allDirections = Enum.GetValues<ObjectDirection>();

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

    private static readonly Dictionary<ObjectDirection, Point> StepMap = new()
    {
        { ObjectDirection.Up, new Point(0, -1) },
        { ObjectDirection.Right, new Point(+1, 0) },
        { ObjectDirection.Down, new Point(0, +1) },
        { ObjectDirection.Left, new Point(-1, 0) }
    };
}