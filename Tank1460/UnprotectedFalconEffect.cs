using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using Tank1460.Common.Extensions;
using Tank1460.LevelObjects;

namespace Tank1460;

internal class UnprotectedFalconEffect : LevelEffect
{
    public UnprotectedFalconEffect(Level level) : base(level)
    {
        RemoveFalconSurroundings(level);
        Remove();
    }

    private static void RemoveFalconSurroundings(Level level)
    {
        foreach (var falcon in level.Falcons)
        {
            var falconRect = falcon.TileRectangle;
            falconRect.Inflate(1, 1);

            var points = falconRect.GetOutlinePoints().Where(level.TileBounds.Contains);
            foreach (var point in points)
            {
                var tile = level.GetTile(point.X, point.Y);
                if (tile?.CollisionType.HasFlag(CollisionType.Shootable) == true)
                    tile.Remove();
            }
        }
    }

    public override void Update(GameTime gameTime)
    {
    }

    public override void Draw(SpriteBatch spriteBatch, Rectangle levelBounds)
    {
    }
}