using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tank1460.LevelObjects.Explosions;

public abstract class Explosion : LevelObject
{
    private IAnimation _animation;

    protected Explosion(Level level) : base(level)
    {
        Level.AddExplosion(this);
    }

    protected abstract string TexturePath();

    protected abstract double FrameTime();

    protected override void LoadContent()
    {
        _animation = new Animation(Level.Content.Load<Texture2D>(TexturePath()), FrameTime(), false);
    }

    public override void Update(GameTime gameTime)
    {
        Sprite.ProcessAnimation(gameTime);

        if (Sprite.HasAnimationEnded)
            Remove();
    }

    protected override IAnimation GetDefaultAnimation() => _animation;
}