
using Microsoft.Xna.Framework;
using System;

namespace Tank1460.Extensions
{
    public static class Vector2Extensions
    {
        public static Point RoundToPoint(this Vector2 vector)
        {
            return new Point((int)Math.Round(vector.X), (int)Math.Round(vector.Y));
        }
    }
}
