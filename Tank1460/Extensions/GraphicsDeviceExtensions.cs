using Microsoft.Xna.Framework;

namespace Tank1460.Extensions;

public static class GraphicsDeviceExtensions
{
    /// <summary>
    /// Сменить размер виртуального экрана.
    /// TODO: Работает пока криво, придумать что-то.
    /// </summary>
    public static void ChangeSize(this GraphicsDeviceManager graphics, Point baseSize, Point? desiredSize = null)
    {
        if (desiredSize is null || desiredSize.Value.X < baseSize.X || desiredSize.Value.Y < baseSize.Y)
        {
            graphics.PreferredBackBufferWidth = baseSize.X;
            graphics.PreferredBackBufferHeight = baseSize.Y;
            return;
        }

        graphics.PreferredBackBufferWidth = desiredSize.Value.X;
        graphics.PreferredBackBufferHeight = desiredSize.Value.Y;
    }
}