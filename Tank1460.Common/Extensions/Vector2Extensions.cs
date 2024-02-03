using System;
using Microsoft.Xna.Framework;

namespace Tank1460.Common.Extensions;

public static class Vector2Extensions
{
    public static Point RoundToPoint(this Vector2 vector)
    {
        return new Point((int)Math.Round(vector.X), (int)Math.Round(vector.Y));
    }

    public static double GetAngleTo(this Vector2 vector, Vector2 target)
    {
        return Math.Atan2(target.Y - vector.Y, target.X - vector.X);
    }

    /// <summary>
    /// Прибавить <paramref name="step"/> к данному вектору и проверить, не перешли уже через <paramref name="target"/>.
    /// Возвращает true, если переход не совершен, то есть если шагать ещё можно. Иначе — false.
    /// </summary>
    /// <remarks>
    /// Используется в объектах, которые безусловно идут от одной точки к другой. Когда метод возвращает false, объект достиг цели.
    /// </remarks>
    public static bool TryStep(this ref Vector2 vector, Vector2 step, Vector2 target)
    {
        vector += step;
        return !((step.X < 0 && vector.X < target.X) || (step.X > 0 && vector.X > target.X) ||
                 (step.Y < 0 && vector.Y < target.Y) || (step.Y > 0 && vector.Y > target.Y));
    }
}