using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tank1460.Common.Extensions;

public static class SpriteBatchExtensions
{
    public static void Draw(this SpriteBatch spriteBatch, Texture2D texture, Point position, Color color) =>
        spriteBatch.Draw(texture, position.ToVector2(), color);

}