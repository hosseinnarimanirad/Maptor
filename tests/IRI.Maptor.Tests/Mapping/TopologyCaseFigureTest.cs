using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using IRI.Maptor.Presentation.Wpf.Controls;
using IRI.Maptor.Tests.Common;

using Xunit;

namespace IRI.Maptor.Tests.Mapping;

/// <summary>
/// The topology guide's figures: every case draws something, left to right and mirrored. Set
/// <c>MAPTOR_TOPOLOGY_FIGURES</c> to a folder to also get one sheet of all cases per direction, to look at.
/// </summary>
public class TopologyCaseFigureTest
{
    private const int Scale = 2;

    private static BitmapSource Render(FrameworkElement element)
    {
        element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        element.Arrange(new Rect(element.DesiredSize));
        element.UpdateLayout();

        var bitmap = new RenderTargetBitmap(
            (int)Math.Ceiling(element.DesiredSize.Width * Scale),
            (int)Math.Ceiling(element.DesiredSize.Height * Scale),
            96 * Scale, 96 * Scale, PixelFormats.Pbgra32);

        bitmap.Render(element);

        return bitmap;
    }

    /// <summary>How many pixels differ from the first pixel's colour: more than the bare card with its dots.</summary>
    private static int InkedPixels(BitmapSource bitmap)
    {
        var stride = bitmap.PixelWidth * 4;
        var pixels = new byte[stride * bitmap.PixelHeight];

        bitmap.CopyPixels(pixels, stride, 0);

        // the card's own colour, read well inside it and away from the dot grid
        int at = (10 * Scale) * stride + (10 * Scale) * 4;
        byte b = pixels[at], g = pixels[at + 1], r = pixels[at + 2];

        int count = 0;

        for (int i = 0; i < pixels.Length; i += 4)
            if (Math.Abs(pixels[i] - b) + Math.Abs(pixels[i + 1] - g) + Math.Abs(pixels[i + 2] - r) > 60)
                count++;

        return count;
    }

    [Fact]
    public void EveryCase_DrawsAScene_InBothDirections() => WpfTestHost.Run(() =>
    {
        var folder = Environment.GetEnvironmentVariable("MAPTOR_TOPOLOGY_FIGURES");

        foreach (var direction in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
        {
            var cases = Enum.GetValues<TopologyGuideCase>();

            foreach (var guideCase in cases)
            {
                var figure = new TopologyCaseFigure { Case = guideCase, FlowDirection = direction };

                var bitmap = Render(figure);

                Assert.Equal(TopologyCaseFigure.FigureWidth * Scale, bitmap.PixelWidth);
                Assert.True(InkedPixels(bitmap) > 900, $"{guideCase} ({direction}) is nearly empty");
            }

            if (string.IsNullOrWhiteSpace(folder))
                continue;

            // one sheet per direction: three figures a row, each with its name
            var sheet = new WrapPanel { Width = 3 * (TopologyCaseFigure.FigureWidth + 16), Background = Brushes.White };

            foreach (var guideCase in cases)
            {
                var cell = new StackPanel { Margin = new Thickness(8) };

                cell.Children.Add(new TopologyCaseFigure { Case = guideCase, FlowDirection = direction });
                cell.Children.Add(new TextBlock { Text = guideCase.ToString(), FontSize = 11, HorizontalAlignment = HorizontalAlignment.Center });

                sheet.Children.Add(cell);
            }

            Directory.CreateDirectory(folder);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(Render(sheet)));

            using var stream = File.Create(Path.Combine(folder, $"topology-figures-{direction}.png"));
            encoder.Save(stream);
        }
    });

    [Fact]
    public void AFixFigure_HasMoreInkThanItsIssue()
    {
        // before → after: two panes, an arrow and a tick
        WpfTestHost.Run(() =>
        {
            var issue = InkedPixels(Render(new TopologyCaseFigure { Case = TopologyGuideCase.EndOvershoot }));
            var fix = InkedPixels(Render(new TopologyCaseFigure { Case = TopologyGuideCase.FixTrimOvershoot }));

            Assert.True(fix > issue, $"{fix} <= {issue}");
        });
    }
}
