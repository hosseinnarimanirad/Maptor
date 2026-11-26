using System.Globalization;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis.PointsOnLines;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Topology.Rules;

namespace IRI.Maptor.Core.Topology.RuleTypes;

/// <summary>
/// One <see cref="PointLineTopologyBuilder{T}"/> pass over a point layer and a line layer, shared by the
/// points-on-lines rule types (R10–R14) of one check: the first of them builds it, the others reuse it.
/// </summary>
internal sealed class PointLineBuild
{
    public const string DistanceParameter = "distance";
    public const string NearDistanceParameter = "nearDistance";
    public const string SequenceFieldParameter = "sequenceField";

    public const double DefaultDistance = 0.5;
    public const double DefaultNearDistance = 50;

    private PointLineBuild(string pointLayer, string lineLayer, IReadOnlyList<Feature<Point>> points, IReadOnlyList<Feature<Point>> lines, PointLineTopology<Point> topology)
    {
        PointLayer = pointLayer;
        LineLayer = lineLayer;
        Points = points;
        Lines = lines;
        Topology = topology;
    }

    public string PointLayer { get; }

    public string LineLayer { get; }

    public IReadOnlyList<Feature<Point>> Points { get; }

    public IReadOnlyList<Feature<Point>> Lines { get; }

    public PointLineTopology<Point> Topology { get; }

    /// <summary>
    /// The shared build for a rule whose points are on side A (R10, R13, R14) or on side B (R11, R12), at the
    /// given distance. Sequence numbers are read when the rule set has an enabled sequence rule (R14) for the
    /// same two layers, so that all five rules can share one pass.
    /// </summary>
    public static PointLineBuild Get(TopologyRuleContext context, bool pointsAreA, double distance)
    {
        var rule = context.Rule;

        var pointLayer = pointsAreA ? rule.LayerA : rule.LayerB!;
        var lineLayer = pointsAreA ? rule.LayerB! : rule.LayerA;

        var points = pointsAreA ? context.FeaturesA : context.FeaturesB;
        var lines = pointsAreA ? context.FeaturesB : context.FeaturesA;

        var sequenceField = SequenceFieldOf(context.RuleSet, pointLayer, lineLayer);

        var key = string.Format(CultureInfo.InvariantCulture, "pointsOnLines|{0}:{1}|{2}:{3}|{4:R}|{5}", pointLayer, context.SelectionOf(pointLayer), lineLayer, context.SelectionOf(lineLayer), distance, sequenceField);

        return context.GetShared(key, () =>
        {
            var options = new PointLineTopologyOptions
            {
                Tolerance = distance,
                CoincidentTolerance = context.SamePlace,
                IsGeodetic = context.IsGeodetic,
            };

            var sequences = sequenceField is null ? null : points.Select(p => SequenceOf(p, sequenceField)).ToList();

            var topology = new PointLineTopologyBuilder<Point>(options).Build(
                lines.Select(l => l.TheGeometry).ToList(),
                points.Select(PointOf).ToList(),
                sequences);

            return new PointLineBuild(pointLayer, lineLayer, points, lines, topology);
        });
    }

    /// <summary>The sequence field of the rule set's enabled sequence rule for these two layers, if any.</summary>
    public static string? SequenceFieldOf(TopologyRuleSet ruleSet, string pointLayer, string lineLayer)
        => ruleSet.Rules
            .Where(r => r.Enabled && r.RuleTypeId == SequenceAlongLineRuleType.TypeId && r.LayerA == pointLayer && r.LayerB == lineLayer)
            .Select(r => r.Parameters.GetText(SequenceFieldParameter))
            .FirstOrDefault(f => !string.IsNullOrWhiteSpace(f));

    /// <summary>The point of a point feature (the first one of a multipoint); null when it has none.</summary>
    public static Point? PointOf(Feature<Point> feature)
    {
        var geometry = feature.TheGeometry;

        if (geometry is null)
            return null;

        if (geometry.Points is { Count: > 0 })
            return geometry.Points[0];

        return geometry.Geometries?.FirstOrDefault(g => g.Points is { Count: > 0 })?.Points[0];
    }

    /// <summary>A whole number from the attribute, or null when it is missing or not a whole number.</summary>
    public static long? SequenceOf(Feature<Point> feature, string field)
    {
        if (feature.Attributes is null || !feature.Attributes.TryGetValue(field, out var value) || value is null)
            return null;

        switch (value)
        {
            case long l:
                return l;
            case int i:
                return i;
            case short s:
                return s;
            case byte b:
                return b;
            case double d when Math.Abs(d - Math.Round(d)) < 1e-9 && Math.Abs(d) < long.MaxValue:
                return (long)Math.Round(d);
            case decimal m when m == decimal.Truncate(m):
                return (long)m;
            default:
                return long.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                    ? parsed
                    : null;
        }
    }
}
