using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.AdvancedStructures;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Topology.Findings;
using IRI.Maptor.Core.Topology.Rules;

namespace IRI.Maptor.Core.Topology.RuleTypes;

/// <summary>
/// R20, "line ends must connect": every end of a line of layer A should meet something. The first of these that
/// holds decides:
/// <list type="number">
/// <item>Connected (nothing reported): another line's end or body is within the same-place distance; the part is
/// closed; the end is within <c>distance</c> of a point of an <c>endCoverLayers</c> layer (e.g. the towers a route
/// properly ends at); or another part of the same line meets it there (that is the parts rule's concern).</item>
/// <item><c>unsnappedEnd</c>: another line's end is within <c>distance</c> (0.5 m) but not at the same place. Ends
/// are grouped, one finding per group; a group wider than twice the distance is a <c>chain</c>.</item>
/// <item><c>overshoot</c>: the line crosses another and ends at most <c>maxOvershoot</c> (5 m) past it.</item>
/// <item><c>undershoot</c>: another line's body is within <c>searchDistance</c> (5 m).</item>
/// <item><c>freeEnd</c>: none of these; information only, the true end of a network.</item>
/// </list>
/// "Another line" is another line of layer A or a line of a <c>connectTo</c> layer.
/// </summary>
public sealed class LineEndsConnectRuleType : TopologyRuleTypeBase
{
    public const string TypeId = "lineEndsConnect";

    public const string UnsnappedEnd = "unsnappedEnd";
    public const string Overshoot = "overshoot";
    public const string Undershoot = "undershoot";
    public const string FreeEnd = "freeEnd";
    public const string Chain = "chain";

    public const string SearchDistanceParameter = "searchDistance";
    public const string MaxOvershootParameter = "maxOvershoot";
    public const string ConnectToParameter = "connectTo";
    public const string EndCoverLayersParameter = "endCoverLayers";

    public LineEndsConnectRuleType()
        : base(TypeId, false,
            (UnsnappedEnd, IssueSeverity.Warning),
            (Overshoot, IssueSeverity.Warning),
            (Undershoot, IssueSeverity.Warning),
            (FreeEnd, IssueSeverity.Info),
            (Chain, IssueSeverity.Error))
    {
    }

    public override IReadOnlyCollection<string> ExtraLayersOf(TopologyRule rule)
        => rule.Parameters.GetTexts(ConnectToParameter).Concat(rule.Parameters.GetTexts(EndCoverLayersParameter)).Distinct().ToList();

    public override double ReachOf(TopologyRule rule)
        => Math.Max(Distance(rule), Math.Max(SearchDistance(rule), MaxOvershoot(rule)));

    public override IReadOnlyList<string> Validate(TopologyRule rule)
    {
        var problems = new List<string>();

        if (Distance(rule) <= 0)
            problems.Add("its distance must be positive.");

        if (SearchDistance(rule) < Distance(rule))
            problems.Add("its search distance must not be smaller than its distance.");

        if (MaxOvershoot(rule) < 0)
            problems.Add("its overshoot limit must not be negative.");

        return problems;
    }

