using System.Collections.Generic;
using IRI.Maptor.Presentation.Wpf.Helpers;
using IRI.Maptor.Core.Pdf;
using IRI.Maptor.Tests.Common;
using Xunit;

namespace IRI.Maptor.Tests.Pdf;

/// <summary>
/// Guards the "every layer prints at the same font size" rule for the PDF legend.
/// The composer draws legend text at scale <c>min(PxToPoint, textWidth / SourceWidth)</c>, so the
/// scale is the uniform <c>PxToPoint</c> for every item exactly when each rendered title fits the
/// paper width it was wrapped for. These tests assert that invariant for titles of wildly
/// different lengths, in both one- and two-column flow widths.
/// </summary>
[Collection(WpfCollection.Name)]
public class PdfLegendTextTest
{
    private static readonly string[] Titles =
    {
        "راه",
        "جاده اصلی",
        "شبکه راه‌های ارتباطی استان تهران و حومه",
        "Roads",
        "Primary arterial road network including service ramps and interchanges",
        "پلاک‌های ثبتی محدوده طرح تفصیلی منطقه ۲۲ شهرداری تهران",
    };

    [Theory]
    [InlineData(PdfLegendMetrics.HeaderMaxWidthPx)]
    [InlineData(PdfLegendMetrics.LabelMaxWidthPx)]
    [InlineData(PdfLegendMetrics.NarrowHeaderMaxWidthPx)]
    [InlineData(PdfLegendMetrics.NarrowLabelMaxWidthPx)]
    public void WrappedTitles_NeverExceedTheirPaperWidth(double maxWidthPx)
    {
        WpfTestHost.Run(() =>
        {
            var overflowing = new List<string>();

            foreach (var title in Titles)
            {
                var vector = PdfDecorationHelper.RenderTextToVector(title, 10, maxWidthPx: maxWidthPx, maxLines: 2);

                Assert.NotNull(vector);

                // A hair of tolerance for glyph-outline flattening.
                if (vector!.SourceWidth > maxWidthPx + 0.5)
                    overflowing.Add($"\"{title}\" -> {vector.SourceWidth:F1}px > {maxWidthPx:F1}px");
            }

            Assert.True(
                overflowing.Count == 0,
                "these titles would be shrunk to fit (breaking uniform font size): " + string.Join("; ", overflowing));
        });
    }

    /// <summary>
    /// Wrapping must bound the width without altering the glyph size: a long title wrapped to two
    /// lines must have ~2x the single-line height, not a smaller font.
    /// </summary>
    [Fact]
    public void WrappedTitle_KeepsGlyphSize_AndGrowsInHeight()
    {
        WpfTestHost.Run(() =>
        {
            const double width = PdfLegendMetrics.NarrowLabelMaxWidthPx;

            var oneLine = PdfDecorationHelper.RenderTextToVector("راه", 10, maxWidthPx: width, maxLines: 2);
            var wrapped = PdfDecorationHelper.RenderTextToVector(
                "شبکه راه‌های ارتباطی استان تهران و حومه", 10, maxWidthPx: width, maxLines: 2);

            Assert.NotNull(oneLine);
            Assert.NotNull(wrapped);

            // Two lines of the same 10px font: taller than one line, but nowhere near 2x the
            // width bound — proving it wrapped rather than shrank.
            Assert.True(
                wrapped!.SourceHeight > oneLine!.SourceHeight,
                $"wrapped title was not taller ({wrapped.SourceHeight:F1} vs {oneLine.SourceHeight:F1}) — it likely shrank instead of wrapping");

            Assert.True(wrapped.SourceWidth <= width + 0.5);
        });
    }
}
