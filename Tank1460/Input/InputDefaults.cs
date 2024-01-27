using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using Tank1460.SaveLoad.Settings;

namespace Tank1460.Input;

internal static class InputDefaults
{
    internal static Dictionary<Buttons, PlayerInputCommands> GetDefaultGamepadBindings() => new()
    {
        { Buttons.DPadUp, PlayerInputCommands.Up },
        { Buttons.DPadDown, PlayerInputCommands.Down },
        { Buttons.DPadLeft, PlayerInputCommands.Left },
        { Buttons.DPadRight, PlayerInputCommands.Right },
        { Buttons.A, PlayerInputCommands.Shoot },
        { Buttons.B, PlayerInputCommands.Shoot },
        { Buttons.Start, PlayerInputCommands.Start }
    };

    internal static KeyboardControlsSettings GetDefaultPlayersKeyboardBindings(PlayerIndex playerIndex)
        => playerIndex switch
        {
            PlayerIndex.One => new()
            {
                Bindings = new[]
                {
                    new KeyboardBinding
                        { Command = PlayerInputCommands.Up, Keys = new[] { Keys.W } },
                    new KeyboardBinding
                        { Command = PlayerInputCommands.Down, Keys = new[] { Keys.S } },
                    new KeyboardBinding
                        { Command = PlayerInputCommands.Left, Keys = new[] { Keys.A } },
                    new KeyboardBinding
                        { Command = PlayerInputCommands.Right, Keys = new[] { Keys.D } },
                    new KeyboardBinding
                        { Command = PlayerInputCommands.Shoot, Keys = new[] { Keys.K } },
                    new KeyboardBinding
                        { Command = PlayerInputCommands.Start, Keys = new[] { Keys.X, Keys.Space } }
                }
            },

            PlayerIndex.Two => new()
            {
                Bindings = new[]
                {
                    new KeyboardBinding
                        { Command = PlayerInputCommands.Up, Keys = new[] { Keys.Up } },
                    new KeyboardBinding
                        { Command = PlayerInputCommands.Down, Keys = new[] { Keys.Down } },
                    new KeyboardBinding
                        { Command = PlayerInputCommands.Left, Keys = new[] { Keys.Left } },
                    new KeyboardBinding
                        { Command = PlayerInputCommands.Right, Keys = new[] { Keys.Right } },
                    new KeyboardBinding
                        { Command = PlayerInputCommands.Shoot, Keys = new[] { Keys.NumPad0 } },
                    new KeyboardBinding
                        { Command = PlayerInputCommands.Start, Keys = new[] { Keys.Enter } }
                }
            },
            _ => throw new ArgumentOutOfRangeException(nameof(playerIndex), playerIndex, null)
        };
}