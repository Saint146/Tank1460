using System;
using Tank1460.LevelObjects.Tiles;

namespace Tank1460.Extensions;

public static class IntExtensions
{
    // Перевести координату X в пикселях в номер тайла.
    public static int CoordToTileX(this int coordX)
    {
        return (int)Math.Floor((double)coordX / Tile.DefaultWidth);
    }

    // Перевести координату Y в пикселях в номер тайла.
    public static int CoordToTileY(this int coordY)
    {
        return (int)Math.Floor((double)coordY / Tile.DefaultHeight);
    }

    public static int CeilingByBase(this int number, int baseNumber)
    {
        return (int)Math.Ceiling(number / (double)baseNumber) * baseNumber;
    }
}