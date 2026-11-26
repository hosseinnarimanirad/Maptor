using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Topology.Findings;
using IRI.Maptor.Core.Topology.Fixes;
using IRI.Maptor.Core.Topology.RuleTypes;

namespace IRI.Maptor.Core.Topology.FixTypes;

/// <summary>
/// Fix for R20 "unsnapped end": the line ends of the group meet at one place (doc 02 §2.4, point 5). They move to the
/// end of the highest-ranked layer; among equal ranks, to the group's first end (the finding's location). A chain gets
/// no fix. Message arguments: the lines, the longest move.
/// </summary>
public sealed class SnapLineEndsFix : ITopologyFixType
{
    public const string TypeId = "snapLineEnds";

    public const string Snap = "snap";

    private static readonly IReadOnlyCollection<string> FixedKinds = new[] { LineEndsConnectRuleType.UnsnappedEnd };

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
        var anchor = FixGeometry.FirstPoint(context.Finding.Location);

        if (anchor is null)
            return TopologyFixOffer.NotOffered(Id, FixReasons.NotFound);

        var frame = LocalMetricFrame.At(anchor.X, anchor.Y, context.IsGeodetic);

        // A group that is not a chain is at most twice the rule distance wide.
        var reach = 2 * LineEndsConnectRuleType.Distance(context.Rule) + 1e-6;

        var members = new List<(FindingFeature Reference, Feature<Point> Feature, int Leaf, bool IsStart, Point Point, double Distance)>();

        foreach (var (reference, feature) in context.Features)
        {
            if (feature.TheGeometry is null)
                continue;

            var leaves = FixGeometry.Leaves(feature.TheGeometry);

            for (int l = 0; l < leaves.Count; l++)
            {
                var points = leaves[l].Points!;

                if (points.Count < 2)
                    continue;

                var startDistance = FixGeometry.Metres(frame, points[0]);
                var endDistance = FixGeometry.Metres(frame, points[points.Count - 1]);

                var isStart = startDistance <= endDistance;
                var distance = Math.Min(startDistance, endDistance);

                if (distance <= reach)
                    members.Add((reference, feature, l, isStart, isStart ? points[0] : points[points.Count - 1], distance));
            }
        }

        if (members.Count < 2 || members.Select(m => (m.Reference.EntityName, m.Reference.FeatureId)).Distinct().Count() < 2)
            return TopologyFixOffer.NotOffered(Id, FixReasons.NotFound);

        var target = Target(context, members.Select(m => (m.Reference, m.Point, m.Distance)).ToList(), anchor);

        var edits = new List<FeatureEdit>();
        var touched = new List<Point> { target };
        var longest = 0.0;

        foreach (var group in members.GroupBy(m => (m.Reference.EntityName, m.Reference.FeatureId)))
        {
            var moving = group.Where(m => FixGeometry.Metres(LocalMetricFrame.At(target.X, target.Y, context.IsGeodetic), m.Point) > 1e-9).ToList();

            if (moving.Count == 0)
                continue;

            var first = moving[0];
            var after = first.Feature.TheGeometry.Clone();
            var leaves = FixGeometry.Leaves(after);

            foreach (var m in moving)
            {
                var points = leaves[m.Leaf].Points!;
                var index = m.IsStart ? 0 : points.Count - 1;
                var neighbour = m.IsStart ? 1 : points.Count - 2;

                touched.Add(new Point(points[index].X, points[index].Y));
                touched.Add(new Point(points[neighbour].X, points[neighbour].Y));

                longest = Math.Max(longest, FixGeometry.Metres(LocalMetricFrame.At(target.X, target.Y, context.IsGeodetic), points[index]));

                points[index] = new Point(target.X, target.Y);
            }

            edits.Add(new FeatureEdit(first.Reference.EntityName, first.Reference.FeatureId, first.Feature.TheGeometry, after));
        }

        if (edits.Count == 0)
            return TopologyFixOffer.NotOffered(Id, FixReasons.NotFound);

        var lines = members.Select(m => m.Reference).Distinct().ToList();

        return TopologyFixOffer.Offered(new TopologyFixProposal(Id, Snap, context.Finding, edits, FixGeometry.Box(touched), new object[] { lines, longest }));
    }

    /// <summary>The end of the highest-ranked layer; the anchor when the ranks are equal or the anchor is of that rank.</summary>
    private static Point Target(TopologyFixContext context, List<(FindingFeature Reference, Point Point, double Distance)> members, Point anchor)
    {
        var ranks = members.Select(m => context.RankOf(m.Reference.EntityName)).ToList();
        var top = ranks.Max();

        if (ranks.All(r => r == top))
            return anchor;

        var best = members.Where(m => context.RankOf(m.Reference.EntityName) == top).ToList();

        if (best.Any(m => m.Distance <= 1e-9))
            return anchor;

        var chosen = best.OrderBy(m => m.Reference.EntityName, StringComparer.Ordinal).ThenBy(m => m.Reference.FeatureId).First();

        return new Point(chosen.Point.X, chosen.Point.Y);
    }
}
