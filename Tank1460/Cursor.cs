using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Linq;

namespace Tank1460;

internal class Cursor
{
    public Point Position { get; private set; }

    protected readonly TimedAnimationPlayer Sprite = new();
    private IAnimation _animation;
    private float _scale = 1.0f;

    public Cursor(ContentManager content)
    {
        LoadContent(content);
        Sprite.PlayAnimation(_animation);
    }

    private void LoadContent(ContentManager content)
    {
        var allTypes = Enum.GetValues<CursorType>();

        var allCursors = allTypes.Select(type => content.Load<Texture2D>($"Sprites/Cursor/{type}")).ToArray();

        _animation = new ShiftingAnimation(allCursors, double.MaxValue, true, GameRules.TimeInFrames(240));
    }

    public void Update(GameTime gameTime, MouseState mouseState, float scale)
    {
        _animation.Process(gameTime);
        Position = mouseState.Position;
        _scale = scale;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        Sprite.Draw(spriteBatch, Position.ToVector2(), _scale);
    }
}