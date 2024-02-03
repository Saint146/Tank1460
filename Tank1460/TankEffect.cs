using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tank1460;

public abstract class TankEffect : Effect
{
    public abstract void Draw(SpriteBatch spriteBatch, Vector2 position);
}