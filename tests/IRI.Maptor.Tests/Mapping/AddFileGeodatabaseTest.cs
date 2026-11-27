using System.IO;
using System.Linq;
using System.Reflection;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Exceptions;
using IRI.Maptor.Core.Persistence.DataSources;
using IRI.Maptor.Core.Persistence.Model;
using IRI.Maptor.Presentation.Wpf.Layers;
using IRI.Maptor.Presentation.Wpf.Services;
using IRI.Maptor.Tests.Common;
using IRI.Maptor.Tests.FileGdb;

namespace IRI.Maptor.Tests.Mapping;

/// <summary>
/// MapViewModelBase.AddFileGeodatabase, the "File geodatabase" command: a folder dialog, then one group per geodatabase
/// with a sub-group per feature dataset, and a message for what was left out. The geodatabase is the synthetic one of
/// <see cref="FileGdbDataSourceTest"/>.
/// </summary>
[Collection(WpfCollection.Name)]
public sealed class AddFileGeodatabaseTest : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "maptor-add-gdb-" + Guid.NewGuid().ToString("N"));

    private readonly string _gdb;

    public AddFileGeodatabaseTest()
    {
        _gdb = Path.Combine(_root, "Test.gdb");
        Directory.CreateDirectory(_gdb);
        FileGdbDataSourceTest.WriteGeodatabase(_gdb);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { }
    }

    private static (PasteDataTest.TestMap Map, AddDataFromFileTest.DialogRecorder Dialogs) NewMap(string? folderResult = null)
    {
        var dialogService = DispatchProxy.Create<IDialogService, AddDataFromFileTest.DialogRecorder>();
        var recorder = (AddDataFromFileTest.DialogRecorder)(object)dialogService;
        recorder.FolderResult = folderResult;

        return (new PasteDataTest.TestMap { DialogService = dialogService }, recorder);
    }

    [Fact]
    public void FolderDialog_AddsOneGroup_WithASubGroupPerFeatureDataset()
    {
        WpfTestHost.Run(() =>
        {
            var (map, dialogs) = NewMap(folderResult: _gdb);

            PasteDataTest.Pump(() => map.AddFileGeodatabase((object)null!));

            Assert.Empty(dialogs.Errors);
            Assert.False(map.IsBusy);

            var group = Assert.IsType<GroupLayer>(Assert.Single(map.Layers));
            Assert.Equal("Test", group.LayerName);
            Assert.True(group.IsSearchable);

            // Wells is at the root; Parcels (alias "Land parcels") is in the Cadastre feature dataset
            Assert.Equal(new[] { "Cadastre", "Wells" }, group.SubLayers.Select(l => l.LayerName).OrderBy(n => n));

            var cadastre = Assert.IsType<GroupLayer>(group.SubLayers.Single(l => l.LayerName == "Cadastre"));
            var parcels = Assert.IsType<VectorLayer>(Assert.Single(cadastre.SubLayers));
            Assert.Equal("Land parcels", parcels.LayerName);
            Assert.True(parcels.IsSearchable);

            var source = Assert.IsType<FileGdbDataSource>(parcels.DataSource);
            var location = Assert.IsType<FileLocation>(source.Location);
            Assert.Equal("Parcels", location.TableName);       // the analyses read the dataset name from here

            Assert.Equal(2, PasteDataTest.Leaves(map).Count);

            // the feature classes that cannot be shown, each with its reason
            var message = Assert.Single(dialogs.Messages);
            Assert.Contains("Archive", message);
            Assert.Contains("Patches", message);
            Assert.Contains("Plots", message);
            Assert.DoesNotContain("Owners", message);           // a standalone table: not a candidate at all
        });
    }

    [Fact]
    public void CancellingTheFolderDialogAddsNothing()
    {
        WpfTestHost.Run(() =>
        {
            var (map, dialogs) = NewMap(folderResult: null);

            PasteDataTest.Pump(() => map.AddFileGeodatabase((object)null!));

            Assert.Empty(map.Layers);
            Assert.Empty(dialogs.Errors);
            Assert.Empty(dialogs.Messages);
            Assert.False(map.IsBusy);
        });
    }

    [Fact]
    public void FolderThatIsNotAGeodatabase_GetsTheUnsupportedFormatMessage()
    {
        var plain = Path.Combine(_root, "Plain");
        Directory.CreateDirectory(plain);

        WpfTestHost.Run(() =>
        {
            var (map, dialogs) = NewMap(folderResult: plain);

            PasteDataTest.Pump(() => map.AddFileGeodatabase((object)null!));

            Assert.Empty(map.Layers);
            Assert.Equal(typeof(MaptorUnsupportedFileFormatException), Assert.Single(dialogs.Errors));
            Assert.False(map.IsBusy);
        });
    }

    [Fact]
    public void DroppedFolder_IsAddedAsAGeodatabase()
    {
        WpfTestHost.Run(() =>
        {
            var (map, dialogs) = NewMap();

            PasteDataTest.Pump(() => map.AddDataFromPath(_gdb, null!));

            Assert.Empty(dialogs.Errors);
            Assert.Equal("Test", Assert.IsType<GroupLayer>(Assert.Single(map.Layers)).LayerName);
        });
    }

    [Fact]
    public void DroppedFile_GoesThroughAddData()
    {
        var file = Path.Combine(_root, "table.xlsx");
        File.WriteAllText(file, "x");

        var plain = Path.Combine(_root, "Plain");
        Directory.CreateDirectory(plain);

        WpfTestHost.Run(() =>
        {
            var (map, dialogs) = NewMap();

            // the same messages as the two commands give
            PasteDataTest.Pump(() => map.AddDataFromPath(file, null!));
            PasteDataTest.Pump(() => map.AddDataFromPath(plain, null!));
            PasteDataTest.Pump(() => map.AddDataFromPath(Path.Combine(_root, "gone.kml"), null!));

            Assert.Empty(map.Layers);
            Assert.Equal(new[] { typeof(MaptorUnsupportedFileFormatException), typeof(MaptorUnsupportedFileFormatException), typeof(MaptorFileNotFoundException) },
                         dialogs.Errors);
            Assert.False(map.IsBusy);
        });
    }

    [Fact]
    public void GeodatabaseWithNothingToShow_SaysSo()
    {
        // only the standalone table and the compressed feature class
        var gdb = Path.Combine(_root, "Tables.gdb");
        Directory.CreateDirectory(gdb);
        FileGdbDataSourceTest.WriteGeodatabase(gdb);

        foreach (var table in new[] { "a00000009", "a0000000c", "a0000000d", "a0000000e" })
            foreach (var file in Directory.GetFiles(gdb, table + ".*"))
                File.Delete(file);

        WpfTestHost.Run(() =>
        {
            var (map, dialogs) = NewMap();

            PasteDataTest.Pump(() => map.AddFileGeodatabase(gdb, null!));

            Assert.Empty(map.Layers);
            Assert.Single(dialogs.Messages);
            Assert.False(map.IsBusy);
        });
    }
}
