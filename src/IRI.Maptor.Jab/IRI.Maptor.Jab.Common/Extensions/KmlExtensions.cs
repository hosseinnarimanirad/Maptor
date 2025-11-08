using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using IRI.Maptor.Jab.Common;
using IRI.Maptor.Jab.Common.Cartography.Symbologies;
using IRI.Maptor.Jab.Common.Helpers;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Extensions;

public static class KmlExtensions
{
    private const string KmlNamespace = "http://www.opengis.net/kml/2.2";

    public static List<ISymbolizer> CreateSymbolizersFromKml(this string fileName, GeometryType geometryType)
    {
        var symbolizers = new List<ISymbolizer>();

        var visualParameters = fileName.TryCreateVisualParametersFromKml(geometryType);

        if (visualParameters != null)
        {
            symbolizers.Add(new SimpleSymbolizer(visualParameters));
        }
        else
        {
            symbolizers.Add(SimpleSymbolizer.Create(null, BrushHelper.PickBrush(), 3, 1));
        }

        return symbolizers;
    }

    public static VisualParameters? TryCreateVisualParametersFromKml(this string fileName, GeometryType geometryType)
    {
        if (string.IsNullOrWhiteSpace(fileName) || !File.Exists(fileName))
        {
            return null;
        }

        try
        {
            var document = XDocument.Load(fileName);
            return document.TryCreateVisualParametersFromKml(geometryType);
        }
        catch
        {
            return null;
        }
    }

    public static VisualParameters? TryCreateVisualParametersFromKml(this XDocument document, GeometryType geometryType)
    {
        if (document == null)
        {
            return null;
        }

        XNamespace kml = KmlNamespace;

        var placemark = document.Descendants(kml + "Placemark").FirstOrDefault();
        if (placemark == null)
        {
            return null;
        }

        var styleElement = ResolveStyleElement(document, placemark, kml);
        if (styleElement == null)
        {
            return null;
        }

        var lineStyle = styleElement.Element(kml + "LineStyle");
        var polyStyle = styleElement.Element(kml + "PolyStyle");
        var iconStyle = styleElement.Element(kml + "IconStyle");

        var strokeHex = ConvertKmlColorToHex(lineStyle?.Element(kml + "color")?.Value);
        var strokeWidth = ParseDouble(lineStyle?.Element(kml + "width")?.Value) ?? 1.0;

        string? fillHex = null;

        if (IsPolygonGeometry(geometryType))
        {
            var fillEnabled = polyStyle?.Element(kml + "fill")?.Value != "0";
            var outlineEnabled = polyStyle?.Element(kml + "outline")?.Value != "0";

            if (fillEnabled)
            {
                fillHex = ConvertKmlColorToHex(polyStyle?.Element(kml + "color")?.Value);
            }

            if (!outlineEnabled)
            {
                strokeHex = null;
            }

            if (string.IsNullOrEmpty(strokeHex))
            {
                strokeHex = ConvertKmlColorToHex(lineStyle?.Element(kml + "color")?.Value);
            }
        }
        else if (IsLineGeometry(geometryType))
        {
            if (string.IsNullOrEmpty(strokeHex))
            {
                strokeHex = ConvertKmlColorToHex(polyStyle?.Element(kml + "color")?.Value);
            }
        }
        else if (IsPointGeometry(geometryType))
        {
            fillHex = ConvertKmlColorToHex(iconStyle?.Element(kml + "color")?.Value)
                      ?? ConvertKmlColorToHex(polyStyle?.Element(kml + "color")?.Value)
                      ?? strokeHex;
        }

        if (string.IsNullOrEmpty(strokeHex) && string.IsNullOrEmpty(fillHex))
        {
            return null;
        }

        var fillBrush = string.IsNullOrEmpty(fillHex) ? null : BrushHelper.CreateBrush(fillHex);
        var strokeBrush = string.IsNullOrEmpty(strokeHex) ? null : BrushHelper.CreateBrush(strokeHex);

        var parameters = new VisualParameters(fillBrush, strokeBrush, strokeWidth, 1);

        if (IsPointGeometry(geometryType) && parameters.PointSymbol is not null)
        {
            var iconScale = ParseDouble(iconStyle?.Element(kml + "scale")?.Value);
            if (iconScale.HasValue && iconScale.Value > 0)
            {
                var size = Math.Clamp(iconScale.Value * parameters.PointSymbol.SymbolWidth, 4, 64);
                parameters.PointSymbol.SymbolWidth = size;
                parameters.PointSymbol.SymbolHeight = size;
            }
        }

        return parameters;
    }

