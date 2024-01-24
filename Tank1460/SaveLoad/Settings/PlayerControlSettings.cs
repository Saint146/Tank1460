using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;

namespace Tank1460.SaveLoad.Settings;

public class PlayerControlSettings
{
    [JsonConverter(typeof(JsonStringEnumMemberConverter))]
    [JsonPropertyName("Player")]
    public PlayerIndex PlayerIndex { get; set; }

    // У одного игрока может быть сразу несколько способов управления.
    public KeyboardControlsSettings Keyboard { get; set; }

    public string[] GamePadIds { get; set; }
}