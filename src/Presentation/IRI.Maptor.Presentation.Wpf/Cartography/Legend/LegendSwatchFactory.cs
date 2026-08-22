using System.Collections.Generic;
using System.Linq;

using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Ogc.SLD;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Presentation.Wpf.Cartography.Symbologies;

using Drawing = System.Drawing;
using Point = IRI.Maptor.Core.Common.Primitives.Point;

namespace IRI.Maptor.Presentation.Wpf.Cartography.Legend;

/// <summary>
/// Renders a single SLD <see cref="Rule"/> to a symbol swatch bitmap by reusing the headless
/// <see cref="GdiBitmapRenderStrategy"/>. A synthetic feature (in swatch-pixel coordinates) is fed
/// to the renderer per geometry kind, and the rule's filter / scale range are neutralized so the
/// swatch always draws (the filter/scale are surfaced as text elsewhere).
/// </summary>
internal static class LegendSwatchFactory
{
    /// <summary>
    /// Builds a swatch <see cref="Drawing.Bitmap"/> for the rule. Never returns null: on failure or
    /// an unsupported/empty rule it returns a transparent cell. Caller owns disposal of the bitmap.
    /// </summary>
    public static Drawing.Bitmap CreateSwatchBitmap(Rule rule, SldLegendOptions options)
    {
        int w = options.SwatchWidth;
        int h = options.SwatchHeight;

        var parsed = rule.ParseWithSource();

        if (parsed.Count == 0)
            return CreateBlank(w, h);

        // Prefer the geometry (line/polygon/point) symbolizers for the swatch; fall back to text.
        // A rule may stack several (e.g. a wide casing line under a narrow core line), so all of
        // them are composited in order rather than only the first.
        var geometryPairs = parsed.Where(p => p.sldSymbolizer is not TextSymbolizer && p.symbolizer is not null).ToList();

        if (geometryPairs.Count == 0)
        {
            var textPair = parsed.FirstOrDefault(p => p.sldSymbolizer is TextSymbolizer);
            return CreateTextSwatch(textPair.symbolizer as LabelSymbolizer, w, h, options);
        }

        foreach (var pair in geometryPairs)
        {
            // Neutralize filter + scale so the synthetic (attribute-less) feature is never dropped.
            pair.symbolizer.IsFilterPassed = _ => true;
            pair.symbolizer.MinScaleDenominator = null;
            pair.symbolizer.MaxScaleDenominator = null;
        }

        var feature = CreateSyntheticFeature(geometryPairs[0].sldSymbolizer, w, h, options.SwatchPadding);

        if (feature is null)
            return CreateBlank(w, h);

        var bitmap = new GdiBitmapRenderStrategy(geometryPairs.Select(p => p.symbolizer).ToList())
            .AsGdiBitmap(new List<Feature<Point>> { feature }, mapScale: 1.0, w, h);

        return bitmap ?? CreateBlank(w, h);
    }

    /// <summary>
    /// A synthetic feature in swatch-pixel coordinates (origin top-left, Y down) matching the
    /// geometry kind of the SLD symbolizer. Returns null for kinds without a geometry swatch.
    /// </summary>
    private static Feature<Point>? CreateSyntheticFeature(Symbolizer sldSymbolizer, int w, int h, int p)
    {
        double midY = h / 2.0;

        switch (sldSymbolizer)
        {
            case LineSymbolizer:
                // Horizontal line with a slight mid dip to reveal joins / caps.
                var linePoints = new List<Point>
                {
                    new Point(p, midY),
                    new Point(w / 2.0, midY - 2),
                    new Point(w - p, midY)
                };
                return new Feature<Point>(Geometry<Point>.Create(linePoints, GeometryType.LineString, 0));

            case PolygonSymbolizer:
                var ring = new List<Point>
                {
                    new Point(p, p),
                    new Point(w - p, p),
                    new Point(w - p, h - p),
                    new Point(p, h - p),
                    new Point(p, p)
                };
                return new Feature<Point>(Geometry<Point>.CreatePolygon(ring, 0));

            case PointSymbolizer:
                return new Feature<Point>(Geometry<Point>.Create(w / 2.0, h / 2.0, 0));

            default:
                return null;
        }
    }

    /// <summary>Draws a short sample string ("Abc") using the label's font / fill.</summary>
    private static Drawing.Bitmap CreateTextSwatch(LabelSymbolizer? label, int w, int h, SldLegendOptions options)
    {
        var bitmap = new Drawing.Bitmap(w, h);

        using (var graphics = Drawing.Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = Drawing.Drawing2D.SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var param = label?.Param;

            string family = param?.FontFamily?.FamilyNames?.Values?.FirstOrDefault() ?? options.FontFamily;
            float size = param is not null ? param.FontSize : options.FontSize;
            Drawing.Brush brush = (param?.Foreground)?.AsGdiBrush() ?? new Drawing.SolidBrush(options.TextColor);

            using (var font = new Drawing.Font(family, size, Drawing.FontStyle.Bold))
            {
                const string sample = "Abc";
                var stringSize = graphics.MeasureString(sample, font);
                var location = new Drawing.PointF(
                    (float)((w - stringSize.Width) / 2.0),
                    (float)((h - stringSize.Height) / 2.0));

                graphics.DrawString(sample, font, brush, location);
            }

            brush.Dispose();
        }

        return bitmap;
    }

    /// <summary>A transparent swatch cell (default 32bpp ARGB bitmaps are fully transparent).</summary>
    private static Drawing.Bitmap CreateBlank(int w, int h) => new Drawing.Bitmap(w, h);
}
