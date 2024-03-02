using Microsoft.Xna.Framework;
using Tank1460.Common.Extensions;

namespace Tank1460.AI.Algo;

internal readonly struct APathNode
{
    public APathNode(Point position, Point target, double traverseDistance)
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