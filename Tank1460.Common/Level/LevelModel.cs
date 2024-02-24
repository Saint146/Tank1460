using Tank1460.Common.Level.Object;
using Tank1460.Common.Level.Object.Tank;
using Tank1460.Common.Level.Object.Tile;

namespace Tank1460.Common.Level;

public class LevelModel
{
    public string ShortName { get; set; }

    public string FullPath { get; set; }

    public TileType[,] Tiles { get; set; }

    public (TankType, int)[] BotTypes { get; set; }

    public LevelObjectModel[] Objects { get; set; }
}