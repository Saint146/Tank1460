using Microsoft.Xna.Framework;
using Tank1460.LevelObjects.Tiles;

namespace Tank1460.Extensions;

public static class PointExtensions
{
    public static bool IsCenteredOnTile(this Point position)
        => position.X % Tile.DefaultWidth == 0 && position.Y % Tile.DefaultHeight == 0;
}