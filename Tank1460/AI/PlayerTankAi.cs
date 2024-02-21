using Tank1460.LevelObjects.Tanks;

namespace Tank1460.AI;

internal abstract class PlayerTankAi : ITankAi
{
    public Tank Tank => PlayerTank;

    protected PlayerTank PlayerTank;

    protected PlayerTankAi(PlayerTank tank)
    {
        PlayerTank = tank;
    }

    public abstract TankOrder Think();
}