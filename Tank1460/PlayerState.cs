using Tank1460.Common.Level.Object.Tank;

namespace Tank1460;

public class PlayerState
{
    public int LivesRemaining { get; set; }

    public TankType? TankType { get; set; }

    public int Score { get; set; }

    public bool TankHasShip { get; set; }
}