using IRI.Maptor.Core.Common.Abstractions;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Core.Spatial.Validation;

/// <summary>
/// One problem in one feature's geometry.
/// </summary>
public class GeometryIssue<T> where T : IPoint, new()
{
    internal GeometryIssue(Feature<T> feature, int featureIndex, GeometryIssueKind kind, int? partIndex, int? ringIndex)
    {
        Feature = feature;
        FeatureIndex = featureIndex;
        Kind = kind;
        PartIndex = partIndex;
        RingIndex = ringIndex;
    }

    /// <summary>The feature, as passed to <see cref="GeometryValidator"/>.</summary>
    public Feature<T> Feature { get; }

    /// <summary>The feature's position in the validated list.</summary>
    public int FeatureIndex { get; }

    public GeometryIssueKind Kind { get; }

    /// <summary>The part inside a multi-part geometry (MultiPolygon, MultiLineString, …); null for a single-part geometry.</summary>
    public int? PartIndex { get; }

    /// <summary>The ring inside a polygon (0 = outer ring); null for points and lines.</summary>
    public int? RingIndex { get; }

    public bool CanFix => Kind.CanFix();

    public bool IsFixedAutomatically => Kind.IsFixedAutomatically();

    public override string ToString() => $"Feature {FeatureIndex}: {Kind} (part {PartIndex?.ToString() ?? "-"}, ring {RingIndex?.ToString() ?? "-"})";
}
