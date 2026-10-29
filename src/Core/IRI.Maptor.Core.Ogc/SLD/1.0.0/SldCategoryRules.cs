using System;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;

namespace IRI.Maptor.Core.Ogc.SLD;

/// <summary>The kind of geometry the generated rules draw.</summary>
public enum SldGeometryKind
{
    Point,
    Line,
    Polygon,
}

/// <summary>One value of the field to give its own rule.</summary>
public sealed class SldCategory
{
    /// <summary>The attribute value the rule matches (compared as invariant text).</summary>
    public object? Value { get; set; }

    /// <summary>The legend text; the value when null.</summary>
    public string? Title { get; set; }

    /// <summary>The colour ("#rrggbb", "#rgb" or "rgba(r,g,b,a)"); null takes the next palette colour.</summary>
    public string? Color { get; set; }
}

public sealed class SldCategoryOptions
{
    public string FieldName { get; set; } = string.Empty;

    public SldGeometryKind Geometry { get; set; }

    /// <summary>
    /// The style to repeat (sizes, widths, dashes, mark, opacities); only its colours change per value.
    /// Null, or a symbolizer of another geometry kind, uses a plain default.
    /// </summary>
    public Symbolizer? Template { get; set; }

    /// <summary>Add a last rule (an <see cref="ElseFilter"/>) for the features no value rule matches: empty or other values.</summary>
    public bool IncludeOtherValues { get; set; } = true;

    public string OtherValuesTitle { get; set; } = "Other values";

    public string OtherValuesColor { get; set; } = "#9e9e9e";
}

/// <summary>
/// Builds categorized symbology: one SLD rule per value of a field (a <c>PropertyIsEqualTo</c> filter and the
/// template's style in the value's colour), optionally followed by an "other values" rule.
/// </summary>
public static class SldCategoryRules
{
    /// <summary>
    /// Twelve clearly distinct colours (matplotlib's "tab10" and two dark tones, ordered so neighbours
    /// contrast); later values reuse them lighter, then darker.
    /// </summary>
    public static IReadOnlyList<string> Palette { get; } =
    [
        "#1f77b4", "#ff7f0e", "#2ca02c", "#d62728", "#9467bd", "#8c564b",
        "#e377c2", "#17becf", "#bcbd22", "#7f7f7f", "#393b79", "#637939",
    ];

    /// <summary>The palette colour of the <paramref name="index"/>-th value.</summary>
    public static string GetPaletteColor(int index)
    {
        var round = index / Palette.Count;
        var color = ParseColor(Palette[index % Palette.Count])!.Value;

        // 0: as is, 1: lighter, 2: darker, 3: lighter still, ...
        var amount = round == 0 ? 0 : (round % 2 == 1 ? 1 : -1) * Math.Min(0.6, 0.3 * ((round + 1) / 2));

        return ToHex(Shade(color.Rgb, amount));
    }

