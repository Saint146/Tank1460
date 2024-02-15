using Microsoft.Xna.Framework.Graphics;

namespace Tank1460.Forms;

internal class FormButton : FormItem
{
    protected override IAnimation Animation => _animation;

    private readonly ShiftingAnimation _animation;

    private FormButton(ShiftingAnimation animation) : base(animation.FrameSize)
    {
        _animation = animation;
    }

    public FormButton(Texture2D normalTexture, Texture2D hoverTexture, Texture2D pressedTexture)
        : this(new ShiftingAnimation(new[] { normalTexture, hoverTexture, pressedTexture }, false))
    {
    }

    public FormButton(Texture2D normalTexture, Texture2D hoverTexture, Texture2D pressedTexture, double[] frameTimes)
        : this(new ShiftingAnimation(new[] { normalTexture, hoverTexture, pressedTexture }, frameTimes, true))
    {
    }

    public FormButton(Texture2D normalTexture, Texture2D hoverTexture, Texture2D pressedTexture, double frameTime)
        : this(new ShiftingAnimation(new[] { normalTexture, hoverTexture, pressedTexture }, frameTime, true))
    {
    }

    public FormButton(Texture2D normalTexture, double frameTime)
        : this(normalTexture, normalTexture, normalTexture, frameTime)
    {
    }

    public void ChangeTexture(FormItemVisualStatus status, Texture2D newTexture)
    {
        _animation.ChangeTexture((int)status, newTexture);
    }

    protected override void OnChangeStatus()
    {
        _animation.ShiftTo((int)Status);
    }
}