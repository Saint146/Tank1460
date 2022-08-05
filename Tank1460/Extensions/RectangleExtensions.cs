using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Tank1460.LevelObjects.Tiles;

namespace Tank1460.Extensions;

public static class RectangleExtensions
{
    /// <summary>
    /// Calculates the signed depth of intersection between two rectangles.
    /// </summary>
    /// <returns>
    /// The amount of overlap between two intersecting rectangles. These
    /// depth values can be negative depending on which wides the rectangles
    /// intersect. This allows callers to determine the correct direction
    /// to push objects in order to resolve collisions.
    /// If the rectangles are not intersecting, Vector2.Zero is returned.
    /// </returns>
    public static Vector2 GetIntersectionDepth(this Rectangle rectA, Rectangle rectB)
    {
        // Calculate half sizes.
        var halfWidthA = rectA.Width / 2.0f;
        var halfHeightA = rectA.Height / 2.0f;
        var halfWidthB = rectB.Width / 2.0f;
        var halfHeightB = rectB.Height / 2.0f;

        // Calculate centers.
        var centerA = new Vector2(rectA.Left + halfWidthA, rectA.Top + halfHeightA);
        var centerB = new Vector2(rectB.Left + halfWidthB, rectB.Top + halfHeightB);

        // Calculate current and minimum-non-intersecting distances between centers.
        var distanceX = centerA.X - centerB.X;
        var distanceY = centerA.Y - centerB.Y;
        var minDistanceX = halfWidthA + halfWidthB;
        var minDistanceY = halfHeightA + halfHeightB;

        // If we are not intersecting at all, return (0, 0).
        if (Math.Abs(distanceX) >= minDistanceX || Math.Abs(distanceY) >= minDistanceY)
            return Vector2.Zero;

        // Calculate and return intersection depths.
        var depthX = distanceX > 0 ? minDistanceX - distanceX : -minDistanceX - distanceX;
        var depthY = distanceY > 0 ? minDistanceY - distanceY : -minDistanceY - distanceY;
        return new Vector2(depthX, depthY);
    }

    /// <summary>
    /// Вернуть центр определенного края прямоугольника с отдалением на <paramref name="offset"/>
    /// </summary>
    /// <param name="rect">Прямоугольник.</param>
    /// <param name="direction">С какой стороны прямоугольника берётся край.</param>
    /// <param name="offset">Отдаление от центра прямоугольника. Может быть отрицательное.</param>
    public static Point GetEdgeCenter(this Rectangle rect, ObjectDirection direction, int offset = -3)
    {
        return direction switch
        {
            ObjectDirection.Up => new Point(rect.Left + rect.Width / 2, rect.Top - offset),
            ObjectDirection.Down => new Point(rect.Left + rect.Width / 2, rect.Bottom + offset),
            ObjectDirection.Left => new Point(rect.Left - offset, rect.Top + rect.Height / 2),
            ObjectDirection.Right => new Point(rect.Right + offset, rect.Top + rect.Height / 2),

            _ => throw new NotImplementedException()
        };
    }

    public static Rectangle? Crop(this Rectangle rect, ObjectDirection direction, int cropValue)
    {
        var x = rect.X;
        var y = rect.Y;
        var width = rect.Width;
        var height = rect.Height;

        switch (direction)
        {
            case ObjectDirection.Right:
                x += cropValue;
                width -= cropValue;
                break;

            case ObjectDirection.Left:
                width -= cropValue;
                break;

            case ObjectDirection.Down:
                y += cropValue;
                height -= cropValue;
                break;

            case ObjectDirection.Up:
                height -= cropValue;
                break;
        }

        if (width <= 0 || height <= 0)
            return null;

        return new Rectangle(x, y, width, height);
    }

    /// <summary>
    /// Вернуть прямоугольник толщиной 1 (с координатами по тайлам) с ближайшими тайлами с указанной стороны.
    /// </summary>
    public static Rectangle NearestTiles(this Rectangle tileRect, ObjectDirection direction)
    {
        return direction switch
        {
            ObjectDirection.Right => new Rectangle(tileRect.Right, tileRect.Y, 1, tileRect.Height),
            ObjectDirection.Left => new Rectangle(tileRect.Left - 1, tileRect.Y, 1, tileRect.Height),
            ObjectDirection.Down => new Rectangle(tileRect.X, tileRect.Bottom, tileRect.Width, 1),
            ObjectDirection.Up => new Rectangle(tileRect.X, tileRect.Top - 1, tileRect.Width, 1),
            _ => throw new ArgumentOutOfRangeException(nameof(direction))
        };
    }

    public static IEnumerable<Point> GetAllPoints(this Rectangle rectangle)
    {
        for (var x = rectangle.Left; x < rectangle.Right; x++)
        for (var y = rectangle.Top; y < rectangle.Bottom; y++)
            yield return new Point(x, y);
    }

    /// <summary>
    /// Определить, какие тайлы (хотя бы частично) занимает указанный прямоугольник.
    /// </summary>
    public static Rectangle RoundToTiles(this Rectangle rectangle)
    {
        var top = rectangle.Top.CoordToTileX();
        var bottom = (rectangle.Bottom - 1).CoordToTileY() + 1;
        var left = rectangle.Left.CoordToTileX();
        var right = (rectangle.Right - 1).CoordToTileX() + 1;

        return new Rectangle(left, top, right - left, bottom - top);
    }

    public static void EnumerateArray<T>(this Rectangle rectangle, T[,] array, Action<T> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        for (var y = Math.Max(0, rectangle.Top); y < Math.Min(rectangle.Bottom, array.GetLength(1)); y++)
        {
            for (var x = Math.Max(0, rectangle.Left); x < Math.Min(rectangle.Right, array.GetLength(0)); x++)
            {
                action(array[x, y]);
            }
        }
    }

    public static Point GetRandomPoint(this Rectangle rectangle)
    {
        var x = Rng.Next(rectangle.Left, rectangle.Width);
        var y = Rng.Next(rectangle.Top, rectangle.Height);
        return new Point(x, y);
    }

    public static Rectangle Multiply(this Rectangle rectangle, Point point)
    {
        return new Rectangle(rectangle.X * point.X, rectangle.Y * point.Y, rectangle.Width * point.X, rectangle.Height * point.Y);
    }

    public static Rectangle Add(this Rectangle rectangle, Point point)
    {
        return new Rectangle(rectangle.X + point.X, rectangle.Y + point.Y, rectangle.Width, rectangle.Height);
    }
}