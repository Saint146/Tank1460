using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Diagnostics;

namespace Tank1460;

/// <summary>
/// Controls playback of an Animation.
/// </summary>
public class AnimationPlayer
{
    /// <summary>
    /// The animation which is currently playing.
    /// </summary>
    public IAnimation Animation
    {
        get => _animation;
        private set
        {
            _animation = value;
            _visibleRect = new Rectangle(0, 0, _animation.FrameWidth, _animation.FrameHeight);
        }
    }
    private IAnimation _animation;

    public Rectangle VisibleRect
    {
        get => _visibleRect;
        set
        {
            Debug.Assert(value.X >= 0);
            Debug.Assert(value.Y >= 0);
            Debug.Assert(value.Width <= Animation.FrameWidth);
            Debug.Assert(value.Height <= Animation.FrameHeight);
            _visibleRect = value;
        }
    }
    private Rectangle _visibleRect;

    /// <summary>
    /// Begins or continues playback of an animation.
    /// </summary>
    public virtual void PlayAnimation(IAnimation animation)
    {
        // If this animation is already running, do not restart it.
        if (Animation == animation)
            return;

        // Start the new animation.
        Animation = animation;
        PlayAnimation();
    }

    public bool HasAnimationEnded => Animation.HasEnded;

    public virtual void PlayAnimation()
    {
        Animation.Reset();
    }

    public void Draw(SpriteBatch spriteBatch, Vector2 position, float scale = 1.0f)
    {
        if (Animation == null)
            throw new NotSupportedException("No animation is currently playing.");

        Animation.Draw(spriteBatch, position, VisibleRect, scale);
    }

    public void AdvanceAnimation()
    {
        Animation.Advance();
    }
}