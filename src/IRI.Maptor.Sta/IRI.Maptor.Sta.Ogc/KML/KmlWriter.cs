using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Ket.KmlFormat;

/// <summary>
/// KML Writer for exporting geometries to KML format
/// Supports KML 2.2 specification
/// Uses XDocument for building KML to avoid XmlSerializer limitations
/// </summary>
public static class KmlWriter
{
    private const string KmlNamespace = "http://www.opengis.net/kml/2.2";

    #region Public Methods - Write to File

    /// <summary>
    /// Writes a single geometry to a KML file
    /// </summary>
    public static void WriteToFile(
        Geometry<Point> geometry,
        string filePath,
        string? name = null,
        string? description = null,
        Func<Point, Point>? projectToGeodeticFunc = null)
    {
        var kmlString = ToKml(geometry, name, description, projectToGeodeticFunc);
        File.WriteAllText(filePath, kmlString);
    }

    /// <summary>
    /// Writes multiple geometries to a KML file
    /// </summary>
    public static void WriteToFile(
        List<Geometry<Point>> geometries,
        string filePath,
        string? documentName = null,
        Func<Point, Point>? projectToGeodeticFunc = null)
    {
        var kmlString = ToKml(geometries, documentName, projectToGeodeticFunc);
        File.WriteAllText(filePath, kmlString);
    }

    /// <summary>
    /// Writes features with attributes to a KML file
    /// </summary>
    public static void WriteToFile(
        List<KmlFeature> features,
        string filePath,
        string? documentName = null,
        Func<Point, Point>? projectToGeodeticFunc = null)
    {
        var kmlString = ToKml(features, documentName, projectToGeodeticFunc);
        File.WriteAllText(filePath, kmlString);
    }

    /// <summary>
    /// Writes geometries to a KML file asynchronously
    /// </summary>
    public static async Task WriteToFileAsync(
        List<Geometry<Point>> geometries,
        string filePath,
        string? documentName = null,
        Func<Point, Point>? projectToGeodeticFunc = null)
    {
        var kmlString = ToKml(geometries, documentName, projectToGeodeticFunc);
        await File.WriteAllTextAsync(filePath, kmlString);
    }

    #endregion

    #region Public Methods - Convert to KML String

    /// <summary>
    /// Converts a single geometry to KML string
    /// </summary>
    public static string ToKml(
        Geometry<Point> geometry,
        string? name = null,
        string? description = null,
        Func<Point, Point>? projectToGeodeticFunc = null)
    {
        XNamespace kml = KmlNamespace;
        
        var kmlDoc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(kml + "kml",
                new XElement(kml + "Document",
                    CreatePlacemarkElement(geometry, name, description, projectToGeodeticFunc, kml)
                )
            )
        );

