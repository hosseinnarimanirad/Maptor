using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;

using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Helpers;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Common.Exceptions;
using IRI.Maptor.Core.Spatial.IO.Dxf;
using IRI.Maptor.Core.Spatial.IO.EsriJson;
using IRI.Maptor.Core.Spatial.IO.TopoJson;
using IRI.Maptor.Core.Spatial.GeoJsonFormat;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Core.SpatialReferenceSystem.MapProjections;
using IRI.Maptor.Core.Persistence.Import;
using IRI.Maptor.Presentation.Wpf.Services;
using IRI.Maptor.Presentation.Wpf.Models.DxfOpenDialog;
using IRI.Maptor.Presentation.Core.Localization;

namespace IRI.Maptor.Presentation.Wpf.ViewModels.Dialogs;

/// <summary>A format the user can switch the import to (shown in the format drop-down).</summary>
public sealed class ImportFormatOption
{
    public ImportFormatOption(DataSourceKind kind) => Kind = kind;

    public DataSourceKind Kind { get; }

    public string DisplayName => ImportOptionsDialogViewModel.GetFormatName(Kind);
}

/// <summary>
/// The one options dialog for every format whose file alone is not enough to add it (GeoJSON, TopoJSON,
/// Esri JSON, CSV, TSV, DXF, shapefile without a .prj). It opens with the file already chosen, or empty for
/// pasted text, and asks only what the format needs: the CRS always, plus the format's own options. The
/// preview of the first 50 points shows each point in WGS84 for the chosen CRS and warns when the values
/// cannot be in that CRS.
/// </summary>
public class ImportOptionsDialogViewModel : DialogViewModelBase
{
    public const int PreviewPointCount = 50;

    private readonly SrsOption _utmOption;
    private SrsOption? _fromFileOption;

    private DataSourceKind _kind;
    private string _rawText = string.Empty;
    private string? _fileText;
    private IReadOnlyList<Point> _filePoints = Array.Empty<Point>();
    private int _fileSrid;

    private SrsOption? _selectedSrsOption;
    private int _utmZone = 39;
    private bool _isNorthHemisphere = true;
    private bool _isLongitudeFirst = true;
    private bool _useFirstLineAsHeader;
    private GeometryType _geometryType = GeometryType.Polygon;
    private bool _isPrettyJson;
    private bool _isLoading;
    private bool _canUserChooseSrs = true;
    private string? _previewError;
    private string? _coordinateWarning;
    private IReadOnlyList<Point> _rawPoints = Array.Empty<Point>();
    private ObservableCollection<PointDisplay> _samplePoints = new();

    // A choice the user made is never overridden by a guess from the data.
    private bool _userChoseSrs;
    private bool _userChoseKind;
    private bool _settingInternally;

    /// <param name="dialogService">Shows error messages.</param>
    /// <param name="kind">The detected format; for pasted text, null until the text is detected.</param>
    /// <param name="filePath">The chosen file, or null for pasted text.</param>
    public ImportOptionsDialogViewModel(IDialogService dialogService, DataSourceKind? kind, string? filePath)
    {
        DialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        FilePath = string.IsNullOrWhiteSpace(filePath) ? null : filePath;

        if (!IsPasteMode && kind is null)
            throw new ArgumentNullException(nameof(kind), "A file needs its detected format.");

        _kind = kind ?? DataSourceKind.Csv;

        _utmOption = new SrsOption { DisplayName = "UTM (user-defined zone)", IsUtm = true };

        AvailableSrsOptions = new ObservableCollection<SrsOption>
        {
            new() { FixedSrid = SridHelper.GeodeticWGS84, DisplayName = "WGS84 (EPSG:4326)", IsUtm = false },
            _utmOption,
            new() { FixedSrid = SridHelper.WebMercator, DisplayName = "Web Mercator (EPSG:3857)", IsUtm = false }
        };

        _selectedSrsOption = AvailableSrsOptions[0];

        AvailableKinds = new ObservableCollection<ImportFormatOption>(GetAlternativeKinds().Select(k => new ImportFormatOption(k)));

        if (!AvailableKinds.Any(o => o.Kind == _kind))
            throw new ArgumentException($"{_kind} cannot be imported through the options dialog.", nameof(kind));

        OpenCommand = new RelayCommand(_ => Open(), _ => CanOpen());
        CancelCommand = new RelayCommand(_ => Cancel());

        Initialization = IsPasteMode ? Task.CompletedTask : LoadFileAsync();
    }

