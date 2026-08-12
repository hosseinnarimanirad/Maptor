using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Jab.Blazor.Rendering;

/// <summary>
/// A vector layer as the canvas needs it: geometry already in Web Mercator, plus the symbology
/// and the on/off state to draw it with.
///
/// Host-agnostic by design — it carries no service URL, no credentials and no schema. A host
/// fetches features however it must and hands the result over here, which is what lets the same
/// renderer serve any Maptor web client. Colours stay in the stack's stored <c>#AARRGGBB</c> form
/// and are converted at draw time (see <see cref="Toc.HexColor"/>).
///
/// Mutable: a TOC toggle flips <see cref="IsVisible"/> or <see cref="Opacity"/> in place and the
/// map redraws, without the features being fetched or re-projected again.
/// </summary>
public sealed class MapVectorLayer
{
    /// <summary>Matches the TOC node id, so a TOC edit can find its layer.</summary>
    public required string Id { get; init; }

    public required string Title { get; init; }

    private FeatureSet<Point>? _features;
    private BoundingBox[]? _featureExtents;

    /// <summary>
    /// Features in Web Mercator, or null while they are still loading. Null and empty are
    /// deliberately different: null means "not fetched yet", empty means "fetched, nothing there".
    /// </summary>
    public FeatureSet<Point>? Features
    {
        get => _features;
        set
        {
            _features = value;
            _featureExtents = null;
        }
    }

    /// <summary>
    /// Per-feature Web Mercator extents, positionally aligned with <c>Features.Features</c>; an
    /// empty or undecodable geometry gets <see cref="BoundingBox.NaN"/>.
    ///
    /// <para>Computed once per fetch and cached. <c>Geometry.GetBoundingBox()</c> runs
    /// <c>GetAllPoints()</c>, which allocates a flattened list of every vertex in the feature — so
    /// calling it per feature is an O(all vertices) allocation storm. Both the draw path (cull
    /// before projecting) and the click path (reject before hit-testing) need exactly this, on
    /// geometry that never changes after loading.</para>
    /// </summary>
    public BoundingBox[] FeatureExtents => _featureExtents ??= BuildFeatureExtents();

    private BoundingBox[] BuildFeatureExtents()
    {
        var features = _features?.Features;

        if (features is null || features.Count == 0)
            return [];

        var extents = new BoundingBox[features.Count];

        for (var i = 0; i < features.Count; i++)
        {
            var geometry = features[i].TheGeometry;

            extents[i] = geometry is null || geometry.IsNullOrEmpty()
                ? BoundingBox.NaN
                : geometry.GetBoundingBox();
        }

        return extents;
    }

    public bool IsVisible { get; set; }

    /// <summary>0..1, applied as canvas globalAlpha on top of any alpha in the colours.</summary>
    public double Opacity { get; set; } = 1;

    public int? MinZoomLevel { get; init; }

    public int? MaxZoomLevel { get; init; }

    public string? FillColor { get; init; }

    public string? StrokeColor { get; init; }

    public double StrokeThickness { get; init; } = 1;

    /// <summary>Drawing order — lower draws first, so higher numbers land on top.</summary>
    public int DrawOrder { get; init; }

    public bool IsWithinZoomRange(int zoomLevel) =>
        (MinZoomLevel is null || zoomLevel >= MinZoomLevel)
        && (MaxZoomLevel is null || zoomLevel <= MaxZoomLevel);

    /// <summary>Everything that must be true before this layer contributes a single pixel.</summary>
    public bool IsDrawnAt(int zoomLevel) =>
        IsVisible && Opacity > 0 && Features is not null && IsWithinZoomRange(zoomLevel);
}
