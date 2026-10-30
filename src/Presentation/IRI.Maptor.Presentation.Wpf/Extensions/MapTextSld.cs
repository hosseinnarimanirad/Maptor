using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Media;

using IRI.Maptor.Core.Ogc.SLD;
using IRI.Maptor.Presentation.Core.Models.MapText;
using IRI.Maptor.Presentation.Wpf.Converters;
using IRI.Maptor.Presentation.Wpf.Helpers;

namespace IRI.Maptor.Extensions;

/// <summary>
/// Two-way conversion between a text-on-map label's <see cref="MapTextStyle"/> and SLD, so text
/// drawings are saved like geometry drawings (GeoJSON point + embedded SLD). The text itself is
/// not style: it is a feature attribute, referenced by the TextSymbolizer's Label.
/// <para>
/// Standard SLD 1.0: font family/size/weight/style, text fill, Halo (Halo frame only) and
/// AnchorPoint 0.5/0 (the label's bottom-centre sits on the point, like the map marker).
/// The rest is VendorOptions, which other engines ignore: underlineText (GeoServer), and
/// maptor:frame, maptor:textAlign, maptor:direction, maptor:background.
/// </para>
/// </summary>
public static class MapTextSld
{
    /// <summary>The feature attribute holding the label text (the same one geometry drawings label with).</summary>
    public const string LabelAttribute = "Label";

    public const string VendorUnderline = "underlineText";
    public const string VendorFrame = "maptor:frame";
    public const string VendorTextAlign = "maptor:textAlign";
    public const string VendorDirection = "maptor:direction";
    public const string VendorBackground = "maptor:background";

    private const string ThemeBackground = "theme";
    private const string Rtl = "rtl";
    private const string Ltr = "ltr";
    private const string HaloRadius = "2";

    public static StyledLayerDescriptor ToSld(MapTextStyle style, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(style);

        var textColor = ColorHelper.TryToWpfColor(style.TextColor) ?? Colors.Black;

        var symbolizer = new TextSymbolizer
        {
            Label = new Label { PropertyName = LabelAttribute },
            Font = new Font
            {
                CssParameters =
                [
                    new CssParameter { Name = SldHelper.CssParameter_FontFamily, Value = style.FontFamilyName },
                    new CssParameter { Name = SldHelper.CssParameter_FontSize, Value = style.FontSize.ToString("0.##", CultureInfo.InvariantCulture) },
                    new CssParameter { Name = SldHelper.CssParameter_FontStyle, Value = style.IsItalic ? "italic" : "normal" },
                    new CssParameter { Name = SldHelper.CssParameter_FontWeight, Value = style.IsBold ? "bold" : "normal" },
                ]
            },
            Fill = CreateFill(textColor),
            LabelPlacement = new LabelPlacement
            {
                PointPlacement = new PointPlacement
                {
                    AnchorPoint = new AnchorPoint
                    {
                        AnchorPointX = new ParameterValueType { Value = "0.5" },
                        AnchorPointY = new ParameterValueType { Value = "0" },
                    }
                }
            },
            VendorOptions =
            [
                new VendorOption { Name = VendorFrame, Value = style.Frame.ToString() },
                new VendorOption { Name = VendorTextAlign, Value = style.Alignment.ToString() },
                new VendorOption { Name = VendorDirection, Value = style.IsRightToLeft ? Rtl : Ltr },
                new VendorOption { Name = VendorBackground, Value = string.IsNullOrWhiteSpace(style.BackgroundColor) ? ThemeBackground : style.BackgroundColor },
            ]
        };

        if (style.IsUnderline)
            symbolizer.VendorOptions.Add(new VendorOption { Name = VendorUnderline, Value = "true" });

        // the halo colour is written out (automatic → its contrast colour) so other tools draw it too
        if (style.Frame == MapTextFrame.Halo)
        {
            var haloColor = ColorHelper.TryToWpfColor(style.HaloColor) ?? ColorToContrastBrushConverter.GetContrastColor(textColor);

            symbolizer.Halo = new Halo
            {
                Radius = new ParameterValueType { Value = HaloRadius },
                Fill = CreateFill(haloColor),
            };
        }

        var sld = new StyledLayerDescriptor();

        sld.NamedLayers.Add(new NamedLayer
        {
            Name = name ?? "Text",
            UserStyles =
            [
                new UserStyle
                {
                    FeatureTypeStyles = [new FeatureTypeStyle { Rules = [new Rule { Symbolizers = [symbolizer] }] }]
                }
            ]
        });

        return sld;
    }