    private static XElement? ResolveStyleElement(XDocument document, XElement placemark, XNamespace kml)
    {
        var inlineStyle = placemark.Element(kml + "Style");
        if (inlineStyle != null)
        {
            return inlineStyle;
        }

        var styleUrl = placemark.Element(kml + "styleUrl")?.Value;
        var resolved = ResolveStyleByUrl(document, styleUrl, kml);
        if (resolved != null)
        {
            return resolved;
        }

        return document.Descendants(kml + "Style").FirstOrDefault();
    }

    private static XElement? ResolveStyleByUrl(XDocument document, string? styleUrl, XNamespace kml)
    {
        var styleId = ExtractStyleId(styleUrl);

        if (string.IsNullOrEmpty(styleId))
        {
            return null;
        }

        var styleElement = document.Descendants(kml + "Style")
            .FirstOrDefault(e => string.Equals(e.Attribute("id")?.Value, styleId, StringComparison.OrdinalIgnoreCase));

        if (styleElement != null)
        {
            return styleElement;
        }

        var styleMapElement = document.Descendants(kml + "StyleMap")
            .FirstOrDefault(e => string.Equals(e.Attribute("id")?.Value, styleId, StringComparison.OrdinalIgnoreCase));

        if (styleMapElement != null)
        {
            var normalUrl = styleMapElement.Elements(kml + "Pair")
                .FirstOrDefault(p => string.Equals(p.Element(kml + "key")?.Value, "normal", StringComparison.OrdinalIgnoreCase))
                ?.Element(kml + "styleUrl")?.Value;

            if (!string.IsNullOrWhiteSpace(normalUrl))
            {
                return ResolveStyleByUrl(document, normalUrl, kml);
            }
        }

        return null;
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

    private static bool IsPointGeometry(GeometryType geometryType) =>
        geometryType == GeometryType.Point || geometryType == GeometryType.MultiPoint;

    private static bool IsLineGeometry(GeometryType geometryType) =>
        geometryType == GeometryType.LineString ||
        geometryType == GeometryType.MultiLineString ||
        geometryType == GeometryType.CircularString ||
        geometryType == GeometryType.CompoundCurve;

    private static bool IsPolygonGeometry(GeometryType geometryType) =>
        geometryType == GeometryType.Polygon ||
        geometryType == GeometryType.MultiPolygon ||
        geometryType == GeometryType.CurvePolygon;

    private static double? ParseDouble(string? value)
    {
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        return null;
    }

    private static string? ConvertKmlColorToHex(string? kmlColor)
    {
        if (string.IsNullOrWhiteSpace(kmlColor))
        {
            return null;
        }

        var raw = kmlColor.Trim();

        if (raw.StartsWith("#", StringComparison.Ordinal))
        {
            raw = raw[1..];
        }

        if (raw.Length == 6)
        {
            raw = "ff" + raw;
        }

        if (raw.Length != 8)
        {
            return null;
        }

        var alpha = raw.Substring(0, 2);
        var blue = raw.Substring(2, 2);
        var green = raw.Substring(4, 2);
        var red = raw.Substring(6, 2);

        return $"#{alpha}{red}{green}{blue}";
    }
}
