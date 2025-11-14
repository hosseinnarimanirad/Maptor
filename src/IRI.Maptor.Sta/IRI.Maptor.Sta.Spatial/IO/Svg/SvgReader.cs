using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Sta.Spatial.IO.Svg;

/// <summary>
/// SVG reader for parsing SVG XML and converting to Geometry and Feature types
/// </summary>
public static class SvgReader
{
    private const string SvgNamespace = "http://www.w3.org/2000/svg";

    /// <summary>
    /// Reads SVG from file and converts to Geometry
    /// </summary>
    public static Geometry<Point> ReadFromFile(string filePath, int srid = 0)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("SVG file not found", filePath);

        var content = File.ReadAllText(filePath);
        return Read(content, srid);
    }

    /// <summary>
    /// Reads SVG from string and converts to Geometry
    /// </summary>
    public static Geometry<Point> Read(string svgContent, int srid = 0)
    {
        if (string.IsNullOrWhiteSpace(svgContent))
            return Geometry<Point>.Empty;

        try
        {
            var doc = XDocument.Parse(svgContent);
            var svgElement = doc.Root;

            if (svgElement == null || svgElement.Name.LocalName != "svg")
                throw new InvalidOperationException("SVG root element not found");

            // Find all geometry elements (circle, polyline, polygon, path, g)
            var geometries = new List<Geometry<Point>>();
            ParseSvgElement(svgElement, geometries, srid);

            if (geometries.Count == 0)
                return Geometry<Point>.Empty;

            if (geometries.Count == 1)
                return geometries[0];

            // Multiple geometries - create GeometryCollection
            return new Geometry<Point>(geometries, GeometryType.GeometryCollection, srid);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error parsing SVG: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Reads SVG from file and converts to Feature
    /// </summary>
    public static Feature<Point> ReadFeatureFromFile(string filePath, int srid = 0, bool preserveAttributes = true)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("SVG file not found", filePath);

        var content = File.ReadAllText(filePath);
        return ReadFeature(content, srid, preserveAttributes);
    }

    /// <summary>
    /// Reads SVG from string and converts to Feature
    /// </summary>
    public static Feature<Point> ReadFeature(string svgContent, int srid = 0, bool preserveAttributes = true)
    {
        var geometry = Read(svgContent, srid);
        var attributes = new Dictionary<string, object>();

        if (preserveAttributes)
        {
            try
            {
                var doc = XDocument.Parse(svgContent);
                var svgElement = doc.Root;

                if (svgElement != null)
                {
                    // Extract attributes from the first geometry element found
                    var firstGeometryElement = FindFirstGeometryElement(svgElement);
                    if (firstGeometryElement != null)
                    {
                        ExtractAttributes(firstGeometryElement, attributes);
                    }
                }
            }
            catch
            {
                // If attribute extraction fails, continue with empty attributes
            }
        }

        return new Feature<Point>(geometry, attributes);
    }

    private static void ParseSvgElement(XElement element, List<Geometry<Point>> geometries, int srid)
    {
        var localName = element.Name.LocalName;

        switch (localName)
        {
            case "circle":
                var circleGeo = ParseCircle(element, srid);
                if (circleGeo != null)
                    geometries.Add(circleGeo);
                break;

            case "polyline":
                var polylineGeo = ParsePolyline(element, srid);
                if (polylineGeo != null)
                    geometries.Add(polylineGeo);
                break;

            case "polygon":
                var polygonGeo = ParsePolygon(element, srid);
                if (polygonGeo != null)
                    geometries.Add(polygonGeo);
                break;

            case "path":
                var pathGeos = ParsePath(element, srid);
                geometries.AddRange(pathGeos);
                break;

            case "g":
                // Group - recursively parse children
                foreach (var child in element.Elements())
                {
                    ParseSvgElement(child, geometries, srid);
                }
                break;

            case "svg":
                // Root element - parse children
                foreach (var child in element.Elements())
                {
                    ParseSvgElement(child, geometries, srid);
                }
                break;
        }
    }

    private static Geometry<Point>? ParseCircle(XElement element, int srid)
    {
        var cxAttr = element.Attribute("cx");
        var cyAttr = element.Attribute("cy");

        if (cxAttr == null || cyAttr == null)
            return null;

        if (double.TryParse(cxAttr.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double cx) &&
            double.TryParse(cyAttr.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double cy))
        {
            return Geometry<Point>.Create(cx, cy, srid);
        }

        return null;
    }

    private static Geometry<Point>? ParsePolyline(XElement element, int srid)
    {
        var pointsAttr = element.Attribute("points");
        if (pointsAttr == null || string.IsNullOrWhiteSpace(pointsAttr.Value))
            return null;

        var points = ParsePointsString(pointsAttr.Value);
        if (points.Count < 2)
            return null;

        return new Geometry<Point>(points, GeometryType.LineString, srid);
    }

    private static Geometry<Point>? ParsePolygon(XElement element, int srid)
    {
        var pointsAttr = element.Attribute("points");
        if (pointsAttr == null || string.IsNullOrWhiteSpace(pointsAttr.Value))
            return null;

        var points = ParsePointsString(pointsAttr.Value);
        if (points.Count < 3)
            return null;

        // Create polygon with single ring
        var ring = new Geometry<Point>(points, GeometryType.LineString, srid);
        return new Geometry<Point>(new List<Geometry<Point>> { ring }, GeometryType.Polygon, srid);
    }

    private static List<Geometry<Point>> ParsePath(XElement element, int srid)
    {
        var dAttr = element.Attribute("d");
        if (dAttr == null || string.IsNullOrWhiteSpace(dAttr.Value))
            return new List<Geometry<Point>>();

        return ParsePathData(dAttr.Value, srid);
    }

    private static List<Geometry<Point>> ParsePathData(string pathData, int srid)
    {
        var geometries = new List<Geometry<Point>>();
        
        // Simple path parser - handles M (MoveTo), L (LineTo), Z (ClosePath)
        // More complex path commands (curves, arcs) are approximated as line segments
        
        var commands = Regex.Split(pathData, @"(?=[MLZmlz])", RegexOptions.IgnoreCase);
        var currentPoints = new List<Point>();
        Point? startPoint = null;

        foreach (var command in commands)
        {
            if (string.IsNullOrWhiteSpace(command))
                continue;

            var trimmed = command.Trim();
            if (trimmed.Length == 0)
                continue;

            var cmd = trimmed[0];
            var coords = trimmed.Substring(1).Trim();

            switch (char.ToUpperInvariant(cmd))
            {
                case 'M': // MoveTo
                    // If we have accumulated points, create geometry
                    if (currentPoints.Count >= 2)
                    {
                        geometries.Add(new Geometry<Point>(currentPoints, GeometryType.LineString, srid));
                    }
                    currentPoints.Clear();
                    var movePoints = ParseCoordinatePairs(coords);
                    if (movePoints.Count > 0)
                    {
                        currentPoints.AddRange(movePoints);
                        startPoint = movePoints[0];
                    }
                    break;

                case 'L': // LineTo
                    var linePoints = ParseCoordinatePairs(coords);
                    currentPoints.AddRange(linePoints);
                    break;

                case 'Z': // ClosePath
                    if (currentPoints.Count >= 3 && startPoint != null)
                    {
                        // Close the path by adding start point if not already closed
                        var lastPoint = currentPoints[currentPoints.Count - 1];
                        if (lastPoint.X != startPoint.X || lastPoint.Y != startPoint.Y)
                        {
                            currentPoints.Add(new Point(startPoint.X, startPoint.Y));
                        }
                        // Create polygon
                        var ring = new Geometry<Point>(currentPoints, GeometryType.LineString, srid);
                        geometries.Add(new Geometry<Point>(new List<Geometry<Point>> { ring }, GeometryType.Polygon, srid));
                    }
                    else if (currentPoints.Count >= 2)
                    {
                        // Create closed line string
                        geometries.Add(new Geometry<Point>(currentPoints, GeometryType.LineString, srid));
                    }
                    currentPoints.Clear();
                    startPoint = null;
                    break;

                // Handle other commands by approximating as line segments
                case 'H': // Horizontal line
                case 'V': // Vertical line
                case 'C': // Cubic Bezier
                case 'S': // Smooth cubic Bezier
                case 'Q': // Quadratic Bezier
                case 'T': // Smooth quadratic Bezier
                case 'A': // Arc
                    // For simplicity, parse coordinates and add as line segments
                    var approxPoints = ParseCoordinatePairs(coords);
                    if (approxPoints.Count > 0 && currentPoints.Count > 0)
                    {
                        currentPoints.AddRange(approxPoints);
                    }
                    break;
            }
        }

        // Add any remaining points as line string
        if (currentPoints.Count >= 2)
        {
            geometries.Add(new Geometry<Point>(currentPoints, GeometryType.LineString, srid));
        }

        return geometries;
    }

    private static List<Point> ParsePointsString(string pointsString)
    {
        var points = new List<Point>();
        var pairs = ParseCoordinatePairs(pointsString);
        points.AddRange(pairs);
        return points;
    }

    private static List<Point> ParseCoordinatePairs(string coordinateString)
    {
        var points = new List<Point>();
        
        // Remove whitespace and split by spaces or commas
        var cleaned = Regex.Replace(coordinateString, @"\s+", " ").Trim();
        var parts = Regex.Split(cleaned, @"[\s,]+");

        for (int i = 0; i < parts.Length - 1; i += 2)
        {
            if (double.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out double x) &&
                double.TryParse(parts[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out double y))
            {
                points.Add(new Point(x, y));
            }
        }

        return points;
    }

    private static XElement? FindFirstGeometryElement(XElement root)
    {
        foreach (var element in root.Descendants())
        {
            var localName = element.Name.LocalName;
            if (localName == "circle" || localName == "polyline" || localName == "polygon" || localName == "path")
            {
                return element;
            }
        }
        return null;
    }

    private static void ExtractAttributes(XElement element, Dictionary<string, object> attributes)
    {
        foreach (var attr in element.Attributes())
        {
            var name = attr.Name.LocalName;
            var value = attr.Value;

            // Skip SVG-specific attributes that don't map to Feature attributes
            if (name == "points" || name == "d" || name == "cx" || name == "cy" || name == "r" ||
                name == "viewBox" || name == "width" || name == "height" || name == "xmlns")
            {
                continue;
            }

            // Map SVG attributes to Feature attributes
            if (name == "id" || name == "class")
            {
                attributes[name] = value;
            }
            else if (name.StartsWith("data-", StringComparison.OrdinalIgnoreCase))
            {
                // Remove "data-" prefix for Feature attributes
                var key = name.Substring(5);
                attributes[key] = value;
            }
            else
            {
                // Store other attributes with original name
                attributes[name] = value;
            }
        }
    }
}

