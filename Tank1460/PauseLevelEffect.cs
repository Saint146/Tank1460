﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Content;
using Tank1460.Common.Extensions;

namespace Tank1460;

public class PauseLevelEffect : LevelEffect
{
    protected readonly TimedAnimationPlayer Sprite = new();
    private IAnimation _animation;

    public override bool CanUpdateWhenGameIsPaused => true;

    public PauseLevelEffect(Level level) : base(level)
    {
        LoadContent(level.Content);
        Sprite.PlayAnimation(_animation);
    }

    public override void Update(GameTime gameTime)
    {
        Sprite.ProcessAnimation(gameTime);
    }

    public override void Draw(SpriteBatch spriteBatch, Rectangle levelBounds)
    {
        Sprite.Draw(spriteBatch, levelBounds.Center - Sprite.VisibleRect.Size.Divide(2));
    }

    private void LoadContent(ContentManagerEx content)
    {
        var font = content.LoadFont(@"Sprites/Font/Pixel8", new Color(0xff0027d1));
        var textTexture = font.CreateTexture(content.GetGraphicsDevice(), "PAUSE");

        _animation = new BlinkingAnimation(textTexture, 16 * Tank1460Game.OneFrameSpan);
        Sprite.PlayAnimation(_animation);
    }
}