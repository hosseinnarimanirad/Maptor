using System.Globalization;
using System.Text;
using System.Xml.Linq;
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Sta.Spatial.IO.Svg;

/// <summary>
/// SVG writer for converting Geometry and Feature types to SVG XML
/// </summary>
public static class SvgWriter
{
    private const string SvgNamespace = "http://www.w3.org/2000/svg";

    /// <summary>
    /// Converts Geometry to SVG string
    /// </summary>
    public static string Write(Geometry<Point> geometry, SvgOptions? options = null)
    {
        if (geometry == null)
            throw new ArgumentNullException(nameof(geometry));

        options ??= new SvgOptions();

        var root = CreateSvgRoot(geometry, options);
        var element = WriteGeometry(geometry, options);
        
        if (element != null)
        {
            root.Add(element);
        }

        var doc = new XDocument(root);
        return doc.ToString();
    }

    /// <summary>
    /// Converts Feature to SVG string
    /// </summary>
    public static string Write(Feature<Point> feature, SvgOptions? options = null)
    {
        if (feature == null)
            throw new ArgumentNullException(nameof(feature));

        options ??= new SvgOptions();

        var root = CreateSvgRoot(feature.TheGeometry, options);
        var element = WriteGeometry(feature.TheGeometry, options);

        if (element != null)
        {
            // Apply Feature attributes if enabled
            if (options.PreserveFeatureAttributes && feature.Attributes != null)
            {
                ApplyFeatureAttributes(element, feature, options);
            }

            root.Add(element);
        }

        var doc = new XDocument(root);
        return doc.ToString();
    }

    /// <summary>
    /// Writes Geometry to SVG file
    /// </summary>
    public static string WriteToFile(Geometry<Point> geometry, string filePath, SvgOptions? options = null)
    {
        var svgContent = Write(geometry, options);
        File.WriteAllText(filePath, svgContent);
        return filePath;
    }

    /// <summary>
    /// Writes Feature to SVG file
    /// </summary>
    public static string WriteToFile(Feature<Point> feature, string filePath, SvgOptions? options = null)
    {
        var svgContent = Write(feature, options);
        File.WriteAllText(filePath, svgContent);
        return filePath;
    }

    private static XElement CreateSvgRoot(Geometry<Point> geometry, SvgOptions options)
    {
        var root = new XElement(XName.Get("svg", SvgNamespace));

        if (geometry.IsNullOrEmpty())
        {
            root.SetAttributeValue("width", "100");
            root.SetAttributeValue("height", "100");
            return root;
        }

        var bbox = geometry.GetBoundingBox();
        var width = bbox.Width;
        var height = bbox.Height;

        // Add padding to viewBox
        var paddingX = width * options.ViewBoxPadding;
        var paddingY = height * options.ViewBoxPadding;

        if (options.IncludeViewBox)
        {
            var viewBoxX = bbox.XMin - paddingX;
            var viewBoxY = bbox.YMin - paddingY;
            var viewBoxWidth = width + (2 * paddingX);
            var viewBoxHeight = height + (2 * paddingY);

            root.SetAttributeValue("viewBox", 
                $"{FormatCoordinate(viewBoxX, options)} {FormatCoordinate(viewBoxY, options)} " +
                $"{FormatCoordinate(viewBoxWidth, options)} {FormatCoordinate(viewBoxHeight, options)}");
        }

        // Set width and height based on viewBox or bounding box
        root.SetAttributeValue("width", FormatCoordinate(width + (2 * paddingX), options));
        root.SetAttributeValue("height", FormatCoordinate(height + (2 * paddingY), options));

        // Add default styling if provided
        if (options.StrokeColor.HasValue || options.FillColor.HasValue)
        {
            var style = new StringBuilder();
            if (options.StrokeColor.HasValue)
            {
                style.Append($"stroke:{options.GetStrokeColorString()};");
                style.Append($"stroke-width:{FormatCoordinate(options.StrokeWidth, options)};");
            }
            if (options.FillColor.HasValue)
            {
                style.Append($"fill:{options.GetFillColorString()};");
            }
            else
            {
                style.Append("fill:none;");
            }
            root.SetAttributeValue("style", style.ToString());
        }

        return root;
    }