    public static List<Rule> Create(IEnumerable<SldCategory> categories, SldCategoryOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.FieldName))
            throw new ArgumentException("A field name is required.", nameof(options));

        var rules = new List<Rule>();
        var paletteIndex = 0;

        foreach (var category in categories)
        {
            var literal = Convert.ToString(category.Value, CultureInfo.InvariantCulture);

            if (literal is null)
                continue;

            var color = ParseColor(category.Color) ?? ParseColor(GetPaletteColor(paletteIndex++))!.Value;

            rules.Add(new Rule
            {
                Name = literal,
                Title = string.IsNullOrWhiteSpace(category.Title) ? literal : category.Title!,
                Filter = new OgcFilter
                {
                    Predicate = new OgcPropertyIsEqualTo { Expressions = { new OgcPropertyName { Value = options.FieldName }, new OgcLiteral { Value = literal } } },
                },
                Symbolizers = { CreateSymbolizer(options, color) },
            });
        }

        if (options.IncludeOtherValues)
        {
            rules.Add(new Rule
            {
                Name = "other",
                Title = options.OtherValuesTitle,
                ElseFilter = new ElseFilter(),
                Symbolizers = { CreateSymbolizer(options, ParseColor(options.OtherValuesColor) ?? ParseColor("#9e9e9e")!.Value) },
            });
        }

        return rules;
    }

    private static Symbolizer CreateSymbolizer(SldCategoryOptions options, (string Rgb, double? Opacity) color)
    {
        var outline = ToHex(Shade(color.Rgb, -0.35));

        switch (options.Geometry)
        {
            case SldGeometryKind.Polygon:
            {
                var template = options.Template as PolygonSymbolizer;

                var fill = new Fill { CssParameters = CopyParameters(template?.Fill?.CssParameters) };
                SetParameter(fill.CssParameters, SldHelper.CssParameter_Fill, color.Rgb);

                if (color.Opacity.HasValue || fill.GetParameter(SldHelper.CssParameter_FillOpacity) is null)
                    SetParameter(fill.CssParameters, SldHelper.CssParameter_FillOpacity, Format(color.Opacity ?? 0.7));

                // a template without an outline keeps none
                Stroke? stroke = null;

                if (template is null || template.Stroke is not null)
                {
                    stroke = new Stroke { CssParameters = CopyParameters(template?.Stroke?.CssParameters) };
                    SetParameter(stroke.CssParameters, SldHelper.CssParameter_Stroke, outline);

                    if (stroke.GetParameter(SldHelper.CssParameter_StrokeWidth) is null)
                        SetParameter(stroke.CssParameters, SldHelper.CssParameter_StrokeWidth, "1");
                }

                return new PolygonSymbolizer { Fill = fill, Stroke = stroke! };
            }

            case SldGeometryKind.Line:
            {
                var template = options.Template as LineSymbolizer;

                var stroke = new Stroke { CssParameters = CopyParameters(template?.Stroke?.CssParameters) };
                SetParameter(stroke.CssParameters, SldHelper.CssParameter_Stroke, color.Rgb);

                if (color.Opacity.HasValue)
                    SetParameter(stroke.CssParameters, SldHelper.CssParameter_StrokeOpacity, Format(color.Opacity.Value));

                if (stroke.GetParameter(SldHelper.CssParameter_StrokeWidth) is null)
                    SetParameter(stroke.CssParameters, SldHelper.CssParameter_StrokeWidth, "2");

                return new LineSymbolizer { Stroke = stroke };
            }

            default:
            {
                var template = options.Template as PointSymbolizer;
                var templateMark = template?.Graphic?.Marks?.FirstOrDefault();

                var mark = new Mark
                {
                    WellKnownName = templateMark?.WellKnownName ?? nameof(WellKnownMark.circle),
                    Fill = new Fill { CssParameters = CopyParameters(templateMark?.Fill?.CssParameters) },
                    Stroke = new Stroke { CssParameters = CopyParameters(templateMark?.Stroke?.CssParameters) },
                };

                SetParameter(mark.Fill.CssParameters, SldHelper.CssParameter_Fill, color.Rgb);

                if (color.Opacity.HasValue)
                    SetParameter(mark.Fill.CssParameters, SldHelper.CssParameter_FillOpacity, Format(color.Opacity.Value));

                SetParameter(mark.Stroke.CssParameters, SldHelper.CssParameter_Stroke, outline);

                var graphic = new Graphic
                {
                    Size = template?.Graphic?.Size ?? 10,
                    Opacity = template?.Graphic?.Opacity,
                    Rotation = template?.Graphic?.Rotation,
                };
                graphic.Marks.Add(mark);

                return new PointSymbolizer { Graphic = graphic };
            }
        }
    }

    private static List<CssParameter> CopyParameters(IEnumerable<CssParameter>? parameters) =>
        parameters?.Select(p => new CssParameter { Name = p.Name, Value = p.Value }).ToList() ?? new List<CssParameter>();

    private static void SetParameter(List<CssParameter> parameters, string name, string value)
    {
        var existing = parameters.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

        if (existing is null)
            parameters.Add(new CssParameter { Name = name, Value = value });
        else
            existing.Value = value;
    }

    private static string Format(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    /// <summary>"#rrggbb", "#rgb" or "rgba(r,g,b,a)" / "rgb(r,g,b)" as a hex colour and an optional opacity; null when unreadable.</summary>
    public static (string Rgb, double? Opacity)? ParseColor(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        text = text!.Trim();

        if (text.StartsWith("#", StringComparison.Ordinal))
        {
            var hex = text.Substring(1);

            if (hex.Length == 3)
                hex = string.Concat(hex.Select(c => new string(c, 2)));

            return hex.Length == 6 && int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _)
                ? ("#" + hex.ToLowerInvariant(), null)
                : null;
        }

        var open = text.IndexOf('(');
        var close = text.LastIndexOf(')');

        if (open < 0 || close < open || !text.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
            return null;

        var parts = text.Substring(open + 1, close - open - 1).Split(',').Select(p => p.Trim()).ToArray();

        if (parts.Length < 3 || !parts.Take(3).All(p => int.TryParse(p, NumberStyles.Integer, CultureInfo.InvariantCulture, out var c) && c is >= 0 and <= 255))
            return null;

        double? opacity = null;

        if (parts.Length >= 4 && double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var alpha) && alpha < 1)
            opacity = Math.Max(0, alpha);

        return (ToHex(parts.Take(3).Select(p => int.Parse(p, CultureInfo.InvariantCulture)).ToArray()), opacity);
    }

    // amount > 0 mixes towards white, < 0 towards black
    private static int[] Shade(string hex, double amount)
    {
        var rgb = Enumerable.Range(0, 3).Select(i => Convert.ToInt32(hex.Substring(1 + i * 2, 2), 16)).ToArray();

        return rgb.Select(c => (int)Math.Round(amount >= 0 ? c + (255 - c) * amount : c * (1 + amount))).ToArray();
    }

    private static string ToHex(int[] rgb) => "#" + string.Concat(rgb.Select(c => Math.Max(0, Math.Min(255, c)).ToString("x2", CultureInfo.InvariantCulture)));
}
