using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tank1460.Common.Extensions;

namespace Tank1460;

internal class FloatingText : LevelEffect
{
    public string Text { get; }

    protected readonly AnimationPlayer Sprite = new();

    private readonly double _effectTime;  //12/49
    private readonly Point _position;
    private double _time;

    public FloatingText(Level level, string text, Point centerPosition, double effectTime) : base(level)
    {
        Text = text;
        _effectTime = effectTime;

        LoadContent(level.Content);
        _position = centerPosition - Sprite.VisibleRect.Size.Divide(2);
    }

    public override void Update(GameTime gameTime)
    {
        _time += gameTime.ElapsedGameTime.TotalSeconds;
        if (_time >= _effectTime)
            Remove();
    }

    public override void Draw(SpriteBatch spriteBatch, Rectangle levelBounds)
    {
        Sprite.Draw(spriteBatch, _position);
    }

    private void LoadContent(ContentManagerEx content)
    {
        var font = content.LoadFont(@"Sprites/Font/Pixel5x8", @"ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", Color.White);
        var textTexture = font.CreateTexture(Text);

        var animation = new Animation(textTexture, new[] { double.MaxValue }, false);
        Sprite.PlayAnimation(animation);
    }
}