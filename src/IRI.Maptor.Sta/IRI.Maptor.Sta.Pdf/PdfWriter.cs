using System.Diagnostics;
using System.IO;
using System.Linq;
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.Advanced;
using PdfSharpCore.Drawing;
using static PdfSharpCore.Pdf.PdfDictionary;

namespace IRI.Maptor.Sta.Pdf;

/// <summary>
/// PDF writer for converting Geometry and Feature types to PDF format
/// </summary>
public static class PdfWriter
{
    /// <summary>
    /// Data structure for layer information when writing multiple layers to PDF
    /// </summary>
    public class LayerPdfData
    {
        public List<Feature<Point>> Features { get; set; } = new();
        public PdfOptions Options { get; set; } = new();
        public int ZIndex { get; set; }
        public double Opacity { get; set; } = 1.0;
        public string LayerName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Data structure for raster tile information when writing raster layers to PDF
    /// </summary>
    public class RasterTileData
    {
        public byte[] ImageBytes { get; set; } = Array.Empty<byte>();
        public BoundingBox WebMercatorExtent { get; set; }
        public double Opacity { get; set; } = 1.0;
    }

    /// <summary>
    /// Data structure for raster layer information when writing raster layers to PDF
    /// </summary>
    public class RasterLayerPdfData
    {
        public List<RasterTileData> Tiles { get; set; } = new();
        public int ZIndex { get; set; }
        public double Opacity { get; set; } = 1.0;
        public string LayerName { get; set; } = string.Empty;
    }
    private const double POINTS_PER_INCH = 72.0;
    private const double A4_WIDTH = 595.0;  // A4 width in points
    private const double A4_HEIGHT = 842.0;  // A4 height in points
    private const double A3_WIDTH = 842.0;  // A3 width in points
    private const double A3_HEIGHT = 1191.0;  // A3 height in points
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

        // Fill if a non-transparent fill is specified.
        var brush = GetBrush(options);
        if (brush != null)
        {
            gfx.DrawEllipse(brush, pdfPoint.X - radius, pdfPoint.Y - radius, radius * 2, radius * 2);
        }

