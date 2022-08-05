using System;

namespace Tank1460.LevelObjects;

[Flags]
public enum CollisionType
{
    None = 0,
    Shootable = 1,
    Impassable = 2,
    ShootableAndImpassable = Shootable | Impassable
}