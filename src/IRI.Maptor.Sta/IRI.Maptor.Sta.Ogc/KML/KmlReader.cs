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

    private static XNamespace ResolveKmlNamespace(XDocument document)
    {
        var rootNamespace = document.Root?.Name.Namespace;
        if (rootNamespace != null && !string.IsNullOrEmpty(rootNamespace.NamespaceName))
        {
            return rootNamespace;
        }

        return KmlNamespace;
    }

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
        XNamespace kml = ResolveKmlNamespace(document);

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
        XNamespace kml = ResolveKmlNamespace(document);

        var styleCatalog = BuildStyleCatalog(document, kml);

        // Find all Placemarks
        var placemarks = document.Descendants(kml + "Placemark");

        foreach (var placemark in placemarks)
        {
            var feature = ExtractFeatureFromPlacemark(placemark, kml, targetSrid, styleCatalog);
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

    private static KmlFeature? ExtractFeatureFromPlacemark(
        XElement placemark,
        XNamespace kml,
        int targetSrid,
        KmlStyleCatalog styleCatalog)
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

        var styleMetadata = ExtractStyleMetadata(placemark, kml, styleCatalog);
        if (styleMetadata != null)
        {
            feature.Style = styleMetadata;
        }

        var regionMetadata = ExtractRegionMetadata(placemark, kml);
        if (regionMetadata != null)
        {
            feature.Region = regionMetadata;
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

    #region Private Helper Methods - Styles & Regions

    private static KmlStyleCatalog BuildStyleCatalog(XDocument document, XNamespace kml)
    {
        var catalog = new KmlStyleCatalog();

        foreach (var styleElement in document.Descendants(kml + "Style"))
        {
            var id = styleElement.Attribute("id")?.Value;
            if (!string.IsNullOrWhiteSpace(id))
            {
                catalog.Styles[id] = new XElement(styleElement);
            }
        }

        foreach (var styleMapElement in document.Descendants(kml + "StyleMap"))
        {
            var id = styleMapElement.Attribute("id")?.Value;
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            var styleMap = new KmlStyleMapEntry();

            foreach (var pair in styleMapElement.Elements(kml + "Pair"))
            {
                var key = pair.Element(kml + "key")?.Value?.Trim();
                var styleUrl = pair.Element(kml + "styleUrl")?.Value?.Trim();

                if (string.Equals(key, "normal", StringComparison.OrdinalIgnoreCase))
                {
                    styleMap.NormalStyleUrl = styleUrl;
                    var normalStyleId = ExtractStyleId(styleUrl);
                    if (!string.IsNullOrWhiteSpace(normalStyleId) && catalog.Styles.TryGetValue(normalStyleId, out var normalStyle))
                    {
                        styleMap.NormalStyle = new XElement(normalStyle);
                    }
                }
            }

            catalog.StyleMaps[id] = styleMap;
        }

        return catalog;
    }

    private static KmlStyleMetadata? ExtractStyleMetadata(
        XElement placemark,
        XNamespace kml,
        KmlStyleCatalog styleCatalog)
    {
        var styleUrlRaw = placemark.Element(kml + "styleUrl")?.Value?.Trim();
        var inlineStyleElement = placemark.Element(kml + "Style");

        if (styleUrlRaw == null && inlineStyleElement == null && !styleCatalog.HasStyles)
        {
            return null;
        }

        var metadata = new KmlStyleMetadata
        {
            StyleUrl = styleUrlRaw,
            InlineStyle = inlineStyleElement != null ? new XElement(inlineStyleElement) : null
        };

        var styleId = ExtractStyleId(styleUrlRaw);
        metadata.StyleId = styleId;

        XElement? representativeStyle = metadata.InlineStyle;

        if (!string.IsNullOrWhiteSpace(styleId))
        {
            if (styleCatalog.StyleMaps.TryGetValue(styleId, out var styleMap))
            {
                metadata.IsStyleMap = true;
                metadata.NormalStyleUrl = styleMap.NormalStyleUrl;
                if (styleMap.NormalStyle != null)
                {
                    metadata.NormalStyle = new XElement(styleMap.NormalStyle);
                    representativeStyle ??= metadata.NormalStyle;
                }
            }
            else if (styleCatalog.Styles.TryGetValue(styleId, out var style))
            {
                metadata.NormalStyle = new XElement(style);
                representativeStyle ??= metadata.NormalStyle;
            }
        }

        if (representativeStyle != null)
        {
            PopulateIconMetadata(metadata, representativeStyle, kml);
        }

        if (metadata.HasAnyStyle ||
            !metadata.StyleUrl.IsNullOrEmpty() ||
            !metadata.StyleId.IsNullOrEmpty() ||
            metadata.IconHref != null)
        {
            return metadata;
        }

        return null;
    }

    private static KmlRegionMetadata? ExtractRegionMetadata(XElement placemark, XNamespace kml)
    {
        var regionElement = placemark.Element(kml + "Region");
        if (regionElement == null)
        {
            return null;
        }

        var regionMetadata = new KmlRegionMetadata();

        var lodElement = regionElement.Element(kml + "Lod");
        if (lodElement != null)
        {
            regionMetadata.MinLodPixels = TryParseDouble(lodElement.Element(kml + "minLodPixels")?.Value);
            regionMetadata.MaxLodPixels = TryParseDouble(lodElement.Element(kml + "maxLodPixels")?.Value);
            regionMetadata.MinFadeExtent = TryParseDouble(lodElement.Element(kml + "minFadeExtent")?.Value);
            regionMetadata.MaxFadeExtent = TryParseDouble(lodElement.Element(kml + "maxFadeExtent")?.Value);
        }

        var latLonAltBoxElement = regionElement.Element(kml + "LatLonAltBox");
        if (latLonAltBoxElement == null)
        {
            latLonAltBoxElement = regionElement.Element(kml + "LatLonBox");
        }

        if (latLonAltBoxElement != null)
        {
            var latLonAltBox = new KmlLatLonAltBox
            {
                North = TryParseDouble(latLonAltBoxElement.Element(kml + "north")?.Value),
                South = TryParseDouble(latLonAltBoxElement.Element(kml + "south")?.Value),
                East = TryParseDouble(latLonAltBoxElement.Element(kml + "east")?.Value),
                West = TryParseDouble(latLonAltBoxElement.Element(kml + "west")?.Value),
                MinAltitude = TryParseDouble(latLonAltBoxElement.Element(kml + "minAltitude")?.Value),
                MaxAltitude = TryParseDouble(latLonAltBoxElement.Element(kml + "maxAltitude")?.Value),
                AltitudeMode = latLonAltBoxElement.Element(kml + "altitudeMode")?.Value ?? latLonAltBoxElement.Element(kml + "altitudeMode")?.Value
            };

            if (latLonAltBox.HasAnyValue)
            {
                regionMetadata.LatLonAltBox = latLonAltBox;
            }
        }

        if (!regionMetadata.HasValues)
        {
            return null;
        }

        return regionMetadata;
    }

    private static void PopulateIconMetadata(KmlStyleMetadata metadata, XElement styleElement, XNamespace kml)
    {
        var iconStyle = styleElement.Element(kml + "IconStyle");
        if (iconStyle == null)
        {
            return;
        }

        metadata.IconScale ??= TryParseDouble(iconStyle.Element(kml + "scale")?.Value);

        var iconElement = iconStyle.Element(kml + "Icon");
        var href = iconElement?.Element(kml + "href")?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(href))
        {
            metadata.IconHref ??= href;
        }
    }

    private static string? ExtractStyleId(string? styleUrl)
    {
        if (string.IsNullOrWhiteSpace(styleUrl))
        {
            return null;
        }

        var trimmed = styleUrl.Trim();
        var hashIndex = trimmed.LastIndexOf('#');

        if (hashIndex >= 0 && hashIndex < trimmed.Length - 1)
        {
            return trimmed[(hashIndex + 1)..];
        }

        return trimmed;
    }

    private static double? TryParseDouble(string? value)
    {
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        return null;
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
    public KmlStyleMetadata? Style { get; set; }
    public KmlRegionMetadata? Region { get; set; }
}

public class KmlStyleMetadata
{
    public string? StyleUrl { get; set; }
    public string? StyleId { get; set; }
    public bool IsStyleMap { get; set; }
    public string? NormalStyleUrl { get; set; }
    public XElement? InlineStyle { get; set; }
    public XElement? NormalStyle { get; set; }
    public string? IconHref { get; set; }
    public double? IconScale { get; set; }

    public bool HasAnyStyle =>
        InlineStyle != null || NormalStyle != null;
}

public class KmlRegionMetadata
{
    public double? MinLodPixels { get; set; }
    public double? MaxLodPixels { get; set; }
    public double? MinFadeExtent { get; set; }
    public double? MaxFadeExtent { get; set; }
    public KmlLatLonAltBox? LatLonAltBox { get; set; }

    internal bool HasValues =>
        MinLodPixels.HasValue ||
        MaxLodPixels.HasValue ||
        MinFadeExtent.HasValue ||
        MaxFadeExtent.HasValue ||
        (LatLonAltBox?.HasAnyValue ?? false);
}

public class KmlLatLonAltBox
{
    public double? North { get; set; }
    public double? South { get; set; }
    public double? East { get; set; }
    public double? West { get; set; }
    public double? MinAltitude { get; set; }
    public double? MaxAltitude { get; set; }
    public string? AltitudeMode { get; set; }

    internal bool HasAnyValue =>
        North.HasValue || South.HasValue || East.HasValue || West.HasValue ||
        MinAltitude.HasValue || MaxAltitude.HasValue || !string.IsNullOrWhiteSpace(AltitudeMode);
}

internal class KmlStyleCatalog
{
    public Dictionary<string, XElement> Styles { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, KmlStyleMapEntry> StyleMaps { get; } = new(StringComparer.OrdinalIgnoreCase);

    public bool HasStyles => Styles.Count > 0 || StyleMaps.Count > 0;
}

internal class KmlStyleMapEntry
{
    public string? NormalStyleUrl { get; set; }
    public XElement? NormalStyle { get; set; }
}

