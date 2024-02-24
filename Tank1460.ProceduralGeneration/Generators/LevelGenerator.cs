using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Tank1460.Common.Extensions;
using Tank1460.Common.Level;
using Tank1460.Common.Level.Object;
using Tank1460.Common.Level.Object.Tile;
using Tank1460.ProceduralGeneration.Options;

namespace Tank1460.ProceduralGeneration.Generators;

public class LevelGenerator
{
    private readonly LevelGenerationOptions _options;

    private TileType[,] _tiles;
    private List<LevelObjectModel> _objects;
    public LevelGenerator(LevelGenerationOptions options)
    {
        _options = options;
    }

    public LevelModel GenerateLevel()
    {
        _tiles = new TileType[_options.Size.X, _options.Size.Y];
        _objects = new List<LevelObjectModel>();

        var playersY = 0;
        var botsY = _options.Size.Y - BotSpawnerModel.DefaultSize.Y;
        var falconX = _options.Size.X / 2 - FalconModel.DefaultSize.X / 2;

        CreateFalconWithBricks(falconX, playersY);
        CreatePlayerSpawners(falconX, playersY);
        CreateBotSpawners(botsY);

        return new LevelModel
        {
            ShortName = "-1",
            FullPath = string.Empty,
            Tiles = _tiles,
            Objects = _objects.ToArray()
        };
    }

    private void CreateFalconWithBricks(int x, int y)
    {
        var falcon = new FalconModel
        {
            Position = new Point(x, y),
            Size = FalconModel.DefaultSize
        };
        _objects.Add(falcon);

        // Окружаем кирпичами со всех сторон.
        var bricksRect = falcon.Bounds;
        bricksRect.Inflate(1, 1);
        foreach (var point in bricksRect.GetOutlinePoints())
        {
            if (!_tiles.ContainsCoords(point.X, point.Y))
                continue;

            _tiles[point.X, point.Y] = TileType.Brick;
        }
    }

    private void CreatePlayerSpawners(int falconX, int playersY)
    {
        _objects.Add(new PlayerSpawnerModel(PlayerIndex.One)
        {
            Position = new Point(falconX - PlayerSpawnerModel.DefaultSize.X - 2, playersY),
            Size = PlayerSpawnerModel.DefaultSize
        });

        _objects.Add(new PlayerSpawnerModel(PlayerIndex.Three)
        {
            Position = new Point(falconX - PlayerSpawnerModel.DefaultSize.X - 2 - PlayerSpawnerModel.DefaultSize.X - 1, playersY),
            Size = PlayerSpawnerModel.DefaultSize
        });

        _objects.Add(new PlayerSpawnerModel(PlayerIndex.Two)
        {
            Position = new Point(falconX + FalconModel.DefaultSize.X + 2, playersY),
            Size = PlayerSpawnerModel.DefaultSize
        });

        _objects.Add(new PlayerSpawnerModel(PlayerIndex.Four)
        {
            Position = new Point(falconX + FalconModel.DefaultSize.X + 2 + PlayerSpawnerModel.DefaultSize.X + 1, playersY),
            Size = PlayerSpawnerModel.DefaultSize
        });
    }

    private void CreateBotSpawners(int botsY)
    {
        _objects.Add(new BotSpawnerModel
        {
            Position = new Point(0, botsY),
            Size = BotSpawnerModel.DefaultSize
        });

        _objects.Add(new BotSpawnerModel
        {
            Position = new Point(_options.Size.X / 2 - BotSpawnerModel.DefaultSize.X / 2, botsY),
            Size = BotSpawnerModel.DefaultSize
        });

        _objects.Add(new BotSpawnerModel
        {
            Position = new Point(_options.Size.X - BotSpawnerModel.DefaultSize.X, botsY),
            Size = BotSpawnerModel.DefaultSize
        });
    }
}