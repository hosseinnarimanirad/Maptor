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
    private const double TitleFontSize = 16;
    private const double TickLength = 3;

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

        if (decorations.ShowGraticule)
            DrawGraticule(gfx, layout.MapFrameRect, transform, decorations.GraticuleIntervalDegrees, labelFont);

        DrawNeatLine(gfx, layout.MapFrameRect);

        if (layout.HasTitleBand)
            DrawTitleBlock(gfx, layout.TitleBandRect, decorations, labelFamily, unicode);

        if (decorations.ShowScaleBar && layout.HasFooterBand)
            DrawScaleBar(gfx, layout.FooterBandRect, transform.Scale, cosCenterLatitude, labelFont);

        if (decorations.PrimaryLogoPngBytes is { Length: > 0 } && layout.HasFooterBand)
            DrawImageRightAligned(gfx, decorations.PrimaryLogoPngBytes, layout.FooterBandRect, padding: 4);
    }

    private static void DrawGraticule(
        XGraphics gfx,
        XRect frame,
        in MapPageTransform transform,
        double? intervalDegrees,
        XFont labelFont)
    {
        var graticule = GraticuleHelper.Create(transform.MapExtent, intervalDegrees);

        var linePen = new XPen(XColor.FromArgb(255, 130, 130, 130), 0.5) { DashStyle = XDashStyle.Dash };
        var tickPen = new XPen(XColors.Black, 0.6);
        var transformCopy = transform;

        // Lines across the map face, clipped to the frame.
        gfx.Save();
        gfx.IntersectClip(frame);

        foreach (var line in graticule.Meridians.Concat(graticule.Parallels))
        {
            var points = line.WebMercatorPoints.Select(p => transformCopy.ToPage(p)).ToArray();

            if (points.Length >= 2)
                gfx.DrawLines(linePen, points);
        }

        gfx.Restore();

        // Edge ticks + labels: meridians top/bottom, parallels left/right.
        foreach (var meridian in graticule.Meridians)
        {
            var x = transformCopy.ToPage(meridian.WebMercatorPoints[0]).X;

            if (x < frame.Left - 0.1 || x > frame.Right + 0.1)
                continue;

            gfx.DrawLine(tickPen, x, frame.Top, x, frame.Top - TickLength);
            gfx.DrawLine(tickPen, x, frame.Bottom, x, frame.Bottom + TickLength);

            var labelSize = gfx.MeasureString(meridian.Label, labelFont);
            gfx.DrawString(meridian.Label, labelFont, XBrushes.Black,
                new XPoint(x - labelSize.Width / 2, frame.Top - TickLength - 2));
            gfx.DrawString(meridian.Label, labelFont, XBrushes.Black,
                new XPoint(x - labelSize.Width / 2, frame.Bottom + TickLength + 2 + labelSize.Height * 0.8));
        }

        foreach (var parallel in graticule.Parallels)
        {
            var y = transformCopy.ToPage(parallel.WebMercatorPoints[0]).Y;

            if (y < frame.Top - 0.1 || y > frame.Bottom + 0.1)
                continue;

            gfx.DrawLine(tickPen, frame.Left, y, frame.Left - TickLength, y);
            gfx.DrawLine(tickPen, frame.Right, y, frame.Right + TickLength, y);

            var labelSize = gfx.MeasureString(parallel.Label, labelFont);
            var baselineY = y + labelSize.Height * 0.35;

            gfx.DrawString(parallel.Label, labelFont, XBrushes.Black,
                new XPoint(frame.Left - TickLength - 2 - labelSize.Width, baselineY));
            gfx.DrawString(parallel.Label, labelFont, XBrushes.Black,
                new XPoint(frame.Right + TickLength + 2, baselineY));
        }
    }

    private static void DrawNeatLine(XGraphics gfx, XRect frame)
    {
        var outer = new XRect(frame.X - 2.5, frame.Y - 2.5, frame.Width + 5, frame.Height + 5);

        gfx.DrawRectangle(new XPen(XColors.Black, 1.2), outer);
        gfx.DrawRectangle(new XPen(XColors.Black, 0.4), frame);
    }

    private static void DrawTitleBlock(
        XGraphics gfx,
        XRect band,
        PdfMapDecorations decorations,
        string labelFamily,
        XPdfFontOptions unicode)
    {
        // Company logo at the left edge of the title band.
        var titleStart = band.Left;

        if (decorations.SecondaryLogoPngBytes is { Length: > 0 })
        {
            var logoRect = DrawImageLeftAligned(gfx, decorations.SecondaryLogoPngBytes, band, padding: 4);
            titleStart = logoRect.Right + 8;
        }

        var titleArea = new XRect(titleStart, band.Top, band.Right - titleStart, band.Height);

        if (decorations.TitlePngBytes is { Length: > 0 })
        {
            DrawImageCentered(gfx, decorations.TitlePngBytes, titleArea, padding: 6);
        }
        else if (!string.IsNullOrWhiteSpace(decorations.TitleText))
        {
            var titleFont = new XFont(labelFamily, TitleFontSize, XFontStyle.Bold, unicode);
            gfx.DrawString(decorations.TitleText, titleFont, XBrushes.Black, titleArea, XStringFormats.Center);
        }
    }

    private static void DrawScaleBar(
        XGraphics gfx,
        XRect footer,
        double pointsPerMercatorMeter,
        double cosCenterLatitude,
        XFont labelFont)
    {
        var (barWidth, _, label) = PdfScaleBarHelper.Choose(pointsPerMercatorMeter, cosCenterLatitude);

        const double barHeight = 4;
        const int segments = 4;

        var barLeft = footer.Left;
        var barTop = footer.Top + footer.Height / 2 - barHeight / 2;

        var borderPen = new XPen(XColors.Black, 0.5);
        var segmentWidth = barWidth / segments;

        for (var i = 0; i < segments; i++)
        {
            var rect = new XRect(barLeft + i * segmentWidth, barTop, segmentWidth, barHeight);

            gfx.DrawRectangle(borderPen, i % 2 == 0 ? XBrushes.Black : XBrushes.White, rect);
        }

        // "0" over the left end, round ground length over the right end.
        var zeroSize = gfx.MeasureString("0", labelFont);
        gfx.DrawString("0", labelFont, XBrushes.Black, new XPoint(barLeft - zeroSize.Width / 2, barTop - 3));

        var labelSize = gfx.MeasureString(label, labelFont);
        gfx.DrawString(label, labelFont, XBrushes.Black, new XPoint(barLeft + barWidth - labelSize.Width / 2, barTop - 3));

        // Representative fraction under the bar, rounded to 3 significant digits.
        var denominator = PdfScaleBarHelper.GetPaperScaleDenominator(pointsPerMercatorMeter, cosCenterLatitude);
        var magnitude = Math.Pow(10, Math.Max(0, Math.Floor(Math.Log10(denominator)) - 2));
        var rounded = Math.Round(denominator / magnitude) * magnitude;
        var ratioText = FormattableString.Invariant($"1 : {rounded:N0}");

        gfx.DrawString(ratioText, labelFont, XBrushes.Black,
            new XPoint(barLeft, barTop + barHeight + 3 + gfx.MeasureString(ratioText, labelFont).Height * 0.8));
    }

    private static XRect DrawImageLeftAligned(XGraphics gfx, byte[] imageBytes, XRect band, double padding)
        => DrawImageInBand(gfx, imageBytes, band, padding, alignRight: false);

    private static XRect DrawImageRightAligned(XGraphics gfx, byte[] imageBytes, XRect band, double padding)
        => DrawImageInBand(gfx, imageBytes, band, padding, alignRight: true);

    private static XRect DrawImageInBand(XGraphics gfx, byte[] imageBytes, XRect band, double padding, bool alignRight)
    {
        try
        {
            using var image = XImage.FromStream(() => new MemoryStream(imageBytes));

            var height = band.Height - 2 * padding;
            var width = height * image.PixelWidth / image.PixelHeight;

            var x = alignRight ? band.Right - padding - width : band.Left + padding;
            var rect = new XRect(x, band.Top + padding, width, height);

            gfx.DrawImage(image, rect);
            return rect;
        }
        catch
        {
            // Unreadable image bytes: skip the logo rather than fail the export.
            return new XRect(alignRight ? band.Right : band.Left, band.Top, 0, 0);
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