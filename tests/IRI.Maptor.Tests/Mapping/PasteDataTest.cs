using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Persistence.DataSources;
using IRI.Maptor.Core.ShapefileFormat;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Presentation.Core.Layers;
using IRI.Maptor.Presentation.Wpf.Services;
using IRI.Maptor.Presentation.Wpf.Services.Project;
using IRI.Maptor.Presentation.Wpf.ViewModels;
using IRI.Maptor.Presentation.Wpf.ViewModels.Dialogs;
using IRI.Maptor.Tests.Common;

namespace IRI.Maptor.Tests.Mapping;

/// <summary>
/// "Paste data" (MapViewModelBase.PasteData) and the layers it makes, including that a .mtproj project saves and
/// rebuilds them. Also the project round trip of a shapefile opened with a chosen CRS. Synthetic data only.
/// </summary>
[Collection(WpfCollection.Name)]
public sealed class PasteDataTest : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "maptor-paste-data-" + Guid.NewGuid().ToString("N"));

    public PasteDataTest()
    {
        Directory.CreateDirectory(_folder);
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public void Dispose()
    {
        try { Directory.Delete(_folder, recursive: true); } catch { }
    }

    /// <summary>Stands in for MapViewer the way MapGridsViewModelTest does: layers go into <see cref="MapViewModelBase.Layers"/>.</summary>
    private sealed class TestMap : MapViewModelBase
    {
        public TestMap()
        {
            InitializeSettings(null, null, null, null);

            Layers = new System.Collections.ObjectModel.ObservableCollection<ILayer>();

            RequestAddLayer = layer => Layers.Add(layer);

            RequestRemoveLayer = layer => Layers.Remove(layer);

            RequestClearLayer = (layer, remove, _, _) =>
            {
                if (remove)
                    Layers.Remove(layer);
            };

            RequestRefresh = _ => { };
        }
    }

    private const string PointAndPolygonKml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <kml xmlns="http://www.opengis.net/kml/2.2"><Document>
          <Placemark><name>tower</name><Point><coordinates>51.3,35.7,0</coordinates></Point></Placemark>
          <Placemark><name>yard</name><Polygon><outerBoundaryIs><LinearRing>
            <coordinates>51.30,35.70,0 51.31,35.70,0 51.31,35.71,0 51.30,35.71,0 51.30,35.70,0</coordinates>
          </LinearRing></outerBoundaryIs></Polygon></Placemark>
        </Document></kml>
        """;

    private const string EsriText = """{"geometryType":"esriGeometryPoint","fields":[],"features":[{"attributes":{"id":1},"geometry":{"x":51.3,"y":35.7}}]}""";

    private static TestMap NewMap()
        => new TestMap { DialogService = DispatchProxy.Create<IDialogService, AddDataFromFileTest.DialogRecorder>() };

    /// <summary>The error messages the map showed, so a failing reader says why.</summary>
    private static string Errors(MapViewModelBase map) => $"layers: {map.Layers.Count}; errors: " + string.Join(", ", ((AddDataFromFileTest.DialogRecorder)(object)map.DialogService).ErrorMessages);

    /// <summary>
    /// Runs <paramref name="action"/> on this STA thread's dispatcher, the way the app's UI thread does, so every
    /// continuation after an await comes back here: WPF layers cannot be built on a thread-pool thread.
    /// </summary>
    private static void Pump(Func<System.Threading.Tasks.Task> action)
    {
        var dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
        System.Threading.SynchronizationContext.SetSynchronizationContext(new System.Windows.Threading.DispatcherSynchronizationContext(dispatcher));

        var frame = new System.Windows.Threading.DispatcherFrame();
        Exception? failure = null;

        dispatcher.BeginInvoke(new Action(async () =>
        {
            try { await action(); }
            catch (Exception ex) { failure = ex; }
            finally { frame.Continue = false; }
        }));

        System.Windows.Threading.Dispatcher.PushFrame(frame);

        if (failure is not null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private static List<ILayer> Leaves(MapViewModelBase map) => map.GetAllLayers(map.Layers.ToList()).ToList();

    private static ImportOptionsDialogResult Pasted(DataSourceKind kind, string text, int srid)
        => new(kind, null, text, srid, true, false, GeometryType.Point);

    [Fact]
    public void PastedKml_IsGroupedLikeAFile_AndKeepsItsText()
    {
        WpfTestHost.Run(() =>
        {
            var map = NewMap();

            Pump(() => map.AddData(Pasted(DataSourceKind.Kml, PointAndPolygonKml, SridHelper.GeodeticWGS84), null!));

            Assert.True(map.Layers.Count == 1, Errors(map));
            var group = map.Layers[0];
            Assert.True(group.IsGroupLayer);
            Assert.Equal("KML Import", group.LayerName);

            var leaves = Leaves(map);
            Assert.Equal(2, leaves.Count);

            Assert.All(leaves, l =>
            {
                var kml = Assert.IsType<KmlDataSource>(l.DataSource);
                Assert.Null(kml.Location);
                Assert.Equal(PointAndPolygonKml, kml.RawKml);
            });

            Assert.False(map.IsBusy);
        });
    }

    [Fact]
    public void PastedEsriJson_UsesTheChosenCrs()
    {
        WpfTestHost.Run(() =>
        {
            var map = NewMap();

            Pump(() => map.AddData(Pasted(DataSourceKind.EsriJson, EsriText, SridHelper.GeodeticWGS84), null!));

            Assert.True(map.Layers.Count == 1, Errors(map));
            var layer = map.Layers[0];
            var esri = Assert.IsType<EsriJsonDataSource>(layer.DataSource);

            Assert.Equal(SridHelper.GeodeticWGS84, esri.OriginalSrid);
            Assert.Equal(EsriText, esri.RawJson);
            Assert.Null(esri.Location);
        });
    }

    [Fact]
    public void PastedKmlAndEsriJson_AreSavedInAProjectAndRebuilt()
    {
        WpfTestHost.Run(() =>
        {
            var projectFile = Path.Combine(Path.GetTempPath(), "paste-test.mtproj");

            var map = NewMap();

            Pump(() => map.AddData(Pasted(DataSourceKind.Kml, PointAndPolygonKml, SridHelper.GeodeticWGS84), null!));
            Pump(() => map.AddData(Pasted(DataSourceKind.EsriJson, EsriText, SridHelper.GeodeticWGS84), null!));

            var project = new MapProjectService(map).BuildProject(projectFile);

            Assert.Equal(2, project.ImportedLayers.Count);

            var kmlEntry = project.ImportedLayers.Single(e => e.Kind == DataSourceKind.Kml);
            Assert.Null(kmlEntry.Location);
            Assert.Equal(PointAndPolygonKml, kmlEntry.Options!.RawText);

            var esriEntry = project.ImportedLayers.Single(e => e.Kind == DataSourceKind.EsriJson);
            Assert.Equal(EsriText, esriEntry.Options!.RawText);
            Assert.Equal(SridHelper.GeodeticWGS84, esriEntry.Options.SourceSrid);

            var reopened = NewMap();

            MapProjectLoadResult result = null!;
            Pump(async () => result = await new MapProjectService(reopened).ApplyAsync(project, projectFile));

            Assert.Empty(result.SkippedLayers);
            Assert.Equal(3, Leaves(reopened).Count);
            Assert.Contains(Leaves(reopened), l => l.DataSource is EsriJsonDataSource { OriginalSrid: SridHelper.GeodeticWGS84 });
        });
    }

    [Fact]
    public void ShapefileOpenedWithAChosenCrs_ReopensWithIt()
    {
        var shp = Path.Combine(_folder, "towers.shp");

        var features = new List<Feature<Point>>
        {
            new(Geometry<Point>.CreatePointOrLineString(new List<Point> { new Point(51.3, 35.7) }, SridHelper.GeodeticWGS84),
                new Dictionary<string, object> { ["id"] = 1 }),
        };

        Shapefile.SaveAsShapefile(shp, features, srs: null);
        File.Delete(Shapefile.GetPrjFileName(shp));

        WpfTestHost.Run(() =>
        {
            var projectFile = Path.Combine(_folder, "project.mtproj");

            var map = NewMap();

            Pump(() => map.AddShapefile(shp, null!, SridHelper.GeodeticWGS84));

            var project = new MapProjectService(map).BuildProject(projectFile);

            var entry = Assert.Single(project.ImportedLayers);
            Assert.Equal(DataSourceKind.Shapefile, entry.Kind);
            Assert.Equal(SridHelper.GeodeticWGS84, entry.Options!.SourceSrid);

            var reopened = NewMap();

            MapProjectLoadResult result = null!;
            Pump(async () => result = await new MapProjectService(reopened).ApplyAsync(project, projectFile));

            Assert.Empty(result.SkippedLayers);
            var source = Assert.IsType<ShapefileDataSource>(Assert.Single(Leaves(reopened)).DataSource);
            Assert.Equal(SridHelper.GeodeticWGS84, source.AssumedSrid);
        });
    }

    [Fact]
    public void ShapefileWithPrj_SavesNoOptions()
    {
        var shp = Path.Combine(_folder, "lines.shp");

        var features = new List<Feature<Point>>
        {
            new(Geometry<Point>.CreatePointOrLineString(new List<Point> { new Point(51.3, 35.7) }, SridHelper.GeodeticWGS84),
                new Dictionary<string, object> { ["id"] = 1 }),
        };

        Shapefile.SaveAsShapefile(shp, features, Core.SpatialReferenceSystem.MapProjections.SrsBases.GeodeticWgs84);

        WpfTestHost.Run(() =>
        {
            var map = NewMap();

            // typed owner: a bare null would pick AddShapefile(object owner, int? maxSizeInKB), which asks for a file
            Pump(() => map.AddShapefile(shp, (object)null!));

            Assert.True(map.Layers.Count == 1, Errors(map));

            var entry = Assert.Single(new MapProjectService(map).BuildProject(Path.Combine(_folder, "p.mtproj")).ImportedLayers);

            Assert.Null(entry.Options);
        });
    }
}
