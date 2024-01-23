using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using Tank1460.SaveLoad.Settings;

namespace Tank1460.PlayerInput;

internal static class InputDefaults
{
    internal static Dictionary<Buttons, PlayerInputs> GetDefaultGamepadBindings() => new()
    {
        { Buttons.DPadUp, PlayerInputs.Up },
        { Buttons.DPadDown, PlayerInputs.Down },
        { Buttons.DPadLeft, PlayerInputs.Left },
        { Buttons.DPadRight, PlayerInputs.Right },
        { Buttons.A, PlayerInputs.Shoot },
        { Buttons.B, PlayerInputs.Shoot },
        { Buttons.Start, PlayerInputs.Start }
    };

    internal static KeyboardControlsSettings GetDefaultPlayersKeyboardBindings(PlayerIndex playerIndex)
        => playerIndex switch
        {
            PlayerIndex.One => new()
            {
                Bindings = new[]
                {
                    new KeyboardBinding
                        { Command = PlayerInputs.Up, Keys = new[] { Keys.W } },
                    new KeyboardBinding
                        { Command = PlayerInputs.Down, Keys = new[] { Keys.S } },
                    new KeyboardBinding
                        { Command = PlayerInputs.Left, Keys = new[] { Keys.A } },
                    new KeyboardBinding
                        { Command = PlayerInputs.Right, Keys = new[] { Keys.D } },
                    new KeyboardBinding
                        { Command = PlayerInputs.Shoot, Keys = new[] { Keys.K } },
                    new KeyboardBinding
                        { Command = PlayerInputs.Start, Keys = new[] { Keys.X, Keys.Space } }
                }
            },

            PlayerIndex.Two => new()
            {
                Bindings = new[]
                {
                    new KeyboardBinding
                        { Command = PlayerInputs.Up, Keys = new[] { Keys.Up } },
                    new KeyboardBinding
                        { Command = PlayerInputs.Down, Keys = new[] { Keys.Down } },
                    new KeyboardBinding
                        { Command = PlayerInputs.Left, Keys = new[] { Keys.Left } },
                    new KeyboardBinding
                        { Command = PlayerInputs.Right, Keys = new[] { Keys.Right } },
                    new KeyboardBinding
                        { Command = PlayerInputs.Shoot, Keys = new[] { Keys.NumPad0 } },
                    new KeyboardBinding
                        { Command = PlayerInputs.Start, Keys = new[] { Keys.Enter } }
                }
            },
            _ => throw new ArgumentOutOfRangeException(nameof(playerIndex), playerIndex, null)
        };
}