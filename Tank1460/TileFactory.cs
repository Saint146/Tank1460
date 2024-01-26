using System;
using Tank1460.LevelObjects.Tiles;

namespace Tank1460;

internal static class TileFactory
{
    // TODO: Создаёт именно тайлы, поэтому не воспринимает типы спавнов или сокола.
    public static Tile CreateTile(Level level, TileType type) => type switch
    {
        TileType.Empty => null,
        TileType.Brick => new BrickTile(level),
        TileType.Concrete => new ConcreteTile(level),
        TileType.Water => new WaterTile(level),
        TileType.Forest => new ForestTile(level),
        TileType.Ice => new IceTile(level),

        // TODO
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };
}