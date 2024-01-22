using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;

namespace Tank1460.SaveLoad;

public class SettingsData
{
    public GameSettingsData Game { get; set; }

    public ScreenSettingsData Screen { get; set; }
}

public class GameSettingsData
{
    public bool CustomCursor { get; set; }
}

public class ScreenSettingsData
{
    public ScreenMode Mode { get; set; }

    public ScreenPoint Position { get; set; }

    public ScreenPoint Size { get; set; }

    public bool IsMaximized { get; set; }
}

[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum ScreenMode
{
    Window,
    Borderless
}

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