    /// <summary>Completes when the file has been read and the preview is ready (tests wait on it).</summary>
    public Task Initialization { get; }

    #region Source and format

    public bool IsPasteMode => FilePath is null;

    public string? FilePath { get; }

    public string? FileName => FilePath is null ? null : Path.GetFileName(FilePath);

    public string Title => IsPasteMode
        ? LocalizationManager.Instance["dialog_importOptions_pasteTitle"]
        : $"{FormatName}: {FileName}";

    public ObservableCollection<ImportFormatOption> AvailableKinds { get; }

    /// <summary>True when the format can be changed: for pasted text, and for files whose extension fits more than one format.</summary>
    public bool CanChooseKind => AvailableKinds.Count > 1;

    public DataSourceKind Kind => _kind;

    public string FormatName => GetFormatName(_kind);

    public ImportFormatOption? SelectedKindOption
    {
        get => AvailableKinds.FirstOrDefault(o => o.Kind == _kind);
        set
        {
            if (value is null || value.Kind == _kind)
                return;

            if (!_settingInternally)
                _userChoseKind = true;

            _kind = value.Kind;

            RaisePropertyChanged();
            RaiseKindDependentProperties();

            // The file data is already read: the JSON kinds share the text, CSV/TSV only change the delimiter.
            RefreshPreview();
        }
    }

    public static string GetFormatName(DataSourceKind kind) => kind switch
    {
        DataSourceKind.Shapefile => "Shapefile",
        DataSourceKind.GeoJson => "GeoJSON",
        DataSourceKind.TopoJson => "TopoJSON",
        DataSourceKind.EsriJson => "Esri JSON",
        DataSourceKind.Csv => "CSV",
        DataSourceKind.Tsv => "TSV",
        DataSourceKind.Dxf => "DXF",
        DataSourceKind.Kml => "KML",
        DataSourceKind.Kmz => "KMZ",
        DataSourceKind.Gpx => "GPX",
        DataSourceKind.GeoPackage => "GeoPackage",
        _ => kind.ToString(),
    };

    private IEnumerable<DataSourceKind> GetAlternativeKinds()
    {
        if (IsPasteMode)
            return ImportFormatDetector.PasteKinds;

        return _kind switch
        {
            DataSourceKind.Csv or DataSourceKind.Tsv => new[] { DataSourceKind.Csv, DataSourceKind.Tsv },
            DataSourceKind.GeoJson or DataSourceKind.TopoJson or DataSourceKind.EsriJson => new[] { DataSourceKind.GeoJson, DataSourceKind.TopoJson, DataSourceKind.EsriJson },
            DataSourceKind.Dxf or DataSourceKind.Shapefile => new[] { _kind },
            _ => Array.Empty<DataSourceKind>(),
        };
    }

    private static bool IsJsonKind(DataSourceKind kind) =>
        kind is DataSourceKind.GeoJson or DataSourceKind.TopoJson or DataSourceKind.EsriJson;

    private bool IsDelimited => _kind is DataSourceKind.Csv or DataSourceKind.Tsv;

    /// <summary>X/Y order matters only where the reader takes it: GeoJSON and CSV/TSV.</summary>
    public bool ShowsLongitudeFirst => _kind is DataSourceKind.GeoJson or DataSourceKind.Csv or DataSourceKind.Tsv;

    public bool ShowsDelimitedOptions => IsDelimited;

    public bool ShowsPrettyJson => IsPasteMode && IsJsonKind(_kind);

    /// <summary>The row with the X/Y order and the pretty-JSON toggle.</summary>
    public bool ShowsOrderOptions => ShowsLongitudeFirst || ShowsPrettyJson;

    /// <summary>The text box: editable for pasted text, a read-only view of the first lines of a CSV/TSV file.</summary>
    public bool ShowsText => IsPasteMode || IsDelimited;

