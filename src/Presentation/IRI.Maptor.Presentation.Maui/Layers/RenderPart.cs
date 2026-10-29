namespace IRI.Maptor.Presentation.Maui.Layers;

internal enum RenderKind
{
    Point,
    Line,
    Polygon,
}

/// <summary>
/// One drawable piece of a layer, with coordinates already projected to WebMercator.
/// A Point part holds a single ring of point coordinates; a Line part holds one or more
/// polylines; a Polygon part holds an exterior ring followed by any hole rings.
/// </summary>
internal sealed class RenderPart
{
    public RenderKind Kind { get; init; }

    public List<(double[] X, double[] Y)> Rings { get; init; } = new();

    /// <summary>The feature this part was built from, used by hit-testing. Null for parts without one.</summary>
    public MapFeature? Feature { get; init; }

    // WebMercator bounds of all rings, used to skip parts outside the view or away from a tap.
    public double XMin { get; init; }
    public double YMin { get; init; }
    public double XMax { get; init; }
    public double YMax { get; init; }

    public bool Intersects(double xMin, double yMin, double xMax, double yMax)
        => XMin <= xMax && XMax >= xMin && YMin <= yMax && YMax >= yMin;
}
