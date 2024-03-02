using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Tank1460.AI.Algo;

/// <summary>
/// Reusable A* path finder.
/// </summary>
public class APathFinder
{
    private const int MaxNeighbours = 4;
    private readonly APathNode[] _neighbours = new APathNode[MaxNeighbours];

    private readonly int _maxSteps;
    private readonly IBinaryHeap<Point, APathNode> _frontier;
    private readonly HashSet<Point> _ignoredPositions;
    private readonly List<Point> _output;
    private readonly IDictionary<Point, Point> _links;

    /// <summary>
    /// Creation of new path finder.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public APathFinder(int maxSteps = int.MaxValue, int initialCapacity = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxSteps);
        ArgumentOutOfRangeException.ThrowIfNegative(initialCapacity);

        _maxSteps = maxSteps;
        var comparer = Comparer<APathNode>.Create((a, b) => b.EstimatedTotalCost.CompareTo(a.EstimatedTotalCost));
        _frontier = new BinaryHeap<Point, APathNode>(comparer, a => a.Position, initialCapacity);
        _ignoredPositions = new HashSet<Point>(initialCapacity);
        _output = new List<Point>(initialCapacity);
        _links = new Dictionary<Point, Point>(initialCapacity);
    }

    /// <summary>
    /// Calculate a new path between two points.
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    public bool Calculate(Point start,
                          Point target,
                          IReadOnlyCollection<Point> obstacles,
                          out IReadOnlyList<Point> path)
    {
        ArgumentNullException.ThrowIfNull(obstacles);

        if (!GenerateNodes(start, target, obstacles))
        {
            path = Array.Empty<Point>();
            return false;
        }

        _output.Clear();
        _output.Add(target);

        while (_links.TryGetValue(target, out target)) _output.Add(target);
        path = _output;
        return true;
    }

    private bool GenerateNodes(Point start, Point target, IReadOnlyCollection<Point> obstacles)
    {
        _frontier.Clear();
        _ignoredPositions.Clear();
        _links.Clear();

        _frontier.Enqueue(new APathNode(start, target, 0));
        _ignoredPositions.UnionWith(obstacles);
        var step = 0;
        while (_frontier.Count > 0 && step++ <= _maxSteps)
        {
            var current = _frontier.Dequeue();
            _ignoredPositions.Add(current.Position);

            if (current.Position.Equals(target)) return true;

            GenerateFrontierNodes(current, target);
        }

        // All nodes analyzed - no path detected.
        return false;
    }

    private void GenerateFrontierNodes(APathNode parent, Point target)
    {
        _neighbours.Fill(parent, target);
        foreach (var newNode in _neighbours)
        {
            // Position is already checked or occupied by an obstacle.
            if (_ignoredPositions.Contains(newNode.Position)) continue;

            // Node is not present in queue.
            if (!_frontier.TryGet(newNode.Position, out var existingNode))
            {
                _frontier.Enqueue(newNode);
                _links[newNode.Position] = parent.Position;
            }

            // Node is present in queue and new optimal path is detected.
            else if (newNode.TraverseDistance < existingNode.TraverseDistance)
            {
                _frontier.Modify(newNode);
                _links[newNode.Position] = parent.Position;
            }
        }
    }
}

internal static class NodeExtensions
{
    public static void Fill(this APathNode[] buffer, APathNode parent, Point target)
    {
        var i = 0;
        foreach (var (position, cost) in NeighboursTemplate)
        {
            var nodePosition = position + parent.Position;
            var traverseDistance = parent.TraverseDistance + cost;
            buffer[i++] = new APathNode(nodePosition, target, traverseDistance);
        }
    }

    private static readonly (Point position, double cost)[] NeighboursTemplate =
    {
        (new Point(1, 0), 1),
        (new Point(0, 1), 1),
        (new Point(-1, 0), 1),
        (new Point(0, -1), 1)
    };
}