    public bool IsTextReadOnly => !IsPasteMode;

    public string? PreviewHint => _kind == DataSourceKind.Shapefile
        ? LocalizationManager.Instance["dialog_importOptions_extentHint"]
        : null;

    private void RaiseKindDependentProperties()
    {
        RaisePropertyChanged(nameof(Kind));
        RaisePropertyChanged(nameof(FormatName));
        RaisePropertyChanged(nameof(Title));
        RaisePropertyChanged(nameof(ShowsLongitudeFirst));
        RaisePropertyChanged(nameof(ShowsDelimitedOptions));
        RaisePropertyChanged(nameof(ShowsPrettyJson));
        RaisePropertyChanged(nameof(ShowsOrderOptions));
        RaisePropertyChanged(nameof(ShowsText));
        RaisePropertyChanged(nameof(PreviewHint));
    }

    #endregion

    #region Text

    /// <summary>Pasted text, or the first lines of a CSV/TSV file.</summary>
    public string RawText
    {
        get => _rawText;
        set
        {
            _rawText = value ?? string.Empty;
            RaisePropertyChanged();

            if (IsPasteMode)
                DetectPastedKind();

            RefreshPreview();
        }
    }

    private void DetectPastedKind()
    {
        if (_userChoseKind)
            return;

        var detected = ImportFormatDetector.DetectFromText(_rawText);

        var option = detected is null ? null : AvailableKinds.FirstOrDefault(o => o.Kind == detected);

        if (option is null || option.Kind == _kind)
            return;

        _settingInternally = true;
        try { SelectedKindOption = option; }
        finally { _settingInternally = false; }
    }

    public bool IsPrettyJson
    {
        get => _isPrettyJson;
        set
        {
            _isPrettyJson = value;
            RaisePropertyChanged();
            FormatJson(value);
        }
    }

    private void FormatJson(bool indented)
    {
        if (string.IsNullOrWhiteSpace(_rawText))
            return;

        try
        {
            using var document = JsonDocument.Parse(_rawText);

            RawText = JsonSerializer.Serialize(document.RootElement, new JsonSerializerOptions { WriteIndented = indented });
        }
        catch (JsonException)
        {
            // Not valid JSON yet: leave the text as the user typed it; the footer says why Open is disabled.
        }
    }

    #endregion

    #region Format options

    public bool IsLongitudeFirst
    {
        get => _isLongitudeFirst;
        set { _isLongitudeFirst = value; RaisePropertyChanged(); RefreshPreview(); }
    }

    public bool UseFirstLineAsHeader
    {
        get => _useFirstLineAsHeader;
        set { _useFirstLineAsHeader = value; RaisePropertyChanged(); RefreshPreview(); }
    }

    public GeometryType SelectedGeometryMode
    {
        get => _geometryType;
        set { _geometryType = value; RaisePropertyChanged(); RefreshPreview(); }
    }

    private char Delimiter => _kind == DataSourceKind.Tsv ? IOHelper.TsvDelimiterChar : IOHelper.CsvDelimiterChar;

    #endregion

    #region Spatial reference

    public ObservableCollection<SrsOption> AvailableSrsOptions { get; }

    public SrsOption? SelectedSrsOption
    {
        get => _selectedSrsOption;
        set
        {
            if (!_settingInternally)
                _userChoseSrs = true;

            _selectedSrsOption = value;
            RaisePropertyChanged();
            OnSrsChanged();
        }
    }

    public int UtmZone
    {
        get => _utmZone;
        set { _utmZone = Math.Clamp(value, 1, 60); RaisePropertyChanged(); OnSrsChanged(); }
    }

    public bool IsNorthHemisphere
    {
        get => _isNorthHemisphere;
        set { _isNorthHemisphere = value; RaisePropertyChanged(); OnSrsChanged(); }
    }

    public bool IsUtmSelected => SelectedSrsOption?.IsUtm == true;

    /// <summary>False when the file names its CRS (DXF, Esri JSON): that CRS is used as it is.</summary>
    public bool CanUserChooseSrs
    {
        get => _canUserChooseSrs;
        private set { _canUserChooseSrs = value; RaisePropertyChanged(); }
    }