    private static XElement? WriteGeometry(Geometry<Point> geometry, SvgOptions options)
    {
        if (geometry.IsNullOrEmpty())
            return null;

        switch (geometry.Type)
        {
            case GeometryType.Point:
                return WritePoint(geometry, options);

            case GeometryType.LineString:
                return WriteLineString(geometry, options);

            case GeometryType.Polygon:
                return WritePolygon(geometry, options);

            case GeometryType.MultiPoint:
                return WriteMultiPoint(geometry, options);

            case GeometryType.MultiLineString:
                return WriteMultiLineString(geometry, options);

            case GeometryType.MultiPolygon:
                return WriteMultiPolygon(geometry, options);

            case GeometryType.GeometryCollection:
                return WriteGeometryCollection(geometry, options);

            default:
                throw new NotImplementedException($"Geometry type {geometry.Type} is not supported for SVG export");
        }
    }

    private static XElement WritePoint(Geometry<Point> geometry, SvgOptions options)
    {
        if (geometry.Points == null || geometry.Points.Count == 0)
            throw new ArgumentException("Point geometry must have at least one point");

        var point = geometry.Points[0];
        var circle = new XElement(XName.Get("circle", SvgNamespace));
        
        circle.SetAttributeValue("cx", FormatCoordinate(point.X, options));
        circle.SetAttributeValue("cy", FormatCoordinate(point.Y, options));
        circle.SetAttributeValue("r", FormatCoordinate(options.PointCircleRadius, options));

        ApplyDefaultStyling(circle, options);
        return circle;
    }

    private static XElement WriteLineString(Geometry<Point> geometry, SvgOptions options)
    {
        if (geometry.Points == null || geometry.Points.Count < 2)
            throw new ArgumentException("LineString geometry must have at least two points");

        var polyline = new XElement(XName.Get("polyline", SvgNamespace));
        var pointsString = string.Join(" ", 
            geometry.Points.Select(p => $"{FormatCoordinate(p.X, options)},{FormatCoordinate(p.Y, options)}"));
        
        polyline.SetAttributeValue("points", pointsString);
        ApplyDefaultStyling(polyline, options);
        return polyline;
    }

    private static XElement WritePolygon(Geometry<Point> geometry, SvgOptions options)
    {
        if (geometry.Geometries == null || geometry.Geometries.Count == 0)
            throw new ArgumentException("Polygon geometry must have at least one ring");

        // Write exterior ring as polygon
        var exteriorRing = geometry.Geometries[0];
        if (exteriorRing.Points == null || exteriorRing.Points.Count < 3)
            throw new ArgumentException("Polygon exterior ring must have at least three points");

        var polygon = new XElement(XName.Get("polygon", SvgNamespace));
        var pointsString = string.Join(" ",
            exteriorRing.Points.Select(p => $"{FormatCoordinate(p.X, options)},{FormatCoordinate(p.Y, options)}"));

        polygon.SetAttributeValue("points", pointsString);
        ApplyDefaultStyling(polygon, options);

        // If there are interior rings (holes), we need to use path instead
        if (geometry.Geometries.Count > 1)
        {
            return WritePolygonAsPath(geometry, options);
        }

        return polygon;
    }

    private static XElement WritePolygonAsPath(Geometry<Point> geometry, SvgOptions options)
    {
        var path = new XElement(XName.Get("path", SvgNamespace));
        var pathData = new StringBuilder();

        // Exterior ring
        var exteriorRing = geometry.Geometries[0];
        if (exteriorRing.Points != null && exteriorRing.Points.Count > 0)
        {
            pathData.Append($"M {FormatCoordinate(exteriorRing.Points[0].X, options)} {FormatCoordinate(exteriorRing.Points[0].Y, options)}");
            for (int i = 1; i < exteriorRing.Points.Count; i++)
            {
                pathData.Append($" L {FormatCoordinate(exteriorRing.Points[i].X, options)} {FormatCoordinate(exteriorRing.Points[i].Y, options)}");
            }
            pathData.Append(" Z"); // Close path
        }

        // Interior rings (holes)
        for (int ringIndex = 1; ringIndex < geometry.Geometries.Count; ringIndex++)
        {
            var ring = geometry.Geometries[ringIndex];
            if (ring.Points != null && ring.Points.Count > 0)
            {
                pathData.Append($" M {FormatCoordinate(ring.Points[0].X, options)} {FormatCoordinate(ring.Points[0].Y, options)}");
                for (int i = 1; i < ring.Points.Count; i++)
                {
                    pathData.Append($" L {FormatCoordinate(ring.Points[i].X, options)} {FormatCoordinate(ring.Points[i].Y, options)}");
                }
                pathData.Append(" Z"); // Close path
            }
        }

        path.SetAttributeValue("d", pathData.ToString());
        ApplyDefaultStyling(path, options);
        return path;
    }

