using System.Collections.Generic;
using Tank1460.Common.Level.Object.Tank;

namespace Tank1460;

internal static class GameRules
{
    public static readonly Dictionary<TankType, int> TankScoreByType = new()
    {
        { TankType.Type0, 500 },
        { TankType.Type1, 500 },
        { TankType.Type2, 500 },
        { TankType.Type3, 500 },
        { TankType.Type4, 100 },
        { TankType.Type5, 200 },
        { TankType.Type6, 300 },
        { TankType.Type7, 400 }
    };

    public static int GetOneUpsGained(int oldScore, int scoreGained)
    {
        // TODO: Проверить логику оригинала.
        const int pointsForOneUp = 20000;

        return (oldScore + scoreGained) / pointsForOneUp - oldScore / pointsForOneUp;
    }
}