        return kmlDoc.Declaration + Environment.NewLine + kmlDoc.ToString();
    }

    /// <summary>
    /// Converts multiple geometries to KML string
    /// </summary>
    public static string ToKml(
        List<Geometry<Point>> geometries,
        string? documentName = null,
        Func<Point, Point>? projectToGeodeticFunc = null)
    {
        XNamespace kml = KmlNamespace;

        var placemarks = geometries.Select((g, index) =>
            CreatePlacemarkElement(g, $"Feature {index + 1}", null, projectToGeodeticFunc, kml)).ToArray();

        var document = new XElement(kml + "Document", placemarks);
        
        if (!string.IsNullOrWhiteSpace(documentName))
        {
            document.AddFirst(new XElement(kml + "name", documentName));
        }

        var kmlDoc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(kml + "kml", document)
        );

        return kmlDoc.Declaration + Environment.NewLine + kmlDoc.ToString();
    }

    /// <summary>
    /// Converts features with attributes to KML string
    /// </summary>
    public static string ToKml(
        List<KmlFeature> features,
        string? documentName = null,
        Func<Point, Point>? projectToGeodeticFunc = null)
    {
        XNamespace kml = KmlNamespace;

        var placemarks = features.Select(f =>
            CreatePlacemarkFromFeature(f, projectToGeodeticFunc, kml)).ToArray();

        var document = new XElement(kml + "Document");

        AddSharedStyles(document, features, kml);

        foreach (var placemark in placemarks)
        {
            document.Add(placemark);
        }

        if (!string.IsNullOrWhiteSpace(documentName))
        {
            document.AddFirst(new XElement(kml + "name", documentName));
        }

        var kmlDoc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(kml + "kml", document)
        );

        return kmlDoc.Declaration + Environment.NewLine + kmlDoc.ToString();
    }

    /// <summary>
    /// Converts geometries to KML string with folders
    /// </summary>
    public static string ToKmlWithFolders(
        Dictionary<string, List<Geometry<Point>>> folders,
        string? documentName = null,
        Func<Point, Point>? projectToGeodeticFunc = null)
    {
        XNamespace kml = KmlNamespace;

        var folderElements = folders.Select(kvp =>
        {
            var placemarks = kvp.Value.Select((g, index) =>
                CreatePlacemarkElement(g, $"Feature {index + 1}", null, projectToGeodeticFunc, kml)).ToArray();

            return new XElement(kml + "Folder",
                new XElement(kml + "name", kvp.Key),
                placemarks);
        }).ToArray();

        var document = new XElement(kml + "Document", folderElements);

        if (!string.IsNullOrWhiteSpace(documentName))
        {
            document.AddFirst(new XElement(kml + "name", documentName));
        }

        var kmlDoc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(kml + "kml", document)
        );

        return kmlDoc.Declaration + Environment.NewLine + kmlDoc.ToString();
    }

    #endregion

    #region Private Helper Methods - Placemark Creation

    private static XElement CreatePlacemarkElement(
        Geometry<Point> geometry,
        string? name,
        string? description,
        Func<Point, Point>? projectToGeodeticFunc,
        XNamespace kml)
    {
        var placemark = new XElement(kml + "Placemark");

        if (!string.IsNullOrWhiteSpace(name))
        {
            placemark.Add(new XElement(kml + "name", name));
        }

        if (!string.IsNullOrWhiteSpace(description))
        {
            placemark.Add(new XElement(kml + "description", description));
        }

        var geometryElement = CreateGeometryElement(geometry, projectToGeodeticFunc, kml);
        if (geometryElement != null)
        {
            placemark.Add(geometryElement);
        }

        return placemark;
    }

    private static XElement CreatePlacemarkFromFeature(
        KmlFeature feature,
        Func<Point, Point>? projectToGeodeticFunc,
        XNamespace kml)
    {
        var placemark = new XElement(kml + "Placemark");

        if (!string.IsNullOrWhiteSpace(feature.Name))
        {
            placemark.Add(new XElement(kml + "name", feature.Name));
        }

        if (!string.IsNullOrWhiteSpace(feature.Description))
        {
            placemark.Add(new XElement(kml + "description", feature.Description));
        }

        AddStyleElements(placemark, feature, kml);

        // Add extended data if attributes exist
        if (feature.Attributes != null && feature.Attributes.Count > 0)
        {
            var simpleDataElements = feature.Attributes.Select(kvp =>
                new XElement(kml + "SimpleData",
                    new XAttribute("name", kvp.Key),
                    kvp.Value));

            placemark.Add(new XElement(kml + "ExtendedData",
                new XElement(kml + "SchemaData", simpleDataElements)));
        }

        var geometryElement = CreateGeometryElement(feature.Geometry, projectToGeodeticFunc, kml);
        if (geometryElement != null)
        {
            placemark.Add(geometryElement);
        }

        return placemark;
    }

    #endregion

    #region Private Helper Methods - Style Handling

    private static void AddSharedStyles(XElement document, IEnumerable<KmlFeature> features, XNamespace kml)
    {
        var uniqueStyles = new HashSet<string>(StringComparer.Ordinal);

        foreach (var feature in features)
        {
            var style = feature.Style;
            if (style?.NormalStyle == null)
            {
                continue;
            }

            var styleClone = new XElement(style.NormalStyle);
            EnsureIconStyle(styleClone, style, kml);

            var key = styleClone.ToString(SaveOptions.DisableFormatting);
            if (uniqueStyles.Add(key))
            {
                document.Add(styleClone);
            }
        }
    }

    private static void AddStyleElements(XElement placemark, KmlFeature feature, XNamespace kml)
    {
        var style = feature.Style;

        if (style != null)
        {
            if (!style.StyleUrl.IsNullOrEmpty())
            {
                placemark.Add(new XElement(kml + "styleUrl", style.StyleUrl));
            }

            if (style.InlineStyle != null)
            {
                var inlineClone = new XElement(style.InlineStyle);
                EnsureIconStyle(inlineClone, style, kml);
                placemark.Add(inlineClone);
            }
            else if (style.NormalStyle == null && !style.IconHref.IsNullOrEmpty())
            {
                placemark.Add(CreateIconStyleElement(style.IconHref!, style.IconScale, kml));
            }

            return;
        }

        if (feature.Attributes == null)
        {
            return;
        }

        if (!feature.Attributes.TryGetValue(KmlAttributeKeys.IconHref, out var iconHrefObj) ||
            iconHrefObj is not string iconHref ||
            string.IsNullOrWhiteSpace(iconHref))
        {
            return;
        }

        double? iconScale = null;
        if (feature.Attributes.TryGetValue(KmlAttributeKeys.IconScale, out var scaleString) &&
            double.TryParse(scaleString, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedScale))
        {
            iconScale = parsedScale;
        }

        placemark.Add(CreateIconStyleElement(iconHref, iconScale, kml));
    }

    private static void EnsureIconStyle(XElement styleElement, KmlStyleMetadata metadata, XNamespace kml)
    {
        if (metadata.IconHref.IsNullOrEmpty())
        {
            return;
        }

        var iconStyle = styleElement.Element(kml + "IconStyle");
        if (iconStyle == null)
        {
            iconStyle = new XElement(kml + "IconStyle");
            styleElement.Add(iconStyle);
        }

        if (metadata.IconScale.HasValue)
        {
            var scaleElement = iconStyle.Element(kml + "scale");
            if (scaleElement == null)
            {
                iconStyle.Add(new XElement(kml + "scale", metadata.IconScale.Value.ToString(CultureInfo.InvariantCulture)));
            }
            else
            {
                scaleElement.Value = metadata.IconScale.Value.ToString(CultureInfo.InvariantCulture);
            }
        }

        var iconElement = iconStyle.Element(kml + "Icon");
        if (iconElement == null)
        {
            iconElement = new XElement(kml + "Icon");
            iconStyle.Add(iconElement);
        }

        var hrefElement = iconElement.Element(kml + "href");
        if (hrefElement == null)
        {
            iconElement.Add(new XElement(kml + "href", metadata.IconHref));
        }
        else
        {
            hrefElement.Value = metadata.IconHref!;
        }
    }

    private static XElement CreateIconStyleElement(string iconHref, double? iconScale, XNamespace kml)
    {
        var styleElement = new XElement(kml + "Style");
        var iconStyle = new XElement(kml + "IconStyle");

        if (iconScale.HasValue)
        {
            iconStyle.Add(new XElement(kml + "scale", iconScale.Value.ToString(CultureInfo.InvariantCulture)));
        }

        var iconElement = new XElement(kml + "Icon",
            new XElement(kml + "href", iconHref));

        iconStyle.Add(iconElement);
        styleElement.Add(iconStyle);

        return styleElement;
    }

    #endregion

    #region Private Helper Methods - Geometry Creation

    private static XElement? CreateGeometryElement(
        Geometry<Point> geometry,
        Func<Point, Point>? projectToGeodeticFunc,
        XNamespace kml)
    {
        if (geometry == null || geometry.IsNullOrEmpty())
            return null;

        return geometry.Type switch
        {
            GeometryType.Point => CreatePointElement(geometry, projectToGeodeticFunc, kml),
            GeometryType.LineString => CreateLineStringElement(geometry, projectToGeodeticFunc, kml),
            GeometryType.Polygon => CreatePolygonElement(geometry, projectToGeodeticFunc, kml),
            GeometryType.MultiPoint => CreateMultiGeometryElement(geometry, projectToGeodeticFunc, kml),
            GeometryType.MultiLineString => CreateMultiGeometryElement(geometry, projectToGeodeticFunc, kml),
            GeometryType.MultiPolygon => CreateMultiGeometryElement(geometry, projectToGeodeticFunc, kml),
            GeometryType.GeometryCollection => CreateMultiGeometryElement(geometry, projectToGeodeticFunc, kml),
            _ => null
        };
    }

    private static XElement CreatePointElement(
        Geometry<Point> geometry,
        Func<Point, Point>? projectToGeodeticFunc,
        XNamespace kml)
    {
        if (geometry.Points == null || geometry.Points.Count == 0)
            return new XElement(kml + "Point");

        var point = geometry.Points[0];
        if (projectToGeodeticFunc != null)
        {
            point = projectToGeodeticFunc(point);
        }

        return new XElement(kml + "Point",
            new XElement(kml + "coordinates", FormatCoordinate(point)));
    }

    private static XElement CreateLineStringElement(
        Geometry<Point> geometry,
        Func<Point, Point>? projectToGeodeticFunc,
        XNamespace kml)
    {
        if (geometry.Points == null || geometry.Points.Count == 0)
            return new XElement(kml + "LineString");

        var points = geometry.Points;
        if (projectToGeodeticFunc != null)
        {
            points = points.Select(projectToGeodeticFunc).ToList();
        }

        return new XElement(kml + "LineString",
            new XElement(kml + "coordinates", FormatCoordinates(points)));
    }

    private static XElement CreatePolygonElement(
        Geometry<Point> geometry,
        Func<Point, Point>? projectToGeodeticFunc,
        XNamespace kml)
    {
        if (geometry.Geometries == null || geometry.Geometries.Count == 0)
            return new XElement(kml + "Polygon");

        var polygon = new XElement(kml + "Polygon");

        // Outer boundary
        var outerRing = geometry.Geometries[0];
        if (outerRing.Points != null && outerRing.Points.Count > 0)
        {
            var points = outerRing.Points;
            if (projectToGeodeticFunc != null)
            {
                points = points.Select(projectToGeodeticFunc).ToList();
            }

            polygon.Add(new XElement(kml + "outerBoundaryIs",
                new XElement(kml + "LinearRing",
                    new XElement(kml + "coordinates", FormatCoordinates(points)))));
        }

        // Inner boundaries (holes)
        if (geometry.Geometries.Count > 1)
        {
            for (int i = 1; i < geometry.Geometries.Count; i++)
            {
                var innerRing = geometry.Geometries[i];
                if (innerRing.Points != null && innerRing.Points.Count > 0)
                {
                    var points = innerRing.Points;
                    if (projectToGeodeticFunc != null)
                    {
                        points = points.Select(projectToGeodeticFunc).ToList();
                    }

                    polygon.Add(new XElement(kml + "innerBoundaryIs",
                        new XElement(kml + "LinearRing",
                            new XElement(kml + "coordinates", FormatCoordinates(points)))));
                }
            }
        }

        return polygon;
    }

    private static XElement CreateMultiGeometryElement(
        Geometry<Point> geometry,
        Func<Point, Point>? projectToGeodeticFunc,
        XNamespace kml)
    {
        if (geometry.Geometries == null || geometry.Geometries.Count == 0)
            return new XElement(kml + "MultiGeometry");

        var geometryElements = geometry.Geometries
            .Select(g => CreateGeometryElement(g, projectToGeodeticFunc, kml))
            .Where(g => g != null)
            .ToArray();

        return new XElement(kml + "MultiGeometry", geometryElements);
    }

    #endregion

    #region Private Helper Methods - Coordinate Formatting

    private static string FormatCoordinate(Point point)
    {
        // KML format: longitude,latitude[,altitude]
        return string.Format(CultureInfo.InvariantCulture, "{0:G17},{1:G17}", point.X, point.Y);
    }

    private static string FormatCoordinates(List<Point> points)
    {
        // KML coordinates are separated by spaces
        return string.Join(" ", points.Select(p => FormatCoordinate(p)));
    }

    #endregion
}
