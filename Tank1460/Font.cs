using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Tank1460.Common.Extensions;

namespace Tank1460;

public class Font
{
    public int CharWidth { get; }

    public int CharHeight { get; }

    private const string DefaultChars = @"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789.,""'?♥#&-_:©!‼ⅠⅡⅢⅣ↑↓→←☐☑ ";

    private readonly string _chars;

    private readonly Texture2D _texture;
    private readonly Color[] _textureData;
    private readonly Dictionary<char, Rectangle> _charTexturePositions = new();

    public Font(Texture2D fontTexture, string chars)
    {
        ArgumentNullException.ThrowIfNull(fontTexture);
        ArgumentException.ThrowIfNullOrEmpty(chars);

        _texture = fontTexture;
        _textureData = new Color[_texture.Width * _texture.Height];
        _texture.GetData(_textureData);

        _chars = chars;
        CharHeight = _texture.Height;
        CharWidth = _texture.Width / _chars.Length;

        InitTexturePositions();
    }

    public Font(Texture2D fontTexture) : this(fontTexture, DefaultChars)
    {
    }

    public void Draw(string text, SpriteBatch spriteBatch, Point startingPosition)
    {
        ArgumentException.ThrowIfNullOrEmpty(text);

        var position = startingPosition.ToVector2();
        foreach (var c in text)
        {
            if (c != ' ')
                spriteBatch.Draw(_texture, position, _charTexturePositions[c], Color.White);

            position.X += CharWidth;
        }
    }

    public Point GetTextSize(string text)
    {
        if (text?.Length is null or 0)
            return Point.Zero;

        var lines = SplitIntoLines(text);
        return new(CharWidth * lines[0].Length, CharHeight * lines.Length);
    }

    public Rectangle GetTextRectangle(string text, Point startingPosition) =>
        new(startingPosition, GetTextSize(text));

    /// <summary>
    /// Создать текстуру из надписи. Поддерживает многострочные надписи.
    /// </summary>
    public Texture2D CreateTexture(string text)
    {
        ArgumentException.ThrowIfNullOrEmpty(text);

        var graphics = _texture.GraphicsDevice;
        var lines = SplitIntoLines(text);
        var linesCount = lines.Length;
        var lineLength = lines[0].Length;

        var linePixelCount = CharWidth * CharHeight * lineLength;

        var t = new Texture2D(graphics, CharWidth * lineLength, CharHeight * linesCount);
        var data = new Color[t.Width * t.Height];

        for (var i = 0; i < data.Length; i++)
        {
            var c = lines[i / linePixelCount][i % linePixelCount % t.Width / CharWidth];
            data[i] = _textureData[i % linePixelCount % CharWidth + CharWidth * _chars.IndexOf(c) + i % linePixelCount / t.Width * _texture.Width];
        }

        t.SetData(data);
        return t;
    }

    /// <summary>
    /// Создать шрифт, где каждый пиксель оригинального шрифта заменяется на переданную текстуру.
    /// </summary>
    public Font CreateFontUsingTextureAsPixel(Texture2D pixelTexture)
    {
        ArgumentNullException.ThrowIfNull(pixelTexture);

        var graphics = _texture.GraphicsDevice;
        var t = new Texture2D(graphics, _charTexturePositions.Count * pixelTexture.Width * _texture.Width, pixelTexture.Height * _texture.Height);
        var patternData = pixelTexture.ToColorData();

        var data = new Color[t.Width * t.Height];

        for (var y = 0; y < _texture.Height; y++)
            for (var x = 0; x < _texture.Width; x++)
            {
                var originalTextureDataIndex = y * _charTexturePositions.Count + x;

                var fontData = _textureData[originalTextureDataIndex];
                if (fontData.A == 0)
                    continue;

                var newTextureDataStartingDataIndex = y * _charTexturePositions.Count * pixelTexture.Height + x * pixelTexture.Width;

                for (var i = 0; i < patternData.Length; i++)
                    data[newTextureDataStartingDataIndex + i / pixelTexture.Width * t.Width + i % pixelTexture.Width] = patternData[i];
            }

        t.SetData(data);
        return new Font(t);
    }

    /// <summary>
    /// Создать шрифт, где каждый символ этого шрифта выкладывается переданной текстурой.
    /// </summary>
    public Font CreateFontUsingTextureAsPattern(Texture2D patternTexture)
    {
        ArgumentNullException.ThrowIfNull(patternTexture);

        // TODO: Написать нормально, крч.
        //if(patternTexture.Width % _texture.Width != 0 ||
        //   patternTexture.Height % _texture.Height !=0 ||
        //   patternTexture.Width / _texture.Width != patternTexture.Height / _texture.Height)
        //    throw new Exception("Соотношение сторон переданной текстуры должно совпадать с соотношением сторон исходной текстуры шрифта, а сама текстура должна быть ровно в ")

        var scale = patternTexture.Height / _texture.Height;

        var graphics = _texture.GraphicsDevice;
        var t = new Texture2D(graphics, _charTexturePositions.Count * patternTexture.Width, patternTexture.Height);
        var patternData = patternTexture.ToColorData();

        var data = new Color[t.Width * t.Height];

        var originalTextureDataIndex = 0;
        for (var y = 0; y < _texture.Height; y++)
        {
            for (var x = 0; x < _texture.Width; x++)
            {
                var fontData = _textureData[originalTextureDataIndex];
                originalTextureDataIndex++;
                if (fontData.A == 0)
                    continue;

                var newTextureY = y * scale;
                var newTextureX = x * scale;

                var newTextureStartingDataIndex = newTextureY * t.Width + newTextureX;

                var patternOffsetX = newTextureX % patternTexture.Width;
                var patternOffsetY = newTextureY % patternTexture.Height;

                for (var patternY = 0; patternY < scale; patternY++)
                {
                    for (var patternX = 0; patternX < scale; patternX++)
                    {
                        var patternTextureDataIndex = (patternOffsetY + patternY) * patternTexture.Width + patternOffsetX + patternX;
                        var newTextureDataIndex = newTextureStartingDataIndex + patternY * t.Width + patternX;
                        data[newTextureDataIndex] = patternData[patternTextureDataIndex];
                    }
                }
            }
        }

        t.SetData(data);

        return new Font(t);
    }

    private void InitTexturePositions()
    {
        for (var i = 0; i < _chars.Length; i++)
        {
            _charTexturePositions.Add(_chars[i], new Rectangle(i * CharWidth, 0, CharWidth, CharHeight));
        }
    }

    private static string[] SplitIntoLines(string text) =>
        text.SplitIntoLines().TopAllToMaxLength();

    private static FontTextOptions _defaultOptions = new()
    {
        CharSpacing = 0,
        LineSpacing = 0
    };
}

public class FontTextOptions
{
    public int CharSpacing { get; set; }

    public int LineSpacing { get; set; }
}