namespace IRI.Maptor.Jab.Maui.Layers;

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
}
