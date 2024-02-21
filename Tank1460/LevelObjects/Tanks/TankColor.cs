using System;

namespace Tank1460.LevelObjects.Tanks;

[Flags]
public enum TankColor
{
    Gray = 1 << 0,
    Yellow = 1 << 1,
    Green = 1 << 2,
    Red = 1 << 3,
    Blue = 1 << 4
}