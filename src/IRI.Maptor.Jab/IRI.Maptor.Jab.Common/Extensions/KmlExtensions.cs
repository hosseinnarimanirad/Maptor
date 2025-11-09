using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using IRI.Maptor.Jab.Common;
using IRI.Maptor.Jab.Common.Cartography.Symbologies;
using IRI.Maptor.Jab.Common.Helpers;
using IRI.Maptor.Ket.KmlFormat;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Extensions;

public static class KmlExtensions
{
    private static readonly XNamespace Kml = "http://www.opengis.net/kml/2.2";

    public static List<ISymbolizer> CreateSymbolizersFromKml(this IEnumerable<Feature<Point>> features, GeometryType geometryType)
    {
        var featureList = features?.Where(f => f != null).ToList() ?? new List<Feature<Point>>();

        if (featureList.Count == 0)
        {
            return new List<ISymbolizer>
            {
                SimpleSymbolizer.Create(null, BrushHelper.PickBrush(), 3, 1)
            };
        }

        var groups = featureList.GroupBy(f => KmlAttributeKeys.GetStyleKey(f));
        var symbolizers = new List<ISymbolizer>();

        foreach (var group in groups)
        {
            var metadata = GetStyleMetadata(group);
            var visualParameters = BuildVisualParameters(metadata, geometryType) ?? VisualParameters.CreateNew();

            var styleKey = group.Key;
            Func<Feature<Point>, bool> filter = styleKey == null
                ? f => string.IsNullOrEmpty(KmlAttributeKeys.GetStyleKey(f))
                : f => string.Equals(KmlAttributeKeys.GetStyleKey(f), styleKey, StringComparison.OrdinalIgnoreCase);

            var symbolizer = new SimpleSymbolizer(filter, visualParameters);

            var regionMetadata = GetRegionMetadata(group);
            ApplyRegionMetadata(symbolizer, regionMetadata);

            symbolizers.Add(symbolizer);
        }

        return symbolizers;
    }

    private static KmlStyleMetadata? GetStyleMetadata(IGrouping<string?, Feature<Point>> group)
    {
        foreach (var feature in group)
        {
            if (feature.Attributes != null &&
                feature.Attributes.TryGetValue(KmlAttributeKeys.StyleMetadata, out var metadataObj) &&
                metadataObj is KmlStyleMetadata metadata)
            {
                return metadata;
            }
        }

        return null;
    }

    private static KmlRegionMetadata? GetRegionMetadata(IGrouping<string?, Feature<Point>> group)
    {
        foreach (var feature in group)
        {
            if (feature.Attributes != null &&
                feature.Attributes.TryGetValue(KmlAttributeKeys.RegionMetadata, out var metadataObj) &&
                metadataObj is KmlRegionMetadata metadata)
            {
                return metadata;
            }
        }

        return null;
    }

    private static void ApplyRegionMetadata(SimpleSymbolizer symbolizer, KmlRegionMetadata? regionMetadata)
    {
        if (regionMetadata == null)
        {
            return;
        }

        if (regionMetadata.MinLodPixels.HasValue)
        {
            symbolizer.MinScaleDenominator = regionMetadata.MinLodPixels.Value;
        }

        if (regionMetadata.MaxLodPixels.HasValue)
        {
            symbolizer.MaxScaleDenominator = regionMetadata.MaxLodPixels.Value;
        }
    }

    private static VisualParameters? BuildVisualParameters(KmlStyleMetadata? metadata, GeometryType geometryType)
    {
        var styleElement = metadata?.InlineStyle ?? metadata?.NormalStyle;

        if (styleElement == null)
        {
            var fallback = VisualParameters.CreateNew();
            ApplyIconMetadata(fallback, metadata, geometryType);
            EnsureDefaults(fallback, geometryType);
            return fallback;
        }

        var visual = new VisualParameters(null, null, 1, 1);

        ApplyPolyStyle(visual, styleElement.Element(Kml + "PolyStyle"), geometryType);
        ApplyLineStyle(visual, styleElement.Element(Kml + "LineStyle"));
        ApplyIconStyle(visual, styleElement.Element(Kml + "IconStyle"));
        ApplyLabelStyle(visual, styleElement.Element(Kml + "LabelStyle"));
        ApplyIconMetadata(visual, metadata, geometryType);

        EnsureDefaults(visual, geometryType);

        return visual;
    }

