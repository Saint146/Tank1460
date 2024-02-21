using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Tank1460.Common.Extensions;
using Tank1460.Common.Level.Object.Tile;
using Tank1460.Globals;
using Tank1460.LevelObjects;

namespace Tank1460;

internal class ArmoredFalconEffect : LevelEffect
{
    private double _currentEffectTime;
    private double _time;

    private readonly Queue<(bool IsArmorUp, double fullEffectTime)> _effectTimes;
    private readonly Dictionary<Falcon, Dictionary<Point, TileType>> _oldTiles;
    private bool _isArmorUp;

    private const int FlashesCount = 6;
    private static readonly double FlashesTime = GameRules.TimeInFrames(16);

    public ArmoredFalconEffect(Level level, double effectTime) : base(level)
    {
        _oldTiles = ScanOldTiles(level);
        _effectTimes = CreateEffectTimes(effectTime);

        _currentEffectTime = 0.0;
        _time = 0.0;
    }

    public override void Update(GameTime gameTime)
    {
        _time += gameTime.ElapsedGameTime.TotalSeconds;
        if (_time <= _currentEffectTime) return;

        while (_time > _currentEffectTime)
        {
            if (!_effectTimes.TryDequeue(out var newEffect))
            {
                // Когда время закончилось, всегда деактивируем эффект.
                _isArmorUp = false;
                RestoreFalconsSurroundings();
                Remove();
                return;
            }

            _time -= _currentEffectTime;

            _isArmorUp = newEffect.IsArmorUp;
            _currentEffectTime = newEffect.fullEffectTime;
        }

        if (_isArmorUp)
            ArmorFalcons();
        else
            RestoreFalconsSurroundings();
    }

    public override void Draw(SpriteBatch spriteBatch, Rectangle levelBounds)
    {
    }

    private void ArmorFalcons()
    {
        foreach (var (_, falconTiles) in _oldTiles)
        {
            foreach (var (point, _) in falconTiles)
            {
                Level.TryRemoveTileAt(point.X, point.Y);
                Level.AddTile(TileFactory.CreateTile(Level, TileType.Concrete), point.X, point.Y);
            }
        }
    }

    private void RestoreFalconsSurroundings()
    {
        foreach (var (_, falconTiles) in _oldTiles)
        {
            foreach (var (point, tileType) in falconTiles)
            {
                Level.TryRemoveTileAt(point.X, point.Y);
                Level.AddTile(TileFactory.CreateTile(Level, tileType), point.X, point.Y);
            }
        }
    }

    /// <summary>
    /// Заполнить длительности эффектов.
    /// </summary>
    private static Queue<(bool IsArmorUp, double effectTime)> CreateEffectTimes(double fullEffectTime)
    {
        var effectTimes = new Queue<(bool IsArmorUp, double fullEffectTime)>();

        var initialEffectTime = fullEffectTime - FlashesCount * 2 * FlashesTime;

        Debug.Assert(initialEffectTime > 0);

        effectTimes.Enqueue((true, initialEffectTime));
        Enumerable.Range(1, FlashesCount).ForEach(_ =>
        {
            effectTimes.Enqueue((false, FlashesTime));
            effectTimes.Enqueue((true, FlashesTime));
        });

        return effectTimes;
    }

    /// <summary>
    /// Считать окружение соколов (не текущее, а из структуры) и составить для каждого список типов тайлов, которые останутся после завершения бонуса.
    /// </summary>
    private static Dictionary<Falcon, Dictionary<Point, TileType>> ScanOldTiles(Level level)
    {
        var oldTiles = new Dictionary<Falcon, Dictionary<Point, TileType>>();
        foreach (var falcon in level.Falcons)
        {
            var falconTiles = oldTiles[falcon] = new Dictionary<Point, TileType>();

            var falconRect = falcon.TileRectangle;
            falconRect.Inflate(1, 1);

            var points = falconRect.GetOutlinePoints().Where(level.TileBounds.Contains);

            foreach (var point in points)
            {
                var oldTile = level.Structure.Tiles[point.X, point.Y];
                // Если были кирпичи или пусто, то вернём кирпичи. Иначе пишем то, что было.
                falconTiles[point] = oldTile is TileType.Empty or TileType.Brick ? TileType.Brick : oldTile;
            }
        }

        return oldTiles;
    }
}