        // Draw circle outline if the stroke is not transparent.
        var pen = GetPen(options);
        if (pen != null)
        {
            gfx.DrawEllipse(pen, pdfPoint.X - radius, pdfPoint.Y - radius, radius * 2, radius * 2);
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

        var pen = GetPen(options);
        if (pen != null)
            gfx.DrawLines(pen, pdfPoints);
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

        var brush = GetBrush(options);
        var pen = GetPen(options);

        // Fill polygon only if a non-transparent fill is specified.
        if (brush != null)
        {
            gfx.DrawPolygon(brush, exteriorPoints, XFillMode.Alternate);
        }

        // Draw polygon outline only if the stroke is not transparent.
        if (pen != null)
        {
            gfx.DrawPolygon(pen, exteriorPoints);
        }

        // Draw interior rings (holes) if any
        for (int ringIndex = 1; ringIndex < geometry.Geometries.Count; ringIndex++)
        {
            var ring = geometry.Geometries[ringIndex];
            if (ring.Points != null && ring.Points.Count > 0)
            {
                var holePoints = ring.Points.Select(p => TransformPoint(p, bbox, pageWidth, pageHeight, options)).ToArray();

                if (brush != null)
                {
                    gfx.DrawPolygon(brush, holePoints, XFillMode.Alternate);
                }

                if (pen != null)
                {
                    gfx.DrawPolygon(pen, holePoints);
                }
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
    /// Gets the pen for drawing strokes, or null when the stroke is fully transparent
    /// (so a transparent stroke is never painted as a visible/white outline).
    /// When no stroke color is specified, a default black pen is used.
    /// </summary>
    private static XPen? GetPen(PdfOptions options)
    {
        var color = options.GetStrokeColor();
        if (color.HasValue)
        {
            // Explicitly transparent stroke => no outline.
            if (color.Value.A <= 0)
                return null;

            return new XPen(color.Value, options.StrokeWidth);
        }

        // No stroke color specified: keep the default black outline.
        return new XPen(XColors.Black, options.StrokeWidth);
    }

    /// <summary>
    /// Gets the brush for filling, or null when there is no fill color or the fill is fully
    /// transparent. A transparent fill must NOT be painted (never as white).
    /// </summary>
    private static XBrush? GetBrush(PdfOptions options)
    {
        var color = options.GetFillColor();
        if (!color.HasValue || color.Value.A <= 0)
            return null;

        return new XSolidBrush(color.Value);
    }

    /// <summary>
    /// Writes multiple layers to PDF with their symbology
    /// </summary>
    public static byte[] WriteLayers(
        List<LayerPdfData> layers,
        BoundingBox mapExtent,
        double mapScale,
        PdfOptions? baseOptions = null,
        List<RasterLayerPdfData>? rasterLayers = null,
        bool supportPdfLayers = true,
        PdfMapDecorations? decorations = null)
    {
        if (layers == null)
            layers = new List<LayerPdfData>();

        if (mapExtent.IsNaN() || !mapExtent.IsValid())
            throw new ArgumentException("Map extent must be valid", nameof(mapExtent));

        // Check if we have any layers to export (raster or vector)
        if ((layers.Count == 0) && (rasterLayers == null || rasterLayers.Count == 0))
            throw new ArgumentException("At least one layer (vector or raster) must be provided", nameof(layers));

        baseOptions ??= new PdfOptions();

        var isDecorated = decorations?.HasAny == true;

        var (pageWidth, pageHeight) = ComputeLayersPageSize(baseOptions, mapExtent, isDecorated);

        // Create PDF document
        var document = new PdfDocument();
        document.Info.Title = baseOptions.Title ?? "Map Export";
        document.Info.Author = baseOptions.Author ?? string.Empty;
        document.Info.Creator = baseOptions.Creator;
        document.Info.Subject = baseOptions.Subject ?? string.Empty;
        document.Info.Keywords = baseOptions.Keywords ?? string.Empty;

        var page = document.AddPage();
        page.Width = pageWidth;
        page.Height = pageHeight;

        // Group layers into render units; each unit becomes one toggleable PDF layer.
        var units = BuildRenderUnits(layers, rasterLayers, GroupByLayerName);

        // Decorated exports render the map into the layout's map frame (and clip to it);
        // plain exports keep the classic full-bleed page.
        PdfMapLayout layout = default;
        MapPageTransform pageTransform;

        if (isDecorated)
        {
            layout = PdfMapLayout.Create(pageWidth, pageHeight, decorations!);
            var frame = layout.MapFrameRect;
            pageTransform = MapPageTransform.Create(mapExtent, frame.X, frame.Y, frame.Width, frame.Height, baseOptions.BoundingBoxPadding, pageHeight);
        }
        else
        {
            pageTransform = MapPageTransform.CreateFullPage(mapExtent, pageWidth, pageHeight, baseOptions.BoundingBoxPadding);
        }

        void DrawClippedUnit(XGraphics target, RenderUnit unit)
        {
            if (isDecorated)
            {
                target.Save();
                target.IntersectClip(layout.MapFrameRect);
            }

            DrawUnit(target, unit, pageTransform, baseOptions);

            if (isDecorated)
                target.Restore();
        }

        if (supportPdfLayers)
        {
            // Draw each layer into its own appended content stream, then wrap that finalized
            // stream in marked content (/OC /ocN BDC ... EMC) bound to an Optional Content Group,
            // so PDF viewers can toggle the layer. Each layer uses its own XGraphics, disposed
            // before the next, so its content stream is complete before we wrap its bytes.
            for (int i = 0; i < units.Count; i++)
            {
                var unit = units[i];

                var ocgRef = CreatePdfLayer(document, unit.LayerName);
                var propertyName = RegisterOptionalContentProperty(page, ocgRef, i);

                var existingContents = SnapshotContentStreams(page);
                using (var layerGfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append))
                {
                    DrawClippedUnit(layerGfx, unit);
                }
                WrapNewContentStreamsWithOptionalContent(page, existingContents, propertyName);
            }
        }
        else
        {
            // Non-layered path: draw everything directly on the page.
            using var gfx = XGraphics.FromPdfPage(page);
            foreach (var unit in units)
            {
                DrawClippedUnit(gfx, unit);
            }
        }

        if (isDecorated)
        {
            // Appended after (and outside) the per-layer OCG streams, so decorations stay
            // visible whatever layers the viewer toggles off.
            using var decorationGfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);
            PdfMapComposer.DrawDecorations(decorationGfx, layout, pageTransform, decorations!);
        }

        using var stream = new MemoryStream();
        document.Save(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Expands the extent so it fills the decorated layout's map frame edge-to-edge
    /// (no letterboxing inside the neat line). Call it before fetching tiles/features
    /// so the whole printed frame has content, and pass the result to WriteLayers.
    /// </summary>
    public static BoundingBox ComputeDecoratedMapExtent(BoundingBox mapExtent, PdfOptions options, PdfMapDecorations decorations)
    {
        var (pageWidth, pageHeight) = ComputeLayersPageSize(options, mapExtent, isDecorated: true);

        var frame = PdfMapLayout.Create(pageWidth, pageHeight, decorations).MapFrameRect;

        var frameAspect = frame.Width / frame.Height;
        var extentAspect = mapExtent.Width / mapExtent.Height;

        if (extentAspect < frameAspect)
            return new BoundingBox(mapExtent.Center, mapExtent.Height * frameAspect, mapExtent.Height);

        return new BoundingBox(mapExtent.Center, mapExtent.Width, mapExtent.Width / frameAspect);
    }

    /// <summary>
    /// Page size for WriteLayers: standard sizes honor orientation; Auto derives the page
    /// from the extent aspect ratio (disallowed for decorated exports, where the frame must
    /// be predictable — it falls back to A4 oriented by the extent).
    /// </summary>
    private static (double PageWidth, double PageHeight) ComputeLayersPageSize(PdfOptions baseOptions, BoundingBox mapExtent, bool isDecorated)
    {
        double pageWidth, pageHeight;

        if (baseOptions.PageSize == PdfPageSize.Auto && isDecorated)
        {
            var landscape = mapExtent.Width > mapExtent.Height;
            pageWidth = landscape ? A4_HEIGHT : A4_WIDTH;
            pageHeight = landscape ? A4_WIDTH : A4_HEIGHT;
        }
        else if (baseOptions.PageSize == PdfPageSize.Auto)
        {
            // Calculate page size based on map extent aspect ratio
            var aspectRatio = mapExtent.Width / mapExtent.Height;

            // Use A4 as base, but adjust to match aspect ratio
            var baseWidth = A4_WIDTH;
            var baseHeight = A4_HEIGHT;

            pageWidth = Math.Max(baseWidth, baseHeight * aspectRatio);
            pageHeight = Math.Max(baseHeight, baseWidth / aspectRatio);

            // Ensure reasonable limits (PDF max is typically around 14,400 points)
            pageWidth = Math.Min(pageWidth, 14400);
            pageHeight = Math.Min(pageHeight, 14400);

            // Ensure minimum size
            pageWidth = Math.Max(pageWidth, 100);
            pageHeight = Math.Max(pageHeight, 100);
        }
        else
        {
            // Use standard page size
            switch (baseOptions.PageSize)
            {
                case PdfPageSize.A4:
                    pageWidth = baseOptions.PageOrientation == PdfPageOrientation.Landscape ? A4_HEIGHT : A4_WIDTH;
                    pageHeight = baseOptions.PageOrientation == PdfPageOrientation.Landscape ? A4_WIDTH : A4_HEIGHT;
                    break;
                case PdfPageSize.A3:
                    pageWidth = baseOptions.PageOrientation == PdfPageOrientation.Landscape ? A3_HEIGHT : A3_WIDTH;
                    pageHeight = baseOptions.PageOrientation == PdfPageOrientation.Landscape ? A3_WIDTH : A3_HEIGHT;
                    break;
                case PdfPageSize.Letter:
                    pageWidth = baseOptions.PageOrientation == PdfPageOrientation.Landscape ? LETTER_HEIGHT : LETTER_WIDTH;
                    pageHeight = baseOptions.PageOrientation == PdfPageOrientation.Landscape ? LETTER_WIDTH : LETTER_HEIGHT;
                    break;
                case PdfPageSize.Custom:
                    pageWidth = baseOptions.CustomPageWidth ?? A4_WIDTH;
                    pageHeight = baseOptions.CustomPageHeight ?? A4_HEIGHT;
                    break;
                default:
                    pageWidth = A4_WIDTH;
                    pageHeight = A4_HEIGHT;
                    break;
            }
        }

        return (pageWidth, pageHeight);
    }

    /// <summary>
    /// Writes geometry to PDF page using map extent transformation
    /// </summary>
    private static void WriteGeometryForLayer(
        XGraphics gfx,
        Geometry<Point> geometry,
        in MapPageTransform pageTransform,
        PdfOptions options)
    {
        if (geometry.IsNullOrEmpty())
            return;

        var transform = pageTransform;

        // Transform point from map coordinates to PDF coordinates
        XPoint TransformPoint(Point point) => transform.ToPage(point);

        // Write geometry based on type
        switch (geometry.Type)
        {
            case GeometryType.Point:
                WritePointForLayer(gfx, geometry, TransformPoint, options);
                break;

            case GeometryType.LineString:
                WriteLineStringForLayer(gfx, geometry, TransformPoint, options);
                break;

            case GeometryType.Polygon:
                WritePolygonForLayer(gfx, geometry, TransformPoint, options);
                break;

            case GeometryType.MultiPoint:
                if (geometry.Geometries != null)
                {
                    foreach (var pointGeo in geometry.Geometries)
                    {
                        WritePointForLayer(gfx, pointGeo, TransformPoint, options);
                    }
                }
                break;

            case GeometryType.MultiLineString:
                if (geometry.Geometries != null)
                {
                    foreach (var lineGeo in geometry.Geometries)
                    {
                        WriteLineStringForLayer(gfx, lineGeo, TransformPoint, options);
                    }
                }
                break;

            case GeometryType.MultiPolygon:
                if (geometry.Geometries != null)
                {
                    foreach (var polygonGeo in geometry.Geometries)
                    {
                        WritePolygonForLayer(gfx, polygonGeo, TransformPoint, options);
                    }
                }
                break;

            case GeometryType.GeometryCollection:
                if (geometry.Geometries != null)
                {
                    foreach (var subGeometry in geometry.Geometries)
                    {
                        WriteGeometryForLayer(gfx, subGeometry, pageTransform, options);
                    }
                }
                break;
        }
    }

    private static void WritePointForLayer(XGraphics gfx, Geometry<Point> geometry, Func<Point, XPoint> transform, PdfOptions options)
    {
        if (geometry.Points == null || geometry.Points.Count == 0)
            return;

        var point = geometry.Points[0];
        var pdfPoint = transform(point);
        var radius = options.PointCircleRadius;

        // Fill first, then outline. Skip whichever is transparent (never paint white).
        var brush = GetBrush(options);
        if (brush != null)
        {
            gfx.DrawEllipse(brush, pdfPoint.X - radius, pdfPoint.Y - radius, radius * 2, radius * 2);
        }

        var pen = GetPen(options);
        if (pen != null)
        {
            gfx.DrawEllipse(pen, pdfPoint.X - radius, pdfPoint.Y - radius, radius * 2, radius * 2);
        }
    }

    private static void WriteLineStringForLayer(XGraphics gfx, Geometry<Point> geometry, Func<Point, XPoint> transform, PdfOptions options)
    {
        if (geometry.Points == null || geometry.Points.Count < 2)
            return;

        var pdfPoints = geometry.Points.Select(transform).ToArray();
        var pen = GetPen(options);
        if (pen != null)
            gfx.DrawLines(pen, pdfPoints);
    }

    private static void WritePolygonForLayer(XGraphics gfx, Geometry<Point> geometry, Func<Point, XPoint> transform, PdfOptions options)
    {
        if (geometry.Geometries == null || geometry.Geometries.Count == 0)
            return;

        // Write exterior ring
        var exteriorRing = geometry.Geometries[0];
        if (exteriorRing.Points == null || exteriorRing.Points.Count < 3)
            return;

        var exteriorPoints = exteriorRing.Points.Select(transform).ToArray();

        var brush = GetBrush(options);
        var pen = GetPen(options);

        // Fill polygon only if a non-transparent fill is specified.
        if (brush != null)
        {
            gfx.DrawPolygon(brush, exteriorPoints, XFillMode.Alternate);
        }

        // Draw polygon outline only if the stroke is not transparent.
        if (pen != null)
        {
            gfx.DrawPolygon(pen, exteriorPoints);
        }

        // Draw interior rings (holes) if any
        for (int ringIndex = 1; ringIndex < geometry.Geometries.Count; ringIndex++)
        {
            var ring = geometry.Geometries[ringIndex];
            if (ring.Points != null && ring.Points.Count > 0)
            {
                var holePoints = ring.Points.Select(transform).ToArray();

                if (brush != null)
                {
                    gfx.DrawPolygon(brush, holePoints, XFillMode.Alternate);
                }

                if (pen != null)
                {
                    gfx.DrawPolygon(pen, holePoints);
                }
            }
        }
    }

    /// <summary>
    /// Writes raster tiles to PDF page using map extent transformation
    /// </summary>
    private static void WriteRasterTilesForLayer(
        XGraphics gfx,
        List<RasterTileData> tiles,
        in MapPageTransform pageTransform,
        PdfOptions baseOptions,
        double layerOpacity)
    {
        if (tiles == null || tiles.Count == 0)
            return;

        var transform = pageTransform;

        // Transform tile extent from map coordinates to PDF coordinates
        XRect TransformExtent(BoundingBox tileExtent) => transform.ToPage(tileExtent);

        // Draw each tile
        foreach (var tile in tiles)
        {
            try
            {
                if (tile.ImageBytes == null || tile.ImageBytes.Length == 0)
                    continue;

                // Convert byte[] to XImage
                using var stream = new MemoryStream(tile.ImageBytes, 0, tile.ImageBytes.Length, false, true);
                stream.Position = 0; // Reset position
                var xImage = XImage.FromStream(() => stream);

                // Transform tile extent to PDF coordinates
                var rect = TransformExtent(tile.WebMercatorExtent);

                // Apply opacity (note: PdfSharpCore doesn't directly support opacity in DrawImage,
                // but we can use a workaround with XGraphicsState if needed)
                // For now, draw the image - opacity is typically handled at the layer level
                gfx.DrawImage(xImage, rect);

                xImage.Dispose();
            }
            catch (Exception)
            {
                // Skip invalid tiles - continue with next tile
            }
        }
    }

    /// <summary>
    /// Default grouping policy: true => one OCG per map layer (grouped by LayerName);
    /// false => one OCG per symbolizer/raster entry.
    /// </summary>
    private const bool GroupByLayerName = true;

    /// <summary>
    /// A single toggleable PDF layer, aggregating all the input entries (raster + vector
    /// symbolizers) that belong to the same map layer.
    /// </summary>
    private sealed class RenderUnit
    {
        public string LayerName = string.Empty;
        public int OrderZIndex;
        public int FirstIndex;
        public List<RasterLayerPdfData> Rasters = new();
        public List<LayerPdfData> Vectors = new();

        public bool HasContent =>
            Rasters.Any(r => r.Tiles != null && r.Tiles.Count > 0) ||
            Vectors.Any(v => v.Features != null && v.Features.Count > 0);
    }

    /// <summary>
    /// Groups raster and vector layer entries into render units (one per toggleable PDF layer),
    /// preserving draw order by ZIndex and skipping units that have nothing to draw.
    /// </summary>
    private static List<RenderUnit> BuildRenderUnits(
        List<LayerPdfData> vectors,
        List<RasterLayerPdfData>? rasters,
        bool groupByLayerName)
    {
        // Flatten to a common list, preserving input order for stable tie-breaking.
        var items = new List<(int ZIndex, int Index, bool IsRaster, string Name, object Data)>();
        int index = 0;

        if (rasters != null)
        {
            foreach (var r in rasters)
            {
                var name = string.IsNullOrWhiteSpace(r.LayerName) ? "Raster Layer" : r.LayerName;
                items.Add((r.ZIndex, index++, true, name, r));
            }
        }

        foreach (var v in vectors)
        {
            var name = string.IsNullOrWhiteSpace(v.LayerName) ? "Vector Layer" : v.LayerName;
            items.Add((v.ZIndex, index++, false, name, v));
        }

        var units = new List<RenderUnit>();
        var byKey = new Dictionary<string, RenderUnit>();

        foreach (var item in items)
        {
            // Group key: by layer name, or unique per item to disable grouping.
            var key = groupByLayerName ? item.Name : ("__" + item.Index);

            if (!byKey.TryGetValue(key, out var unit))
            {
                unit = new RenderUnit
                {
                    LayerName = item.Name,
                    OrderZIndex = item.ZIndex,
                    FirstIndex = item.Index
                };
                byKey[key] = unit;
                units.Add(unit);
            }
            else if (item.ZIndex < unit.OrderZIndex)
            {
                unit.OrderZIndex = item.ZIndex;
            }

            if (item.IsRaster)
                unit.Rasters.Add((RasterLayerPdfData)item.Data);
            else
                unit.Vectors.Add((LayerPdfData)item.Data);
        }

        // Keep only units with drawable content; order by ZIndex then first appearance.
        return units
            .Where(u => u.HasContent)
            .OrderBy(u => u.OrderZIndex)
            .ThenBy(u => u.FirstIndex)
            .ToList();
    }

    /// <summary>
    /// Draws a render unit (rasters beneath vectors) onto the given graphics target.
    /// Shared by the layered (per-form) and non-layered (direct page) paths.
    /// </summary>
    private static void DrawUnit(
        XGraphics target,
        RenderUnit unit,
        in MapPageTransform pageTransform,
        PdfOptions baseOptions)
    {
        // Rasters first (drawn beneath the vector features).
        foreach (var rasterLayerData in unit.Rasters)
        {
            if (rasterLayerData.Tiles == null || rasterLayerData.Tiles.Count == 0)
                continue;

            var combinedOpacity = baseOptions.Opacity * rasterLayerData.Opacity;
            WriteRasterTilesForLayer(target, rasterLayerData.Tiles, pageTransform, baseOptions, combinedOpacity);
        }

        // Then vector symbolizer entries, in their original order.
        foreach (var layerData in unit.Vectors)
        {
            if (layerData.Features == null || layerData.Features.Count == 0)
                continue;

            var combinedOpacity = baseOptions.Opacity * layerData.Opacity;

            var layerOptions = new PdfOptions
            {
                StrokeColor = layerData.Options.StrokeColor,
                FillColor = layerData.Options.FillColor,
                StrokeWidth = layerData.Options.StrokeWidth,
                Opacity = combinedOpacity,
                PointCircleRadius = layerData.Options.PointCircleRadius
            };

            foreach (var feature in layerData.Features)
            {
                if (feature?.TheGeometry == null || feature.TheGeometry.IsNullOrEmpty())
                    continue;

                WriteGeometryForLayer(target, feature.TheGeometry, pageTransform, layerOptions);
            }
        }
    }

    /// <summary>
    /// Creates a PDF Optional Content Group (OCG / toggleable layer) and returns its indirect
    /// reference. The OCG is made an indirect object so the reference is valid immediately.
    /// </summary>
    private static PdfReference CreatePdfLayer(PdfDocument document, string layerName)
    {
        // Create the OCG dictionary and make it an indirect object. Without this,
        // PdfArray.Add embeds the dictionary inline and ocg.Reference stays null.
        var ocg = new PdfDictionary(document);
        ocg.Elements["/Type"] = new PdfName("/OCG");
        ocg.Elements["/Name"] = new PdfString(layerName ?? string.Empty, PdfStringEncoding.Unicode);
        document.Internals.AddObject(ocg);

        var catalog = document.Internals.Catalog;

        // Create the /OCProperties tree once, with a default configuration (/D).
        if (catalog.Elements["/OCProperties"] == null)
        {
            var ocProperties = new PdfDictionary(document);
            ocProperties.Elements["/OCGs"] = new PdfArray(document);

            var d = new PdfDictionary(document);
            d.Elements["/Name"] = new PdfString("Default", PdfStringEncoding.Unicode);
            d.Elements["/BaseState"] = new PdfName("/ON");
            d.Elements["/Order"] = new PdfArray(document);
            d.Elements["/ON"] = new PdfArray(document);
            ocProperties.Elements["/D"] = d;

            catalog.Elements["/OCProperties"] = ocProperties;
        }

        var ocProps = (PdfDictionary)catalog.Elements["/OCProperties"];
        var dDict = (PdfDictionary)ocProps.Elements["/D"];

        // Register the OCG's indirect reference in /OCGs, the default /Order, and /ON (visible).
        ((PdfArray)ocProps.Elements["/OCGs"]).Elements.Add(ocg.Reference);
        ((PdfArray)dDict.Elements["/Order"]).Elements.Add(ocg.Reference);
        ((PdfArray)dDict.Elements["/ON"]).Elements.Add(ocg.Reference);

        return ocg.Reference;
    }

    /// <summary>
    /// Registers the OCG reference under a unique name in the page's /Resources /Properties
    /// dictionary and returns that name (e.g. "/oc0") for use as a BDC marked-content operand.
    /// </summary>
    private static string RegisterOptionalContentProperty(PdfPage page, PdfReference ocgRef, int index)
    {
        var document = page.Owner;

        // page.Resources is the same resource dictionary the page's XGraphics uses.
        var resources = page.Resources;

        var properties = resources.Elements.GetDictionary("/Properties");
        if (properties == null)
        {
            properties = new PdfDictionary(document);
            resources.Elements["/Properties"] = properties;
        }

        var name = "/oc" + index;
        properties.Elements[name] = ocgRef;
        return name;
    }

    /// <summary>
    /// Captures the set of content-stream dictionaries currently attached to the page.
    /// </summary>
    private static HashSet<PdfDictionary> SnapshotContentStreams(PdfPage page)
    {
        var set = new HashSet<PdfDictionary>();
        foreach (var item in page.Contents.Elements)
        {
            var dict = (item as PdfReference)?.Value as PdfDictionary ?? item as PdfDictionary;
            if (dict != null)
                set.Add(dict);
        }
        return set;
    }

    /// <summary>
    /// Wraps every content stream added to the page since <paramref name="existing"/> was captured
    /// in an optional-content marked-content sequence (/OC name BDC ... EMC). The streams are
    /// already finalized (their XGraphics is disposed) and not yet compressed, so the byte
    /// prepend/append is safe; PdfSharpCore compresses them on save.
    /// </summary>
    private static void WrapNewContentStreamsWithOptionalContent(PdfPage page, HashSet<PdfDictionary> existing, string propertyName)
    {
        var bdcBytes = System.Text.Encoding.ASCII.GetBytes("/OC " + propertyName + " BDC\n");
        var emcBytes = System.Text.Encoding.ASCII.GetBytes("\nEMC\n");

        foreach (var item in page.Contents.Elements)
        {
            var dict = (item as PdfReference)?.Value as PdfDictionary ?? item as PdfDictionary;
            if (dict == null || existing.Contains(dict))
                continue;

            var currentBytes = dict.Stream?.Value;
            if (currentBytes == null)
            {
                dict.CreateStream(ConcatBytes(bdcBytes, emcBytes));
            }
            else
            {
                dict.Stream.Value = ConcatBytes(bdcBytes, currentBytes, emcBytes);
            }
        }
    }

    private static byte[] ConcatBytes(params byte[][] arrays)
    {
        var length = arrays.Sum(a => a.Length);
        var result = new byte[length];
        var offset = 0;
        foreach (var a in arrays)
        {
            Buffer.BlockCopy(a, 0, result, offset, a.Length);
            offset += a.Length;
        }
        return result;
    }


}