using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Tank1460;

/// <summary>
/// Анимация, позволяющая переключаться между текстурами одинакового размера прямо на ходу анимации.
/// Например, если спрайт играет основную анимацию, но ещё и должен мигать.
/// Фактически, это "двумерная" анимация, можно одженерить и запилить то же с N измерений.
/// </summary>
public class ShiftingAnimation : IAnimation
{
    public Point FrameSize => new (FrameWidth, FrameHeight);

    public int FrameWidth { get; }

    public int FrameHeight { get; }

    public bool HasEnded { get; private set; }

    public IReadOnlyList<Texture2D> Textures => _textures;
    private readonly List<Texture2D> _textures;

    public Texture2D ActiveTexture { get; private set; }

    private int TextureIndex { get; set; }

    private double[] FrameTimes { get; }

    private double[] ShiftTimes { get; }

    private bool IsLooping { get; }

    private int FrameCount { get; }

    private int FrameIndex { get; set; }
    private double _frameTime, _shiftTime;

    public ShiftingAnimation(IReadOnlyCollection<Texture2D> textures, double[] frameTimes, bool isLooping, double[] shiftTimes)
    {
        _textures = textures.ToList();
        if (textures.Count == 0)
            throw new Exception("There must be at least one texture.");
        ShiftTo(0);

        if (textures.Any(texture => texture.Bounds != ActiveTexture.Bounds))
            throw new Exception("All textures must be exactly the same size.");

        FrameTimes = frameTimes;
        IsLooping = isLooping;
        Debug.Assert(shiftTimes.Length == textures.Count);
        ShiftTimes = shiftTimes;

        FrameCount = frameTimes.Length;
        FrameWidth = ActiveTexture.Width / FrameCount;
        FrameHeight = ActiveTexture.Height;
    }

    public ShiftingAnimation(IReadOnlyCollection<Texture2D> textures, double[] frameTimes, bool isLooping, double shiftTime = double.MaxValue)
        : this(textures,
               frameTimes,
               isLooping,
               Enumerable.Repeat(shiftTime, textures.Count).ToArray())
    {
    }

    public ShiftingAnimation(IReadOnlyList<Texture2D> textures, double frameTime, bool isLooping, double shiftTime = double.MaxValue)
        : this(textures,
            Enumerable.Repeat(frameTime, textures[0].Width / textures[0].Height).ToArray(),
            isLooping,
            shiftTime)
    {
    }

    public ShiftingAnimation(IReadOnlyList<Texture2D> textures, bool isLooping)
        : this(textures, double.MaxValue, isLooping)
    {
    }

    public void ChangeTexture(int textureIndex, Texture2D newTexture)
    {
        if (newTexture.Width != ActiveTexture.Width || newTexture.Height != ActiveTexture.Height)
            throw new ArgumentException("New texture must be the same size as the existing ones.", nameof(newTexture));

        if (textureIndex < 0 || textureIndex >= Textures.Count)
            throw new ArgumentOutOfRangeException(nameof(textureIndex));

        _textures[textureIndex] = newTexture;

        // Перезаписать активную текстуру при необходимости.
        if (textureIndex == TextureIndex)
            ShiftTo(textureIndex);
    }

    public void Reset()
    {
        HasEnded = false;
        FrameIndex = 0;
        _frameTime = 0.0;
        _shiftTime = 0.0;
    }

    public void Advance()
    {
        if (IsLooping)
        {
            FrameIndex = (FrameIndex + 1) % FrameCount;
        }
        else
        {
            if (++FrameIndex < FrameCount)
                return;

            HasEnded = true;
            FrameIndex = FrameCount - 1;
        }
    }

    public void ShiftTo(int textureIndex)
    {
        TextureIndex = textureIndex;
        ActiveTexture = Textures[textureIndex];
    }

    public void Shift()
    {
        ShiftTo((TextureIndex + 1) % Textures.Count);
    }

    public void Process(GameTime gameTime)
    {
        _frameTime += gameTime.ElapsedGameTime.TotalSeconds;
        while (_frameTime > FrameTimes[FrameIndex])
        {
            _frameTime -= FrameTimes[FrameIndex];
            Advance();
        }

        _shiftTime += gameTime.ElapsedGameTime.TotalSeconds;
        while (_shiftTime > ShiftTimes[TextureIndex])
        {
            _shiftTime -= ShiftTimes[TextureIndex];
            Shift();
        }
    }

    public void Draw(SpriteBatch spriteBatch, Point position)
    {
        Draw(spriteBatch, position.ToVector2(), new Rectangle(0, 0, FrameWidth, FrameHeight), 1.0f);
    }

    public void Draw(SpriteBatch spriteBatch, Vector2 position, Rectangle visibleRectangle, float scale)
    {
        var source = new Rectangle(visibleRectangle.X + FrameIndex * FrameHeight, visibleRectangle.Y, visibleRectangle.Width, visibleRectangle.Height);

        spriteBatch.Draw(ActiveTexture, position + visibleRectangle.Location.ToVector2(), source, Color.White, 0.0f, new Vector2(0,0), scale, SpriteEffects.None, 0.0f);
    }
}