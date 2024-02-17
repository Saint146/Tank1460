using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tank1460.Common.Extensions;

namespace Tank1460;

public class TextAnimation : IAnimation
{
    public Point FrameSize { get; }

    public string Text
    {
        get => _text;
        set
        {
            var lines = value.SplitIntoLines();
            var lineLength = lines.FirstOrDefault()?.Length ?? 0;

            if (lineLength > _sizeInChars.X || lines.Length > _sizeInChars.Y)
                throw new ArgumentOutOfRangeException(nameof(value));

            // TODO: Вместо рисования текста каждый кадр хранить текстуру и здесь пересоздавать её?
            _text = value;
        }
    }

    public int FrameWidth { get; }

    public int FrameHeight { get; }

    public bool HasEnded => false;

    private readonly Font _font;
    private readonly Point _sizeInChars;
    private string _text;

    public TextAnimation(Font font, Point sizeInChars)
    {
        _font = font;
        _sizeInChars = sizeInChars;

        FrameSize = new(x: font.CharWidth * sizeInChars.X,
                        y: font.CharHeight * sizeInChars.Y);

        FrameWidth = FrameSize.X;
        FrameHeight = FrameSize.Y;
    }

    public void Reset()
    {
    }

    public void Advance()
    {
    }

    public void Process(GameTime gameTime)
    {
    }

    public void Draw(SpriteBatch spriteBatch, Vector2 position, Rectangle visibleRectangle, float scale)
    {
        Debug.Assert(visibleRectangle.Size == FrameSize);

        _font.Draw(Text, spriteBatch, position.ToPoint() + visibleRectangle.Location);
    }

    public void Draw(SpriteBatch spriteBatch, Point position)
    {
        Draw(spriteBatch, position.ToVector2(), new Rectangle(0, 0, FrameWidth, FrameHeight), 1.0f);
    }
}