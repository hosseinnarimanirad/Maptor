using System.IO;
using System.Text;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Persistence.Import;

namespace IRI.Maptor.Tests.Persistence;

/// <summary>
/// The format detector behind "add data from file" and "paste data". Every file is made-up text written to
/// a temporary folder; no sample data is read.
/// </summary>
public sealed class ImportFormatDetectorTest : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "maptor-import-detect-" + Guid.NewGuid().ToString("N"));

    public ImportFormatDetectorTest() => Directory.CreateDirectory(_folder);

    public void Dispose()
    {
        try { Directory.Delete(_folder, recursive: true); } catch { }
    }

    private string Write(string fileName, string content, bool bom = false)
    {
        var path = Path.Combine(_folder, fileName);
        File.WriteAllText(path, content, new UTF8Encoding(bom));
        return path;
    }

    private const string GeoJsonCollection = """{"type":"FeatureCollection","features":[{"type":"Feature","properties":{},"geometry":{"type":"Point","coordinates":[51.3,35.7]}}]}""";
    private const string GeoJsonFeaturesFirst = """{"features":[{"geometry":{"type":"Point","coordinates":[1,2]},"properties":{"a":1}}],"type":"FeatureCollection"}""";
    private const string GeoJsonGeometry = """{"type":"Polygon","coordinates":[[[0,0],[1,0],[1,1]]]}""";
    private const string TopoJsonText = """{"type":"Topology","objects":{"a":{"type":"GeometryCollection","geometries":[]}},"arcs":[]}""";
    private const string TopoJsonArcsFirst = """{"arcs":[[[0,0],[1,1]]],"objects":{},"type":"Topology"}""";
    private const string EsriJsonText = """{"displayFieldName":"","geometryType":"esriGeometryPoint","spatialReference":{"wkid":4326},"fields":[],"features":[{"attributes":{"id":1},"geometry":{"x":51.3,"y":35.7}}]}""";
    private const string EsriJsonFeaturesFirst = """{"features":[{"geometry":{"x":1,"y":2},"attributes":{"id":1}}]}""";

    [Theory]
    [InlineData("a.shp", DataSourceKind.Shapefile)]
    [InlineData("a.KML", DataSourceKind.Kml)]
    [InlineData("a.kmz", DataSourceKind.Kmz)]
    [InlineData("a.gpx", DataSourceKind.Gpx)]
    [InlineData("a.gpkg", DataSourceKind.GeoPackage)]
    [InlineData("a.dxf", DataSourceKind.Dxf)]
    [InlineData("a.csv", DataSourceKind.Csv)]
    [InlineData("a.tsv", DataSourceKind.Tsv)]
    public void ExtensionDecidesWithoutReadingTheFile(string fileName, DataSourceKind expected)
    {
        // The file does not exist: these formats are never opened to be detected.
        Assert.Equal(expected, ImportFormatDetector.DetectFromFile(Path.Combine(_folder, fileName)));
    }

    [Theory]
    [InlineData("a.mbtiles")]
    [InlineData("a.tif")]
    [InlineData("a")]
    [InlineData("")]
    public void UnsupportedExtensionIsNull(string fileName)
    {
        Assert.Null(ImportFormatDetector.DetectFromFile(fileName.Length == 0 ? "" : Path.Combine(_folder, fileName)));
    }

    [Theory]
    [InlineData(GeoJsonCollection, DataSourceKind.GeoJson)]
    [InlineData(GeoJsonFeaturesFirst, DataSourceKind.GeoJson)]
    [InlineData(GeoJsonGeometry, DataSourceKind.GeoJson)]
    [InlineData(TopoJsonText, DataSourceKind.TopoJson)]
    [InlineData(TopoJsonArcsFirst, DataSourceKind.TopoJson)]
    [InlineData(EsriJsonText, DataSourceKind.EsriJson)]
    [InlineData(EsriJsonFeaturesFirst, DataSourceKind.EsriJson)]
    public void JsonFileIsToldApartByContent(string json, DataSourceKind expected)
    {
        Assert.Equal(expected, ImportFormatDetector.DetectFromFile(Write("data.json", json)));
    }

    [Fact]
    public void ByteOrderMarkIsSkipped()
    {
        Assert.Equal(DataSourceKind.TopoJson, ImportFormatDetector.DetectFromFile(Write("data.json", TopoJsonText, bom: true)));
    }

    [Fact]
    public void IndentedJsonIsDetected()
    {
        var indented = "{\r\n  // comment\r\n  \"type\" : \"FeatureCollection\",\r\n  \"features\" : [ ]\r\n}";

        Assert.Equal(DataSourceKind.GeoJson, ImportFormatDetector.DetectFromFile(Write("data.json", indented)));
    }

    [Fact]
    public void ContentWinsOverGeoJsonExtension()
    {
        Assert.Equal(DataSourceKind.TopoJson, ImportFormatDetector.DetectFromFile(Write("data.geojson", TopoJsonText)));
    }

    [Fact]
    public void UnclearContentFallsBackToTheExtension()
    {
        Assert.Equal(DataSourceKind.GeoJson, ImportFormatDetector.DetectFromFile(Write("data.geojson", """{"name":"x"}""")));
        Assert.Equal(DataSourceKind.TopoJson, ImportFormatDetector.DetectFromFile(Write("data.topojson", "not json")));
    }

    [Theory]
    [InlineData("""{"name":"x","value":1}""")]
    [InlineData("""[1,2,3]""")]
    [InlineData("not json at all")]
    [InlineData("")]
    public void JsonOfNoSupportedKindIsNull(string json)
    {
        Assert.Null(ImportFormatDetector.DetectFromFile(Write("data.json", json)));
    }

    [Fact]
    public void CutOffJsonIsStillDetectedFromItsStart()
    {
        var start = """{"type":"FeatureCollection","features":[{"type":"Feature","geometry":{"type":"Point","coordinates":[1,""";

        Assert.Equal(DataSourceKind.GeoJson, ImportFormatDetector.DetectJson(Encoding.UTF8.GetBytes(start)));
    }

    [Fact]
    public void DecidingMemberAfterALargeMemberIsFound()
    {
        var big = new StringBuilder("""{"name":" """);
        big.Append('x', 200_000);
        big.Append("\",\"type\":\"Topology\",\"objects\":{},\"arcs\":[]}");

        Assert.Equal(DataSourceKind.TopoJson, ImportFormatDetector.DetectFromFile(Write("big.json", big.ToString())));
    }

    [Theory]
    [InlineData("51.3\t35.7\n51.4\t35.8", DataSourceKind.Tsv)]
    [InlineData("51.3,35.7\n51.4,35.8", DataSourceKind.Csv)]
    [InlineData("51.3 35.7\n51.4 35.8", DataSourceKind.Csv)]
    public void TxtFileIsTsvWhenALineHasATab(string content, DataSourceKind expected)
    {
        Assert.Equal(expected, ImportFormatDetector.DetectFromFile(Write("points.txt", content)));
    }

    [Theory]
    [InlineData(GeoJsonCollection, DataSourceKind.GeoJson)]
    [InlineData("  \r\n" + TopoJsonText, DataSourceKind.TopoJson)]
    [InlineData(EsriJsonText, DataSourceKind.EsriJson)]
    [InlineData("x\ty\n51.3\t35.7", DataSourceKind.Tsv)]
    [InlineData("x,y\n51.3,35.7", DataSourceKind.Csv)]
    [InlineData("51.3 35.7", DataSourceKind.Csv)]
    public void PastedTextIsDetected(string text, DataSourceKind expected)
    {
        Assert.Equal(expected, ImportFormatDetector.DetectFromText(text));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   \r\n ")]
    [InlineData("<kml></kml>")]
    [InlineData("""{"name":"x"}""")]
    public void PastedTextOfNoSupportedKindIsNull(string? text)
    {
        Assert.Null(ImportFormatDetector.DetectFromText(text));
    }

    [Theory]
    [InlineData(DataSourceKind.Kml, true)]
    [InlineData(DataSourceKind.Kmz, true)]
    [InlineData(DataSourceKind.Gpx, true)]
    [InlineData(DataSourceKind.GeoPackage, true)]
    [InlineData(DataSourceKind.GeoJson, false)]
    [InlineData(DataSourceKind.TopoJson, false)]
    [InlineData(DataSourceKind.EsriJson, false)]
    [InlineData(DataSourceKind.Csv, false)]
    [InlineData(DataSourceKind.Tsv, false)]
    [InlineData(DataSourceKind.Dxf, false)]
    public void OnlySelfDescribingFormatsSkipTheOptions(DataSourceKind kind, bool expected)
    {
        Assert.Equal(expected, ImportFormatDetector.CanAddWithoutOptions(kind, Path.Combine(_folder, "a.any")));
    }

    [Fact]
    public void ShapefileSkipsTheOptionsOnlyWithAReadablePrj()
    {
        var shp = Path.Combine(_folder, "roads.shp");

        Assert.False(ImportFormatDetector.CanAddWithoutOptions(DataSourceKind.Shapefile, shp));

        Write("roads.prj", "GEOGCS[\"GCS_WGS_1984\",DATUM[\"D_WGS_1984\",SPHEROID[\"WGS_1984\",6378137.0,298.257223563]],PRIMEM[\"Greenwich\",0.0],UNIT[\"Degree\",0.0174532925199433]]");

        Assert.True(ImportFormatDetector.CanAddWithoutOptions(DataSourceKind.Shapefile, shp));
    }

    [Fact]
    public void EveryPasteKindCanAlsoBeOpenedFromAFile()
    {
        Assert.All(ImportFormatDetector.PasteKinds, k => Assert.Contains(k, ImportFormatDetector.FileKinds));
    }
}
