using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tank1460;

public abstract class LevelEffect : Effect
{
    public virtual bool CanUpdateWhenGameIsPaused => false;

    protected Level Level;

    protected LevelEffect(Level level) : base()
    {
        Level = level;
    }

    public abstract void Draw(SpriteBatch spriteBatch, Rectangle levelBounds);
}