using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Topology.Fixes;
using IRI.Maptor.Core.Topology.RuleTypes;

namespace IRI.Maptor.Core.Topology.FixTypes;

/// <summary>
/// Fix for R20 "overshoot": the line is cut back to where it crosses the other line, so it ends on it. Message
/// arguments: the line, the length cut off, the other line.
/// </summary>
public sealed class TrimOvershootFix : ITopologyFixType
{
    public const string TypeId = "trimOvershoot";

    public const string Trim = "trim";

    private static readonly IReadOnlyCollection<string> FixedKinds = new[] { LineEndsConnectRuleType.Overshoot };

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
        var end = FixGeometry.FirstPoint(context.Finding.Location);

        if (features.Count < 2 || end is null || features[0].Feature.TheGeometry is null || features[1].Feature.TheGeometry is null)
            return TopologyFixOffer.NotOffered(Id, FixReasons.FeatureMissing);

        var (lineRef, line) = features[0];
        var (otherRef, other) = features[1];

        var frame = LocalMetricFrame.At(end.X, end.Y, context.IsGeodetic);

        var leaves = FixGeometry.Leaves(line.TheGeometry);
        var nearestEnd = FixGeometry.NearestEnd(leaves, frame);

        if (nearestEnd is null)
            return TopologyFixOffer.NotOffered(Id, FixReasons.NotFound);

        var (leaf, isStart, _) = nearestEnd.Value;
        var points = leaves[leaf].Points!;
        var n = points.Count;

        int At(int j) => isStart ? j : n - 1 - j;

        var otherSegments = FixGeometry.Leaves(other.TheGeometry)
            .SelectMany(l => Enumerable.Range(0, Math.Max(0, l.Points!.Count - 1)).Select(i => (FixGeometry.ToMetres(frame, l.Points[i]), FixGeometry.ToMetres(frame, l.Points[i + 1]))))
            .ToList();

        var samePlace = context.RuleSet.SamePlace;
        var limit = LineEndsConnectRuleType.MaxOvershoot(context.Rule) + LineEndsConnectRuleType.Distance(context.Rule);

        var walked = 0.0;

        for (int k = 0; k + 1 < n; k++)
        {
            var from = FixGeometry.ToMetres(frame, points[At(k)]);
            var to = FixGeometry.ToMetres(frame, points[At(k + 1)]);
            var length = Math.Sqrt((to.X - from.X) * (to.X - from.X) + (to.Y - from.Y) * (to.Y - from.Y));

            double? best = null;

            foreach (var (q0, q1) in otherSegments)
            {
                if (FixGeometry.Intersect(from, to, q0, q1) is { } hit && walked + hit.T * length > samePlace && (best is null || hit.T < best))
                    best = hit.T;
            }

            if (best is double t)
            {
                var cut = walked + t * length;

                if (cut > limit)
                    break;

                var crossing = FixGeometry.FromMetres(frame, from.X + t * (to.X - from.X), from.Y + t * (to.Y - from.Y));

                var after = line.TheGeometry.Clone();
                var afterPoints = FixGeometry.Leaves(after)[leaf].Points!;

                var kept = Enumerable.Range(k + 1, n - k - 1).Select(j => afterPoints[At(j)]).ToList();

                if (!isStart)
                    kept.Reverse();

                afterPoints.Clear();

                if (isStart)
                {
                    afterPoints.Add(crossing);
                    afterPoints.AddRange(kept);
                }
                else
                {
                    afterPoints.AddRange(kept);
                    afterPoints.Add(crossing);
                }

                var removed = Enumerable.Range(0, k + 1).Select(j => points[At(j)]).Append(crossing);

                var edit = new FeatureEdit(lineRef.EntityName, lineRef.FeatureId, line.TheGeometry, after);

                return TopologyFixOffer.Offered(new TopologyFixProposal(Id, Trim, context.Finding, new[] { edit }, FixGeometry.Box(removed), new object[] { lineRef, cut, otherRef }));
            }

            walked += length;

            if (walked > limit)
                break;
        }

        return TopologyFixOffer.NotOffered(Id, FixReasons.NotFound);
    }
}
