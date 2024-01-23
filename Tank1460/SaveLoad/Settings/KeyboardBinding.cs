using System.Text.Json.Serialization;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using Tank1460.PlayerInput;

namespace Tank1460.SaveLoad.Settings;

public class KeyboardBinding
{
    [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumMemberConverter))]
    public PlayerInputs Command { get; set; }

    // TODO: Не работает этот конвертер так, всё равно пишет числами.
    [JsonProperty(ItemConverterType = typeof(JsonStringEnumMemberConverter))]
    public Keys[] Keys { get; set; }
}