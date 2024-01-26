﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tank1460.LevelObjects.Tiles;

class WaterTile : Tile
{
    public WaterTile(Level level) : base(level)
    {
    }

    public override CollisionType CollisionType => CollisionType.PassablyOnlyByShip;

    protected override IAnimation GetAnimation() => new Animation(Level.Content.Load<Texture2D>(@"Sprites/Tiles/Water"), 32.0 * Tank1460Game.OneFrameSpan, true);

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        Sprite.ProcessAnimation(gameTime);
    }
}