    public override IEnumerable<TopologyFinding> Check(TopologyRuleContext context, CancellationToken cancellationToken)
    {
        var rule = context.Rule;

        var distance = Distance(rule);
        var searchDistance = SearchDistance(rule);
        var maxOvershoot = MaxOvershoot(rule);
        var samePlace = context.SamePlace;
        var isGeodetic = context.IsGeodetic;

        // All lines an end may meet: layer A first, then the connect-to layers.
        var layers = new[] { GeometryPreparer.Lines(context, rule.LayerA) }
            .Concat(rule.Parameters.GetTexts(ConnectToParameter).Where(l => l != rule.LayerA).Select(l => GeometryPreparer.Lines(context, l)))
            .ToList();

        var entries = layers.SelectMany(layer => Enumerable.Range(0, layer.Count).Select(i => (Layer: layer.EntityName, Feature: layer.Features[i], Line: layer.Lines[i]))).ToList();

        var ownCount = layers[0].Count;

        var lines = entries.Select(e => e.Line).ToList();

        var proximity = new LineProximity<Point>(lines);

        var crossings = SegmentCrossings.Find(lines.Take(ownCount).ToList(), lines, samePlace)
            .Where(c => c.LineIndex != c.OtherLineIndex)
            .ToList();

        var ends = new List<LineEnd>();

        for (int l = 0; l < entries.Count; l++)
        {
            foreach (var part in entries[l].Line.Parts)
            {
                ends.Add(new LineEnd(l, part, isStart: true));
                ends.Add(new LineEnd(l, part, isStart: false));
            }
        }

        var endGrid = PointGrid(ends.Select(e => e.Point).ToList(), distance, isGeodetic);

        var coverPoints = rule.Parameters.GetTexts(EndCoverLayersParameter)
            .SelectMany(context.FeaturesOf)
            .Select(PointLineBuild.PointOf)
            .Where(p => p is not null)
            .Select(p => p!)
            .ToList();

        var coverGrid = PointGrid(coverPoints, distance, isGeodetic);

        var unsnapped = new List<LineEnd>();

        for (int e = 0; e < ends.Count; e++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var end = ends[e];

            if (end.Line >= ownCount)
                continue;

            var frame = LocalMetricFrame.At(end.Point, isGeodetic);

            double Metres(Point other)
            {
                var (east, north) = frame.ToMetres(other);
                return Math.Sqrt(east * east + north * north);
            }

            var nearbyEnds = Near(endGrid, end.Point, distance, isGeodetic)
                .Where(i => i != e)
                .Select(i => (Index: i, Distance: Metres(ends[i].Point)))
                .Where(x => x.Distance <= distance)
                .ToList();

            // 1. Connected.
            var closesItself = Metres(end.OtherEnd) <= samePlace;
            var meetsOwnPart = nearbyEnds.Any(x => ends[x.Index].Line == end.Line && ends[x.Index].Part != end.Part);
            var meetsOtherEnd = nearbyEnds.Any(x => ends[x.Index].Line != end.Line && x.Distance <= samePlace);
            var meetsOtherBody = proximity.Nearest(end.Point, samePlace, l => l != end.Line) is not null;
            var isCovered = Near(coverGrid, end.Point, distance, isGeodetic).Any(i => Metres(coverPoints[i]) <= distance);

            if (closesItself || meetsOwnPart || meetsOtherEnd || meetsOtherBody || isCovered)
                continue;

            var line = entries[end.Line];
            var location = Geometry<Point>.Create(end.Point.X, end.Point.Y, line.Line.Srid);

            // 2. Another end within the distance.
            if (nearbyEnds.Any(x => ends[x.Index].Line != end.Line))
            {
                unsnapped.Add(end);
                continue;
            }

            // 3. Past a crossing.
            var past = crossings
                .Where(c => c.LineIndex == end.Line && c.PartIndex == end.Part.Index)
                .Select(c => (Crossing: c, Length: end.IsStart ? c.Measure : end.Part.Length - c.Measure))
                .Where(x => x.Length > samePlace)
                .OrderBy(x => x.Length)
                .FirstOrDefault();

            if (past.Crossing is not null && past.Length <= maxOvershoot)
            {
                yield return context.Finding(
                    Overshoot,
                    new[] { Reference(context, line), Reference(context, entries[past.Crossing.OtherLineIndex]) },
                    location,
                    past.Length,
                    ValueUnit.Metres,
                    Discriminator(end));

                continue;
            }

            // 4. Short of another line.
            var nearest = proximity.Nearest(end.Point, searchDistance, l => l != end.Line);

            if (nearest is not null)
            {
                yield return context.Finding(
                    Undershoot,
                    new[] { Reference(context, line), Reference(context, entries[nearest.LineIndex]) },
                    location,
                    nearest.Distance,
                    ValueUnit.Metres,
                    Discriminator(end));

                continue;
            }

            // 5. Nothing nearby.
            yield return context.Finding(FreeEnd, new[] { Reference(context, line) }, location, discriminator: Discriminator(end));
        }

        foreach (var finding in UnsnappedGroups(context, entries, ends, unsnapped, distance, isGeodetic))
            yield return finding;
    }

