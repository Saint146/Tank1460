using System;

namespace Tank1460.LevelObjects;

/// <summary>
/// Свойства снаряда.
/// </summary>
[Flags]
public enum ShellProperties
{
    /// <summary>
    /// Обычный снаряд.
    /// </summary>
    Normal = 0,

    /// <summary>
    /// Бронебойный снаряд.
    /// </summary>
    ArmorPiercing = 1 << 0,

    /// <summary>
    /// Косящий кусты снаряд.
    /// </summary>
    Pruning = 1 << 1
}