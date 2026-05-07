using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Jab.Common.Services;
using IRI.Maptor.Jab.Common.Properties;
using IRI.Maptor.Sta.Spatial.IO.TopoJson;
using IRI.Maptor.Sta.Spatial.GeoJsonFormat;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Jab.Common.Models.DxfOpenDialog;
using System.Text.Json;

namespace IRI.Maptor.Jab.Common.ViewModels.Dialogs;

public class GeoJsonTopoJsonOpenDialogViewModel : DialogViewModelBase
{
    private readonly IDialogService _dialogService;
    private readonly SrsOption _utmOption;
    private string _filePath = string.Empty;
    private string _rawJson = string.Empty;
    private SrsOption? _selectedSrsOption;
    private int _utmZone = 39;
    private bool _utmHemisphereNorth = true;
    private bool _isLongitudeFirst = true;
    private ObservableCollection<PointDisplay> _samplePoints = new();

    public GeoJsonTopoJsonOpenDialogViewModel(IDialogService dialogService, bool isGeoJson = true, int? initialSrid = null)
    {
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        IsGeoJson = isGeoJson;

        _utmOption = new SrsOption { DisplayName = "UTM (user-defined zone)", IsUtm = true };
        AvailableSrsOptions = new ObservableCollection<SrsOption>
        {
            new() { FixedSrid = SridHelper.GeodeticWGS84, DisplayName = "WGS84 (EPSG:4326)", IsUtm = false },
            _utmOption,
            new() { FixedSrid = SridHelper.WebMercator, DisplayName = "Web Mercator (EPSG:3857)", IsUtm = false }
        };

        SelectedSrsOption = AvailableSrsOptions[0];

        if (initialSrid.HasValue && initialSrid.Value > 0)
            ApplyInitialSrid(initialSrid.Value);

        BrowseCommand = new RelayCommand(_ => Browse());
        OpenCommand = new RelayCommand(_ => Open(), _ => CanOpen());
        CancelCommand = new RelayCommand(_ => Cancel());
        RemoveFileCommand = new RelayCommand(_ => RemoveSelectedFile(), _ => !string.IsNullOrEmpty(FilePath));
        FormatJsonCommand = new RelayCommand(_ => FormatJson(true), _ => !string.IsNullOrEmpty(RawJson));

    }

    public bool IsGeoJson { get; }

    public string DialogTitle => IsGeoJson ? Resources.dialog_geoJsonOpen_title : Resources.dialog_topojsonOpen_title;

    public ObservableCollection<SrsOption> AvailableSrsOptions { get; }