    public int EffectiveSelectedSrid
    {
        get
        {
            if (SelectedSrsOption is null)
                return 0;

            if (SelectedSrsOption.IsUtm)
                return _isNorthHemisphere ? SridHelper.GetUtmSrid(_utmZone) : SridHelper.GetUtmSouthSrid(_utmZone);

            return SelectedSrsOption.FixedSrid ?? 0;
        }
    }

    private void OnSrsChanged()
    {
        RaisePropertyChanged(nameof(IsUtmSelected));
        RaisePropertyChanged(nameof(EffectiveSelectedSrid));

        // TopoJSON reads its points through the CRS; the others only need new WGS84 columns and warning.
        if (_kind == DataSourceKind.TopoJson)
            RefreshPreview();
        else
            UpdatePointsAndWarning();
    }

    private void SelectSrsInternally(SrsOption option)
    {
        // Re-selecting the same option must not refresh again: a TopoJSON refresh guesses the CRS.
        if (ReferenceEquals(option, _selectedSrsOption))
            return;

        _settingInternally = true;
        try { SelectedSrsOption = option; }
        finally { _settingInternally = false; }
    }

    private void ApplySrid(int srid)
    {
        var utm = SridHelper.GetUtmZone(srid);

        if (utm.isUtm)
        {
            _utmZone = utm.zone!.Value;
            _isNorthHemisphere = utm.isNorthHemisphere!.Value;
            RaisePropertyChanged(nameof(UtmZone));
            RaisePropertyChanged(nameof(IsNorthHemisphere));
            SelectSrsInternally(_utmOption);
            return;
        }

        var match = AvailableSrsOptions.FirstOrDefault(o => !o.IsUtm && o != _fromFileOption && o.FixedSrid == srid);

        if (match is null)
        {
            if (_fromFileOption is null || _fromFileOption.FixedSrid != srid)
            {
                RemoveFromFileOption();

                _fromFileOption = new SrsOption
                {
                    FixedSrid = srid,
                    DisplayName = string.Format(LocalizationManager.Instance["dialog_importOptions_srsFromFile"], srid),
                };

                AvailableSrsOptions.Add(_fromFileOption);
            }

            match = _fromFileOption;
        }

        SelectSrsInternally(match);
    }

    private void RemoveFromFileOption()
    {
        if (_fromFileOption is null)
            return;

        if (SelectedSrsOption == _fromFileOption)
            SelectSrsInternally(AvailableSrsOptions[0]);

        AvailableSrsOptions.Remove(_fromFileOption);
        _fromFileOption = null;
    }

    /// <summary>
    /// The data names no CRS: pick the option the coordinates fit, so projected values are not opened as
    /// degrees. The UTM zone cannot be told from the coordinates and is left for the user to set.
    /// </summary>
    private void GuessSrs(IReadOnlyList<Point> points)
    {
        if (_userChoseSrs || points.Count == 0)
            return;

        if (points.All(IsDegrees))
            SelectSrsInternally(AvailableSrsOptions.First(o => o.FixedSrid == SridHelper.GeodeticWGS84));
        else if (points.All(IsInUtmRange))
            SelectSrsInternally(_utmOption);
    }

    #endregion

    #region Preview

    public bool IsLoading
    {
        get => _isLoading;
        private set { _isLoading = value; RaisePropertyChanged(); RefreshValidation(); }
    }

    public ObservableCollection<PointDisplay> SamplePoints
    {
        get => _samplePoints;
        private set { _samplePoints = value; RaisePropertyChanged(); }
    }

    /// <summary>Why the coordinates may not fit the chosen CRS, or null. A hint only: Open stays enabled.</summary>
    public string? CoordinateWarning
    {
        get => _coordinateWarning;
        private set { _coordinateWarning = value; RaisePropertyChanged(); }
    }

