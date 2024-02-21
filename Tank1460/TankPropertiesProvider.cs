using System.Collections.Generic;
using Tank1460.Common.Level.Object.Tank;
using Tank1460.LevelObjects;

namespace Tank1460;

public static class TankPropertiesProvider
{
    public static TankProperties Get(TankType tankType) => TankTypePropertiesMap[tankType];

    private static readonly Dictionary<TankType, TankProperties> TankTypePropertiesMap = new()
    {
        { TankType.P0, new TankProperties(0.75, 1, ShellSpeed.Normal, ShellProperties.Normal) },
        { TankType.P1, new TankProperties(0.75, 1, ShellSpeed.Fast, ShellProperties.Normal) },
        { TankType.P2, new TankProperties(0.75, 2, ShellSpeed.Fast, ShellProperties.Normal) },
        { TankType.P3, new TankProperties(0.75, 2, ShellSpeed.Fast, ShellProperties.ArmorPiercing) },
        { TankType.P4, new TankProperties(0.75, 2, ShellSpeed.Fast, ShellProperties.ArmorPiercing | ShellProperties.Pruning) },

        { TankType.B0, new TankProperties(0.50, 1, ShellSpeed.Normal, ShellProperties.Normal) },
        { TankType.B1, new TankProperties(1.00, 1, ShellSpeed.Normal, ShellProperties.Normal) },
        { TankType.B2, new TankProperties(0.50, 1, ShellSpeed.Fast, ShellProperties.Normal) },
        { TankType.B3, new TankProperties(0.50, 1, ShellSpeed.Normal, ShellProperties.Normal) },
        { TankType.B9, new TankProperties(0.50, 2, ShellSpeed.Fast, ShellProperties.ArmorPiercing) }
    };
}