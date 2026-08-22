using System;
using System.IO;
using System.Linq;

using Xunit;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.IO.VectorTiles;

namespace IRI.Maptor.Tests.Spatial;

/// <summary>
/// Decoder-only tests for Mapbox Vector Tiles, run against a real tile blob extracted from
/// countries.mbtiles (Natural Earth). These exercise the pure decode path (no SQLite), so they
/// do not depend on the native SQLite provider being initialized in the test host.
/// </summary>
public class MvtTileDecoderTest
{
    private const int WebMercator = 3857;

    private static byte[] LoadSampleTile()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "MbTilesSamples", "countries_z0_x0_y0.mvt");
        return File.ReadAllBytes(path);
    }

    [Fact]
    public void Decompress_RecognizesGzip_AndInflates()
    {
        var raw = LoadSampleTile();

        Assert.True(MvtDecompressionHelper.IsGzip(raw));

        var decompressed = MvtDecompressionHelper.Decompress(raw);

        Assert.True(decompressed.Length > raw.Length);
        Assert.False(MvtDecompressionHelper.IsGzip(decompressed));
    }

    [Fact]
    public void Decode_ReturnsExpectedLayers_WithFeatures()
    {
        var tile = MvtTileReader.Decode(MvtDecompressionHelper.Decompress(LoadSampleTile()));

        var names = tile.Layers.Select(l => l.Name).ToList();

        Assert.Contains("country", names);
        Assert.Contains("state", names);

        var country = tile.Layers.First(l => l.Name == "country");

        Assert.True(country.Extent > 0);
        Assert.NotEmpty(country.Features);
    }

    [Fact]
    public void Decode_PolygonFeature_ProducesGeometryWithinTileBounds()
    {
        var tile = MvtTileReader.Decode(MvtDecompressionHelper.Decompress(LoadSampleTile()));

        var country = tile.Layers.First(l => l.Name == "country");

        var toPoint = MvtTileTransform.LocalToWebMercator(zoom: 0, tileColumn: 0, tileRow: 0, extent: country.Extent);

        var polygonFeatures = country.Features.Where(f => f.GeometryKind == MvtGeometryKind.Polygon).ToList();

        Assert.NotEmpty(polygonFeatures);

        var decoded = polygonFeatures
            .Select(f => MvtGeometryDecoder.ToGeometry(f, toPoint, WebMercator))
            .Where(g => g != null)
            .ToList();

        // Diagnostic-friendly: at least one polygon feature must decode to a geometry.
        Assert.True(decoded.Count > 0,
            $"No polygon decoded out of {polygonFeatures.Count}. First feature raw geometry ints: {polygonFeatures[0].Geometry.Count}");

        var geometry = decoded.First();

        Assert.Equal(WebMercator, geometry!.Srid);

        var points = geometry.GetAllPoints().ToList();

        Assert.NotEmpty(points);

        // The z0 tile spans the whole Web Mercator world; every vertex must land inside it.
        double max = MvtTileTransform.MaxExtent;
        const double tolerance = 1.0;

        foreach (var point in points)
        {
            Assert.InRange(point.X, -max - tolerance, max + tolerance);
            Assert.InRange(point.Y, -max - tolerance, max + tolerance);
        }
    }

    [Fact]
    public void Decode_AttributesAreResolved()
    {
        var tile = MvtTileReader.Decode(MvtDecompressionHelper.Decompress(LoadSampleTile()));

        var country = tile.Layers.First(l => l.Name == "country");

        // At least one feature should carry resolved key/value attributes.
        Assert.Contains(country.Features, f => f.Attributes.Count > 0);
    }
}
