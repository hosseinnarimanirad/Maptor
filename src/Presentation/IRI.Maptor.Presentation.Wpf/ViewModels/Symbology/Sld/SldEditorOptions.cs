using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Media;

using IRI.Maptor.Core.Ogc.SLD;
using IRI.Maptor.Presentation.Core.Localization;

namespace IRI.Maptor.Presentation.Wpf.ViewModels.Symbology;

/// <summary>One choice in an SLD editor list: the value written to the SLD and its name in the user's language.</summary>
public sealed class SldOption
{
    public SldOption(object value, string name, string? symbol = null)
    {
        Value = value;
        Name = name;
        Symbol = symbol;
    }

    public object Value { get; }

    public string Name { get; }

    /// <summary>A short sign shown beside the name (the filter operators' =, ≠, &lt; …).</summary>
    public string? Symbol { get; }

    public override string ToString() => Name;
}

/// <summary>A named stroke-dasharray, with the dashes in stroke widths for the on-screen sample.</summary>
public sealed class SldDashPreset
{
    public SldDashPreset(string name, string pattern)
    {
        Name = name;
        Pattern = pattern;

        // SLD dashes are in pixels, a WPF sample's in multiples of its stroke width (2 here)
        Dashes = new DoubleCollection(ParsePattern(pattern).Select(d => d / 2));
    }

    public string Name { get; }

    /// <summary>The stroke-dasharray as written to the SLD; empty for a solid line.</summary>
    public string Pattern { get; }

    public DoubleCollection Dashes { get; }

    public override string ToString() => Pattern;

    private static IEnumerable<double> ParsePattern(string pattern)
    {
        foreach (var part in pattern.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (double.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                yield return value;
        }
    }
}

/// <summary>
/// The fixed choice lists of the SLD editor, named in the current UI language. The values are what the
/// symbolizer view models already read and write, so the lists change only what the user sees.
/// </summary>
public static class SldEditorOptions
{
    private static string L(string key) => LocalizationManager.Instance[key];

    public static IReadOnlyList<SldOption> Marks => new[]
    {
        new SldOption(WellKnownMark.circle, L("sldEditor_mark_circle")),
        new SldOption(WellKnownMark.square, L("sldEditor_mark_square")),
        new SldOption(WellKnownMark.triangle, L("sldEditor_mark_triangle")),
        new SldOption(WellKnownMark.star, L("sldEditor_mark_star")),
        new SldOption(WellKnownMark.cross, L("sldEditor_mark_cross")),
        new SldOption(WellKnownMark.x, L("sldEditor_mark_x")),
    };

    public static IReadOnlyList<SldOption> LineCaps => new[]
    {
        new SldOption(Sld_StrokeLineCap.Butt, L("sldEditor_cap_butt")),
        new SldOption(Sld_StrokeLineCap.Round, L("sldEditor_cap_round")),
        new SldOption(Sld_StrokeLineCap.Square, L("sldEditor_cap_square")),
    };

    public static IReadOnlyList<SldOption> LineJoins => new[]
    {
        new SldOption(Sld_StrokeLineJoin.Mitre, L("sldEditor_join_mitre")),
        new SldOption(Sld_StrokeLineJoin.Round, L("sldEditor_join_round")),
        new SldOption(Sld_StrokeLineJoin.Bevel, L("sldEditor_join_bevel")),
    };

    public static IReadOnlyList<SldOption> FilterOperators => new[]
    {
        new SldOption(FilterComparisonOperator.Equal, L("sldEditor_op_equal"), "="),
        new SldOption(FilterComparisonOperator.NotEqual, L("sldEditor_op_notEqual"), "≠"),
        new SldOption(FilterComparisonOperator.LessThan, L("sldEditor_op_lessThan"), "<"),
        new SldOption(FilterComparisonOperator.LessThanOrEqual, L("sldEditor_op_lessThanOrEqual"), "≤"),
        new SldOption(FilterComparisonOperator.GreaterThan, L("sldEditor_op_greaterThan"), ">"),
        new SldOption(FilterComparisonOperator.GreaterThanOrEqual, L("sldEditor_op_greaterThanOrEqual"), "≥"),
    };

    public static IReadOnlyList<SldDashPreset> DashPresets => new[]
    {
        new SldDashPreset(L("sldEditor_dash_solid"), ""),
        new SldDashPreset(L("sldEditor_dash_dashed"), "8 4"),
        new SldDashPreset(L("sldEditor_dash_longDash"), "16 6"),
        new SldDashPreset(L("sldEditor_dash_dotted"), "2 4"),
        new SldDashPreset(L("sldEditor_dash_dashDot"), "10 4 2 4"),
    };

    private static IReadOnlyList<string>? _fontFamilies;

    /// <summary>The installed font families by name (read once); the label font box suggests these.</summary>
    public static IReadOnlyList<string> FontFamilies => _fontFamilies ??= Fonts.SystemFontFamilies
        .Select(f => f.Source)
        .Where(name => !string.IsNullOrWhiteSpace(name))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
        .ToList();
}
