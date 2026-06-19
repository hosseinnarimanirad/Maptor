using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

using IRI.Maptor.Jab.Maui.Controls;
using IRI.Maptor.Jab.Maui.Helpers;
using IRI.Maptor.Jab.Maui.Layers;
using IRI.Maptor.Jab.Maui.Mvvm;
using IRI.Maptor.Jab.Maui.Projects;
using IRI.Maptor.Jab.Maui.Services;

using Microsoft.Maui.Controls;
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
    private readonly IProjectService _projectService;

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
    private bool _isProjectsVisible;

    private Project? _currentProject;
    private bool _suppressPersist;
    private readonly HashSet<MapLayer> _trackedLayers = new();

    /// <summary>The active project, or null. Setting it updates the nav-bar title.</summary>
    private Project? CurrentProject
    {
        get => _currentProject;
        set
        {
            if (!ReferenceEquals(_currentProject, value))
            {
                _currentProject = value;
                OnPropertyChanged(nameof(CurrentProjectName));
                OnPropertyChanged(nameof(HasCurrentProject));
            }
        }
    }

    public MainViewModel()
        : this(new LocationService(), new GeoJsonFileService(), new ProjectService())
    {
    }

    public MainViewModel(
        ILocationService locationService,
        IGeoJsonFileService geoJsonFileService,
        IProjectService projectService)
    {
        _locationService = locationService;
        _geoJsonFileService = geoJsonFileService;
        _projectService = projectService;

        ZoomInCommand = new RelayCommand(() => ZoomLevel = Math.Min(19, ZoomLevel + 1));
        ZoomOutCommand = new RelayCommand(() => ZoomLevel = Math.Max(1, ZoomLevel - 1));
        GoToCommand = new RelayCommand(GoToTypedLocation);
        MyLocationCommand = new RelayCommand(async () => await ShowMyLocationAsync());
        AddGeoJsonCommand = new RelayCommand(async () => await AddGeoJsonAsync());
        LoadSampleCommand = new RelayCommand(async () => await LoadSampleAsync());
        ToggleLegendCommand = new RelayCommand(ToggleLegend);
        ToggleProjectsCommand = new RelayCommand(ToggleProjects);
        AddProjectCommand = new RelayCommand(async () => await AddProjectAsync());
        OpenProjectCommand = new RelayCommand<Project>(async project => await OpenProjectAsync(project));
        DeleteProjectCommand = new RelayCommand<Project>(async project => await DeleteProjectAsync(project));

        // Persist the current project whenever its layers change.
        Layers.CollectionChanged += OnLayersChanged;
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
        set
        {
            if (SetProperty(ref _selectedBaseMap, value))
            {
                PersistCurrentProject();
            }
        }
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

    /// <summary>Whether the projects slide-in panel is open.</summary>
    public bool IsProjectsVisible
    {
        get => _isProjectsVisible;
        set => SetProperty(ref _isProjectsVisible, value);
    }

    /// <summary>Name of the active project, shown in the nav bar (empty if none).</summary>
    public string CurrentProjectName => _currentProject?.Name ?? string.Empty;

    public bool HasCurrentProject => _currentProject is not null;

    public ObservableCollection<MauiBaseMap> BaseMaps { get; } = new(Enum.GetValues<MauiBaseMap>());

    /// <summary>Saved projects shown in the projects panel.</summary>
    public ObservableCollection<Project> Projects { get; } = new();

    public RelayCommand ZoomInCommand { get; }

    public RelayCommand ZoomOutCommand { get; }

    public RelayCommand GoToCommand { get; }

    public RelayCommand MyLocationCommand { get; }

    public RelayCommand AddGeoJsonCommand { get; }

    public RelayCommand LoadSampleCommand { get; }

    public RelayCommand ToggleLegendCommand { get; }

    public RelayCommand ToggleProjectsCommand { get; }

    public RelayCommand AddProjectCommand { get; }

    public RelayCommand<Project> OpenProjectCommand { get; }

    public RelayCommand<Project> DeleteProjectCommand { get; }

    public string Coordinates => $"Lat {Latitude:F4}°, Lon {Longitude:F4}°, Zoom {ZoomLevel}";

    /// <summary>Navigate to a free-form "lat, lon" string (used by the search button).</summary>
    public void GoTo(string locationText)
    {
        LocationText = locationText;
        GoToTypedLocation();
    }

    /// <summary>Loads saved projects from disk; call once when the page appears.</summary>
    public async Task LoadProjectsAsync()
    {
        var projects = await _projectService.LoadAllAsync();

        Projects.Clear();
        foreach (var project in projects)
        {
            Projects.Add(project);
        }
    }

    private void ToggleLegend()
    {
        IsLegendVisible = !IsLegendVisible;
        if (IsLegendVisible)
        {
            IsProjectsVisible = false;
        }
    }

    private void ToggleProjects()
    {
        IsProjectsVisible = !IsProjectsVisible;
        if (IsProjectsVisible)
        {
            IsLegendVisible = false;
        }
    }

    private async Task AddProjectAsync()
    {
        var page = Application.Current?.MainPage;
        if (page is null)
        {
            return;
        }

        var name = await page.DisplayPromptAsync(
            "New project",
            "Project name",
            initialValue: $"Project {Projects.Count + 1}");

        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        // Save whatever is currently shown into the previous project before switching.
        PersistCurrentProject();

        var project = new Project { Name = name.Trim(), BaseMap = SelectedBaseMap };
        await _projectService.SaveAsync(project);
        Projects.Add(project);

        // Switch to the new (empty) project.
        CurrentProject = project;
        _suppressPersist = true;
        Layers.Clear();
        _suppressPersist = false;

        IsProjectsVisible = false;
        StatusMessage = $"Project '{project.Name}' created.";
    }

    private Task OpenProjectAsync(Project? project)
    {
        if (project is null)
        {
            return Task.CompletedTask;
        }

        // Capture the outgoing project's current state first.
        PersistCurrentProject();

        CurrentProject = project;
        _suppressPersist = true;

        SelectedBaseMap = project.BaseMap;

        Layers.Clear();
        foreach (var stored in project.Layers)
        {
            var layer = BuildLayer(stored);
            if (layer is not null)
            {
                Layers.Add(layer);
            }
        }

        _suppressPersist = false;

        IsProjectsVisible = false;
        IsLegendVisible = true;
        StatusMessage = $"Opened '{project.Name}'.";

        return Task.CompletedTask;
    }

    private async Task DeleteProjectAsync(Project? project)
    {
        if (project is null)
        {
            return;
        }

        await _projectService.DeleteAsync(project);
        Projects.Remove(project);

        if (ReferenceEquals(project, _currentProject))
        {
            CurrentProject = null;
            _suppressPersist = true;
            Layers.Clear();
            _suppressPersist = false;
        }
    }

    /// <summary>Closes both slide-in panels (e.g. when the map is tapped).</summary>
    public void CloseSidebars()
    {
        IsLegendVisible = false;
        IsProjectsVisible = false;
    }

    private static MapLayer? BuildLayer(ProjectLayer stored)
    {
        if (string.IsNullOrEmpty(stored.GeoJson))
        {
            return null;
        }

        var color = Color.FromArgb(stored.ColorHex);
        var layer = GeoJsonLayerFactory.FromGeoJson(stored.GeoJson, stored.Name, color);
        layer.IsVisible = stored.IsVisible;
        return layer;
    }

    private void OnLayersChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Keep per-layer subscriptions in sync so edits (color, visibility, name) also persist.
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            foreach (var layer in _trackedLayers)
            {
                layer.PropertyChanged -= OnLayerPropertyChanged;
            }

            _trackedLayers.Clear();

            foreach (var layer in Layers)
            {
                layer.PropertyChanged += OnLayerPropertyChanged;
                _trackedLayers.Add(layer);
            }
        }
        else
        {
            foreach (MapLayer layer in e.OldItems ?? (System.Collections.IList)Array.Empty<MapLayer>())
            {
                layer.PropertyChanged -= OnLayerPropertyChanged;
                _trackedLayers.Remove(layer);
            }

            foreach (MapLayer layer in e.NewItems ?? (System.Collections.IList)Array.Empty<MapLayer>())
            {
                layer.PropertyChanged += OnLayerPropertyChanged;
                _trackedLayers.Add(layer);
            }
        }

        PersistCurrentProject();
    }

    private void OnLayerPropertyChanged(object? sender, PropertyChangedEventArgs e)
        => PersistCurrentProject();

    /// <summary>Captures the current layers/basemap into the active project and saves it.</summary>
    private async void PersistCurrentProject()
    {
        if (_suppressPersist || _currentProject is null)
        {
            return;
        }

        _currentProject.BaseMap = SelectedBaseMap;
        _currentProject.Layers = Layers
            .Where(l => !string.IsNullOrEmpty(l.SourceGeoJson))
            .Select(l => new ProjectLayer
            {
                Name = l.Name,
                Description = l.Description,
                ColorHex = l.Color.ToArgbHex(),
                IsVisible = l.IsVisible,
                GeoJson = l.SourceGeoJson!,
            })
            .ToList();

        await _projectService.SaveAsync(_currentProject);
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
