namespace Tank1460.Forms;

internal class FormImage : FormItem
{
    protected override IAnimation Animation { get; }

    internal FormImage(IAnimation animation) : base(animation.FrameSize)
    {
        Animation = animation;
    }
}