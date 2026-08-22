using IRI.Maptor.Core.Common.Primitives;

namespace IRI.Maptor.Core.Pdf;

/// <summary>
/// A reusable point-marker template stamped at each point feature, reproducing the map's
/// on-screen symbol. Either a set of vector figures (flattened from the WPF GeometrySymbol)
/// or a raster image (from an ImageSymbol). Coordinates are in PDF points, centered on the
/// marker origin; the marker is drawn at a fixed paper size (not scaled with the map).
/// </summary>
public class PdfPointMarker
{
    /// <summary>
    /// Vector outline(s) of the marker, centered on the origin. Preferred when present.
    /// </summary>
    public List<PdfMarkerFigure>? Figures { get; set; }

    /// <summary>
    /// Raster marker image (PNG), drawn centered on the point at <see cref="Width"/>×<see cref="Height"/> points.
    /// </summary>
    public byte[]? ImagePngBytes { get; set; }

    public double Width { get; set; }

    public double Height { get; set; }

    public bool HasVector => Figures is { Count: > 0 };

    public bool HasImage => ImagePngBytes is { Length: > 0 } && Width > 0 && Height > 0;
}

/// <summary>
/// One sub-path of a vector marker; points are in PDF points, centered on the marker origin.
/// </summary>
public class PdfMarkerFigure
{
    public List<Point> Points { get; set; } = new();

    public bool IsClosed { get; set; }

    public bool IsFilled { get; set; }
}