    private async Task LoadFileAsync()
    {
        IsLoading = true;

        try
        {
            var path = FilePath!;
            var kind = _kind;

            switch (kind)
            {
                case DataSourceKind.Csv:
                case DataSourceKind.Tsv:
                    _rawText = await Task.Run(() => ReadFirstLines(path, PreviewPointCount));
                    RaisePropertyChanged(nameof(RawText));
                    break;

                case DataSourceKind.GeoJson:
                case DataSourceKind.TopoJson:
                case DataSourceKind.EsriJson:
                    _fileText = await Task.Run(() => File.ReadAllText(path));
                    break;

                case DataSourceKind.Dxf:
                    var preview = await DxfReader.GetPreviewAsync(path, PreviewPointCount);
                    _filePoints = preview.SamplePoints;
                    _fileSrid = preview.DetectedSrid;
                    break;

                case DataSourceKind.Shapefile:
                    var box = await Task.Run(() => IRI.Maptor.Core.ShapefileFormat.Shapefile.GetFileHeader(path).MinimumBoundingBox);
                    _filePoints = new[] { new Point(box.XMin, box.YMin), new Point(box.XMax, box.YMax) };
                    break;
            }

            _previewError = null;
        }
        catch (Exception ex)
        {
            _previewError = ex is IOException or UnauthorizedAccessException
                ? MaptorLockedFileException.Instance.Message
                : string.Format(LocalizationManager.Instance["dialog_importOptions_validationUnreadable"], FormatName);
        }
        finally
        {
            _isLoading = false;
            RaisePropertyChanged(nameof(IsLoading));
        }

        if (_previewError is null)
            RefreshPreview();
        else
            RefreshValidation();
    }

    private static string ReadFirstLines(string path, int count)
    {
        var lines = new List<string>(count);
        int total = 0;

        foreach (var line in File.ReadLines(path))
        {
            if (total < count)
                lines.Add(line);

            total++;
        }

        var text = string.Join(Environment.NewLine, lines);

        return total > count ? text + Environment.NewLine + $"... ({total - count} more lines)" : text;
    }

    /// <summary>Re-reads the points from the current text/file data and options.</summary>
    private void RefreshPreview()
    {
        if (IsLoading)
            return;

        int detectedSrid = 0;
        IReadOnlyList<Point> points;

        try
        {
            _previewError = null;
            points = ReadPoints(out detectedSrid);
        }
        catch (Exception)
        {
            points = Array.Empty<Point>();

            _previewError = string.IsNullOrWhiteSpace(CurrentText) && IsPasteMode
                ? null
                : string.Format(LocalizationManager.Instance["dialog_importOptions_validationUnreadable"], FormatName);
        }

        _rawPoints = points;

        if (detectedSrid > 0)
        {
            ApplySrid(detectedSrid);
            CanUserChooseSrs = false;
        }
        else
        {
            CanUserChooseSrs = true;
            RemoveFromFileOption();
            GuessSrs(points);
        }

        UpdatePointsAndWarning();
    }

    private string CurrentText => IsPasteMode ? _rawText : (_fileText ?? string.Empty);

    private IReadOnlyList<Point> ReadPoints(out int detectedSrid)
    {
        detectedSrid = 0;

        switch (_kind)
        {
            case DataSourceKind.Csv:
            case DataSourceKind.Tsv:
                return DelimitedTextValidator.ExtractSamplePoints(
                    GetDelimitedTextToImport(), Delimiter, UseFirstLineAsHeader, IsLongitudeFirst, SelectedGeometryMode,
                    skipPreviewMarker: !IsPasteMode, PreviewPointCount);

            case DataSourceKind.GeoJson:
                if (string.IsNullOrWhiteSpace(CurrentText))
                    return Array.Empty<Point>();
                return GeoJsonFeatureSet.ExtractSamplePoints(GeoJsonFeatureSet.Parse(CurrentText), IsLongitudeFirst, PreviewPointCount);

            case DataSourceKind.TopoJson:
                if (string.IsNullOrWhiteSpace(CurrentText))
                    return Array.Empty<Point>();
                var srid = EffectiveSelectedSrid > 0 ? EffectiveSelectedSrid : SridHelper.GeodeticWGS84;
                return TopoJson.ExtractSamplePoints(TopoJson.Parse(CurrentText), srid, PreviewPointCount);

            case DataSourceKind.EsriJson:
                if (string.IsNullOrWhiteSpace(CurrentText))
                    return Array.Empty<Point>();
                return ReadEsriJsonPoints(CurrentText, out detectedSrid);

            case DataSourceKind.Dxf:
                detectedSrid = _fileSrid;
                return _filePoints;

            case DataSourceKind.Shapefile:
                return _filePoints;

            default:
                return Array.Empty<Point>();
        }
    }

