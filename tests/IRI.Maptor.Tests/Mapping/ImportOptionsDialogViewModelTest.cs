using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Presentation.Wpf.Services;
using IRI.Maptor.Presentation.Wpf.ViewModels.Dialogs;

namespace IRI.Maptor.Tests.Mapping;

/// <summary>
/// The one import options dialog, driven without a window. Every input is made-up text (pasted, or written
/// to a temporary file); no sample data is read.
/// </summary>
public sealed class ImportOptionsDialogViewModelTest : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "maptor-import-options-" + Guid.NewGuid().ToString("N"));

    public ImportOptionsDialogViewModelTest() => Directory.CreateDirectory(_folder);

    public void Dispose()
    {
        try { Directory.Delete(_folder, recursive: true); } catch { }
    }

    /// <summary>A dialog service that does nothing: the view model only shows messages through it.</summary>
    public class NullDialogService : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            var type = targetMethod?.ReturnType;

            if (type == typeof(Task))
                return Task.CompletedTask;

            if (type is { IsGenericType: true } && type.GetGenericTypeDefinition() == typeof(Task<>))
                return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(type.GetGenericArguments()[0]).Invoke(null, new object?[] { null });

            return type is { IsValueType: true } && type != typeof(void) ? Activator.CreateInstance(type) : null;
        }
    }

    private static IDialogService Dialogs => DispatchProxy.Create<IDialogService, NullDialogService>();

    private static ImportOptionsDialogViewModel Paste(string text)
    {
        var vm = new ImportOptionsDialogViewModel(Dialogs, kind: null, filePath: null);
        vm.RawText = text;
        return vm;
    }

    private async Task<ImportOptionsDialogViewModel> OpenFile(string fileName, string content, DataSourceKind kind)
    {
        var path = Path.Combine(_folder, fileName);
        File.WriteAllText(path, content);

        var vm = new ImportOptionsDialogViewModel(Dialogs, kind, path);
        await vm.Initialization;
        return vm;
    }

    private const string GeoJsonDegrees = """{"type":"FeatureCollection","features":[{"type":"Feature","properties":{},"geometry":{"type":"LineString","coordinates":[[51.3,35.7],[51.4,35.8]]}}]}""";
    private const string GeoJsonUtm = """{"type":"FeatureCollection","features":[{"type":"Feature","properties":{},"geometry":{"type":"Point","coordinates":[537000,3952000]}}]}""";
    private const string EsriJsonWebMercator = """{"geometryType":"esriGeometryPoint","spatialReference":{"wkid":3857},"fields":[],"features":[{"attributes":{"id":1},"geometry":{"x":5714000,"y":4259000}}]}""";
    private const string EsriJsonOtherCrs = """{"geometryType":"esriGeometryPoint","spatialReference":{"wkid":2154},"fields":[],"features":[{"attributes":{"id":1},"geometry":{"x":650000,"y":6860000}}]}""";

    [Fact]
    public void PasteModeStartsEmptyWithOpenDisabled()
    {
        var vm = new ImportOptionsDialogViewModel(Dialogs, null, null);

        Assert.True(vm.IsPasteMode);
        Assert.True(vm.ShowsText);
        Assert.False(vm.IsTextReadOnly);
        Assert.True(vm.CanChooseKind);
        Assert.NotNull(vm.ValidationMessage);
        Assert.False(vm.OpenCommand.CanExecute(null));
    }

    [Fact]
    public void PastedJsonIsDetectedAndPreviewed()
    {
        var vm = Paste(GeoJsonDegrees);

        Assert.Equal(DataSourceKind.GeoJson, vm.Kind);
        Assert.Equal(2, vm.SamplePoints.Count);
        Assert.Equal(SridHelper.GeodeticWGS84, vm.EffectiveSelectedSrid);
        Assert.Null(vm.CoordinateWarning);
        Assert.Null(vm.ValidationMessage);
        Assert.Equal(51.3, vm.SamplePoints[0].Longitude!.Value, 6);
        Assert.Equal(35.7, vm.SamplePoints[0].Latitude!.Value, 6);
    }

    [Fact]
    public void ProjectedValuesAreGuessedAsUtmAndShownInWgs84()
    {
        var vm = Paste(GeoJsonUtm);

        Assert.True(vm.IsUtmSelected);

        // Zone 39 north is the default zone: 537000 / 3952000 lies in Iran.
        Assert.Equal(SridHelper.GetUtmSrid(39), vm.EffectiveSelectedSrid);
        Assert.InRange(vm.SamplePoints[0].Longitude!.Value, 51, 52);
        Assert.InRange(vm.SamplePoints[0].Latitude!.Value, 35, 36);
        Assert.Null(vm.CoordinateWarning);
    }

    [Fact]
    public void WrongCrsChoiceWarnsButKeepsOpenEnabled()
    {
        var vm = Paste(GeoJsonUtm);

        vm.SelectedSrsOption = vm.AvailableSrsOptions.First(o => o.FixedSrid == SridHelper.GeodeticWGS84);

        Assert.NotNull(vm.CoordinateWarning);
        Assert.Null(vm.SamplePoints[0].Longitude);
        Assert.True(vm.OpenCommand.CanExecute(null));
    }

    [Fact]
    public void DegreesReadAsUtmWarn()
    {
        var vm = Paste(GeoJsonDegrees);

        vm.SelectedSrsOption = vm.AvailableSrsOptions.First(o => o.IsUtm);

        Assert.NotNull(vm.CoordinateWarning);
    }

    [Fact]
    public void UserChoiceIsNotOverriddenByLaterText()
    {
        var vm = Paste(GeoJsonDegrees);

        vm.SelectedSrsOption = vm.AvailableSrsOptions.First(o => o.FixedSrid == SridHelper.WebMercator);
        vm.RawText = GeoJsonUtm;

        Assert.Equal(SridHelper.WebMercator, vm.EffectiveSelectedSrid);
    }

    [Fact]
    public void LongitudeFirstSwapsThePreview()
    {
        var vm = Paste("35.7,51.3\n35.8,51.4\n35.9,51.5");

        Assert.Equal(DataSourceKind.Csv, vm.Kind);
        Assert.Equal(35.7, vm.SamplePoints[0].X, 6);

        vm.IsLongitudeFirst = false;

        Assert.Equal(51.3, vm.SamplePoints[0].X, 6);
        Assert.Equal(35.7, vm.SamplePoints[0].Y, 6);
    }

    [Fact]
    public void PastedTabTextIsTsvAndSpaceSeparatedXyIsDelimitedOnOpen()
    {
        Assert.Equal(DataSourceKind.Tsv, Paste("51.3\t35.7\n51.4\t35.8\n51.5\t35.9").Kind);

        var vm = Paste("51.3 35.7\n51.4 35.8\n51.5 35.9");

        Assert.Equal(DataSourceKind.Csv, vm.Kind);
        Assert.Equal(3, vm.SamplePoints.Count);

        vm.OpenCommand.Execute(null);

        Assert.NotNull(vm.Result);
        Assert.Contains("51.3,35.7", vm.Result!.RawText);
        Assert.Null(vm.Result.FilePath);
    }

    [Fact]
    public void ChosenFormatStaysWhenTheTextChanges()
    {
        var vm = Paste(GeoJsonDegrees);

        vm.SelectedKindOption = vm.AvailableKinds.First(o => o.Kind == DataSourceKind.TopoJson);
        vm.RawText = GeoJsonUtm;

        Assert.Equal(DataSourceKind.TopoJson, vm.Kind);
        Assert.NotNull(vm.ValidationMessage);
    }

    [Fact]
    public void BrokenJsonCannotBeOpened()
    {
        var vm = Paste("{\"type\":\"FeatureCollection\",\"features\":[{");

        Assert.NotNull(vm.ValidationMessage);
        Assert.False(vm.OpenCommand.CanExecute(null));
    }

    [Fact]
    public void CsvValidationStillApplies()
    {
        var vm = Paste("51.3,35.7\n51.4,35.8");

        // The default geometry is a polygon: two points are too few.
        Assert.NotNull(vm.ValidationMessage);

        vm.SelectedGeometryMode = GeometryType.LineString;

        Assert.Null(vm.ValidationMessage);
    }

    [Fact]
    public async Task EsriJsonFileUsesTheCrsItNames()
    {
        var vm = await OpenFile("points.json", EsriJsonWebMercator, DataSourceKind.EsriJson);

        Assert.False(vm.IsPasteMode);
        Assert.False(vm.CanUserChooseSrs);
        Assert.Equal(SridHelper.WebMercator, vm.EffectiveSelectedSrid);
        Assert.Single(vm.SamplePoints);
        Assert.Null(vm.ValidationMessage);
        Assert.Equal("Esri JSON: points.json", vm.Title);

        // A .json file can be switched between the JSON kinds.
        Assert.Equal(3, vm.AvailableKinds.Count);
    }

    [Fact]
    public async Task CrsOutsideTheListIsAddedAsFromFile()
    {
        var vm = await OpenFile("lambert.json", EsriJsonOtherCrs, DataSourceKind.EsriJson);

        Assert.Equal(2154, vm.EffectiveSelectedSrid);
        Assert.Equal(4, vm.AvailableSrsOptions.Count);
        Assert.Null(vm.CoordinateWarning);
    }

    [Fact]
    public async Task SwitchingAFileToAnotherJsonKindReReadsIt()
    {
        var vm = await OpenFile("points.json", EsriJsonWebMercator, DataSourceKind.EsriJson);

        vm.SelectedKindOption = vm.AvailableKinds.First(o => o.Kind == DataSourceKind.GeoJson);

        Assert.True(vm.CanUserChooseSrs);
        Assert.Equal(3, vm.AvailableSrsOptions.Count);
        Assert.NotNull(vm.ValidationMessage);
    }

    [Fact]
    public async Task CsvFileShowsItsFirstLinesReadOnly()
    {
        var lines = string.Join("\n", Enumerable.Range(0, 80).Select(i => $"51.{i:00},35.{i:00}"));

        var vm = await OpenFile("points.csv", lines, DataSourceKind.Csv);

        Assert.True(vm.ShowsText);
        Assert.True(vm.IsTextReadOnly);
        Assert.Contains("(30 more lines)", vm.RawText);
        Assert.Equal(ImportOptionsDialogViewModel.PreviewPointCount, vm.SamplePoints.Count);
        Assert.Null(vm.ValidationMessage);

        vm.OpenCommand.Execute(null);

        Assert.Equal(DataSourceKind.Csv, vm.Result!.Kind);
        Assert.EndsWith("points.csv", vm.Result.FilePath);
        Assert.Null(vm.Result.RawText);
    }

    [Fact]
    public async Task OpenReturnsTheChosenOptions()
    {
        var vm = await OpenFile("roads.geojson", GeoJsonUtm, DataSourceKind.GeoJson);

        vm.UtmZone = 40;
        vm.OpenCommand.Execute(null);

        Assert.True(vm.DialogResult);
        Assert.Equal(DataSourceKind.GeoJson, vm.Result!.Kind);
        Assert.Equal(SridHelper.GetUtmSrid(40), vm.Result.SourceSrid);
        Assert.True(vm.Result.IsLongitudeFirst);
    }

    [Fact]
    public async Task CancelReturnsNothing()
    {
        var vm = await OpenFile("roads.geojson", GeoJsonDegrees, DataSourceKind.GeoJson);

        vm.CancelCommand.Execute(null);

        Assert.NotEqual(true, vm.DialogResult);
        Assert.Null(vm.Result);
    }

    [Fact]
    public void OnlyFormatOptionsThatApplyAreShown()
    {
        var vm = Paste(EsriJsonWebMercator);

        Assert.Equal(DataSourceKind.EsriJson, vm.Kind);
        Assert.False(vm.ShowsLongitudeFirst);
        Assert.False(vm.ShowsDelimitedOptions);
        Assert.True(vm.ShowsPrettyJson);

        vm.RawText = "51.3,35.7";

        Assert.True(vm.ShowsLongitudeFirst);
        Assert.True(vm.ShowsDelimitedOptions);
        Assert.False(vm.ShowsPrettyJson);
    }

    [Fact]
    public void PastedKmlIsWgs84AndLocked()
    {
        var vm = Paste("""<kml xmlns="http://www.opengis.net/kml/2.2"><Placemark><Point><coordinates>51.3,35.7,0</coordinates></Point></Placemark></kml>""");

        Assert.Equal(DataSourceKind.Kml, vm.Kind);
        Assert.False(vm.CanUserChooseSrs);
        Assert.Equal(SridHelper.GeodeticWGS84, vm.EffectiveSelectedSrid);
        Assert.Single(vm.SamplePoints);
        Assert.Equal(51.3, vm.SamplePoints[0].Longitude!.Value, 6);
        Assert.False(vm.ShowsLongitudeFirst);
        Assert.Null(vm.ValidationMessage);

        vm.OpenCommand.Execute(null);

        Assert.Equal(DataSourceKind.Kml, vm.Result!.Kind);
        Assert.Contains("<kml", vm.Result.RawText);
    }

    [Fact]
    public void KmlWithoutGeometryCannotBeOpened()
    {
        var vm = Paste("""<kml xmlns="http://www.opengis.net/kml/2.2"><Document><name>empty</name></Document></kml>""");

        Assert.Equal(DataSourceKind.Kml, vm.Kind);
        Assert.NotNull(vm.ValidationMessage);
        Assert.False(vm.OpenCommand.CanExecute(null));
    }

    [Fact]
    public void FileWithoutAKindIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new ImportOptionsDialogViewModel(Dialogs, null, Path.Combine(_folder, "a.json")));
        Assert.Throws<ArgumentException>(() => new ImportOptionsDialogViewModel(Dialogs, DataSourceKind.Kml, Path.Combine(_folder, "a.kml")));
    }
}
