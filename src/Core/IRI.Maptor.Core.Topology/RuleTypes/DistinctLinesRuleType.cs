using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.AdvancedStructures;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.Topology.Findings;
using IRI.Maptor.Core.Topology.Rules;

namespace IRI.Maptor.Core.Topology.RuleTypes;

/// <summary>
/// R22, "lines must not duplicate", within layer A:
/// <list type="bullet">
/// <item><c>wholeLine</c>: two lines with the same vertices within <c>distance</c> (default: the same-place
/// distance), in the same or the opposite order. Decided by comparing vertices, so it does not depend on how the
/// lines run beside each other.</item>
/// <item><c>sharedStretch</c> (only when <c>sharedStretch</c> is 1): two lines that run within <c>distance</c> of
/// each other, the same or the opposite way (within 30°), for at least <c>minimumLength</c> (10 m) in all.</item>
/// </list>
/// One finding per pair of lines.
/// </summary>
public sealed class DistinctLinesRuleType : TopologyRuleTypeBase
{
    public const string TypeId = "distinctLines";

    public const string WholeLine = "wholeLine";
    public const string SharedStretch = "sharedStretch";

    public const string MinimumLengthParameter = "minimumLength";
    public const string SharedStretchParameter = "sharedStretch";

    private const double SharedStretchMaxAngle = 30;

    public DistinctLinesRuleType()
        : base(TypeId, false, (WholeLine, IssueSeverity.Error), (SharedStretch, IssueSeverity.Warning))
    {
    }

    public override double ReachOf(TopologyRule rule) => rule.Parameters.GetNumber(PointLineBuild.DistanceParameter, 0);

    public override IReadOnlyList<string> Validate(TopologyRule rule)
        => rule.Parameters.GetNumber(PointLineBuild.DistanceParameter, 0) < 0 ? new[] { "its distance must not be negative." } : Array.Empty<string>();

    public override IEnumerable<TopologyFinding> Check(TopologyRuleContext context, CancellationToken cancellationToken)
    {
        var rule = context.Rule;

        var distance = rule.Parameters.GetNumber(PointLineBuild.DistanceParameter, context.SamePlace);
        var minimumLength = rule.Parameters.GetNumber(MinimumLengthParameter, 10);
        var checkStretches = rule.Parameters.GetNumber(SharedStretchParameter, 0) != 0;

        var layer = GeometryPreparer.Lines(context, rule.LayerA);
        var isGeodetic = context.IsGeodetic;

        var vertices = layer.Lines.Select(l => l.Parts.SelectMany(p => p.Vertices).ToList()).ToList();

        var boxes = BoxGrid(layer, distance, isGeodetic);

        var duplicates = new HashSet<(int, int)>();

        for (int a = 0; a < layer.Count; a++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var b in boxes.Query(BoxOf(layer.Lines[a], distance, isGeodetic)).Where(b => b > a).OrderBy(b => b))
            {
                if (!SameVertices(vertices[a], vertices[b], distance, isGeodetic))
                    continue;

                duplicates.Add((a, b));

                yield return context.Finding(
                    WholeLine,
                    Pair(context, layer, a, b),
                    layer.Geometries[a],
                    layer.Lines[a].Length,
                    ValueUnit.Metres);
            }
        }

        if (!checkStretches)
            yield break;

        var coverage = new LineCoverageIndex<Point>(layer.Lines);

        for (int a = 0; a < layer.Count; a++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var stretches = coverage.Of(layer.Lines[a], distance, SharedStretchMaxAngle);

            foreach (var byOther in stretches.Where(s => s.OtherLineIndex > a).GroupBy(s => s.OtherLineIndex).OrderBy(g => g.Key))
            {
                var b = byOther.Key;

                if (duplicates.Contains((a, b)))
                    continue;

                var total = LineCoverage.CoveredLength(byOther);

                if (total < minimumLength)
                    continue;

                var longest = byOther.OrderByDescending(s => s.Length).First();
                var part = layer.Lines[a].PartWithIndex(longest.PartIndex)!;

                yield return context.Finding(
                    SharedStretch,
                    Pair(context, layer, a, b),
                    part.Extract(longest.From, longest.To, layer.Lines[a].Srid),
                    total,
                    ValueUnit.Metres);
            }
        }
    }

    private static IReadOnlyList<FindingFeature> Pair(TopologyRuleContext context, PreparedLayer layer, int a, int b)
        => new[] { layer.Features[a], layer.Features[b] }.OrderBy(f => f.Id).Select(context.FeatureA).ToList();

    /// <summary>The same vertices within the distance, in the same or the opposite order.</summary>
    private static bool SameVertices(List<Point> a, List<Point> b, double distance, bool isGeodetic)
    {
        if (a.Count != b.Count || a.Count == 0)
            return false;

        bool Close(Point p, Point q)
        {
            var (east, north) = LocalMetricFrame.At(p, isGeodetic).ToMetres(q);
            return east * east + north * north <= distance * distance;
        }

        var forward = true;
        var backward = true;

        for (int i = 0; i < a.Count && (forward || backward); i++)
        {
            forward = forward && Close(a[i], b[i]);
            backward = backward && Close(a[i], b[b.Count - 1 - i]);
        }

        return forward || backward;
    }

    private static BoxGrid<int> BoxGrid(PreparedLayer layer, double distance, bool isGeodetic)
    {
        var sizes = layer.Lines.Select(l => l.Geometry.GetBoundingBox()).Select(b => Math.Max(b.Width, b.Height)).OrderBy(s => s).ToList();

        var grid = new BoxGrid<int>(sizes.Count == 0 ? 1 : Math.Max(sizes[sizes.Count / 2], 1e-9));

        for (int i = 0; i < layer.Count; i++)
            grid.Add(i, BoxOf(layer.Lines[i], distance, isGeodetic));

        return grid;
    }

    private static BoundingBox BoxOf(MeasuredLine<Point> line, double distance, bool isGeodetic)
    {
        var box = line.Geometry.GetBoundingBox();
        var (dx, dy) = LocalMetricFrame.At(box.Center, isGeodetic).SearchHalfSize(distance);

        return new BoundingBox(box.XMin - dx, box.YMin - dy, box.XMax + dx, box.YMax + dy);
    }
}
