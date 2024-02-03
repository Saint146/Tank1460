using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Microsoft.Xna.Framework.Content;
using Tank1460.Common.Extensions;
using Tank1460.Common.Level;
using Tank1460.Common.Level.Object.Tank;
using Tank1460.Common.Level.Object.Tile;

namespace Tank1460.Common.ContentPipeline;

internal class LvlContentTypeReader : ContentTypeReader<LevelStructure>
{
    protected override LevelStructure Read(ContentReader input, LevelStructure existingInstance)
    {
        var xml = input.ReadString();
        var document = XDocument.Parse(xml);

        var levelElement = document.Element("level") ?? throw new Exception("Cannot find root element 'level'.");

        var tilesAsString = levelElement.Element("tiles")?.Value ?? throw new Exception("Cannot find element 'level/tiles'.");
        return new LevelStructure
        {
            Tiles = DeserializeTiles(tilesAsString),
            BotTypes = DeserializeBotTypes(levelElement.Element("botTypes")?.Value)
        };
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

    private static TileType TileTypeFromChar(char tileType) => tileType switch
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