    private static IReadOnlyList<Point> ReadEsriJsonPoints(string json, out int detectedSrid)
    {
        var featureSet = EsriJsonFeatureSet.Parse(json);

        if (featureSet is null || featureSet.Features is null || featureSet.Features.Count == 0)
            throw new InvalidDataException("No Esri JSON features.");

        detectedSrid = featureSet.SpatialReference?.LatestWkid ?? featureSet.SpatialReference?.Wkid ?? 0;

        var result = new List<Point>();

        foreach (var feature in featureSet.Features)
        {
            if (feature?.Geometry is null)
                continue;

            foreach (var p in feature.AsFeature(detectedSrid).TheGeometry.GetAllPoints())
            {
                result.Add(new Point(p.X, p.Y));

                if (result.Count >= PreviewPointCount)
                    return result;
            }
        }

        return result;
    }

    private void UpdatePointsAndWarning()
    {
        var srid = EffectiveSelectedSrid;

        SrsBase? source = null;

        try { source = srid > 0 ? SrsBase.Create(srid) : null; }
        catch { source = null; }

        SamplePoints = new ObservableCollection<PointDisplay>(_rawPoints.Select((p, i) =>
        {
            var wgs84 = ToWgs84(p, srid, source);

            return new PointDisplay { Index = i + 1, X = p.X, Y = p.Y, Longitude = wgs84?.X, Latitude = wgs84?.Y };
        }));

        CoordinateWarning = GetCoordinateWarning(_rawPoints, srid, SelectedSrsOption == _fromFileOption && _fromFileOption is not null);

        RefreshValidation();
    }

    private static Point? ToWgs84(Point p, int srid, SrsBase? source)
    {
        if (srid == SridHelper.GeodeticWGS84)
            return IsDegrees(p) ? p : null;

        if (source is null)
            return null;

        try
        {
            var result = p.Project(source, SrsBases.GeodeticWgs84);

            return double.IsFinite(result.X) && double.IsFinite(result.Y) && IsDegrees(result) ? result : null;
        }
        catch
        {
            return null;
        }
    }

    private const double WebMercatorLimit = 20037508.3427892;

    private static bool IsDegrees(Point p) => Math.Abs(p.X) <= 180 && Math.Abs(p.Y) <= 90;

    // UTM easting is about 166 000–834 000 m inside a zone; northing 0–10 000 000 m.
    private static bool IsInUtmRange(Point p) => p.X > 100_000 && p.X < 900_000 && p.Y >= 0 && p.Y <= 10_000_000;

    private static bool IsInWebMercatorRange(Point p) => Math.Abs(p.X) <= WebMercatorLimit && Math.Abs(p.Y) <= WebMercatorLimit * 1.001;

    /// <summary>
    /// A warning when the coordinates cannot be in the CRS: values beyond ±180/±90 read as degrees, degree-sized
    /// values read as metres, or values outside the usual UTM / Web Mercator range. None for a CRS from the file.
    /// </summary>
    internal static string? GetCoordinateWarning(IReadOnlyList<Point> points, int srid, bool srsFromFile)
    {
        if (points.Count == 0 || srid <= 0 || srsFromFile)
            return null;

        var loc = LocalizationManager.Instance;

        if (srid == SridHelper.GeodeticWGS84)
        {
            int i = IndexOf(points, p => !IsDegrees(p));

            return i < 0 ? null : string.Format(loc["dialog_importOptions_warnNotDegrees"], i + 1, Format(points[i].X), Format(points[i].Y));
        }

        bool isUtm = SridHelper.GetUtmZone(srid).isUtm;

        if (!isUtm && srid != SridHelper.WebMercator)
            return null;

        if (points.All(IsDegrees))
            return loc["dialog_importOptions_warnLooksLikeDegrees"];

        int j = IndexOf(points, p => isUtm ? !IsInUtmRange(p) : !IsInWebMercatorRange(p));

        return j < 0 ? null : string.Format(loc["dialog_importOptions_warnOutsideRange"], j + 1, Format(points[j].X), Format(points[j].Y));
    }

