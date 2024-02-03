using Tank1460.Common.Extensions;
using Tank1460.Common.Level.Object;

namespace Tank1460.LevelObjects.Tiles;

public abstract class DestructibleTile : Tile
{
    protected DestructibleTile(Level level) : base(level)
    {
    }

    protected void Reduce(ObjectDirection shotFromDirection, int sizeToReduceBy)
    {
        var newRect = LocalBounds.Crop(shotFromDirection, sizeToReduceBy);

        if (newRect is not null)
            LocalBounds = newRect.Value;
        else
            Remove();
    }
}