using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

using IRI.Maptor.Jab.Maui.Layers;
using IRI.Maptor.Sta.SpatialReferenceSystem;

using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

using BoundingBox = IRI.Maptor.Sta.Common.Primitives.BoundingBox;
using StaPoint = IRI.Maptor.Sta.Common.Primitives.Point;

namespace IRI.Maptor.Jab.Maui.Controls;

/// <summary>
/// A lightweight MAUI map control that displays tile basemaps (Google / OpenStreetMap)
/// and supports pan and zoom. State is exposed through bindable <see cref="Latitude"/>,
/// <see cref="Longitude"/>, <see cref="ZoomLevel"/> and <see cref="BaseMap"/> properties
/// for MVVM use.
/// </summary>
public class MapViewer : ContentView
{
    private readonly GraphicsView _canvas;
    private readonly TileMapDrawable _drawable;
    private readonly TileImageCache _cache;

    // Internal view state.
    private double _centerX;     // WebMercator
    private double _centerY;     // WebMercator
    private double _resolution;  // WebMercator units per pixel

    // Guards property write-back from re-triggering the bindable-property handlers.
    private bool _suppress;

    // Gesture scratch state.
    private double _panStartCenterX;
    private double _panStartCenterY;
    private double _pinchStartResolution;

    // Extent requested before the control had a size; applied on first layout.
    private BoundingBox? _pendingExtent;

    public MapViewer()
    {
        _cache = new TileImageCache(InvalidateCanvas);
        _drawable = new TileMapDrawable(_cache);

        _canvas = new GraphicsView { Drawable = _drawable };
        _canvas.SizeChanged += OnCanvasSizeChanged;

        Content = _canvas;

        _resolution = MapViewerMath.ZoomToResolution(ZoomLevel);
        UpdateCenterFromLatLon();
        UpdateDrawableState();

        // Wire the default Layers collection (accessing the property creates it).
        _drawable.Layers = Layers;
        Layers.CollectionChanged += OnLayersCollectionChanged;

        AddGestures();
    }

    private void OnCanvasSizeChanged(object? sender, EventArgs e)
    {
        if (_pendingExtent.HasValue)
        {
            var extent = _pendingExtent.Value;
            _pendingExtent = null;
            ZoomToExtent(extent);
        }

        RefreshDrawable();
    }

    #region Bindable properties

    public static readonly BindableProperty LatitudeProperty = BindableProperty.Create(
        nameof(Latitude), typeof(double), typeof(MapViewer), 35.6892, BindingMode.TwoWay, propertyChanged: OnGeoChanged);

    public static readonly BindableProperty LongitudeProperty = BindableProperty.Create(
        nameof(Longitude), typeof(double), typeof(MapViewer), 51.3890, BindingMode.TwoWay, propertyChanged: OnGeoChanged);

    public static readonly BindableProperty ZoomLevelProperty = BindableProperty.Create(
        nameof(ZoomLevel), typeof(int), typeof(MapViewer), 11, BindingMode.TwoWay, propertyChanged: OnZoomChanged);

    public static readonly BindableProperty BaseMapProperty = BindableProperty.Create(
        nameof(BaseMap), typeof(MauiBaseMap), typeof(MapViewer), MauiBaseMap.GoogleRoadMap, BindingMode.TwoWay, propertyChanged: OnBaseMapChanged);

    public double Latitude
    {
        get => (double)GetValue(LatitudeProperty);
        set => SetValue(LatitudeProperty, value);
    }

    public double Longitude
    {
        get => (double)GetValue(LongitudeProperty);
        set => SetValue(LongitudeProperty, value);
    }

    public int ZoomLevel
    {
        get => (int)GetValue(ZoomLevelProperty);
        set => SetValue(ZoomLevelProperty, value);
    }

    public MauiBaseMap BaseMap
    {
        get => (MauiBaseMap)GetValue(BaseMapProperty);
        set => SetValue(BaseMapProperty, value);
    }

    private static void OnGeoChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var map = (MapViewer)bindable;

        if (map._suppress)
        {
            return;
        }

