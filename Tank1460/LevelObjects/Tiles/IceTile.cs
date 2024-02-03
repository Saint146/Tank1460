using Microsoft.Xna.Framework.Graphics;
using Tank1460.Common.Level.Object.Tile;

namespace Tank1460.LevelObjects.Tiles;

public class IceTile : Tile
{
    public IceTile(Level level) : base(level)
    {
    }
    public override TileType Type => TileType.Ice;


    public override CollisionType CollisionType => CollisionType.None;

    protected override IAnimation GetAnimation() => new Animation(Level.Content.Load<Texture2D>(@"Sprites/Tiles/Ice"), false);
}