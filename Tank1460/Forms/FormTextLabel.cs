using Microsoft.Xna.Framework;

namespace Tank1460.Forms;

class FormTextLabel : FormItem
{
    public string Text
    {
        get => Animation.Text;
        set => Animation.Text = value;
    }

    protected override TextAnimation Animation { get; }

    public FormTextLabel(Font font, Point sizeInChars) : base(new(x: font.CharWidth * sizeInChars.X, y: font.CharHeight * sizeInChars.Y))
    {
        Animation = new TextAnimation(font, sizeInChars);
    }

    public FormTextLabel(Font font, int widthInChars, int heightInChars) : this(font, new(widthInChars, heightInChars))
    {
    }
}