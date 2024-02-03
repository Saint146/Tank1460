﻿using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Diagnostics;
using Tank1460.Common.Extensions;

namespace Tank1460;

public class Font
{
    public int CharWidth { get; }

    public int CharHeight { get; }

    private const string
        Chars = @"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789.,""'?♥#&-:©!│║ "; // Дальше пока не надо.

    private readonly Texture2D _texture;
    private readonly Color[] _textureData;
    private readonly Dictionary<char, Rectangle> _charTexturePositions = new();

    public Font(Texture2D fontTexture)
    {
        ArgumentNullException.ThrowIfNull(fontTexture);

        _texture = fontTexture;
        _textureData = new Color[_texture.Width * _texture.Height];
        _texture.GetData(_textureData);

        CharHeight = _texture.Height;
        CharWidth = _texture.Width / Chars.Length;

        InitTexturePositions();
    }

    public void Draw(string text, SpriteBatch spriteBatch, Point startingPosition)
    {
        ArgumentException.ThrowIfNullOrEmpty(text);

        var position = startingPosition.ToVector2();
        foreach (var c in text)
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

    /// <summary>
    /// Создать текстуру из надписи. Поддерживает многострочные надписи.
    /// </summary>
    public Texture2D CreateTexture(GraphicsDevice graphics, string text)
    {
        ArgumentException.ThrowIfNullOrEmpty(text);

        var lines = text.SplitIntoLines().TopAllToMaxLength();
        var linesCount = lines.Length;
        var lineLength = lines[0].Length;

        var linePixelCount = CharWidth * CharHeight * lineLength;

        var t = new Texture2D(graphics, CharWidth * lineLength, CharHeight * linesCount);
        var data = new Color[t.Width * t.Height];

        for (var i = 0; i < data.Length; i++)
        {
            var c = lines[i / linePixelCount][i % linePixelCount % t.Width / CharWidth];
            data[i] = _textureData[i % linePixelCount % CharWidth + CharWidth * Chars.IndexOf(c) + i % linePixelCount / t.Width * _texture.Width];
        }

        t.SetData(data);
        return t;
    }
}