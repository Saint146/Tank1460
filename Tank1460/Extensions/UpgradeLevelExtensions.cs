using System;
using Tank1460.LevelObjects;

namespace Tank1460.Extensions;

public static class UpgradeLevelExtensions
{
    public static UpgradeLevel LevelUp(this UpgradeLevel level)
    {
        if (Enum.IsDefined(typeof(UpgradeLevel), level + 1))
            level++;

        return level;
    }

    public static UpgradeLevel LevelDown(this UpgradeLevel level)
    {
        if (Enum.IsDefined(typeof(UpgradeLevel), level - 1))
            level--;

        return level;
    }
}