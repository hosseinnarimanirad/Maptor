using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Helpers;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace IRI.Maptor.Sta.Pdf;

/// <summary>
/// Draws the map-layout decorations (graticule, neat line, title block, scale bar, logos)
/// around/over the map frame. Runs after all map layers, outside any optional content
/// group, so decorations stay visible regardless of PDF layer toggles.
/// </summary>
internal static class PdfMapComposer
{
    private const double LabelFontSize = 7;
    private const double TitleFontSize = 12;
    private const double CompanyTitleFontSize = 11;
    private const double CompanySubtitleFontSize = 9;
    private const double TickLength = 3;
    private const double CompanyLogoBox = 64;

    public static void DrawDecorations(
        XGraphics gfx,
        PdfMapLayout layout,
        in MapPageTransform transform,
        PdfMapDecorations decorations)
    {
        var labelFamily = EmbeddedFontResolver.Register(decorations.LabelFontBytes, decorations.LabelFontFamily);
        var unicode = new XPdfFontOptions(PdfFontEncoding.Unicode);
        var labelFont = new XFont(labelFamily, LabelFontSize, XFontStyle.Regular, unicode);

        var centerGeodetic = MapProjects.WebMercatorToGeodeticWgs84(transform.MapExtent.Center);
        var cosCenterLatitude = Math.Cos(centerGeodetic.Y * Math.PI / 180.0);

        // Graticule LINES are drawn earlier (between the basemap and the vector layers) by
        // WriteLayers; here we only add the edge ticks + labels, which sit on top in the margin.
        if (decorations.ShowGraticule)
            DrawGraticuleTicksAndLabels(gfx, layout.MapFrameRect, transform, decorations.GraticuleIntervalDegrees, labelFont, decorations.UsePersianDigits);

        DrawNeatLine(gfx, layout.MapFrameRect);

        // Center column: map title in a small bordered box atop the map.
        if (layout.HasTitleBox)
            DrawTitleBox(gfx, layout.TitleBoxRect, decorations, labelFamily, unicode);

        // Legend column (left in LTR, right in RTL): reserved box (header + empty body for now).
        if (layout.HasLegendColumn)
            DrawLegendColumn(gfx, layout.LegendColumnRect, decorations);

        // Company column: logo, then company title + subtitle.
        if (layout.HasCompanyColumn)
            DrawCompanyColumn(gfx, layout.CompanyColumnRect, decorations, labelFamily, unicode);

        // Bottom band: date/time (under legend), scale bar (centered under map), Maptor logo (under company).
        if (layout.HasBottomBand)
        {
            if (decorations.ShowScaleBar)
                DrawScaleBar(gfx, layout.ScaleBarCellRect, transform.Scale, cosCenterLatitude, labelFont, decorations.UsePersianDigits);

            if (layout.DateTimeCellRect.Width > 0 &&
                (decorations.DateTimeVector is { IsValid: true } || !string.IsNullOrWhiteSpace(decorations.DateTimeText)))
                DrawDateTime(gfx, layout.DateTimeCellRect, decorations, labelFont);

            if (layout.MaptorCellRect.Width > 0)
            {
                if (decorations.PrimaryVectorLogo is { IsValid: true } vectorLogo)
                    DrawPoweredByVectorLogo(gfx, vectorLogo, layout.MaptorCellRect);
                else if (decorations.PrimaryLogoPngBytes is { Length: > 0 })
                    DrawPoweredByLogo(gfx, decorations.PrimaryLogoPngBytes, layout.MaptorCellRect);
            }
        }
    }

    private static readonly XColor _brandGray = XColor.FromArgb(255, 128, 128, 128);

    /// <summary>
    /// Draws the producer brand mark as vector, gray, centered and small in the given cell.
    /// </summary>
    private static void DrawPoweredByVectorLogo(XGraphics gfx, PdfVectorLogo logo, XRect cell)
    {
        const double padding = 6;

        var targetHeight = (cell.Height - 2 * padding) * 0.7;
        if (targetHeight <= 0)
            return;

        var scale = targetHeight / logo.SourceHeight;
        var targetWidth = logo.SourceWidth * scale;

        var originX = cell.Left + (cell.Width - targetWidth) / 2;
        var originY = cell.Top + (cell.Height - targetHeight) / 2;

        FillVectorFigures(gfx, logo, originX, originY, scale, _brandGray);
    }

