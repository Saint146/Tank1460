using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tank1460.Forms;

internal abstract class FormItem
{
    protected abstract IAnimation Animation { get; }

    protected FormItemVisualStatus Status { get; private set; }

    public Rectangle Bounds { get; private set; }

    public Point Position
    {
        get => Bounds.Location;
        set => Bounds = new Rectangle(value, Bounds.Size);
    }

    protected FormItem(Point initialSize)
    {
        Bounds = new Rectangle(Point.Zero, initialSize);
    }

    internal virtual void Update(GameTime gameTime)
    {
        Animation.Process(gameTime);
    }

    internal void Draw(SpriteBatch spriteBatch)
    {
        Animation.Draw(spriteBatch, Bounds.Location);
    }

    internal void SetStatus(FormItemVisualStatus status)
    {
        if (Status != status)
        {
            Status = status;
            OnChangeStatus();
        }
    }

    protected virtual void OnChangeStatus()
    {
    }
}