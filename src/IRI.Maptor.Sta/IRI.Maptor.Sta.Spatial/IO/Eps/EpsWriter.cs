using System.Globalization;
using System.Text;
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Sta.Spatial.IO.Eps;

/// <summary>
/// EPS writer for converting Geometry and Feature types to EPS format
/// </summary>
public static class EpsWriter
{
    private const string EPS_VERSION = "3.0";

    /// <summary>
    /// Converts Geometry to EPS string
    /// </summary>
    public static string Write(Geometry<Point> geometry, EpsOptions? options = null)
    {
        if (geometry == null)
            throw new ArgumentNullException(nameof(geometry));

        options ??= new EpsOptions();

        var sb = new StringBuilder();

        // Write EPS header
        WriteEpsHeader(geometry, options, sb);

        // Write PostScript prolog (optional setup)
        WriteProlog(options, sb);

        // Write geometry as PostScript commands
        WriteGeometry(geometry, options, sb);

        // Write footer
        sb.AppendLine("%%EOF");

        return sb.ToString();
    }

    /// <summary>
    /// Converts Feature to EPS string
    /// </summary>
    public static string Write(Feature<Point> feature, EpsOptions? options = null)
    {
        if (feature == null)
            throw new ArgumentNullException(nameof(feature));

        options ??= new EpsOptions();

        var sb = new StringBuilder();

        // Write EPS header
        WriteEpsHeader(feature.TheGeometry, options, sb);

        // Write Feature attributes as comments if enabled
        if (options.PreserveFeatureAttributes && feature.Attributes != null)
        {
            foreach (var attr in feature.Attributes)
            {
                sb.AppendLine($"%%{attr.Key}: {attr.Value}");
            }
        }

        // Write PostScript prolog
        WriteProlog(options, sb);

        // Write geometry as PostScript commands
        WriteGeometry(feature.TheGeometry, options, sb);

        // Write footer
        sb.AppendLine("%%EOF");

        return sb.ToString();
    }

    /// <summary>
    /// Writes Geometry to EPS file
    /// </summary>
    public static string WriteToFile(Geometry<Point> geometry, string filePath, EpsOptions? options = null)
    {
        var epsContent = Write(geometry, options);
        File.WriteAllText(filePath, epsContent);
        return filePath;
    }

    /// <summary>
    /// Writes Feature to EPS file
    /// </summary>
    public static string WriteToFile(Feature<Point> feature, string filePath, EpsOptions? options = null)
    {
        var epsContent = Write(feature, options);
        File.WriteAllText(filePath, epsContent);
        return filePath;
    }

    /// <summary>
    /// Writes EPS header with bounding box
    /// </summary>
    private static void WriteEpsHeader(Geometry<Point> geometry, EpsOptions options, StringBuilder sb)
    {
        sb.AppendLine($"%!PS-Adobe-{EPS_VERSION} EPSF-{EPS_VERSION}");

        // Creator
        sb.AppendLine($"%%Creator: {options.Creator}");

        // Title
        if (!string.IsNullOrEmpty(options.Title))
        {
            sb.AppendLine($"%%Title: {options.Title}");
        }

        // Bounding Box
        if (geometry.IsNullOrEmpty())
        {
            sb.AppendLine("%%BoundingBox: 0 0 100 100");
        }
        else
        {
            var bbox = geometry.GetBoundingBox();
            var paddingX = bbox.Width * options.BoundingBoxPadding;
            var paddingY = bbox.Height * options.BoundingBoxPadding;

            var llx = bbox.XMin - paddingX;
            var lly = bbox.YMin - paddingY;
            var urx = bbox.XMax + paddingX;
            var ury = bbox.YMax + paddingY;

            sb.AppendLine($"%%BoundingBox: {FormatCoordinate(llx, options)} {FormatCoordinate(lly, options)} {FormatCoordinate(urx, options)} {FormatCoordinate(ury, options)}");
        }

        sb.AppendLine("%%EndComments");
    }

