using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Ogc.Kml;
using IRI.Maptor.Core.Persistence.DataSources;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Core.SpatialReferenceSystem.MapProjections;

namespace IRI.Maptor.Tests.OGC;

/// <summary>
/// Saving a KML/KMZ layer whose file mixes points, lines and polygons. The map loads such a file as one sub-layer
/// per geometry category, each with its own source on the same file; saving one of them must write the whole file
/// (it used to write only its own features: deleting the only point and saving emptied the file).
/// Synthetic coordinates, temporary files.
/// </summary>
public class KmlDataSourceSaveTest : IDisposable
{
    private const string Kml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <kml xmlns="http://www.opengis.net/kml/2.2">
        <Document>
          <Placemark><name>square</name>
            <Polygon><outerBoundaryIs><LinearRing><coordinates>51.30,35.70,0 51.31,35.70,0 51.31,35.71,0 51.30,35.71,0 51.30,35.70,0</coordinates></LinearRing></outerBoundaryIs></Polygon>
          </Placemark>
          <Placemark><name>second square</name>
            <Polygon><outerBoundaryIs><LinearRing><coordinates>51.32,35.70,0 51.33,35.70,0 51.33,35.71,0 51.32,35.71,0 51.32,35.70,0</coordinates></LinearRing></outerBoundaryIs></Polygon>
          </Placemark>
          <Placemark><name>line</name>
            <LineString><coordinates>51.37,35.72,0 51.38,35.73,0 51.39,35.72,0</coordinates></LineString>
          </Placemark>
          <Placemark><name>point</name>
            <Point><coordinates>51.40,35.75,0</coordinates></Point>
          </Placemark>
        </Document>
        </kml>
        """;

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "maptor-kml-save-" + Guid.NewGuid().ToString("N"));

    public KmlDataSourceSaveTest() => Directory.CreateDirectory(_folder);

    public void Dispose()
    {
        try { Directory.Delete(_folder, recursive: true); } catch { }
    }

    private string WriteKml()
    {
        var file = Path.Combine(_folder, "mixed.kml");
        File.WriteAllText(file, Kml);
        return file;
    }

    // what MapViewModelBase.AddKmlfile does: read, to Web Mercator, one group per geometry category
    private static List<List<Feature<Point>>> Groups(List<KmlFeature> kmlFeatures) =>
        kmlFeatures.ToFeatures()
            .Select(f => f.Transform(MapProjects.GeodeticWgs84ToWebMercator<Point>, SridHelper.WebMercator))
            .GroupBy(f => f.GeometryType.GetCategory())
            .Select(g => g.ToList())
            .ToList();

    private static Dictionary<string, int> CountByCategory(List<KmlFeature> kmlFeatures) =>
        kmlFeatures.ToFeatures().GroupBy(f => f.GeometryType.GetCategory().ToString()).ToDictionary(g => g.Key, g => g.Count());

    [Fact]
    public async Task SavingOneSubLayer_WritesEverySubLayer()
    {
        var file = WriteKml();

        var sources = KmlDataSource.CreateForFile(file, Groups(await KmlReader.ReadFeaturesFromFileAsync(file)));

        Assert.Equal(3, sources.Count);

        var points = sources.Single(s => s.GeometryType == GeometryType.Point);
        var polygons = sources.Single(s => s.GeometryType == GeometryType.Polygon);

        // delete the only point (as the Geometry tab's Delete does) and save that sub-layer
        points.Remove((await points.GetAsFeatureSetAsync()).Features.Single());

        Assert.True(points.HasPendingChanges);

        await points.SaveChangesAsync();

        var saved = await KmlReader.ReadFeaturesFromFileAsync(file);

        Assert.Equal(3, saved.Count);
        Assert.Equal(new Dictionary<string, int> { ["Polygon"] = 2, ["Polyline"] = 1 }, CountByCategory(saved));
        Assert.Equal(new[] { "line", "second square", "square" }, saved.Select(f => f.Name).OrderBy(n => n));
        Assert.False(points.HasPendingChanges);

        // an edit in another sub-layer, saved from a third one: written too, and saved for all
        var square = (await polygons.GetAsFeatureSetAsync()).Features.First();
        polygons.Remove(square);

        await sources.Single(s => s.GeometryType == GeometryType.LineString).SaveChangesAsync();

        Assert.Equal(2, (await KmlReader.ReadFeaturesFromFileAsync(file)).Count);
        Assert.False(polygons.HasPendingChanges);
    }

    [Fact]
    public async Task Save_KeepsCoordinatesInWgs84()
    {
        var file = WriteKml();

        var sources = KmlDataSource.CreateForFile(file, Groups(await KmlReader.ReadFeaturesFromFileAsync(file)));

        await sources[0].SaveChangesAsync();

        var point = (await KmlReader.ReadFeaturesFromFileAsync(file)).Single(f => f.Name == "point").Geometry.Points[0];

        Assert.Equal(51.40, point.X, 6);
        Assert.Equal(35.75, point.Y, 6);
    }

    [Fact]
    public async Task Kmz_SavingOneSubLayer_WritesEverySubLayer()
    {
        var kmz = Path.Combine(_folder, "mixed.kmz");

        await KmzWriter.WriteToFileAsync(await KmlReader.ReadFeaturesFromFileAsync(WriteKml()), kmz);

        var sources = KmzDataSource.CreateForFile(kmz, Groups(await KmzReader.ReadFeaturesFromFileAsync(kmz)));

        var points = sources.Single(s => s.GeometryType == GeometryType.Point);

        points.Remove((await points.GetAsFeatureSetAsync()).Features.Single());

        await points.SaveChangesAsync();

        Assert.Equal(new Dictionary<string, int> { ["Polygon"] = 2, ["Polyline"] = 1 }, CountByCategory(await KmzReader.ReadFeaturesFromFileAsync(kmz)));
    }

    [Fact]
    public async Task SingleCategoryFile_StillSavesOnItsOwn()
    {
        var file = Path.Combine(_folder, "points.kml");

        File.WriteAllText(file, Kml.Replace(Kml[Kml.IndexOf("  <Placemark><name>square", StringComparison.Ordinal)..Kml.IndexOf("  <Placemark><name>point", StringComparison.Ordinal)], string.Empty));

        var source = KmlDataSource.CreateForFile(file, Groups(await KmlReader.ReadFeaturesFromFileAsync(file))).Single();

        await source.SaveChangesAsync();

        Assert.Equal("point", (await KmlReader.ReadFeaturesFromFileAsync(file)).Single().Name);
    }
}
