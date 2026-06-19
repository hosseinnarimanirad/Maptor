using System.Collections.ObjectModel;

using IRI.Maptor.Jab.Maui.Controls;
using IRI.Maptor.Jab.Maui.Helpers;
using IRI.Maptor.Jab.Maui.Layers;
using IRI.Maptor.Jab.Maui.Mvvm;
using IRI.Maptor.Jab.Maui.Services;

using Microsoft.Maui.Graphics;
using Microsoft.Maui.Storage;

namespace IRI.Maptor.SampleMauiApp.ViewModels;

public class MainViewModel : ObservableBase
{
    private const int LocationZoomLevel = 15;

    private static readonly Color[] _layerPalette =
    {
        Colors.Red, Colors.RoyalBlue, Colors.ForestGreen, Colors.Orange, Colors.MediumPurple,
    };

    private readonly ILocationService _locationService;
    private readonly IGeoJsonFileService _geoJsonFileService;

    private int _nextColorIndex;

    // Default view: Tehran, Iran.
    private double _latitude = 35.6892;
    private double _longitude = 51.3890;
    private int _zoomLevel = 11;
    private MauiBaseMap _selectedBaseMap = MauiBaseMap.GoogleRoadMap;

    private double? _markerLatitude;
    private double? _markerLongitude;

    private string _locationText = string.Empty;
    private string? _statusMessage;
    private bool _isBusy;
    private bool _isLegendVisible;
    private bool _isRecording;

    public MainViewModel()
        : this(new LocationService(), new GeoJsonFileService())
    {
    }

    public MainViewModel(ILocationService locationService, IGeoJsonFileService geoJsonFileService)
    {
        _locationService = locationService;
        _geoJsonFileService = geoJsonFileService;

        ZoomInCommand = new RelayCommand(() => ZoomLevel = Math.Min(19, ZoomLevel + 1));
        ZoomOutCommand = new RelayCommand(() => ZoomLevel = Math.Max(1, ZoomLevel - 1));
        GoToCommand = new RelayCommand(GoToTypedLocation);
        MyLocationCommand = new RelayCommand(async () => await ShowMyLocationAsync());
        AddGeoJsonCommand = new RelayCommand(async () => await AddGeoJsonAsync());
        LoadSampleCommand = new RelayCommand(async () => await LoadSampleAsync());
        ToggleLegendCommand = new RelayCommand(() => IsLegendVisible = !IsLegendVisible);
        RecordCommand = new RelayCommand(ToggleRecording);
    }

    /// <summary>Vector layers shown on the map and listed in the legend.</summary>
    public ObservableCollection<MapLayer> Layers { get; } = new();

    public double Latitude
    {
        get => _latitude;
        set
        {
            if (SetProperty(ref _latitude, value))
            {
                OnPropertyChanged(nameof(Coordinates));
            }
        }
    }

    public double Longitude
    {
        get => _longitude;
        set
        {
            if (SetProperty(ref _longitude, value))
            {
                OnPropertyChanged(nameof(Coordinates));
            }
        }
    }

    public int ZoomLevel
    {
        get => _zoomLevel;
        set
        {
            if (SetProperty(ref _zoomLevel, value))
            {
                OnPropertyChanged(nameof(Coordinates));
            }
        }
    }

    public MauiBaseMap SelectedBaseMap
    {
        get => _selectedBaseMap;
        set => SetProperty(ref _selectedBaseMap, value);
    }

    public double? MarkerLatitude
    {
        get => _markerLatitude;
        set => SetProperty(ref _markerLatitude, value);
    }

    public double? MarkerLongitude
    {
        get => _markerLongitude;
        set => SetProperty(ref _markerLongitude, value);
    }

    /// <summary>Free-form "lat, lon" text the user can paste and navigate to.</summary>
    public string LocationText
    {
        get => _locationText;
        set => SetProperty(ref _locationText, value);
    }

