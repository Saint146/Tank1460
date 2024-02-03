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
}