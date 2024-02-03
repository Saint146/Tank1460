using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Tank1460.Common.Extensions;

public static class MouseStateExtensions
{
    public static MouseState CopyWithPosition(this MouseState mouseState, Point position) =>
        new(
            position.X,
            position.Y,
            mouseState.ScrollWheelValue,
            mouseState.LeftButton,
            mouseState.MiddleButton,
            mouseState.RightButton,
            mouseState.XButton1,
            mouseState.XButton2
        );
}