    private static void ApplyIconMetadata(VisualParameters visual, KmlStyleMetadata? metadata, GeometryType geometryType)
    {
        if (metadata == null || metadata.IconHref.IsNullOrEmpty() || !IsPointGeometry(geometryType))
        {
            return;
        }

        visual.PointSymbol.IconHref = metadata.IconHref;

        if (metadata.IconScale.HasValue && metadata.IconScale.Value > 0)
        {
            var baseSize = Math.Max(visual.PointSymbol.SymbolWidth, 12);
            var size = Math.Clamp(baseSize * metadata.IconScale.Value, 4, 128);
            visual.PointSymbol.SymbolWidth = size;
            visual.PointSymbol.SymbolHeight = size;
        }
    }

    private static void ApplyPolyStyle(VisualParameters visual, XElement? polyStyleElement, GeometryType geometryType)
    {
        if (polyStyleElement == null)
        {
            return;
        }

        var fillEnabled = !string.Equals(polyStyleElement.Element(Kml + "fill")?.Value, "0", StringComparison.OrdinalIgnoreCase);
        var outlineEnabled = !string.Equals(polyStyleElement.Element(Kml + "outline")?.Value, "0", StringComparison.OrdinalIgnoreCase);
        var fillHex = ConvertKmlColorToHex(polyStyleElement.Element(Kml + "color")?.Value);

        if (fillEnabled && !string.IsNullOrEmpty(fillHex) && IsPolygonGeometry(geometryType))
        {
            visual.Fill = BrushHelper.CreateBrush(fillHex);
        }

        if (!outlineEnabled)
        {
            visual.Stroke = null;
        }
    }

    private static void ApplyLineStyle(VisualParameters visual, XElement? lineStyleElement)
    {
        if (lineStyleElement == null)
        {
            return;
        }

        var lineHex = ConvertKmlColorToHex(lineStyleElement.Element(Kml + "color")?.Value);
        var strokeWidth = TryParseDouble(lineStyleElement.Element(Kml + "width")?.Value);

        if (!string.IsNullOrEmpty(lineHex))
        {
            visual.Stroke = BrushHelper.CreateBrush(lineHex);
        }

        if (strokeWidth.HasValue && strokeWidth.Value > 0)
        {
            visual.StrokeThickness = strokeWidth.Value;
        }
    }

    private static void ApplyIconStyle(VisualParameters visual, XElement? iconStyleElement)
    {
        if (iconStyleElement == null || visual.PointSymbol == null)
        {
            return;
        }

        var iconHex = ConvertKmlColorToHex(iconStyleElement.Element(Kml + "color")?.Value);
        var scale = TryParseDouble(iconStyleElement.Element(Kml + "scale")?.Value);

        if (!string.IsNullOrEmpty(iconHex))
        {
            visual.Fill = BrushHelper.CreateBrush(iconHex);
        }

        if (scale.HasValue && scale.Value > 0)
        {
            var size = Math.Clamp(scale.Value * visual.PointSymbol.SymbolWidth, 4, 64);
            visual.PointSymbol.SymbolWidth = size;
            visual.PointSymbol.SymbolHeight = size;
        }
    }

    private static void ApplyLabelStyle(VisualParameters visual, XElement? labelStyleElement)
    {
        if (labelStyleElement == null)
        {
            return;
        }

        var labelHex = ConvertKmlColorToHex(labelStyleElement.Element(Kml + "color")?.Value);
        var labelScale = TryParseDouble(labelStyleElement.Element(Kml + "scale")?.Value);

        if (!string.IsNullOrEmpty(labelHex))
        {
            visual.Foreground = BrushHelper.CreateBrush(labelHex);
        }

        if (labelScale.HasValue && labelScale.Value > 0)
        {
            visual.FontSize = (int)Math.Max(8, 12 * labelScale.Value);
        }
    }

    private static void EnsureDefaults(VisualParameters visual, GeometryType geometryType)
    {
        if (visual.Stroke == null && IsLineGeometry(geometryType))
        {
            visual.Stroke = BrushHelper.PickBrush();
        }

        if (visual.Fill == null && IsPolygonGeometry(geometryType))
        {
            visual.Fill = BrushHelper.PickBrush();
        }
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

    private static double? TryParseDouble(string? value)
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
