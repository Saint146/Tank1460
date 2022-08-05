using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tank1460;

public interface IAnimation
{
    int FrameWidth { get; }

    int FrameHeight { get; }

    bool HasEnded { get; }

    void Reset();

    void Advance();

    void Process(GameTime gameTime);

    void Draw(SpriteBatch spriteBatch, Vector2 position, Rectangle visibleRectangle, float scale);
}