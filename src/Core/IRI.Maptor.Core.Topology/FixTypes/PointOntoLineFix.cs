using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Topology.Findings;
using IRI.Maptor.Core.Topology.Fixes;
using IRI.Maptor.Core.Topology.RuleTypes;
using IRI.Maptor.Extensions;

namespace IRI.Maptor.Core.Topology.FixTypes;

/// <summary>
/// Fix for R10 "near a line": puts the point and its nearest line together. The lower-ranked layer moves (FR-5.4, D62):
/// <list type="bullet">
/// <item>The point ranks higher (e.g. surveyed towers, and routes drawn as the lines joining them): the line's nearest
/// vertex moves onto the point when it is within the fix distance of it, otherwise a new vertex is put at the point
/// on the nearest segment.</item>
/// <item>The line ranks higher, or the ranks are equal: the point moves to the nearest place on the line.</item>
/// </list>
/// Nothing is offered when the move would be longer than the rule's fix distance (D63). Message arguments: the point,
/// the distance moved, the line.
/// </summary>
public sealed class PointOntoLineFix : ITopologyFixType
{
    public const string TypeId = "pointOntoLine";

    public const string MovePoint = "movePoint";
    public const string MoveLineVertex = "moveLineVertex";
    public const string InsertLineVertex = "insertLineVertex";

    private static readonly IReadOnlyCollection<string> FixedKinds = new[] { PointOnLineRuleType.NearLine };

    public string Id => TypeId;

    public string RuleTypeId => PointOnLineRuleType.TypeId;

    public IReadOnlyCollection<string> Kinds => FixedKinds;

    public IReadOnlyList<TopologyFixOffer> Propose(TopologyFixContext context)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));

        return new[] { ProposeOne(context) };
    }

    private TopologyFixOffer ProposeOne(TopologyFixContext context)
    {
        var pointFeature = context.FeatureOf(FeatureRole.A);
        var lineFeature = context.FeatureOf(FeatureRole.B);

        var point = FixGeometry.FirstPoint(pointFeature?.TheGeometry);
        var lineGeometry = lineFeature?.TheGeometry;

        if (point is null || lineGeometry is null || lineGeometry.IsNullOrEmpty())
            return TopologyFixOffer.NotOffered(Id, FixReasons.FeatureMissing);

        var frame = LocalMetricFrame.At(point.X, point.Y, context.IsGeodetic);

        var leaves = FixGeometry.Leaves(lineGeometry);
        var segment = FixGeometry.NearestSegment(leaves, frame);
        var vertex = FixGeometry.NearestVertex(leaves, frame);

        if (segment is null || vertex is null)
            return TopologyFixOffer.NotOffered(Id, FixReasons.FeatureMissing);

        var fixDistance = PointOnLineRuleType.FixDistance(context.Rule);

        if (segment.Value.Distance > fixDistance)
            return TopologyFixOffer.NotOffered(Id, FixReasons.BeyondFixDistance, segment.Value.Distance, fixDistance);

        var pointRef = context.Finding.Features.First(f => f.Role == FeatureRole.A);
        var lineRef = context.Finding.Features.First(f => f.Role == FeatureRole.B);

        if (context.RankOf(context.Rule.LayerA) > context.RankOf(context.Rule.LayerB!))
        {
            var after = lineGeometry.Clone();
            var afterLeaves = FixGeometry.Leaves(after);

            string variant;
            double moved;
            List<Point> touched;

            if (vertex.Value.Distance <= fixDistance)
            {
                var points = afterLeaves[vertex.Value.Leaf].Points!;

                touched = Around(points, vertex.Value.Index);
                points[vertex.Value.Index] = new Point(point.X, point.Y);

                variant = MoveLineVertex;
                moved = vertex.Value.Distance;
            }
            else
            {
                var points = afterLeaves[segment.Value.Leaf].Points!;

                touched = new List<Point> { points[segment.Value.Index], points[segment.Value.Index + 1] };
                points.Insert(segment.Value.Index + 1, new Point(point.X, point.Y));

                variant = InsertLineVertex;
                moved = segment.Value.Distance;
            }

            touched.Add(new Point(point.X, point.Y));

            var edit = new FeatureEdit(lineRef.EntityName, lineRef.FeatureId, lineGeometry, after);

            return TopologyFixOffer.Offered(new TopologyFixProposal(Id, variant, context.Finding, new[] { edit }, FixGeometry.Box(touched), new object[] { pointRef, moved, lineRef }));
        }
        else
        {
            var target = FixGeometry.FromMetres(frame, segment.Value.East, segment.Value.North);

            var after = pointFeature!.TheGeometry.Clone();
            FixGeometry.Leaves(after).First(l => l.Points!.Count > 0).Points![0] = target;

            var edit = new FeatureEdit(pointRef.EntityName, pointRef.FeatureId, pointFeature.TheGeometry, after);

            var area = FixGeometry.Box(new[] { new Point(point.X, point.Y), target });

            return TopologyFixOffer.Offered(new TopologyFixProposal(Id, MovePoint, context.Finding, new[] { edit }, area, new object[] { pointRef, segment.Value.Distance, lineRef }));
        }
    }

    /// <summary>A vertex and its neighbours: the segments a moved vertex changes.</summary>
    private static List<Point> Around(List<Point> points, int index)
    {
        var result = new List<Point>();

        for (int i = Math.Max(0, index - 1); i <= Math.Min(points.Count - 1, index + 1); i++)
            result.Add(new Point(points[i].X, points[i].Y));

        return result;
    }
}
