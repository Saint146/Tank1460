using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Input;

namespace Tank1460.Extensions;

public static class GamePadExtensions
{
    public static Dictionary<int, GamePadCapabilities> GetAllConnectedGamepads()
    {
        return Enumerable.Range(0, GamePad.MaximumGamePadCount)
                         .Select(i => (Index: i, Capabilities: GamePad.GetCapabilities(i)))
                         .Where(c => c.Capabilities.IsConnected)
                         .ToDictionary(c => c.Index, c => c.Capabilities);
    }
}