using Microsoft.Xna.Framework;

namespace Tank1460.Extensions;

public static class PointExtensions
{
    public static Vector2 ToVector2(this Point point)
    {
        return new Vector2(point.X, point.Y);
    }

    public static Point NearestTileCoords(this Point point, ObjectDirection direction)
    {
        var x = point.X;
        var y = point.Y;

        switch (direction)
        {
            case ObjectDirection.Right:
                x++;
                break;

            case ObjectDirection.Left:
                x--;
                break;

            case ObjectDirection.Down:
                y++;
                break;

            case ObjectDirection.Up:
                y--;
                break;
        }

        return new Point(x, y);
    }

    public static Point Multiply(this Point point, int multiplier) => new(point.X * multiplier, point.Y * multiplier);
}