    // px (1/96") -> points (1/72"): keep printed labels at their on-screen physical size.
    private const double LabelPxToPoint = 72.0 / 96.0;

    // Nudge labels up-and-right of the anchor so the text clears the point marker instead of
    // sitting dead-centered on it (standard point-label placement).
    private const double LabelOffset = 3.0;

    private static readonly XColor _labelBackground = XColor.FromArgb(200, 255, 255, 255);

    /// <summary>
    /// Draws a feature label as vector glyph outlines placed just above-right of <paramref name="anchor"/>
    /// (page coordinates), behind a translucent-white readability box.
    /// </summary>
    internal static void DrawFeatureLabel(XGraphics gfx, PdfVectorLogo glyphs, XPoint anchor, IRI.Maptor.Sta.Spatial.IO.Dxf.RgbColor? color)
    {
        if (!glyphs.IsValid)
            return;

        var w = glyphs.SourceWidth * LabelPxToPoint;
        var h = glyphs.SourceHeight * LabelPxToPoint;

        // Right of the point, vertically centered, then lifted slightly so it reads as up-right.
        var originX = anchor.X + LabelOffset;
        var originY = anchor.Y - h / 2 - LabelOffset;

        const double pad = 1.5;
        gfx.DrawRectangle(new XSolidBrush(_labelBackground), new XRect(originX - pad, originY - pad, w + 2 * pad, h + 2 * pad));

        var fill = color is { } c ? XColor.FromArgb(c.A, c.R, c.G, c.B) : XColors.Black;
        FillVectorFigures(gfx, glyphs, originX, originY, LabelPxToPoint, fill);
    }

    /// <summary>
    /// Fills a vector logo/text's figures as an even-odd path (so glyph holes render) at the given
    /// page origin and scale, in the given color.
    /// </summary>
    private static void FillVectorFigures(XGraphics gfx, PdfVectorLogo vector, double originX, double originY, double scale, XColor color)
    {
        var path = new XGraphicsPath { FillMode = XFillMode.Alternate };

        foreach (var figure in vector.Figures)
        {
            if (figure.Points.Count < 2)
                continue;

            var pts = figure.Points
                .Select(p => new XPoint(originX + p.X * scale, originY + p.Y * scale))
                .ToArray();

            path.AddPolygon(pts);
        }

        gfx.DrawPath(new XSolidBrush(color), path);
    }

    /// <summary>
    /// Draws vector-outline text scaled to fit (<paramref name="width"/> × <paramref name="maxHeight"/>),
    /// aligned horizontally within the width, its top at <paramref name="top"/>. Returns the bottom Y.
    /// </summary>
    private static double DrawVectorText(XGraphics gfx, PdfVectorLogo text, double left, double top, double width, double maxHeight, XStringAlignment hAlign)
    {
        if (!text.IsValid)
            return top;

        var scale = Math.Min(width / text.SourceWidth, maxHeight / text.SourceHeight);
        var w = text.SourceWidth * scale;
        var h = text.SourceHeight * scale;

        var x = hAlign switch
        {
            XStringAlignment.Near => left,
            XStringAlignment.Far => left + width - w,
            _ => left + (width - w) / 2,
        };

        FillVectorFigures(gfx, text, x, top, scale, XColors.Black);
        return top + h;
    }

    /// <summary>
    /// Draws vector-outline text centered vertically inside a box, capped at <paramref name="maxHeight"/>.
    /// </summary>
    private static void DrawVectorTextInBox(XGraphics gfx, PdfVectorLogo text, XRect box, double padding, XStringAlignment hAlign, double maxHeight)
    {
        if (!text.IsValid)
            return;

        var availW = Math.Max(1, box.Width - 2 * padding);
        var availH = Math.Max(1, Math.Min(box.Height - 2 * padding, maxHeight));

        var scale = Math.Min(availW / text.SourceWidth, availH / text.SourceHeight);
        var h = text.SourceHeight * scale;
        var top = box.Top + (box.Height - h) / 2;

        DrawVectorText(gfx, text, box.Left + padding, top, availW, h, hAlign);
    }

