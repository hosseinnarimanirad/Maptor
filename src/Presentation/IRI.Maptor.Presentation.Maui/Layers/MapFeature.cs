using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;

using Point = IRI.Maptor.Core.Common.Primitives.Point;

namespace IRI.Maptor.Presentation.Maui.Layers;

/// <summary>
/// A feature of a <see cref="MapLayer"/>: its id and attributes, kept beside the drawn geometry so a
/// tap on the map (<see cref="Controls.MapViewer.HitTest"/>) can say what is under the finger.
/// </summary>
public sealed class MapFeature
{
    private static readonly IReadOnlyDictionary<string, object?> NoAttributes = new Dictionary<string, object?>();

    internal MapFeature(int id, Guid key, IReadOnlyDictionary<string, object?>? attributes)
    {
        Id = id;
        Key = key;
        Attributes = attributes ?? NoAttributes;
    }

    internal static MapFeature From(Feature<Point> feature)
        // Same dictionary instance, only seen as nullable values: no copy for every feature of a view.
        => new(feature.Id, feature.Key, (IReadOnlyDictionary<string, object?>?)(object?)feature.Attributes);

    public int Id { get; }

    public Guid Key { get; }

    /// <summary>The attribute values by field name, as the source delivered them.</summary>
    public IReadOnlyDictionary<string, object?> Attributes { get; }

    /// <summary>WebMercator bounds of the feature's geometry; null while the feature has no geometry.</summary>
    public BoundingBox? Extent { get; internal set; }

    // The drawable pieces of this feature, reused to highlight it (MapGraphic.FromFeature).
    internal List<RenderPart> Parts { get; } = new();
}
