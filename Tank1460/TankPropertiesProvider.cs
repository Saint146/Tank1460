using System.Collections.Generic;
using Tank1460.Common.Level.Object.Tank;
using Tank1460.LevelObjects;

namespace Tank1460;

public static class TankPropertiesProvider
{
    public static TankProperties Get(TankType tankType) => TankTypePropertiesMap[tankType];

    private static readonly Dictionary<TankType, TankProperties> TankTypePropertiesMap = new()
    {
        { TankType.Type0, new TankProperties(0.75, 1, ShellSpeed.Normal, ShellProperties.Normal) },
        { TankType.Type1, new TankProperties(0.75, 1, ShellSpeed.Fast, ShellProperties.Normal) },
        { TankType.Type2, new TankProperties(0.75, 2, ShellSpeed.Fast, ShellProperties.Normal) },
        { TankType.Type3, new TankProperties(0.75, 2, ShellSpeed.Fast, ShellProperties.ArmorPiercing | ShellProperties.Pruning) },
        { TankType.Type4, new TankProperties(0.50, 1, ShellSpeed.Normal, ShellProperties.Normal) },
        { TankType.Type5, new TankProperties(1.00, 1, ShellSpeed.Normal, ShellProperties.Normal) },
        { TankType.Type6, new TankProperties(0.50, 1, ShellSpeed.Fast, ShellProperties.Normal) },
        { TankType.Type7, new TankProperties(0.50, 1, ShellSpeed.Normal, ShellProperties.Normal) }
    };
}