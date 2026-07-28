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
using IRI.Maptor.Sta.Common.Abstractions;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.SpatialReferenceSystem;

namespace IRI.Maptor.Sta.KmlFormat;

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
    public static async Task WriteToFileAsync(
        Geometry<Point> geometry,
        string filePath,
        string? name = null,
        string? description = null)
    {
        var kmlString = ToKml(geometry, name, description);

        await File.WriteAllTextAsync(filePath, kmlString);
    }

    /// <summary>
    /// Writes features with attributes to a KML file
    /// </summary>
    public static async Task WriteToFileAsync(
        List<KmlFeature> features,
        string filePath,
        string? documentName = null)
    {
        var kmlString = ToKml(features, documentName);

        await File.WriteAllTextAsync(filePath, kmlString);
    }

    /// <summary>
    /// Writes geometries to a KML file asynchronously
    /// </summary>
    public static async Task WriteToFileAsync(
        List<Geometry<Point>> geometries,
        string filePath,
        string? documentName = null)
    {
        var kmlString = ToKml(geometries, documentName);

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
        string? description = null)
    {
        EnsureGeodeticWgs84(geometry);

        XNamespace kml = KmlNamespace;

        var kmlDoc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(kml + "kml",
                new XElement(kml + "Document",
                    CreatePlacemarkElement(geometry, name, description, kml)
                )
            )
        );

        return kmlDoc.Declaration + Environment.NewLine + kmlDoc.ToString();
    }

    /// <summary>
    /// Converts a single geometry with Z values to KML string
    /// </summary>
    public static string ToKml(
        Geometry<PointZ> geometry,
        string? name = null,
        string? description = null)
    {
        EnsureGeodeticWgs84(geometry);

        XNamespace kml = KmlNamespace;

        var kmlDoc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(kml + "kml",
                new XElement(kml + "Document",
                    CreatePlacemarkElementFromPointZ(geometry, name, description, kml)
                )
            )
        );

        return kmlDoc.Declaration + Environment.NewLine + kmlDoc.ToString();
    }

    /// <summary>
    /// Converts an IGeometry to KML string (supports both 2D and 3D geometries)
    /// </summary>
    public static string ToKml(
        IGeometry geometry,
        string? name = null,
        string? description = null)
    {
        return geometry switch
        {
            Geometry<PointZ> gz => ToKml(gz, name, description),
            Geometry<Point> g => ToKml(g, name, description),
            _ => throw new NotSupportedException($"Unsupported geometry type: {geometry.GetType()}")
        };
    }

    /// <summary>
    /// Converts multiple geometries to KML string
    /// </summary>
    public static string ToKml(
        List<Geometry<Point>> geometries,
        string? documentName = null)
    {
        foreach (var geometry in geometries)
        {
            EnsureGeodeticWgs84(geometry);
        }

        XNamespace kml = KmlNamespace;

        var placemarks = geometries.Select((g, index) =>
            CreatePlacemarkElement(g, $"Feature {index + 1}", null, kml)).ToArray();

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
    /// Converts multiple geometries with Z values to KML string
    /// </summary>
    public static string ToKml(
        List<Geometry<PointZ>> geometries,
        string? documentName = null)
    {
        foreach (var geometry in geometries)
        {
            EnsureGeodeticWgs84(geometry);
        }

        XNamespace kml = KmlNamespace;

        var placemarks = geometries.Select((g, index) =>
            CreatePlacemarkElementFromPointZ(g, $"Feature {index + 1}", null, kml)).ToArray();

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
    /// Converts multiple geometries (2D or 3D) to KML string
    /// </summary>
    public static string ToKml(
        List<IGeometry> geometries,
        string? documentName = null)
    {
        XNamespace kml = KmlNamespace;

        var placemarks = geometries.Select((g, index) =>
        {
            return g switch
            {
                Geometry<PointZ> gz => CreatePlacemarkElementFromPointZ(gz, $"Feature {index + 1}", null, kml),
                Geometry<Point> gp => CreatePlacemarkElement(gp, $"Feature {index + 1}", null, kml),
                _ => throw new NotSupportedException($"Unsupported geometry type: {g.GetType()}")
            };
        }).ToArray();

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
        string? documentName = null)
    {
        foreach (var feature in features)
        {
            if (feature.Geometry != null)
            {
                EnsureGeodeticWgs84(feature.Geometry);
            }
        }

        XNamespace kml = KmlNamespace;

        var placemarks = features.Select(f =>
            CreatePlacemarkFromFeature(f, kml)).ToArray();

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
        string? documentName = null)
    {
        foreach (var geometries in folders.Values)
        {
            foreach (var geometry in geometries)
            {
                EnsureGeodeticWgs84(geometry);
            }
        }

        XNamespace kml = KmlNamespace;

        var folderElements = folders.Select(kvp =>
        {
            var placemarks = kvp.Value.Select((g, index) =>
                CreatePlacemarkElement(g, $"Feature {index + 1}", null, kml)).ToArray();

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

    #region Private Helper Methods - SRID Validation

    private static void EnsureGeodeticWgs84<T>(Geometry<T> geometry) where T : IPoint, new()
    {
        if (geometry == null || geometry.IsNullOrEmpty())
        {
            return;
        }

        if (geometry.Srid != SridHelper.GeodeticWGS84)
        {
            throw new ArgumentException(
                $"Geometry must be in WGS84 (SRID {SridHelper.GeodeticWGS84}), but was {geometry.Srid}.",
                nameof(geometry));
        }
    }

    #endregion

    #region Private Helper Methods - Placemark Creation

    private static XElement CreatePlacemarkElement(
        Geometry<Point> geometry,
        string? name,
        string? description,
        XNamespace kml)
    {
        EnsureGeodeticWgs84(geometry);

        var placemark = new XElement(kml + "Placemark");

        if (!string.IsNullOrWhiteSpace(name))
        {
            placemark.Add(new XElement(kml + "name", name));
        }

        if (!string.IsNullOrWhiteSpace(description))
        {
            placemark.Add(new XElement(kml + "description", description));
        }

        var geometryElement = CreateGeometryElement(geometry, kml);
        if (geometryElement != null)
        {
            placemark.Add(geometryElement);
        }

        return placemark;
    }

    private static XElement CreatePlacemarkElementFromPointZ(
        Geometry<PointZ> geometry,
        string? name,
        string? description,
        XNamespace kml)
    {
        EnsureGeodeticWgs84(geometry);

        var placemark = new XElement(kml + "Placemark");

        if (!string.IsNullOrWhiteSpace(name))
        {
            placemark.Add(new XElement(kml + "name", name));
        }

        if (!string.IsNullOrWhiteSpace(description))
        {
            placemark.Add(new XElement(kml + "description", description));
        }

        var geometryElement = CreateGeometryElementFromPointZ(geometry, kml);
        if (geometryElement != null)
        {
            placemark.Add(geometryElement);
        }

        return placemark;
    }

    private static XElement CreatePlacemarkFromFeature(
        KmlFeature feature,
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

        var geometryElement = CreateGeometryElement(feature.Geometry, kml);
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
        bool styleUrlAdded = false;

        // 1. Process explicit Style object
        if (style != null)
        {
            if (!string.IsNullOrEmpty(style.StyleUrl))
            {
                placemark.Add(new XElement(kml + "styleUrl", style.StyleUrl));
                styleUrlAdded = true;
            }

            if (style.InlineStyle != null)
            {
                var inlineClone = new XElement(style.InlineStyle);
                EnsureIconStyle(inlineClone, style, kml);
                placemark.Add(inlineClone);
            }
            else if (style.NormalStyle == null && !string.IsNullOrEmpty(style.IconHref))
            {
                placemark.Add(CreateIconStyleElement(style.IconHref!, style.IconScale, kml));
            }
        }

        // 2. If no styleUrl from Style object, try attributes
        if (!styleUrlAdded && feature.Attributes != null)
        {
            // Prefer explicit KmlStyleUrl
            if (feature.Attributes.TryGetValue(KmlAttributeKeys.StyleUrl, out var styleUrlObj) &&
                styleUrlObj is string styleUrl && !string.IsNullOrWhiteSpace(styleUrl))
            {
                placemark.Add(new XElement(kml + "styleUrl", styleUrl));
                styleUrlAdded = true;
            }
            // Otherwise use KmlStyleId (add missing '#')
            else if (feature.Attributes.TryGetValue(KmlAttributeKeys.StyleId, out var styleIdObj) &&
                     styleIdObj is string styleId && !string.IsNullOrWhiteSpace(styleId))
            {
                placemark.Add(new XElement(kml + "styleUrl", "#" + styleId));
                styleUrlAdded = true;
            }
        }

        // 3. Fallback: create an inline icon style from attributes (if no styleUrl was added)
        if (feature.Attributes == null) return;

        if (!styleUrlAdded &&
            feature.Attributes.TryGetValue(KmlAttributeKeys.IconHref, out var iconHrefObj) &&
            iconHrefObj is string iconHref && !string.IsNullOrWhiteSpace(iconHref))
        {
            double? iconScale = null;
            if (feature.Attributes.TryGetValue(KmlAttributeKeys.IconScale, out var scaleString) &&
                double.TryParse(scaleString, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedScale))
            {
                iconScale = parsedScale;
            }
            placemark.Add(CreateIconStyleElement(iconHref, iconScale, kml));
        }
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
        XNamespace kml)
    {
        if (geometry == null || geometry.IsNullOrEmpty())
            return null;

        return geometry.Type switch
        {
            GeometryType.Point => CreatePointElement(geometry, kml),
            GeometryType.LineString => CreateLineStringElement(geometry, kml),
            GeometryType.Polygon => CreatePolygonElement(geometry, kml),
            GeometryType.MultiPoint => CreateMultiGeometryElement(geometry, kml),
            GeometryType.MultiLineString => CreateMultiGeometryElement(geometry, kml),
            GeometryType.MultiPolygon => CreateMultiGeometryElement(geometry, kml),
            GeometryType.GeometryCollection => CreateMultiGeometryElement(geometry, kml),
            _ => null
        };
    }

    private static XElement? CreateGeometryElementFromPointZ(
        Geometry<PointZ> geometry,
        XNamespace kml)
    {
        if (geometry == null || geometry.IsNullOrEmpty())
            return null;

        return geometry.Type switch
        {
            GeometryType.Point => CreatePointElementFromPointZ(geometry, kml),
            GeometryType.LineString => CreateLineStringElementFromPointZ(geometry, kml),
            GeometryType.Polygon => CreatePolygonElementFromPointZ(geometry, kml),
            GeometryType.MultiPoint => CreateMultiGeometryElementFromPointZ(geometry, kml),
            GeometryType.MultiLineString => CreateMultiGeometryElementFromPointZ(geometry, kml),
            GeometryType.MultiPolygon => CreateMultiGeometryElementFromPointZ(geometry, kml),
            GeometryType.GeometryCollection => CreateMultiGeometryElementFromPointZ(geometry, kml),
            _ => null
        };
    }

    private static XElement CreatePointElement(
        Geometry<Point> geometry,
        XNamespace kml)
    {
        if (geometry.Points == null || geometry.Points.Count == 0)
            return new XElement(kml + "Point");

        var point = geometry.Points[0];

        return new XElement(kml + "Point",
            new XElement(kml + "coordinates", FormatCoordinate(point)));
    }

    private static XElement CreateLineStringElement(
        Geometry<Point> geometry,
        XNamespace kml)
    {
        if (geometry.Points == null || geometry.Points.Count == 0)
            return new XElement(kml + "LineString");

        return new XElement(kml + "LineString",
            new XElement(kml + "coordinates", FormatCoordinates(geometry.Points, isRing: false)));
    }

    private static XElement CreatePolygonElement(
        Geometry<Point> geometry,
        XNamespace kml)
    {
        if (geometry.Geometries == null || geometry.Geometries.Count == 0)
            return new XElement(kml + "Polygon");

        var polygon = new XElement(kml + "Polygon");

        // Outer boundary
        var outerRing = geometry.Geometries[0];
        if (outerRing.Points != null && outerRing.Points.Count > 0)
        {
            polygon.Add(new XElement(kml + "outerBoundaryIs",
                new XElement(kml + "LinearRing",
                    new XElement(kml + "coordinates", FormatCoordinates(outerRing.Points, isRing: true)))));
        }

        // Inner boundaries (holes)
        if (geometry.Geometries.Count > 1)
        {
            for (int i = 1; i < geometry.Geometries.Count; i++)
            {
                var innerRing = geometry.Geometries[i];
                if (innerRing.Points != null && innerRing.Points.Count > 0)
                {
                    polygon.Add(new XElement(kml + "innerBoundaryIs",
                        new XElement(kml + "LinearRing",
                            new XElement(kml + "coordinates", FormatCoordinates(innerRing.Points, isRing: true)))));
                }
            }
        }

        return polygon;
    }

    private static XElement CreateMultiGeometryElement(
        Geometry<Point> geometry,
        XNamespace kml)
    {
        if (geometry.Geometries == null || geometry.Geometries.Count == 0)
            return new XElement(kml + "MultiGeometry");

        var geometryElements = geometry.Geometries
            .Select(g => CreateGeometryElement(g, kml))
            .Where(g => g != null)
            .ToArray();

        return new XElement(kml + "MultiGeometry", geometryElements);
    }

    private static XElement CreatePointElementFromPointZ(
        Geometry<PointZ> geometry,
        XNamespace kml)
    {
        if (geometry.Points == null || geometry.Points.Count == 0)
            return new XElement(kml + "Point");

        var pointZ = geometry.Points[0];

        return new XElement(kml + "Point",
            new XElement(kml + "coordinates", FormatCoordinate(pointZ)));
    }

    private static XElement CreateLineStringElementFromPointZ(
        Geometry<PointZ> geometry,
        XNamespace kml)
    {
        if (geometry.Points == null || geometry.Points.Count == 0)
            return new XElement(kml + "LineString");

        return new XElement(kml + "LineString",
            new XElement(kml + "coordinates", FormatCoordinates(geometry.Points, isRing: false)));
    }

    private static XElement CreatePolygonElementFromPointZ(
        Geometry<PointZ> geometry,
        XNamespace kml)
    {
        if (geometry.Geometries == null || geometry.Geometries.Count == 0)
            return new XElement(kml + "Polygon");

        var polygon = new XElement(kml + "Polygon");

        // Outer boundary
        var outerRing = geometry.Geometries[0];
        if (outerRing.Points != null && outerRing.Points.Count > 0)
        {
            polygon.Add(new XElement(kml + "outerBoundaryIs",
                new XElement(kml + "LinearRing",
                    new XElement(kml + "coordinates", FormatCoordinates(outerRing.Points, isRing: true)))));
        }

        // Inner boundaries (holes)
        if (geometry.Geometries.Count > 1)
        {
            for (int i = 1; i < geometry.Geometries.Count; i++)
            {
                var innerRing = geometry.Geometries[i];
                if (innerRing.Points != null && innerRing.Points.Count > 0)
                {
                    polygon.Add(new XElement(kml + "innerBoundaryIs",
                        new XElement(kml + "LinearRing",
                            new XElement(kml + "coordinates", FormatCoordinates(innerRing.Points, isRing: true)))));
                }
            }
        }

        return polygon;
    }

    private static XElement CreateMultiGeometryElementFromPointZ(
        Geometry<PointZ> geometry,
        XNamespace kml)
    {
        if (geometry.Geometries == null || geometry.Geometries.Count == 0)
            return new XElement(kml + "MultiGeometry");

        var geometryElements = geometry.Geometries
            .Select(g => CreateGeometryElementFromPointZ(g, kml))
            .Where(g => g != null)
            .ToArray();

        return new XElement(kml + "MultiGeometry", geometryElements);
    }

    #endregion

    #region Private Helper Methods - Coordinate Formatting

    private static string FormatCoordinate<T>(T point) where T : IPoint
    {
        // KML format: longitude,latitude[,altitude]
        if (point is PointZ pointZ)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:G17},{1:G17},{2:G17}", pointZ.X, pointZ.Y, pointZ.Z);
        }

        return string.Format(CultureInfo.InvariantCulture, "{0:G17},{1:G17}", point.X, point.Y);
    }

    private static string FormatCoordinates<T>(List<T> points, bool isRing) where T : IPoint
    {
        if (points.IsNullOrEmpty())
            return string.Empty;

        var pts = points;

        if (isRing && points.Count > 0)
        {
            var first = points[0];

            var last = points[points.Count - 1];

            // Compare by value; you may use a small tolerance if needed
            if (!first.Equals(last))
            {
                pts = new List<T>(points) { first };
            }
        }

        // KML coordinates are separated by spaces
        return string.Join(" ", pts.Select(p => FormatCoordinate(p)));
    }

    #endregion
}
