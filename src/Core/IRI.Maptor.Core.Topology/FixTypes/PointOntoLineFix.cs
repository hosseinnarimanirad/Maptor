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
/// <item>The point ranks higher (e.g. surveyed towers, and routes drawn as the lines joining them): the line's nearest vertex moves
/// onto the point when it is within the fix distance of it, otherwise a new vertex is put at the point on the nearest
/// segment.</item>
/// <item>The line ranks higher, or the ranks are equal: the point moves to the nearest place on the line.</item>
/// </list>
/// Nothing is offered when the move would be longer than the rule's fix distance (D63).
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

    public TopologyFixOffer Propose(TopologyFixContext context)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));

        var pointFeature = context.FeatureOf(FeatureRole.A);
        var lineFeature = context.FeatureOf(FeatureRole.B);

        var point = FirstPoint(pointFeature?.TheGeometry);
        var lineGeometry = lineFeature?.TheGeometry;

        if (point is null || lineGeometry is null || lineGeometry.IsNullOrEmpty())
            return TopologyFixOffer.NotOffered(Id, FixReasons.FeatureMissing);

        var frame = LocalMetricFrame.At(point.X, point.Y, context.IsGeodetic);

        var leaves = Leaves(lineGeometry);
        var segment = NearestSegment(leaves, frame);
        var vertex = NearestVertex(leaves, frame);

        if (segment is null || vertex is null)
            return TopologyFixOffer.NotOffered(Id, FixReasons.FeatureMissing);

        var fixDistance = PointOnLineRuleType.FixDistance(context.Rule);

        if (segment.Value.Distance > fixDistance)
            return TopologyFixOffer.NotOffered(Id, FixReasons.BeyondFixDistance, segment.Value.Distance, fixDistance);

        var pointLayer = context.Rule.LayerA;
        var lineLayer = context.Rule.LayerB!;

        if (context.RankOf(pointLayer) > context.RankOf(lineLayer))
        {
            var after = lineGeometry.Clone();
            var afterLeaves = Leaves(after);

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

            var edit = new FeatureEdit(lineLayer, lineFeature!.Id, lineGeometry, after);

            return TopologyFixOffer.Offered(new TopologyFixProposal(Id, variant, context.Finding, new[] { edit }, Box(touched), new object[] { moved }));
        }
        else
        {
            var (x, y) = frame.FromMetres(segment.Value.East, segment.Value.North);

            var after = pointFeature!.TheGeometry.Clone();
            var target = Leaves(after).First(l => l.Points!.Count > 0).Points!;
            target[0] = new Point(x, y);

            var edit = new FeatureEdit(pointLayer, pointFeature.Id, pointFeature.TheGeometry, after);

            var area = Box(new List<Point> { new(point.X, point.Y), new(x, y) });

            return TopologyFixOffer.Offered(new TopologyFixProposal(Id, MovePoint, context.Finding, new[] { edit }, area, new object[] { segment.Value.Distance }));
        }
    }

    /// <summary>The leaf geometries (lines, rings, points) in depth-first order; a clone has the same order.</summary>
    private static List<Geometry<Point>> Leaves(Geometry<Point> geometry)
    {
        var leaves = new List<Geometry<Point>>();

        void Visit(Geometry<Point> g)
        {
            if (g.Points is not null)
                leaves.Add(g);
            else if (g.Geometries is not null)
                foreach (var child in g.Geometries)
                    Visit(child);
        }

        Visit(geometry);

        return leaves;
    }

    private static Point? FirstPoint(Geometry<Point>? geometry)
    {
        if (geometry is null || geometry.IsNullOrEmpty())
            return null;

        var first = Leaves(geometry).SelectMany(l => l.Points!).FirstOrDefault();

        return first is null ? null : new Point(first.X, first.Y);
    }

    /// <summary>The nearest place on any segment, in metres from the point (the frame's origin).</summary>
    private static (int Leaf, int Index, double East, double North, double Distance)? NearestSegment(List<Geometry<Point>> leaves, LocalMetricFrame frame)
    {
        (int, int, double, double, double)? best = null;
        var bestDistance = double.PositiveInfinity;

        for (int l = 0; l < leaves.Count; l++)
        {
            var points = leaves[l].Points!;

            for (int i = 0; i + 1 < points.Count; i++)
            {
                var (ax, ay) = frame.ToMetres(points[i].X, points[i].Y);
                var (bx, by) = frame.ToMetres(points[i + 1].X, points[i + 1].Y);

                var dx = bx - ax;
                var dy = by - ay;
                var length2 = dx * dx + dy * dy;

                var t = length2 == 0 ? 0 : Math.Max(0, Math.Min(1, (-ax * dx - ay * dy) / length2));

                var qx = ax + t * dx;
                var qy = ay + t * dy;
                var distance = Math.Sqrt(qx * qx + qy * qy);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = (l, i, qx, qy, distance);
                }
            }
        }

        return best;
    }

    private static (int Leaf, int Index, double Distance)? NearestVertex(List<Geometry<Point>> leaves, LocalMetricFrame frame)
    {
        (int, int, double)? best = null;
        var bestDistance = double.PositiveInfinity;

        for (int l = 0; l < leaves.Count; l++)
        {
            var points = leaves[l].Points!;

            for (int i = 0; i < points.Count; i++)
            {
                var (east, north) = frame.ToMetres(points[i].X, points[i].Y);
                var distance = Math.Sqrt(east * east + north * north);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = (l, i, distance);
                }
            }
        }

        return best;
    }

    /// <summary>A vertex and its neighbours: the segments a moved vertex changes.</summary>
    private static List<Point> Around(List<Point> points, int index)
    {
        var result = new List<Point>();

        for (int i = Math.Max(0, index - 1); i <= Math.Min(points.Count - 1, index + 1); i++)
            result.Add(new Point(points[i].X, points[i].Y));

        return result;
    }

    private static BoundingBox Box(List<Point> points)
        => new(points.Min(p => p.X), points.Min(p => p.Y), points.Max(p => p.X), points.Max(p => p.Y));
}
