using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Ogc.SLD;
using IRI.Maptor.Sta.Pdf;
using IRI.Maptor.Sta.Spatial.IO.Dxf;
using IRI.Maptor.Jab.Common.Cartography.Legend;
using IRI.Maptor.Jab.Common.Cartography.Symbologies;
using IRI.Maptor.Jab.Common.Layers;

using Drawing = System.Drawing;

namespace IRI.Maptor.Jab.Common.Helpers;

/// <summary>
/// Builds the printed legend column's content (<see cref="PdfLegendGroup"/> per layer) from the
/// layers' <see cref="SymbolizableLayer.SymbologyLegend"/>. Labels are rendered as vector glyph
/// outlines (IranSans, crisp, RTL-safe) and swatches as opaque PNGs — PdfSharpCore mis-renders
/// images carrying an SMask, so transparency is flattened onto white here.
/// </summary>
public static class PdfLegendHelper
{
    // One size for everything (layer names and rule titles): text is drawn at a fixed px→point
    // scale in the PDF, so this IS the printed size. Long titles wrap up to MaxTitleLines within
    // their paper width and are then ellipsized, rather than being shrunk to fit.
    private const double LegendFontSizePx = 10;
    private const int MaxTitleLines = 2;

    /// <summary>
    /// Builds one legend group per layer that has rows visible at the printed scale. A layer with
    /// a single (simple) symbology collapses to one headerless line — swatch + layer name; label
    /// (text) rules are skipped, since a text sample says nothing useful in a printed legend.
    /// </summary>
    /// <param name="topFirstLayers">Visible-in-scale layers, topmost (highest ZIndex) first.</param>
    /// <param name="mapScale">Fractional map scale (e.g. 1/25000); the rules' scale denominator is 1/mapScale.</param>
    public static List<PdfLegendGroup> BuildLegendGroups(IEnumerable<SymbolizableLayer> topFirstLayers, double mapScale)
    {
        var result = new List<PdfLegendGroup>();
        var scaleDenominator = 1.0 / mapScale;

        foreach (var layer in topFirstLayers)
        {
            var legend = layer.SymbologyLegend;

            if (legend.IsEmpty)
                continue;

            var rows = legend.Groups
                .SelectMany(g => g.Rows)
                .Where(r => !IsTextOnlyRule(r.Rule) && IsRuleVisibleAtScale(r.Rule, scaleDenominator))
                .ToList();

            if (rows.Count == 0)
                continue;

            if (rows.Count == 1)
            {
                // Simple symbology: one line — the swatch in front of the layer name, no header.
                result.Add(new PdfLegendGroup
                {
                    Entries =
                    {
                        BuildEntry(rows[0].Rule, layer.LayerName),
                    },
                });

                continue;
            }

            var group = new PdfLegendGroup
            {
                HeaderVector = RenderTitle(layer.LayerName, PdfLegendMetrics.HeaderMaxWidthPx),
                HeaderVectorNarrow = RenderTitle(layer.LayerName, PdfLegendMetrics.NarrowHeaderMaxWidthPx),
            };

            foreach (var row in rows)
                group.Entries.Add(BuildEntry(row.Rule, RuleTitle(row.Rule)));

            result.Add(group);
        }

        return result;
    }

    /// <summary>
    /// One legend row: the rule's symbol (vector when we can express it, else a raster fallback)
    /// plus its title in both flow widths.
    /// </summary>
    private static PdfLegendEntry BuildEntry(Rule? rule, string title)
    {
        var vector = rule is null ? null : BuildVectorSwatch(rule);

        return new PdfLegendEntry
        {
            SwatchVector = vector,

            // Only pay for the GDI raster when the vector path couldn't express the symbology.
            SwatchPngBytes = vector is null && rule is not null ? RenderOpaqueSwatchPng(rule) : null,

            LabelVector = RenderTitle(title, PdfLegendMetrics.LabelMaxWidthPx),
            LabelVectorNarrow = RenderTitle(title, PdfLegendMetrics.NarrowLabelMaxWidthPx),
        };
    }

