using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Tank1460.Common.Extensions;

namespace Tank1460.AI.Algo;

/// <summary>
/// Reusable A* path finder.
/// </summary>
public class APath
{
    private const int MaxNeighbours = 4;
    private readonly PathNode[] _neighbours = new PathNode[MaxNeighbours];

    private readonly int _maxSteps;
    private readonly IBinaryHeap<Point, PathNode> _frontier;
    private readonly HashSet<Point> _ignoredPositions;
    private readonly List<Point> _output;
    private readonly IDictionary<Point, Point> _links;

    /// <summary>
    /// Creation of new path finder.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public APath(int maxSteps = int.MaxValue, int initialCapacity = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxSteps);
        ArgumentOutOfRangeException.ThrowIfNegative(initialCapacity);

        _maxSteps = maxSteps;
        var comparer = Comparer<PathNode>.Create((a, b) => b.EstimatedTotalCost.CompareTo(a.EstimatedTotalCost));
        _frontier = new BinaryHeap<Point, PathNode>(comparer, a => a.Position, initialCapacity);
        _ignoredPositions = new HashSet<Point>(initialCapacity);
        _output = new List<Point>(initialCapacity);
        _links = new Dictionary<Point, Point>(initialCapacity);
    }

    /// <summary>
    /// Calculate a new path between two points.
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    public bool Calculate(Point start, Point target, 
                          IReadOnlyCollection<Point> obstacles, 
                          out IReadOnlyCollection<Point> path)
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

        _frontier.Enqueue(new PathNode(start, target, 0));
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

    private void GenerateFrontierNodes(PathNode parent, Point target)
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
    private static readonly (Point position, double cost)[] NeighboursTemplate =
    {
        (new Point(1, 0), 1),
        (new Point(0, 1), 1),
        (new Point(-1, 0), 1),
        (new Point(0, -1), 1)
    };
        
    public static void Fill(this PathNode[] buffer, PathNode parent, Point target)
    {
        var i = 0;
        foreach (var (position, cost) in NeighboursTemplate)
        {
            var nodePosition = position + parent.Position;
            var traverseDistance = parent.TraverseDistance + cost;
            buffer[i++] = new PathNode(nodePosition, target, traverseDistance);
        }
    }
}

internal readonly struct PathNode
{
    public PathNode(Point position, Point target, double traverseDistance)
    {
        Position = position;
        TraverseDistance = traverseDistance;
        var heuristicDistance = (position - target).DistanceEstimate();
        EstimatedTotalCost = traverseDistance + heuristicDistance;
    }

    public Point Position { get; }
    public double TraverseDistance { get; }
    public double EstimatedTotalCost { get; }
}

internal interface IBinaryHeap<in TKey, T>
{
    void Enqueue(T item);
    T Dequeue();
    void Clear();
    bool TryGet(TKey key, out T value);
    void Modify(T value);
    int Count { get; }
}

internal class BinaryHeap<TKey, T> : IBinaryHeap<TKey, T> where TKey : IEquatable<TKey>
{
    private readonly IDictionary<TKey, int> _map;
    private readonly IList<T> _collection;
    private readonly IComparer<T> _comparer;
    private readonly Func<T, TKey> _lookupFunc;
        
    public BinaryHeap(IComparer<T> comparer, Func<T, TKey> lookupFunc, int capacity)
    {
        _comparer = comparer;
        _lookupFunc = lookupFunc;
        _collection = new List<T>(capacity);
        _map = new Dictionary<TKey, int>(capacity);
    }

    public int Count => _collection.Count;

    public void Enqueue(T item)
    {
        _collection.Add(item);
        var i = _collection.Count - 1;
        _map[_lookupFunc(item)] = i;
        while(i > 0)
        {
            var j = (i - 1) / 2;
                
            if (_comparer.Compare(_collection[i], _collection[j]) <= 0)
                break;

            Swap(i, j);
            i = j;
        }
    }

    public T Dequeue()
    {
        if (_collection.Count == 0) return default;
            
        var result = _collection.First();
        RemoveRoot();
        _map.Remove(_lookupFunc(result));
        return result;
    }

    public void Clear()
    {
        _collection.Clear();
        _map.Clear();
    }

    public bool TryGet(TKey key, out T value)
    {
        if (!_map.TryGetValue(key, out var index))
        {
            value = default;
            return false;
        }
            
        value = _collection[index];
        return true;
    }

    public void Modify(T value)
    {
        if (!_map.TryGetValue(_lookupFunc(value), out var index))
            throw new KeyNotFoundException(nameof(value));
            
        _collection[index] = value;
    }
        
    private void RemoveRoot()
    {
        _collection[0] = _collection.Last();
        _map[_lookupFunc(_collection[0])] = 0;
        _collection.RemoveAt(_collection.Count - 1);

        var i = 0;
        while(true)
        {
            var largest = LargestIndex(i);
            if (largest == i)
                return;

            Swap(i, largest);
            i = largest;
        }
    }

    private void Swap(int i, int j)
    {
        (_collection[i], _collection[j]) = (_collection[j], _collection[i]);
        _map[_lookupFunc(_collection[i])] = i;
        _map[_lookupFunc(_collection[j])] = j;
    }

    private int LargestIndex(int i)
    {
        var leftInd = 2 * i + 1;
        var rightInd = 2 * i + 2;
        var largest = i;

        if (leftInd < _collection.Count && _comparer.Compare(_collection[leftInd], _collection[largest]) > 0) largest = leftInd;

        if (rightInd < _collection.Count && _comparer.Compare(_collection[rightInd], _collection[largest]) > 0) largest = rightInd;
            
        return largest;
    }
}