    /// <summary>One finding per group of ends that are close but not at the same place.</summary>
    private static IEnumerable<TopologyFinding> UnsnappedGroups(
        TopologyRuleContext context,
        List<(string Layer, Feature<Point> Feature, MeasuredLine<Point> Line)> entries,
        List<LineEnd> ends,
        List<LineEnd> unsnapped,
        double distance,
        bool isGeodetic)
    {
        if (unsnapped.Count == 0)
            yield break;

        var clusters = PositionClusterer.Cluster(ends.Select(e => (Point?)e.Point).ToList(), distance, isGeodetic);

        var candidates = new HashSet<LineEnd>(unsnapped);

        foreach (var cluster in clusters)
        {
            var members = cluster.Members.Select(i => ends[i]).ToList();

            var first = members.Where(candidates.Contains).OrderBy(m => entries[m.Line].Feature.Id).ThenBy(m => m.Part.Index).ThenBy(m => m.IsStart ? 0 : 1).FirstOrDefault();

            if (first is null)
                continue;

            var features = members
                .Select(m => entries[m.Line])
                .GroupBy(x => (x.Layer, x.Feature.Id))
                .Select(g => g.First())
                .OrderBy(x => x.Layer, StringComparer.Ordinal)
                .ThenBy(x => x.Feature.Id)
                .Select(x => Reference(context, x))
                .ToList();

            yield return context.Finding(
                cluster.IsChain ? Chain : UnsnappedEnd,
                features,
                Geometry<Point>.Create(first.Point.X, first.Point.Y, entries[first.Line].Line.Srid),
                cluster.Diameter,
                ValueUnit.Metres,
                Discriminator(first));
        }
    }

    private static FindingFeature Reference(TopologyRuleContext context, (string Layer, Feature<Point> Feature, MeasuredLine<Point> Line) entry)
        => context.FeatureOf(entry.Layer, entry.Feature);

    private static string Discriminator(LineEnd end) => $"part {end.Part.Index} {(end.IsStart ? "start" : "end")}";

    private static BoxGrid<int> PointGrid(IReadOnlyList<Point> points, double distance, bool isGeodetic)
    {
        var cell = points.Count == 0 ? 1 : 2 * points.Select(p => LocalMetricFrame.At(p, isGeodetic).SearchHalfSize(distance)).Max(h => Math.Max(h.HalfWidth, h.HalfHeight));

        var grid = new BoxGrid<int>(cell);

        for (int i = 0; i < points.Count; i++)
            grid.Add(i, points[i].X, points[i].Y, points[i].X, points[i].Y);

        return grid;
    }

    private static IEnumerable<int> Near(BoxGrid<int> grid, Point point, double distance, bool isGeodetic)
    {
        var (dx, dy) = LocalMetricFrame.At(point, isGeodetic).SearchHalfSize(distance);

        return grid.Query(point.X - dx, point.Y - dy, point.X + dx, point.Y + dy);
    }

    internal static double Distance(TopologyRule rule) => rule.Parameters.GetNumber(PointLineBuild.DistanceParameter, PointLineBuild.DefaultDistance);

    internal static double SearchDistance(TopologyRule rule) => rule.Parameters.GetNumber(SearchDistanceParameter, 5);

    internal static double MaxOvershoot(TopologyRule rule) => rule.Parameters.GetNumber(MaxOvershootParameter, 5);

    /// <summary>One end of one part of one line.</summary>
    private sealed class LineEnd
    {
        public LineEnd(int line, MeasuredPart<Point> part, bool isStart)
        {
            Line = line;
            Part = part;
            IsStart = isStart;
            Point = isStart ? part.Vertices[0] : part.Vertices[part.Vertices.Count - 1];
            OtherEnd = isStart ? part.Vertices[part.Vertices.Count - 1] : part.Vertices[0];
        }

        public int Line { get; }

        public MeasuredPart<Point> Part { get; }

        public bool IsStart { get; }

        public Point Point { get; }

        public Point OtherEnd { get; }
    }
}