    /// <summary>
    /// Draws the dashed gray graticule lines across the map face, clipped to the frame. Called
    /// between the basemap and the vector layers so the grid sits above the tiles but below data.
    /// </summary>
    public static void DrawGraticuleLines(XGraphics gfx, XRect frame, in MapPageTransform transform, double? intervalDegrees)
    {
        var graticule = GraticuleHelper.Create(transform.MapExtent, intervalDegrees);

        // Absolute dash pattern (in 0.6pt-pen units → ~1.8pt on/1.8pt off) so the lines print
        // clearly dashed; PdfSharpCore scales DashStyle by width, which reads near-solid at 0.5pt.
        var linePen = new XPen(XColor.FromArgb(255, 120, 120, 120), 0.6)
        {
            DashStyle = XDashStyle.Dash,
            DashPattern = new double[] { 3, 3 }
        };
        var transformCopy = transform;

        gfx.Save();
        gfx.IntersectClip(frame);

        foreach (var line in graticule.Meridians.Concat(graticule.Parallels))
        {
            var points = line.WebMercatorPoints.Select(p => transformCopy.ToPage(p)).ToArray();

            if (points.Length >= 2)
                gfx.DrawLines(linePen, points);
        }

        gfx.Restore();
    }

    private static void DrawGraticuleTicksAndLabels(
        XGraphics gfx,
        XRect frame,
        in MapPageTransform transform,
        double? intervalDegrees,
        XFont labelFont,
        bool usePersianDigits)
    {
        var graticule = GraticuleHelper.Create(transform.MapExtent, intervalDegrees);

        var tickPen = new XPen(XColors.Black, 0.6);
        var transformCopy = transform;

        // Edge ticks + labels: meridians top/bottom, parallels left/right.
        foreach (var meridian in graticule.Meridians)
        {
            var x = transformCopy.ToPage(meridian.WebMercatorPoints[0]).X;

            if (x < frame.Left - 0.1 || x > frame.Right + 0.1)
                continue;

            gfx.DrawLine(tickPen, x, frame.Top, x, frame.Top - TickLength);
            gfx.DrawLine(tickPen, x, frame.Bottom, x, frame.Bottom + TickLength);

            var label = Localize(meridian.Label, usePersianDigits);
            var labelSize = gfx.MeasureString(label, labelFont);
            gfx.DrawString(label, labelFont, XBrushes.Black,
                new XPoint(x - labelSize.Width / 2, frame.Top - TickLength - 2));
            gfx.DrawString(label, labelFont, XBrushes.Black,
                new XPoint(x - labelSize.Width / 2, frame.Bottom + TickLength + 2 + labelSize.Height * 0.8));
        }

        foreach (var parallel in graticule.Parallels)
        {
            var y = transformCopy.ToPage(parallel.WebMercatorPoints[0]).Y;

            if (y < frame.Top - 0.1 || y > frame.Bottom + 0.1)
                continue;

            gfx.DrawLine(tickPen, frame.Left, y, frame.Left - TickLength, y);
            gfx.DrawLine(tickPen, frame.Right, y, frame.Right + TickLength, y);

            // Latitude labels are rotated to vertical so they occupy only their glyph height
            // horizontally (≈ one line) — keeps them inside the graticule margin and clear of the
            // side columns, instead of the wide horizontal text that could reach the legend border.
            var label = Localize(parallel.Label, usePersianDigits);
            var labelSize = gfx.MeasureString(label, labelFont);
            var halfHeight = labelSize.Height / 2;

            // Left edge reads bottom-to-top (-90); right edge reads top-to-bottom (+90).
            DrawVerticalEdgeLabel(gfx, label, labelFont, frame.Left - TickLength - 2 - halfHeight, y, -90);
            DrawVerticalEdgeLabel(gfx, label, labelFont, frame.Right + TickLength + 2 + halfHeight, y, 90);
        }
    }

