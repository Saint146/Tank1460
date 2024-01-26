using System;

namespace Tank1460.LevelObjects;

[Flags]
public enum CollisionType
{
    None = 0,
    Shootable = 1,
    Impassable = 2,
    PassablyOnlyByShip = 4,
    ShootableAndImpassable = Shootable | Impassable
}