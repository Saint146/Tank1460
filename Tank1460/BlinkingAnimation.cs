using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Tank1460.Common.Extensions;

namespace Tank1460;

/// <summary>
/// Отличается от обычной анимации тем, что каждый кадр здесь представлен отдельной текстурой.
/// </summary>
public class BlinkingAnimation : IAnimation
{
    public int FrameWidth { get; }

    public int FrameHeight { get; }

    public bool HasEnded { get; private set; }

    private Texture2D[] Textures { get; }

    private double[] FrameTimes { get; }

    private bool IsLooping { get; }

    private int FrameCount { get; }

    /// <summary>
    /// Gets the index of the current frame in the animation.
    /// </summary>
    private int FrameIndex { get; set; }

    /// <summary>
    /// The amount of time in seconds that the current frame has been shown for.
    /// </summary>
    private double _time;

    public BlinkingAnimation(IEnumerable<Texture2D> textures, double[] frameTimes, bool isLooping)
    {
        Textures = textures?.ToArray() ?? throw new ArgumentNullException(nameof(textures));

        if (Textures.Length != frameTimes.Length)
            throw new Exception("Number of frameTimes should be equal to number of textures.");

        if (!Textures.TryGetFirst(out var firstNotNullTexture, t => t is not null))
            throw new Exception("At least one texture of a blinking animation should be not null.");

        FrameTimes = frameTimes;
        IsLooping = isLooping;

        FrameCount = frameTimes.Length;
        FrameWidth = firstNotNullTexture.Width;
        FrameHeight = firstNotNullTexture.Height;
    }

    public BlinkingAnimation(ICollection<Texture2D> textures, double frameTime, bool isLooping)
        : this(textures.ToList(),
               // Assume square frames.
               Enumerable.Repeat(frameTime, textures.Count).ToArray(),
               isLooping)
    {
    }

    public BlinkingAnimation(Texture2D texture, double frameTime)
        : this(new[] { texture, null },
               frameTime,
               true)
    {
    }

    public void Reset()
    {
        HasEnded = false;
        FrameIndex = 0;
        _time = 0.0;
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

    public void Process(GameTime gameTime)
    {
        _time += gameTime.ElapsedGameTime.TotalSeconds;

        while (_time > FrameTimes[FrameIndex])
        {
            _time -= FrameTimes[FrameIndex];
            Advance();
        }
    }

    public void Draw(SpriteBatch spriteBatch, Vector2 position, Rectangle visibleRectangle, float scale)
    {
        var texture = Textures[FrameIndex];
        if (texture is null)
            return;

        spriteBatch.Draw(texture, position + visibleRectangle.Location.ToVector2(), visibleRectangle, Color.White, 0.0f, new Vector2(0,0), scale, SpriteEffects.None, 0.0f);
    }
}