    /// <summary>
    /// Writes PostScript prolog (setup code)
    /// </summary>
    private static void WriteProlog(EpsOptions options, StringBuilder sb)
    {
        sb.AppendLine("%%BeginProlog");
        sb.AppendLine("/saveobj save def");
        sb.AppendLine("gsave");
        sb.AppendLine("%%EndProlog");
    }

    /// <summary>
    /// Writes geometry as PostScript commands
    /// </summary>
    private static void WriteGeometry(Geometry<Point> geometry, EpsOptions options, StringBuilder sb)
    {
        if (geometry.IsNullOrEmpty())
            return;

        switch (geometry.Type)
        {
            case GeometryType.Point:
                WritePoint(geometry, options, sb);
                break;

            case GeometryType.LineString:
                WriteLineString(geometry, options, sb);
                break;

            case GeometryType.Polygon:
                WritePolygon(geometry, options, sb);
                break;

            case GeometryType.MultiPoint:
                WriteMultiPoint(geometry, options, sb);
                break;

            case GeometryType.MultiLineString:
                WriteMultiLineString(geometry, options, sb);
                break;

            case GeometryType.MultiPolygon:
                WriteMultiPolygon(geometry, options, sb);
                break;

            case GeometryType.GeometryCollection:
                WriteGeometryCollection(geometry, options, sb);
                break;

            default:
                throw new NotImplementedException($"Geometry type {geometry.Type} is not supported for EPS export");
        }
    }

    /// <summary>
    /// Writes Point as PostScript commands
    /// </summary>
    private static void WritePoint(Geometry<Point> geometry, EpsOptions options, StringBuilder sb)
    {
        if (geometry.Points == null || geometry.Points.Count == 0)
            throw new ArgumentException("Point geometry must have at least one point");

        var point = geometry.Points[0];
        sb.AppendLine("newpath");
        sb.AppendLine($"{FormatCoordinate(point.X, options)} {FormatCoordinate(point.Y, options)} moveto");
        sb.AppendLine($"{FormatCoordinate(point.X, options)} {FormatCoordinate(point.Y, options)} lineto");

        ApplyStyling(options, sb);
        sb.AppendLine("stroke");
    }

    /// <summary>
    /// Writes LineString as PostScript commands
    /// </summary>
    private static void WriteLineString(Geometry<Point> geometry, EpsOptions options, StringBuilder sb)
    {
        if (geometry.Points == null || geometry.Points.Count < 2)
            throw new ArgumentException("LineString geometry must have at least two points");

        sb.AppendLine("newpath");
        var firstPoint = geometry.Points[0];
        sb.AppendLine($"{FormatCoordinate(firstPoint.X, options)} {FormatCoordinate(firstPoint.Y, options)} moveto");

        for (int i = 1; i < geometry.Points.Count; i++)
        {
            var point = geometry.Points[i];
            sb.AppendLine($"{FormatCoordinate(point.X, options)} {FormatCoordinate(point.Y, options)} lineto");
        }

        ApplyStyling(options, sb);
        sb.AppendLine("stroke");
    }

