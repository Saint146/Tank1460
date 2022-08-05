using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tank1460;

public abstract class TankEffect
{
    public bool ToRemove;

    public abstract void Update(GameTime gameTime);

    public abstract void Draw(SpriteBatch spriteBatch, Vector2 position);
}