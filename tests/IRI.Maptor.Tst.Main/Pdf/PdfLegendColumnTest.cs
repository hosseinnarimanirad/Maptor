using System.Collections.Generic;
using System.IO;
using System.Text;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Pdf;
using IRI.Maptor.Sta.Spatial.IO.Dxf;
using IRI.Maptor.Sta.Spatial.Primitives;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.Advanced;
using PdfSharpCore.Pdf.IO;
using Xunit;

using Drawing = System.Drawing;

namespace IRI.Maptor.Tst.Main.Pdf;

/// <summary>
/// Tests for the decorated export's legend column: legend groups/entries supplied via
/// PdfMapDecorations must be drawn (swatch image + vector label) without breaking the export,
/// including the shrink-and-clip path when far more rows are supplied than fit.
/// </summary>
public class PdfLegendColumnTest
{
    private static readonly BoundingBox MapExtent = new BoundingBox(0, 0, 100, 100);

    private static Feature<Point> MakeLineFeature()
    {
        var points = new List<Point>
        {
            new Point(10, 10),
            new Point(50, 60),
            new Point(90, 20),
        };

        var geometry = Geometry<Point>.Create(points, GeometryType.LineString, 0);
        return new Feature<Point>(geometry);
    }

    private static List<PdfWriter.LayerPdfData> MakeSingleLayer()
    {
        return new List<PdfWriter.LayerPdfData>
        {
            new PdfWriter.LayerPdfData
            {
                LayerName = "Roads",
                ZIndex = 1,
                Features = new List<Feature<Point>> { MakeLineFeature() },
                Options = new PdfOptions(),
            },
        };
    }

    /// <summary>A simple closed triangle standing in for rendered text glyphs.</summary>
    private static PdfVectorLogo MakeVector()
    {
        return new PdfVectorLogo
        {
            SourceWidth = 40,
            SourceHeight = 12,
            Figures = new List<PdfMarkerFigure>
            {
                new PdfMarkerFigure
                {
                    IsClosed = true,
                    IsFilled = true,
                    Points = new List<Point> { new Point(0, 12), new Point(20, 0), new Point(40, 12) },
                },
            },
        };
    }

    /// <summary>A tiny opaque (24bpp, no alpha/SMask) PNG, like the flattened swatches.</summary>
    private static byte[] MakeOpaqueSwatchPng()
    {
        using var bitmap = new Drawing.Bitmap(8, 6, Drawing.Imaging.PixelFormat.Format24bppRgb);

        using (var graphics = Drawing.Graphics.FromImage(bitmap))
        {
            graphics.Clear(Drawing.Color.White);
            graphics.FillRectangle(Drawing.Brushes.DarkRed, 1, 1, 6, 4);
        }

        using var stream = new MemoryStream();
        bitmap.Save(stream, Drawing.Imaging.ImageFormat.Png);
        return stream.ToArray();
    }

    private static PdfMapDecorations MakeLegendDecorations(int entryCount)
    {
        var group = new PdfLegendGroup { HeaderVector = MakeVector() };

        for (var i = 0; i < entryCount; i++)
        {
            group.Entries.Add(new PdfLegendEntry
            {
                SwatchPngBytes = MakeOpaqueSwatchPng(),
                LabelVector = MakeVector(),
            });
        }

        return new PdfMapDecorations
        {
            ShowLegendColumn = true,
            LegendGroups = new List<PdfLegendGroup> { group },
        };
    }

    private static PdfDocument Reopen(byte[] bytes)
    {
        var ms = new MemoryStream(bytes);
        return PdfReader.Open(ms, PdfDocumentOpenMode.ReadOnly);
    }

    private static string GetAllContentText(PdfDocument doc)
    {
        var sb = new StringBuilder();
        foreach (var page in doc.Pages)
        {
            foreach (var item in page.Contents.Elements)
            {
                var dict = (item as PdfReference)?.Value as PdfDictionary ?? item as PdfDictionary;
                var raw = dict?.Stream?.UnfilteredValue;
                if (raw != null)
                    sb.Append(Encoding.ASCII.GetString(raw));
            }
        }
        return sb.ToString();
    }

    [Fact]
    public void WriteLayers_WithLegendEntries_DrawsSwatchImage()
    {
        var bytes = PdfWriter.WriteLayers(
            MakeSingleLayer(), MapExtent, mapScale: 1000, decorations: MakeLegendDecorations(entryCount: 2));

        Assert.NotNull(bytes);
        Assert.NotEmpty(bytes);

        using var doc = Reopen(bytes);
        Assert.Equal(1, doc.PageCount);

        // The swatch PNGs must land as image XObjects painted in the content stream.
        var resources = doc.Pages[0].Elements.GetDictionary("/Resources");
        var xObjects = resources?.Elements.GetDictionary("/XObject");
        Assert.NotNull(xObjects);

        // One image paint per swatch (the two entries): "/I0 Do", "/I1 Do".
        var content = GetAllContentText(doc);
        Assert.Contains("/I0 Do", content);
        Assert.Contains("/I1 Do", content);
    }