    private static XElement WriteMultiPoint(Geometry<Point> geometry, SvgOptions options)
    {
        if (geometry.Geometries == null || geometry.Geometries.Count == 0)
            throw new ArgumentException("MultiPoint geometry must have at least one point");

        var group = new XElement(XName.Get("g", SvgNamespace));
        
        foreach (var pointGeo in geometry.Geometries)
        {
            var circle = WritePoint(pointGeo, options);
            group.Add(circle);
        }

        return group;
    }

    private static XElement WriteMultiLineString(Geometry<Point> geometry, SvgOptions options)
    {
        if (geometry.Geometries == null || geometry.Geometries.Count == 0)
            throw new ArgumentException("MultiLineString geometry must have at least one line string");

        var group = new XElement(XName.Get("g", SvgNamespace));
        
        foreach (var lineGeo in geometry.Geometries)
        {
            var polyline = WriteLineString(lineGeo, options);
            group.Add(polyline);
        }

        return group;
    }

    private static XElement WriteMultiPolygon(Geometry<Point> geometry, SvgOptions options)
    {
        if (geometry.Geometries == null || geometry.Geometries.Count == 0)
            throw new ArgumentException("MultiPolygon geometry must have at least one polygon");

        var group = new XElement(XName.Get("g", SvgNamespace));
        
        foreach (var polygonGeo in geometry.Geometries)
        {
            var polygon = WritePolygon(polygonGeo, options);
            group.Add(polygon);
        }

        return group;
    }

    private static XElement WriteGeometryCollection(Geometry<Point> geometry, SvgOptions options)
    {
        if (geometry.Geometries == null || geometry.Geometries.Count == 0)
            throw new ArgumentException("GeometryCollection must have at least one geometry");

        var group = new XElement(XName.Get("g", SvgNamespace));
        
        foreach (var subGeometry in geometry.Geometries)
        {
            var element = WriteGeometry(subGeometry, options);
            if (element != null)
            {
                group.Add(element);
            }
        }

        return group;
    }

    private static void ApplyDefaultStyling(XElement element, SvgOptions options)
    {
        if (options.StrokeColor.HasValue)
        {
            element.SetAttributeValue("stroke", options.GetStrokeColorString());
            element.SetAttributeValue("stroke-width", FormatCoordinate(options.StrokeWidth, options));
        }

        if (options.FillColor.HasValue)
        {
            element.SetAttributeValue("fill", options.GetFillColorString());
        }
        else
        {
            // For polygons, default fill is needed; for polylines, no fill
            if (element.Name.LocalName == "polygon" || element.Name.LocalName == "path")
            {
                element.SetAttributeValue("fill", "none");
            }
        }
    }

    private static void ApplyFeatureAttributes(XElement element, Feature<Point> feature, SvgOptions options)
    {
        if (feature.Attributes == null)
            return;

        foreach (var attr in feature.Attributes)
        {
            var key = attr.Key;
            var value = attr.Value?.ToString() ?? string.Empty;

            // Map common attribute names to SVG attributes
            if (key.Equals("id", StringComparison.OrdinalIgnoreCase))
            {
                element.SetAttributeValue("id", value);
            }
            else if (key.Equals("class", StringComparison.OrdinalIgnoreCase))
            {
                element.SetAttributeValue("class", value);
            }
            else if (key.StartsWith("data-", StringComparison.OrdinalIgnoreCase))
            {
                element.SetAttributeValue(key, value);
            }
            else if (key.Equals("style", StringComparison.OrdinalIgnoreCase))
            {
                // Merge with existing style if any
                var existingStyle = element.Attribute("style")?.Value ?? string.Empty;
                element.SetAttributeValue("style", $"{existingStyle};{value}");
            }
            else
            {
                // Store as data attribute
                element.SetAttributeValue($"data-{key.ToLowerInvariant()}", value);
            }
        }
    }

    private static string FormatCoordinate(double value, SvgOptions options)
    {
        return value.ToString($"F{options.CoordinatePrecision}", CultureInfo.InvariantCulture);
    }
}

