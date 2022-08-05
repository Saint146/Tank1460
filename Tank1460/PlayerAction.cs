using System;

namespace Tank1460;

[Flags]
public enum PlayerAction
{
    MoveUp,
    MoveDown,
    MoveLeft,
    MoveRight,
    ShootOnce,
    ShootContinuously
}