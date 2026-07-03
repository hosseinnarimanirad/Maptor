using IRI.Maptor.Sta.Common.Primitives;
using PdfSharpCore.Drawing;

namespace IRI.Maptor.Sta.Pdf;

/// <summary>
/// Maps web-mercator map coordinates onto a rectangular frame of a PDF page
/// (aspect-preserving min-fit, centered, Y flipped for PDF's bottom-left origin).
/// With the frame covering the whole page this is equivalent to the classic
/// full-bleed export transform.
/// </summary>
internal readonly struct MapPageTransform
{
    /// <summary>
    /// Page points per map unit
    /// </summary>
    public double Scale { get; }

    public double OffsetX { get; }

    /// <summary>
    /// Offset of the scaled extent's bottom edge, measured from the page bottom
    /// </summary>
    public double OffsetYFromBottom { get; }

    public double PageHeight { get; }

    public BoundingBox MapExtent { get; }

    private MapPageTransform(double scale, double offsetX, double offsetYFromBottom, double pageHeight, BoundingBox mapExtent)
    {
        Scale = scale;
        OffsetX = offsetX;
        OffsetYFromBottom = offsetYFromBottom;
        PageHeight = pageHeight;
        MapExtent = mapExtent;
    }

    /// <summary>
    /// Fits <paramref name="mapExtent"/> (inflated by <paramref name="paddingRatio"/>) into the
    /// given frame. Frame coordinates use the page's top-left origin (XGraphics convention).
    /// </summary>
    public static MapPageTransform Create(
        BoundingBox mapExtent,
        double frameX,
        double frameY,
        double frameWidth,
        double frameHeight,
        double paddingRatio,
        double pageHeight)
    {
        var contentWidth = mapExtent.Width * (1 + 2 * paddingRatio);
        var contentHeight = mapExtent.Height * (1 + 2 * paddingRatio);

        if (contentWidth <= 0) contentWidth = 1;
        if (contentHeight <= 0) contentHeight = 1;

        var scale = Math.Min(frameWidth / contentWidth, frameHeight / contentHeight);

        var scaledWidth = mapExtent.Width * scale;
        var scaledHeight = mapExtent.Height * scale;

        var offsetX = frameX + (frameWidth - scaledWidth) / 2.0;
        var offsetYFromBottom = (pageHeight - frameY - frameHeight) + (frameHeight - scaledHeight) / 2.0;

        return new MapPageTransform(scale, offsetX, offsetYFromBottom, pageHeight, mapExtent);
    }

    public static MapPageTransform CreateFullPage(BoundingBox mapExtent, double pageWidth, double pageHeight, double paddingRatio)
        => Create(mapExtent, 0, 0, pageWidth, pageHeight, paddingRatio, pageHeight);

    public XPoint ToPage(Point point)
    {
        var x = (point.X - MapExtent.XMin) * Scale + OffsetX;
        var y = PageHeight - ((point.Y - MapExtent.YMin) * Scale + OffsetYFromBottom);
        return new XPoint(x, y);
    }

    public XRect ToPage(BoundingBox extent)
    {
        var x1 = (extent.XMin - MapExtent.XMin) * Scale + OffsetX;
        var x2 = (extent.XMax - MapExtent.XMin) * Scale + OffsetX;
        var y1 = PageHeight - ((extent.YMin - MapExtent.YMin) * Scale + OffsetYFromBottom);
        var y2 = PageHeight - ((extent.YMax - MapExtent.YMin) * Scale + OffsetYFromBottom);

        return new XRect(
            Math.Min(x1, x2),
            Math.Min(y1, y2),
            Math.Abs(x2 - x1),
            Math.Abs(y2 - y1));
    }
}