using System;

namespace Tank1460.Input;

[Flags]
public enum PlayerInputCommands
{
    None = 0,
    Up = 1 << 0,
    Down = 1 << 1,
    Left = 1 << 2,
    Right = 1 << 3,
    Shoot = 1 << 4,
    Start = 1 << 5
}