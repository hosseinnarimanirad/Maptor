using System.Collections.Generic;

using IRI.Maptor.Sta.Ogc.SLD;
using IRI.Maptor.Jab.Common.Helpers;

using Imaging = System.Windows.Media.Imaging;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

namespace IRI.Maptor.Jab.Common.Cartography.Legend;

/// <summary>
/// Flattens a <see cref="StyledLayerDescriptor"/> into a <see cref="SymbologyLegend"/> — one group
/// per FeatureTypeStyle, one row per <see cref="Rule"/> — rendering each rule's symbol swatch and
/// formatting its filter / scale range as text. Drives both the WPF details panel and PNG export.
/// </summary>
public static class SldLegendBuilder
{
    public static SymbologyLegend Build(StyledLayerDescriptor? sld, SldLegendOptions? options = null)
    {
        options ??= SldLegendOptions.Default;

        var legend = new SymbologyLegend();

        if (sld is null)
            return legend;

        if (sld.NamedLayers is not null)
        {
            foreach (var namedLayer in sld.NamedLayers)
                AddLayer(legend, namedLayer.Name, namedLayer.UserStyles, options);
        }

        if (sld.UserLayers is not null)
        {
            foreach (var userLayer in sld.UserLayers)
                AddLayer(legend, userLayer.Name, userLayer.UserStyles, options);
        }

        return legend;
    }

    private static void AddLayer(SymbologyLegend legend, string? layerName, List<UserStyle>? userStyles, SldLegendOptions options)
    {
        if (userStyles is null)
            return;

        foreach (var userStyle in userStyles)
        {
            if (userStyle.FeatureTypeStyles is null)
                continue;

            foreach (var featureTypeStyle in userStyle.FeatureTypeStyles)
            {
                var group = new LegendStyleGroup
                {
                    Header = FirstNonEmpty(featureTypeStyle.Title, userStyle.Title, userStyle.Name, layerName)
                };

                if (featureTypeStyle.Rules is not null)
                {
                    foreach (var rule in featureTypeStyle.Rules)
                        group.Rows.Add(BuildRow(rule, options));
                }

                legend.Groups.Add(group);
            }
        }
    }

    private static LegendRuleRow BuildRow(Rule rule, SldLegendOptions options)
    {
        var row = new LegendRuleRow
        {
            Title = FirstNonEmpty(rule.Title, rule.Name) ?? "(unnamed)",
            FilterText = FilterTextFormatter.Format(rule.Filter),
            ScaleText = FilterTextFormatter.FormatScale(rule.MinScaleDenominator, rule.MaxScaleDenominator),
            Rule = rule
        };

        using (var bitmap = LegendSwatchFactory.CreateSwatchBitmap(rule, options))
        {
            row.SwatchImage = ToFrozenBitmapImage(bitmap);
        }

        return row;
    }

    private static Imaging.BitmapImage? ToFrozenBitmapImage(System.Drawing.Bitmap bitmap)
    {
        var image = ImageUtility.CreateBitmapImage(bitmap, ImageFormat.Png);

        if (image != null && image.CanFreeze)
            image.Freeze();

        return image;
    }

    private static string? FirstNonEmpty(params string?[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (!string.IsNullOrWhiteSpace(candidate))
                return candidate;
        }

        return null;
    }
}
