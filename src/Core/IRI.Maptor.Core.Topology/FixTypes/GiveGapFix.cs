using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Topology.Findings;
using IRI.Maptor.Core.Topology.Fixes;
using IRI.Maptor.Core.Topology.RuleTypes;

namespace IRI.Maptor.Core.Topology.FixTypes;

/// <summary>
/// Fix for R41: empty space between neighbouring polygons is given to one of them
/// (<see cref="PolygonBoolean.Union{T}"/>).
/// <list type="bullet">
/// <item><c>enclosedGap</c>: the gap's outline is added to the polygon taking it.</item>
/// <item><c>sliver</c>: the thin space along the reported stretch, between the stretch and the facing stretch of the
/// neighbour's boundary, is added; the taking polygon then meets the other along the stretch.</item>
/// </list>
/// The lower-ranked layers take it (FR-5.4); among equal ranks the expert chooses, one offer per polygon. Message
/// arguments: the polygon taking the space, the other polygons.
/// </summary>
public sealed class GiveGapFix : ITopologyFixType
{
    public const string TypeId = "giveGap";

    public const string TakeGap = "takeGap";
    public const string TakeSliver = "takeSliver";

    private const double ExactTolerance = 1e-6;

    private static readonly IReadOnlyCollection<string> FixedKinds = new[] { PolygonsNoGapRuleType.Sliver, PolygonsNoGapRuleType.EnclosedGap };

    public string Id => TypeId;

    public string RuleTypeId => PolygonsNoGapRuleType.TypeId;

    public IReadOnlyCollection<string> Kinds => FixedKinds;

