using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.AdvancedStructures;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Topology.Findings;
using IRI.Maptor.Core.Topology.Rules;

namespace IRI.Maptor.Core.Topology.RuleTypes;

/// <summary>
/// R41, "polygons must not have gaps", within layer A:
/// <list type="bullet">
/// <item><c>sliver</c>: a stretch, at least <c>minimumLength</c> (1 m) long, where the boundaries of two polygons run
/// closer than <c>distance</c> (0.5 m) but farther than the same-place distance, with empty space between them. A
/// street between blocks is wider than the distance, so it is not a sliver; the outer edge of the layer is never
/// one.</item>
/// <item><c>enclosedGap</c> (only when <c>enclosedGaps</c> is 1): empty space fully surrounded by two or more
/// polygons, at most <c>maximumGapArea</c> square metres (no limit by default).</item>
/// </list>
/// </summary>
public sealed class PolygonsNoGapRuleType : TopologyRuleTypeBase
{
    public const string TypeId = "polygonsNoGap";

    public const string Sliver = "sliver";
    public const string EnclosedGap = "enclosedGap";

    public const string MinimumLengthParameter = "minimumLength";
    public const string EnclosedGapsParameter = "enclosedGaps";
    public const string MaximumGapAreaParameter = "maximumGapArea";

    public PolygonsNoGapRuleType()
        : base(TypeId, false, (Sliver, IssueSeverity.Warning), (EnclosedGap, IssueSeverity.Info))
    {
    }

    public override double ReachOf(TopologyRule rule) => Distance(rule);

    public override IReadOnlyList<string> Validate(TopologyRule rule)
        => Distance(rule) <= 0 ? new[] { "its distance must be positive." } : Array.Empty<string>();

    public override IEnumerable<TopologyFinding> Check(TopologyRuleContext context, CancellationToken cancellationToken)
    {
        var rule = context.Rule;

        var distance = Distance(rule);
        var minimumLength = rule.Parameters.GetNumber(MinimumLengthParameter, 1);
        var samePlace = context.SamePlace;
        var isGeodetic = context.IsGeodetic;

        var layer = GeometryPreparer.Polygons(context, rule.LayerA);

        var boxes = layer.Geometries.Select(g => g.GetBoundingBox()).ToList();

        var grid = new BoxGrid<int>(PolygonsNoOverlapRuleType.CellSize(boxes));

        for (int i = 0; i < boxes.Count; i++)
            grid.Add(i, Grow(boxes[i], distance, isGeodetic));

        for (int a = 0; a < layer.Count; a++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var b in grid.Query(Grow(boxes[a], distance, isGeodetic)).Where(b => b > a).OrderBy(b => b))
            {
                var near = LineCoverage.Compute(layer.Lines[a], new[] { layer.Lines[b] }, distance);

                if (near.Count == 0)
                    continue;

                var shared = LineCoverage.Compute(layer.Lines[a], new[] { layer.Lines[b] }, samePlace);

                var apart = LineCoverage.Subtract(near.Select(s => (s.PartIndex, s.From, s.To)), shared.Select(s => (s.PartIndex, s.From, s.To)));

                var neighbour = new LineProximity<Point>(new[] { layer.Lines[b] });

                var ordinal = 0;

                foreach (var stretch in apart.Where(s => s.To - s.From >= minimumLength))
                {
                    var part = layer.Lines[a].PartWithIndex(stretch.PartIndex)!;

                    var middle = part.PointAt((stretch.From + stretch.To) / 2);

                    var across = neighbour.Nearest(middle, distance * 1.01);

                    if (across is null)
                        continue;

                    var gapPoint = new Point((middle.X + across.Location.X) / 2, (middle.Y + across.Location.Y) / 2);

                    if (TopologyUtility.IsPointInPolygon(layer.Geometries[a], gapPoint) || TopologyUtility.IsPointInPolygon(layer.Geometries[b], gapPoint))
                        continue;

                    var features = new[] { layer.Features[a], layer.Features[b] }.OrderBy(f => f.Id).Select(context.FeatureA).ToList();

                    yield return context.Finding(
                        Sliver,
                        features,
                        part.Extract(stretch.From, stretch.To, layer.Lines[a].Srid),
                        stretch.To - stretch.From,
                        ValueUnit.Metres,
                        $"stretch {ordinal++}",
                        new object[] { across.Distance });
                }
            }
        }

        if (rule.Parameters.GetNumber(EnclosedGapsParameter, 0) == 0)
            yield break;

        var maximumArea = rule.Parameters.GetNumber(MaximumGapAreaParameter, double.PositiveInfinity);

        var gaps = EnclosedGaps.Find(layer.Geometries.Select(g => (Geometry<Point>?)g).ToList(), isGeodetic, samePlace);

        foreach (var byNeighbours in gaps.Where(g => g.Area <= maximumArea).GroupBy(g => string.Join(",", g.PolygonIndices)))
        {
            var ordinal = 0;

            foreach (var gap in byNeighbours.OrderBy(g => g.Area))
            {
                var features = gap.PolygonIndices.Select(i => layer.Features[i]).OrderBy(f => f.Id).Select(context.FeatureA).ToList();

                yield return context.Finding(EnclosedGap, features, gap.Outline, gap.Area, ValueUnit.SquareMetres, $"gap {ordinal++}");
            }
        }
    }

    private static BoundingBox Grow(BoundingBox box, double distance, bool isGeodetic)
    {
        var (dx, dy) = LocalMetricFrame.At(box.Center, isGeodetic).SearchHalfSize(distance);

        return new BoundingBox(box.XMin - dx, box.YMin - dy, box.XMax + dx, box.YMax + dy);
    }

    private static double Distance(TopologyRule rule) => rule.Parameters.GetNumber(PointLineBuild.DistanceParameter, 0.5);
}
