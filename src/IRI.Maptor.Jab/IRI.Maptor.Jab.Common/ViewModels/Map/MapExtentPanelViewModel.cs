using System;
using System.Windows.Data;
using System.Windows.Input;
using System.ComponentModel;
using System.Collections.ObjectModel;

using IRI.Maptor.Jab.Common.Models;
using IRI.Maptor.Jab.Common.Helpers;
using IRI.Maptor.Sta.Spatial.Helpers;
using IRI.Maptor.Jab.Common.Abstractions;
using IRI.Maptor.Jab.Common.Assets.Commands;
using IRI.Maptor.Sta.SpatialReferenceSystem;

namespace IRI.Maptor.Jab.Common.ViewModels.Map;

public sealed class MapExtentPanelViewModel : Notifier, IDisposable
{
    //private readonly MapViewModelBase _map;
    private readonly IMapExtentBookmarkStore _store;

    private bool _disposed;

    private ScaleComboItem? _selectedScaleItem;
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

    public ICollectionView ScaleItemsView { get; }

    private readonly ObservableCollection<ScaleComboItem> _scaleItems = new();

    public ObservableCollection<MapExtentBookmark> Bookmarks { get; }

    /// <summary>Same instance as <see cref="_map"/> — for binding toolbar commands in XAML.</summary>
    public MapViewModelBase Map { get; }

    private string _newBookmarkTitle = string.Empty;
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

    //private string _zoomLevelText = string.Empty;
    //public string ZoomLevelText
    //{
    //    get => _zoomLevelText;
    //    private set
    //    {
    //        if (_zoomLevelText == value)
    //            return;

    //        _zoomLevelText = value;
    //        RaisePropertyChanged();
    //    }
    //}
    public string ZoomLevelText => Map.NearestZoomLevel.ToString();

    //private string _scaleText = string.Empty;
    //public string ScaleText
    //{
    //    get => _scaleText;
    //    private set
    //    {
    //        if (_scaleText == value)
    //            return;

    //        _scaleText = value;
    //        RaisePropertyChanged();
    //    }
    //}
    public string ScaleText => $"1:{Map.CurrentPointInverseNearestGoogleScale:N0}";
    public string ScaleText2 => $"1:{Map.CurrentPointInverseMapScale:N0}";

    //private string _groundResolutionText = string.Empty;
    //public string GroundResolutionText
    //{
    //    get => _groundResolutionText;
    //    private set
    //    {
    //        if (_groundResolutionText == value)
    //            return;

    //        _groundResolutionText = value;
    //        RaisePropertyChanged();
    //    }
    //}
    public string GroundResolutionText => $"{Map.CurrentPointGroundResolution:N2} m/px";


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

        // remove invalid bookmarks
        for (int i = Bookmarks.Count - 1; i >= 0; i--)
        {
            if (!Bookmarks[i].IsValid())
            {
                DeleteBookmark(Bookmarks[i]);
            }
        }

        _store.Save(Bookmarks);

        Map.OnZoomChanged -= OnMapZoomChanged;
        Map.OnZoomChanged += OnMapZoomChanged;

        Map.OnMapExtentChanged -= OnMapExtentChangedHandler;
        Map.OnMapExtentChanged += OnMapExtentChangedHandler;

        RefreshMetrics();
    }

    private void DeleteBookmark(MapExtentBookmark? bookmark)
    {
        if (bookmark is null)
            return;

        Bookmarks.Remove(bookmark);

        _store.Save(Bookmarks);

        Map.RemovePredefinedExtent(bookmark.Id);
    }

    private void OnMapZoomChanged(object? sender, double mapScale) => RefreshMetrics();

    private void OnMapExtentChangedHandler(object? sender, EventArgs e) => RefreshMetrics();

    public void RefreshMetrics()
    {
        //ZoomLevelText = Map.NearestZoomLevel.ToString();

        //ScaleText = $"1:{Map.InverseMapScale:N0}";
        //ScaleText = $"1:{Map.CurrentPointInverseMapScale:N0}";

        //var center = Map.CurrentExtent.Center;
        //var wgs = MapProjects.WebMercatorToGeodeticWgs84(center);
        //double lat = wgs?.Y ?? 0;
        //var gr = WebMercatorUtility.CalculateGroundResolution(Map.NearestZoomLevel, lat);
        //var gr = Map.CurrentPointGroundResolution;

        //GroundResolutionText = $"{gr:N2} m/px";
        RaisePropertyChanged(nameof(ScaleText));
        RaisePropertyChanged(nameof(GroundResolutionText));
        RaisePropertyChanged(nameof(ZoomLevelText));
        RaisePropertyChanged(nameof(ScaleText2));
    }

    #region Command

    private RelayCommand? _saveBookmarkCommand;
    public RelayCommand SaveBookmarkCommand =>
        _saveBookmarkCommand ??= new RelayCommand(
            async _ =>
            {
                var title = NewBookmarkTitle?.Trim() ?? string.Empty;

                if (string.IsNullOrEmpty(title))
                    return;

                var extent = Map.CurrentExtent;

                var bookmark = MapExtentBookmark.FromTitleAndExtent(title, extent);

                //if (Map.RequestCaptureThumbnailAsync is not null)
                //{
                var bmp = await Map.CaptureThumbnailAsync(extent, 48, 48);

                if (bmp is not null)
                {
                    bookmark.ThumbnailBytes = ImageUtility.GetPngBytes(bmp);
                    bookmark.LoadThumbnail();
                }
                //}

                Bookmarks.Add(bookmark);

                _store.Save(Bookmarks);

                this.Map.PredefinedExtents.Add(new Models.Spatialable.EnvelopeMarkupLabelTriple(bookmark));

                NewBookmarkTitle = string.Empty;
            },
            _ => !string.IsNullOrWhiteSpace(NewBookmarkTitle));


    private RelayCommand? _goToBookmarkCommand;
    public RelayCommand GoToBookmarkCommand =>
        _goToBookmarkCommand ??= new RelayCommand(param =>
        {
            if (param is not MapExtentBookmark b)
                return;

            Map.ZoomToExtent(b.WebMercatorExtent, isExactExtent: true, isNewExtent: true);
        });


    private RelayCommand? _deleteBookmarkCommand;
    public RelayCommand DeleteBookmarkCommand =>
        _deleteBookmarkCommand ??= new RelayCommand(param =>
        {
            //if (p is not MapExtentBookmark b)
            //    return;

            DeleteBookmark(param as MapExtentBookmark);

            //Bookmarks.Remove(b);

            //_store.Save(Bookmarks);

            //Map.RemovePredefinedExtent(b.Id);
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
                    var wgs = MapProjects.WebMercatorToGeodeticWgs84(center);

                    double lat = wgs?.Y ?? 0;

                    var webMercatorScale = Math.Cos(lat * Math.PI / 180.0) * item.Model.Scale;

                    Map.Zoom(webMercatorScale, center);
                }
            },
            _ => SelectedScaleItem is not null);

    #endregion

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        Map.OnZoomChanged -= OnMapZoomChanged;

        Map.OnMapExtentChanged -= OnMapExtentChangedHandler;
    }
}