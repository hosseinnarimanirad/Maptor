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
using DocumentFormat.OpenXml.Office.CustomUI;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Spatial.Analysis;

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
     
    //public string NearestGoogleZoomLevel => Map.NearestGoogleZoomLevel.ToString();

    //public string InverseMapScale_NearestGoogleZoomLevel => Map.InverseMapScale_NearestGoogleZoomLevel.ToString();
     
    //public string CurrentPointNearestGoogleScale => FormattableString.Invariant($"1:{Map.CurrentPointInverseNearestGoogleScale:N0}");

    //public string CurrentPointMapScale => FormattableString.Invariant($"1:{Map.InverseMapScale_CurrentPoint:N0}");

    //public string WebMercatorMapScale => FormattableString.Invariant($"1:{Map.InverseMapScale:N0}");
     
    //public string CurrentPointGroundResolution => FormattableString.Invariant($"{Map.CurrentPointGroundResolution:N2}");
     
    private bool _useGroundScaleForSandardScales = true;
    public bool UseGroundScaleForSandardScales
    {
        get { return _useGroundScaleForSandardScales; }
        set
        {
            _useGroundScaleForSandardScales = value;
            RaisePropertyChanged();
        }
    }


    private string _topLabel = string.Empty;
    public string TopLabel
    {
        get { return _topLabel; }
        set
        {
            _topLabel = value;
            RaisePropertyChanged();
        }
    }


    private string _bottomLabel = string.Empty;
    public string BottomLabel
    {
        get { return _bottomLabel; }
        set
        {
            _bottomLabel = value;
            RaisePropertyChanged();
        }
    }


    private string _leftLabel = string.Empty;
    public string LeftLabel
    {
        get { return _leftLabel; }
        set
        {
            _leftLabel = value;
            RaisePropertyChanged();
        }
    }


    private string _rightLabel = string.Empty;
    public string RightLabel
    {
        get { return _rightLabel; }
        set
        {
            _rightLabel = value;
            RaisePropertyChanged();
        }
    }

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
        //RaisePropertyChanged(nameof(NearestGoogleZoomLevel));
        //RaisePropertyChanged(nameof(CurrentPointNearestGoogleScale));
        //RaisePropertyChanged(nameof(WebMercatorMapScale));
        //RaisePropertyChanged(nameof(CurrentPointMapScale));
        //RaisePropertyChanged(nameof(CurrentPointGroundResolution));



        var geodeticExtent = Map.CurrentExtent.Transform(MapProjects.WebMercatorToGeodeticWgs84);

        var bottomLength = SpatialUtility.GetEllipsoidalLength(geodeticExtent.BottomLeft, geodeticExtent.BottomRight);
        var topLength = SpatialUtility.GetEllipsoidalLength(geodeticExtent.TopLeft, geodeticExtent.TopRight);
        var leftLength = SpatialUtility.GetEllipsoidalLength(geodeticExtent.BottomLeft, geodeticExtent.TopLeft);
        var rightLength = SpatialUtility.GetEllipsoidalLength(geodeticExtent.BottomRight, geodeticExtent.TopRight);

        BottomLabel = UnitHelper.GetLengthLabel(bottomLength);
        TopLabel = UnitHelper.GetLengthLabel(topLength);
        LeftLabel = UnitHelper.GetLengthLabel(leftLength);
        RightLabel = UnitHelper.GetLengthLabel(rightLength);
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

                var bmp = await Map.CaptureThumbnailAsync(extent, 48, 48);

                if (bmp is not null)
                {
                    bookmark.ThumbnailBytes = ImageUtility.GetPngBytes(bmp);
                    bookmark.LoadThumbnail();
                }

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
        _deleteBookmarkCommand ??= new RelayCommand(param => DeleteBookmark(param as MapExtentBookmark));


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

                    var webMercatorScale = UseGroundScaleForSandardScales ? Math.Cos(lat * Math.PI / 180.0) * item.Model.Scale : item.Model.Scale;

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