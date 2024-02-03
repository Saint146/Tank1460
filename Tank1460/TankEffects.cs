using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tank1460;

public class TankEffects : EffectCollection<TankEffect>
{
    public void Draw(SpriteBatch spriteBatch, Vector2 position)
    {
        Effects.ForEach(effect => effect.Draw(spriteBatch, position));
    }
}