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

            // the picker lists every dataset; the user accepted what it ticked at first
            var picker = Assert.Single(dialogs.Pickers);
            Assert.Equal("Test.gdb", picker.GeodatabaseName);
            Assert.Equal(6, picker.Items.Count);
            Assert.Equal(new[] { "Wells", "Plots", "Parcels" }, picker.Items.Where(i => i.IsSelected).Select(i => i.Name));

            var group = Assert.IsType<GroupLayer>(Assert.Single(map.Layers));
            Assert.Equal("Test", group.LayerName);
            Assert.True(group.IsSearchable);

            // Wells and Plots are at the root; Parcels (alias "Land parcels") is in the Cadastre feature dataset
            Assert.Equal(new[] { "Cadastre", "Plots", "Wells" }, group.SubLayers.Select(l => l.LayerName).OrderBy(n => n));

            // Plots gives no coordinate system: it takes the picker's choice, WGS84 by default
            var plots = Assert.IsType<FileGdbDataSource>(((VectorLayer)group.SubLayers.Single(l => l.LayerName == "Plots")).DataSource);
            Assert.Equal(4326, plots.AssumedSrid);

            var cadastre = Assert.IsType<GroupLayer>(group.SubLayers.Single(l => l.LayerName == "Cadastre"));
            var parcels = Assert.IsType<VectorLayer>(Assert.Single(cadastre.SubLayers));
            Assert.Equal("Land parcels", parcels.LayerName);
            Assert.True(parcels.IsSearchable);

            var source = Assert.IsType<FileGdbDataSource>(parcels.DataSource);
            var location = Assert.IsType<FileLocation>(source.Location);
            Assert.Equal("Parcels", location.TableName);       // the analyses read the dataset name from here

            Assert.Equal(3, PasteDataTest.Leaves(map).Count);

            // everything ticked could be added: no message
            Assert.Empty(dialogs.Messages);
        });
    }

    [Fact]
    public void OnlyTheTickedDatasetsAreAdded()
    {
        WpfTestHost.Run(() =>
        {
            var (map, dialogs) = NewMap(folderResult: _gdb);

            dialogs.PickerAction = picker =>
            {
                picker.AreAllSelected = false;
                picker.Items.Single(i => i.Name == "Wells").IsSelected = true;
                picker.AddCommand.Execute(null);
            };

            PasteDataTest.Pump(() => map.AddFileGeodatabase((object)null!));

            var group = Assert.IsType<GroupLayer>(Assert.Single(map.Layers));
            Assert.Equal("Wells", Assert.Single(group.SubLayers).LayerName);   // no empty feature-dataset group either
        });
    }

    [Fact]
    public void CancellingThePickerAddsNothing()
    {
        WpfTestHost.Run(() =>
        {
            var (map, dialogs) = NewMap(folderResult: _gdb);

            dialogs.PickerAction = picker => picker.CancelCommand.Execute(null);

            PasteDataTest.Pump(() => map.AddFileGeodatabase((object)null!));

            Assert.Single(dialogs.Pickers);
            Assert.Empty(map.Layers);
            Assert.Empty(dialogs.Messages);
            Assert.False(map.IsBusy);
        });
    }

    [Fact]
    public void LayersOverTheRowLimitAreBlocked()
    {
        WpfTestHost.Run(() =>
        {
            var (map, dialogs) = NewMap(folderResult: _gdb);
            map.FileGdbRowLimit = 1;   // Wells and Plots have 1 row, Parcels 5

            PasteDataTest.Pump(() => map.AddFileGeodatabase((object)null!));

            var picker = Assert.Single(dialogs.Pickers);
            var parcels = picker.Items.Single(i => i.Name == "Parcels");
            Assert.False(parcels.IsAvailable);
            Assert.False(parcels.IsSelected);
            Assert.Equal(new[] { "Plots", "Wells" }, PasteDataTest.Leaves(map).Select(l => l.LayerName).OrderBy(n => n));
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

            // the picker shows why nothing can be added; Add stays disabled
            var picker = Assert.Single(dialogs.Pickers);
            Assert.Equal(0, picker.AvailableCount);
            Assert.False(picker.AddCommand.CanExecute(null));
            Assert.Empty(map.Layers);
            Assert.Empty(dialogs.Messages);
            Assert.False(map.IsBusy);
        });
    }

    [Fact]
    public void GeodatabaseWithoutDatasets_SaysSo()
    {
        var gdb = Path.Combine(_root, "Empty.gdb");
        Directory.CreateDirectory(gdb);
        GdbTestGeodatabase.WriteSystemCatalog(gdb, (1, "GDB_SystemCatalog"), (4, "GDB_Items"));
        GdbTestGeodatabase.WriteItems(gdb);

        WpfTestHost.Run(() =>
        {
            var (map, dialogs) = NewMap();

            PasteDataTest.Pump(() => map.AddFileGeodatabase(gdb, null!));

            Assert.Empty(dialogs.Pickers);
            Assert.Empty(dialogs.Errors);
            Assert.Single(dialogs.Messages);
            Assert.Empty(map.Layers);
        });
    }
}
