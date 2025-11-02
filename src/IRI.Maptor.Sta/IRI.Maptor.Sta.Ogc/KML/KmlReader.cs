using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Ket.KmlFormat;

/// <summary>
/// KML Reader for parsing KML files and extracting geometries
/// Supports KML 2.2 specification
/// Uses XDocument for parsing to avoid XmlSerializer limitations
/// </summary>
public static class KmlReader
{
    private const string KmlNamespace = "http://www.opengis.net/kml/2.2";

    #region Public Methods

    /// <summary>
    /// Reads and parses a KML file from the specified path
    /// </summary>
    /// <param name="filePath">Path to the KML file</param>
    /// <param name="targetSrid">Target SRID for the geometries (default: 4326 - WGS84)</param>
    /// <returns>List of geometries extracted from the KML file</returns>
    public static List<Geometry<Point>> ReadFromFile(string filePath, int targetSrid = 4326)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"KML file not found: {filePath}", filePath);

        try
        {
            var document = XDocument.Load(filePath);
            return ExtractGeometries(document, targetSrid);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to parse KML file: {filePath}", ex);
        }
    }

    /// <summary>
    /// Reads and parses a KML file asynchronously
    /// </summary>
    /// <param name="filePath">Path to the KML file</param>
    /// <param name="targetSrid">Target SRID for the geometries (default: 4326 - WGS84)</param>
    /// <returns>List of geometries extracted from the KML file</returns>
    public static async Task<List<Geometry<Point>>> ReadFromFileAsync(string filePath, int targetSrid = 4326)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"KML file not found: {filePath}", filePath);

        return await Task.Run(() => ReadFromFile(filePath, targetSrid));
    }

    /// <summary>
    /// Parses a KML string and extracts geometries
    /// </summary>
    /// <param name="kmlString">KML content as string</param>
    /// <param name="targetSrid">Target SRID for the geometries (default: 4326 - WGS84)</param>
    /// <returns>List of geometries extracted from the KML string</returns>
    public static List<Geometry<Point>> Parse(string kmlString, int targetSrid = 4326)
    {
        if (string.IsNullOrWhiteSpace(kmlString))
            throw new ArgumentException("KML string cannot be null or empty", nameof(kmlString));

        try
        {
            var document = XDocument.Parse(kmlString);
            return ExtractGeometries(document, targetSrid);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to parse KML string", ex);
        }
    }

    /// <summary>
    /// Reads KML with feature attributes (ExtendedData)
    /// </summary>
    /// <param name="filePath">Path to the KML file</param>
    /// <param name="targetSrid">Target SRID for the geometries (default: 4326 - WGS84)</param>
    /// <returns>List of features with geometries and attributes</returns>
    public static List<KmlFeature> ReadFeaturesFromFile(string filePath, int targetSrid = 4326)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"KML file not found: {filePath}", filePath);

        try
        {
            var document = XDocument.Load(filePath);
            return ExtractFeatures(document, targetSrid);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to parse KML file: {filePath}", ex);
        }
    }

    /// <summary>
    /// Parses KML string and extracts features with attributes
    /// </summary>
    /// <param name="kmlString">KML content as string</param>
    /// <param name="targetSrid">Target SRID for the geometries (default: 4326 - WGS84)</param>
    /// <returns>List of features with geometries and attributes</returns>
    public static List<KmlFeature> ParseFeatures(string kmlString, int targetSrid = 4326)
    {
        if (string.IsNullOrWhiteSpace(kmlString))
            throw new ArgumentException("KML string cannot be null or empty", nameof(kmlString));

        try
        {
            var document = XDocument.Parse(kmlString);
            return ExtractFeatures(document, targetSrid);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to parse KML string", ex);
        }
    }

    #endregion

    #region Private Helper Methods - Geometry Extraction

    private static List<Geometry<Point>> ExtractGeometries(XDocument document, int targetSrid)
    {
        var geometries = new List<Geometry<Point>>();
        XNamespace kml = KmlNamespace;

        // Find all Placemarks
        var placemarks = document.Descendants(kml + "Placemark");

        foreach (var placemark in placemarks)
        {
            var geometry = ExtractGeometryFromPlacemark(placemark, kml, targetSrid);
            if (geometry != null && !geometry.IsNullOrEmpty())
            {
                geometries.Add(geometry);
            }
        }

        return geometries;
    }

    private static List<KmlFeature> ExtractFeatures(XDocument document, int targetSrid)
    {
        var features = new List<KmlFeature>();
        XNamespace kml = KmlNamespace;

        // Find all Placemarks
        var placemarks = document.Descendants(kml + "Placemark");

        foreach (var placemark in placemarks)
        {
            var feature = ExtractFeatureFromPlacemark(placemark, kml, targetSrid);
            if (feature != null)
            {
                features.Add(feature);
            }
        }

        return features;
    }

    private static Geometry<Point>? ExtractGeometryFromPlacemark(XElement placemark, XNamespace kml, int targetSrid)
    {
        // Try to find different geometry types
        var point = placemark.Element(kml + "Point");
        if (point != null)
            return ParsePoint(point, kml, targetSrid);

        var lineString = placemark.Element(kml + "LineString");
        if (lineString != null)
            return ParseLineString(lineString, kml, targetSrid);

        var linearRing = placemark.Element(kml + "LinearRing");
        if (linearRing != null)
            return ParseLinearRing(linearRing, kml, targetSrid);

        var polygon = placemark.Element(kml + "Polygon");
        if (polygon != null)
            return ParsePolygon(polygon, kml, targetSrid);

        var multiGeometry = placemark.Element(kml + "MultiGeometry");
        if (multiGeometry != null)
            return ParseMultiGeometry(multiGeometry, kml, targetSrid);

        return null;
    }

    private static KmlFeature? ExtractFeatureFromPlacemark(XElement placemark, XNamespace kml, int targetSrid)
    {
        var geometry = ExtractGeometryFromPlacemark(placemark, kml, targetSrid);

        if (geometry == null || geometry.IsNullOrEmpty())
            return null;

        var feature = new KmlFeature
        {
            Geometry = geometry,
            Name = placemark.Element(kml + "name")?.Value,
            Description = placemark.Element(kml + "description")?.Value,
            Id = placemark.Attribute("id")?.Value,
            Attributes = new Dictionary<string, string>()
        };

        // Extract ExtendedData
        var extendedData = placemark.Element(kml + "ExtendedData");
        if (extendedData != null)
        {
            var schemaDataElements = extendedData.Elements(kml + "SchemaData");
            foreach (var schemaData in schemaDataElements)
            {
                var simpleDataElements = schemaData.Elements(kml + "SimpleData");
                foreach (var simpleData in simpleDataElements)
                {
                    var name = simpleData.Attribute("name")?.Value;
                    if (!string.IsNullOrEmpty(name))
                    {
                        feature.Attributes[name] = simpleData.Value ?? string.Empty;
                    }
                }
            }
        }

        return feature;
    }

    #endregion

    #region Private Helper Methods - Geometry Parsing

    private static Geometry<Point>? ParsePoint(XElement pointElement, XNamespace kml, int srid)
    {
        var coordinatesElement = pointElement.Element(kml + "coordinates");
        if (coordinatesElement == null)
            return null;

        var coordinates = coordinatesElement.Value;
        var point = ParseSingleCoordinate(coordinates);

        if (point == null)
            return null;

        return new Geometry<Point>(new List<Point> { point }, GeometryType.Point, srid);
    }

    private static Geometry<Point>? ParseLineString(XElement lineStringElement, XNamespace kml, int srid)
    {
        var coordinatesElement = lineStringElement.Element(kml + "coordinates");
        if (coordinatesElement == null)
            return null;

        var coordinates = coordinatesElement.Value;
        var points = ParseCoordinates(coordinates);

        if (points == null || points.Count < 2)
            return null;

        return new Geometry<Point>(points, GeometryType.LineString, srid);
    }

    private static Geometry<Point>? ParseLinearRing(XElement linearRingElement, XNamespace kml, int srid)
    {
        var coordinatesElement = linearRingElement.Element(kml + "coordinates");
        if (coordinatesElement == null)
            return null;

        var coordinates = coordinatesElement.Value;
        var points = ParseCoordinates(coordinates);

        if (points == null || points.Count < 3)
            return null;

        // Ensure the ring is closed (first point = last point)
        if (points.First().X != points.Last().X || points.First().Y != points.Last().Y)
        {
            points.Add(new Point(points.First().X, points.First().Y));
        }

        return new Geometry<Point>(points, GeometryType.LineString, true, srid);
    }

    private static Geometry<Point>? ParsePolygon(XElement polygonElement, XNamespace kml, int srid)
    {
        var rings = new List<Geometry<Point>>();

        // Outer boundary
        var outerBoundary = polygonElement.Element(kml + "outerBoundaryIs");
        if (outerBoundary != null)
        {
            var linearRing = outerBoundary.Element(kml + "LinearRing");
            if (linearRing != null)
            {
                var outerRing = ParseLinearRing(linearRing, kml, srid);
                if (outerRing != null)
                {
                    rings.Add(outerRing);
                }
            }
        }

        // Inner boundaries (holes)
        var innerBoundaries = polygonElement.Elements(kml + "innerBoundaryIs");
        foreach (var innerBoundary in innerBoundaries)
        {
            var linearRing = innerBoundary.Element(kml + "LinearRing");
            if (linearRing != null)
            {
                var innerRing = ParseLinearRing(linearRing, kml, srid);
                if (innerRing != null)
                {
                    rings.Add(innerRing);
                }
            }
        }

        if (rings.Count == 0)
            return null;

        return new Geometry<Point>(rings, GeometryType.Polygon, srid);
    }

    private static Geometry<Point>? ParseMultiGeometry(XElement multiGeometryElement, XNamespace kml, int srid)
    {
        var geometries = new List<Geometry<Point>>();

        // Parse all child geometries
        foreach (var child in multiGeometryElement.Elements())
        {
            Geometry<Point>? parsed = null;

            if (child.Name.LocalName == "Point")
            {
                parsed = ParsePoint(child, kml, srid);
            }
            else if (child.Name.LocalName == "LineString")
            {
                parsed = ParseLineString(child, kml, srid);
            }
            else if (child.Name.LocalName == "LinearRing")
            {
                parsed = ParseLinearRing(child, kml, srid);
            }
            else if (child.Name.LocalName == "Polygon")
            {
                parsed = ParsePolygon(child, kml, srid);
            }

            if (parsed != null && !parsed.IsNullOrEmpty())
            {
                geometries.Add(parsed);
            }
        }

        if (geometries.Count == 0)
            return null;

        // Determine the multi-geometry type
        var firstType = geometries.First().Type;
        var isHomogeneous = geometries.All(g => g.Type == firstType);

        GeometryType multiType = isHomogeneous ? firstType switch
        {
            GeometryType.Point => GeometryType.MultiPoint,
            GeometryType.LineString => GeometryType.MultiLineString,
            GeometryType.Polygon => GeometryType.MultiPolygon,
            _ => GeometryType.GeometryCollection
        } : GeometryType.GeometryCollection;

        return new Geometry<Point>(geometries, multiType, srid);
    }

    #endregion

    #region Private Helper Methods - Coordinate Parsing

    private static Point? ParseSingleCoordinate(string coordinateString)
    {
        if (string.IsNullOrWhiteSpace(coordinateString))
            return null;

        var parts = coordinateString.Trim().Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 2)
            return null;

        if (double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double x) &&
            double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double y))
        {
            // KML format is longitude,latitude (x,y) or longitude,latitude,altitude
            return new Point(x, y);
        }

        return null;
    }

    private static List<Point> ParseCoordinates(string coordinatesString)
    {
        if (string.IsNullOrWhiteSpace(coordinatesString))
            return new List<Point>();

        var points = new List<Point>();

        // KML coordinates can be separated by whitespace or newlines
        var coordinateSets = coordinatesString.Trim()
            .Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var coordSet in coordinateSets)
        {
            var point = ParseSingleCoordinate(coordSet);
            if (point != null)
            {
                points.Add(point);
            }
        }

        return points;
    }

    #endregion
}

/// <summary>
/// Represents a KML feature with geometry and attributes
/// </summary>
public class KmlFeature
{
    public Geometry<Point> Geometry { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Id { get; set; }
    public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>();
}
