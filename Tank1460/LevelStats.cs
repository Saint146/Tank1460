using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Tank1460;

internal class LevelStats
{
    public Dictionary<PlayerIndex, PlayerLevelStats> PlayerStats { get; set; }

    public LevelStats(IEnumerable<PlayerIndex> playersInGame)
    {
        PlayerStats = playersInGame.ToDictionary(playerIndex => playerIndex,
                                                 _ => new PlayerLevelStats());
    }
}