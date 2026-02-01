using System.Diagnostics;
using System.IO;
using System.Linq;
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

    /// <summary>
    /// Writes multiple layers to PDF with their symbology
    /// </summary>
    public static byte[] WriteLayers(
        List<LayerPdfData> layers,
        BoundingBox mapExtent,
        double mapScale,
        PdfOptions? baseOptions = null,
        List<RasterLayerPdfData>? rasterLayers = null,
        bool supportPdfLayers = true)
    {
        if (layers == null)
            layers = new List<LayerPdfData>();

        if (mapExtent.IsNaN() || !mapExtent.IsValid())
            throw new ArgumentException("Map extent must be valid", nameof(mapExtent));

        // Check if we have any layers to export (raster or vector)
        if ((layers.Count == 0) && (rasterLayers == null || rasterLayers.Count == 0))
            throw new ArgumentException("At least one layer (vector or raster) must be provided", nameof(layers));

        baseOptions ??= new PdfOptions();

        // Calculate page size - use standard page size or calculate based on aspect ratio
        double pageWidth, pageHeight;
        
        if (baseOptions.PageSize == PdfPageSize.Auto)
        {
            // Calculate page size based on map extent aspect ratio
            var aspectRatio = mapExtent.Width / mapExtent.Height;
            
            // Use A4 as base, but adjust to match aspect ratio
            var baseWidth = A4_WIDTH;
            var baseHeight = A4_HEIGHT;
            
            if (aspectRatio > 1)
            {
                // Landscape orientation
                pageWidth = Math.Max(baseWidth, baseHeight * aspectRatio);
                pageHeight = Math.Max(baseHeight, baseWidth / aspectRatio);
            }
            else
            {
                // Portrait orientation
                pageWidth = Math.Max(baseWidth, baseHeight * aspectRatio);
                pageHeight = Math.Max(baseHeight, baseWidth / aspectRatio);
            }
            
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

        var gfx = XGraphics.FromPdfPage(page);

        try
        {
            // Combine raster and vector layers, sort by ZIndex
            var allLayerItems = new List<(int ZIndex, bool IsRaster, object Data)>();
            
            // Add raster layers
            if (rasterLayers != null)
            {
                foreach (var rasterLayer in rasterLayers)
                {
                    allLayerItems.Add((rasterLayer.ZIndex, true, rasterLayer));
                }
            }
            
            // Add vector layers
            foreach (var vectorLayer in layers)
            {
                allLayerItems.Add((vectorLayer.ZIndex, false, vectorLayer));
            }
            
            // Sort all layers by ZIndex (ascending - lower ZIndex drawn first)
            var sortedLayers = allLayerItems.OrderBy(l => l.ZIndex).ToList();

            // Draw each layer
            foreach (var layerItem in sortedLayers)
            {
                PdfDictionary pdfLayer = null;
                
                if (supportPdfLayers)
                {
                    // Create PDF layer for this map layer
                    string layerName = layerItem.IsRaster 
                        ? ((RasterLayerPdfData)layerItem.Data).LayerName 
                        : ((LayerPdfData)layerItem.Data).LayerName;
                    
                    if (string.IsNullOrWhiteSpace(layerName))
                        layerName = layerItem.IsRaster ? "Raster Layer" : "Vector Layer";
                    
                    pdfLayer = CreatePdfLayer(document, layerName);
                    BeginLayerContent(gfx, pdfLayer);
                }
                
                try
                {
                    if (layerItem.IsRaster)
                    {
                        // Draw raster layer
                        var rasterLayerData = (RasterLayerPdfData)layerItem.Data;
                        if (rasterLayerData.Tiles != null && rasterLayerData.Tiles.Count > 0)
                        {
                            var combinedOpacity = baseOptions.Opacity * rasterLayerData.Opacity;
                            WriteRasterTilesForLayer(gfx, rasterLayerData.Tiles, mapExtent, pageWidth, pageHeight, baseOptions, combinedOpacity);
                        }
                    }
                    else
                    {
                        // Draw vector layer
                        var layerData = (LayerPdfData)layerItem.Data;
                        if (layerData.Features == null || layerData.Features.Count == 0)
                            continue;

                        // Combine layer opacity with base opacity
                        var combinedOpacity = baseOptions.Opacity * layerData.Opacity;

                        // Create layer-specific options
                        var layerOptions = new PdfOptions
                        {
                            StrokeColor = layerData.Options.StrokeColor,
                            FillColor = layerData.Options.FillColor,
                            StrokeWidth = layerData.Options.StrokeWidth,
                            Opacity = combinedOpacity,
                            BoundingBoxPadding = baseOptions.BoundingBoxPadding, // Use base padding for transformation
                            PointCircleRadius = layerData.Options.PointCircleRadius
                        };

                        // Draw all features in this layer
                        foreach (var feature in layerData.Features)
                        {
                            if (feature?.TheGeometry == null || feature.TheGeometry.IsNullOrEmpty())
                                continue;

                            WriteGeometryForLayer(gfx, feature.TheGeometry, mapExtent, pageWidth, pageHeight, layerOptions);
                        }
                    }
                }
                finally
                {
                    if (supportPdfLayers && pdfLayer != null)
                    {
                        EndLayerContent(gfx);
                    }
                }
            }
        }
        finally
        {
            gfx.Dispose();
        }

        using var stream = new MemoryStream();
        document.Save(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Writes geometry to PDF page using map extent transformation
    /// </summary>
    private static void WriteGeometryForLayer(
        XGraphics gfx,
        Geometry<Point> geometry,
        BoundingBox mapExtent,
        double pageWidth,
        double pageHeight,
        PdfOptions options)
    {
        if (geometry.IsNullOrEmpty())
            return;

        // Calculate padding (as percentage of map extent)
        var paddingX = mapExtent.Width * options.BoundingBoxPadding;
        var paddingY = mapExtent.Height * options.BoundingBoxPadding;

        // Calculate content dimensions with padding
        var contentWidth = mapExtent.Width + (2 * paddingX);
        var contentHeight = mapExtent.Height + (2 * paddingY);

        if (contentWidth <= 0) contentWidth = 1;
        if (contentHeight <= 0) contentHeight = 1;

        // Calculate scale factors to fit content within page
        var scaleX = pageWidth / contentWidth;
        var scaleY = pageHeight / contentHeight;
        var scale = Math.Min(scaleX, scaleY); // Maintain aspect ratio

        // Calculate scaled dimensions
        var scaledWidth = mapExtent.Width * scale;
        var scaledHeight = mapExtent.Height * scale;
        
        // Calculate offset to center content on page (accounting for padding)
        var offsetX = (pageWidth - scaledWidth) / 2.0;
        var offsetY = (pageHeight - scaledHeight) / 2.0;

        // Transform point from map coordinates to PDF coordinates
        XPoint TransformPoint(Point point)
        {
            // Transform from map coordinates to scaled coordinates
            var x = (point.X - mapExtent.XMin) * scale + offsetX;
            // PDF uses bottom-left origin, so flip Y
            var y = pageHeight - ((point.Y - mapExtent.YMin) * scale + offsetY);
            return new XPoint(x, y);
        }

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
                        WriteGeometryForLayer(gfx, subGeometry, mapExtent, pageWidth, pageHeight, options);
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

        var pen = GetPen(options);
        gfx.DrawEllipse(pen, pdfPoint.X - radius, pdfPoint.Y - radius, radius * 2, radius * 2);

        if (options.FillColor.HasValue)
        {
            var brush = GetBrush(options);
            gfx.DrawEllipse(brush, pdfPoint.X - radius, pdfPoint.Y - radius, radius * 2, radius * 2);
        }
    }

    private static void WriteLineStringForLayer(XGraphics gfx, Geometry<Point> geometry, Func<Point, XPoint> transform, PdfOptions options)
    {
        if (geometry.Points == null || geometry.Points.Count < 2)
            return;

        var pdfPoints = geometry.Points.Select(transform).ToArray();
        var pen = GetPen(options);
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

        // Fill polygon if fill color is specified
        if (options.FillColor.HasValue)
        {
            var brush = GetBrush(options);
            gfx.DrawPolygon(brush, exteriorPoints, XFillMode.Alternate);
        }

        // Draw polygon outline
        var pen = GetPen(options);
        gfx.DrawPolygon(pen, exteriorPoints);

        // Draw interior rings (holes) if any
        for (int ringIndex = 1; ringIndex < geometry.Geometries.Count; ringIndex++)
        {
            var ring = geometry.Geometries[ringIndex];
            if (ring.Points != null && ring.Points.Count > 0)
            {
                var holePoints = ring.Points.Select(transform).ToArray();

                if (options.FillColor.HasValue)
                {
                    var brush = GetBrush(options);
                    gfx.DrawPolygon(brush, holePoints, XFillMode.Alternate);
                }

                gfx.DrawPolygon(pen, holePoints);
            }
        }
    }

    /// <summary>
    /// Writes raster tiles to PDF page using map extent transformation
    /// </summary>
    private static void WriteRasterTilesForLayer(
        XGraphics gfx,
        List<RasterTileData> tiles,
        BoundingBox mapExtent,
        double pageWidth,
        double pageHeight,
        PdfOptions baseOptions,
        double layerOpacity)
    {
        if (tiles == null || tiles.Count == 0)
            return;

        // Calculate padding (as percentage of map extent)
        var paddingX = mapExtent.Width * baseOptions.BoundingBoxPadding;
        var paddingY = mapExtent.Height * baseOptions.BoundingBoxPadding;

        // Calculate content dimensions with padding
        var contentWidth = mapExtent.Width + (2 * paddingX);
        var contentHeight = mapExtent.Height + (2 * paddingY);

        if (contentWidth <= 0) contentWidth = 1;
        if (contentHeight <= 0) contentHeight = 1;

        // Calculate scale factors to fit content within page
        var scaleX = pageWidth / contentWidth;
        var scaleY = pageHeight / contentHeight;
        var scale = Math.Min(scaleX, scaleY); // Maintain aspect ratio

        // Calculate scaled dimensions
        var scaledWidth = mapExtent.Width * scale;
        var scaledHeight = mapExtent.Height * scale;
        
        // Calculate offset to center content on page (accounting for padding)
        var offsetX = (pageWidth - scaledWidth) / 2.0;
        var offsetY = (pageHeight - scaledHeight) / 2.0;

        // Transform tile extent from map coordinates to PDF coordinates
        XRect TransformExtent(BoundingBox tileExtent)
        {
            // Transform from map coordinates to scaled coordinates
            var x1 = (tileExtent.XMin - mapExtent.XMin) * scale + offsetX;
            var x2 = (tileExtent.XMax - mapExtent.XMin) * scale + offsetX;
            // PDF uses bottom-left origin, so flip Y
            var y1 = pageHeight - ((tileExtent.YMin - mapExtent.YMin) * scale + offsetY);
            var y2 = pageHeight - ((tileExtent.YMax - mapExtent.YMin) * scale + offsetY);
            
            return new XRect(
                Math.Min(x1, x2),
                Math.Min(y1, y2),
                Math.Abs(x2 - x1),
                Math.Abs(y2 - y1));
        }

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
    /// Creates a PDF Optional Content Group (layer) for the given layer name
    /// Returns the OCG dictionary (reference will be created when added to arrays)
    /// </summary>
    private static PdfDictionary CreatePdfLayer(PdfDocument document, string layerName)
    {
        // Create OCG dictionary
        var ocg = new PdfDictionary(document);
        ocg.Elements["/Type"] = new PdfName("/OCG");
        ocg.Elements["/Name"] = new PdfString(layerName);
        
        // Access catalog through Internals property
        var catalog = document.Internals.Catalog;
        
        // Add to document's OCProperties if not already present
        if (catalog.Elements["/OCProperties"] == null)
        {
            var ocProperties = new PdfDictionary(document);
            var ocgs = new PdfArray(document);
            ocProperties.Elements["/OCGs"] = ocgs;
            
            // Create default viewing state
            var d = new PdfDictionary(document);
            d.Elements["/BaseState"] = new PdfName("/ON");
            var order = new PdfArray(document);
            d.Elements["/Order"] = order;
            ocProperties.Elements["/D"] = d;
            
            catalog.Elements["/OCProperties"] = ocProperties;
        }
        
        // Add OCG to document's OCProperties - adding to array will create reference automatically
        var ocProps = catalog.Elements["/OCProperties"] as PdfDictionary;
        var ocgsArray = ocProps.Elements["/OCGs"] as PdfArray;
        ocgsArray.Elements.Add(ocg);
        
        // Add to Order array for default viewing state
        // After adding to ocgsArray, the reference should be available
        var dDict = ocProps.Elements["/D"] as PdfDictionary;
        var orderArray = dDict.Elements["/Order"] as PdfArray;
        // Add the dictionary - PdfSharpCore will handle the reference when saving
        orderArray.Elements.Add(ocg);
        
        return ocg;
    }

    /// <summary>
    /// Begins a marked content sequence with optional content group
    /// Note: PdfSharpCore's XGraphics doesn't support marked content operators directly.
    /// We need to work at the PDF content stream level, which requires accessing the page's content stream.
    /// For now, this creates the layer structure but doesn't mark content (layers won't be toggleable).
    /// </summary>
    private static void BeginLayerContent(XGraphics gfx, PdfDictionary ocg)
    {
        // PdfSharpCore doesn't expose marked content operators through XGraphics API
        // To properly implement this, we would need to:
        // 1. Access the page's content stream directly
        // 2. Insert "/OC ocgReference BDC" before drawing operations
        // 3. Insert "EMC" after drawing operations
        // This is complex and may require modifying PdfSharpCore or working at a lower level
        // For now, layers are created in the document structure but content isn't marked
        // This means the layer panel will show layers but they won't be toggleable
    }

    /// <summary>
    /// Ends a marked content sequence
    /// </summary>
    private static void EndLayerContent(XGraphics gfx)
    {
        // See BeginLayerContent - currently a no-op
    }
}

