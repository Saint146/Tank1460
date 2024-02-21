using System.Collections.Generic;
using Tank1460.Common.Level.Object.Tank;
using Tank1460.LevelObjects;

namespace Tank1460;

public static class TankPropertiesProvider
{
    public static TankProperties Get(TankType tankType) => TankTypePropertiesMap[tankType];

    private static readonly Dictionary<TankType, TankProperties> TankTypePropertiesMap = new()
    {
        { TankType.TypeP0, new TankProperties(0.75, 1, ShellSpeed.Normal, ShellProperties.Normal) },
        { TankType.TypeP1, new TankProperties(0.75, 1, ShellSpeed.Fast, ShellProperties.Normal) },
        { TankType.TypeP2, new TankProperties(0.75, 2, ShellSpeed.Fast, ShellProperties.Normal) },
        { TankType.TypeP3, new TankProperties(0.75, 2, ShellSpeed.Fast, ShellProperties.ArmorPiercing) },
        { TankType.TypeP4, new TankProperties(0.75, 2, ShellSpeed.Fast, ShellProperties.ArmorPiercing | ShellProperties.Pruning) },

        { TankType.TypeB0, new TankProperties(0.50, 1, ShellSpeed.Normal, ShellProperties.Normal) },
        { TankType.TypeB1, new TankProperties(1.00, 1, ShellSpeed.Normal, ShellProperties.Normal) },
        { TankType.TypeB2, new TankProperties(0.50, 1, ShellSpeed.Fast, ShellProperties.Normal) },
        { TankType.TypeB3, new TankProperties(0.50, 1, ShellSpeed.Normal, ShellProperties.Normal) }
    };
}