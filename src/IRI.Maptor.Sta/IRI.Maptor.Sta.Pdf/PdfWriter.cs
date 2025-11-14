using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using PdfSharpCore.Pdf;
using PdfSharpCore.Drawing;

namespace IRI.Maptor.Sta.Pdf;

/// <summary>
/// PDF writer for converting Geometry and Feature types to PDF format
/// </summary>
public static class PdfWriter
{
    private const double POINTS_PER_INCH = 72.0;
    private const double A4_WIDTH = 595.0;  // A4 width in points
    private const double A4_HEIGHT = 842.0;  // A4 height in points
    private const double LETTER_WIDTH = 612.0;  // Letter width in points
    private const double LETTER_HEIGHT = 792.0;  // Letter height in points

    /// <summary>
    /// Converts Geometry to PDF bytes
    /// </summary>
    public static byte[] Write(Geometry<Point> geometry, PdfOptions? options = null)
    {
        if (geometry == null)
            throw new ArgumentNullException(nameof(geometry));

        options ??= new PdfOptions();

        using var document = CreatePdfDocument(geometry, options);
        var page = document.AddPage();
        
        // Calculate and set page size
        var (pageWidth, pageHeight) = CalculatePageSize(geometry, options);
        page.Width = pageWidth;
        page.Height = pageHeight;
        
        if (!geometry.IsNullOrEmpty())
        {
            var gfx = XGraphics.FromPdfPage(page);
            WriteGeometry(gfx, geometry, options);
            gfx.Dispose();
        }

        using var stream = new MemoryStream();
        document.Save(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Converts Feature to PDF bytes
    /// </summary>
    public static byte[] Write(Feature<Point> feature, PdfOptions? options = null)
    {
        if (feature == null)
            throw new ArgumentNullException(nameof(feature));

        options ??= new PdfOptions();

        // Apply Feature attributes to options if enabled
        if (options.PreserveFeatureAttributes && feature.Attributes != null)
        {
            if (feature.Attributes.TryGetValue("Title", out var title) && title != null)
                options.Title = title.ToString();
            if (feature.Attributes.TryGetValue("Author", out var author) && author != null)
                options.Author = author.ToString();
            if (feature.Attributes.TryGetValue("Creator", out var creator) && creator != null)
                options.Creator = creator.ToString();
            if (feature.Attributes.TryGetValue("Subject", out var subject) && subject != null)
                options.Subject = subject.ToString();
            if (feature.Attributes.TryGetValue("Keywords", out var keywords) && keywords != null)
                options.Keywords = keywords.ToString();
        }

        return Write(feature.TheGeometry, options);
    }

    /// <summary>
    /// Writes Geometry to PDF file
    /// </summary>
    public static string WriteToFile(Geometry<Point> geometry, string filePath, PdfOptions? options = null)
    {
        var pdfBytes = Write(geometry, options);
        File.WriteAllBytes(filePath, pdfBytes);
        return filePath;
    }

    /// <summary>
    /// Writes Feature to PDF file
    /// </summary>
    public static string WriteToFile(Feature<Point> feature, string filePath, PdfOptions? options = null)
    {
        var pdfBytes = Write(feature, options);
        File.WriteAllBytes(filePath, pdfBytes);
        return filePath;
    }

    /// <summary>
    /// Creates PDF document with appropriate page size
    /// </summary>
    private static PdfDocument CreatePdfDocument(Geometry<Point> geometry, PdfOptions options)
    {
        var document = new PdfDocument();

        // Set document metadata
        document.Info.Title = options.Title ?? "PDF Document";
        document.Info.Author = options.Author ?? string.Empty;
        document.Info.Creator = options.Creator;
        document.Info.Subject = options.Subject ?? string.Empty;
        document.Info.Keywords = options.Keywords ?? string.Empty;

        return document;
    }

    /// <summary>
    /// Calculates page size based on geometry and options
    /// </summary>
    private static (double width, double height) CalculatePageSize(Geometry<Point> geometry, PdfOptions options)
    {
        switch (options.PageSize)
        {
            case PdfPageSize.Auto:
                if (geometry.IsNullOrEmpty())
                {
                    // Default size if geometry is empty
                    return (A4_WIDTH, A4_HEIGHT);
                }

                var bbox = geometry.GetBoundingBox();
                if (bbox.IsNaN() || !bbox.IsValid())
                {
                    return (A4_WIDTH, A4_HEIGHT);
                }

                var paddingX = bbox.Width * options.BoundingBoxPadding;
                var paddingY = bbox.Height * options.BoundingBoxPadding;

                var contentWidth = bbox.Width + (2 * paddingX);
                var contentHeight = bbox.Height + (2 * paddingY);

                // Ensure minimum size
                if (contentWidth < 100) contentWidth = 100;
                if (contentHeight < 100) contentHeight = 100;

                return (contentWidth, contentHeight);

            case PdfPageSize.A4:
                return options.PageOrientation == PdfPageOrientation.Landscape 
                    ? (A4_HEIGHT, A4_WIDTH) 
                    : (A4_WIDTH, A4_HEIGHT);

            case PdfPageSize.Letter:
                return options.PageOrientation == PdfPageOrientation.Landscape 
                    ? (LETTER_HEIGHT, LETTER_WIDTH) 
                    : (LETTER_WIDTH, LETTER_HEIGHT);

            case PdfPageSize.Custom:
                if (options.CustomPageWidth.HasValue && options.CustomPageHeight.HasValue)
                {
                    return (options.CustomPageWidth.Value, options.CustomPageHeight.Value);
                }
                // Fallback to A4 if custom size not specified
                return (A4_WIDTH, A4_HEIGHT);

            default:
                return (A4_WIDTH, A4_HEIGHT);
        }
    }

    /// <summary>
    /// Transforms geometry coordinates to PDF page coordinates
    /// </summary>
    private static XPoint TransformPoint(Point point, BoundingBox geometryBbox, double pageWidth, double pageHeight, PdfOptions options)
    {
        if (geometryBbox.IsNaN() || !geometryBbox.IsValid())
        {
            return new XPoint(0, 0);
        }

        var paddingX = geometryBbox.Width * options.BoundingBoxPadding;
        var paddingY = geometryBbox.Height * options.BoundingBoxPadding;

        // Calculate scale factors
        var contentWidth = geometryBbox.Width + (2 * paddingX);
        var contentHeight = geometryBbox.Height + (2 * paddingY);

        // Avoid division by zero
        if (contentWidth <= 0) contentWidth = 1;
        if (contentHeight <= 0) contentHeight = 1;

        var scaleX = pageWidth / contentWidth;
        var scaleY = pageHeight / contentHeight;
        var scale = Math.Min(scaleX, scaleY); // Maintain aspect ratio

        // Transform coordinates
        // PDF uses bottom-left origin, so we need to flip Y
        var x = (point.X - geometryBbox.XMin + paddingX) * scale;
        var y = pageHeight - ((point.Y - geometryBbox.YMin + paddingY) * scale);

        return new XPoint(x, y);
    }

    /// <summary>
    /// Writes geometry to PDF page
    /// </summary>
    private static void WriteGeometry(XGraphics gfx, Geometry<Point> geometry, PdfOptions options)
    {
        if (geometry.IsNullOrEmpty())
            return;

        var page = gfx.PdfPage;
        var pageWidth = page.Width;
        var pageHeight = page.Height;

        var bbox = geometry.GetBoundingBox();
        
        // Handle invalid bounding box
        if (bbox.IsNaN() || !bbox.IsValid())
        {
            return;
        }

        switch (geometry.Type)
        {
            case GeometryType.Point:
                WritePoint(gfx, geometry, bbox, pageWidth, pageHeight, options);
                break;

            case GeometryType.LineString:
                WriteLineString(gfx, geometry, bbox, pageWidth, pageHeight, options);
                break;

            case GeometryType.Polygon:
                WritePolygon(gfx, geometry, bbox, pageWidth, pageHeight, options);
                break;

            case GeometryType.MultiPoint:
                WriteMultiPoint(gfx, geometry, bbox, pageWidth, pageHeight, options);
                break;

            case GeometryType.MultiLineString:
                WriteMultiLineString(gfx, geometry, bbox, pageWidth, pageHeight, options);
                break;

            case GeometryType.MultiPolygon:
                WriteMultiPolygon(gfx, geometry, bbox, pageWidth, pageHeight, options);
                break;

            case GeometryType.GeometryCollection:
                WriteGeometryCollection(gfx, geometry, bbox, pageWidth, pageHeight, options);
                break;

            default:
                throw new NotImplementedException($"Geometry type {geometry.Type} is not supported for PDF export");
        }
    }

    /// <summary>
    /// Writes Point as circle
    /// </summary>
    private static void WritePoint(XGraphics gfx, Geometry<Point> geometry, BoundingBox bbox, double pageWidth, double pageHeight, PdfOptions options)
    {
        if (geometry.Points == null || geometry.Points.Count == 0)
            return;

        var point = geometry.Points[0];
        var pdfPoint = TransformPoint(point, bbox, pageWidth, pageHeight, options);
        var radius = options.PointCircleRadius;

        ApplyStyling(gfx, options, isPolygon: false);

        // Draw circle for point
        gfx.DrawEllipse(GetPen(options), pdfPoint.X - radius, pdfPoint.Y - radius, radius * 2, radius * 2);
        
        // Fill if fill color is specified
        if (options.FillColor.HasValue)
        {
            gfx.DrawEllipse(GetBrush(options), pdfPoint.X - radius, pdfPoint.Y - radius, radius * 2, radius * 2);
        }
    }

    /// <summary>
    /// Writes LineString as lines
    /// </summary>
    private static void WriteLineString(XGraphics gfx, Geometry<Point> geometry, BoundingBox bbox, double pageWidth, double pageHeight, PdfOptions options)
    {
        if (geometry.Points == null || geometry.Points.Count < 2)
            return;

        var pdfPoints = geometry.Points.Select(p => TransformPoint(p, bbox, pageWidth, pageHeight, options)).ToArray();

        ApplyStyling(gfx, options, isPolygon: false);
        gfx.DrawLines(GetPen(options), pdfPoints);
    }

    /// <summary>
    /// Writes Polygon with fill and stroke
    /// </summary>
    private static void WritePolygon(XGraphics gfx, Geometry<Point> geometry, BoundingBox bbox, double pageWidth, double pageHeight, PdfOptions options)
    {
        if (geometry.Geometries == null || geometry.Geometries.Count == 0)
            return;

        // Write exterior ring
        var exteriorRing = geometry.Geometries[0];
        if (exteriorRing.Points == null || exteriorRing.Points.Count < 3)
            return;

        var exteriorPoints = exteriorRing.Points.Select(p => TransformPoint(p, bbox, pageWidth, pageHeight, options)).ToArray();

        ApplyStyling(gfx, options, isPolygon: true);

        // Fill polygon if fill color is specified
        if (options.FillColor.HasValue)
        {
            gfx.DrawPolygon(GetBrush(options), exteriorPoints, XFillMode.Alternate);
        }

        // Draw polygon outline
        gfx.DrawPolygon(GetPen(options), exteriorPoints);

        // Draw interior rings (holes) if any
        for (int ringIndex = 1; ringIndex < geometry.Geometries.Count; ringIndex++)
        {
            var ring = geometry.Geometries[ringIndex];
            if (ring.Points != null && ring.Points.Count > 0)
            {
                var holePoints = ring.Points.Select(p => TransformPoint(p, bbox, pageWidth, pageHeight, options)).ToArray();
                
                // Fill hole (with background color or no fill)
                if (options.FillColor.HasValue)
                {
                    gfx.DrawPolygon(GetBrush(options), holePoints, XFillMode.Alternate);
                }
                
                // Draw hole outline
                gfx.DrawPolygon(GetPen(options), holePoints);
            }
        }
    }

    /// <summary>
    /// Writes MultiPoint
    /// </summary>
    private static void WriteMultiPoint(XGraphics gfx, Geometry<Point> geometry, BoundingBox bbox, double pageWidth, double pageHeight, PdfOptions options)
    {
        if (geometry.Geometries == null || geometry.Geometries.Count == 0)
            return;

        foreach (var pointGeo in geometry.Geometries)
        {
            WritePoint(gfx, pointGeo, bbox, pageWidth, pageHeight, options);
        }
    }

    /// <summary>
    /// Writes MultiLineString
    /// </summary>
    private static void WriteMultiLineString(XGraphics gfx, Geometry<Point> geometry, BoundingBox bbox, double pageWidth, double pageHeight, PdfOptions options)
    {
        if (geometry.Geometries == null || geometry.Geometries.Count == 0)
            return;

        foreach (var lineGeo in geometry.Geometries)
        {
            WriteLineString(gfx, lineGeo, bbox, pageWidth, pageHeight, options);
        }
    }

    /// <summary>
    /// Writes MultiPolygon
    /// </summary>
    private static void WriteMultiPolygon(XGraphics gfx, Geometry<Point> geometry, BoundingBox bbox, double pageWidth, double pageHeight, PdfOptions options)
    {
        if (geometry.Geometries == null || geometry.Geometries.Count == 0)
            return;

        foreach (var polygonGeo in geometry.Geometries)
        {
            WritePolygon(gfx, polygonGeo, bbox, pageWidth, pageHeight, options);
        }
    }

    /// <summary>
    /// Writes GeometryCollection
    /// </summary>
    private static void WriteGeometryCollection(XGraphics gfx, Geometry<Point> geometry, BoundingBox bbox, double pageWidth, double pageHeight, PdfOptions options)
    {
        if (geometry.Geometries == null || geometry.Geometries.Count == 0)
            return;

        foreach (var subGeometry in geometry.Geometries)
        {
            WriteGeometry(gfx, subGeometry, options);
        }
    }

    /// <summary>
    /// Applies styling (stroke color, fill color, line width)
    /// </summary>
    private static void ApplyStyling(XGraphics gfx, PdfOptions options, bool isPolygon)
    {
        // Styling is applied via Pen and Brush objects
        // This method is called before drawing operations
    }

    /// <summary>
    /// Gets pen for drawing strokes
    /// </summary>
    private static XPen GetPen(PdfOptions options)
    {
        var color = options.GetStrokeColor() ?? XColors.Black;
        return new XPen(color, options.StrokeWidth);
    }

    /// <summary>
    /// Gets brush for filling
    /// </summary>
    private static XBrush GetBrush(PdfOptions options)
    {
        var color = options.GetFillColor() ?? XColors.Black;
        return new XSolidBrush(color);
    }
}

