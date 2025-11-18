using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Tank1460.Common.Extensions;
using Tank1460.LevelObjects;
using Tank1460.LevelObjects.Tanks;

namespace Tank1460.Managers
{
    /// <summary>
    /// Handles collision detection and spatial queries for level objects.
    /// Extracted from the god object Level class to manage collisions separately.
    /// </summary>
    internal class LevelCollisionDetector
    {
        private readonly List<LevelObject>[,] _tileObjectMap;
        private readonly Rectangle _tileBounds;

        public LevelCollisionDetector(List<LevelObject>[,] tileObjectMap, Rectangle tileBounds)
        {
            _tileObjectMap = tileObjectMap;
            _tileBounds = tileBounds;
        }

        public ICollection<LevelObject> GetLevelObjectsInTile(int x, int y)
        {
            return _tileObjectMap[x, y];
        }

        public HashSet<LevelObject> GetLevelObjectsInTiles(Rectangle tileRectangle)
        {
            var objects = new HashSet<LevelObject>();
            tileRectangle.EnumerateArray(_tileObjectMap, objects.UnionWith);
            return objects;
        }

        public void HandleChangeTileBounds(LevelObject levelObject, Rectangle oldTileBounds, Rectangle? newTileBounds)
        {
            oldTileBounds.EnumerateArray(_tileObjectMap, tileLevelObjects => { tileLevelObjects.Remove(levelObject); });
            newTileBounds?.EnumerateArray(_tileObjectMap, tileLevelObjects => { tileLevelObjects.Add(levelObject); });
        }

        public bool CanTankPassThroughTile(Tank tank, Point tilePoint)
        {
            var tileObjects = _tileObjectMap.ElementAtOrDefault(tilePoint.X, tilePoint.Y);
            return tileObjects?.All(o => !o.CollisionType.HasFlag(CollisionType.Impassable) && (!o.CollisionType.HasFlag(CollisionType.PassableOnlyByShip) || tank.HasShip)) == true;
        }

        public ICollection<LevelObject> GetAllCollisionsSimple(LevelObject levelObject, IReadOnlyCollection<LevelObject> excludedObjects = null)
        {
            if (DetectOutOfBoundsCollisionSimple(levelObject.TileRectangle))
                return new LevelObject[] { null };

            var closeObjects = GetLevelObjectsInTiles(levelObject.TileRectangle);
            closeObjects.Remove(levelObject);
            if (excludedObjects is not null)
                closeObjects.RemoveWhere(excludedObjects.Contains);

            closeObjects.RemoveWhere(closeObject =>
                levelObject.BoundingRectangle.GetIntersectionDepth(closeObject.BoundingRectangle) == Vector2.Zero);

            return closeObjects;
        }

        private bool DetectOutOfBoundsCollisionSimple(Rectangle tileRect)
        {
            if (tileRect.Top < _tileBounds.Top)
                return true;

            if (tileRect.Bottom > _tileBounds.Bottom)
                return true;

            if (tileRect.Left < _tileBounds.Left)
                return true;

            if (tileRect.Right > _tileBounds.Right)
                return true;

            return false;
        }
    }
}