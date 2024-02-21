using Tank1460.LevelObjects.Tanks;

namespace Tank1460.AI;

internal abstract class BotTankAi : ITankAi
{
    public Tank Tank => BotTank;

    protected BotTank BotTank;

    protected BotTankAi(BotTank tank)
    {
        BotTank = tank;
    }

    public abstract TankOrder Think();
}