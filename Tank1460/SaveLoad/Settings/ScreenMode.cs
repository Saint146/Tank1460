using System.Text.Json.Serialization;

namespace Tank1460.SaveLoad.Settings;

[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum ScreenMode
{
    Window,
    Borderless
}