using Microsoft.Xna.Framework;

namespace Tank1460.SaveLoad.Settings;

public class PlayerControlSettings
{
    public PlayerIndex PlayerIndex;

    // У одного игрока может быть сразу несколько способов управления.
    public KeyboardControlsSettings Keyboard { get; set; }

    public string[] GamePadIds { get; set; }
}