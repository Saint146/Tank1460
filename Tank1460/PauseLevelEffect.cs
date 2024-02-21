using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tank1460.Common.Extensions;
using Tank1460.Globals;

namespace Tank1460;

public class PauseLevelEffect : LevelEffect
{
    protected readonly TimedAnimationPlayer Sprite = new();
    private IAnimation _animation;

    private const string Text = "PAUSE";

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
        var textTexture = font.CreateTexture(Text);

        _animation = new BlinkingAnimation(textTexture, GameRules.TimeInFrames(16));
    }
}