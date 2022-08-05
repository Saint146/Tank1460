using System.Collections.Generic;
using Tank1460.LevelObjects;

namespace Tank1460;

public static class ShootingPropertiesProvider
{
    private static readonly ShootingProperties EnemyDefaultProperties = new(1, ShellSpeed.Normal, ShellDamage.Normal);

    private static readonly Dictionary<UpgradeLevel, ShootingProperties> PlayerDefaultProperties = new()
    {
        { UpgradeLevel.Basic, new ShootingProperties(1, ShellSpeed.Normal, ShellDamage.Normal) },
        { UpgradeLevel.FastBullet, new ShootingProperties(1, ShellSpeed.Fast, ShellDamage.Normal) },
        { UpgradeLevel.DoubleFastBullet, new ShootingProperties(2, ShellSpeed.Fast, ShellDamage.Normal) },
        { UpgradeLevel.ArmorPiercing, new ShootingProperties(2, ShellSpeed.Fast, ShellDamage.ArmorPiercing) }
    };

    public static ShootingProperties GetEnemyProperties() => EnemyDefaultProperties;

    public static ShootingProperties GetPlayerProperties(UpgradeLevel upgradeLevel) => PlayerDefaultProperties[upgradeLevel];
}