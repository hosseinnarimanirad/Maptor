using System.Collections.Generic;
using System.IO;
using System.Linq;
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

namespace IRI.Maptor.Tst.Main.Pdf;

/// <summary>
/// Tests for PdfWriter.WriteLayers Optional Content Group (OCG / toggleable layer) support.
/// Verifies that each map layer becomes exactly one toggleable PDF layer bound to a form XObject.
/// </summary>
public class PdfWriterLayersTest
{
    private static readonly BoundingBox MapExtent = new BoundingBox(0, 0, 100, 100);

    private static Feature<Point> MakeLineFeature(double offset)
    {
        var points = new List<Point>
        {
            new Point(offset, offset),
            new Point(offset + 10, offset + 20),
            new Point(offset + 30, offset + 5),
        };

        var geometry = Geometry<Point>.Create(points, GeometryType.LineString, 0);
        return new Feature<Point>(geometry);
    }

    private static PdfWriter.LayerPdfData MakeLayer(string name, int zIndex, params Feature<Point>[] features)
    {
        return new PdfWriter.LayerPdfData
        {
            LayerName = name,
            ZIndex = zIndex,
            Features = features.ToList(),
            Options = new PdfOptions(),
        };
    }

    private static PdfDocument Reopen(byte[] bytes)
    {
        var ms = new MemoryStream(bytes);
        return PdfReader.Open(ms, PdfDocumentOpenMode.ReadOnly);
    }

    private static List<string> GetOcgNames(PdfDocument doc)
    {
        var names = new List<string>();
        var ocProps = doc.Internals.Catalog.Elements.GetDictionary("/OCProperties");
        if (ocProps == null)
            return names;

        var ocgs = ocProps.Elements.GetArray("/OCGs");
        if (ocgs == null)
            return names;

        foreach (var item in ocgs.Elements)
        {
            var dict = (item as PdfReference)?.Value as PdfDictionary ?? item as PdfDictionary;
            if (dict != null)
                names.Add(dict.Elements.GetString("/Name"));
        }

        return names;
    }

    /// <summary>
    /// Counts page content streams that carry an optional-content marked-content sequence
    /// (/OC name BDC ... EMC) — i.e. the number of toggleable layers actually drawn.
    /// </summary>
    private static int CountOptionalContentMarkedStreams(PdfDocument doc)
    {
        var count = 0;

        foreach (var page in doc.Pages)
        {
            foreach (var item in page.Contents.Elements)
            {
                var dict = (item as PdfReference)?.Value as PdfDictionary ?? item as PdfDictionary;
                var raw = dict?.Stream?.UnfilteredValue;
                if (raw == null)
                    continue;

                var text = Encoding.ASCII.GetString(raw);
                if (text.Contains("/OC ") && text.Contains("BDC") && text.Contains("EMC"))
                    count++;
            }
        }

        return count;
    }

