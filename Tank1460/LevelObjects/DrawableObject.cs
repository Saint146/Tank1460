using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tank1460.LevelObjects;

public abstract class DrawableObject : UpdateableObject
{
    protected DrawableObject(Level level) : base(level)
    {
    }

    public abstract void Draw(GameTime gameTime, SpriteBatch spriteBatch);
}