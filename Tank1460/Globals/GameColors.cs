using Microsoft.Xna.Framework;

namespace Tank1460.Globals;

internal static class GameColors
{
    public static Color White { get; } = Color.White;

    public static Color Yellow { get; } = new(0xff3898fc);

    public static Color Red { get; } = new(0xff0027d1);

    public static Color Black { get; } = Color.Black;

    public static Color Curtain { get; } = new(0xff7f7f7f);

    public static Color LevelBack => Black;

    public static Color DefaultTextPressed { get; } = new(0xff250060);

    public static Color BlackTextShadow { get; } = new(0xffbbbbbb);
}