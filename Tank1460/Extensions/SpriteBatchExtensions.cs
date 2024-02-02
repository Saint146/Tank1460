using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace Tank1460.Extensions;

public static class SpriteBatchExtensions
{
    public static void Draw(this SpriteBatch spriteBatch, Texture2D texture, Point position, Color color) =>
        spriteBatch.Draw(texture, position.ToVector2(), color);

    /// <summary>
    /// Нарисовать столько меток, сколько передано.
    /// </summary>
    public static void DrawDebugMark(this SpriteBatch spriteBatch, Rectangle rectangle, Color color, int mark = 0)
    {
        const float radius = 3.5f;

        switch (mark)
        {
            case 0:
                spriteBatch.DrawEllipse(rectangle.Center.ToVector2(), new Vector2(radius), 16, color);
                break;

            case 1:
                spriteBatch.DrawLine(rectangle.Center.ToVector2(), radius, -(float)Math.PI * 0.75f, color);
                break;

            case 2:
                spriteBatch.DrawLine(rectangle.Center.ToVector2(), radius, -(float)Math.PI * 0.25f, color);
                spriteBatch.DrawLine(rectangle.Center.ToVector2(), radius, -(float)Math.PI * 0.75f, color);
                break;

            default:
                const float angle = -(float)Math.PI * 0.75f;
                var deltaAngle = 2 * (float)Math.PI / mark;
                for (var i = 0; i < mark; i++)
                    spriteBatch.DrawLine(rectangle.Center.ToVector2(), radius, angle + deltaAngle * i, color);
                break;
        }
    }

    /// <summary>
    /// Нарисовать стрелку, показывающую в направлении указанной цели
    /// </summary>
    public static void DrawDebugArrow(this SpriteBatch spriteBatch, Rectangle rectangle, Color color, Point? target)
    {
        const float radius = 3.5f;

        if (target is null)
        {
            spriteBatch.DrawDebugMark(rectangle, color);
            return;
        }

        var center = rectangle.Center.ToVector2();
        var angle = (float)center.GetAngleTo(target.Value.ToVector2());
        spriteBatch.DrawLine(center, radius, angle, color);
    }
}