    [Fact]
    public void WriteLayers_OverflowingEntries_FlowIntoTwoColumns()
    {
        // Too many rows for one column, but fits two: swatches must be painted at (at least)
        // two distinct x positions.
        var bytes = PdfWriter.WriteLayers(
            MakeSingleLayer(), MapExtent, mapScale: 1000, decorations: MakeLegendDecorations(entryCount: 60));

        using var doc = Reopen(bytes);
        var content = GetAllContentText(doc);

        // Image paints look like "q 16 0 0 12 <x> <y> cm /I0 Do Q"; collect the x translations.
        var xPositions = new HashSet<string>();
        foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(
            content, @"([-\d.]+) [-\d.]+ cm /I\d+ Do"))
        {
            xPositions.Add(match.Groups[1].Value);
        }

        Assert.True(xPositions.Count >= 2, $"expected swatches in two columns, got x positions: {string.Join(", ", xPositions)}");
    }

    [Fact]
    public void WriteLayers_WithManyLegendEntries_ClipsWithoutThrowing()
    {
        // Far more rows than even two shrunk columns can hold: exercises the
        // shrink-to-fit pass, the clipping cutoff and the "+N" tail.
        var bytes = PdfWriter.WriteLayers(
            MakeSingleLayer(), MapExtent, mapScale: 1000, decorations: MakeLegendDecorations(entryCount: 200));

        Assert.NotNull(bytes);
        Assert.NotEmpty(bytes);

        using var doc = Reopen(bytes);
        Assert.Equal(1, doc.PageCount);
    }

    /// <summary>
    /// Vector swatches must be drawn as PDF vector art — no raster images at all — so they stay
    /// crisp when the reader zooms in.
    /// </summary>
    [Fact]
    public void WriteLayers_WithVectorSwatches_DrawsVectorArtAndNoImages()
    {
        var group = new PdfLegendGroup { HeaderVector = MakeVector() };

        group.Entries.Add(new PdfLegendEntry
        {
            LabelVector = MakeVector(),
            SwatchVector = new PdfLegendSwatch
            {
                Parts =
                {
                    new PdfLegendSwatchPart
                    {
                        Shape = PdfLegendSwatchShape.Polygon,
                        Fill = new RgbColor(204, 0, 0, 255),
                        Stroke = new RgbColor(0, 0, 0, 255),
                        StrokeWidth = 1.0,
                    },
                },
            },
        });

        group.Entries.Add(new PdfLegendEntry
        {
            LabelVector = MakeVector(),
            SwatchVector = new PdfLegendSwatch
            {
                Parts =
                {
                    new PdfLegendSwatchPart
                    {
                        Shape = PdfLegendSwatchShape.Line,
                        Stroke = new RgbColor(0, 0, 255, 255),
                        StrokeWidth = 2.0,
                    },
                },
            },
        });

        var decorations = new PdfMapDecorations
        {
            ShowLegendColumn = true,
            LegendGroups = new List<PdfLegendGroup> { group },
        };

        var bytes = PdfWriter.WriteLayers(MakeSingleLayer(), MapExtent, mapScale: 1000, decorations: decorations);

        using var doc = Reopen(bytes);
        var content = GetAllContentText(doc);

        // No image painted anywhere: the swatches are pure vector.
        Assert.DoesNotContain(" Do", content);
        Assert.Null(doc.Pages[0].Elements.GetDictionary("/Resources")?.Elements.GetDictionary("/XObject"));

        // The polygon swatch's red fill is a real PDF fill color, not baked into pixels.
        Assert.Contains("0.8 0 0", content);

        // ...and the line swatch's blue stroke likewise.
        Assert.Contains("0 0 1 RG", content);
    }

    [Fact]
    public void WriteLayers_EmptyLegendGroups_KeepsEmptyColumn()
    {
        // No groups: the reserved column must render exactly as before (border + header only).
        var decorations = new PdfMapDecorations { ShowLegendColumn = true };

        var bytes = PdfWriter.WriteLayers(MakeSingleLayer(), MapExtent, mapScale: 1000, decorations: decorations);

        Assert.NotNull(bytes);
        Assert.NotEmpty(bytes);

        using var doc = Reopen(bytes);
        Assert.Equal(1, doc.PageCount);
    }
}
