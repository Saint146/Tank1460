using Microsoft.Xna.Framework;

namespace Tank1460.Common.Level.Object;

public abstract class LevelObjectModel
{
    public abstract LevelObjectType Type { get; }

    public Point Position { get; set; }

    public Point Size { get; set; }

    public Rectangle Bounds => new(Position, Size);
}