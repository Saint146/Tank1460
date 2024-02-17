using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tank1460.Forms;

internal abstract class FormItem
{
    public Rectangle Bounds { get; private set; }

    public Point Position
    {
        get => Bounds.Location;
        set => Bounds = new Rectangle(value, Bounds.Size);
    }

    public bool Visible { get; set; } = true;

    protected abstract IAnimation Animation { get; }

    protected FormItemVisualStatus Status { get; private set; }

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
        if (Status == status)
            return;

        Status = status;
        OnChangeStatus();
    }

    protected virtual void OnChangeStatus()
    {
    }
}