    /// <summary>
    /// The text style of a text drawing's SLD: one whose drawn style holds only TextSymbolizers
    /// (geometry drawings always carry a point/line/polygon symbolizer). Null for anything else.
    /// Missing or unreadable parts fall back to the <see cref="MapTextStyle"/> defaults, so an SLD
    /// written by another tool (standard parts only) still loads.
    /// </summary>
    public static MapTextStyle? TryParse(StyledLayerDescriptor? sld)
    {
        var symbolizers = GetDrawnSymbolizers(sld);

        if (symbolizers.Count == 0 || symbolizers.Any(s => s is not TextSymbolizer))
            return null;

        var text = (TextSymbolizer)symbolizers[0];
        var style = new MapTextStyle();

        var fontFamily = text.Font?.GetParameter(SldHelper.CssParameter_FontFamily)?.Value;
        if (!string.IsNullOrWhiteSpace(fontFamily))
            style.FontFamilyName = fontFamily.Trim();

        var fontSize = text.Font?.GetParameter(SldHelper.CssParameter_FontSize)?.Value;
        if (double.TryParse(fontSize, NumberStyles.Float, CultureInfo.InvariantCulture, out var size) && double.IsFinite(size) && size > 0)
            style.FontSize = size;

        style.IsBold = Is(text.Font?.GetParameter(SldHelper.CssParameter_FontWeight)?.Value, "bold");
        style.IsItalic = Is(text.Font?.GetParameter(SldHelper.CssParameter_FontStyle)?.Value, "italic")
                      || Is(text.Font?.GetParameter(SldHelper.CssParameter_FontStyle)?.Value, "oblique");

        var textColor = ReadFill(text.Fill) ?? Colors.Black;
        style.TextColor = textColor.ToString();

        var options = text.VendorOptions ?? [];
        string? Option(string optionName) => options.LastOrDefault(o => string.Equals(o.Name, optionName, StringComparison.OrdinalIgnoreCase))?.Value?.Trim();

        style.IsUnderline = Is(Option(VendorUnderline), "true");

        // no frame option (another tool): a halo means Halo, else just the text
        style.Frame = Enum.TryParse<MapTextFrame>(Option(VendorFrame), true, out var frame) && Enum.IsDefined(frame)
            ? frame
            : (text.Halo is not null ? MapTextFrame.Halo : MapTextFrame.Plain);

        if (Enum.TryParse<MapTextAlignment>(Option(VendorTextAlign), true, out var alignment) && Enum.IsDefined(alignment))
            style.Alignment = alignment;

        var direction = Option(VendorDirection);
        if (Is(direction, Rtl) || Is(direction, Ltr))
            style.IsRightToLeft = Is(direction, Rtl);

        var background = Option(VendorBackground);
        style.BackgroundColor = string.IsNullOrWhiteSpace(background) || Is(background, ThemeBackground) ? null : background;

        // a halo colour equal to the automatic one stays automatic, so it keeps following the text colour
        var haloColor = ReadFill(text.Halo?.Fill);
        style.HaloColor = haloColor is null || haloColor == ColorToContrastBrushConverter.GetContrastColor(textColor) ? null : haloColor.ToString();

        return style;
    }

    /// <summary>The attribute the label text is read from (the TextSymbolizer's Label), "Label" when absent.</summary>
    public static string GetLabelAttribute(StyledLayerDescriptor? sld)
    {
        var propertyName = GetDrawnSymbolizers(sld).OfType<TextSymbolizer>().FirstOrDefault()?.Label?.PropertyName;

        return string.IsNullOrWhiteSpace(propertyName) ? LabelAttribute : propertyName.Trim();
    }

    // the symbolizers of the style that is drawn (named layers and user layers, default style),
    // the same selection ParseToSymbolizers makes
    private static List<Symbolizer> GetDrawnSymbolizers(StyledLayerDescriptor? sld)
    {
        if (sld is null)
            return [];

        var userStyles = sld.NamedLayers.Select(n => SldStyleExtensions.SelectStyle(n.UserStyles, null))
            .Concat(sld.UserLayers.Select(u => SldStyleExtensions.SelectStyle(u.UserStyles, null)));

        return userStyles.Where(s => s is not null)
            .SelectMany(s => s!.FeatureTypeStyles ?? [])
            .SelectMany(f => f.Rules ?? [])
            .SelectMany(r => r.Symbolizers ?? [])
            .Where(s => s is not null)
            .ToList();
    }

    // the same fill + fill-opacity pair the geometry symbolizers write
    private static Fill CreateFill(Color color) => SldExtensions.CreateFill(new SolidColorBrush(color));

    // null when the fill or its colour is missing or unreadable
    private static Color? ReadFill(Fill? fill)
    {
        if (fill?.GetParameter(SldHelper.CssParameter_Fill) is null)
            return null;

        return ColorHelper.TryToWpfColor(fill.FillValue, fill.FillOpacityValue);
    }

    private static bool Is(string? value, string expected) => string.Equals(value?.Trim(), expected, StringComparison.OrdinalIgnoreCase);
}
