using Tank1460.LevelObjects.Tanks;

namespace Tank1460.AI;

internal interface ITankAi
{
    Tank Tank { get; }
    TankOrder Think();
}