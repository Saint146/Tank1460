using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tank1460;

public class Animation : IAnimation
{
    public int FrameWidth { get; }

    public int FrameHeight { get; }

    public bool HasEnded { get; private set; }

    /// <summary>
    /// All frames in the animation arranged horizontally.
    /// </summary>
    private Texture2D Texture { get; }

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

    public Animation(Texture2D texture, double[] frameTimes, bool isLooping)
    {
        Texture = texture;
        FrameTimes = frameTimes;
        IsLooping = isLooping;

        FrameCount = frameTimes.Length;
        FrameWidth = Texture.Width / FrameCount;
        FrameHeight = Texture.Height;
    }

    public Animation(Texture2D texture, double frameTime, bool isLooping)
        : this(texture,
            // Assume square frames.
            Enumerable.Repeat(frameTime, texture.Width / texture.Height).ToArray(),
            isLooping)
    {
    }

    public Animation(Texture2D texture, bool isLooping) : this(texture, double.MaxValue, isLooping)
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
        var source = new Rectangle(visibleRectangle.X + FrameIndex * Texture.Height, visibleRectangle.Y, visibleRectangle.Width, visibleRectangle.Height);

        spriteBatch.Draw(Texture, position + visibleRectangle.Location.ToVector2(), source, Color.White, 0.0f, new Vector2(0,0), scale, SpriteEffects.None, 0.0f);
    }
}