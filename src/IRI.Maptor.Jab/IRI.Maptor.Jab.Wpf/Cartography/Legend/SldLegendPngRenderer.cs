using System;
using System.Collections.Generic;
using System.IO;

using IRI.Maptor.Sta.Ogc.SLD;

using Drawing = System.Drawing;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

namespace IRI.Maptor.Jab.Wpf.Cartography.Legend;

/// <summary>
/// Composes a <see cref="SymbologyLegend"/> into a single legend image (one row per rule: a symbol
/// swatch, a title, and optional filter / scale captions, grouped by style). This is the "export /
/// print" counterpart of the in-app WPF panel; both share <see cref="LegendSwatchFactory"/>.
/// </summary>
public static class SldLegendPngRenderer
{
    private sealed class Line
    {
        public bool IsHeader;
        public string? HeaderText;
        public LegendRuleRow? Row;
        public string? Caption;

        public float Height;
        public float TitleHeight;
        public float CaptionHeight;
        public float ContentWidth;
    }

    public static Drawing.Bitmap RenderToBitmap(StyledLayerDescriptor? sld, SldLegendOptions? options = null)
    {
        options ??= SldLegendOptions.Default;
        return RenderToBitmap(SldLegendBuilder.Build(sld, options), options);
    }

    public static byte[] RenderToPngBytes(StyledLayerDescriptor? sld, SldLegendOptions? options = null)
        => RenderToPngBytes(SldLegendBuilder.Build(sld, options ?? SldLegendOptions.Default), options);

    public static void RenderToFile(StyledLayerDescriptor? sld, string pngPath, SldLegendOptions? options = null)
        => RenderToFile(SldLegendBuilder.Build(sld, options ?? SldLegendOptions.Default), pngPath, options);

    public static byte[] RenderToPngBytes(SymbologyLegend legend, SldLegendOptions? options = null)
    {
        using var bitmap = RenderToBitmap(legend, options);
        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }

    public static void RenderToFile(SymbologyLegend legend, string pngPath, SldLegendOptions? options = null)
    {
        using var bitmap = RenderToBitmap(legend, options);
        bitmap.Save(pngPath, ImageFormat.Png);
    }

    public static Drawing.Bitmap RenderToBitmap(SymbologyLegend legend, SldLegendOptions? options = null)
    {
        options ??= SldLegendOptions.Default;

        var lines = Flatten(legend, options);

        if (lines.Count == 0)
            return new Drawing.Bitmap(1, 1);

        using var titleFont = new Drawing.Font(options.FontFamily, options.FontSize);
        using var captionFont = new Drawing.Font(options.FontFamily, Math.Max(6f, options.FontSize - 2f));
        using var headerFont = new Drawing.Font(options.FontFamily, options.FontSize + 1f, Drawing.FontStyle.Bold);

        // Measure pass.
        float maxContentWidth;
        float totalHeight;
        using (var measureBitmap = new Drawing.Bitmap(1, 1))
        using (var measure = Drawing.Graphics.FromImage(measureBitmap))
        {
            Measure(lines, measure, options, titleFont, captionFont, headerFont, out maxContentWidth, out totalHeight);
        }

        int width = (int)Math.Ceiling(options.Padding * 2 + maxContentWidth);
        int height = (int)Math.Ceiling(options.Padding * 2 + totalHeight);

        var result = new Drawing.Bitmap(Math.Max(1, width), Math.Max(1, height));

        using (var graphics = Drawing.Graphics.FromImage(result))
        {
            graphics.Clear(options.Background);
            graphics.SmoothingMode = Drawing.Drawing2D.SmoothingMode.AntiAlias;
            graphics.InterpolationMode = Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.TextRenderingHint = Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Compose(result, graphics, lines, options, titleFont, captionFont, headerFont);
        }

        return result;
    }

    private static List<Line> Flatten(SymbologyLegend legend, SldLegendOptions options)
    {
        var lines = new List<Line>();

        if (legend?.Groups is null)
            return lines;

        if (!string.IsNullOrWhiteSpace(options.Title))
            lines.Add(new Line { IsHeader = true, HeaderText = options.Title });

        foreach (var group in legend.Groups)
        {
            if (options.ShowGroupHeaders && !string.IsNullOrWhiteSpace(group.Header))
                lines.Add(new Line { IsHeader = true, HeaderText = group.Header });

            foreach (var row in group.Rows)
                lines.Add(new Line { IsHeader = false, Row = row, Caption = BuildCaption(row, options) });
        }

        return lines;
    }

    private static void Measure(
        List<Line> lines, Drawing.Graphics measure, SldLegendOptions options,
        Drawing.Font titleFont, Drawing.Font captionFont, Drawing.Font headerFont,
        out float maxContentWidth, out float totalHeight)
    {
        maxContentWidth = 0f;
        totalHeight = 0f;

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];

