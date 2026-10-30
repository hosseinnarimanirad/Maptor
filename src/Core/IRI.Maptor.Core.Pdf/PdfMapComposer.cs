using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.SpatialReferenceSystem;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace IRI.Maptor.Core.Pdf;

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

        // Legend column (left in LTR, right in RTL): bordered box with header + legend rows.
        if (layout.HasLegendColumn)
            DrawLegendColumn(gfx, layout.LegendColumnRect, decorations, labelFont);

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
    internal static void DrawFeatureLabel(XGraphics gfx, PdfVectorLogo glyphs, XPoint anchor, IRI.Maptor.Core.Spatial.IO.Dxf.RgbColor? color)
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

    private const double LegendPad = PdfLegendMetrics.ColumnPadding;
    private const double LegendSwatchW = PdfLegendMetrics.SwatchWidth;
    private const double LegendSwatchH = 15;
    private const double LegendSwatchTextGap = PdfLegendMetrics.SwatchTextGap;
    private const double LegendRowGap = 2;
    private const double LegendGroupGap = 5;                          // extra gap above each layer block
    private const double LegendColGap = PdfLegendMetrics.SubColumnGap;
    private const double LegendMinShrink = 0.55;     // below this rows stay readable no longer; clip and show "+N"
    private const double LegendTailHeight = 10;      // reserved for the "+N" clipped-rows indicator

    /// <summary>
    /// One flattened legend line: a layer-name header (no swatch), a rule row (swatch + title),
    /// or a simple-symbology layer collapsed to swatch + layer name. Each carries both text
    /// variants — wrapped for one-column and for two-column flow — so the chosen flow always
    /// draws text that already fits its width.
    /// </summary>
    private sealed class LegendItem
    {
        public PdfVectorLogo? Vector { get; set; }

        public PdfVectorLogo? VectorNarrow { get; set; }

        public PdfLegendSwatch? SwatchVector { get; set; }

        public byte[]? Swatch { get; set; }

        public bool HasSwatch => SwatchVector is { IsValid: true } || Swatch is { Length: > 0 };

        /// <summary>A rule row (counted by the "+N" tail) rather than a layer-name header.</summary>
        public bool IsRow { get; set; }

        /// <summary>First line of a layer block — gets the group gap above it.</summary>
        public bool StartsGroup { get; set; }

        /// <summary>The text variant matching the current flow (falls back to the wide one).</summary>
        public PdfVectorLogo? TextFor(bool narrow) => narrow ? VectorNarrow ?? Vector : Vector;
    }

    /// <summary>
    /// The uniform scale legend text is drawn at. The WPF side renders every title at the same
    /// pixel size and wraps/ellipsizes it to the paper width it will actually get, so this is a
    /// constant — all layers print at one font size instead of being shrunk to fit. The width
    /// term is only a guard against a mismatched (unwrapped) vector.
    /// </summary>
    private static double LegendTextScale(PdfVectorLogo vector, double textW) =>
        Math.Min(PdfLegendMetrics.PxToPoint, textW / vector.SourceWidth);

    /// <summary>
    /// Draws the legend column: a bordered box with a header at the top and the legend groups
    /// (layer name + swatch/title rule rows) stacked below. When the natural content height
    /// exceeds the column, the rows first re-flow into two narrower columns, then shrink
    /// uniformly down to <see cref="LegendMinShrink"/>; whatever still doesn't fit is clipped
    /// and summarized as a "+N" tail line.
    /// </summary>
    private static void DrawLegendColumn(XGraphics gfx, XRect column, PdfMapDecorations decorations, XFont labelFont)
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

        // Body: the legend groups.
        var bodyTop = dividerY + LegendPad;
        var bodyBottom = column.Bottom - LegendPad;
        var availW = column.Width - 2 * LegendPad;
        var availH = bodyBottom - bodyTop;

        if (!decorations.HasLegendEntries || availH < 8 || availW < LegendSwatchW + LegendSwatchTextGap + 4)
            return;

        var items = FlattenLegendItems(decorations);

        if (items.Count == 0)
            return;

        // Fit strategy: single column at natural size; overflow re-flows into two narrower
        // columns (using the narrow-wrapped titles); still too tall shrinks uniformly;
        // whatever remains is clipped with "+N".
        var columns = 1;
        var narrow = false;
        var swatchW = LegendSwatchW;
        var swatchH = LegendSwatchH;
        var colW = availW;
        var natural = MeasureLegendStack(items, colW, swatchW, swatchH, narrow);
        var capacity = availH;

        if (natural > capacity)
        {
            columns = 2;
            narrow = true;
            colW = (availW - LegendColGap) / 2;
            natural = MeasureLegendStack(items, colW, swatchW, swatchH, narrow);
            capacity = 2 * availH;
        }

        if (natural <= 0)
            return;

        // Shrink factor: heights and gaps scale down together; widths stay as laid out.
        var f = natural <= capacity ? 1.0 : Math.Max(LegendMinShrink, capacity / natural);
        var tailReserve = natural * f > capacity ? LegendTailHeight : 0;

        // RTL mirrors everything: columns fill right-to-left, the swatch sits on the right
        // of its row and text is right-aligned.
        var rtl = decorations.RightToLeft;
        var hAlign = rtl ? XStringAlignment.Far : XStringAlignment.Near;

        double ColumnLeft(int c)
        {
            var offset = c * (colW + LegendColGap);
            return rtl ? column.Right - LegendPad - colW - offset : column.Left + LegendPad + offset;
        }

        // Draw pass: flow top-down, spilling into the next column; stop when out of columns.
        var col = 0;
        var y = bodyTop;
        var index = 0;

        for (; index < items.Count; index++)
        {
            var item = items[index];
            var itemH = MeasureLegendItem(item, colW, swatchW, swatchH, narrow) * f;

            if (itemH <= 0)
                continue;

            if (y > bodyTop && item.StartsGroup)
                y += LegendGroupGap * f;

            var limit = col == columns - 1 ? bodyBottom - tailReserve : bodyBottom;

            if (y + itemH > limit)
            {
                col++;

                if (col >= columns)
                    break;

                y = bodyTop;
                limit = col == columns - 1 ? bodyBottom - tailReserve : bodyBottom;

                if (y + itemH > limit)
                    break;
            }

            var colLeft = ColumnLeft(col);

            if (item.HasSwatch)
            {
                var swatchX = rtl ? colLeft + colW - swatchW : colLeft;
                var textLeft = rtl ? colLeft : colLeft + swatchW + LegendSwatchTextGap;
                var labelW = colW - swatchW - LegendSwatchTextGap;
                var swatchCell = new XRect(swatchX, y + (itemH - swatchH * f) / 2, swatchW, swatchH * f);

                if (item.SwatchVector is { IsValid: true } vectorSwatch)
                    DrawLegendSwatch(gfx, swatchCell, vectorSwatch);
                else if (item.Swatch is { Length: > 0 } swatchBytes)
                    DrawImageContainedInBox(gfx, swatchBytes, swatchCell);

                if (item.TextFor(narrow) is { IsValid: true } label)
                {
                    var labelH = label.SourceHeight * LegendTextScale(label, labelW) * f;
                    DrawVectorText(gfx, label, textLeft, y + (itemH - labelH) / 2, labelW, labelH, hAlign);
                }
            }
            else if (item.TextFor(narrow) is { IsValid: true } text)
            {
                var textH = text.SourceHeight * LegendTextScale(text, colW) * f;
                DrawVectorText(gfx, text, colLeft, y + (itemH - textH) / 2, colW, textH, hAlign);
            }

            y += itemH + LegendRowGap * f;
        }

        // "+N" tail for the rows that didn't fit (numeric only — no localization key needed).
        var clippedRows = 0;

        for (; index < items.Count; index++)
        {
            // Count the clipped rule rows; a pure layer-name header is not a row of its own.
            if (items[index].IsRow)
                clippedRows++;
        }

        if (clippedRows > 0)
        {
            var tailFormat = new XStringFormat
            {
                Alignment = hAlign,
                LineAlignment = XLineAlignment.Center,
            };

            gfx.DrawString(
                Localize($"+{clippedRows}", decorations.UsePersianDigits),
                labelFont,
                XBrushes.Black,
                new XRect(ColumnLeft(columns - 1), bodyBottom - LegendTailHeight, colW, LegendTailHeight),
                tailFormat);
        }
    }

    /// <summary>
    /// Flattens the legend groups into drawable lines. A group with a header yields the header
    /// line plus one line per rule row; a headerless single-entry group (simple symbology) yields
    /// one swatch + layer-name line.
    /// </summary>
    private static List<LegendItem> FlattenLegendItems(PdfMapDecorations decorations)
    {
        var items = new List<LegendItem>();

        foreach (var group in decorations.LegendGroups)
        {
            var first = items.Count;

            if (group.HeaderVector is { IsValid: true })
                items.Add(new LegendItem { Vector = group.HeaderVector, VectorNarrow = group.HeaderVectorNarrow });

            foreach (var entry in group.Entries)
            {
                items.Add(new LegendItem
                {
                    Vector = entry.LabelVector,
                    VectorNarrow = entry.LabelVectorNarrow,
                    SwatchVector = entry.SwatchVector,
                    Swatch = entry.SwatchPngBytes,
                    IsRow = true,
                });
            }

            if (items.Count > first)
                items[first].StartsGroup = true;
        }

        return items;
    }

    /// <summary>
    /// Natural (unshrunk) height of a legend line: the swatch cell or the text block drawn at
    /// the uniform legend text scale, whichever is taller.
    /// </summary>
    private static double MeasureLegendItem(LegendItem item, double colW, double swatchW, double swatchH, bool narrow)
    {
        var hasSwatch = item.HasSwatch;
        var textW = hasSwatch ? colW - swatchW - LegendSwatchTextGap : colW;

        double textH = 0;

        if (item.TextFor(narrow) is { IsValid: true } vector && textW > 0)
            textH = vector.SourceHeight * LegendTextScale(vector, textW);

        return Math.Max(hasSwatch ? swatchH : 0, textH);
    }

    /// <summary>
    /// Draws a legend symbol as vector art inside the swatch cell: a stroked line, a filled and
    /// outlined rectangle, or a centered point marker — mirroring how the map itself draws the
    /// same symbolizer, so the swatch stays crisp at any zoom level.
    /// </summary>
    private static void DrawLegendSwatch(XGraphics gfx, XRect cell, PdfLegendSwatch swatch)
    {
        if (cell.Width <= 0 || cell.Height <= 0)
            return;

        foreach (var part in swatch.Parts)
        {
            // Never let a heavy on-screen stroke swallow the small swatch cell.
            var width = Math.Max(0.2, Math.Min(part.StrokeWidth, cell.Height * 0.6));

            // Mirror PdfWriter's GetPen/GetBrush so a swatch matches the drawn map exactly:
            // an unspecified stroke still outlines in black; an explicitly transparent one
            // (and a transparent fill) is never painted.
            var pen = part.Stroke is { } stroke
                ? ToSwatchColor(stroke, part.Opacity) is { } strokeColor ? new XPen(strokeColor, width) : null
                : new XPen(XColors.Black, width);

            var brush = ToSwatchColor(part.Fill, part.Opacity) is { } fillColor ? new XSolidBrush(fillColor) : null;

            switch (part.Shape)
            {
                case PdfLegendSwatchShape.Line:
                {
                    if (pen == null)
                        break;

                    // A shallow mid dip reveals joins/caps, like the on-screen sample.
                    var midY = cell.Top + cell.Height / 2;
                    var dip = Math.Min(2.0, cell.Height * 0.15);
                    var inset = Math.Min(1.5, cell.Width * 0.1);

                    gfx.DrawLines(pen, new[]
                    {
                        new XPoint(cell.Left + inset, midY),
                        new XPoint(cell.Left + cell.Width / 2, midY - dip),
                        new XPoint(cell.Right - inset, midY),
                    });

                    break;
                }

                case PdfLegendSwatchShape.Polygon:
                {
                    // Inset so a thick outline stays inside the cell.
                    var inset = Math.Min(width / 2 + 0.5, Math.Min(cell.Width, cell.Height) / 3);
                    var box = new XRect(cell.Left + inset, cell.Top + inset,
                        Math.Max(0.5, cell.Width - 2 * inset), Math.Max(0.5, cell.Height - 2 * inset));

                    if (brush != null)
                        gfx.DrawRectangle(brush, box);

                    if (pen != null)
                        gfx.DrawRectangle(pen, box);

                    break;
                }

                case PdfLegendSwatchShape.Point:
                {
                    var center = new XPoint(cell.Left + cell.Width / 2, cell.Top + cell.Height / 2);

                    if (part.Marker is { HasVector: true } marker)
                    {
                        DrawLegendMarkerFigures(gfx, marker, center, brush, pen, cell);
                    }
                    else if (part.Marker is { HasImage: true } imageMarker)
                    {
                        var side = Math.Min(cell.Height, cell.Width);
                        DrawImageContainedInBox(gfx, imageMarker.ImagePngBytes!,
                            new XRect(center.X - side / 2, center.Y - side / 2, side, side));
                    }
                    else
                    {
                        var radius = Math.Max(1.0, Math.Min(part.PointRadius, Math.Min(cell.Width, cell.Height) / 2 - width / 2));

                        if (brush != null)
                            gfx.DrawEllipse(brush, center.X - radius, center.Y - radius, radius * 2, radius * 2);

                        if (pen != null)
                            gfx.DrawEllipse(pen, center.X - radius, center.Y - radius, radius * 2, radius * 2);
                    }

                    break;
                }
            }
        }
    }

    /// <summary>
    /// Draws a point marker's vector figures centered in the cell, scaled down if the on-screen
    /// symbol is larger than the swatch. Mirrors the map's marker drawing (fill, then stroke).
    /// </summary>
    private static void DrawLegendMarkerFigures(XGraphics gfx, PdfPointMarker marker, XPoint center, XBrush? brush, XPen? pen, XRect cell)
    {
        var extent = 0.0;

        foreach (var figure in marker.Figures!)
        {
            foreach (var point in figure.Points)
                extent = Math.Max(extent, Math.Max(Math.Abs(point.X), Math.Abs(point.Y)));
        }

        // Marker points are centered on the origin, so extent is its half-size.
        var limit = Math.Min(cell.Width, cell.Height) / 2;
        var scale = extent > limit && extent > 0 ? limit / extent : 1.0;

        foreach (var figure in marker.Figures!)
        {
            if (figure.Points == null || figure.Points.Count < 2)
                continue;

            var pts = figure.Points
                .Select(p => new XPoint(center.X + p.X * scale, center.Y + p.Y * scale))
                .ToArray();

            if (figure.IsClosed)
            {
                if (figure.IsFilled && brush != null)
                    gfx.DrawPolygon(brush, pts, XFillMode.Alternate);

                if (pen != null)
                    gfx.DrawPolygon(pen, pts);
            }
            else if (pen != null)
            {
                gfx.DrawLines(pen, pts);
            }
        }
    }

    /// <summary>Folds the part's opacity into the color's alpha (same rule as PdfOptions).</summary>
    private static XColor? ToSwatchColor(IRI.Maptor.Core.Spatial.IO.Dxf.RgbColor? color, double opacity)
    {
        if (color is not { } c || c.A <= 0)
            return null;

        var alpha = (byte)Math.Round(c.A * Math.Max(0.0, Math.Min(1.0, opacity)));

        return alpha <= 0 ? null : XColor.FromArgb(alpha, c.R, c.G, c.B);
    }

    /// <summary>Total stacked height of all lines (plus gaps) at natural size, single flow.</summary>
    private static double MeasureLegendStack(List<LegendItem> items, double colW, double swatchW, double swatchH, bool narrow)
    {
        double total = 0;

        for (var i = 0; i < items.Count; i++)
        {
            if (i > 0 && items[i].StartsGroup)
                total += LegendGroupGap;

            var itemH = MeasureLegendItem(items[i], colW, swatchW, swatchH, narrow);

            if (itemH > 0)
                total += itemH + LegendRowGap;
        }

        return total;
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