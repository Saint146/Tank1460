namespace Tank1460.Menu;

internal class FormLabel : FormItem
{
    protected override IAnimation Animation { get; }

    internal FormLabel(IAnimation animation) : base(animation.FrameSize)
    {
        Animation = animation;
    }
}