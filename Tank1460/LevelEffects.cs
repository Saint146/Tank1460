using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using Tank1460.Common.Extensions;

namespace Tank1460;

public class LevelEffects : EffectCollection<LevelEffect>
{
    public void Update(GameTime gameTime, bool isGamePaused)
    {
        if (!isGamePaused)
        {
            Update(gameTime);
            return;
        }

        Effects.Where(effect => effect.CanUpdateWhenGameIsPaused).ForEach(effect => effect.Update(gameTime));
        RemoveAll(effect => effect.ToRemove);
    }

    public void Draw(SpriteBatch spriteBatch, Rectangle levelBounds)
    {
        Effects.ForEach(effect => effect.Draw(spriteBatch, levelBounds));
    }
}