    public IReadOnlyList<TopologyFixOffer> Propose(TopologyFixContext context)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));

        var members = context.Features.Where(m => m.Feature.TheGeometry is not null).ToList();

        if (members.Count < 2 || members.Count < context.Finding.Features.Count || context.Finding.Location is null)
            return new[] { TopologyFixOffer.NotOffered(Id, FixReasons.FeatureMissing) };

        var isSliver = context.Finding.Kind == PolygonsNoGapRuleType.Sliver;

        var space = isSliver
            ? SliverOutline(context.Finding.Location, members[0].Feature.TheGeometry, members[1].Feature.TheGeometry, context.IsGeodetic)
            : context.Finding.Location;

        if (space is null)
            return new[] { TopologyFixOffer.NotOffered(Id, FixReasons.NotFound) };

        var lowest = members.Min(m => context.RankOf(m.Reference.EntityName));

        var offers = new List<TopologyFixOffer>();

        foreach (var (taker, feature) in members.Where(m => context.RankOf(m.Reference.EntityName) == lowest))
        {
            var after = PolygonBoolean.Union(feature.TheGeometry, space, context.IsGeodetic, ExactTolerance);

            if (after is null)
            {
                offers.Add(TopologyFixOffer.NotOffered(Id, FixReasons.NotFound));
                continue;
            }

            var others = members.Where(m => !ReferenceEquals(m.Feature, feature)).Select(m => m.Reference).ToList();

            var edit = new FeatureEdit(taker.EntityName, taker.FeatureId, feature.TheGeometry, after);

            offers.Add(TopologyFixOffer.Offered(new TopologyFixProposal(Id, isSliver ? TakeSliver : TakeGap, context.Finding, new[] { edit },
                FixGeometry.Box(new[] { space }), new object[] { taker, others })));
        }

        return offers;
    }

    /// <summary>
    /// The thin space along a sliver stretch: the convex hull of the stretch (on one polygon's boundary) and the facing
    /// part of the other polygon's boundary (the shorter way round its ring), without both polygons. Null when
    /// nothing is left.
    /// </summary>
    internal static Geometry<Point>? SliverOutline(Geometry<Point> stretch, Geometry<Point> first, Geometry<Point> second, bool isGeodetic)
    {
        var stretchPoints = FixGeometry.Leaves(stretch).SelectMany(l => l.Points!).ToList();

        if (stretchPoints.Count < 2)
            return null;

        var box = first.GetBoundingBox().Add(second.GetBoundingBox());
        var frame = LocalMetricFrame.At(box.Center, isGeodetic);

        var a = PlanarPolygon.From(first, frame);
        var b = PlanarPolygon.From(second, frame);

        var path = stretchPoints.Select(p => frame.ToMetres(p.X, p.Y)).Select(m => (X: m.East, Y: m.North)).ToList();

        // The stretch lies on one polygon; the other faces it.
        var middle = path[path.Count / 2];
        var facing = Distance(a, middle) <= Distance(b, middle) ? b : a;

        var start = Nearest(facing, path[0]);
        var end = Nearest(facing, path[path.Count - 1]);

        if (start is null || end is null || start.Value.Ring != end.Value.Ring)
            return null;

        var ring = facing.Rings[start.Value.Ring];
        var back = Walk(ring, end.Value, start.Value);

        // The convex hull of both stretches, without the two polygons, is exactly the empty space between them
        // there; a stretch that turns a corner by a few centimetres leaves no spike this way.
        var points = stretchPoints.Select(p => new Point(p.X, p.Y))
            .Concat(back.Select(m => frame.FromMetres(m.X, m.Y)).Select(c => new Point(c.X, c.Y)))
            .ToList();

        var hull = ComputationalGeometry.CreateConvexHull(points);

        if (hull.Count < 3)
            return null;

        var region = Geometry<Point>.CreatePolygon(hull, stretch.Srid);

        var withoutFirst = PolygonBoolean.Difference(region, first, isGeodetic, ExactTolerance);

        return withoutFirst is null ? null : PolygonBoolean.Difference(withoutFirst, second, isGeodetic, ExactTolerance);
    }

    private static double Distance(PlanarPolygon polygon, (double X, double Y) point)
        => polygon.Edges().Select(e => SegmentDistance(point, e.Start, e.End)).DefaultIfEmpty(double.MaxValue).Min();

    private static double SegmentDistance((double X, double Y) point, (double X, double Y) p, (double X, double Y) q)
    {
        var dx = q.X - p.X;
        var dy = q.Y - p.Y;
        var length2 = dx * dx + dy * dy;

        var t = length2 == 0 ? 0 : Math.Max(0, Math.Min(1, ((point.X - p.X) * dx + (point.Y - p.Y) * dy) / length2));

        var x = p.X + t * dx - point.X;
        var y = p.Y + t * dy - point.Y;

        return Math.Sqrt(x * x + y * y);
    }

    /// <summary>The nearest place on any ring: the ring, the edge, the place and how far along the ring it is.</summary>
    private static (int Ring, int Edge, (double X, double Y) Point, double Along)? Nearest(PlanarPolygon polygon, (double X, double Y) point)
    {
        (int, int, (double, double), double)? best = null;
        var bestDistance = double.MaxValue;

        for (int r = 0; r < polygon.Rings.Count; r++)
        {
            var ring = polygon.Rings[r];
            var along = 0.0;

            for (int i = 0; i < ring.Length; i++)
            {
                var p = ring[i];
                var q = ring[(i + 1) % ring.Length];

                var dx = q.X - p.X;
                var dy = q.Y - p.Y;
                var length2 = dx * dx + dy * dy;
                var length = Math.Sqrt(length2);

                var t = length2 == 0 ? 0 : Math.Max(0, Math.Min(1, ((point.X - p.X) * dx + (point.Y - p.Y) * dy) / length2));

                var x = p.X + t * dx;
                var y = p.Y + t * dy;
                var distance = Math.Sqrt((x - point.X) * (x - point.X) + (y - point.Y) * (y - point.Y));

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = (r, i, (x, y), along + t * length);
                }

                along += length;
            }
        }

        return best;
    }

    /// <summary>The ring's points from one place to another, the shorter way round, both places included.</summary>
    private static List<(double X, double Y)> Walk((double X, double Y)[] ring, (int Ring, int Edge, (double X, double Y) Point, double Along) from, (int Ring, int Edge, (double X, double Y) Point, double Along) to)
    {
        var perimeter = 0.0;

        for (int i = 0; i < ring.Length; i++)
        {
            var p = ring[i];
            var q = ring[(i + 1) % ring.Length];
            perimeter += Math.Sqrt((q.X - p.X) * (q.X - p.X) + (q.Y - p.Y) * (q.Y - p.Y));
        }

        var forward = ((to.Along - from.Along) % perimeter + perimeter) % perimeter;

        var result = new List<(double X, double Y)> { from.Point };
        var n = ring.Length;

        // Edge i runs from ring[i] to ring[i + 1].
        if (forward <= perimeter - forward)
        {
            // Forward: the vertices after the start's edge, up to the end's edge's first vertex.
            if (!(from.Edge == to.Edge && to.Along >= from.Along))
            {
                for (int i = (from.Edge + 1) % n; ; i = (i + 1) % n)
                {
                    result.Add(ring[i]);

                    if (i == to.Edge)
                        break;
                }
            }
        }
        else
        {
            // Backward: the start's edge's first vertex, down to the vertex after the end's edge.
            if (!(from.Edge == to.Edge && to.Along <= from.Along))
            {
                for (int i = from.Edge; ; i = (i - 1 + n) % n)
                {
                    result.Add(ring[i]);

                    if (i == (to.Edge + 1) % n)
                        break;
                }
            }
        }
        result.Add(to.Point);

        return result;
    }
}