        map.UpdateCenterFromLatLon();
        map.RefreshDrawable();
    }

    private static void OnZoomChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var map = (MapViewer)bindable;

        if (map._suppress)
        {
            return;
        }

        map._resolution = MapViewerMath.ZoomToResolution(map.ZoomLevel);
        map.RefreshDrawable();
    }

    private static void OnBaseMapChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((MapViewer)bindable).RefreshDrawable();
    }

    public static readonly BindableProperty MarkerLatitudeProperty = BindableProperty.Create(
        nameof(MarkerLatitude), typeof(double?), typeof(MapViewer), null, propertyChanged: OnMarkerChanged);

    public static readonly BindableProperty MarkerLongitudeProperty = BindableProperty.Create(
        nameof(MarkerLongitude), typeof(double?), typeof(MapViewer), null, propertyChanged: OnMarkerChanged);

    /// <summary>Latitude of an optional marker (e.g. the user's location). Null hides it.</summary>
    public double? MarkerLatitude
    {
        get => (double?)GetValue(MarkerLatitudeProperty);
        set => SetValue(MarkerLatitudeProperty, value);
    }

    /// <summary>Longitude of an optional marker. Null hides it.</summary>
    public double? MarkerLongitude
    {
        get => (double?)GetValue(MarkerLongitudeProperty);
        set => SetValue(MarkerLongitudeProperty, value);
    }

    private static void OnMarkerChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var map = (MapViewer)bindable;
        map.UpdateMarker();
        map.InvalidateCanvas();
    }

    public static readonly BindableProperty LayersProperty = BindableProperty.Create(
        nameof(Layers), typeof(ObservableCollection<MapLayer>), typeof(MapViewer), null,
        defaultValueCreator: _ => new ObservableCollection<MapLayer>(),
        propertyChanged: OnLayersChanged);

    /// <summary>The vector layers (e.g. loaded GeoJSON) drawn on top of the basemap.</summary>
    public ObservableCollection<MapLayer> Layers
    {
        get => (ObservableCollection<MapLayer>)GetValue(LayersProperty);
        set => SetValue(LayersProperty, value);
    }

    private static void OnLayersChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var map = (MapViewer)bindable;

        if (oldValue is ObservableCollection<MapLayer> oldLayers)
        {
            oldLayers.CollectionChanged -= map.OnLayersCollectionChanged;
            foreach (var layer in oldLayers)
            {
                layer.PropertyChanged -= map.OnLayerPropertyChanged;
            }
        }

        if (newValue is ObservableCollection<MapLayer> newLayers)
        {
            newLayers.CollectionChanged += map.OnLayersCollectionChanged;
            foreach (var layer in newLayers)
            {
                layer.PropertyChanged += map.OnLayerPropertyChanged;
            }
        }

        map._drawable.Layers = map.Layers;
        map.InvalidateCanvas();
    }

    private void OnLayersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (MapLayer layer in e.OldItems)
            {
                layer.PropertyChanged -= OnLayerPropertyChanged;
            }
        }

        if (e.NewItems != null)
        {
            foreach (MapLayer layer in e.NewItems)
            {
                layer.PropertyChanged += OnLayerPropertyChanged;
            }
        }

        _drawable.Layers = Layers;

        // Auto-zoom to a freshly added layer so the user sees the data.
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            var added = e.NewItems?.OfType<MapLayer>().LastOrDefault();

            if (added?.Extent is not null)
            {
                ZoomToExtent(added.Extent.Value);
            }
        }

        InvalidateCanvas();
    }

    private void OnLayerPropertyChanged(object? sender, PropertyChangedEventArgs e) => InvalidateCanvas();

    #endregion

    #region Public API

    /// <summary>Zoom in one integer level, keeping the current center.</summary>
    public void ZoomIn() => SetZoomLevel(MapViewerMath.ResolutionToZoom(_resolution) + 1);

    /// <summary>Zoom out one integer level, keeping the current center.</summary>
    public void ZoomOut() => SetZoomLevel(MapViewerMath.ResolutionToZoom(_resolution) - 1);

    /// <summary>
    /// Fit the view to a WebMercator extent (e.g. a layer's bounds). If the control is not
    /// yet sized, the request is deferred until the first layout pass.
    /// </summary>
    public void ZoomToExtent(BoundingBox extent, double marginFactor = 1.15)
    {
        double width = _canvas.Width;
        double height = _canvas.Height;

        if (width <= 0 || height <= 0)
        {
            _pendingExtent = extent;
            return;
        }

        _centerX = (extent.XMin + extent.XMax) / 2.0;
        _centerY = (extent.YMin + extent.YMax) / 2.0;

        if (extent.Width <= 0 && extent.Height <= 0)
        {
            // A single point — use a sensible street-level zoom.
            _resolution = MapViewerMath.ZoomToResolution(15);
        }
        else
        {
            var resolution = Math.Max(
                Math.Max(extent.Width, 1e-6) / width,
                Math.Max(extent.Height, 1e-6) / height) * marginFactor;

            _resolution = MapViewerMath.ClampResolution(resolution);
        }

        WriteBackState();
        RefreshDrawable();
    }

    /// <summary>Center the map on a geographic location, optionally changing the zoom level.</summary>
    public void GoTo(double latitude, double longitude, int? zoomLevel = null)
    {
        var mercator = MapProjects.GeodeticWgs84ToWebMercator(new StaPoint(longitude, latitude));
        _centerX = mercator.X;
        _centerY = mercator.Y;

        if (zoomLevel.HasValue)
        {
            _resolution = MapViewerMath.ZoomToResolution(zoomLevel.Value);
        }

        WriteBackState();
        RefreshDrawable();
    }

    #endregion

    #region State helpers

    private void SetZoomLevel(int level)
    {
        level = Math.Clamp(level, MapViewerMath.MinZoom, MapViewerMath.MaxZoom);
        _resolution = MapViewerMath.ZoomToResolution(level);
        WriteBackState();
        RefreshDrawable();
    }

    private void UpdateCenterFromLatLon()
    {
        var mercator = MapProjects.GeodeticWgs84ToWebMercator(new StaPoint(Longitude, Latitude));
        _centerX = mercator.X;
        _centerY = mercator.Y;
    }

    private void WriteBackState()
    {
        var geodetic = MapProjects.WebMercatorToGeodeticWgs84(new StaPoint(_centerX, _centerY));

        _suppress = true;
        Longitude = geodetic.X;
        Latitude = geodetic.Y;
        ZoomLevel = MapViewerMath.ResolutionToZoom(_resolution);
        _suppress = false;
    }

    private void UpdateDrawableState()
    {
        _drawable.CenterX = _centerX;
        _drawable.CenterY = _centerY;
        _drawable.Resolution = _resolution;
        _drawable.UrlFunc = BaseMapUrlResolver.GetUrlFunc(BaseMap);
        _drawable.LayerKey = BaseMap.ToString();
    }

    private void UpdateMarker()
    {
        if (MarkerLatitude.HasValue && MarkerLongitude.HasValue)
        {
            var mercator = MapProjects.GeodeticWgs84ToWebMercator(new StaPoint(MarkerLongitude.Value, MarkerLatitude.Value));
            _drawable.MarkerX = mercator.X;
            _drawable.MarkerY = mercator.Y;
        }
        else
        {
            _drawable.MarkerX = null;
            _drawable.MarkerY = null;
        }
    }

    private void RefreshDrawable()
    {
        UpdateDrawableState();
        InvalidateCanvas();
    }

    private void InvalidateCanvas()
    {
        if (MainThread.IsMainThread)
        {
            _canvas.Invalidate();
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(() => _canvas.Invalidate());
        }
    }

    #endregion

    #region Gestures

    private void AddGestures()
    {
        var pan = new PanGestureRecognizer();
        pan.PanUpdated += OnPanUpdated;
        _canvas.GestureRecognizers.Add(pan);

        var pinch = new PinchGestureRecognizer();
        pinch.PinchUpdated += OnPinchUpdated;
        _canvas.GestureRecognizers.Add(pinch);

        var doubleTap = new TapGestureRecognizer { NumberOfTapsRequired = 2 };
        doubleTap.Tapped += OnDoubleTapped;
        _canvas.GestureRecognizers.Add(doubleTap);
    }

    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _panStartCenterX = _centerX;
                _panStartCenterY = _centerY;
                break;

            case GestureStatus.Running:
                _centerX = _panStartCenterX - e.TotalX * _resolution;
                _centerY = _panStartCenterY + e.TotalY * _resolution;
                RefreshDrawable();
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                WriteBackState();
                RefreshDrawable();
                break;
        }
    }

    private void OnPinchUpdated(object? sender, PinchGestureUpdatedEventArgs e)
    {
        switch (e.Status)
        {
            case GestureStatus.Started:
                _pinchStartResolution = _resolution;
                break;

            case GestureStatus.Running:
                if (e.Scale <= 0)
                {
                    break;
                }

                ZoomAboutNormalizedPoint(e.ScaleOrigin.X, e.ScaleOrigin.Y, _pinchStartResolution / e.Scale);
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                WriteBackState();
                RefreshDrawable();
                break;
        }
    }

    private void OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        var width = _canvas.Width;
        var height = _canvas.Height;
        var position = e.GetPosition(_canvas);

        var targetResolution = MapViewerMath.ClampResolution(_resolution * 0.5);

        if (position.HasValue && width > 0 && height > 0)
        {
            ZoomAboutNormalizedPoint(position.Value.X / width, position.Value.Y / height, targetResolution);
        }
        else
        {
            _resolution = targetResolution;
        }

        WriteBackState();
        RefreshDrawable();
    }

    /// <summary>
    /// Change the resolution while keeping the world point under the given normalized
    /// (0..1) screen location fixed.
    /// </summary>
    private void ZoomAboutNormalizedPoint(double normalizedX, double normalizedY, double targetResolution)
    {
        double width = _canvas.Width;
        double height = _canvas.Height;

        if (width <= 0 || height <= 0)
        {
            _resolution = MapViewerMath.ClampResolution(targetResolution);
            RefreshDrawable();
            return;
        }

        double pixelX = normalizedX * width;
        double pixelY = normalizedY * height;

        double worldXBefore = _centerX + (pixelX - width / 2.0) * _resolution;
        double worldYBefore = _centerY - (pixelY - height / 2.0) * _resolution;

        _resolution = MapViewerMath.ClampResolution(targetResolution);

        double worldXAfter = _centerX + (pixelX - width / 2.0) * _resolution;
        double worldYAfter = _centerY - (pixelY - height / 2.0) * _resolution;

        _centerX += worldXBefore - worldXAfter;
        _centerY += worldYBefore - worldYAfter;

        RefreshDrawable();
    }

    #endregion
}