    public string? StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (SetProperty(ref _statusMessage, value))
            {
                OnPropertyChanged(nameof(HasStatus));
            }
        }
    }

    public bool HasStatus => !string.IsNullOrEmpty(StatusMessage);

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public bool IsLegendVisible
    {
        get => _isLegendVisible;
        set => SetProperty(ref _isLegendVisible, value);
    }

    public bool IsRecording
    {
        get => _isRecording;
        set
        {
            if (SetProperty(ref _isRecording, value))
            {
                OnPropertyChanged(nameof(RecordColor));
            }
        }
    }

    /// <summary>Glyph color for the record button: red while recording, white otherwise.</summary>
    public Color RecordColor => IsRecording ? Colors.Red : Colors.White;

    public ObservableCollection<MauiBaseMap> BaseMaps { get; } = new(Enum.GetValues<MauiBaseMap>());

    public RelayCommand ZoomInCommand { get; }

    public RelayCommand ZoomOutCommand { get; }

    public RelayCommand GoToCommand { get; }

    public RelayCommand MyLocationCommand { get; }

    public RelayCommand AddGeoJsonCommand { get; }

    public RelayCommand LoadSampleCommand { get; }

    public RelayCommand ToggleLegendCommand { get; }

    public RelayCommand RecordCommand { get; }

    public string Coordinates => $"Lat {Latitude:F4}°, Lon {Longitude:F4}°, Zoom {ZoomLevel}";

    /// <summary>Navigate to a free-form "lat, lon" string (used by the search button).</summary>
    public void GoTo(string locationText)
    {
        LocationText = locationText;
        GoToTypedLocation();
    }

    private void ToggleRecording()
    {
        IsRecording = !IsRecording;
        StatusMessage = IsRecording ? "Recording…" : null;
    }

    private void GoToTypedLocation()
    {
        if (CoordinateParser.TryParseLatLon(LocationText, out var latitude, out var longitude))
        {
            MoveTo(latitude, longitude);
            StatusMessage = null;
        }
        else
        {
            StatusMessage = "Enter coordinates as 'lat, lon' (e.g. 35.6892, 51.3890)";
        }
    }

    private async Task ShowMyLocationAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        StatusMessage = "Getting your location…";

        try
        {
            var location = await _locationService.GetCurrentLocationAsync();

            if (location is null)
            {
                StatusMessage = "Location unavailable. Check permissions and that location is enabled.";
                return;
            }

            MoveTo(location.Latitude, location.Longitude);
            StatusMessage = null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task AddGeoJsonAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        StatusMessage = "Opening GeoJSON…";

        try
        {
            var color = _layerPalette[_nextColorIndex++ % _layerPalette.Length];

            var layer = await _geoJsonFileService.PickAndLoadAsync(color);

            if (layer is null)
            {
                StatusMessage = null; // cancelled
                return;
            }

            Layers.Add(layer);          // MapViewer auto-zooms to the new layer
            IsLegendVisible = true;
            StatusMessage = null;
        }
        catch (Exception)
        {
            StatusMessage = "Could not load that file as GeoJSON.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadSampleAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        StatusMessage = "Loading sample…";

        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("sample.geojson");
            using var reader = new StreamReader(stream);
            var text = await reader.ReadToEndAsync();

            var color = _layerPalette[_nextColorIndex++ % _layerPalette.Length];
            var layer = GeoJsonLayerFactory.FromGeoJson(text, "Sample", color);

            Layers.Add(layer);
            IsLegendVisible = true;
            StatusMessage = null;
        }
        catch (Exception)
        {
            StatusMessage = "Could not load the bundled sample.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void MoveTo(double latitude, double longitude)
    {
        ZoomLevel = LocationZoomLevel;
        Latitude = latitude;
        Longitude = longitude;
        MarkerLatitude = latitude;
        MarkerLongitude = longitude;
    }
}
