using System;
using System.Collections.Generic;
using System.Linq;
using Tank1460.Common.Level.Object.Tank;

namespace Tank1460;

internal class PlayerLevelStats
{
    public Dictionary<TankType, int> BotsDefeated { get; set; } = new();

    public PlayerLevelStats()
    {
        //BotsDefeated = Enum.GetValues<TankType>().ToDictionary(type => type, _ => 0);
    }
}