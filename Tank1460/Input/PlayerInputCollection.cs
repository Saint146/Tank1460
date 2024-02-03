using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Tank1460.Common.Extensions;

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