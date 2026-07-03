using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Xunit;

using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Ket.SqlitePersistence.MbTiles;

namespace IRI.Maptor.Tst.Main.Spatial;

/// <summary>
/// Integration tests for the vector MBTiles pipeline (SQLite reader + provider + decoder + data
/// source) against the real countries.mbtiles. These reproduce the exact path the map render uses,
/// surfacing any failure that the map's render try/catch would otherwise swallow.
/// </summary>
public class MbTilesVectorDataSourceTest
{
    private const string MbTilesPath = @"E:\Programming\100.IRI.Maptor\countries.mbtiles";

    private const double Max = 20037508.342789244;

    static MbTilesVectorDataSourceTest()
    {
        // Microsoft.Data.Sqlite.Core needs a native provider registered in the test host.
        SQLitePCL.Batteries_V2.Init();
    }

    [Fact]
    public void Provider_Layers_HaveGeometryType_AndExtentIsNotNaN()
    {
        if (!File.Exists(MbTilesPath))
            return;

        using var provider = new MbTilesVectorTileProvider(MbTilesPath);

        Assert.NotEmpty(provider.VectorLayers);

        // Every layer must have a (possibly defaulted) geometry type so the layer is symbolizable.
        Assert.All(provider.VectorLayers, info => Assert.NotNull(info.GeometryType));

        // No bounds metadata in this file -> extent must be derived (whole world), never NaN.
        Assert.False(provider.WebMercatorExtent.IsNaN());
    }

    [Fact]
    public async Task DataSource_GetAsFeatureSet_ReturnsCountryFeatures()
    {
        if (!File.Exists(MbTilesPath))
            return;

        using var provider = new MbTilesVectorTileProvider(MbTilesPath);

        var info = provider.VectorLayers.First(i => i.Id == "country");

        var dataSource = new MbTilesVectorDataSource(provider, info);

        // A continental extent over Europe/Africa (Web Mercator) at max zoom -> a bounded set of
        // tiles that definitely contain country polygons.
        var extent = new BoundingBox(0, 4_800_000, 2_300_000, 7_400_000);

        var featureSet = await dataSource.GetAsFeatureSetAsync(double.NaN, extent);

        Assert.False(featureSet.HasNoGeometry());
        Assert.NotEmpty(featureSet.Features);

        // Returned geometry must be in Web Mercator world bounds.
        var points = featureSet.Features.First().TheGeometry.GetAllPoints().ToList();
        Assert.NotEmpty(points);
        Assert.All(points, p =>
        {
            Assert.InRange(p.X, -Max - 1, Max + 1);
            Assert.InRange(p.Y, -Max - 1, Max + 1);
        });
    }
}
