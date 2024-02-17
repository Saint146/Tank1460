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

    public static MouseState CopyWithAllButtonsReleased(this MouseState mouseState) =>
        new(
            mouseState.X,
            mouseState.Y,
            mouseState.ScrollWheelValue,
            ButtonState.Released,
            ButtonState.Released,
            ButtonState.Released,
            ButtonState.Released,
            ButtonState.Released
        );
}