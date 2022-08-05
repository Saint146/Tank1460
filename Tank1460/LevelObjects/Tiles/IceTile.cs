using Microsoft.Xna.Framework.Graphics;

namespace Tank1460.LevelObjects.Tiles;

public class IceTile : Tile
{
    public IceTile(Level level) : base(level)
    {
    }

    public override CollisionType CollisionType => CollisionType.None;

    protected override IAnimation GetAnimation() => new Animation(Level.Content.Load<Texture2D>(@"Sprites/Tiles/Ice"), false);
}