    /// <summary>
    /// Writes Polygon as PostScript commands
    /// </summary>
    private static void WritePolygon(Geometry<Point> geometry, EpsOptions options, StringBuilder sb)
    {
        if (geometry.Geometries == null || geometry.Geometries.Count == 0)
            throw new ArgumentException("Polygon geometry must have at least one ring");

        // Write exterior ring
        var exteriorRing = geometry.Geometries[0];
        if (exteriorRing.Points == null || exteriorRing.Points.Count < 3)
            throw new ArgumentException("Polygon exterior ring must have at least three points");

        sb.AppendLine("newpath");
        var firstPoint = exteriorRing.Points[0];
        sb.AppendLine($"{FormatCoordinate(firstPoint.X, options)} {FormatCoordinate(firstPoint.Y, options)} moveto");

        for (int i = 1; i < exteriorRing.Points.Count; i++)
        {
            var point = exteriorRing.Points[i];
            sb.AppendLine($"{FormatCoordinate(point.X, options)} {FormatCoordinate(point.Y, options)} lineto");
        }

        // Write interior rings (holes) if any
        for (int ringIndex = 1; ringIndex < geometry.Geometries.Count; ringIndex++)
        {
            var ring = geometry.Geometries[ringIndex];
            if (ring.Points != null && ring.Points.Count > 0)
            {
                var holeFirstPoint = ring.Points[0];
                sb.AppendLine($"{FormatCoordinate(holeFirstPoint.X, options)} {FormatCoordinate(holeFirstPoint.Y, options)} moveto");
                for (int i = 1; i < ring.Points.Count; i++)
                {
                    var point = ring.Points[i];
                    sb.AppendLine($"{FormatCoordinate(point.X, options)} {FormatCoordinate(point.Y, options)} lineto");
                }
            }
        }

        sb.AppendLine("closepath");
        
        // Set fill color and fill
        if (options.FillColor.HasValue)
        {
            sb.AppendLine($"{options.GetFillColorString()} setrgbcolor");
            sb.AppendLine("fill");
        }
        
        // Set stroke color and stroke
        if (options.StrokeColor.HasValue)
        {
            sb.AppendLine($"{options.GetStrokeColorString()} setrgbcolor");
        }
        else
        {
            sb.AppendLine("0 0 0 setrgbcolor"); // Default black
        }
        sb.AppendLine($"{FormatCoordinate(options.StrokeWidth, options)} setlinewidth");
        sb.AppendLine("stroke");
    }

    /// <summary>
    /// Writes MultiPoint as PostScript commands
    /// </summary>
    private static void WriteMultiPoint(Geometry<Point> geometry, EpsOptions options, StringBuilder sb)
    {
        if (geometry.Geometries == null || geometry.Geometries.Count == 0)
            throw new ArgumentException("MultiPoint geometry must have at least one point");

        foreach (var pointGeo in geometry.Geometries)
        {
            WritePoint(pointGeo, options, sb);
        }
    }

    /// <summary>
    /// Writes MultiLineString as PostScript commands
    /// </summary>
    private static void WriteMultiLineString(Geometry<Point> geometry, EpsOptions options, StringBuilder sb)
    {
        if (geometry.Geometries == null || geometry.Geometries.Count == 0)
            throw new ArgumentException("MultiLineString geometry must have at least one line string");

        foreach (var lineGeo in geometry.Geometries)
        {
            WriteLineString(lineGeo, options, sb);
        }
    }

    /// <summary>
    /// Writes MultiPolygon as PostScript commands
    /// </summary>
    private static void WriteMultiPolygon(Geometry<Point> geometry, EpsOptions options, StringBuilder sb)
    {
        if (geometry.Geometries == null || geometry.Geometries.Count == 0)
            throw new ArgumentException("MultiPolygon geometry must have at least one polygon");

        foreach (var polygonGeo in geometry.Geometries)
        {
            WritePolygon(polygonGeo, options, sb);
        }
    }

    /// <summary>
    /// Writes GeometryCollection as PostScript commands
    /// </summary>
    private static void WriteGeometryCollection(Geometry<Point> geometry, EpsOptions options, StringBuilder sb)
    {
        if (geometry.Geometries == null || geometry.Geometries.Count == 0)
            throw new ArgumentException("GeometryCollection must have at least one geometry");

        foreach (var subGeometry in geometry.Geometries)
        {
            WriteGeometry(subGeometry, options, sb);
        }
    }

    /// <summary>
    /// Applies styling (stroke color, line width) for non-polygon geometries
    /// </summary>
    private static void ApplyStyling(EpsOptions options, StringBuilder sb)
    {
        // Set stroke color
        if (options.StrokeColor.HasValue)
        {
            sb.AppendLine($"{options.GetStrokeColorString()} setrgbcolor");
        }
        else
        {
            sb.AppendLine("0 0 0 setrgbcolor"); // Default black
        }
        
        // Set line width
        sb.AppendLine($"{FormatCoordinate(options.StrokeWidth, options)} setlinewidth");
    }

    /// <summary>
    /// Formats coordinate with specified precision
    /// </summary>
    private static string FormatCoordinate(double value, EpsOptions options)
    {
        return value.ToString($"F{options.CoordinatePrecision}", CultureInfo.InvariantCulture);
    }
}
