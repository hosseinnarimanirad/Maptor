using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Spatial.Validation;
using IRI.Maptor.Core.Topology.Rules;
using IRI.Maptor.Extensions;

namespace IRI.Maptor.Core.Topology.RuleTypes;

/// <summary>A layer's usable features, each with its geometry prepared for measuring.</summary>
internal sealed class PreparedLayer
{
    public PreparedLayer(string entityName, IReadOnlyList<Feature<Point>> features, IReadOnlyList<Geometry<Point>> geometries, IReadOnlyList<MeasuredLine<Point>> lines)
    {
        EntityName = entityName;
        Features = features;
        Geometries = geometries;
        Lines = lines;
    }

    public string EntityName { get; }

    /// <summary>The usable features, in the layer's order.</summary>
    public IReadOnlyList<Feature<Point>> Features { get; }

    /// <summary>The geometry each feature is checked with: its own, or its repaired one.</summary>
    public IReadOnlyList<Geometry<Point>> Geometries { get; }

    /// <summary>The lines, or for polygons their rings, measured.</summary>
    public IReadOnlyList<MeasuredLine<Point>> Lines { get; }

    public int Count => Features.Count;
}

/// <summary>
/// Prepares a layer's features for the relation rules, once per check: a feature whose geometry cannot be
/// repaired (no geometry, bad coordinates, nothing left) is left out, because the geometry rule (R00) reports
/// it; a feature with a repairable problem (a repeated point, a closed ring) is checked with its repaired
/// geometry.
/// </summary>
internal static class GeometryPreparer
{
    public static PreparedLayer Lines(TopologyRuleContext context, string entityName)
        => Prepare(context, entityName, polygons: false);

    public static PreparedLayer Polygons(TopologyRuleContext context, string entityName)
        => Prepare(context, entityName, polygons: true);

    private static PreparedLayer Prepare(TopologyRuleContext context, string entityName, bool polygons)
    {
        var key = $"prepared|{(polygons ? "rings" : "lines")}|{entityName}:{context.SelectionOf(entityName)}|{context.IsGeodetic}";

        return context.GetShared(key, () =>
        {
            var features = new List<Feature<Point>>();
            var geometries = new List<Geometry<Point>>();
            var lines = new List<MeasuredLine<Point>>();

            foreach (var feature in context.FeaturesOf(entityName))
            {
                var geometry = Usable(feature);

                if (geometry is null)
                    continue;

                var measured = polygons ? MeasuredLine<Point>.FromRings(geometry, context.IsGeodetic) : MeasuredLine<Point>.FromLine(geometry, context.IsGeodetic);

                if (measured.IsEmpty)
                    continue;

                features.Add(feature);
                geometries.Add(geometry);
                lines.Add(measured);
            }

            return new PreparedLayer(entityName, features, geometries, lines);
        });
    }

    private static Geometry<Point>? Usable(Feature<Point> feature)
    {
        if (feature.TheGeometry is null || feature.TheGeometry.IsNullOrEmpty())
            return null;

        var result = GeometryValidator.Validate(feature);

        if (result is null)
            return feature.TheGeometry;

        return result.Outcome == GeometryRepairOutcome.Repaired ? result.RepairedGeometry : null;
    }
}
