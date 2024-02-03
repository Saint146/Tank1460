using Microsoft.Xna.Framework;

namespace Tank1460.Extensions;

internal static class RectangleExtensions
{
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
}