    /// <summary>
    /// Builds the rule's symbol as PDF vector art — one part per geometry symbolizer, in order
    /// (so a wide casing line still sits under its narrow core line), carrying the same colors,
    /// stroke thickness and point marker the map draws. Returns null when the rule has no
    /// geometry symbolizer to express, leaving the caller to fall back to a raster swatch.
    /// </summary>
    private static PdfLegendSwatch? BuildVectorSwatch(Rule rule)
    {
        try
        {
            var swatch = new PdfLegendSwatch();

            foreach (var (symbolizer, sldSymbolizer) in rule.ParseWithSource())
            {
                if (sldSymbolizer is TextSymbolizer || symbolizer?.Param is null)
                    continue;

                var shape = sldSymbolizer switch
                {
                    LineSymbolizer => PdfLegendSwatchShape.Line,
                    PolygonSymbolizer => PdfLegendSwatchShape.Polygon,
                    PointSymbolizer => PdfLegendSwatchShape.Point,
                    _ => (PdfLegendSwatchShape?)null,
                };

                if (shape is null)
                    continue;

                var param = symbolizer.Param;
                var pointSymbol = param.PointSymbol;

                swatch.Parts.Add(new PdfLegendSwatchPart
                {
                    Shape = shape.Value,
                    Fill = ToRgbColor(param.Fill),
                    Stroke = ToRgbColor(param.Stroke),

                    // Screen px -> PDF points, matching how map symbology is printed.
                    StrokeWidth = param.StrokeThickness * PdfLegendMetrics.PxToPoint,
                    Opacity = param.Opacity,
                    Marker = shape == PdfLegendSwatchShape.Point ? PdfDecorationHelper.BuildPointMarker(pointSymbol) : null,
                    PointRadius = pointSymbol is null
                        ? 3.0
                        : Math.Max(pointSymbol.SymbolWidth, pointSymbol.SymbolHeight) / 2.0 * PdfLegendMetrics.PxToPoint,
                });
            }

            return swatch.IsValid ? swatch : null;
        }
        catch
        {
            return null;
        }
    }

    private static RgbColor? ToRgbColor(System.Windows.Media.Brush? brush)
    {
        var color = brush?.AsSolidColor();

        return color.HasValue ? new RgbColor(color.Value.R, color.Value.G, color.Value.B, color.Value.A) : null;
    }

    /// <summary>
    /// Renders a title at the single legend font size, wrapped/ellipsized to its paper width.
    /// </summary>
    private static PdfVectorLogo? RenderTitle(string text, double maxWidthPx) =>
        PdfDecorationHelper.RenderTextToVector(text, LegendFontSizePx, maxWidthPx: maxWidthPx, maxLines: MaxTitleLines);

    /// <summary>
    /// The rule's display title; empty when the rule is unnamed — the printed legend shows the
    /// bare swatch rather than an "(unnamed)" placeholder.
    /// </summary>
    private static string RuleTitle(Rule? rule)
    {
        if (!string.IsNullOrWhiteSpace(rule?.Title))
            return rule!.Title;

        if (!string.IsNullOrWhiteSpace(rule?.Name))
            return rule!.Name;

        return string.Empty;
    }

    /// <summary>A rule carrying nothing but TextSymbolizers — i.e. a label style.</summary>
    private static bool IsTextOnlyRule(Rule? rule) =>
        rule is { Symbolizers.Count: > 0 } && rule.Symbolizers.All(s => s is TextSymbolizer);

    /// <summary>
    /// Same denominator semantics as <c>BaseLayer.CanRenderLayer</c>: a bound only applies when
    /// present and positive.
    /// </summary>
    private static bool IsRuleVisibleAtScale(Rule? rule, double scaleDenominator)
    {
        if (rule is null)
            return true;

        return scaleDenominator >= (rule.MinScaleDenominator is > 0 ? rule.MinScaleDenominator.Value : 0) &&
               scaleDenominator <= (rule.MaxScaleDenominator is > 0 ? rule.MaxScaleDenominator.Value : double.MaxValue);
    }

    /// <summary>
    /// Re-renders the rule's swatch and flattens it onto white as a 24bpp (alpha-free) PNG, so
    /// PdfSharpCore never sees an SMask. Null on render failure (the row still prints its label).
    /// </summary>
    private static byte[]? RenderOpaqueSwatchPng(Rule rule)
    {
        try
        {
            using var swatch = LegendSwatchFactory.CreateSwatchBitmap(rule, SldLegendOptions.Default);

            using var opaque = new Drawing.Bitmap(swatch.Width, swatch.Height, Drawing.Imaging.PixelFormat.Format24bppRgb);

            using (var graphics = Drawing.Graphics.FromImage(opaque))
            {
                graphics.Clear(Drawing.Color.White);
                graphics.DrawImageUnscaled(swatch, 0, 0);
            }

            using var stream = new MemoryStream();
            opaque.Save(stream, Drawing.Imaging.ImageFormat.Png);
            return stream.ToArray();
        }
        catch
        {
            return null;
        }
    }
}
