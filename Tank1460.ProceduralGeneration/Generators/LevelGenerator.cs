using Tank1460.Common.Level;
using Tank1460.Common.Level.Object.Tile;
using Tank1460.ProceduralGeneration.Options;

namespace Tank1460.ProceduralGeneration.Generators;

public class LevelGenerator
{
    private readonly LevelGenerationOptions _options;

    public LevelGenerator(LevelGenerationOptions options)
    {
        _options = options;
    }

    public LevelStructure GenerateLevel()
    {
        var tiles = new TileType[_options.Size.X, _options.Size.Y];

        tiles[8, 24] = TileType.Player1Spawn;
        tiles[16, 24] = TileType.Player2Spawn;
        tiles[5, 24] = TileType.Player3Spawn;
        tiles[19, 24] = TileType.Player4Spawn;

        tiles[12, 24] = TileType.Falcon;

        tiles[0, 0] = TileType.BotSpawn;
        tiles[12, 0] = TileType.BotSpawn;
        tiles[24, 0] = TileType.BotSpawn;

        tiles[12, 12] = TileType.Concrete;
        tiles[13, 12] = TileType.Concrete;
        
        return new LevelStructure
        {
            Tiles = tiles
        };
    }
}