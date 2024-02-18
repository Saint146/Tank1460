using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tank1460.Common.Level.Object.Tile;

namespace Tank1460.LevelObjects.Tiles;

class WaterTile : Tile
{
    public WaterTile(Level level) : base(level)
    {
    }
    public override TileType Type => TileType.Water;


    public override CollisionType CollisionType => CollisionType.PassableOnlyByShip;

    protected override IAnimation GetAnimation() => new Animation(Level.Content.Load<Texture2D>(@"Sprites/Tiles/Water"), 32.0 * Tank1460Game.OneFrameSpan, true);

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        Sprite.ProcessAnimation(gameTime);
    }
}