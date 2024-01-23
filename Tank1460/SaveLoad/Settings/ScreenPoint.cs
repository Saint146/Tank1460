using Microsoft.Xna.Framework;

namespace Tank1460.SaveLoad.Settings;

// TODO: Почему-то обычный Point не хочет автоматически сериализоваться, так-то нафиг не нужно это дублирование.
public struct ScreenPoint
{
    public int X { get; set; }
    public int Y { get; set; }

    public static ScreenPoint FromPoint(Point point) => new()
    {
        X = point.X,
        Y = point.Y
    };

    public Point ToPoint() => new()
    {
        X = X,
        Y = Y
    };
}