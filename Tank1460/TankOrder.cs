using System;

namespace Tank1460;

[Flags]
public enum TankOrder
{
    None = 0,

    MoveUp = 1 << 0,
    MoveDown = 1 << 1,
    MoveLeft = 1 << 2,
    MoveRight = 1 << 3,
    Shoot = 1 << 4
}