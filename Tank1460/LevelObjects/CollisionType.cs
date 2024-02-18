using System;

namespace Tank1460.LevelObjects;

[Flags]
public enum CollisionType
{
    None = 0,
    Shootable = 1 << 0,
    Impassable = 1 << 1,
    PassableOnlyByShip = 1 << 2,

    ShootableAndImpassable = Shootable | Impassable
}