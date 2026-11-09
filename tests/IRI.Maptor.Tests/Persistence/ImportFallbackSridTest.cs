using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Persistence.DataSources;
using IRI.Maptor.Core.ShapefileFormat;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;

namespace IRI.Maptor.Tests.Persistence;

/// <summary>
/// The CRS the import options dialog asks for reaches the readers that could not open such files before: a
/// shapefile without a .prj and Esri JSON without a spatial reference. Synthetic data only: the shapefile is
/// written with the library's own writer into a temporary folder.
/// </summary>
public sealed class ImportFallbackSridTest : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "maptor-import-fallback-" + Guid.NewGuid().ToString("N"));

    public ImportFallbackSridTest()
    {
        Directory.CreateDirectory(_folder);
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public void Dispose()
    {
        try { Directory.Delete(_folder, recursive: true); } catch { }
    }

    private string WritePointShapefileWithoutPrj()
    {
        var path = Path.Combine(_folder, "towers.shp");

        var features = new[] { (51.30, 35.70), (51.40, 35.80), (51.50, 35.90) }
            .Select((p, i) => new Feature<Point>(
                Geometry<Point>.CreatePointOrLineString(new List<Point> { new Point(p.Item1, p.Item2) }, SridHelper.GeodeticWGS84),
                new Dictionary<string, object> { ["id"] = i + 1 }))
            .ToList();

        Shapefile.SaveAsShapefile(path, features, srs: null);

        File.Delete(Shapefile.GetPrjFileName(path));

        return path;
    }

    [Fact]
    public void ShapefileWithoutPrj_NeedsAFallback()
    {
        var path = WritePointShapefileWithoutPrj();

        Assert.ThrowsAny<Exception>(() => ShapefileDataSourceFactory.CreateLazy(path));
    }

    [Fact]
    public async Task ShapefileWithoutPrj_OpensWithTheChosenSrid()
    {
        var path = WritePointShapefileWithoutPrj();

        var dataSource = ShapefileDataSourceFactory.CreateLazy(path, null, SridHelper.GeodeticWGS84);

        Assert.Equal(SridHelper.GeodeticWGS84, dataSource.AssumedSrid);

        await dataSource.LoadAsync();

        var features = (await dataSource.GetAsFeatureSetAsync((Geometry<Point>?)null)).Features;

        Assert.Equal(3, features.Count);

        // read as WGS84 and shown in Web Mercator: 51.3° E is about 5.71 million metres east
        Assert.InRange(features[0].TheGeometry.GetAllPoints()[0].X, 5_700_000, 5_720_000);
    }

    [Fact]
    public void ShapefileWithPrj_IgnoresTheFallback()
    {
        var path = WritePointShapefileWithoutPrj();

        Shapefile.SaveAsPrj(path, Core.SpatialReferenceSystem.MapProjections.SrsBases.GeodeticWgs84, overwrite: true);

        var dataSource = ShapefileDataSourceFactory.CreateLazy(path, null, SridHelper.WebMercator);

        Assert.Null(dataSource.AssumedSrid);
    }

    private const string EsriWithoutSpatialReference = """{"geometryType":"esriGeometryPoint","fields":[],"features":[{"attributes":{"id":1},"geometry":{"x":51.3,"y":35.7}}]}""";

    private const string EsriInWebMercator = """{"geometryType":"esriGeometryPoint","spatialReference":{"wkid":3857},"fields":[],"features":[{"attributes":{"id":1},"geometry":{"x":5714000,"y":4259000}}]}""";

    [Fact]
    public async Task EsriJsonWithoutSpatialReference_UsesTheFallback()
    {
        var dataSource = await EsriJsonDataSource.CreateFromTextAsync(EsriWithoutSpatialReference, SridHelper.GeodeticWGS84);

        Assert.NotNull(dataSource);
        Assert.Equal(SridHelper.GeodeticWGS84, dataSource!.OriginalSrid);
        Assert.InRange((await dataSource.GetAsFeatureSetAsync((Geometry<Point>?)null)).Features[0].TheGeometry.GetAllPoints()[0].X, 5_700_000, 5_720_000);
    }

    [Fact]
    public async Task EsriJsonSpatialReference_WinsOverTheFallback()
    {
        var dataSource = await EsriJsonDataSource.CreateFromTextAsync(EsriInWebMercator, SridHelper.GeodeticWGS84);

        Assert.Equal(SridHelper.WebMercator, dataSource!.OriginalSrid);
    }

    [Fact]
    public async Task EsriJsonOriginalSridIsTheFilesOwn()
    {
        // Before, the SRID after projection (Web Mercator) was kept for every file, so a save rewrote a WGS84
        // file in Web Mercator.
        var wgs84 = EsriWithoutSpatialReference.Replace("\"fields\"", "\"spatialReference\":{\"wkid\":4326},\"fields\"");

        var dataSource = await EsriJsonDataSource.CreateFromTextAsync(wgs84);

        Assert.Equal(SridHelper.GeodeticWGS84, dataSource!.OriginalSrid);
    }
}
