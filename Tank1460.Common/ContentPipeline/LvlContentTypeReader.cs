using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Tank1460.Common.Extensions;
using Tank1460.Common.Level;
using Tank1460.Common.Level.Object;
using Tank1460.Common.Level.Object.Tank;
using Tank1460.Common.Level.Object.Tile;

namespace Tank1460.Common.ContentPipeline;

internal class LvlContentTypeReader : ContentTypeReader<LevelModel>
{
    protected override LevelModel Read(ContentReader input, LevelModel existingInstance)
    {
        var xml = input.ReadString();
        var document = XDocument.Parse(xml);

        var levelElement = document.Element("level") ?? throw new Exception("Cannot find root element 'level'.");

        var infoElement = levelElement.Element("info") ?? throw new Exception("Cannot find element 'level/info'.");
        var shortName = infoElement.Attribute("shortName")?.Value ?? throw new Exception("Cannot find attribute 'shortName' of the 'level/info' element.");
        var fullPath = infoElement.Attribute("fullPath")?.Value ?? throw new Exception("Cannot find attribute 'fullPath' of the 'level/info' element.");
        
        var tilesAsString = levelElement.Element("tiles")?.Value ?? throw new Exception("Cannot find element 'level/tiles'.");

        var objectsElement = levelElement.Element("objects");

        return new LevelModel
        {
            ShortName = shortName,
            FullPath = fullPath,
            Tiles = DeserializeTiles(tilesAsString),
            Objects = DeserializeObjects(objectsElement).ToArray(),
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

    private IEnumerable<LevelObjectModel> DeserializeObjects(XElement objectsElement)
    {
        if (objectsElement is null)
            yield break;

        // TODO: Это точно можно сделать гораздо проще, но сейчас модели супер-простые, поэтому так.
        foreach (var node in objectsElement.Nodes())
        {
            var element = (XElement)node;
            var position = new Point(int.Parse(element.Attribute("x")!.Value), int.Parse(element.Attribute("y")!.Value));
            var size = new Point(int.Parse(element.Attribute("w")!.Value), int.Parse(element.Attribute("h")!.Value));

            yield return element.Name.LocalName.ToLower() switch
            {
                "falcon" => new FalconModel
                {
                    Position = position,
                    Size = size
                },

                "botspawner" => new BotSpawnerModel
                {
                    Position = position,
                    Size = size
                },

                "playerspawner" => new PlayerSpawnerModel((PlayerIndex)(int.Parse(element.Attribute("player")!.Value) - 1))
                {
                    Position = position,
                    Size = size
                },

                _ => throw new NotSupportedException($"Unsupported element type '{element.Name}'.")
            };
        }
    }

    private static (TankType, int)[] DeserializeBotTypes(string botTypesAsString)
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
            if (split.Length != 2 || !Enum.TryParse<TankType>(split[0], out var botType) || !int.TryParse(split[1], out var botCount))
                throw new Exception($"Invalid format for bot type entry '{botTypeString}'. It should look like 'B2*15'.");

            result.Add((botType, botCount));
        }

        return result.ToArray();
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

        // TODO: Убрать после того, как приберусь в файлах левелов.
        '1' or '2' or 'R' or 's' => TileType.Empty,

        _ => throw new ArgumentOutOfRangeException(nameof(tileType), tileType, null)
    };
}