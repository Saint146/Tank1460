using System.Collections.Generic;
using Tank1460.Common.Level.Object.Tank;

namespace Tank1460.Globals;

internal static class GameRules
{
#if DEBUG
    public static bool ShowObjectsBoundaries { get; set; }
    public static bool ShowBotsPeriods { get; set; } = true;
    public static bool ShowAiPaths { get; set; }
    public static bool ShowObstructedTiles { get; set; }
#endif

    public static double TimeInFrames(int frameCount) => frameCount * OneFrameSpan;

    public static readonly Dictionary<TankType, int> TankScoreByType = new()
    {
        { TankType.P0, 500 },
        { TankType.P1, 500 },
        { TankType.P2, 500 },
        { TankType.P3, 500 },

        { TankType.B0, 100 },
        { TankType.B1, 200 },
        { TankType.B2, 300 },
        { TankType.B3, 400 },
        { TankType.B9, 500 }
    };

    public static bool AiEnabled { get; set; }

    public static bool AiHasInfiniteLives { get; set; }

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