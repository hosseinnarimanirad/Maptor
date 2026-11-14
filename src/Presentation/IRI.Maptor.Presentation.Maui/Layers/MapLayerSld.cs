using System.Globalization;

using IRI.Maptor.Core.Ogc.SLD;

using Microsoft.Maui.Graphics;

using SldStroke = IRI.Maptor.Core.Ogc.SLD.Stroke;
using SldFill = IRI.Maptor.Core.Ogc.SLD.Fill;

namespace IRI.Maptor.Presentation.Maui.Layers;

/// <summary>
/// Applies an SLD 1.0 style to a <see cref="MapLayer"/>. A MapLayer has one style for all its features, so only a
/// <i>single</i> style is applied: the default user style with exactly one rule and no filter. A categorized style
/// (several rules, or filters) is refused rather than painting every feature like its first category; the caller keeps
/// its own colours then.
/// </summary>
public static class MapLayerSld
{
    /// <summary>
    /// Parses <paramref name="sldXml"/> and applies it to <paramref name="layer"/>; false (layer unchanged) when it is
    /// empty, malformed or not a single style.
    /// </summary>
    public static bool TryApply(MapLayer layer, string? sldXml)
    {
        if (string.IsNullOrWhiteSpace(sldXml) || !SldHelper.TryParse(sldXml, out var sld, out _) || sld is null)
        {
            return false;
        }

        return TryApply(layer, sld);
    }

    public static bool TryApply(MapLayer layer, StyledLayerDescriptor sld)
    {
        var styles = sld.NamedLayers.SelectMany(l => l.UserStyles).Concat(sld.UserLayers.SelectMany(l => l.UserStyles)).ToList();

        var style = styles.FirstOrDefault(s => s.IsDefault == true) ?? styles.FirstOrDefault();

        var rules = style?.FeatureTypeStyles.SelectMany(f => f.Rules).ToList();

        if (rules is not { Count: 1 } || rules[0].Filter is not null || rules[0].ElseFilter is not null)
        {
            return false;
        }

        bool applied = false;

        foreach (var symbolizer in rules[0].Symbolizers)
        {
            switch (symbolizer)
            {
                case PointSymbolizer point:
                    applied |= ApplyPoint(layer, point);
                    break;

                case LineSymbolizer line:
                    applied |= ApplyStroke(layer, line.Stroke);
                    break;

                case PolygonSymbolizer polygon:
                    applied |= ApplyFill(layer, polygon.Fill);
                    applied |= ApplyStroke(layer, polygon.Stroke);
                    break;
            }
        }

        return applied;
    }

    private static bool ApplyPoint(MapLayer layer, PointSymbolizer point)
    {
        var graphic = point.Graphic;

        if (graphic is null)
        {
            return false;
        }

        if (graphic.Size is int size and > 0)
        {
            layer.PointSize = size;
        }

        if (graphic.Marks.FirstOrDefault() is not { } mark)
        {
            return graphic.Size is > 0;
        }

        layer.PointShape = mark.WellKnownName?.Trim().ToLowerInvariant() switch
        {
            "square" => PointShape.Square,
            "triangle" => PointShape.Triangle,
            "circle" => PointShape.Circle,
            null or "" => PointShape.Circle,
            // star, cross, x, ...: no such symbol here; a diamond stays distinct from the plain shapes.
            _ => PointShape.Diamond,
        };

        if (ReadColor(mark.Fill?.GetParameter("fill")?.Value, mark.Fill?.GetParameter("fill-opacity")?.Value) is { } fill)
        {
            layer.FillColor = fill;
            layer.Color = fill;
        }

        if (ReadColor(mark.Stroke?.GetParameter("stroke")?.Value, mark.Stroke?.GetParameter("stroke-opacity")?.Value) is { } outline)
        {
            layer.OutlineColor = outline;
        }

        if (ReadNumber(mark.Stroke?.GetParameter("stroke-width")?.Value) is { } outlineWidth)
        {
            layer.OutlineWidth = outlineWidth;
        }

        return true;
    }

    private static bool ApplyStroke(MapLayer layer, SldStroke? stroke)
    {
        if (stroke is null)
        {
            return false;
        }

        bool applied = false;

        if (ReadColor(stroke.GetParameter("stroke")?.Value, stroke.GetParameter("stroke-opacity")?.Value) is { } color)
        {
            layer.Color = color;
            applied = true;
        }

        var width = ReadNumber(stroke.GetParameter("stroke-width")?.Value);

        if (width is > 0)
        {
            layer.StrokeWidth = width.Value;
            applied = true;
        }

        // SLD dashes are pixels; MapLayer's are multiples of the stroke width.
        if (stroke.GetParameter("stroke-dasharray")?.Value is { Length: > 0 } dashes)
        {
            var unit = width is > 0 ? width.Value : layer.StrokeWidth;

            var pattern = dashes.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(d => ReadNumber(d))
                .Where(d => d is > 0)
                .Select(d => (float)(d!.Value / Math.Max(unit, 0.1)))
                .ToArray();

            if (pattern.Length >= 2)
            {
                layer.DashPattern = pattern;
                applied = true;
            }
        }

        return applied;
    }

    private static bool ApplyFill(MapLayer layer, SldFill? fill)
    {
        if (ReadColor(fill?.GetParameter("fill")?.Value, fill?.GetParameter("fill-opacity")?.Value) is not { } color)
        {
            return false;
        }

        layer.FillColor = color;
        return true;
    }

    /// <summary>"#RRGGBB" (SLD's form) with an optional 0..1 opacity; null for anything else.</summary>
    private static Color? ReadColor(string? hex, string? opacity)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return null;
        }

        try
        {
            var color = Color.FromArgb(hex.Trim());

            return ReadNumber(opacity) is { } alpha ? color.WithAlpha((float)Math.Clamp(alpha, 0, 1)) : color;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static double? ReadNumber(string? text)
        => double.TryParse(text?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : null;
}