    private static int IndexOf(IReadOnlyList<Point> points, Func<Point, bool> predicate)
    {
        for (int i = 0; i < points.Count; i++)
        {
            if (predicate(points[i]))
                return i;
        }

        return -1;
    }

    private static string Format(double value) => value.ToString("0.######", CultureInfo.InvariantCulture);

    #endregion

    #region Validation and result

    public RelayCommand OpenCommand { get; }

    public RelayCommand CancelCommand { get; }

    /// <summary>Set when the user presses Open.</summary>
    public ImportOptions? Result { get; private set; }

    /// <summary>
    /// Why Open is disabled, or null when it is enabled. Shown in the dialog footer so the user is not left
    /// guessing at a greyed-out button.
    /// </summary>
    public string? ValidationMessage
    {
        get
        {
            var loc = LocalizationManager.Instance;

            if (IsLoading)
                return null;

            if (IsPasteMode && string.IsNullOrWhiteSpace(_rawText))
                return loc["dialog_importOptions_validationNoText"];

            if (_previewError is not null)
                return _previewError;

            if (EffectiveSelectedSrid <= 0)
                return loc["dialog_common_validationNoSrs"];

            if (IsUtmSelected && (UtmZone < 1 || UtmZone > 60))
                return loc["dialog_common_validationUtmZone"];

            if (IsDelimited)
                return ValidateDelimitedText();

            if (_rawPoints.Count == 0)
                return loc["dialog_importOptions_validationNoPoints"];

            return null;
        }
    }

    private string? ValidateDelimitedText()
    {
        var result = DelimitedTextValidator.Validate(
            GetDelimitedTextToImport(), Delimiter, UseFirstLineAsHeader, IsLongitudeFirst, SelectedGeometryMode, skipPreviewMarker: !IsPasteMode);

        var loc = LocalizationManager.Instance;

        return result.Error switch
        {
            DelimitedTextError.None => null,
            DelimitedTextError.TooFewColumns => string.Format(loc["dialog_csvTsvOpen_validationTooFewColumns"], result.Line),
            DelimitedTextError.ColumnCountMismatch => string.Format(loc["dialog_csvTsvOpen_validationColumnCount"], result.Line, result.Columns, result.ExpectedColumns),
            DelimitedTextError.NotNumber => string.Format(loc["dialog_csvTsvOpen_validationNotNumber"], result.Line),
            DelimitedTextError.TooFewPoints => string.Format(loc["dialog_csvTsvOpen_validationTooFewPoints"], result.RequiredPoints),
            _ => null,
        };
    }

    /// <summary>
    /// The CSV/TSV text that is checked and imported. Pasted two-number lines separated by spaces get the
    /// delimiter; the text box itself is never changed. A file's first lines are used as they are.
    /// </summary>
    private string GetDelimitedTextToImport() =>
        IsPasteMode ? DelimitedTextValidator.ConvertSpaceSeparatedXy(_rawText, Delimiter, UseFirstLineAsHeader) : _rawText;

    private void RefreshValidation()
    {
        RaisePropertyChanged(nameof(ValidationMessage));
        OpenCommand?.RaiseCanExecuteChanged();
    }

    private bool CanOpen() => !IsLoading && ValidationMessage is null;

    private void Open()
    {
        if (!CanOpen())
            return;

        string? text = null;

        if (IsPasteMode)
            text = IsDelimited ? GetDelimitedTextToImport() : _rawText;

        Result = new ImportOptions(
            _kind,
            FilePath,
            text,
            EffectiveSelectedSrid,
            IsLongitudeFirst,
            UseFirstLineAsHeader,
            SelectedGeometryMode);

        DialogResult = true;

        RequestClose?.Invoke();
    }

    private void Cancel()
    {
        DialogResult = null;

        RequestClose?.Invoke();
    }

    #endregion
}
