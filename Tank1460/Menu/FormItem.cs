using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tank1460.Menu;

internal class FormItem
{
    public Rectangle Bounds { get; private set; }

    public Point Position
    {
        get => Bounds.Location;
        set => Bounds = new Rectangle(value, Bounds.Size);
    }

    private readonly ShiftingAnimation _animation;

    private FormItem(ShiftingAnimation animation)
    {
        _animation = animation;
        Bounds = new Rectangle(Point.Zero, _animation.FrameSize);
    }

    public FormItem(Texture2D normalTexture, Texture2D hoverTexture, Texture2D pressedTexture)
        : this(new ShiftingAnimation(new[] { normalTexture, hoverTexture, pressedTexture }, false))
    {
    }

    public FormItem(Texture2D normalTexture, Texture2D hoverTexture, Texture2D pressedTexture, double[] frameTimes)
        : this(new ShiftingAnimation(new[] { normalTexture, hoverTexture, pressedTexture }, frameTimes, true))
    {
    }

    public FormItem(Texture2D normalTexture, Texture2D hoverTexture, Texture2D pressedTexture, double frameTime)
        : this(new ShiftingAnimation(new[] { normalTexture, hoverTexture, pressedTexture }, frameTime, true))
    {
    }

    internal virtual void Update(GameTime gameTime, FormItemVisualStatus status)
    {
        _animation.Process(gameTime);
        _animation.ShiftTo((int)status);
    }

    internal virtual void Draw(SpriteBatch spriteBatch)
    {
        _animation.Draw(spriteBatch, Bounds.Location);
    }
}