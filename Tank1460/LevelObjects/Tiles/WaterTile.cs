using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Tank1460.LevelObjects.Tiles;

class WaterTile : Tile
{
    public WaterTile(Level level) : base(level)
    {
    }

    public override CollisionType CollisionType => CollisionType.Impassable;

    protected override IAnimation GetAnimation() => new Animation(Level.Content.Load<Texture2D>(@"Sprites/Tiles/Water"), 32.0 * Tank1460Game.OneFrameSpan, true);

    public override void Update(GameTime gameTime, KeyboardState keyboardState)
    {
        base.Update(gameTime, keyboardState);
        Sprite.ProcessAnimation(gameTime);
    }
}