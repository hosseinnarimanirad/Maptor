using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Topology.Fixes;
using IRI.Maptor.Core.Topology.RuleTypes;

namespace IRI.Maptor.Core.Topology.FixTypes;

/// <summary>
/// Fix for R20 "undershoot" (D64): the end goes straight on, in the direction of its last segment, until it meets the
/// other line within the search distance; when that direction misses, a new end segment joins it to the nearest place
/// on the other line. Message arguments: the line, the length added, the other line.
/// </summary>
public sealed class ExtendUndershootFix : ITopologyFixType
{
    public const string TypeId = "extendUndershoot";

    public const string ExtendAlong = "extendAlong";
    public const string JoinNearest = "joinNearest";

    private static readonly IReadOnlyCollection<string> FixedKinds = new[] { LineEndsConnectRuleType.Undershoot };

    public string Id => TypeId;

    public string RuleTypeId => LineEndsConnectRuleType.TypeId;

    public IReadOnlyCollection<string> Kinds => FixedKinds;

    public IReadOnlyList<TopologyFixOffer> Propose(TopologyFixContext context)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));

        return new[] { ProposeOne(context) };
    }

    private TopologyFixOffer ProposeOne(TopologyFixContext context)
    {
        var features = context.Features;
        var location = FixGeometry.FirstPoint(context.Finding.Location);

        if (features.Count < 2 || location is null || features[0].Feature.TheGeometry is null || features[1].Feature.TheGeometry is null)
            return TopologyFixOffer.NotOffered(Id, FixReasons.FeatureMissing);

        var (lineRef, line) = features[0];
        var (otherRef, other) = features[1];

        var leaves = FixGeometry.Leaves(line.TheGeometry);
        var nearestEnd = FixGeometry.NearestEnd(leaves, LocalMetricFrame.At(location.X, location.Y, context.IsGeodetic));

        if (nearestEnd is null)
            return TopologyFixOffer.NotOffered(Id, FixReasons.NotFound);

        var (leaf, isStart, _) = nearestEnd.Value;
        var points = leaves[leaf].Points!;
        var endIndex = isStart ? 0 : points.Count - 1;
        var end = points[endIndex];
        var previous = points[isStart ? 1 : points.Count - 2];

        // Metres around the end: the end is the origin.
        var frame = LocalMetricFrame.At(end.X, end.Y, context.IsGeodetic);
        var searchDistance = LineEndsConnectRuleType.SearchDistance(context.Rule);

        var otherLeaves = FixGeometry.Leaves(other.TheGeometry);

        var after = line.TheGeometry.Clone();
        var afterPoints = FixGeometry.Leaves(after)[leaf].Points!;

        // 1. Straight on.
        var (px, py) = FixGeometry.ToMetres(frame, previous);
        var back = Math.Sqrt(px * px + py * py);

        if (back > 1e-9)
        {
            var direction = (-px / back, -py / back);

            var hit = otherLeaves
                .SelectMany(l => Enumerable.Range(0, Math.Max(0, l.Points!.Count - 1)).Select(i => FixGeometry.RayHit((0, 0), direction, FixGeometry.ToMetres(frame, l.Points[i]), FixGeometry.ToMetres(frame, l.Points[i + 1]))))
                .Where(s => s is > 1e-9)
                .Select(s => s!.Value)
                .Where(s => s <= searchDistance)
                .DefaultIfEmpty(double.NaN)
                .Min();

            if (!double.IsNaN(hit))
            {
                var reached = FixGeometry.FromMetres(frame, direction.Item1 * hit, direction.Item2 * hit);

                afterPoints[endIndex] = reached;

                var edit = new FeatureEdit(lineRef.EntityName, lineRef.FeatureId, line.TheGeometry, after);

                return TopologyFixOffer.Offered(new TopologyFixProposal(Id, ExtendAlong, context.Finding, new[] { edit }, FixGeometry.Box(new[] { end, reached }), new object[] { lineRef, hit, otherRef }));
            }
        }

        // 2. The nearest place on the other line.
        var nearest = FixGeometry.NearestSegment(otherLeaves, frame);

        if (nearest is null || nearest.Value.Distance > searchDistance || nearest.Value.Distance <= 1e-9)
            return TopologyFixOffer.NotOffered(Id, FixReasons.NotFound);

        var joined = FixGeometry.FromMetres(frame, nearest.Value.East, nearest.Value.North);

        if (isStart)
            afterPoints.Insert(0, joined);
        else
            afterPoints.Add(joined);

        var joinEdit = new FeatureEdit(lineRef.EntityName, lineRef.FeatureId, line.TheGeometry, after);

        return TopologyFixOffer.Offered(new TopologyFixProposal(Id, JoinNearest, context.Finding, new[] { joinEdit }, FixGeometry.Box(new[] { end, joined }), new object[] { lineRef, nearest.Value.Distance, otherRef }));
    }
}
