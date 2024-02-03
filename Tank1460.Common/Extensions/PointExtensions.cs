using System;
using Microsoft.Xna.Framework;
using Tank1460.Common.Level.Object;

namespace Tank1460.Common.Extensions;

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
            default:
                throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
        }

        return new Point(x, y);
    }

    public static Point Multiply(this Point point, int multiplier) => new(point.X * multiplier, point.Y * multiplier);

    /// <summary>
    /// Применить обратную трансформацию к точке.
    /// </summary>
    /// <remarks>
    /// Вероятно, отвалится, если в матрице будет поворот, например. Но для текущих задач работает.
    /// </remarks>
    public static Point ApplyReversedTransformation(this Point point, Matrix transformation) =>
        new(
            x: (int)((point.X - transformation.Translation.X) * 2 / (transformation.Right.X - transformation.Left.X)),
            y: (int)((point.Y - transformation.Translation.Y) * 2 / (transformation.Up.Y - transformation.Down.Y))
        );
}