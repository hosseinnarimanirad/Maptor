using System;
using System.IO;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using IRI.Maptor.Jab.Common.Abstractions;
using IRI.Maptor.Jab.Common.Assets.Commands;
using IRI.Maptor.Jab.Common.Helpers;
using IRI.Maptor.Jab.Common.Models;
using IRI.Maptor.Sta.Spatial.Helpers;
using IRI.Maptor.Sta.SpatialReferenceSystem;

namespace IRI.Maptor.Jab.Common.ViewModels.Map;

public sealed class MapExtentPanelViewModel : Notifier, IDisposable
{
    //private readonly MapViewModelBase _map;
    private readonly IMapExtentBookmarkStore _store;
    private bool _disposed;

    private string _newBookmarkTitle = string.Empty;
    private string _zoomLevelText = string.Empty;
    private string _scaleText = string.Empty;
    private string _groundResolutionText = string.Empty;
    private ScaleComboItem? _selectedScaleItem;

    public MapExtentPanelViewModel(MapViewModelBase map, IMapExtentBookmarkStore? store = null)
    {
        Map = map ?? throw new ArgumentNullException(nameof(map));
        _store = store ?? new JsonMapExtentBookmarkStore();

        //Map = map;

        foreach (var g in GoogleScale.GoogleScales)
            _scaleItems.Add(new ScaleComboItem("Google", g));

        foreach (var s in ScaleModel.Scales)
            _scaleItems.Add(new ScaleComboItem("Standard", s));

        var cvs = new CollectionViewSource { Source = _scaleItems };
        cvs.GroupDescriptions.Add(new PropertyGroupDescription(nameof(ScaleComboItem.Group)));
        ScaleItemsView = cvs.View;

        Bookmarks = new ObservableCollection<MapExtentBookmark>();
        foreach (var b in _store.Load())
        {
            b.LoadThumbnail();
            Bookmarks.Add(b);
        }

        Map.OnZoomChanged += OnMapZoomChanged;
        Map.OnMapExtentChanged += OnMapExtentChangedHandler;

        RefreshMetrics();
    }

    /// <summary>Same instance as <see cref="_map"/> — for binding toolbar commands in XAML.</summary>
    public MapViewModelBase Map { get; }

    public ICollectionView ScaleItemsView { get; }

    private readonly ObservableCollection<ScaleComboItem> _scaleItems = new();

    public ObservableCollection<MapExtentBookmark> Bookmarks { get; }

    public string NewBookmarkTitle
    {
        get => _newBookmarkTitle;
        set
        {
            if (_newBookmarkTitle == value)
                return;

            _newBookmarkTitle = value;
            RaisePropertyChanged();
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public string ZoomLevelText
    {
        get => _zoomLevelText;
        private set
        {
            if (_zoomLevelText == value)
                return;

            _zoomLevelText = value;
            RaisePropertyChanged();
        }
    }

    public string ScaleText
    {
        get => _scaleText;
        private set
        {
            if (_scaleText == value)
                return;

            _scaleText = value;
            RaisePropertyChanged();
        }
    }

    public string GroundResolutionText
    {
        get => _groundResolutionText;
        private set
        {
            if (_groundResolutionText == value)
                return;

            _groundResolutionText = value;
            RaisePropertyChanged();
        }
    }

    public ScaleComboItem? SelectedScaleItem
    {
        get => _selectedScaleItem;
        set
        {
            if (_selectedScaleItem == value)
                return;

            _selectedScaleItem = value;
            RaisePropertyChanged();
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private RelayCommand? _saveBookmarkCommand;
    public RelayCommand SaveBookmarkCommand =>
        _saveBookmarkCommand ??= new RelayCommand(
            async _ =>
            {
                var title = NewBookmarkTitle?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(title))
                    return;

                var extent   = Map.CurrentExtent;
                var bookmark = MapExtentBookmark.FromTitleAndExtent(title, extent);

                if (Map.RequestCaptureThumbnailAsync is not null)
                {
                    var bmp = await Map.RequestCaptureThumbnailAsync(extent, 75, 75);
                    if (bmp is not null)
                    {
                        bookmark.ThumbnailBytes = EncodePng(bmp);
                        bookmark.LoadThumbnail();
                    }
                }

                Bookmarks.Add(bookmark);
                _store.Save(Bookmarks);
                NewBookmarkTitle = string.Empty;
            },
            _ => !string.IsNullOrWhiteSpace(NewBookmarkTitle));

    private static byte[] EncodePng(BitmapSource bmp)
    {
        using var ms = new MemoryStream();
        var enc = new PngBitmapEncoder();
        enc.Frames.Add(BitmapFrame.Create(bmp));
        enc.Save(ms);
        return ms.ToArray();
    }

    private RelayCommand? _goToBookmarkCommand;
    public RelayCommand GoToBookmarkCommand =>
        _goToBookmarkCommand ??= new RelayCommand(p =>
        {
            if (p is not MapExtentBookmark b)
                return;

            Map.ZoomToExtent(b.ToBoundingBox(), isExactExtent: true, isNewExtent: true);
        });

    private RelayCommand? _deleteBookmarkCommand;
    public RelayCommand DeleteBookmarkCommand =>
        _deleteBookmarkCommand ??= new RelayCommand(p =>
        {
            if (p is not MapExtentBookmark b)
                return;

            Bookmarks.Remove(b);
            _store.Save(Bookmarks);
        });

    private RelayCommand? _applyScaleCommand;
    public RelayCommand ApplyScaleCommand =>
        _applyScaleCommand ??= new RelayCommand(
            _ =>
            {
                var item = SelectedScaleItem;
                if (item is null)
                    return;

                var center = Map.CurrentExtent.Center;

                if (item.Model is GoogleScale g)
                {
                    Map.ZoomAndCenterToGoogleZoomLevel(g.ZoomLevel, center);
                }
                else
                {
                    Map.Zoom(item.Model.Scale, center);
                }
            },
            _ => SelectedScaleItem is not null);

    private void OnMapZoomChanged(object? sender, double mapScale)
    {
        RefreshMetrics();
    }

    private void OnMapExtentChangedHandler(object? sender, EventArgs e)
    {
        RefreshMetrics();
    }

    private void RefreshMetrics()
    {
        ZoomLevelText = Map.CurrentZoomLevel.ToString();
        ScaleText = $"1:{Map.MapScale:N0}";

        var center = Map.CurrentExtent.Center;
        var wgs = MapProjects.WebMercatorToGeodeticWgs84(center);
        double lat = wgs?.Y ?? 0;
        var gr = WebMercatorUtility.CalculateGroundResolution(Map.CurrentZoomLevel, lat);
        GroundResolutionText = $"{gr:N2} m/px";
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        Map.OnZoomChanged -= OnMapZoomChanged;
        Map.OnMapExtentChanged -= OnMapExtentChangedHandler;
    }
}

public sealed class ScaleComboItem
{
    public ScaleComboItem(string group, ScaleModel model)
    {
        Group = group;
        Model = model;
    }

    public string Group { get; }

    public ScaleModel Model { get; }

    public string DisplayLabel => Model is GoogleScale g ? g.ToString() : $"1:{Model.InverseScale:N0}";
}
