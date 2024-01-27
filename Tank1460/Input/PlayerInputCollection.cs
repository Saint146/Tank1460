using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Tank1460.Extensions;

namespace Tank1460.Input;

public class PlayerInputCollection : Dictionary<PlayerIndex, PlayerInput>
{
    public PlayerInputCollection()
    {
    }

    public PlayerInputCollection(IEnumerable<PlayerIndex> playerIndices)
    {
        playerIndices.ForEach(playerIndex => this[playerIndex] = new PlayerInput());
    }

    public void ClearInputs()
    {
        Values.ForEach(input => input.Clear());
    }
}