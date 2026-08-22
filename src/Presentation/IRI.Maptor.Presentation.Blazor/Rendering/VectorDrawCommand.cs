namespace IRI.Maptor.Presentation.Blazor.Rendering;

/// <summary>
/// One layer's worth of geometry, already projected to screen pixels, as a single interop payload.
///
/// Paths are FLAT coordinate arrays ([x0,y0,x1,y1,…]) rather than arrays of point objects: the
/// JSON marshaling between WASM and JS is the bottleneck in a Blazor rendering loop, and a flat
/// double[] serialises to a bare number list instead of one object per vertex. A layer with 20k
/// vertices is the difference between one compact array and 20k tiny objects.
/// </summary>
public sealed class VectorDrawCommand
{
    public required string Key { get; init; }

    /// <summary>"point", "polyline" or "polygon" — tells the canvas how to stroke/fill the paths.</summary>
    public required string Kind { get; init; }

    public string Fill { get; init; } = "transparent";

    public string Stroke { get; init; } = "transparent";

    public double LineWidth { get; init; } = 1;

    /// <summary>Layer opacity, applied as globalAlpha over whatever alpha the colours carry.</summary>
    public double Alpha { get; init; } = 1;

    /// <summary>Radius in pixels for "point" kind; ignored otherwise.</summary>
    public double PointRadius { get; init; } = 4;

    public required IReadOnlyList<double[]> Paths { get; init; }
}