    public string FilePath
    {
        get => _filePath;
        set
        {
            _filePath = value ?? string.Empty;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsRawTextReadOnly));
        }
    }

    public string RawJson
    {
        get => _rawJson;
        set
        {
            _rawJson = value ?? string.Empty;
            RaisePropertyChanged();
            UpdateSamplePoints();
            RaisePropertyChanged(nameof(CanOpen));
        }
    }

    public bool IsRawTextReadOnly => !string.IsNullOrWhiteSpace(FilePath);

    public SrsOption? SelectedSrsOption
    {
        get => _selectedSrsOption;
        set
        {
            _selectedSrsOption = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsUtmSelected));
            RaisePropertyChanged(nameof(EffectiveSelectedSrid));
        }
    }

    public int UtmZone
    {
        get => _utmZone;
        set
        {
            _utmZone = Math.Clamp(value, 1, 60);
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(EffectiveSelectedSrid));
        }
    }

    public bool IsNorthHemisphere
    {
        get => _utmHemisphereNorth;
        set
        {
            _utmHemisphereNorth = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(EffectiveSelectedSrid));
        }
    }

    public bool IsLongitudeFirst
    {
        get => _isLongitudeFirst;
        set { _isLongitudeFirst = value; RaisePropertyChanged(); UpdateSamplePoints(); }
    }

    public bool IsUtmSelected => SelectedSrsOption?.IsUtm == true;

    public int EffectiveSelectedSrid
    {
        get
        {
            if (SelectedSrsOption == null)
                return 0;
            if (SelectedSrsOption.IsUtm)
                return _utmHemisphereNorth ? SridHelper.GetUtmSrid(_utmZone) : SridHelper.GetUtmSouthSrid(_utmZone);
            return SelectedSrsOption.FixedSrid ?? 0;
        }
    }

    private bool _isPrettyJson = false;
    public bool IsPrettyJson
    {
        get { return _isPrettyJson; }
        set
        {
            _isPrettyJson = value;
            RaisePropertyChanged();
            FormatJson(value);
        }
    }


    public ObservableCollection<PointDisplay> SamplePoints
    {
        get => _samplePoints;
        set { _samplePoints = value ?? new ObservableCollection<PointDisplay>(); RaisePropertyChanged(); }
    }

    public RelayCommand BrowseCommand { get; }
    public RelayCommand OpenCommand { get; }
    public RelayCommand CancelCommand { get; }

    public RelayCommand RemoveFileCommand { get; }

    public RelayCommand FormatJsonCommand { get; set; }

    public GeoJsonTopoJsonOpenDialogResult? Result { get; private set; }

    private void FormatJson(bool isPretty)
    {
        try
        {
            // Parse the JSON into a DOM
            using JsonDocument document = JsonDocument.Parse(RawJson);

            // Serialize back with indentation
            string prettyJson = JsonSerializer.Serialize(document.RootElement, new JsonSerializerOptions
            {
                WriteIndented = isPretty
            });

            RawJson = prettyJson;
        }
        catch (Exception)
        {
            throw;
        }
    }

    private void ApplyInitialSrid(int srid)
    {
        if (srid >= 32601 && srid <= 32660)
        {
            SelectedSrsOption = _utmOption;
            UtmZone = srid - 32600;
            IsNorthHemisphere = true;
        }
        else if (srid >= 32701 && srid <= 32760)
        {
            SelectedSrsOption = _utmOption;
            UtmZone = srid - 32700;
            IsNorthHemisphere = false;
        }
        else
        {
            var match = AvailableSrsOptions.FirstOrDefault(o => !o.IsUtm && o.FixedSrid == srid);
            if (match != null)
                SelectedSrsOption = match;
        }
    }

    private void UpdateSamplePoints()
    {
        if (string.IsNullOrWhiteSpace(RawJson))
        {
            SamplePoints = new ObservableCollection<PointDisplay>();
            return;
        }

        try
        {
            if (IsGeoJson)
            {
                var featureSet = GeoJsonFeatureSet.Parse(RawJson);
                var points = GeoJsonFeatureSet.ExtractSamplePoints(featureSet, IsLongitudeFirst, 50);
                SamplePoints = new ObservableCollection<PointDisplay>(
                    points.Select((p, i) => new PointDisplay { Index = i + 1, X = p.X, Y = p.Y }));
            }
            else
            {
                var topology = TopoJson.Parse(RawJson);
                var points = TopoJson.ExtractSamplePoints(topology, EffectiveSelectedSrid > 0 ? EffectiveSelectedSrid : 4326, 50);
                SamplePoints = new ObservableCollection<PointDisplay>(
                    points.Select((p, i) => new PointDisplay { Index = i + 1, X = p.X, Y = p.Y }));
            }
        }
        catch
        {
            SamplePoints = new ObservableCollection<PointDisplay>();
        }
    }

    private async Task Browse()
    {
        //var filter = IsGeoJson
        //    ? "GeoJSON (*.json;*.geojson)|*.json;*.geojson|All files (*.*)|*.*"
        //    : "TopoJSON (*.json;*.topojson)|*.json;*.topojson|All files (*.*)|*.*";
        var filter = IsGeoJson ? DataSourceKind.GeoJson : DataSourceKind.TopoJson;

        var path = await _dialogService.ShowOpenFileDialogAsync(filter, null);

        if (string.IsNullOrEmpty(path))
            return;

        FilePath = path;
        try
        {
            RawJson = File.ReadAllText(path);
        }
        catch
        {
            RawJson = string.Empty;
        }
    }

    private bool CanOpen()
    {
        if (string.IsNullOrWhiteSpace(RawJson))
            return false;

        if (EffectiveSelectedSrid <= 0)
            return false;

        if (IsUtmSelected && (UtmZone < 1 || UtmZone > 60))
            return false;

        return true;
    }

    private void RemoveSelectedFile()
    {
        this.FilePath = string.Empty;
        this.RawJson = string.Empty;
        this.SamplePoints = new ObservableCollection<PointDisplay>();
    }

    private void Open()
    {
        if (!CanOpen())
            return;

        var jsonToImport = RawJson;

        if (!string.IsNullOrEmpty(FilePath) && File.Exists(FilePath))
        {
            try
            {
                jsonToImport = File.ReadAllText(FilePath);
            }
            catch
            {
                var idx = RawJson.IndexOf("... (", StringComparison.Ordinal);
                jsonToImport = idx >= 0 ? RawJson[..idx].TrimEnd() : RawJson;
            }
        }

        Result = new GeoJsonTopoJsonOpenDialogResult(
            string.IsNullOrEmpty(FilePath) ? null : FilePath,
            jsonToImport,
            EffectiveSelectedSrid,
            IsGeoJson,
            IsLongitudeFirst);
        DialogResult = true;
        RequestClose?.Invoke();
    }

    private void Cancel()
    {
        DialogResult = null;
        RequestClose?.Invoke();
    }
}
