using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Tank1460;

public class Font
{
    private readonly Texture2D _texture;

    private const string
        Chars = @"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789.,""'?♥#&-:©!│║ "; // Дальше пока не надо.

    private const int CharWidth = 8;
    private const int CharHeight = 8;

    private readonly Dictionary<char, Rectangle> _charTexturePositions = new();

    public Font(Texture2D fontTexture)
    {
        _texture = fontTexture;
        InitTexturePositions();
    }

    public void Draw(string text, SpriteBatch spriteBatch, Point startingPosition)
    {
        var position = startingPosition.ToVector2();
        foreach(var c in text)
        {
            spriteBatch.Draw(_texture, position, _charTexturePositions[c], Color.White);
            position.X += CharWidth;
        }
    }

    public Rectangle GetTextRectangle(string text, Point startingPosition) =>
        new(startingPosition.X, startingPosition.Y, CharWidth * text.Length, CharHeight);

    private void InitTexturePositions()
    {
        for (var i = 0; i < Chars.Length; i++)
        {
            _charTexturePositions.Add(Chars[i], new Rectangle(i * CharWidth, 0, CharWidth, CharHeight));
        }
    }
}