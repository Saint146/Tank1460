using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Tank1460.Extensions;

namespace Tank1460;

public class Font
{
    private readonly Texture2D _texture;

    private const string
        Chars = @"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789.,""'?♥#&-:©!│║ "; // Дальше пока не надо.

    private const int CharWidth = 8;
    private const int CharHeight = 8;

    private readonly Dictionary<char, Rectangle> _charTexturePositions = new();

    public Font(ContentManager content, Color? fontColor = null)
    {
        _texture = content.Load<Texture2D>(@"Sprites/Hud/Font");
        if (fontColor.HasValue)
            _texture = _texture.RecolorAsCopy(Color.Black, fontColor.Value);

        InitTexturePositions();
    }

    public void Draw(string text, SpriteBatch spriteBatch, Vector2 startingPosition)
    {
        var position = startingPosition;
        foreach(var c in text)
        {
            spriteBatch.Draw(_texture, position, _charTexturePositions[c], Color.White);
            position.X += CharWidth;
        }
    }

    private void InitTexturePositions()
    {
        for (var i = 0; i < Chars.Length; i++)
        {
            _charTexturePositions.Add(Chars[i], new Rectangle(i * CharWidth, 0, CharWidth, CharHeight));
        }
    }
}