    [Fact]
    public void WriteLayers_GroupsSymbolizersIntoOneOcgPerMapLayer()
    {
        // "Rivers" has two symbolizers (two LayerPdfData entries sharing the same name)
        // and must collapse into a single toggleable layer.
        var layers = new List<PdfWriter.LayerPdfData>
        {
            MakeLayer("Roads", 1, MakeLineFeature(5)),
            MakeLayer("Rivers", 2, MakeLineFeature(15)),
            MakeLayer("Rivers", 2, MakeLineFeature(25)),
            MakeLayer("Borders", 3, MakeLineFeature(35)),
        };

        var bytes = PdfWriter.WriteLayers(layers, MapExtent, mapScale: 1000, supportPdfLayers: true);

        Assert.NotNull(bytes);
        Assert.NotEmpty(bytes);

        using var doc = Reopen(bytes);

        var names = GetOcgNames(doc);
        Assert.Equal(3, names.Count); // 3 distinct map layers, not 4 symbolizer entries
        Assert.Contains("Roads", names);
        Assert.Contains("Rivers", names);
        Assert.Contains("Borders", names);

        // Each map layer is drawn in its own optional-content marked-content sequence.
        Assert.Equal(3, CountOptionalContentMarkedStreams(doc));

        // Guard against the "white page" regression: the actual drawing operators must be
        // present inside the marked content (moveto / lineto / stroke).
        var content = GetAllContentText(doc);
        Assert.Contains(" m\n", content);
        Assert.Contains(" l\n", content);
        Assert.Contains("S\n", content);
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
    public void WriteLayers_NonLayered_HasNoOptionalContentProperties()
    {
        var layers = new List<PdfWriter.LayerPdfData>
        {
            MakeLayer("Roads", 1, MakeLineFeature(5)),
            MakeLayer("Rivers", 2, MakeLineFeature(15)),
        };

        var bytes = PdfWriter.WriteLayers(layers, MapExtent, mapScale: 1000, supportPdfLayers: false);

        Assert.NotNull(bytes);
        Assert.NotEmpty(bytes);

        using var doc = Reopen(bytes);

        Assert.Null(doc.Internals.Catalog.Elements.GetDictionary("/OCProperties"));
        Assert.Equal(0, CountOptionalContentMarkedStreams(doc));
        Assert.Equal(1, doc.PageCount);
    }

    private static Feature<Point> MakeSquarePolygon()
    {
        var ringPoints = new List<Point>
        {
            new Point(10, 10),
            new Point(90, 10),
            new Point(90, 90),
            new Point(10, 90),
            new Point(10, 10),
        };
        var ring = Geometry<Point>.Create(ringPoints, GeometryType.LineString, 0);
        var polygon = Geometry<Point>.Create(new List<Geometry<Point>> { ring }, GeometryType.Polygon, 0);
        return new Feature<Point>(polygon);
    }

    private static byte[] WriteSinglePolygonLayer(RgbColor? fill, RgbColor? stroke)
    {
        var layer = new PdfWriter.LayerPdfData
        {
            LayerName = "Parcels",
            ZIndex = 1,
            Features = new List<Feature<Point>> { MakeSquarePolygon() },
            Options = new PdfOptions { FillColor = fill, StrokeColor = stroke, StrokeWidth = 1.0 },
        };
        return PdfWriter.WriteLayers(new List<PdfWriter.LayerPdfData> { layer }, MapExtent, 1000, supportPdfLayers: true);
    }

    [Fact]
    public void WriteLayers_TransparentFill_IsNotPainted()
    {
        // Fully transparent (alpha 0) white fill + opaque black outline.
        var bytes = WriteSinglePolygonLayer(
            fill: new RgbColor(255, 255, 255, 0),
            stroke: new RgbColor(0, 0, 0, 255));

        using var doc = Reopen(bytes);
        var content = GetAllContentText(doc);

        // Outline must be drawn, but there must be NO fill operator (transparent => no paint).
        Assert.Contains("S\n", content);        // stroke present
        Assert.DoesNotContain("f*\n", content); // no even-odd fill
        Assert.DoesNotContain("f\n", content);  // no nonzero fill

        // And the white fill color must never be set in the content stream.
        Assert.DoesNotContain("1 1 1 rg", content);
    }

    [Fact]
    public void WriteLayers_OpaqueFill_IsPainted()
    {
        // Control case: an opaque fill MUST produce a fill operator.
        var bytes = WriteSinglePolygonLayer(
            fill: new RgbColor(200, 0, 0, 255),
            stroke: new RgbColor(0, 0, 0, 255));

        using var doc = Reopen(bytes);
        var content = GetAllContentText(doc);

        Assert.Contains("f*\n", content); // even-odd fill present
    }

    [Fact]
    public void WriteLayers_EmptyLayer_ProducesNoOcg()
    {
        var layers = new List<PdfWriter.LayerPdfData>
        {
            MakeLayer("Roads", 1, MakeLineFeature(5)),
            MakeLayer("Empty", 2), // no features
        };

        var bytes = PdfWriter.WriteLayers(layers, MapExtent, mapScale: 1000, supportPdfLayers: true);

        using var doc = Reopen(bytes);

        var names = GetOcgNames(doc);
        Assert.Single(names);
        Assert.Contains("Roads", names);
        Assert.DoesNotContain("Empty", names);
    }
}
