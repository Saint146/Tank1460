using System.Collections.Generic;
using Tank1460.Common.Level.Object.Tank;

namespace Tank1460;

internal static class GameRules
{
#if DEBUG
    public static bool ShowObjectsBoundaries { get; set; }
    public static bool ShowBotsPeriods { get; set; } = true;
#endif

    public static double TimeInFrames(int frameCount) => frameCount * OneFrameSpan;

    public static readonly Dictionary<TankType, int> TankScoreByType = new()
    {
        { TankType.TypeP0, 500 },
        { TankType.TypeP1, 500 },
        { TankType.TypeP2, 500 },
        { TankType.TypeP3, 500 },
        { TankType.TypeB0, 100 },
        { TankType.TypeB1, 200 },
        { TankType.TypeB2, 300 },
        { TankType.TypeB3, 400 }
    };

    private const int Fps = 60;

    /// <summary>
    /// Длительность одного кадра в секундах.
    /// </summary>
    private const double OneFrameSpan = 1.0d / Fps;
    
    public static int GetOneUpsGained(int oldScore, int scoreGained)
    {
        // TODO: Проверить логику оригинала.
        const int pointsForOneUp = 20000;

        return (oldScore + scoreGained) / pointsForOneUp - oldScore / pointsForOneUp;
    }
}