using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using Tank1460.Common.Extensions;

namespace Tank1460.Extensions;

internal static class SpriteBatchExtensions
{
    /// <summary>
    /// Нарисовать столько меток, сколько передано.
    /// </summary>
    public static void DrawDebugMark(this SpriteBatch spriteBatch, Rectangle rectangle, Color color, int mark = 0)
    {
        const float radius = 3.5f;

        switch (mark)
        {
            case 0:
                spriteBatch.DrawEllipse(rectangle.Center.ToVector2(), new Vector2(radius), 16, color, 0.8f);
                break;

            case 1:
                spriteBatch.DrawLine(rectangle.Center.ToVector2(), radius, -(float)Math.PI * 0.75f, color, 0.8f);
                break;

            case 2:
                spriteBatch.DrawLine(rectangle.Center.ToVector2(), radius, -(float)Math.PI * 0.25f, color, 0.8f);
                spriteBatch.DrawLine(rectangle.Center.ToVector2(), radius, -(float)Math.PI * 0.75f, color, 0.8f);
                break;

            default:
                const float angle = -(float)Math.PI * 0.75f;
                var deltaAngle = 2 * (float)Math.PI / mark;
                for (var i = 0; i < mark; i++)
                    spriteBatch.DrawLine(rectangle.Center.ToVector2(), radius, angle + deltaAngle * i, color, 0.8f);
                break;
        }
    }

    /// <summary>
    /// Нарисовать стрелку, показывающую в направлении указанной цели.
    /// </summary>
    public static void DrawDebugArrow(this SpriteBatch spriteBatch, Rectangle rectangle, Color color, Point? target)
    {
        const float pi = (float)Math.PI;
        const float radius = 5.5f;

        if (target is null)
        {
            spriteBatch.DrawDebugMark(rectangle, color);
            return;
        }

        var center = rectangle.Center.ToVector2();
        var angle = (float)center.GetAngleTo(target.Value.ToVector2());
        var endPoint = center.GetPointByAngleAndDistance(angle, radius);

        spriteBatch.DrawLine(center, endPoint, color, 0.8f);
        spriteBatch.DrawLine(endPoint, radius / 2, pi + angle + 0.7f, color, 0.8f);
        spriteBatch.DrawLine(endPoint, radius / 2, pi + angle - 0.7f, color, 0.8f);
    }

    public static void DrawReticle(this SpriteBatch spriteBatch, Color color, Point target, float radius = 8f)
    {
        const float outsideMargin = 1.5f;

        spriteBatch.DrawCircle(target.ToVector2(), radius, 12, color);
        spriteBatch.DrawLine(new Vector2(target.X - radius - outsideMargin, target.Y),
                             new Vector2(target.X - radius / 3, target.Y),
                             color);

        spriteBatch.DrawLine(new Vector2(target.X + radius + outsideMargin, target.Y),
                             new Vector2(target.X + radius / 3, target.Y),
                             color);

        spriteBatch.DrawLine(new Vector2(target.X, target.Y - radius - outsideMargin),
                             new Vector2(target.X, target.Y - radius / 3),
                             color);

        spriteBatch.DrawLine(new Vector2(target.X, target.Y + radius + outsideMargin),
                             new Vector2(target.X, target.Y + radius / 3),
                             color);
    }
}