            if (line.IsHeader)
            {
                var size = measure.MeasureString(line.HeaderText, headerFont);
                line.Height = size.Height;
                line.ContentWidth = size.Width;
            }
            else
            {
                var titleSize = measure.MeasureString(line.Row!.Title, titleFont);
                line.TitleHeight = titleSize.Height;

                float textWidth = titleSize.Width;

                if (!string.IsNullOrWhiteSpace(line.Caption))
                {
                    var captionSize = measure.MeasureString(line.Caption, captionFont);
                    line.CaptionHeight = captionSize.Height;
                    textWidth = Math.Max(textWidth, captionSize.Width);
                }

                float textHeight = line.TitleHeight + line.CaptionHeight;
                line.Height = Math.Max(options.SwatchHeight, textHeight);
                line.ContentWidth = options.SwatchWidth + options.SwatchTextGap + textWidth;
            }

            maxContentWidth = Math.Max(maxContentWidth, line.ContentWidth);
            totalHeight += line.Height;

            if (i < lines.Count - 1)
                totalHeight += options.RowSpacing;
        }
    }

    private static void Compose(
        Drawing.Bitmap target, Drawing.Graphics graphics, List<Line> lines, SldLegendOptions options,
        Drawing.Font titleFont, Drawing.Font captionFont, Drawing.Font headerFont)
    {
        bool rtl = options.IsRtl;

        using var titleBrush = new Drawing.SolidBrush(options.TextColor);
        using var captionBrush = new Drawing.SolidBrush(Blend(options.TextColor, options.Background, 0.45f));

        var format = new Drawing.StringFormat();
        if (rtl)
            format.FormatFlags = Drawing.StringFormatFlags.DirectionRightToLeft;

        float padding = options.Padding;
        float y = padding;

        float swatchX = rtl
            ? target.Width - padding - options.SwatchWidth
            : padding;

        float textLeft = rtl
            ? padding
            : padding + options.SwatchWidth + options.SwatchTextGap;

        float textAreaWidth = rtl
            ? swatchX - options.SwatchTextGap - padding
            : target.Width - padding - textLeft;

        textAreaWidth = Math.Max(1f, textAreaWidth);

        foreach (var line in lines)
        {
            if (line.IsHeader)
            {
                var headerRect = new Drawing.RectangleF(padding, y, target.Width - 2 * padding, line.Height);
                graphics.DrawString(line.HeaderText, headerFont, titleBrush, headerRect, format);
                y += line.Height + options.RowSpacing;
                continue;
            }

            var row = line.Row!;

            // Swatch, vertically centered within the row.
            float swatchY = y + (line.Height - options.SwatchHeight) / 2f;
            using (var swatch = row.Rule is null ? null : LegendSwatchFactory.CreateSwatchBitmap(row.Rule, options))
            {
                if (swatch is not null)
                    graphics.DrawImage(swatch, swatchX, swatchY, options.SwatchWidth, options.SwatchHeight);
            }

            // Text block, vertically centered within the row.
            float textBlockHeight = line.TitleHeight + line.CaptionHeight;
            float textTop = y + (line.Height - textBlockHeight) / 2f;

            var titleRect = new Drawing.RectangleF(textLeft, textTop, textAreaWidth, line.TitleHeight);
            graphics.DrawString(row.Title, titleFont, titleBrush, titleRect, format);

            if (!string.IsNullOrWhiteSpace(line.Caption))
            {
                var captionRect = new Drawing.RectangleF(textLeft, textTop + line.TitleHeight, textAreaWidth, line.CaptionHeight);
                graphics.DrawString(line.Caption, captionFont, captionBrush, captionRect, format);
            }

            y += line.Height + options.RowSpacing;
        }

        format.Dispose();
    }

    private static string? BuildCaption(LegendRuleRow row, SldLegendOptions options)
    {
        var parts = new List<string>();

        if (options.ShowFieldText && !string.IsNullOrWhiteSpace(row.FieldText))
            parts.Add(row.FieldText!);

        if (options.ShowScaleText && !string.IsNullOrWhiteSpace(row.ScaleText))
            parts.Add(row.ScaleText!);

        return parts.Count == 0 ? null : string.Join("  ·  ", parts);
    }

    private static Drawing.Color Blend(Drawing.Color a, Drawing.Color b, float t)
    {
        // b may be transparent (e.g. transparent background); fall back to gray in that case.
        if (b.A == 0)
            b = Drawing.Color.Gray;

        int Mix(int x, int y) => (int)Math.Round(x * (1 - t) + y * t);

        return Drawing.Color.FromArgb(255, Mix(a.R, b.R), Mix(a.G, b.G), Mix(a.B, b.B));
    }
}