    /// <summary>
    /// Draws a graticule label rotated to vertical (<paramref name="angle"/> = -90 reads bottom-to-top,
    /// +90 reads top-to-bottom), centered on (<paramref name="centerX"/>, <paramref name="centerY"/>).
    /// Stays embedded-font PDF text, so it prints crisp/vector.
    /// </summary>
    private static void DrawVerticalEdgeLabel(XGraphics gfx, string text, XFont font, double centerX, double centerY, double angle)
    {
        gfx.Save();
        gfx.RotateAtTransform(angle, new XPoint(centerX, centerY));
        gfx.DrawString(text, font, XBrushes.Black, new XPoint(centerX, centerY), XStringFormats.Center);
        gfx.Restore();
    }

    /// <summary>
    /// Converts ASCII digits (0–9) to Persian/Farsi digits (۰–۹) when <paramref name="usePersian"/>.
    /// Only touches digit characters — degree/minute/second signs and hemisphere letters pass through.
    /// </summary>
    private static string Localize(string text, bool usePersian)
    {
        if (!usePersian || string.IsNullOrEmpty(text))
            return text;

        var chars = text.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (chars[i] >= '0' && chars[i] <= '9')
                chars[i] = (char)(chars[i] - '0' + 0x06F0);
        }
        return new string(chars);
    }

    private static void DrawNeatLine(XGraphics gfx, XRect frame)
    {
        var outer = new XRect(frame.X - 2.5, frame.Y - 2.5, frame.Width + 5, frame.Height + 5);

        gfx.DrawRectangle(new XPen(XColors.Black, 1.2), outer);
        gfx.DrawRectangle(new XPen(XColors.Black, 0.4), frame);
    }

    /// <summary>
    /// Draws the map title centered in a small bordered box atop the map column.
    /// </summary>
    private static void DrawTitleBox(
        XGraphics gfx,
        XRect box,
        PdfMapDecorations decorations,
        string labelFamily,
        XPdfFontOptions unicode)
    {
        gfx.DrawRectangle(new XPen(XColors.Black, 0.8), box);

        if (decorations.TitleVector is { IsValid: true } titleVector)
        {
            DrawVectorTextInBox(gfx, titleVector, box, padding: 3, XStringAlignment.Center, maxHeight: 16);
        }
        else if (decorations.TitlePngBytes is { Length: > 0 })
        {
            DrawImageCentered(gfx, decorations.TitlePngBytes, box, padding: 3);
        }
        else if (!string.IsNullOrWhiteSpace(decorations.TitleText))
        {
            var titleFont = new XFont(labelFamily, TitleFontSize, XFontStyle.Bold, unicode);
            gfx.DrawString(decorations.TitleText, titleFont, XBrushes.Black, box, XStringFormats.Center);
        }
    }

    /// <summary>
    /// Draws the reserved legend column: a bordered box with a header at the top and an
    /// (intentionally) empty body. Legend items are added in a later step.
    /// </summary>
    private static void DrawLegendColumn(XGraphics gfx, XRect column, PdfMapDecorations decorations)
    {
        if (column.Width <= 0 || column.Height <= 0)
            return;

        var pen = new XPen(XColors.Black, 0.8);
        gfx.DrawRectangle(pen, column);

        var headerHeight = Math.Min(22, column.Height);
        var headerBox = new XRect(column.Left + 4, column.Top + 3, Math.Max(1, column.Width - 8), Math.Max(1, headerHeight - 6));

        if (decorations.LegendHeaderVector is { IsValid: true } legendVector)
        {
            DrawVectorTextInBox(gfx, legendVector, headerBox, padding: 0, XStringAlignment.Center, maxHeight: headerBox.Height);
        }
        else if (decorations.LegendHeaderPngBytes is { Length: > 0 })
        {
            DrawImageContainedInBox(gfx, decorations.LegendHeaderPngBytes, headerBox);
        }

        // Divider under the header.
        var dividerY = column.Top + headerHeight;
        if (dividerY < column.Bottom)
            gfx.DrawLine(pen, column.Left, dividerY, column.Right, dividerY);
    }

    /// <summary>
    /// Draws the company column top-down: logo, then company title, then company subtitle.
    /// </summary>
    private static void DrawCompanyColumn(
        XGraphics gfx,
        XRect column,
        PdfMapDecorations decorations,
        string labelFamily,
        XPdfFontOptions unicode)
    {
        const double padding = 6;

        var left = column.Left + padding;
        var width = Math.Max(1, column.Width - 2 * padding);
        var y = column.Top + padding;

        if (decorations.SecondaryLogoPngBytes is { Length: > 0 })
        {
            var side = Math.Min(CompanyLogoBox, width);
            var box = new XRect(column.Left + (column.Width - side) / 2, y, side, side);
            DrawImageContainedInBox(gfx, decorations.SecondaryLogoPngBytes, box);
            y = box.Bottom + 8;
        }

        y = DrawColumnTextElement(gfx, decorations.CompanyTitleVector, decorations.CompanyTitlePngBytes, decorations.CompanyTitleText,
            left, y, width, maxHeight: 18, labelFamily, CompanyTitleFontSize, XFontStyle.Bold, unicode);

        DrawColumnTextElement(gfx, decorations.CompanySubtitleVector, decorations.CompanySubtitlePngBytes, decorations.CompanySubtitleText,
            left, y + 4, width, maxHeight: 14, labelFamily, CompanySubtitleFontSize, XFontStyle.Regular, unicode);
    }

    /// <summary>
    /// Draws a column text element — vector glyph outlines (preferred, crisp/RTL-safe), else a
    /// pre-rendered raster, else PDF-text fallback — centered horizontally in <paramref name="width"/>
    /// starting at <paramref name="top"/>. Returns the bottom Y so callers can stack the next element.
    /// </summary>
    private static double DrawColumnTextElement(
        XGraphics gfx, PdfVectorLogo? vector, byte[]? png, string? text,
        double left, double top, double width, double maxHeight,
        string labelFamily, double fontSize, XFontStyle fontStyle, XPdfFontOptions unicode)
    {
        if (vector is { IsValid: true })
            return DrawVectorText(gfx, vector, left, top, width, maxHeight, XStringAlignment.Center);

        if (png is { Length: > 0 })
        {
            try
            {
                using var image = XImage.FromStream(() => new MemoryStream(png));
                var scale = Math.Min(width / image.PixelWidth, maxHeight / image.PixelHeight);
                var w = image.PixelWidth * scale;
                var h = image.PixelHeight * scale;
                var x = left + (width - w) / 2;
                gfx.DrawImage(image, x, top, w, h);
                return top + h;
            }
            catch
            {
                // Unreadable image bytes: fall through to text (or nothing).
            }
        }

        if (!string.IsNullOrWhiteSpace(text))
        {
            var font = new XFont(labelFamily, fontSize, fontStyle, unicode);
            var area = new XRect(left, top, width, font.Height);
            gfx.DrawString(text, font, XBrushes.Black, area, XStringFormats.TopCenter);
            return top + font.Height;
        }

        return top;
    }

    /// <summary>
    /// Draws the export date/time left-aligned, vertically centered in the cell: vector glyph
    /// outlines when available (crisp), else embedded-font PDF text.
    /// </summary>
    private static void DrawDateTime(XGraphics gfx, XRect cell, PdfMapDecorations decorations, XFont labelFont)
    {
        if (decorations.DateTimeVector is { IsValid: true } vector)
        {
            DrawVectorTextInBox(gfx, vector, cell, padding: 2, XStringAlignment.Near, maxHeight: 9);
            return;
        }

        var text = decorations.DateTimeText!;
        var size = gfx.MeasureString(text, labelFont);
        var x = cell.Left + 2;
        var y = cell.Top + cell.Height / 2 + size.Height * 0.3;

        gfx.DrawString(text, labelFont, XBrushes.Black, new XPoint(x, y));
    }

    private static void DrawScaleBar(
        XGraphics gfx,
        XRect cell,
        double pointsPerMercatorMeter,
        double cosCenterLatitude,
        XFont labelFont,
        bool usePersianDigits)
    {
        var (barWidth, _, rawLabel) = PdfScaleBarHelper.Choose(pointsPerMercatorMeter, cosCenterLatitude);
        var label = Localize(rawLabel, usePersianDigits);

        const double barHeight = 4;
        const int segments = 4;

        // Centered horizontally under the map column.
        var barLeft = cell.Left + Math.Max(0, (cell.Width - barWidth) / 2);
        var barTop = cell.Top + cell.Height / 2 - barHeight / 2;

        var borderPen = new XPen(XColors.Black, 0.5);
        var segmentWidth = barWidth / segments;

        for (var i = 0; i < segments; i++)
        {
            var rect = new XRect(barLeft + i * segmentWidth, barTop, segmentWidth, barHeight);

            gfx.DrawRectangle(borderPen, i % 2 == 0 ? XBrushes.Black : XBrushes.White, rect);
        }

        // "0" over the left end, round ground length over the right end.
        var zero = Localize("0", usePersianDigits);
        var zeroSize = gfx.MeasureString(zero, labelFont);
        gfx.DrawString(zero, labelFont, XBrushes.Black, new XPoint(barLeft - zeroSize.Width / 2, barTop - 3));

        var labelSize = gfx.MeasureString(label, labelFont);
        gfx.DrawString(label, labelFont, XBrushes.Black, new XPoint(barLeft + barWidth - labelSize.Width / 2, barTop - 3));

        // Representative fraction under the bar, rounded to 3 significant digits.
        var denominator = PdfScaleBarHelper.GetPaperScaleDenominator(pointsPerMercatorMeter, cosCenterLatitude);
        var magnitude = Math.Pow(10, Math.Max(0, Math.Floor(Math.Log10(denominator)) - 2));
        var rounded = Math.Round(denominator / magnitude) * magnitude;
        var ratioText = Localize(FormattableString.Invariant($"1 : {rounded:N0}"), usePersianDigits);
        var ratioSize = gfx.MeasureString(ratioText, labelFont);

        gfx.DrawString(ratioText, labelFont, XBrushes.Black,
            new XPoint(barLeft + barWidth / 2 - ratioSize.Width / 2, barTop + barHeight + 3 + ratioSize.Height * 0.8));
    }

    /// <summary>
    /// Draws the small gray "powered by Maptor" brand mark centered in the given cell, no border.
    /// </summary>
    private static void DrawPoweredByLogo(XGraphics gfx, byte[] imageBytes, XRect cell)
    {
        const double padding = 6;

        try
        {
            using var image = XImage.FromStream(() => new MemoryStream(imageBytes));

            // Deliberately small — a brand mark, not a headline element.
            var height = (cell.Height - 2 * padding) * 0.7;
            if (height <= 0)
                return;

            var width = height * image.PixelWidth / image.PixelHeight;
            var x = cell.Left + (cell.Width - width) / 2;
            var y = cell.Top + (cell.Height - height) / 2;

            gfx.DrawImage(image, x, y, width, height);
        }
        catch
        {
            // Unreadable logo bytes: skip rather than fail the export.
        }
    }

    /// <summary>
    /// Draws the image scaled to fit inside the box (contain, aspect-preserved), centered.
    /// </summary>
    private static void DrawImageContainedInBox(XGraphics gfx, byte[] imageBytes, XRect box)
    {
        try
        {
            using var image = XImage.FromStream(() => new MemoryStream(imageBytes));

            var scale = Math.Min(box.Width / image.PixelWidth, box.Height / image.PixelHeight);
            var width = image.PixelWidth * scale;
            var height = image.PixelHeight * scale;

            var x = box.Left + (box.Width - width) / 2;
            var y = box.Top + (box.Height - height) / 2;

            gfx.DrawImage(image, x, y, width, height);
        }
        catch
        {
            // Unreadable logo bytes: skip rather than fail the export.
        }
    }

    private static void DrawImageCentered(XGraphics gfx, byte[] imageBytes, XRect area, double padding)
    {
        try
        {
            using var image = XImage.FromStream(() => new MemoryStream(imageBytes));

            var height = area.Height - 2 * padding;
            var width = height * image.PixelWidth / image.PixelHeight;

            if (width > area.Width - 2 * padding)
            {
                width = area.Width - 2 * padding;
                height = width * image.PixelHeight / image.PixelWidth;
            }

            var rect = new XRect(
                area.Left + (area.Width - width) / 2,
                area.Top + (area.Height - height) / 2,
                width,
                height);

            gfx.DrawImage(image, rect);
        }
        catch
        {
            // Unreadable image bytes: skip the title image rather than fail the export.
        }
    }
}