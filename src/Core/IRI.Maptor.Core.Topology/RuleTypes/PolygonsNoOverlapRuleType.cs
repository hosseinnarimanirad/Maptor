using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.AdvancedStructures;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Topology.Findings;
using IRI.Maptor.Core.Topology.Rules;

namespace IRI.Maptor.Core.Topology.RuleTypes;

/// <summary>
/// R40, "polygons must not overlap", within layer A: two polygons sharing more than <c>minimumArea</c>
/// (0.01 m²). An overlap no wider than <c>minimumWidth</c> (default: the same-place distance) is a shared boundary
/// drawn twice and is ignored. Kinds: <c>duplicate</c> (each covers the other), <c>inside</c> (one lies within the
/// other), <c>overlap</c>, and <c>thinOverlap</c> (narrower than <c>sliverWidth</c>, 0.5 m). The overlap is measured
/// with <see cref="PolygonOverlap"/>, which stays right where the polygons share edges or vertices.
/// </summary>
public sealed class PolygonsNoOverlapRuleType : TopologyRuleTypeBase
{
    public const string TypeId = "polygonsNoOverlap";

    public const string Duplicate = "duplicate";
    public const string Inside = "inside";
    public const string Overlap = "overlap";
    public const string ThinOverlap = "thinOverlap";

    public const string MinimumAreaParameter = "minimumArea";
    public const string MinimumWidthParameter = "minimumWidth";
    public const string SliverWidthParameter = "sliverWidth";

    /// <summary>Boundaries this close are one line when measuring; see <see cref="PolygonOverlap.Measure{T}"/>.</summary>
    private const double ExactTolerance = 1e-6;

    public PolygonsNoOverlapRuleType()
        : base(TypeId, false,
            (Duplicate, IssueSeverity.Error),
            (Inside, IssueSeverity.Error),
            (Overlap, IssueSeverity.Error),
            (ThinOverlap, IssueSeverity.Warning))
    {
    }

    public override IReadOnlyList<string> Validate(TopologyRule rule)
        => rule.Parameters.GetNumber(MinimumAreaParameter, 0.01) < 0 ? new[] { "its minimum area must not be negative." } : Array.Empty<string>();

    public override IEnumerable<TopologyFinding> Check(TopologyRuleContext context, CancellationToken cancellationToken)
    {
        var rule = context.Rule;

        var minimumArea = rule.Parameters.GetNumber(MinimumAreaParameter, 0.01);
        var minimumWidth = rule.Parameters.GetNumber(MinimumWidthParameter, context.SamePlace);
        var sliverWidth = rule.Parameters.GetNumber(SliverWidthParameter, 0.5);

        var layer = GeometryPreparer.Polygons(context, rule.LayerA);

        var boxes = layer.Geometries.Select(g => g.GetBoundingBox()).ToList();

        var grid = new BoxGrid<int>(CellSize(boxes));

        for (int i = 0; i < boxes.Count; i++)
            grid.Add(i, boxes[i]);

        for (int a = 0; a < layer.Count; a++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var b in grid.Query(boxes[a]).Where(b => b > a && boxes[a].Intersects(boxes[b])).OrderBy(b => b))
            {
                var measure = PolygonOverlap.Measure(layer.Geometries[a], layer.Geometries[b], context.IsGeodetic, ExactTolerance);

                if (measure.Area <= minimumArea || measure.Width <= minimumWidth)
                    continue;

                var tolerance = Math.Max(minimumArea, 0.001 * Math.Max(measure.FirstArea, measure.SecondArea));

                var coversFirst = Math.Abs(measure.Area - measure.FirstArea) <= tolerance;
                var coversSecond = Math.Abs(measure.Area - measure.SecondArea) <= tolerance;

                var kind = coversFirst && coversSecond ? Duplicate
                    : coversFirst || coversSecond ? Inside
                    : measure.Width < sliverWidth ? ThinOverlap
                    : Overlap;

                var features = new[] { layer.Features[a], layer.Features[b] }.OrderBy(f => f.Id).Select(context.FeatureA).ToList();

                var location = measure.Centroid is Point centre
                    ? Geometry<Point>.Create(centre.X, centre.Y, layer.Geometries[a].Srid)
                    : null;

                yield return context.Finding(kind, features, location, measure.Area, ValueUnit.SquareMetres, messageArguments: new object[] { measure.Width });
            }
        }
    }

    internal static double CellSize(IReadOnlyList<BoundingBox> boxes)
    {
        var sizes = boxes.Select(b => Math.Max(b.Width, b.Height)).OrderBy(s => s).ToList();

        return sizes.Count == 0 ? 1 : Math.Max(sizes[sizes.Count / 2], 1e-9);
    }
}
