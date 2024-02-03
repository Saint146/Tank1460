using System.Collections.Generic;
using Tank1460.Common.Level.Object.Tank;
using Tank1460.Common.Level.Object.Tile;

namespace Tank1460.Common.Level;

public class LevelStructure
{
    public TileType[,] Tiles { get; set; }

    public IReadOnlyList<(TankType, int)> BotTypes { get; set; }
}