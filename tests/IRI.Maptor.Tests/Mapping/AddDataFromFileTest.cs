using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Exceptions;
using IRI.Maptor.Presentation.Wpf.Services;
using IRI.Maptor.Presentation.Wpf.ViewModels;
using IRI.Maptor.Presentation.Wpf.ViewModels.Dialogs;
using IRI.Maptor.Tests.Common;

namespace IRI.Maptor.Tests.Mapping;

/// <summary>
/// MapViewModelBase.AddDataFromFile, the one "add data from file" command: which files go straight to their reader,
/// which open the import options dialog, and which get a message. Every file is made-up text in a temporary folder.
/// </summary>
[Collection(WpfCollection.Name)]
public sealed class AddDataFromFileTest : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "maptor-add-data-" + Guid.NewGuid().ToString("N"));

    public AddDataFromFileTest() => Directory.CreateDirectory(_folder);

    public void Dispose()
    {
        try { Directory.Delete(_folder, recursive: true); } catch { }
    }

    private sealed class TestMap : MapViewModelBase
    {
        public int? KmlLimit { get; set; }

        protected override int? GetImportSizeLimit(DataSourceKind kind)
            => kind == DataSourceKind.Kml && KmlLimit.HasValue ? KmlLimit : base.GetImportSizeLimit(kind);
    }

    /// <summary>Records the options dialog calls and the error messages; every other call does nothing.</summary>
    public class DialogRecorder : DispatchProxy
    {
        public List<(DataSourceKind? Kind, string? FilePath)> OptionsDialogs { get; } = new();

        public List<Type> Errors { get; } = new();

        public ImportOptionsDialogResult? OptionsResult { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            switch (targetMethod?.Name)
            {
                case nameof(IDialogService.ShowImportOptionsDialogAsync):
                    OptionsDialogs.Add(((DataSourceKind?)args![0], (string?)args[1]));
                    return Task.FromResult(OptionsResult);

                case nameof(IDialogService.ShowErrorMessage):
                    Errors.Add(args![0]!.GetType());
                    return Task.CompletedTask;
            }

            var type = targetMethod?.ReturnType;

            if (type == typeof(Task))
                return Task.CompletedTask;

            if (type is { IsGenericType: true } && type.GetGenericTypeDefinition() == typeof(Task<>))
                return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(type.GetGenericArguments()[0]).Invoke(null, new object?[] { null });

            return type is { IsValueType: true } && type != typeof(void) ? Activator.CreateInstance(type) : null;
        }
    }

    private string Write(string fileName, string content)
    {
        var path = Path.Combine(_folder, fileName);
        File.WriteAllText(path, content);
        return path;
    }

    private static (DialogRecorder Dialogs, bool IsBusy) Add(string path, Action<TestMap>? setup = null, ImportOptionsDialogResult? optionsResult = null)
    {
        var dialogService = DispatchProxy.Create<IDialogService, DialogRecorder>();
        var recorder = (DialogRecorder)(object)dialogService;
        recorder.OptionsResult = optionsResult;

        bool isBusy = true;

        WpfTestHost.Run(() =>
        {
            var map = new TestMap { DialogService = dialogService };
            setup?.Invoke(map);

            map.AddDataFromFile(path, null!).GetAwaiter().GetResult();

            isBusy = map.IsBusy;
        });

        return (recorder, isBusy);
    }

    [Theory]
    [InlineData("points.csv", "51.3,35.7", DataSourceKind.Csv)]
    [InlineData("points.txt", "51.3\t35.7", DataSourceKind.Tsv)]
    [InlineData("roads.geojson", """{"type":"FeatureCollection","features":[]}""", DataSourceKind.GeoJson)]
    [InlineData("roads.json", """{"type":"Topology","objects":{},"arcs":[]}""", DataSourceKind.TopoJson)]
    [InlineData("roads.json", """{"geometryType":"esriGeometryPoint","features":[]}""", DataSourceKind.EsriJson)]
    [InlineData("drawing.dxf", "0\nEOF\n", DataSourceKind.Dxf)]
    [InlineData("roads.shp", "not read: only its .prj is looked for", DataSourceKind.Shapefile)]
    public void FormatsWithoutEnoughInformationOpenTheOptionsDialog(string fileName, string content, DataSourceKind kind)
    {
        var path = Write(fileName, content);

        var (dialogs, isBusy) = Add(path);

        var call = Assert.Single(dialogs.OptionsDialogs);
        Assert.Equal(kind, call.Kind);
        Assert.Equal(path, call.FilePath);
        Assert.Empty(dialogs.Errors);
        Assert.False(isBusy);
    }

    [Fact]
    public void KmlGoesStraightToItsReader()
    {
        // An empty KML: the reader's own "empty file" message proves it was read without the options dialog.
        var path = Write("places.kml", """<?xml version="1.0" encoding="UTF-8"?><kml xmlns="http://www.opengis.net/kml/2.2"><Document></Document></kml>""");

        var (dialogs, isBusy) = Add(path);

        Assert.Empty(dialogs.OptionsDialogs);
        Assert.Equal(typeof(MaptorEmptyFileException), Assert.Single(dialogs.Errors));
        Assert.False(isBusy);
    }

    [Theory]
    [InlineData("table.xlsx", "x")]
    [InlineData("data.json", """{"name":"not spatial"}""")]
    public void UnsupportedFilesGetAMessage(string fileName, string content)
    {
        var (dialogs, isBusy) = Add(Write(fileName, content));

        Assert.Empty(dialogs.OptionsDialogs);
        Assert.Equal(typeof(MaptorUnsupportedFileFormatException), Assert.Single(dialogs.Errors));
        Assert.False(isBusy);
    }

    [Fact]
    public void MissingFileGetsAMessage()
    {
        var (dialogs, _) = Add(Path.Combine(_folder, "gone.kml"));

        Assert.Equal(typeof(MaptorFileNotFoundException), Assert.Single(dialogs.Errors));
    }

    [Fact]
    public void FileOverTheSizeLimitIsNotRead()
    {
        var path = Write("places.kml", "<kml/>");

        var (dialogs, isBusy) = Add(path, map => map.KmlLimit = 0);

        Assert.Empty(dialogs.OptionsDialogs);
        Assert.Equal(typeof(MaptorFileSizeExceedToOpenException), Assert.Single(dialogs.Errors));
        Assert.False(isBusy);
    }

    [Fact]
    public void CancellingTheOptionsDialogAddsNothing()
    {
        var path = Write("points.csv", "51.3,35.7");

        var (dialogs, isBusy) = Add(path, optionsResult: null);

        Assert.Single(dialogs.OptionsDialogs);
        Assert.Empty(dialogs.Errors);
        Assert.False(isBusy);
    }

    [Fact]
    public void FileDialogFilterListsEveryFormatOnce()
    {
        var filter = IRI.Maptor.Core.Persistence.Import.ImportFormatDetector.GetFileDialogFilter("All supported files");

        var parts = filter.Split('|');

        Assert.Equal(0, parts.Length % 2);
        Assert.Equal("All supported files", parts[0]);

        var all = parts[1].Split(';');

        foreach (var pattern in new[] { "*.shp", "*.kml", "*.kmz", "*.gpx", "*.gpkg", "*.dxf", "*.geojson", "*.topojson", "*.json", "*.csv", "*.tsv", "*.txt" })
            Assert.Contains(pattern, all);

        Assert.Equal(all.Length, all.Distinct(StringComparer.OrdinalIgnoreCase).Count());

        var names = parts.Where((_, i) => i % 2 == 0).ToList();

        Assert.Equal(names.Count, names.Distinct().Count());
    }
}
