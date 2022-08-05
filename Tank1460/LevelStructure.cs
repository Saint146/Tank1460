using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Tank1460.Extensions;

namespace Tank1460;

public class LevelStructure
{
    public TileType[,] Tiles { get; }

    public IReadOnlyList<(TankType, int)> BotTypes { get; }

    public IReadOnlyList<int> BotBonusNumbers { get; }

    public LevelStructure(string levelPath)
    {
        using var fileStream = TitleContainer.OpenStream(levelPath);
        var document = XDocument.Load(fileStream);

        var levelElement = document.Element("level") ?? throw new Exception("Cannot find root element 'level'.");

        var tilesAsString = levelElement.Element("tiles")?.Value ?? throw new Exception("Cannot find element 'level/tiles'.");
        Tiles = DeserializeTiles(tilesAsString);
        BotTypes = DeserializeBotTypes(levelElement.Element("botTypes")?.Value);
        BotBonusNumbers = DeserializeBotBonusNumbers(levelElement.Element("botBonusNumbers")?.Value);
    }

    private static TileType[,] DeserializeTiles(string tilesAsString)
    {
        var lines = tilesAsString.SplitIntoLines(StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0)
            throw new Exception("Tiles cannot be empty.");

        var width = lines[0].Length;
        var tiles = new TileType[width, lines.Length];

        for (var y = 0; y < lines.Length; y++)
        {
            var line = lines[y];

            if (line.Length != width)
                throw new Exception(
                    $"The length of line {y} ({line.Length}) is different from all preceeding lines ({width}).");

            for (var x = 0; x < width; x++)
            {
                try
                {
                    tiles[x, y] = TileTypeFromChar(line[x]);
                }
                catch (NotSupportedException)
                {
                    throw new NotSupportedException(
                        $"Unsupported tile type character '{line[x]}' at position {x}, {y}.");
                }
            }
        }

        return tiles;
    }

    private static IReadOnlyList<(TankType, int)> DeserializeBotTypes(string botTypesAsString)
    {
        if (botTypesAsString is null)
            return null;

        var botTypesStrings = botTypesAsString.Split(',', StringSplitOptions.TrimEntries);
        if (botTypesStrings.Length == 0)
            return null;

        var result = new List<(TankType, int)>();
        foreach (var botTypeString in botTypesStrings)
        {
            var split = botTypeString.Split('*', StringSplitOptions.TrimEntries);
            if (split.Length != 2 || !int.TryParse(split[0], out var botType) || !int.TryParse(split[1], out var botCount))
                throw new Exception($"Invalid format for bot type entry '{botTypeString}'. It should look like '3*15'.");

            result.Add(((TankType)botType, botCount));
        }

        return result;
    }

    private static IReadOnlyList<int> DeserializeBotBonusNumbers(string botBonusNumbersAsString)
    {
        if (botBonusNumbersAsString is null)
            return null;

        var split = botBonusNumbersAsString.Split(',', StringSplitOptions.TrimEntries);
        if (split.Length == 0)
            return null;

        var result = new List<int>();
        foreach (var botNumberString in split)
        {
            if (!int.TryParse(botNumberString, out var botNumber))
                throw new Exception($"Cannot parse bot number '{botNumberString}'. It should be an integer.");

            result.Add(botNumber);
        }

        return result;
    }

    private static TileType TileTypeFromChar(char tileType) =>
        tileType switch
        {
            // Blank space
            '.' => TileType.Empty,

            // Brick
            'X' => TileType.Brick,

            // Concrete
            'Q' => TileType.Concrete,

            // Water
            '~' => TileType.Water,

            // Forest
            '#' => TileType.Forest,

            // Ice
            '/' => TileType.Ice,

            // Player 1 start point
            '1' => TileType.Player1Spawn,

            // Player 2 start point
            '2' => TileType.Player2Spawn,

            // Bot spawn point
            'R' => TileType.BotSpawn,

            // Falcon
            's' => TileType.Falcon,

            // Unknown tile type character
            _ => throw new NotSupportedException()
        };
}