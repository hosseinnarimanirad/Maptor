// BESMELLAHERAHMANERAHIM
// ALLAHOMAAJJELLEVALIEKALFARAJ

using System;
using System.Linq;
using System.Windows;
using System.Net.Http;
using System.Threading;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Media.Animation;

using IRI.Maptor.Extensions;
using IRI.Maptor.Jab.Common;
using IRI.Maptor.Jab.Common.Models;
using IRI.Maptor.Jab.Controls.Models;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Common.Services;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Jab.Common.TileServices;
using IRI.Maptor.Sta.Persistence.DataSources;
using IRI.Maptor.Jab.Common.Models.Spatialable;

using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Jab.Common.Events;
using IRI.Maptor.Sta.Spatial.Model;
using IRI.Maptor.Sta.Spatial.Helpers;
using IRI.Maptor.Sta.Spatial.Analysis;
using IRI.Maptor.Jab.Common.ViewModels;
using IRI.Maptor.Jab.Common.Assets.Data;
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;
using IRI.Maptor.Sta.Persistence.Abstractions;
using IRI.Maptor.Sta.Persistence.RasterDataSources;
using IRI.Maptor.Jab.Common.Cartography.RenderingStrategies;

using sb = IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Jab.Common.Views.Controls;
using IRI.Maptor.Jab.Common.Cartography.Symbologies;
using IRI.Maptor.Jab.Common.ViewModels.CoordinateEditor;
using IRI.Maptor.Jab.Common.ViewModels.Map;
using IRI.Maptor.Jab.Common.Data;
using IRI.Maptor.Jab.Common.Helpers;
using System.Windows.Media.Imaging;

//using Geometry = IRI.Maptor.Sta.Spatial.Primitives.Geometry<IRI.Maptor.Sta.Common.Primitives.Point>;

namespace IRI.Maptor.Jab.Controls.Views;

public partial class MapViewer : NotifiableUserControl
{
    //#region INotifyPropertyChanged

    //public event PropertyChangedEventHandler PropertyChanged;

    //protected void RaisePropertyChanged([CallerMemberName] string propertyName = "")
    //{
    //    PropertyChangedEventHandler handler = PropertyChanged;

    //    if (handler != null)
    //        handler(this, new PropertyChangedEventArgs(propertyName));
    //}

    //#endregion


    #region Debugging

    const string _eventEntered = "(MapViewer) - EVENT RAISE";
    const string _eventLeaved = "(MapViewer) - EVENT LEAVED";
    const string _eventEscaped = "(MapViewer) - EVENT ESCAPE";
    const string _methodBegins = "(MapViewer) - METHOD BEGINS";
    const string _methodFinished = "(MapViewer) - METHOD FINISHED";
    const string _methodEscaped = "(MapViewer) - METHOD ESCAPE";
    const string _refreshCalled = "(MapViewer) - REFRESH CALLED";
    const string _benchmarking = "(MapViewer) - BENCHMARKING";
    const string _info = "(MapViewer) - info";

    private void DebugWriteLine(string message)
    {
        Debug.WriteLine($"MapViewer; {DateTime.Now.ToLongTimeString()}; {message}");
    }

    #endregion


    #region Events

    public event MouseEventHandler OnMapMouseMove;

    public event EventHandler<ZoomEventArgs> OnZoomChanged;

    public event EventHandler<PointEventArgs> OnPointSelected;

    public EventHandler<PointEventArgs> CurrentEditingPointChanged;

    public event EventHandler<MapStatusEventArgs> OnStatusChanged;

    public event EventHandler<MapActionEventArgs> OnMapActionChanged;

    public event EventHandler<bool> OnExtentChanged;

    public event EventHandler<EditableFeatureLayer> OnEditableFeatureLayerChanged;

    #endregion


    #region Fields, Properties

    int minBoundingBoxSize = 10;

    private readonly object locker = new object();

    ExtentManager extentManager = new ExtentManager();

    LayerManager _layerManager = new LayerManager();

    List<Job> jobs = new List<Job>();

    //used to handle concurrent changes of zoomTransform.ScaleX/Y
    double _theScreenScale;

    public ObservableCollection<ILayer> Layers { get { return _layerManager.CurrentLayers; } }

    double baseScaleX, baseScaleY = -1;

    TransformGroup viewTransform = new TransformGroup();

    TranslateTransform panTransform = new TranslateTransform();

    ScaleTransform zoomTransform = new ScaleTransform();

    TranslateTransform panTransformForPoints = new TranslateTransform();

    //private System.Net.WebProxy? _proxy;

    //public System.Net.WebProxy? Proxy
    //{
    //    get { return _proxy; }
    //    set
    //    {
    //        _proxy = value;

    //        if (_proxy?.Address != null)
    //        {
    //            HttpClientHandler handler = new HttpClientHandler();
    //            handler.Proxy = _proxy;
    //            handler.UseProxy = true;
    //            HttpClient = new System.Net.Http.HttpClient(handler) { Timeout = new TimeSpan(0, 0, seconds: 10) };
    //            HttpClient.DefaultRequestHeaders.Add("User-Agent", "app!");
    //        }
    //        else
    //        {
    //            HttpClientHandler handler = new HttpClientHandler();
    //            handler.Proxy = null;
    //            handler.UseProxy = false;
    //            HttpClient = new System.Net.Http.HttpClient(handler) { Timeout = new TimeSpan(0, 0, seconds: 10) };
    //            HttpClient.DefaultRequestHeaders.Add("User-Agent", "app!");
    //        }

    //    }
    //}

    //public System.Net.Http.HttpClient HttpClient { get; set; }

    public double ScreenScale
    {
        //94.09.24: zoomTransform.ScaleX  may be at an animation
        //get { return this.zoomTransform.ScaleX * baseScaleX; }
        get { return this._theScreenScale * baseScaleX; }
    }

    private double _mapScale;
    public double MapScale
    {
        get => _mapScale;

        set
        {
            if (this._mapScale != value)
            {
                this._mapScale = value;

                RaisePropertyChanged();

                Zoom(value);
            }
        }
    }

    public double InverseMapScale => 1.0 / MapScale;

    public UIElementCollection Elements { get { return mapView.Children; } }


    public Cursor PanCursor { get; set; } = Cursors.Hand;
    public Cursor ZoomInCursor { get; set; } = Cursors.Arrow;
    public Cursor ZoomOutCursor { get; set; } = Cursors.Arrow;
    public Cursor ZoomInRectangleCursor { get; set; } = Cursors.Cross;
    public Cursor ZoomOutRectangleCursor { get; set; } = Cursors.Cross;

    private Dictionary<MapAction, Cursor> CursorSettings;

    private MapAction _currentMouseAction = MapAction.Pan;

    public MapAction CurrentMouseAction
    {
        get { return _currentMouseAction; }
        set
        {
            this.SetCursor(CursorSettings[value]);

            if (_currentMouseAction == value)
                return;

            _currentMouseAction = value;
            RaisePropertyChanged();

            this.OnMapActionChanged?.Invoke(null, new MapActionEventArgs(value));
        }
    }


    private MapStatus _status;
    public MapStatus Status
    {
        get { return _status; }
        set
        {
            _status = value;
            RaisePropertyChanged();
            this.OnStatusChanged?.Invoke(null, new MapStatusEventArgs(value));
        }
    }

    public bool IsPanning { get; set; } = false;

    /// <summary>
    /// Based On Google Zoom Levels
    /// </summary>
    public int NearestGoogleZoomLevel => WebMercatorUtility.GetZoomLevel(this.MapScale);

    private void UpdateTileInfos()
    {
        this.CurrentTileInfos = WebMercatorUtility.WebMercatorBoundingBoxToGoogleTileRegions(this.CurrentExtent, this.NearestGoogleZoomLevel);
    }

    private List<TileInfo> _currentTileInfos;

    public List<TileInfo> CurrentTileInfos
    {
        get { return _currentTileInfos; }
        set
        {
            _currentTileInfos = value;

            this.extentManager.Update(_currentTileInfos);
        }
    }

    private Point _currentEditingPoint;

    // in WebMercator
    public Point CurrentEditingPoint
    {
        get { return _currentEditingPoint; }
        set
        {
            _currentEditingPoint = value;
            RaisePropertyChanged();
            this.CurrentEditingPointChanged?.Invoke(null, new PointEventArgs(value));
        }
    }


    private Point _currentPoint;

    /// <summary>
    /// Current Mouse Position (longitude/Latitude) in decimal degree
    /// </summary>
    public Point CurrentPoint
    {
        get { return _currentPoint; }
        set
        {
            _currentPoint = value;
            RaisePropertyChanged();
        }
    }

    //public double CurrentPointScale { get; set; }
    //public double CurrentPointScale2 { get; set; }

    //public double CurrentPointGroundResolution { get; set; }

    public sb.BoundingBox CurrentExtent
    {
        get
        {
            if (this.mapView.ActualHeight == 0 || this.mapView.ActualWidth == 0)
            {
                return new sb.BoundingBox(0, 0, 0, 0);
            }

            Point p1 = new Point(0, 0);

            Point p2 = new Point(this.mapView.ActualWidth, this.mapView.ActualHeight);

            Point mapPoint1 = ScreenToMap(p1);

            Point mapPoint2 = ScreenToMap(p2);

            return new sb.BoundingBox(mapPoint1.X, mapPoint2.Y, mapPoint2.X, mapPoint1.Y);
        }
    }

    public sb.Point CurrentExtentCenterInWgs84
    {
        get
        {
            var extent = CurrentExtent;

            if (extent.IsValidPlus())
            {
                return MapProjects.WebMercatorToGeodeticWgs84(CurrentExtent.Center);
            }
            else
            {
                return null;
            }
        }
    }

    public MapViewer()
    {
        InitializeComponent();

        this.CursorSettings = new Dictionary<MapAction, Cursor>() {
            { MapAction.Pan, PanCursor },
            { MapAction.ZoomIn, ZoomInCursor },
            { MapAction.ZoomOut, ZoomOutCursor },
            { MapAction.ZoomInRectangle, ZoomInRectangleCursor },
            { MapAction.ZoomOutRectangle, ZoomOutRectangleCursor },
            { MapAction.None, Cursors.AppStarting },
        };

        this.RegisterRightClickOptions();

        _layerManager.RequestRefreshVisibility = RefreshLayerVisibility;

        this.extentManager.OnTilesAdded -= ExtentManager_OnTilesAdded;
        this.extentManager.OnTilesAdded += ExtentManager_OnTilesAdded;

        this.extentManager.OnTilesRemoved -= ExtentManager_OnTilesRemoved;
        this.extentManager.OnTilesRemoved += ExtentManager_OnTilesRemoved;

        baseScaleX = this.mapView.FlowDirection == FlowDirection.RightToLeft ? -1 : 1;

        this.zoomTransform.ScaleX = baseScaleX;
        this.zoomTransform.ScaleY = baseScaleY;

        this._theScreenScale = baseScaleX;

        this.viewTransform.Children.Add(panTransform);

        this.viewTransform.Children.Add(zoomTransform);

        this.mapView.MouseMove += (sender, e) =>
        {
            this.CurrentPoint = ScreenToGeodetic(e.GetPosition(this.mapView));

            //if (this.CurrentPoint != null && !double.IsNaN(this.CurrentPoint.Y))
            //{
            //    this.CurrentPointScale = WebMercatorUtility.CalculateMapScale(this.NearestGoogleZoomLevel, CurrentPoint.Y);
            //    this.CurrentPointScale2 = WebMercatorUtility.WebMercatorScaleToMapScale(this.MapScale, CurrentPoint.Y);

            //    this.CurrentPointGroundResolution = WebMercatorUtility.CalculateGroundResolution(this.NearestGoogleZoomLevel, CurrentPoint.Y);
            //    var currentPointGroundResolution = WebMercatorUtility.CalculateGroundResolution(CurrentPointScale);

            //    var theScale1_local = WebMercatorUtility.CalculateMapScale(this.NearestGoogleZoomLevel, CurrentPoint.Y);
            //    var theScale_equator = WebMercatorUtility.CalculateMapScale(this.NearestGoogleZoomLevel, 0);


            //    var groundRes = WebMercatorUtility.CalculateGroundResolution(theScale_equator);
            //    var groundRes2 = WebMercatorUtility.CalculateGroundResolution(this.NearestGoogleZoomLevel, 0);
            //}

            this.OnMapMouseMove?.Invoke(sender, e);
        };

        this.OnZoomChanged += (sender, e) =>
        {
            this._mapScale = this.ToMapScale(this.ScreenScale);

            RaisePropertyChanged(nameof(MapScale));

            RaisePropertyChanged(nameof(NearestGoogleZoomLevel));
             
            UpdateTileInfos();

            this._layerManager.UpdateIsInRange(InverseMapScale);

        };
    }

    MapViewModelBase _presenter;

    #endregion

    public async Task Register(MapViewModelBase presenter,
                                List<IriProvince93>? provinces = null)
    {
        if (presenter == null)
            return;

        _presenter = presenter;

        presenter.RequestPrint = this.Print;

        //presenter.RequestGetAsDrawingVisual = this.GetAsDrawingVisual;

        //presenter.RequestCaptureThumbnailAsync = (ext, w, h) => this.CaptureThumbnailAsync(ext, w, h);

        presenter.RequestGetOrderedLayers = () => this._layerManager.GetOrderedLayers();

        presenter.RequestGetActualHeight = () => this.mapView.ActualHeight;

        presenter.RequestGetActualWidth = () => this.mapView.ActualWidth;

        presenter.RegisterAction = async (i) => { await this.Register(i); };

        presenter.RequestSetDefaultCursor = this.SetDefaultCursor;

        presenter.RequestSetCursor = this.SetCursor;

        presenter.RequestRefresh = this.Refresh;

        presenter.RequestRefreshLayerVisibility = this.RefreshLayerVisibility;

        presenter.RequestIranExtent = () => { this.ZoomToExtent(sb.BoundingBoxes.Mercator_Iran); };

        presenter.RequestFullExtent = this.FullExtent;

        presenter.RequestEnableRectangleZoom = this.ZoomIn;

        presenter.RequestEnableZoomOut = this.ZoomOutPoint;

        presenter.FireIsMouseWheelZoomEnabledChanged = e =>
        {
            if (e)
                this.EnableZoomingOnMouseWheel();

            else
                this.DisableZoomingOnMouseWheel();
        };

        presenter.FireIsDoubleClickZoomEnabledChanged = e =>
        {
            if (e)
                this.EnableZoomOnDoubleClick();

            else
                this.DisableZoomOnDoubleClick();
        };

        presenter.Layers = this.Layers;

        presenter.RequestSetConnectedState = this.SetConnectionState;

        presenter.RequestRefreshBaseMaps = this.RefreshBaseMaps;

        presenter.RequestSetTileService = (iMapProvider, isCachEnabled, cacheDirectory, isOffline, getlocalFileName, opacity) =>
        {
            this.UnSetTileServices();

            this.SetTileService(iMapProvider, isCachEnabled, cacheDirectory, isOffline, getlocalFileName, opacity);

            this.RefreshBaseMaps();
        };

        presenter.RequestMapScale = () => { return this.MapScale; };

        //presenter.RequestCurrentPointGroundResolution = () => { return this.CurrentPointGroundResolution; };

        //presenter.RequestCurrentPointScale = () => { return this.CurrentPointScale; };
        //presenter.RequestCurrentPointScale2 = () => { return this.CurrentPointScale2; };

        presenter.RequestCurrentExtent = () => { return this.CurrentExtent; };

        //presenter.RequestCurrentZoomLevel = () => { return this.CurrentZoomLevel; };

        this.OnMapMouseMove += (sender, e) => { presenter.FireMouseMove(this.CurrentPoint); };

        this.OnZoomChanged += (sender, e) => { presenter.FireZoomChanged(this.MapScale); };

        this.OnExtentChanged += (sender, e) => { presenter.FireMapExtentChanged(this.CurrentExtent, e); };

        this.MouseUp += (sender, e) => { presenter.FireMapMouseUp(this.CurrentPoint); };

        this.CurrentEditingPointChanged += (sender, e) =>
        {
            presenter.UpdateCurrentEditingPoint(e.Point.AsPoint());
        };

        this.OnStatusChanged += (sender, e) => { presenter.FireMapStatusChanged(e.Status); };

        this.OnMapActionChanged += (sender, e) => { presenter.FireMapActionChanged(e.Action); };

        this.OnEditableFeatureLayerChanged += (sender, e) => { presenter.CurrentEditingLayer = e; };

        presenter.RequestZoomToPoint = (center, mapScale) => this.ZoomAndCenter(mapScale, center);

        presenter.RequestZoomToScale = mapScale => this.ZoomAndCenter(mapScale, this.CurrentExtent.Center);

        presenter.RequestZoomAtViewCenter = zoomIn => this.ZoomAtViewCenter(zoomIn);

        presenter.RequestZoomAndCenterToGoogleZoomLevel = this.ZoomAndCenterToGoogleZoomLevel;

        presenter.RequestRegisterMapOptions = (arg) => { this.RegisterRightClickContextOptions(arg.View, arg.DataContext); };

        presenter.RequestRemoveMapOptions = this.RemoveRightClickOptions;

        presenter.RequestUnregisterMapOptions = this.UnregisterRightClickContextOptions;

        presenter.RequestPanTo = (point, callback) => { this.PanTo(point.X, point.Y, callback); };

        presenter.RequestFlashPoints = Flash;

        presenter.RequestFlashPoint = Flash;

        presenter.RequestZoomToExtent = (boundingBox, isExactExtent, isNewExtent, callback) => { this.ZoomToExtent(boundingBox, false, isExactExtent, isNewExtent, callback); };

        presenter.RequestAddPointToNewDrawing = p =>
        {
            AddPointToNewDrawing((sb.Point)p);
        };

        presenter.RequestGetDrawingAsync = (mode, options, display) => GetDrawingAsync(mode, options, display);

        presenter.RequestCancelNewDrawing = CancelDrawing;

        presenter.RequestFinishDrawingPart = FinishDrawingPart;

        presenter.RequestFinishNewDrawing = FinishDrawing;

        presenter.RequestCancelEdit = CancelEditGeometry;

        presenter.RequestFinishEdit = FinishEditing;

        presenter.RequestMeasure = MeasureAsync;

        presenter.RequestCancelMeasure = this.CancelMeasure;

        presenter.RequestGetBezier = GetBezierAsync;

        presenter.RequestEdit = this.EditGeometryAsync;

        presenter.RequestAddSpecialPointLayer = this.AddSpecialPointLayerToMap;

        presenter.RequestSetLayer = this.SetLayer;

        presenter.RequestRemoveLayer = this.RemoveLayer;

        presenter.RequestAddLayer = (l) =>
        {
            this.SetLayer(l);
            _layerManager._loadCancellationToken = presenter.LoadCancellationToken;
            this.AddLayer(l);
        };

        presenter.RequestTransformScreenGeometryToWebMercatorGeometry = (screenGeo) =>
        {
            var mapGeo = screenGeo.Transform(p => ScreenToMap(p.AsWpfPoint()).AsPoint(), SridHelper.WebMercator);

            return mapGeo;
        };

        presenter.RequestRemovePolyBezierLayers = RemovePolyBezierLayers;

        presenter.RequestAddPolyBezier = (name, points, geometry, showSymbolOnly, decorationVisuals) =>
          {
              var layer = PolyBezierLayer.Create(name, points, this.viewTransform, geometry, decorationVisuals);

              layer.IsControlsShown = !showSymbolOnly;

              layer.IsDecorated = showSymbolOnly;

              layer.IsBezierShown = !showSymbolOnly;

              RegisterPolyBezierLayer(layer);

              this.SetLayer(layer);

              this.AddPolyBezierLayer(layer);
          };

        presenter.RequestAddGeometries = this.DrawGeometriesAsync;

        presenter.RequestHighlightGeometries = this.HighlightGeometries;

        presenter.RequestSelectGeometries = SelectGeometriesAsync;

        presenter.RequestClearLayer = (layer, remove, forceRemove, keepEmptyParentGroup) => ClearLayer(layer, remove, forceRemove, keepEmptyParentGroup);

        presenter.RequestClearLayerByCriteria = this.Clear;

        presenter.RequestClearLayerByTag = this.Clear;

        presenter.RequestPan = this.Pan;

        presenter.RequestZoomToFeature = this.ZoomToFeature;

        presenter.RequestShowGeometryComparison = this.ShowGeometryComparison;
        presenter.RequestClearGeometryComparison = this.ClearGeometryComparison;
        presenter.RequestShowFeatureChangesDialog = this.ShowFeatureChangesDialog;

        //presenter.RequestIdentify = (point, options) => new ObservableCollection<FeatureSet<sb.Point>>(this.GetFeatures(point, options));

        //presenter.RequestSearch = searchText => new ObservableCollection<FeatureSet<sb.Point>>(this.GetFeatures(searchText));

        presenter.RequestGetPoint = SelectPointAsync;

        presenter.RequestMapDistanceToScreenDistance = this.MapToScreen;

        presenter.RequestScreenDistanceMapDistance = this.ScreenToMap;

        presenter.RequestGetMapToScreenMatrix = () => this.viewTransform.Value;

        presenter.RequestGetScreenToMapMatrix = () =>
        {
            var matrix = this.viewTransform.Value;

            if (matrix.HasInverse)
            {
                matrix.Invert();

                return matrix;
            }

            return null;
        };

        var zoomToExtentAction = new Action<EnvelopeMarkupLabelTriple>(a =>
        {
            //this.ZoomToExtent(IriProvinces93WmEnvelopes.ToBoundingBox(a.Province));
            this.ZoomToExtent(a.WebMercatorExtent);
        });

        presenter.PredefinedExtents.CollectionChanged += (sender, e) =>
        {
            var newItems = e.NewItems?.Cast<EnvelopeMarkupLabelTriple>()?.ToList();

            if (!newItems.IsNullOrEmpty())
            {
                foreach (var item in newItems)
                {
                    item.RequestRaiseSelected = zoomToExtentAction;
                }
            }

            var oldItems = e.OldItems?.Cast<EnvelopeMarkupLabelTriple>()?.ToList();

            if (!oldItems.IsNullOrEmpty())
            {
                foreach (var item in oldItems)
                {
                    item.RequestRaiseSelected = null;
                }
            }
        };

        var ostanha = EnvelopeMarkupLabelTriple.GetProvinces93Wm();

        //var ostanha = EnvelopeMarkupLabelTriple.GetProvinces93Wm(a =>
        //{
        //    //this.ZoomToExtent(IriProvinces93WmEnvelopes.ToBoundingBox(a.Province));
        //    this.ZoomToExtent(a.WebMercatorExtent);
        //});

        var predefinedExtents = provinces is null ? ostanha : ostanha.Where(o => o.Province.HasValue && provinces.Contains(o.Province.Value)).ToList();

        foreach (var item in predefinedExtents)
        {
            presenter.PredefinedExtents.Add(item);
        }

        foreach (var item in presenter.MapExtentPanel.Bookmarks)
        {
            presenter.PredefinedExtents.Add(new EnvelopeMarkupLabelTriple(item));
        }

        presenter.Pan();

        presenter.SetMapCursorSet1();

        await presenter.InitializeAsync();

        presenter.RegisterMapOptions();

    }

    private void PredefinedExtents_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {

    }

    #region Conversions

    private double? _unitDistance;

    /// <summary>
    /// Size of each pixel (in meter)
    /// </summary>
    /// <returns></returns>
    private double GetUnitDistance()
    {
        if (_unitDistance == null || double.IsNaN(_unitDistance.Value))
        {
            PresentationSource source = PresentationSource.FromVisual(this.mapView);

            if (source == null)
                return 1;

            double dpiX = 96.0 * source.CompositionTarget.TransformToDevice.M11;
            double dpiY = 96.0 * source.CompositionTarget.TransformToDevice.M22;

            if (dpiX != dpiY)
            {

            }

            //size of each pixel (in meter)
            _unitDistance = IRI.Maptor.Sta.Common.Helpers.ConversionHelper.InchToMeterFactor / dpiX;
        }

        return _unitDistance.Value;
    }

    public double PixelSize => GetUnitDistance();

    private double ToScreenScale(double mapScale) => mapScale / GetUnitDistance();

    private double ToMapScale(double screenScale) => screenScale * GetUnitDistance();

    // Screen => Map (WebMercator) => Geodetic (Wgs84)

    public Point ScreenToMap(Point point) => this.viewTransform.Inverse.Transform(point);
    public Point MapToScreen(Point point) => this.viewTransform.Transform(point);

    public Point MapToGeodetic(Point point) => this.WebMercatorToGeodetic(point);
    public Point GeodeticToMap(Point point) => MapProjects.GeodeticWgs84ToWebMercator(point.AsPoint()).AsWpfPoint();

    public Point ScreenToGeodetic(Point point) => MapToGeodetic(ScreenToMap(point));
    public Point GeodeticToScreen(Point point) => MapToScreen(GeodeticToMap(point));

    /// <summary>
    /// WebMercator distance in Meter
    /// </summary>
    /// <param name="webMercatorDistance"></param>
    /// <returns>Screen distance in pixel</returns>
    public double MapToScreen(double webMercatorDistance) => webMercatorDistance * MapScale / GetUnitDistance();
    public double ScreenToMap(double screenDistance) => screenDistance * GetUnitDistance() / MapScale;

    private Point WebMercatorToGeodetic(Point point)
    {
        try
        {
            return MapProjects.WebMercatorToGeodeticWgs84(point.AsPoint()).AsWpfPoint();
        }
        catch (Exception)
        {
            return new Point(double.NaN, double.NaN);
        }
    }

    #endregion


    #region Public Layer Management

    public void SetClusteredLayer(string layerName, ScaleInterval scaleInterval, string imageDirectory, Func<string, FrameworkElement> viewMaker, Action<object> mouseDownHandler = null)
    {
        var layer = ClusteredPointLayer.Create(imageDirectory, viewMaker);

        if (mouseDownHandler != null)
        {
            layer.OnMouseDown += (sender, e) => mouseDownHandler(sender);
        }

        layer.LayerName = layerName;

        layer.VisibleRange = scaleInterval;

        this._layerManager.Add(layer, InverseMapScale);

        this.AddComplexLayer(layer.GetLayer(MapScale), true);
    }

    //public void SetRasterLayer(string layerName, ScaleInterval scaleInterval, IDataSource dataSource, double opacity, bool isBaseMap = false, bool isPyramid = false, RenderMode rendering = RenderMode.Default)
    //{
    //    if (dataSource == null)
    //    {
    //        return;
    //    }
    //    this._layerManager.Add(new RasterLayer(dataSource, layerName, scaleInterval, isBaseMap, isPyramid, Visibility.Visible, opacity, rendering), 1.0 / _mapScale);
    //}

    public void SetTileService(TileMapProvider mapProvider, bool isCachEnabled = false, string? cacheDirectory = null, bool isOffline = false, Func<TileInfo, string>? getFileName = null, double opacity = 1)
    {
        if (mapProvider is null)
            return;

        var layer = new TileServiceLayer(mapProvider, opacity, getFileName) { VisibleRange = ScaleInterval.All };

        if (isCachEnabled && IOHelper.TryCreateDirectory(cacheDirectory))
        {
            layer.EnableCaching(cacheDirectory);
        }

        layer.IsOffline = isOffline;

        this._layerManager.Add(layer, InverseMapScale);
    }

    public void UnSetTileService(string providerFullName)
    {
        this._layerManager.RemoveTile(providerFullName, forceRemove: true);
    }

    public void UnSetTileServices(int groupId = 1)
    {
        this._layerManager.Remove(layer => layer.Type == LayerType.BaseMap && layer is TileServiceLayer && (layer as TileServiceLayer).GroupId == groupId,
                                    forceRemove: true,
                                    keepEmptyParentGroup: false);
    }

    public void SetLayer(ILayer layer)
    {
        ConfigureLayer(layer);

        this._layerManager.Add(layer, InverseMapScale);
    }

    private void ConfigureLayer(ILayer layer)
    {
        if (layer.RequestChangeVisibility is null)
            layer.RequestChangeVisibility = RefreshLayerVisibility;

        if (!layer.SubLayers.IsNullOrEmpty())
        {
            foreach (var sublayer in layer.SubLayers)
            {
                ConfigureLayer(sublayer);
            }
        }
    }

    public void RemoveLayer(ILayer layer)
    {
        this._layerManager.Remove(layer, forceRemove: true, keepEmptyParentGroup: false);
    }

    public void SetSpecialPointLayer(string layerName, ScaleInterval scaleInterval, List<Locateable> items, double opacity = 1)
    {
        this._layerManager.Add(new SpecialPointLayer(layerName, items, opacity, scaleInterval, LayerType.Complex), InverseMapScale);
    }

    public void AddSpecialPointLayerToMap(string layerName, ScaleInterval scaleInterval, List<Locateable> items)
    {
        var specialLayer = new SpecialPointLayer(layerName, items, visibleRange: scaleInterval);

        this.SetLayer(specialLayer);

        this.AddComplexLayer(specialLayer);
    }

    public void AddSpecialPointLayerToMap(SpecialPointLayer layer)
    {
        this.SetLayer(layer);

        if (layer.VisibleRange.IsInRange(1.0 / this.MapScale))
        {
            this.AddComplexLayer(layer);
        }
    }

    public void AddSpecialLineLayerToMap(SpecialLineLayer layer)
    {
        this.SetLayer(layer);

        this.AddSpecialLineLayer(layer, null);
    }

    public void AddPolyBezierLayer(PolyBezierLayer layer)
    {
        if (!layer.IsBezierShown && !layer.IsDecorated)
        {
            throw new NotImplementedException();
        }

        layer.Redraw(this.viewTransform);

        if (layer.IsBezierShown)
        {
            var path = layer.GetMainPath();

            AddPanablePathToMapView(path);

            Canvas.SetZIndex(path, layer.ZIndex);

            AddComplexLayer(layer.GetMainPointLayer(), false);

            var controlPath = layer.GetControlPath();

            controlPath.Visibility = layer.IsControlsShown ? Visibility.Visible : Visibility.Collapsed;

            AddPanablePathToMapView(controlPath);

            //98.01.20
            //Canvas.SetZIndex(controlPath, layer.ZIndex + 1);
            Canvas.SetZIndex(controlPath, int.MaxValue);

            if (layer.IsControlsShown)
            {
                AddComplexLayer(layer.GetControlPointLayer(), false);
            }
        }
    }

    public void AddLayer(ILayer layer)
    {
        if (layer.IsGroupLayer)
        {
            foreach (var item in layer.SubLayers)
            {
                AddLayer(item);
            }

            return;
        }

        var mapScale = this.MapScale;

        // 1401.12.20
        //if (layer.VisualParameters == null || layer.VisualParameters.Visibility != Visibility.Visible)
        if (!layer.CanRenderLayer(mapScale))
        {
            layer.Element = null;

            return;
        }

        if (layer.RenderMode == RenderMode.Tiled)
            return;

        if (layer is ClusteredPointLayer)
        {
            Action action = () =>
            {
                AddComplexLayer((layer as ClusteredPointLayer).GetLayer(mapScale));
            };

            Task.Run(() => this.jobs.Add(new Job(new LayerTag(mapScale) { LayerType = LayerType.Complex, Tile = null },
                Dispatcher.BeginInvoke(action, DispatcherPriority.Background, null))));
        }
        // لایه‌ای که عارضه زمان
        // انجام عملیات ترسیم 
        // نمایش می ده و مدیریت می‌کنه
        else if (layer is DrawingLayer)
        {
            //Action action = () =>
            //{
            AddEditableFeatureLayer((layer as DrawingLayer).GetLayer());
            //};
            //Task.Run(() => this.jobs.Add(new Job(new LayerTag(mapScale) { LayerType = LayerType.Drawing, Tile = null },
            //    Dispatcher.BeginInvoke(action, DispatcherPriority.Background, null))));
        }
        else if (layer is EditableFeatureLayer)
        {
            AddEditableFeatureLayer(layer as EditableFeatureLayer);
        }
        else if (layer is PolyBezierLayer)
        {
            AddPolyBezierLayer(layer as PolyBezierLayer);
        }
        else if (layer is SpecialLineLayer)
        {
            AddSpecialLineLayer(layer as SpecialLineLayer, null);
        }
        else if (layer is GridLayer)
        {
            Action action = async () =>
            {
                await AddGridLayer(layer as GridLayer);
            };

            var extent = this.CurrentExtent;

            Task.Run(() =>
               this.jobs.Add(new Job(
                  new LayerTag(mapScale) { LayerType = LayerType.VectorLayer, BoundingBox = extent },
                  Dispatcher.BeginInvoke(action, DispatcherPriority.Background, null)))
              );
        }
        else if (layer.Type == LayerType.Complex /*|| layer.Type == LayerType.MoveableItem*/)
        {
            SpecialPointLayer? specialPointLayer = null;

            if (layer is SpecialPointLayer)
            {
                specialPointLayer = (SpecialPointLayer)layer;
            }
            else
            {
                // in the case of text layer
                specialPointLayer = ((DrawingItemLayer)layer).SpecialPointLayer;
            }

            if (specialPointLayer is not null)
            {
                Action action = () => AddComplexLayer(specialPointLayer);

                Task.Run(() =>
                  this.jobs.Add(new Job(new LayerTag(mapScale) { LayerType = LayerType.Complex, Tile = null },
                      Dispatcher.BeginInvoke(action, DispatcherPriority.Background, null))));
            }
        }
        else if (layer is TileServiceLayer)
        {
            //their Rendering property must be Tiled and catched by the first `if`
            throw new NotImplementedException();

            //They are handled when UpdateTileInfos is fired
            //continue;
        }
        else if (layer.Type != LayerType.Raster && layer.Type != LayerType.BaseMap && layer.Type != LayerType.ImagePyramid)
        {
            VectorLayer vectorLayer = (VectorLayer)layer;

            Action action = async () =>
            {
                //AddTiledLayer(vectorLayer);
                await AddNonTiledLayer(vectorLayer);
                //ClearBasemap();
            };

            var extent = this.CurrentExtent;

            Task.Run(() =>
               this.jobs.Add(new Job(
                  new LayerTag(mapScale) { LayerType = LayerType.VectorLayer, BoundingBox = extent },
                  Dispatcher.BeginInvoke(action, DispatcherPriority.Background, null)))
              );

        }
        else if (layer.Type != LayerType.Label)
        {
            if (layer.RenderMode == RenderMode.Default)
            {
                var extent = this.CurrentExtent;

                Action action = () =>
                {
                    var rasterLayer = layer as RasterLayer;

                    if (rasterLayer == null)
                    {
                        return;
                    }

                    if (!layer.Extent.Intersects(this.CurrentExtent) && layer.Type != LayerType.BaseMap && !(rasterLayer.DataSource is OfflineGoogleMapDataSource))
                    {
                        System.Diagnostics.Debug.WriteLine($"raster layer escaped!");
                        return;
                    }

                    AddLayer((RasterLayer)layer, extent);

                    //Remove Old base maps
                    ClearOutOfExtent(false);
                };

                Task.Run(() =>
                      this.jobs.Add(new Job(
                         new LayerTag(mapScale) { LayerType = layer.Type, BoundingBox = extent },
                         Dispatcher.BeginInvoke(action, DispatcherPriority.Background, null)))
                        );
            }
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    //consider removing GridLayer and use VectorLayer instead
    private async Task AddGridLayer(GridLayer gridLayer)
    {
        var extent = this.CurrentExtent;

        var mapScale = this.MapScale;

        //consider if layer was Labeled
        var lines = await gridLayer.GetLinesAsync(extent);

        if (lines.IsNullOrEmpty())
            return;

        VectorLayer layer = new VectorLayer(
            "temp grid",
            lines,
            VisualParameters.CreateNew(1),
            LayerType.VectorLayer,
            RenderMode.Default,
            RasterizationMethod.DrawingVisual);

        await this.AddNonTiledLayer(layer);
    }

    #endregion


    #region Private Layer Management

    private void AddTiledLayer(VectorLayer layer)
    {
        Debug.WriteLine($"AddTiledLayer; {DateTime.Now.ToLongTimeString()}; AddTiledLayer called LayerName: {layer.LayerName}");

        if (layer is null)
            return;

        var mapScale = MapScale;

        if (!layer.CanRenderLayer(mapScale))
        {
            layer.Element = null;

            return;
        }

        Action action = async () =>
        {
            if (this.CurrentTileInfos is null)
                return;

            layer.TileManager.Update(CurrentTileInfos.Select(i => i.Parse()).ToList());

            foreach (var region in CurrentTileInfos)
            {
                await AddTiledLayerAsync(layer, region);
            }
        };

        var extent = this.CurrentExtent;

        Task.Run(() =>
        {
            this.jobs.Add(new Job(
                new LayerTag(mapScale) { LayerType = layer.Type, BoundingBox = extent },
                Dispatcher.BeginInvoke(action, DispatcherPriority.Background, null)));
        });
    }


    //This method should be improved. it is not working well
    private async Task AddTiledLayerAsync(VectorLayer layer, TileInfo tile)
    {
        var mapScale = MapScale;

        var totalExtent = this.CurrentExtent;

        var _vt = viewTransform.Clone();

        var layerTile = layer.TileManager.Find(tile);

        if (layerTile is null || layerTile.IsProcessing)
            return;

        if (tile.ZoomLevel != this.NearestGoogleZoomLevel)
        {
            layerTile.IsProcessing = false;
            return;
        }

        layerTile.IsProcessing = true;

        var feature = await (layer.DataSource as IVectorDataSource)!.GetAsFeatureSetAsync(mapScale, tile.WebMercatorExtent);

        if (feature is null || feature.Features.IsNullOrEmpty())
            return;

        if (tile.ZoomLevel != this.NearestGoogleZoomLevel || MapScale != mapScale)
        {
            layerTile.IsProcessing = false;
            return;
        }

        double tileScreenWidth = MapToScreen(tile.WebMercatorExtent.Width);

        double tileScreenHeight = MapToScreen(tile.WebMercatorExtent.Height);

        var area = ParseToRectangleGeometry(tile.WebMercatorExtent);

        var shiftX = tile.WebMercatorExtent.TopLeft.X - totalExtent.TopLeft.X;
        var shiftY = tile.WebMercatorExtent.TopLeft.Y - totalExtent.TopLeft.Y;

        Func<sb.Point, sb.Point> transform = p => _vt.Transform(new Point(p.X - shiftX, p.Y - shiftY)).AsPoint();

        var features = feature.Transform(transform, null).Features;

        var renderingStrategy = RenderStrategyContext.Create(layer);

        var imageBrush = renderingStrategy.Render(features, mapScale, tileScreenWidth, tileScreenHeight);

        if (tile.ZoomLevel != this.NearestGoogleZoomLevel)
        {
            Debug.Print($"MapViewer; {DateTime.Now.ToLongTimeString()}; AddTiledLayerAsync Layer escaped! ZoomLevel Conflict 3 {layer.LayerName} - {tile.ToShortString()} expected zoomLevel:{this.NearestGoogleZoomLevel}");
            return;
        }

        if (imageBrush != null)
        {
            Path path = new Path()
            {
                Data = area,
                Tag = new LayerTag(mapScale) { Layer = layer, IsTiled = true, IsDrawn = true, IsNew = true, Tile = tile.Clone() },
                Fill = imageBrush
            };

            layer.Element = path;

            this.mapView.Children.Add(path);

            Canvas.SetZIndex(path, layer.ZIndex);
        }

        layerTile.IsProcessing = false;
    }


    // todo: consider if layer was Labeled
    private async Task AddNonTiledLayer(VectorLayer layer)
    {
        try
        {
            if (this.CurrentTileInfos == null)
                return;

            var extent = this.CurrentExtent;

            var mapScale = this.MapScale;

            var feature = await (layer.DataSource as IVectorDataSource)!.GetAsFeatureSetAsync(mapScale, extent);

            if (feature is null || feature.Features.IsNullOrEmpty())
                return;

            if (this.MapScale != mapScale || this.CurrentExtent != extent)
                return;

            var area = ParseToRectangleGeometry(extent);

            Func<sb.Point, sb.Point> transform = p => this.MapToScreen(p.AsWpfPoint()).AsPoint();

            var features = feature.Transform(transform).Features;


            var renderingStrategy = RenderStrategyContext.Create(layer);

            var imageBrush = renderingStrategy.Render(features, mapScale, this.mapView.ActualWidth, this.mapView.ActualHeight);

            if (imageBrush is null || this.MapScale != mapScale || this.CurrentExtent != extent)
                return;

            Path path = new Path()
            {
                Data = area,
                Tag = new LayerTag(mapScale) { Layer = layer, IsTiled = false, IsDrawn = true, IsNew = true },
                Fill = imageBrush
            };

            layer.Element = path;

            this.mapView.Children.Add(path);

            Canvas.SetZIndex(path, layer.ZIndex);
        }
        catch (Exception ex)
        {

        }
    }

    private Geometry/*RectangleGeometry */ ParseToRectangleGeometry(sb.BoundingBox mapBoundingBox)
    {
        // approach #1: this approach produce gaps between tiles for +20 zooms 

        //var p1 = mapBoundingBox.TopLeft.AsWpfPoint();
        //var p2 = mapBoundingBox.BottomRight.AsWpfPoint();
        //RectangleGeometry geometry = new RectangleGeometry(new Rect(p1, p2), 0, 0, viewTransform);
        //return geometry;

        // Use PathGeometry with explicit corners to avoid Rect's X+Width/Y+Height
        // floating-point associativity - same coordinate must render at same position
        // for adjacent tiles (Rect computes Right=X+Width which can drift from XMax)
        var pf = new PathFigure
        {
            StartPoint = new System.Windows.Point(mapBoundingBox.XMin, mapBoundingBox.YMax),
            IsClosed = true
        };
        pf.Segments.Add(new LineSegment(new System.Windows.Point(mapBoundingBox.XMax, mapBoundingBox.YMax), true));
        pf.Segments.Add(new LineSegment(new System.Windows.Point(mapBoundingBox.XMax, mapBoundingBox.YMin), true));
        pf.Segments.Add(new LineSegment(new System.Windows.Point(mapBoundingBox.XMin, mapBoundingBox.YMin), true));

        var pathGeometry = new PathGeometry();
        pathGeometry.Figures.Add(pf);
        pathGeometry.Transform = viewTransform;
        return pathGeometry;
    }


    private void AddSpecialLineLayer(SpecialLineLayer layer, Action mouseDown = null)
    {
        var paths = layer.GetPaths(this.viewTransform, this.CurrentExtent, mouseDown);

        foreach (var item in paths)
        {
            (item.RenderTransform as TransformGroup).Children.Add(this.panTransformForPoints);

            item.Tag = new LayerTag(0) { IsTiled = false, Layer = layer, LayerType = LayerType.AnimatingItem };

            if (!this.mapView.Children.Contains(item))
            {
                this.mapView.Children.Add(item);

                //Canvas.SetZIndex(item, int.MaxValue);
                Canvas.SetZIndex(item, layer.ZIndex);
            }
        }

    }

    private void AddEditableFeatureLayer(EditableFeatureLayer layer)
    {
        var path = layer.GetPath(this.viewTransform);

        //path.RenderTransform = this.viewTransform;
        path.RenderTransform = this.panTransformForPoints;

        path.Tag = new LayerTag(0) { Layer = layer, IsTiled = false, LayerType = LayerType.EditableItem };

        this.mapView.Children.Add(path);

        Canvas.SetZIndex(path, layer.ZIndex);

        AddComplexLayer(layer.GetMidVertices(), false);

        AddComplexLayer(layer.GetVertices(), false);

        if (layer.Options.IsMeasureVisible)
        {
            AddComplexLayer(layer.GetEdgeLengthes(), true);
        }

        AddComplexLayer(layer.GetPrimaryVerticesLabels(), true);
    }



    private async void AddLayer(RasterLayer layer, sb.BoundingBox boundingBox)
    {
        var paths = await layer.ParseToPath(boundingBox, this.viewTransform, this.MapScale, GetUnitDistance());

        foreach (var item in paths)
        {
            if (layer.Type == LayerType.BaseMap)
            {
                this.mapView.Children.Insert(0, item);
            }
            else
            {
                this.mapView.Children.Add(item);
            }
        }
    }

    private async Task AddTileServiceLayerAsync(TileServiceLayer layer, TileInfo tile)
    {
        try
        {
            if (_presenter == null)
                return;

            if (tile.ZoomLevel != NearestGoogleZoomLevel || !layer.HasTheSameMapProvider(_presenter.SelectedMapProvider))
                return;

            // 1401.11.07
            var geoImage = await layer.GetTileAsync(tile, _presenter.HttpClient);

            if (tile.ZoomLevel != NearestGoogleZoomLevel || !layer.HasTheSameMapProvider(this._presenter.SelectedMapProvider))
                return;

            var webMercatorExtent = geoImage.GeodeticWgs84BoundingBox.Transform(MapProjects.GeodeticWgs84ToWebMercator);

            //var tileWebMercatorExtent = tile.WebMercatorExtent;

            //if (webMercatorExtent != tileWebMercatorExtent)
            //{

            //}

            //var prevTileX = new TileInfo(tile.RowNumber, tile.ColumnNumber - 1, tile.ZoomLevel);
            //var nextTileX = new TileInfo(tile.RowNumber, tile.ColumnNumber + 1, tile.ZoomLevel);
            //var nextTileY = new TileInfo(tile.RowNumber + 1, tile.ColumnNumber, tile.ZoomLevel);

            //if (prevTileX.WebMercatorExtent.XMax != tile.WebMercatorExtent.XMin)
            //{

            //}
            //if (nextTileX.WebMercatorExtent.XMin != tile.WebMercatorExtent.XMax)
            //{// this break point never hit
            //}
            //if (nextTileY.WebMercatorExtent.YMax != tile.WebMercatorExtent.YMin)
            //{// this break point never hit

            //}

            //if (tile.ZoomLevel > 19)
            //{
            //    double overlapEpsilon = GetUnitDistance() / MapScale ;

            //    webMercatorExtent = webMercatorExtent.Extend(overlapEpsilon);
            //}

            var area = ParseToRectangleGeometry(webMercatorExtent);

            ImageBrush fill;

            try
            {
                fill = new ImageBrush(IRI.Maptor.Jab.Common.Helpers.ImageUtility.CreateBitmapImage(geoImage.Image));
            }
            catch (Exception ex)
            {
                fill = new ImageBrush();
                Debug.WriteLine($"MapViewer; AddLayerAsync(TileServiceLayer) {ex.Message}");
            }

            Path path = new Path()
            {
                Fill = fill,
                //Data = geometry,
                Data = area,
                StrokeThickness = _presenter.MapSettings.ShowTileBorder ? 1 : 0,
                Stroke = Brushes.Gray,
                StrokeDashArray = [2, 3],
                Tag = new LayerTag(this.MapScale) { Layer = layer, Tile = tile },
                //ToolTip = $"XMin: {webMercatorExtent.XMin:G18}, XMax: {webMercatorExtent.XMax:G18}",
                //SnapsToDevicePixels = true,
                //UseLayoutRounding = true,
                //ClipToBounds = true,
            };

            layer.Element = path;

            if (layer.Type == LayerType.BaseMap)
            {
                this.mapView.Children.Insert(0, path);
            }
            else
            {
                this.mapView.Children.Add(path);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("AddLayerAsync " + ex.Message);
        }
    }

    #endregion


    #region Refresh

    private void RefreshBaseMaps()
    {
        this.Clear(tag => (tag.IsTiled || tag.LayerType == LayerType.BaseMap), false);

        var tiles = this.CurrentTileInfos;

        if (tiles == null)
            return;

        // 1401.12.05
        IEnumerable<ILayer> infos = this._layerManager.UpdateAndGetLayers(InverseMapScale, RenderMode.Tiled).ToList();

        foreach (var tile in tiles)
        {
            RefreshTiles(infos, tile, layer => layer.RenderMode == RenderMode.Tiled && layer.Type == LayerType.BaseMap);
        }
    }

    public void RefreshTiles(IEnumerable<ILayer> infos, TileInfo tile, Func<ILayer, bool> criteria)
    {
        double mapScale = MapScale;

        Action action = async () =>
        {
            foreach (ILayer item in infos)
            {
                if (!item.CanRenderLayer(mapScale))
                {
                    item.Element = null;

                    continue;
                }

                if (this.CurrentTileInfos == null || !this.CurrentTileInfos.Contains(tile))
                    return;

                if (item.RenderMode != RenderMode.Tiled)
                    continue;

                //Do not draw if criteria not satisfied
                if (criteria(item))
                {
                    if (item is VectorLayer vectorLayer)
                    {
                        vectorLayer.TileManager.TryAdd(tile);

                        await AddTiledLayerAsync(vectorLayer, tile);
                    }
                    else if (item is TileServiceLayer tileServiceLayer)
                    {
                        await AddTileServiceLayerAsync(tileServiceLayer, tile);
                    }
                    else
                    {
                        //return;
                        throw new NotImplementedException();
                    }
                }
            }
        };

        Task.Run(() =>
        {
            //lock (locker)
            //{
            this.jobs.Add(
                    new Job(
                        new LayerTag(mapScale) { LayerType = LayerType.None, Tile = tile },
                        Dispatcher.BeginInvoke(action, DispatcherPriority.Background)));
            //}
        });

    }

    private void RefreshLayerVisibility(ILayer layer)
    {
        if (layer == null)
            return;

        //Clear current layer
        this.ClearLayer(layer, remove: false, forceRemove: false);

        if (layer.RenderMode == RenderMode.Tiled && layer is VectorLayer vectorLayer)
        {
            AddTiledLayer(vectorLayer);
        }
        else
        {
            AddLayer(layer);
        }
    }

    //POTENTIALLY ERROR PROUNE; Check if exceptions are catched correctly; 
    //POTENTIALLY ERROR PROUNE; Captured Variables
    //IMPROVEMENT; use vector approach for light vector layers insted of "AddLayerAsDrawing"
    public void Refresh(bool isNewExtent)
    {
        if (this.CurrentTileInfos == null)
            return;

        StopUnnecessaryJobs();

        ClearNonTiled();

        var mapScale = this.MapScale;

        IEnumerable<ILayer> infos = this._layerManager.UpdateAndGetLayers(1.0 / mapScale, RenderMode.Default);

        if (infos == null) return;

        foreach (ILayer item in infos)
        {
            if (!item.CanRenderLayer(mapScale))
            {
                item.Element = null;

                continue;
            }

            if (MapScale != mapScale)
                return;

            if (item.RenderMode == RenderMode.Tiled)
                continue;

            AddLayer(item);
        }
        //****************************

        //ResetViewTransformForPoints();
        this.panTransformForPoints.X = 0;
        this.panTransformForPoints.Y = 0;

        this.OnExtentChanged?.Invoke(null, isNewExtent);
    }

    #endregion

    #region Moveable Item

    bool itemIsMoving = false;

    FrameworkElement currentMoveableItem;


    //POTENTIALLY ERROR PROUNE; What if the Element has no scaletransform
    private void AddComplexLayer(SpecialPointLayer specialPointLayer, bool withAnimation = true)
    {
        specialPointLayer.HandleCollectionChanged = (e) =>
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    foreach (var item in e.NewItems)
                    {
                        AddComplexLayerItem(specialPointLayer, (Locateable)item, withAnimation);
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    foreach (var item in e.OldItems)
                    {
                        var locateable = item as Locateable;

                        if (this.mapView.Children.Contains(locateable.Element))
                            this.mapView.Children.Remove(locateable.Element);
                    }
                    break;

                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    ClearLayer(specialPointLayer, remove: false, forceRemove: false);
                    break;

                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                default:
                    throw new NotImplementedException();
            }
        };

        var items = specialPointLayer.Items.Where(i => this.CurrentExtent.Intersects(new sb.Point(i.X, i.Y))).ToList();

        foreach (var item in items)
        {
            AddComplexLayerItem(specialPointLayer, item, withAnimation);
        }
    }

    //POTENTIALLY ERROR PRONOUN
    private void AddComplexLayerItem(SpecialPointLayer specialPointLayer, Locateable item, bool withAnimation = true)
    {
        item.OnPositionChanged -= Item_OnPositionChanged;
        item.OnPositionChanged += Item_OnPositionChanged;

        var element = item.Element;

        if (this.mapView.Children.Contains(element))
            return;

        //element.Opacity = specialPointLayer.Opacity;

        specialPointLayer.Element = element;

        var height = double.IsNaN(element.Height) ? element.ActualHeight : element.Height;
        var width = double.IsNaN(element.Width) ? element.ActualWidth : element.Width;

        var screenLocation = item.AncherFunction(MapToScreen(new Point(item.X, item.Y)), width, height);

        if (height != 0 && width != 0)
        {
            var tempPoint = item.AncherFunction(new Point(0, 0), width, height);

            element.RenderTransformOrigin = new Point(-tempPoint.X / width, -tempPoint.Y / height);
        }
        else
        {
            element.RenderTransformOrigin = new Point(0, 0);
        }

        if (element is ActiveExtentView activeExtentView)
        {
            var mapWidth = ScreenToMap(width);
            var mapHeight = ScreenToMap(height);

            var newExtent = new sb.BoundingBox(new sb.Point(item.X, item.Y), mapWidth, mapHeight);

            Canvas.SetLeft(activeExtentView, 0);
            Canvas.SetTop(activeExtentView, 0);

            (activeExtentView.DataContext as ActiveExtentViewModel)?.UpdateExtent(newExtent, true);

            //var testName = "#testB";
            //_presenter.RemoveDrawingItem(testName);
            //_presenter.AddDrawingItem(newExtent.AsGeometry<sb.Point>(SridHelper.WebMercator), testName, VisualParameters.Get(Colors.Red, Colors.Blue, 10, 0.9));
        }

        var scaleTransform = ((TransformGroup)(element.RenderTransform)).Children.First();

        ((TransformGroup)(element.RenderTransform)).Children.Clear();

        ((TransformGroup)(element.RenderTransform)).Children.Add(scaleTransform);

        ((TransformGroup)(element.RenderTransform)).Children.Add(this.panTransformForPoints);

        ((TransformGroup)(element.RenderTransform)).Children.Add(new TranslateTransform(screenLocation.X, screenLocation.Y));

        //What about other types: RightClickOption, GridAndGraticule
        //if (specialPointLayer.Type == LayerType.MoveableItem)
        if (specialPointLayer.IsMovable)
        {
            element.Tag = new LayerTag(this.MapScale)
            {
                Layer = specialPointLayer,
                IsTiled = false,
                LayerType = specialPointLayer.Type,
                // in the case specialPointLayer is used in DrawingItemLayer to handle proper remove of element from canvas
                AncestorLayerId = specialPointLayer.ParentLayerId,
            };

            element.MouseLeftButtonDown -= Element_MouseDownForMoveableItem;
            element.MouseLeftButtonDown += Element_MouseDownForMoveableItem;
        }
        else if (specialPointLayer.Type == LayerType.Complex || specialPointLayer.Type == LayerType.EditableItem)
        {
            element.Tag = new LayerTag(this.MapScale)
            {
                Layer = specialPointLayer,
                IsTiled = false,
                LayerType = specialPointLayer.Type,
                AncestorLayerId = specialPointLayer.ParentLayerId
            };
        }
        else
        {
            throw new NotImplementedException();
        }

        if (withAnimation)
        {
            AddToCanvasWithAnimation(element, element.Opacity, specialPointLayer);
        }
        else
        {
            if (this.mapView.Children.Contains(element))
                return;

            this.mapView.Children.Add(element);

            if (specialPointLayer.AlwaysTop)
            {
                Canvas.SetZIndex(element, specialPointLayer.ZIndex);
            }

        }
    }

    private void AddToCanvasWithAnimation(FrameworkElement element, double finalOpacity, SpecialPointLayer specialPointLayer)
    {
        if (this.mapView.Children.Contains(element))
            return;

        DoubleAnimation animation = new DoubleAnimation()
        {
            From = 0,
            To = finalOpacity,
            Duration = new Duration(TimeSpan.FromMilliseconds(1000)),
            FillBehavior = FillBehavior.Stop,
            AccelerationRatio = .2,
            DecelerationRatio = .2
        };

        this.mapView.Children.Add(element);

        if (specialPointLayer.AlwaysTop)
        {
            Canvas.SetZIndex(element, int.MaxValue);
        }
        else
        {
            Canvas.SetZIndex(element, specialPointLayer.ZIndex);
        }

        element.BeginAnimation(OpacityProperty, animation);
    }


    public void Element_MouseDownForMoveableItem(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;

        itemIsMoving = true;

        var element = sender as FrameworkElement;

        this.currentMoveableItem = element;

        this.mapView.CaptureMouse();

        this.mapView.MouseMove -= Element_MouseMoveForMoveableItem;
        this.mapView.MouseMove += Element_MouseMoveForMoveableItem;

        this.mapView.MouseUp -= Element_MouseUpForMoveableItem;
        this.mapView.MouseUp += Element_MouseUpForMoveableItem;


        this.prevMouseLocation = (e.GetPosition(this.mapView));

        this.startPointLocationForPan = this.prevMouseLocation;

        var layer = ((this.currentMoveableItem.Tag as LayerTag).Layer as SpecialPointLayer);

        layer.SelectLocatable(this.currentMoveableItem);
    }

    public void Element_MouseMoveForMoveableItem(object sender, MouseEventArgs e)
    {
        Point currentMouseLocation = (e.GetPosition(this.mapView));

        var currentMapLocation = ScreenToMap(currentMouseLocation);

        var prevMapLocation = ScreenToMap(prevMouseLocation);

        var layer = ((this.currentMoveableItem.Tag as LayerTag).Layer as SpecialPointLayer);

        var locateable = layer.Get(this.currentMoveableItem);

        if (locateable != null)
        {
            locateable.X += currentMapLocation.X - prevMapLocation.X;
            locateable.Y += currentMapLocation.Y - prevMapLocation.Y;

            if (locateable.CanBeUsedAsEditingPoint)
            {
                this.CurrentEditingPoint = new Point(locateable.X, locateable.Y);
            }
        }

        this.prevMouseLocation = currentMouseLocation;

    }

    public void Element_MouseUpForMoveableItem(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;

        Point currentMouseLocation = ScreenToMap((e.GetPosition(this.mapView)));

        var prevMapLocation = ScreenToMap(prevMouseLocation);

        var offset = new Point(currentMouseLocation.X - prevMapLocation.X, currentMouseLocation.Y - prevMapLocation.Y);

        var layer = ((this.currentMoveableItem.Tag as LayerTag).Layer as SpecialPointLayer);

        var locateable = layer.Get(this.currentMoveableItem);

        if (locateable != null)
        {
            locateable.X += offset.X;
            locateable.Y += offset.Y;

            if (locateable.CanBeUsedAsEditingPoint)
            {
                this.CurrentEditingPoint = new Point(locateable.X, locateable.Y);
            }

        }

        this.mapView.MouseMove -= Element_MouseMoveForMoveableItem;

        this.mapView.MouseUp -= Element_MouseUpForMoveableItem;

        itemIsMoving = false;

        this.mapView.ReleaseMouseCapture();

        locateable?.RaiseMouseUpEvent();
    }

    //POTENTIALLY ERROR PROUNE; What if the Element has no scaletransform
    private void Item_OnPositionChanged(object? sender, EventArgs e)
    {
        var item = sender as Locateable;

        var element = item.Element;

        var width = double.IsNaN(element.Width) ? element.ActualWidth : element.Width;
        var height = double.IsNaN(element.Height) ? element.ActualHeight : element.Height;

        var screenLocation = item.AncherFunction(MapToScreen(new Point(item.X, item.Y)), width, height);

        ((TransformGroup)(element.RenderTransform)).Children[2] = (new TranslateTransform(screenLocation.X, screenLocation.Y));

    }


    #endregion

    #region Others
    //POTENTIALLY ERROR PROUNE; not sure everything is considered or not
    private void ResetMapViewEvents()
    {
        //this.SetCursor(Cursors.Arrow);
        this.SetCursor(CursorSettings[_currentMouseAction]);

        this.mapView.MouseDown -= mapView_MouseDownForPan;
        this.mapView.MouseUp -= mapView_MouseUpForPan;
        this.mapView.MouseMove -= mapView_MouseMoveForPan;

        this.mapView.MouseUp -= mapView_MouseDownForZoomOut;
        this.mapView.MouseDown -= mapView_MouseDownForZoom;
        this.mapView.MouseUp -= mapView_MouseUpForZoom;


        //this.mapView.MouseDown -= MapView_MouseDownForStartDrawing;
        this.mapView.MouseDown -= MapView_MouseDownForPanWhileStartDrawing;
        this.mapView.MouseUp -= MapView_MouseUpForPanWhileStartDrawing;
        this.mapView.MouseMove -= MapView_MouseMoveForPanWhileStartDrawing;


        this.mapView.MouseMove -= MapView_MouseMoveForPanWhileStartNewPart;
        this.mapView.MouseDown -= MapView_MouseDownForPanWhileStartNewPart;
        this.mapView.MouseUp -= MapView_MouseUpForPanWhileStartNewPart;


        this.mapView.MouseDown -= MapView_MouseDownForPanWhileDrawing;
        this.mapView.MouseMove -= MapView_MouseMoveForPanWhileDrawing;
        this.mapView.MouseUp -= MapView_MouseUpForForPanWhileDrawing;

        this.mapView.MouseMove -= MapView_MouseMoveSelectThePoint;
        this.mapView.MouseDown -= MapView_MouseDownForPanWhileSelectThePoint;

        this.mapView.MouseMove -= MapView_MouseMoveForDrawing;


        this.mapView.MouseDown -= MapView_MouseDownForRectangleDrawing;
        this.mapView.MouseMove -= MapView_MouseMoveForRectangleDrawing;
        this.mapView.MouseUp -= MapView_MouseUpForRectangleDrawing;
    }

    public void ReleaseEvents()
    {
        this.ResetMapViewEvents();
    }

    public void SetDefaultCursor(MapAction action, Cursor cursor)
    {
        this.CursorSettings[action] = cursor;
    }

    public void SetCursor(Cursor cursor)
    {
        if (itWasPanningWhileDrawing ||
            itWasPanningWhileStartDrawing ||
            itWasPanningWhileStartNewPart ||
            itWasPanningWhileSelectThePoint)
            return;

        // this condition prevent the cursor to
        // change to cross in drawig mode
        //if (this.Status == MapStatus.Drawing)
        //    return;

        this.mapView.Cursor = cursor;
    }

    public void SetConnectionState(bool isConnectedToInternet)
    {
        foreach (var item in this.Layers)
        {
            if (item is TileServiceLayer)
            {
                ((TileServiceLayer)item).IsOffline = !isConnectedToInternet;
            }
        }
    }

    #endregion


    #region Clearing-Removing layers, Job Management

    private void CancelAsyncMapInteractions()
    {
        CancelDrawing();

        CancelEditGeometry();

        CancelGetBezier();

        //CancelMeasure();
    }

    private void StopUnnecessaryJobs()
    {
        lock (locker)
        {
            for (int i = this.jobs.Count - 1; i >= 0; i--)
            {
                var currentJob = this.jobs[i];

                if (currentJob == null)
                {
                    this.jobs.Remove(currentJob);

                    continue;
                }

                //Do not cancel processing tiles when mouse up for pan
                //if (this.jobs[i].Tag.Tile != null && this.CurrentTileInfos.Contains(jobs[i].Tag.Tile) && jobs[i].Tag.Scale == this.MapScale)
                if (this.CurrentTileInfos.Contains(currentJob.Tag.Tile) && currentJob.Tag.Scale == this.MapScale)
                    continue;

                //Debug.WriteLine($"Job Stopped [@StopUnnecessaryJobs]; tile: {currentJob.Tag.Tile?.ToShortString()} jobScale:{currentJob.Tag.Scale} expectedScale:{MapScale}");

                currentJob.Operation.Abort();

                this.jobs.Remove(currentJob);
            }
        }
    }

    private void ClearOutOfExtent(bool justTiled)
    {
        for (int i = this.mapView.Children.Count - 1; i >= 0; i--)
        {
            //Complex layer items may not be Path, so use FrameworkElement
            var tag = (LayerTag)((FrameworkElement)this.mapView.Children[i]).Tag;

            if (justTiled && !tag.IsTiled)
                continue;

            if (tag.Tile != null && (tag.Tile.ZoomLevel != this.NearestGoogleZoomLevel || !this.CurrentTileInfos.Contains(tag.Tile)))
            {
                this.mapView.Children.RemoveAt(i);

                continue;
            }

            if (this.CurrentTileInfos.Contains(tag.Tile) && tag.Tile.ZoomLevel == this.NearestGoogleZoomLevel)// && tag.Scale == this.MapScale)
                continue;

            //Why checking both layer type and extent?
            if (tag.LayerType == LayerType.Raster && this.CurrentExtent.Intersects(tag.Layer.Extent))
                continue;

            //Why checking both layer type and extent?
            if (tag.LayerType == LayerType.ImagePyramid && this.CurrentExtent.Intersects(tag.Layer.Extent))
                continue;


            //1397.04.02 why not checking this first?
            //if (( tag.LayerType.HasFlag(LayerType.Feature) ||   tag.LayerType.HasFlag(LayerType.VectorLayer)) &&
            if ((tag.LayerType == LayerType.VectorLayer) &&
                this.CurrentExtent.Intersects(tag?.Layer?.Extent ?? sb.BoundingBox.NaN))
                continue;

            //if (tag.LayerType.HasFlag(LayerType.Drawing) && this.CurrentExtent.Intersects(tag?.Layer?.Extent ?? sb.BoundingBox.NaN))
            //    continue;

            //if (tag.LayerType.HasFlag(LayerType.MoveableItem) && this.CurrentExtent.Intersects(tag?.Layer?.Extent ?? sb.BoundingBox.NaN))
            //    continue;
            if (tag.LayerType == LayerType.Drawing && this.CurrentExtent.Intersects(tag?.Layer?.Extent ?? sb.BoundingBox.NaN))
                continue;

            if (tag.LayerType == LayerType.Complex /*LayerType.MoveableItem*/ && this.CurrentExtent.Intersects(tag?.Layer?.Extent ?? sb.BoundingBox.NaN))
                continue;


            this.mapView.Children.RemoveAt(i);
        }
    }

    public void ClearNonTiled()
    {
        Clear(tag => !tag.IsTiled && tag.LayerType != LayerType.BaseMap, remove: false, forceRemove: false);
    }

    public void ClearTiled()
    {
        Clear(tag => tag.IsTiled, remove: false, forceRemove: false);
    }

    public void Clear(Predicate<LayerTag> criteria, bool remove, bool forceRemove = false, bool keepEmptyParentGroup = false)
    {
        for (int i = this.mapView.Children.Count - 1; i >= 0; i--)
        {
            var tag = ((LayerTag)((FrameworkElement)(this.mapView.Children[i])).Tag);

            if (criteria(tag))
            {
                this.mapView.Children.RemoveAt(i);

                if (remove && tag.Layer != null)
                {
                    _layerManager.Remove(tag.Layer, forceRemove: forceRemove, keepEmptyParentGroup: keepEmptyParentGroup);
                }
            }
        }
    }

    public void Clear(Predicate<ILayer> criteria, bool remove, bool forceRemove = false, bool keepEmptyParentGroup = false)
    {
        for (int i = this.mapView.Children.Count - 1; i >= 0; i--)
        {
            //Complex layer items may not be Path, so use FrameworkElement
            var tag = ((LayerTag)((FrameworkElement)(this.mapView.Children[i])).Tag);

            if (tag?.Layer != null)
            {
                if (criteria(tag.Layer))
                {
                    this.mapView.Children.RemoveAt(i);
                }
            }
        }

        if (remove)
        {
            _layerManager.Remove(criteria, forceRemove: forceRemove, keepEmptyParentGroup: false);
        }
    }

    public void ClearLayer(LayerType type, bool remove, bool forceRemove = false)
    {
        //Clear(tag => tag.LayerType.HasFlag(type), remove, forceRemove);
        Clear(tag => tag.LayerType == (type), remove, forceRemove);
    }

    public void ClearLayer(ILayer layer, bool remove, bool forceRemove = false, bool keepEmptyParentGroup = false)
    {
        Clear(new Predicate<LayerTag>(tag => tag.Layer == layer), remove, forceRemove, keepEmptyParentGroup);

        //in the case of image pyramids, the actual layer will not be remove
        //"tag.AncestorLayerId == layer?.Id" is not a layer in layerManagemnr
        //so _layerManager.Remove(layer, forceRemove) removes it from map layers and
        //ClearLayerByAncestor(layer) removes if from canvas
        ClearLayerByAncestor(layer);

        //97.08.17
        //In the case of DrawingLayer, it is not directly added to children of MapViewr
        //so in order to remove it, it should be done here
        if (remove)
        {
            _layerManager.Remove(layer, forceRemove, keepEmptyParentGroup);
        }
    }

    public void ClearLayerByAncestor(ILayer ancestorLayer)
    {
        Clear(new Predicate<LayerTag>(tag => tag.AncestorLayerId == ancestorLayer?.LayerId), false, false);
    }

    public void ClearLayer(string layerName, bool remove, bool forceRemove = false)
    {
        Clear(layer => layer.LayerName == layerName, remove, forceRemove);
    }

    public void RemovePolyBezierLayers()
    {
        for (int i = this.Layers.Count - 1; i >= 0; i--)
        {
            if (this.Layers[i] is PolyBezierLayer)
            {
                RemovePolyBezierLayer(this.Layers[i] as PolyBezierLayer);
            }
        }
    }

    public void RemoveGeometries()
    {
        ClearLayer(LayerType.Drawing, true);
    }

    public void RemoveEditableFeatureLayer(EditableFeatureLayer layer)
    {
        if (layer != null)
        {
            ClearLayer((ILayer)layer, remove: true, forceRemove: false);

            ClearLayer((ILayer)layer.GetVertices(), remove: true, forceRemove: false);

            ClearLayer((ILayer)layer.GetMidVertices(), remove: true, forceRemove: false);

            ClearLayer((ILayer)layer.GetEdgeLengthes(), remove: true, forceRemove: false);

            ClearLayer(layer.GetPrimaryVerticesLabels(), remove: true, forceRemove: false);
        }
    }

    public void RemovePolyBezierLayer(PolyBezierLayer layer)
    {
        ClearLayer(layer, remove: true, forceRemove: true);

        ClearLayer(layer.GetMainPointLayer(), remove: true, forceRemove: true);

        ClearLayer(layer.GetControlPointLayer(), remove: true, forceRemove: true);

        if (layer.IsDecorated)
        {
            ClearLayer(layer.GetDecorateLayer(), remove: true, forceRemove: true);
        }
    }

    #endregion


    #region RightClick Options

    FrameworkElement rightClickOptions;

    ILocateable rightClickDataContext;

    public void RemoveRightClickOptions()
    {
        if (this.mapView.Children.Contains(rightClickOptions))
        {
            this.mapView.Children.Remove(rightClickOptions);
        }

        ClearLayer(LayerType.RightClickOption, true);
    }

    public void RegisterRightClickOptions()
    {
        this.mapView.MouseUp -= mapView_MouseUpForRightClickOptions;
        this.mapView.MouseUp += mapView_MouseUpForRightClickOptions;
    }

    public void RegisterRightClickContextOptions<T>(ILocateable context) where T : FrameworkElement, new()
    {
        this.mapView.MouseUp -= mapView_MouseUpForRightClickOptions;
        this.mapView.MouseUp += mapView_MouseUpForRightClickOptions;

        this.rightClickOptions = new T();

        this.rightClickDataContext = context;

        this.rightClickOptions.DataContext = context;
    }

    public void RegisterRightClickContextOptions(FrameworkElement view, ILocateable context)
    {
        this.mapView.MouseUp -= mapView_MouseUpForRightClickOptions;
        this.mapView.MouseUp += mapView_MouseUpForRightClickOptions;

        this.rightClickOptions = view;

        this.rightClickDataContext = context;

        this.rightClickOptions.DataContext = context;
    }


    public void UnregisterRightClickContextOptions()
    {
        this.rightClickOptions = null;

        this.mapView.MouseUp -= mapView_MouseUpForRightClickOptions;
    }

    void mapView_MouseUpForRightClickOptions(object sender, MouseButtonEventArgs e)
    {
        //Do not raise when other options are available
        if (e.OriginalSource != this.mapView && this.Status != MapStatus.Drawing)
        {
            return;
        }

        RemoveRightClickOptions();

        if (e.ChangedButton != MouseButton.Right || itemIsMoving)
        {
            return;
        }

        var screenLocation = e.GetPosition(this.mapView);

        FrameworkElement? view = null;

        if (this.Status == MapStatus.Drawing)
        {
            view = GetRightClickOptionsForDraw();

            var context = (ILocateable)view.DataContext;

            context.Location = ScreenToMap(screenLocation).AsPoint();

            view = (FrameworkElement)Activator.CreateInstance(view.GetType());

            view.DataContext = context;
        }
        else if (rightClickOptions != null)
        {
            view = (FrameworkElement)Activator.CreateInstance(rightClickOptions.GetType());

            this.rightClickDataContext.Location = ScreenToMap(screenLocation).AsPoint();

            view.DataContext = this.rightClickDataContext;

        }

        if (view == null)
            return;

        view.RenderTransformOrigin = new Point(view.Width / 2, -view.Height);

        var scaleTransform = ((TransformGroup)(view.RenderTransform)).Children.First();
        ((TransformGroup)(view.RenderTransform)).Children.Clear();

        ((TransformGroup)(view.RenderTransform)).Children.Add(scaleTransform);

        ((TransformGroup)(view.RenderTransform)).Children.Add(this.panTransformForPoints);

        ((TransformGroup)(view.RenderTransform)).Children.Add(
            new TranslateTransform(
                screenLocation.X - view.Width / 2,
                screenLocation.Y - view.Height / 2));

        Canvas.SetZIndex(view, int.MaxValue);

        view.Tag = new LayerTag(this.MapScale) { IsTiled = false, LayerType = LayerType.RightClickOption };

        this.mapView.Children.Add(view);

    }

    public void AddRightClickOptions(FrameworkElement options, MouseButtonEventArgs e, ILocateable context)
    {
        RemoveRightClickOptions();

        if (e.ChangedButton != MouseButton.Right || itemIsMoving)
        {
            return;
        }

        var screenLocation = e.GetPosition(this.mapView);

        var rightClickOptions = options;// (FrameworkElement)Activator.CreateInstance(options.GetType());

        context.Location = ScreenToMap(screenLocation).AsPoint();

        rightClickOptions.DataContext = context;

        rightClickOptions.RenderTransformOrigin = new Point(rightClickOptions.Width / 2, -rightClickOptions.Height);

        var scaleTransform = ((TransformGroup)(rightClickOptions.RenderTransform)).Children.First();
        ((TransformGroup)(rightClickOptions.RenderTransform)).Children.Clear();

        ((TransformGroup)(rightClickOptions.RenderTransform)).Children.Add(scaleTransform);

        ((TransformGroup)(rightClickOptions.RenderTransform)).Children.Add(this.panTransformForPoints);

        ((TransformGroup)(rightClickOptions.RenderTransform)).Children.Add(
            new TranslateTransform(
                screenLocation.X - rightClickOptions.Width / 2,
                screenLocation.Y - rightClickOptions.Height / 2));

        Canvas.SetZIndex(rightClickOptions, int.MaxValue);

        rightClickOptions.Tag = new LayerTag(this.MapScale) { IsTiled = false, LayerType = LayerType.RightClickOption };

        this.mapView.Children.Add(rightClickOptions);

        e.Handled = true;

    }

    #endregion


    #region DrawGeometries & Anot

    public void Flash(List<IRI.Maptor.Sta.Common.Primitives.Point> points)
    {
        //ClearAnimatingItems();
        ClearLayer(LayerType.AnimatingItem, false);

        if (points == null)
            return;

        foreach (var item in points)
        {
            AddFlash(item);
        }
    }

    public void Flash(IRI.Maptor.Sta.Common.Primitives.Point mapPoint)
    {
        //ClearAnimatingItems();
        ClearLayer(LayerType.AnimatingItem, false);

        AddFlash(mapPoint);
    }

    private void AddFlash(IRI.Maptor.Sta.Common.Primitives.Point mapPoint)
    {
        if (mapPoint == null || mapPoint.IsNaN())
        {
            return;
        }

        Point point = this.panTransformForPoints.Inverse.Transform(this.viewTransform.Transform(mapPoint.AsWpfPoint()));

        EllipseGeometry geo = new EllipseGeometry(point, 8, 8);

        geo.Transform = this.panTransformForPoints;

        Path path = new Path()
        {
            Data = geo,
            Fill = new SolidColorBrush(Colors.Yellow),
            StrokeThickness = 2,
            Stroke = new SolidColorBrush(Colors.DarkGray),
            Tag = new LayerTag(MapScale) { LayerType = LayerType.AnimatingItem, IsTiled = false },// "anot",
            Opacity = .8
        };

        Point center = this.panTransformForPoints.Transform(point);

        ScaleTransform s = new ScaleTransform(1, 1, center.X, center.Y);

        path.RenderTransform = s;

        this.mapView.Children.Add(path);

        Canvas.SetZIndex(path, int.MaxValue);

        DoubleAnimation animation = new DoubleAnimation()
        {
            RepeatBehavior = new RepeatBehavior(5),
            AccelerationRatio = .2,
            DecelerationRatio = .8,
            FillBehavior = System.Windows.Media.Animation.FillBehavior.Stop,
            From = 1,
            To = 0,
            Duration = new Duration(TimeSpan.FromMilliseconds(1000))
        };
        DoubleAnimation scaleAnimation = new DoubleAnimation()
        {
            RepeatBehavior = new RepeatBehavior(5),
            AccelerationRatio = .2,
            DecelerationRatio = .8,
            FillBehavior = System.Windows.Media.Animation.FillBehavior.Stop,
            From = 1,
            To = 1.5,
            Duration = new Duration(TimeSpan.FromMilliseconds(1000))
        };

        path.BeginAnimation(Path.OpacityProperty, animation);
        s.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
        s.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
    }

    private async Task DrawGeometriesAsync(string layerName, LayerType layerType, List<Geometry<sb.Point>> geometries, VisualParameters visualParams)
    {
        if (geometries.IsNullOrEmpty())
            return;

        if (layerType != LayerType.Drawing && layerType != LayerType.Selection && layerType != LayerType.Highlight)
            throw new ArgumentOutOfRangeException("MapViewer > DrawGeometriesAsync > wrong layerType");

        var source = new MemoryDataSource(geometries);

        var layer = new VectorLayer(
            layerName,
            source,
            [new SimpleSymbolizer(visualParams)],
            layerType,
            RenderMode.Default,
            RasterizationMethod.DrawingVisual,
            ScaleInterval.All);

        this._layerManager.Add(layer, InverseMapScale);

        //AddTiledLayer(layer);
        await AddNonTiledLayer(layer);
    }


    //Get the FontFamily in method parameters
    public async Task DrawGeometriesAsync(string layerName, List<Geometry<sb.Point>> geometries, VisualParameters visualParams)
    {
        await DrawGeometriesAsync(layerName, LayerType.Drawing, geometries, visualParams);

    }

    public async Task HighlightGeometries(string layerName, List<Geometry<sb.Point>> geometries, VisualParameters visualParams)
    {
        await DrawGeometriesAsync(layerName, LayerType.Highlight, geometries, visualParams);
    }

    public async Task SelectGeometriesAsync(string layerName, List<Geometry<sb.Point>> geometries, VisualParameters visualParams)
    {
        ClearLayer(LayerType.Selection, true);

        await DrawGeometriesAsync(layerName, LayerType.Selection, geometries, visualParams);
    }

    #endregion


    #region Print

    public void Print()
    {
        //IRI.Maptor.Jab.Common.Helpers.PrintHelper.Print(this.mapView);
        IRI.Maptor.Jab.Common.Helpers.PrintHelper.Print(this);
    }

    #endregion


    #region Pan

    Point prevMouseLocation;

    //POSSIBLY EXTRA FIELD
    Point startPointLocationForPan;

    //bool isPanEnabled = false;

    public void Pan()
    {
        //this.isPanEnabled = true;

        ResetMapViewEvents();

        this.mapView.MouseDown -= mapView_MouseDownForPan;
        this.mapView.MouseDown += mapView_MouseDownForPan;

        this.CurrentMouseAction = MapAction.Pan;

        //this.mapView.MouseUp -= mapView_MouseUpForPan;
        //this.mapView.MouseUp += mapView_MouseUpForPan;
    }

    public void ReleasePan()
    {
        //this.isPanEnabled = false;
        this.CurrentMouseAction = MapAction.None;

        ResetMapViewEvents();

        this.mapView.MouseDown -= mapView_MouseDownForPan;
    }

    private void mapView_MouseDownForPan(object sender, MouseButtonEventArgs e)
    {
        //Debug.WriteLine(new StackTrace().GetFrame(0).GetMethod().Name, _eventEntered);

        if (this.viewTransform == null || itemIsMoving)
        {
            //Debug.WriteLine(new StackTrace().GetFrame(0).GetMethod().Name, _eventEscaped);

            return;
        }

        this.IsPanning = true;
        //Abort();

        Mouse.Capture(this.mapView);

        //ClearAnimatingItems();
        ClearLayer(LayerType.AnimatingItem, false);

        ClearLayer(LayerType.RightClickOption, false);

        this.prevMouseLocation = e.GetPosition(this.mapView);

        this.startPointLocationForPan = this.prevMouseLocation;

        this.MouseMove -= mapView_MouseMoveForPan;
        this.MouseMove += mapView_MouseMoveForPan;

        this.MouseUp -= mapView_MouseUpForPan;
        this.MouseUp += mapView_MouseUpForPan;
    }

    private void mapView_MouseUpForPan(object sender, MouseButtonEventArgs e)
    {
        this.MouseMove -= mapView_MouseMoveForPan;

        this.MouseUp -= mapView_MouseUpForPan;

        this.IsPanning = false;

        this.mapView.ReleaseMouseCapture();

        Point currentMouseLocation = e.GetPosition(this.mapView);

        double xOffset = currentMouseLocation.X - this.startPointLocationForPan.X;

        double yOffset = currentMouseLocation.Y - this.startPointLocationForPan.Y;

        if (Math.Abs(xOffset) > 2 || Math.Abs(yOffset) > 2)
        {
            Refresh(isNewExtent: true);
        }
        else
        {
            //Debug.WriteLine(new StackTrace().GetFrame(0).GetMethod().Name, _methodEscaped);
        }
    }

    private void mapView_MouseMoveForPan(object sender, MouseEventArgs e)
    {
        Point currentMouseLocation = e.GetPosition(this.mapView);

        double xOffset = currentMouseLocation.X - this.prevMouseLocation.X;

        double yOffset = currentMouseLocation.Y - this.prevMouseLocation.Y;

        if (Math.Abs(xOffset) > 2 || Math.Abs(yOffset) > 2)
        {
            this.panTransform.X += xOffset * 1.0 / this.zoomTransform.ScaleX;

            this.panTransform.Y += yOffset * 1.0 / this.zoomTransform.ScaleY;

            this.prevMouseLocation = currentMouseLocation;

            this.panTransformForPoints.X += xOffset;

            this.panTransformForPoints.Y += yOffset;

            UpdateTileInfos();
        }

    }

    private void ExtentManager_OnTilesRemoved(object sender, CustomEventArgs<List<TileInfo>> e)
    {
        lock (locker)
        {
            for (int i = jobs.Count - 1; i >= 0; i--)
            {
                var currentJob = jobs[i];

                if (currentJob == null)
                {
                    this.jobs.Remove(currentJob);

                    continue;
                }

                if (e.Arg.Contains(currentJob.Tag.Tile) && !this.CurrentTileInfos.Contains(currentJob.Tag.Tile))
                {
                    currentJob.Operation.Abort();

                    this.jobs.Remove(currentJob);
                }
            }
        }

        ClearOutOfExtent(true);
    }

    private void ExtentManager_OnTilesAdded(object sender, CustomEventArgs<List<TileInfo>> e)
    {
        // 1401.12.05
        IEnumerable<ILayer> infos = this._layerManager.UpdateAndGetLayers(InverseMapScale, RenderMode.Tiled).ToList();

        foreach (var item in e.Arg)
        {
            if (item.ZoomLevel != this.NearestGoogleZoomLevel)
                continue;

            if (!CurrentTileInfos.Contains(item))
                continue;

            RefreshTiles(infos, item, l => true);
        }
    }

    Path GetTileBorder(TileInfo tile, bool isNew, bool isOld, bool isDefault)
    {
        var p1 = tile.WebMercatorExtent.TopLeft.AsWpfPoint();

        var p2 = tile.WebMercatorExtent.BottomRight.AsWpfPoint();

        RectangleGeometry geometry = new RectangleGeometry(new Rect(p1, p2), 0, 0);

        geometry.Transform = viewTransform;

        SolidColorBrush stroke;

        if (isNew)
        {
            stroke = new SolidColorBrush(Colors.Green);
        }
        else if (isOld)
        {
            stroke = new SolidColorBrush(Colors.Red);
        }
        else if (isDefault)
        {
            stroke = new SolidColorBrush(Colors.Gray);
        }
        else
        {
            throw new NotImplementedException();
        }

        Path path = new Path()
        {
            Data = geometry,
            Fill = new SolidColorBrush(Colors.Transparent),
            StrokeThickness = 1,
            Stroke = stroke,
            Tag = new LayerTag(this.MapScale) { Tile = tile, IsTiled = true, IsNew = true, LayerType = LayerType.GridAndGraticule }

        };

        return path;
    }

    private void ClearTileBorder(TileInfo tile)
    {
        for (int i = this.mapView.Children.Count - 1; i >= 0; i--)
        {
            Shape temp = this.mapView.Children[i] as Shape;

            if (temp != null)
            {
                var tag = temp.Tag as LayerTag;

                if (tag != null && tag.LayerType != LayerType.BaseMap)
                {
                    if (tag.Tile.Equals(tile))
                    {
                        this.mapView.Children.RemoveAt(i);
                    }
                }
            }
        }
    }

    //Consider removeing checking Tag is LayerTag, All childrens should have a LayerTag as Tag
    private List<Path> Find(TileInfo tile)
    {
        List<Path> result = new List<Path>();

        foreach (var item in mapView.Children)
        {
            if (item is Path)
            {
                if ((item as Path).Tag is LayerTag)
                {
                    if (tile.Equals(((item as Path).Tag as LayerTag).Tile))
                    {
                        result.Add(item as Path);
                    }
                }
            }
        }

        return result;
    }

    //It has animation
    public void Pan(double xOffset, double yOffset, Action callback = null)
    {
        ClearLayer(LayerType.AnimatingItem, false);

        counterValue = 4;
        counter = 0;

        if (double.IsNaN(xOffset + yOffset))
            return;

        if (Math.Abs(xOffset) > 2 || Math.Abs(yOffset) > 2)
        {

            DoubleAnimation animation = new DoubleAnimation()
            {
                Duration = new Duration(TimeSpan.FromMilliseconds(100)),
                FillBehavior = FillBehavior.Stop
            };

            animation.Completed += new EventHandler(delegate (object o, EventArgs e)
                                    {
                                        if (++counter != counterValue)
                                            return;

                                        UpdateTileInfos();

                                        Refresh(isNewExtent: true);

                                        if (callback != null)
                                            _ = Dispatcher.BeginInvoke(callback, DispatcherPriority.Background, null);

                                        this.counterValue = -1;
                                    });

            animation.To = this.panTransform.X + xOffset * 1.0 / this.zoomTransform.ScaleX;
            this.panTransform.BeginAnimation(TranslateTransform.XProperty, animation);

            animation.To = this.panTransform.Y + yOffset * 1.0 / this.zoomTransform.ScaleY;
            this.panTransform.BeginAnimation(TranslateTransform.YProperty, animation);

            animation.To = this.panTransformForPoints.X + xOffset;
            this.panTransformForPoints.BeginAnimation(TranslateTransform.XProperty, animation);

            animation.To = this.panTransformForPoints.Y + yOffset;
            this.panTransformForPoints.BeginAnimation(TranslateTransform.YProperty, animation);

            this.panTransform.X += xOffset * 1.0 / this.zoomTransform.ScaleX;
            this.panTransform.Y += yOffset * 1.0 / this.zoomTransform.ScaleY;
            this.panTransformForPoints.X = this.panTransformForPoints.X + xOffset;
            this.panTransformForPoints.Y = this.panTransformForPoints.Y + yOffset;
        }
        else
        {
            if (callback != null)
            {
                Dispatcher.BeginInvoke(callback, System.Windows.Threading.DispatcherPriority.Background, null);
            }
        }
    }

    public void PanTo(double x, double y, Action callback)
    {
        double screenY = this.mapView.ActualHeight / 2.0;

        double screenX = this.mapView.ActualWidth / 2.0;

        Point center = MapToScreen(new Point(x, y));

        this.Pan(screenX - center.X, screenY - center.Y, callback);
    }

    #endregion


    #region Zoom

    Point firstZoomBound;

    int counter; int counterValue;

    //POTENTIALLY ERROR PROUNE; Tag value should not be `string`
    Rectangle rectangle = new Rectangle()
    {
        Stroke = new SolidColorBrush(new Color() { R = 255, G = 200, B = 0, A = 255 }),
        StrokeThickness = 2,
        Fill = new SolidColorBrush(new Color() { R = 255, G = 240, B = 0, A = 40 }),
        Tag = new LayerTag(-1) { IsTiled = false, LayerType = LayerType.Drawing }
    };

    public void EnableZoomOnDoubleClick()
    {
        this.mapView.MouseDown -= MapView_MouseDownForDoubleClickZoom;
        this.mapView.MouseDown += MapView_MouseDownForDoubleClickZoom;
    }

    public void DisableZoomOnDoubleClick()
    {
        this.mapView.MouseDown -= MapView_MouseDownForDoubleClickZoom;
    }

    public void EnableZoomingOnMouseWheel()
    {
        this.mapView.MouseWheel -= mapView_MouseWheel;
        this.mapView.MouseWheel += mapView_MouseWheel;
    }

    public void DisableZoomingOnMouseWheel()
    {
        this.mapView.MouseWheel -= mapView_MouseWheel;
    }


    public void FullExtent()
    {
        ZoomToExtent(this._layerManager.CalculateMapExtent(), false);
    }

    public void ZoomIn()
    {
        ResetMapViewEvents();

        this.mapView.MouseDown -= mapView_MouseDownForZoom;
        this.mapView.MouseDown += mapView_MouseDownForZoom;

        this.CurrentMouseAction = MapAction.ZoomInRectangle;
    }

    public void ZoomOutPoint()
    {
        ResetMapViewEvents();

        this.mapView.MouseUp -= mapView_MouseDownForZoomOut;
        this.mapView.MouseUp += mapView_MouseDownForZoomOut;

        this.CurrentMouseAction = MapAction.ZoomOut;
    }


    private void MapView_MouseDownForDoubleClickZoom(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount > 1)
            ZoomWheelAtWindowPoint(true, e.GetPosition(this.mapView));
    }

    private void mapView_MouseDownForZoom(object sender, MouseButtonEventArgs e)
    {
        if (this.viewTransform == null)
            return;

        Mouse.Capture(this.mapView);

        this.firstZoomBound = e.GetPosition(this.mapView);

        rectangle.Width = 0; rectangle.Height = 0;

        rectangle.Fill.Freeze();

        this.mapView.Children.Add(rectangle);

        Canvas.SetZIndex(rectangle, int.MaxValue);

        this.mapView.MouseUp -= mapView_MouseUpForZoom;
        this.mapView.MouseUp += mapView_MouseUpForZoom;

        this.mapView.MouseMove -= mapView_MouseMoveForZoom;
        this.mapView.MouseMove += mapView_MouseMoveForZoom;
    }

    private void mapView_MouseMoveForZoom(object sender, MouseEventArgs e)
    {
        // in order to let the right click options on map work
        if (e.LeftButton != MouseButtonState.Pressed)
            return;

        Point currMouseLocation = e.GetPosition(this.mapView);

        double xOffset = currMouseLocation.X - this.prevMouseLocation.X;

        double yOffset = currMouseLocation.Y - this.prevMouseLocation.Y;

        Rect rect = new Rect(this.firstZoomBound, currMouseLocation);

        rectangle.Width = rect.Width; rectangle.Height = rect.Height;

        Canvas.SetTop(rectangle, rect.Top);

        Canvas.SetLeft(rectangle, rect.Left);
    }

    private void mapView_MouseUpForZoom(object sender, MouseButtonEventArgs e)
    {
        this.mapView.MouseUp -= mapView_MouseUpForZoom;
        this.mapView.MouseMove -= mapView_MouseMoveForZoom;

        this.mapView.Children.Remove(rectangle);

        Point currMouseLocation = e.GetPosition(this.mapView);

        Rect rect = new Rect(ScreenToMap(firstZoomBound), ScreenToMap(currMouseLocation));

        var boundingBox = sb.BoundingBox.Create(ScreenToMap(firstZoomBound).AsPoint(), ScreenToMap(currMouseLocation).AsPoint());

        this.mapView.ReleaseMouseCapture();

        // in order to let the right click options on map work
        if (e.ChangedButton != MouseButton.Left)
            return;

        ZoomToExtent(boundingBox, true);
    }

    private void mapView_MouseDownForZoomOut(object sender, MouseButtonEventArgs e)
    {
        Point canvasPosition = e.GetPosition(this.mapView);

        ZoomToPoint(canvasPosition, .75);
    }

    private void mapView_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        ZoomWheelAtWindowPoint(e.Delta > 0, e.GetPosition(this.mapView));
    }

    public void Zoom(double mapScale)
    {
        this.ZoomAndCenter(mapScale, CurrentExtent.Center);
    }

    public void ZoomAndCenterToGoogleZoomLevel(int googleZoomLevel, sb.Point mapCenter, Action callback, bool withAnimation = true)
    {
        //var wgs84Center = MapProjects.WebMercatorToGeodeticWgs84(mapCenter);

        var mapScale = WebMercatorUtility.GetGoogleMapScale(CheckGoogleZoomLevel(googleZoomLevel)/*, wgs84Center?.Y*/);

        ZoomAndCenter(mapScale, mapCenter, callback, withAnimation);
    }

    public void ZoomAndCenter(double mapScale, sb.Point mapCenter, Action? callback = null, bool withAnimation = true)
    {
        double scale = ToScreenScale(mapScale);

        double width = this.mapView.ActualWidth / scale;

        double height = this.mapView.ActualHeight / scale;

        sb.BoundingBox boundingBox =
            new sb.BoundingBox(mapCenter.X - width / 2.0, mapCenter.Y - height / 2.0, mapCenter.X + width / 2.0, mapCenter.Y + height / 2.0);

        ZoomToExtent(boundingBox, false, true, true, callback, withAnimation);
    }

    private void ZoomToFeature(Geometry<sb.Point> geometry)
    {
        if (geometry.IsNullOrEmpty())
            return;

        if (geometry.Type == GeometryType.Point)
        {
            //98.01.18. consider using
            this.ZoomAndCenter(WebMercatorUtility.GetGoogleMapScale(13), geometry.AsPoint());
        }
        else
        {
            this.ZoomToExtent(geometry.GetBoundingBox());
        }
    }

    private const string GeometryComparisonLayerName = "__FeatureChanges_GeometryComparison";

    private void ShowFeatureChangesDialog(IRI.Maptor.Sta.Spatial.Primitives.Feature<sb.Point> feature, List<sb.Field>? fields)
    {
        var vm = new IRI.Maptor.Jab.Common.ViewModels.Map.FeatureChangesViewModel(feature, fields);
        vm.RequestZoomToFeature = g => this.ZoomToFeature(g);
        vm.RequestShowGeometryComparison = (oldGeo, newGeo) => _presenter.RequestShowGeometryComparison?.Invoke(oldGeo, newGeo);
        vm.RequestZoomToExtent = (bbox, isExact, isNew, callback) => this.ZoomToExtent(bbox, false, isExact, isNew, callback);

        var dialog = new Views.Dialogs.FeatureChangesDialogView
        {
            Owner = Window.GetWindow(this),
            DataContext = vm
        };
        dialog.Closed += (s, e) => _presenter.RequestClearGeometryComparison?.Invoke();
        dialog.ShowDialog();
    }

    private async void ShowGeometryComparison(Geometry<sb.Point>? oldGeometry, Geometry<sb.Point>? newGeometry)
    {
        ClearGeometryComparison();

        var boxes = new List<sb.BoundingBox>();
        if (oldGeometry != null && !oldGeometry.IsNullOrEmpty())
        {
            var oldGeo = oldGeometry.Srid != SridHelper.WebMercator
                ? oldGeometry.Project(SrsBases.WebMercator)
                : oldGeometry;
            var oldParams = new VisualParameters(null, new SolidColorBrush(Colors.Gray), 3, 0.8)
            { DashStyle = new System.Windows.Media.DashStyle([4, 4], 0) };
            await DrawGeometriesAsync(GeometryComparisonLayerName + "_Old", [oldGeo], oldParams);
            boxes.Add(oldGeo.GetBoundingBox());
        }
        if (newGeometry != null && !newGeometry.IsNullOrEmpty())
        {
            var newGeo = newGeometry.Srid != SridHelper.WebMercator
                ? newGeometry.Project(SrsBases.WebMercator)
                : newGeometry;
            var newParams = VisualParameters.GetDefaultForHighlight(3);
            await DrawGeometriesAsync(GeometryComparisonLayerName + "_New", [newGeo], newParams);
            boxes.Add(newGeo.GetBoundingBox());
        }
        if (boxes.Count > 0)
        {
            var merged = sb.BoundingBox.GetMergedBoundingBox(boxes);
            this.ZoomToExtent(merged);
        }
    }

    private void ClearGeometryComparison()
    {
        ClearLayer(GeometryComparisonLayerName + "_Old", true);
        ClearLayer(GeometryComparisonLayerName + "_New", true);
    }

    public void ZoomToExtent(sb.BoundingBox boundingBox)
    {
        ZoomToExtent(boundingBox, false, true);
    }

    /// <summary>
    /// Wheel-style zoom step at a window (pixel) point — same behavior as mouse wheel.
    /// </summary>
    public void ZoomAtWindowPoint(bool zoomIn, Point windowPoint)
    {
        ZoomWheelAtWindowPoint(zoomIn, windowPoint);
    }

    /// <summary>
    /// Wheel-style zoom at the center of the map view.
    /// </summary>
    public void ZoomAtViewCenter(bool zoomIn)
    {
        if (this.mapView.ActualWidth <= 0 || this.mapView.ActualHeight <= 0)
            return;

        ZoomWheelAtWindowPoint(zoomIn, new Point(this.mapView.ActualWidth / 2.0, this.mapView.ActualHeight / 2.0));
    }

    private void ZoomWheelAtWindowPoint(bool zoomIn, Point windowCenter)
    {
        var mapCenter = ScreenToMap(windowCenter);

        if (_presenter.MapSettings.IsGoogleZoomLevelsEnabled)
        {
            int newZoomLevel = zoomIn ? WebMercatorUtility.GetNextZoomLevel(NearestGoogleZoomLevel) : WebMercatorUtility.GetPreviousZoomLevel(NearestGoogleZoomLevel);

            newZoomLevel = CheckGoogleZoomLevel(newZoomLevel);

            //98.01.18. consider using
            this.ZoomAndCenter(WebMercatorUtility.GetGoogleMapScale(newZoomLevel), mapCenter.AsPoint());
        }
        else
        {
            double delta = zoomIn ? 1.5 : 0.5;

            double newMapScale = ToMapScale(this.ScreenScale * delta);

            this.ZoomAndCenter(newMapScale, mapCenter.AsPoint());
        }
    }

    //It has animation
    private async void ZoomToExtent(sb.BoundingBox mapBoundingBox, bool canChangeToPointZoom, bool isExactExtent = true, bool isNewExtent = true, Action? callback = null, bool withAnimation = true)
    {
        if (mapBoundingBox.IsNaN()/* double.IsNaN(mapBoundingBox.Width + mapBoundingBox.Height)*/)
            return;

        var mapCenter = mapBoundingBox.Center;

        if (mapBoundingBox.Width + mapBoundingBox.Height < minBoundingBoxSize)
        {
            int newZoomLevel = WebMercatorUtility.GetNextZoomLevel(NearestGoogleZoomLevel);

            var wgs84Center = MapProjects.WebMercatorToGeodeticWgs84(mapCenter);

            this.ZoomAndCenter(WebMercatorUtility.GetGoogleMapScale(newZoomLevel, wgs84Center.Y), mapCenter, callback);

            return;
        }

        ClearLayer(LayerType.AnimatingItem, false);

        ClearLayer(LayerType.Complex, false);

        ClearLayer(LayerType.EditableItem, false);

        counter = 0;

        if (mapBoundingBox.GetDiagonalLength() < 15 && canChangeToPointZoom)
        {
            ZoomToPoint(MapToScreen(mapBoundingBox.TopLeft.AsWpfPoint()), 1.25);

            return;
        }

        counterValue = 8;

        //Point intermediateExtentCenter = new Point((mapBoundingBox.Left + mapBoundingBox.Right) / 2.0,
        //                                            (mapBoundingBox.Top + mapBoundingBox.Bottom) / 2.0);
        var intermediateExtentCenter = mapCenter.AsWpfPoint();

        Point windowCenter = new Point(this.mapView.ActualWidth / 2.0, this.mapView.ActualHeight / 2.0);

        Point screenExtentCenter = MapToScreen(intermediateExtentCenter);

        double scale = double.NaN;

        //if (IsGoogleZoomLevelsEnabled)
        //{
        //    var newZoomLevel = WebMercatorUtility.EstimateZoomLevel(mapBoundingBox, this.mapView.ActualWidth, this.mapView.ActualHeight);

        //    var mapScale = WebMercatorUtility.GetGoogleMapScale(newZoomLevel);

        //    scale = ToScreenScale(mapScale);
        //}
        //else
        //{
        double xScale = (isExactExtent ? this.mapView.ActualWidth : this.mapView.ActualWidth - 20) / mapBoundingBox.Width;

        double yScale = (isExactExtent ? this.mapView.ActualHeight : this.mapView.ActualHeight - 20) / mapBoundingBox.Height;

        scale = xScale > yScale ? yScale : xScale;
        //}

        if (double.IsNaN(scale))
            return;

        double pointScale = ToMapScale(scale) / this.MapScale;

        if (withAnimation)
        {
            try
            {
                var duration = new Duration(TimeSpan.FromMilliseconds(100));

                var fillBehavior = FillBehavior.Stop;

                DoubleAnimation animationPanX = new DoubleAnimation(windowCenter.X - intermediateExtentCenter.X, duration, fillBehavior);
                var t1 = AnimateAsync(() => { this.panTransform.BeginAnimation(TranslateTransform.XProperty, animationPanX); }, animationPanX);

                DoubleAnimation animationPanY = new DoubleAnimation(windowCenter.Y - intermediateExtentCenter.Y, duration, fillBehavior);
                var t2 = AnimateAsync(() => { this.panTransform.BeginAnimation(TranslateTransform.YProperty, animationPanY); }, animationPanY);

                DoubleAnimation animationPanPX = new DoubleAnimation(windowCenter.X - screenExtentCenter.X, duration, fillBehavior);
                var t3 = AnimateAsync(() => { this.panTransformForPoints.BeginAnimation(TranslateTransform.XProperty, animationPanPX); }, animationPanPX);

                DoubleAnimation animationPanPY = new DoubleAnimation(windowCenter.Y - screenExtentCenter.Y, duration, fillBehavior);
                var t4 = AnimateAsync(() => { this.panTransformForPoints.BeginAnimation(TranslateTransform.YProperty, animationPanPY); }, animationPanPY);


                DoubleAnimation animationZoomX = new DoubleAnimation(this.mapView.ActualWidth / 2.0, duration, fillBehavior);
                var t5 = AnimateAsync(() => { this.zoomTransform.BeginAnimation(ScaleTransform.CenterXProperty, animationZoomX); }, animationZoomX);

                DoubleAnimation animationZoomY = new DoubleAnimation(this.mapView.ActualHeight / 2.0, duration, fillBehavior);
                var t6 = AnimateAsync(() => { this.zoomTransform.BeginAnimation(ScaleTransform.CenterYProperty, animationZoomY); }, animationZoomY);

                DoubleAnimation animationZoomSX = new DoubleAnimation(scale * baseScaleX, duration, fillBehavior);
                var t7 = AnimateAsync(() => { this.zoomTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animationZoomSX); }, animationZoomSX);

                DoubleAnimation animationZoomSY = new DoubleAnimation(scale * baseScaleY, duration, fillBehavior);
                var t8 = AnimateAsync(() => { this.zoomTransform.BeginAnimation(ScaleTransform.ScaleYProperty, animationZoomSY); }, animationZoomSY);

                await Task.WhenAll(t1, t2, t3, t4, t5, t6, t7, t8);

            }
            catch (Exception ex)
            {
                throw;
            }
        }

        this.panTransform.X = windowCenter.X - intermediateExtentCenter.X;
        this.panTransform.Y = windowCenter.Y - intermediateExtentCenter.Y;

        this.panTransformForPoints.X = 0;
        this.panTransformForPoints.Y = 0;

        this.zoomTransform.CenterX = this.mapView.ActualWidth / 2.0;

        this.zoomTransform.CenterY = this.mapView.ActualHeight / 2.0;

        this.zoomTransform.ScaleX = scale * baseScaleX;

        this.zoomTransform.ScaleY = scale * baseScaleY;

        this._theScreenScale = scale * baseScaleX;

        this.OnZoomChanged?.Invoke(null, ZoomEventArgs.EmptyArg);

        Refresh(isNewExtent);

        if (callback != null)
        {
            await Dispatcher.BeginInvoke(callback, DispatcherPriority.Background, null);
        }


    }

    private void ZoomToPoint(Point windowPoint, double deltaZoom)
    {
        Point layerPoint = this.viewTransform.Inverse.Transform(windowPoint);

        this.panTransform.X = this.mapView.ActualWidth / 2.0 - layerPoint.X;

        this.panTransform.Y = this.mapView.ActualHeight / 2.0 - layerPoint.Y;

        //94.09.24: zoomTranform.ScaleX value may be at an animation!
        //double scale = this.zoomTransform.ScaleX * deltaZoom;
        double scale = this._theScreenScale * deltaZoom;
        //
        this.zoomTransform.CenterX = this.mapView.ActualWidth / 2.0;

        this.zoomTransform.CenterY = this.mapView.ActualHeight / 2.0;

        this.zoomTransform.ScaleX = scale * baseScaleX;

        this.zoomTransform.ScaleY = scale * baseScaleY;

        this._theScreenScale = scale * baseScaleX;

        this.OnZoomChanged?.Invoke(null, ZoomEventArgs.EmptyArg);

        Refresh(isNewExtent: true);
    }



    private Task AnimateAsync(Action action, DoubleAnimation animation)
    {
        TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

        animation.Completed += (s, e) => { tcs.SetResult(true); };

        action();

        return tcs.Task;
    }

    private int CheckGoogleZoomLevel(int googleZoomLevel)
    {
        return Math.Clamp(googleZoomLevel, _presenter.MapSettings.MinGoogleZoomLevel, _presenter.MapSettings.MaxGoogleZoomLevel);

        //if (googleZoomLevel < _presenter.MapSettings.MinGoogleZoomLevel)
        //{
        //    return _presenter.MapSettings.MinGoogleZoomLevel;
        //}
        //else if (googleZoomLevel > _presenter.MapSettings.MaxGoogleZoomLevel)
        //{
        //    return _presenter.MapSettings.MaxGoogleZoomLevel;
        //}

        //return googleZoomLevel;
    }

    #endregion


    #region Draw Shapes

    bool displayDrawingPath = false;

    DrawingLayer drawingLayer;

    EditableFeatureLayerOptions drawingOptions = new EditableFeatureLayerOptions();

    DrawMode drawMode;

    Action<Point> onMoveForDrawAction = null;

    bool itWasPanningWhileDrawing { get; set; }

    bool itWasPanningWhileStartDrawing { get; set; }

    bool itWasPanningWhileStartNewPart { get; set; }

    CancellationTokenSource? drawingCancellationToken;

    TaskCompletionSource<Response<Geometry<sb.Point>>> drawingTcs;



    // Rectangle used for drag-based rectangle drawing (DrawMode.Rectangle)
    Rectangle drawingRectangle;

    Point rectangleFirstScreenPoint;

    sb.Point rectangleFirstMapPoint;


    private void MapView_MouseDownForPanWhileStartDrawing(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
            return;

        e.Handled = true;

        itWasPanningWhileStartDrawing = false;

        this.prevMouseLocation = e.GetPosition(this.mapView);

        //if (e.ChangedButton != MouseButton.Left)
        //    return;

        //this.prevMouseLocation = e.GetPosition(this.mapView);

        //var webMercatorPoint = ScreenToMap(this.prevMouseLocation).AsPoint();

        //AddFirstPointForNewDrawing(webMercatorPoint);
    }

    private void MapView_MouseMoveForPanWhileStartDrawing(object sender, MouseEventArgs e)
    {
        Point currentMouseLocation = e.GetPosition(this.mapView);

        if (e.LeftButton == MouseButtonState.Pressed)
        {
            double xOffset = currentMouseLocation.X - this.prevMouseLocation.X;

            double yOffset = currentMouseLocation.Y - this.prevMouseLocation.Y;

            if (Math.Abs(xOffset) > _knownAsPanThreshold || Math.Abs(yOffset) > _knownAsPanThreshold)
            {
                this.panTransform.X += xOffset * 1.0 / this.zoomTransform.ScaleX;

                this.panTransform.Y += yOffset * 1.0 / this.zoomTransform.ScaleY;

                this.prevMouseLocation = currentMouseLocation;

                this.panTransformForPoints.X += xOffset;

                this.panTransformForPoints.Y += yOffset;

                UpdateTileInfos();

                this.itWasPanningWhileStartDrawing = true;
            }
            else { }
        }
        else
        {

        }
    }

    private void MapView_MouseUpForPanWhileStartDrawing(object sender, MouseButtonEventArgs e)
    {
        if (itWasPanningWhileStartDrawing)
        {
            this.ResetMapViewEvents();

            this.Refresh(isNewExtent: true);

            this.mapView.MouseMove -= MapView_MouseMoveForPanWhileStartDrawing;
            this.mapView.MouseMove += MapView_MouseMoveForPanWhileStartDrawing;

            this.mapView.MouseDown -= MapView_MouseDownForPanWhileStartDrawing;
            this.mapView.MouseDown += MapView_MouseDownForPanWhileStartDrawing;

            this.mapView.MouseUp -= MapView_MouseUpForPanWhileStartDrawing;
            this.mapView.MouseUp += MapView_MouseUpForPanWhileStartDrawing;

            itWasPanningWhileStartDrawing = false;

            this.SetCursor(Cursors.Cross);

            return;
        }
        else
        {
            if (e.ChangedButton != MouseButton.Left)
                return;

            this.prevMouseLocation = e.GetPosition(this.mapView);

            var webMercatorPoint = ScreenToMap(this.prevMouseLocation).AsPoint();

            AddFirstPointForNewDrawing(webMercatorPoint);
        }
    }


    private void AddFirstPointForNewDrawing(sb.Point webMercatorPoint)
    {
        if (true)
        {
            if (webMercatorPoint.IsNaN())
                return;
        }

        this.mapView.MouseDown -= MapView_MouseDownForPanWhileStartDrawing;
        this.mapView.MouseUp -= MapView_MouseUpForPanWhileStartDrawing;
        this.mapView.MouseMove -= MapView_MouseMoveForPanWhileStartDrawing;

        this.ClearLayer(drawingLayer, remove: true, forceRemove: true);

        this.drawingLayer = new DrawingLayer(this.drawMode, this.viewTransform, ScreenToMap, webMercatorPoint, drawingOptions);

        this.drawingLayer.OnRequestFinishDrawing += (s, arg) =>
        {
            FinishDrawing();
        };

        this.drawingLayer.RequestCancelDrawing = () => this.CancelDrawing();

        this.SetLayer(drawingLayer);

        this.AddEditableFeatureLayer(drawingLayer.GetLayer());

        if (this.drawMode == DrawMode.Point)
        {
            FinishDrawing();
        }
        else
        {
            //this.mapView.CaptureMouse();

            this.mapView.MouseMove -= MapView_MouseMoveForPanWhileDrawing;
            this.mapView.MouseMove += MapView_MouseMoveForPanWhileDrawing;

            this.mapView.MouseDown -= MapView_MouseDownForPanWhileDrawing;
            this.mapView.MouseDown += MapView_MouseDownForPanWhileDrawing;

            this.mapView.MouseUp -= MapView_MouseUpForForPanWhileDrawing;
            this.mapView.MouseUp += MapView_MouseUpForForPanWhileDrawing;

            //this.mapView.MouseMove -= MapView_MouseMoveForDrawing;
            //this.mapView.MouseMove += MapView_MouseMoveForDrawing;
        }
    }

    private void MapView_MouseDownForPanWhileStartNewPart(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
            return;

        e.Handled = true;

        itWasPanningWhileStartNewPart = false;

        this.prevMouseLocation = e.GetPosition(this.mapView);

    }

    private void MapView_MouseUpForPanWhileStartNewPart(object sender, MouseButtonEventArgs e)
    {
        if (itWasPanningWhileStartNewPart)
        {
            this.ResetMapViewEvents();

            this.Refresh(isNewExtent: true);

            this.mapView.MouseMove -= MapView_MouseMoveForPanWhileStartNewPart;
            this.mapView.MouseMove += MapView_MouseMoveForPanWhileStartNewPart;

            this.mapView.MouseDown -= MapView_MouseDownForPanWhileStartNewPart;
            this.mapView.MouseDown += MapView_MouseDownForPanWhileStartNewPart;

            this.mapView.MouseUp -= MapView_MouseUpForPanWhileStartNewPart;
            this.mapView.MouseUp += MapView_MouseUpForPanWhileStartNewPart;

            itWasPanningWhileStartNewPart = false;

            this.SetCursor(Cursors.Cross);

            return;
        }
        else
        {
            if (e.ChangedButton != MouseButton.Left)
                return;

            this.prevMouseLocation = (e.GetPosition(this.mapView));

            var webMercatorPoint = ScreenToMap(this.prevMouseLocation).AsPoint();

            this.mapView.MouseDown -= MapView_MouseDownForPanWhileStartNewPart;
            this.mapView.MouseUp -= MapView_MouseUpForPanWhileStartNewPart;
            this.mapView.MouseMove -= MapView_MouseMoveForPanWhileStartNewPart;

            if (this.drawMode == DrawMode.Point)
            {
                FinishDrawing();
            }
            else
            {
                this.drawingLayer.StartNewPart(webMercatorPoint);

                this.mapView.MouseMove -= MapView_MouseMoveForPanWhileDrawing;
                this.mapView.MouseMove += MapView_MouseMoveForPanWhileDrawing;

                this.mapView.MouseDown -= MapView_MouseDownForPanWhileDrawing;
                this.mapView.MouseDown += MapView_MouseDownForPanWhileDrawing;

                this.mapView.MouseUp -= MapView_MouseUpForForPanWhileDrawing;
                this.mapView.MouseUp += MapView_MouseUpForForPanWhileDrawing;

                //this.mapView.MouseMove -= MapView_MouseMoveForDrawing;
                //this.mapView.MouseMove += MapView_MouseMoveForDrawing;
            }
        }
    }

    private void MapView_MouseMoveForPanWhileStartNewPart(object sender, MouseEventArgs e)
    {
        Point currentMouseLocation = e.GetPosition(this.mapView);

        if (e.LeftButton == MouseButtonState.Pressed)
        {
            double xOffset = currentMouseLocation.X - this.prevMouseLocation.X;

            double yOffset = currentMouseLocation.Y - this.prevMouseLocation.Y;

            if (Math.Abs(xOffset) > _knownAsPanThreshold || Math.Abs(yOffset) > _knownAsPanThreshold)
            {
                this.panTransform.X += xOffset * 1.0 / this.zoomTransform.ScaleX;

                this.panTransform.Y += yOffset * 1.0 / this.zoomTransform.ScaleY;

                this.prevMouseLocation = currentMouseLocation;

                this.panTransformForPoints.X += xOffset;

                this.panTransformForPoints.Y += yOffset;

                UpdateTileInfos();

                this.itWasPanningWhileStartNewPart = true;
            }
            else { }
        }
        else
        {

        }
    }

    private void AddPointToNewDrawing(sb.Point webMercatorPoint)
    {
        if (drawingLayer == null)
        {
            AddFirstPointForNewDrawing(webMercatorPoint);
        }
        else
        {
            DoMoveForStartDrawing(webMercatorPoint);

            AddPointForNewDrawing(webMercatorPoint);
        }
    }

    private void FinishDrawingPart()
    {
        if (drawingLayer != null && drawingLayer.TryFinishDrawingPart())
        {
            this.mapView.MouseUp -= MapView_MouseUpForForPanWhileDrawing;
            //this.mapView.MouseDown -= MapView_MouseDownForStartDrawing;
            this.mapView.MouseDown -= MapView_MouseDownForPanWhileStartDrawing;
            this.mapView.MouseUp -= MapView_MouseUpForPanWhileStartDrawing;
            this.mapView.MouseMove -= MapView_MouseMoveForPanWhileStartDrawing;

            this.mapView.MouseMove -= MapView_MouseMoveForPanWhileDrawing;
            this.mapView.MouseDown -= MapView_MouseDownForPanWhileDrawing;


            //1399.08.22
            //this.mapView.MouseDown -= MapView_MouseDownForStartNewPart;
            //this.mapView.MouseDown += MapView_MouseDownForStartNewPart;

            this.mapView.MouseMove -= MapView_MouseMoveForPanWhileStartNewPart;
            this.mapView.MouseMove += MapView_MouseMoveForPanWhileStartNewPart;

            this.mapView.MouseDown -= MapView_MouseDownForPanWhileStartNewPart;
            this.mapView.MouseDown += MapView_MouseDownForPanWhileStartNewPart;

            this.mapView.MouseUp -= MapView_MouseUpForPanWhileStartNewPart;
            this.mapView.MouseUp += MapView_MouseUpForPanWhileStartNewPart;

            //this.mapView.MouseMove -= MapView_MouseMoveForDrawing;
            //this.mapView.MouseMove += MapView_MouseMoveForDrawing;

        }
    }

    private void FinishDrawing()
    {
        if (drawingLayer?.GetFinalGeometry()?.IsValid() == true)
        {

            this.mapView.MouseUp -= MapView_MouseUpForForPanWhileDrawing;

            //this.mapView.MouseDown -= MapView_MouseDownForStartDrawing;
            this.mapView.MouseDown -= MapView_MouseDownForPanWhileStartDrawing;
            this.mapView.MouseUp -= MapView_MouseUpForPanWhileStartDrawing;
            this.mapView.MouseMove -= MapView_MouseMoveForPanWhileStartDrawing;

            this.mapView.MouseMove -= MapView_MouseMoveForPanWhileDrawing;
            this.mapView.MouseDown -= MapView_MouseDownForPanWhileDrawing;

            //1399.08.22
            //this.mapView.MouseDown -= MapView_MouseDownForStartNewPart;
            this.mapView.MouseMove -= MapView_MouseMoveForPanWhileStartNewPart;
            this.mapView.MouseDown -= MapView_MouseDownForPanWhileStartNewPart;
            this.mapView.MouseUp -= MapView_MouseUpForPanWhileStartNewPart;

            this.mapView.MouseMove -= MapView_MouseMoveForDrawing;

            ResetMapViewEvents();

            this.ClearLayer(drawingLayer, remove: true, forceRemove: true);

            this.RemoveEditableFeatureLayer(drawingLayer.GetLayer());

            drawingCancellationToken = null;

            drawingTcs.SetResult(ResponseFactory.Create(drawingLayer.GetFinalGeometry()));

            this.Pan();
        }
    }

    private void MapView_MouseMoveForDrawing(object sender, MouseEventArgs e)
    {
        this.CurrentEditingPoint = ScreenToMap(e.GetPosition(this.mapView));
    }

    private void MapView_MouseMoveForPanWhileDrawing(object sender, MouseEventArgs e)
    {
        Point currentMouseLocation = e.GetPosition(this.mapView);

        if (e.LeftButton == MouseButtonState.Pressed)
        {
            double xOffset = currentMouseLocation.X - this.prevMouseLocation.X;

            double yOffset = currentMouseLocation.Y - this.prevMouseLocation.Y;

            if (Math.Abs(xOffset) > _knownAsPanThreshold || Math.Abs(yOffset) > _knownAsPanThreshold)
            {
                this.panTransform.X += xOffset * 1.0 / this.zoomTransform.ScaleX;

                this.panTransform.Y += yOffset * 1.0 / this.zoomTransform.ScaleY;

                this.prevMouseLocation = currentMouseLocation;

                this.panTransformForPoints.X += xOffset;

                this.panTransformForPoints.Y += yOffset;

                UpdateTileInfos();

                this.itWasPanningWhileDrawing = true;
            }
            else { }
        }
        else
        {
            var mapLocation = ScreenToMap(currentMouseLocation);

            this.drawingLayer.UpdateLastVertexLocation(mapLocation.AsPoint());

            onMoveForDrawAction?.Invoke(mapLocation);
        }
    }

    private void MapView_MouseDownForPanWhileDrawing(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
            return;

        e.Handled = true;

        itWasPanningWhileDrawing = false;

        this.prevMouseLocation = e.GetPosition(this.mapView);
    }


    private void MapView_MouseDownForRectangleDrawing(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
            return;

        if (this.viewTransform == null)
            return;

        e.Handled = true;

        this.rectangleFirstScreenPoint = e.GetPosition(this.mapView);

        this.rectangleFirstMapPoint = ScreenToMap(this.rectangleFirstScreenPoint).AsPoint();

        this.drawingRectangle = new Rectangle()
        {
            Stroke = drawingOptions.Visual.Stroke,
            StrokeThickness = drawingOptions.Visual.StrokeThickness,
            Fill = drawingOptions.Visual.Fill,
            Tag = new LayerTag(-1) { IsTiled = false, LayerType = LayerType.Drawing }
        };

        if (this.rectangleFirstMapPoint.IsNaN())
            return;

        Mouse.Capture(this.mapView);

        drawingRectangle.Width = 0;
        drawingRectangle.Height = 0;

        if (!this.mapView.Children.Contains(drawingRectangle))
        {
            this.mapView.Children.Add(drawingRectangle);
            Canvas.SetZIndex(drawingRectangle, int.MaxValue);
        }
    }

    private void MapView_MouseMoveForRectangleDrawing(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
            return;

        Point currentMouseLocation = e.GetPosition(this.mapView);

        Rect rect = new Rect(this.rectangleFirstScreenPoint, currentMouseLocation);

        drawingRectangle.Width = rect.Width;
        drawingRectangle.Height = rect.Height;

        Canvas.SetTop(drawingRectangle, rect.Top);
        Canvas.SetLeft(drawingRectangle, rect.Left);
    }

    private void MapView_MouseUpForRectangleDrawing(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
            return;

        this.mapView.MouseDown -= MapView_MouseDownForRectangleDrawing;
        this.mapView.MouseMove -= MapView_MouseMoveForRectangleDrawing;
        this.mapView.MouseUp -= MapView_MouseUpForRectangleDrawing;

        this.mapView.Children.Remove(drawingRectangle);

        this.mapView.ReleaseMouseCapture();

        Point secondScreenPoint = e.GetPosition(this.mapView);

        var secondMapPoint = ScreenToMap(secondScreenPoint).AsPoint();

        if (secondMapPoint.IsNaN())
        {
            drawingTcs.TrySetCanceled();
            return;
        }

        var bbox = sb.BoundingBox.Create(this.rectangleFirstMapPoint, secondMapPoint);

        if (bbox.Width == 0 || bbox.Height == 0)
        {
            drawingTcs.TrySetCanceled();
            return;
        }

        var p1 = bbox.TopLeft;
        var p2 = bbox.TopRight;
        var p3 = bbox.BottomRight;
        var p4 = bbox.BottomLeft;

        var ringPoints = new List<sb.Point> { p1, p2, p3, p4 };

        var polygon = Geometry<sb.Point>.CreatePolygon(ringPoints, SridHelper.WebMercator);

        drawingCancellationToken = null;

        drawingTcs.SetResult(ResponseFactory.Create(polygon));

        this.Pan();
    }

    private void MapView_MouseUpForForPanWhileDrawing(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
            return;

        //e.Handled = true;

        this.prevMouseLocation = e.GetPosition(this.mapView);

        var webMercatorPoint = ScreenToMap(this.prevMouseLocation).AsPoint();

        AddPointForNewDrawing(webMercatorPoint);
    }

    private void DoMoveForStartDrawing(sb.Point webMercatorPoint)
    {
        this.drawingLayer.UpdateLastVertexLocation(webMercatorPoint);

        onMoveForDrawAction?.Invoke(webMercatorPoint.AsWpfPoint());
    }

    private void AddPointForNewDrawing(sb.Point webMercatorPoint)
    {
        if (itWasPanningWhileDrawing)
        {
            //this.mapView.ReleaseMouseCapture();

            this.ResetMapViewEvents();

            this.Refresh(isNewExtent: true);

            this.drawingLayer.AddSemiVertex(webMercatorPoint);

            //this.mapView.CaptureMouse();

            this.mapView.MouseMove -= MapView_MouseMoveForPanWhileDrawing;
            this.mapView.MouseMove += MapView_MouseMoveForPanWhileDrawing;

            this.mapView.MouseDown -= MapView_MouseDownForPanWhileDrawing;
            this.mapView.MouseDown += MapView_MouseDownForPanWhileDrawing;

            this.mapView.MouseUp -= MapView_MouseUpForForPanWhileDrawing;
            this.mapView.MouseUp += MapView_MouseUpForForPanWhileDrawing;

            this.mapView.MouseMove -= MapView_MouseMoveForDrawing;
            this.mapView.MouseMove += MapView_MouseMoveForDrawing;

            itWasPanningWhileDrawing = false;

            return;
        }

        this.drawingLayer.AddVertex(webMercatorPoint);

        this.drawingLayer.AddSemiVertex(webMercatorPoint);
    }

    private FrameworkElement GetRightClickOptionsForDraw()
    {
        var presenter = new MapOptionsViewModel(
        rightToolTip: "تکمیل",
        leftToolTip: "لغو",
        middleToolTip: "تکمیل تکه‌جاری",

        rightSymbol: MapOptionsIcon.FromMaterial(MahApps.Metro.IconPacks.PackIconMaterialKind.CheckBold),
        leftSymbol: MapOptionsIcon.FromMaterial(MahApps.Metro.IconPacks.PackIconMaterialKind.CloseThick),
        middleSymbol: MapOptionsIcon.FromPhosphorIcons(MahApps.Metro.IconPacks.PackIconPhosphorIconsKind.CirclesThreePlusFill));

        presenter.LeftCommandAction = i =>
        {
            this.CancelDrawing();
            this.RemoveRightClickOptions();
        };
        presenter.RightCommandAction = i =>
        {
            this.FinishDrawing();
            this.RemoveRightClickOptions();
        }
        ;
        presenter.MiddleCommandAction = i =>
        {
            this.FinishDrawingPart();
            this.RemoveRightClickOptions();
        };


        var view = new Jab.Common.Views.MapOptions.MapThreeOptions(true);

        view.DataContext = presenter;

        return view;
    }

    private Task<Response<Geometry<sb.Point>>> GetDrawing(DrawMode mode, EditableFeatureLayerOptions options, bool display = false)
    {
        drawingTcs = new TaskCompletionSource<Response<Geometry<sb.Point>>>();

        drawingCancellationToken = new CancellationTokenSource();

        //in order to not show delete button when drawing new measure
        this.CurrentEditingLayer = null;

        this.displayDrawingPath = display;

        options.IsNewDrawing = true;

        this.drawingOptions = options;

        this.drawMode = mode;

        if (this.viewTransform == null || drawMode == DrawMode.Freehand)
            drawingTcs.TrySetCanceled();

        ResetMapViewEvents();

        this.SetCursor(Cursors.Cross);

        drawingCancellationToken.Token.Register(() =>
        {
            drawingTcs.TrySetCanceled();

            //this.SetCursor(Cursors.Arrow);
            this.SetCursor(CursorSettings[_currentMouseAction]);

            this.mapView.MouseUp -= MapView_MouseUpForForPanWhileDrawing;

            //this.mapView.MouseDown -= MapView_MouseDownForStartDrawing;
            this.mapView.MouseDown -= MapView_MouseDownForPanWhileStartDrawing;
            this.mapView.MouseUp -= MapView_MouseUpForPanWhileStartDrawing;
            this.mapView.MouseMove -= MapView_MouseMoveForPanWhileStartDrawing;

            this.mapView.MouseMove -= MapView_MouseMoveForPanWhileDrawing;
            this.mapView.MouseDown -= MapView_MouseDownForPanWhileDrawing;

            this.mapView.MouseMove -= MapView_MouseMoveForDrawing;

            this.mapView.MouseDown -= MapView_MouseDownForRectangleDrawing;
            this.mapView.MouseMove -= MapView_MouseMoveForRectangleDrawing;
            this.mapView.MouseUp -= MapView_MouseUpForRectangleDrawing;

            this.mapView.Children.Remove(drawingRectangle);

            this.ClearLayer(drawingLayer, remove: true, forceRemove: true);

            if (drawingLayer != null)
            {
                RemoveEditableFeatureLayer(drawingLayer.GetLayer());
            }

            drawingTcs = null;

            drawingCancellationToken = null;

        }, useSynchronizationContext: false);

        if (this.drawMode == DrawMode.Rectangle)
        {
            // Drag-based axis-aligned rectangle drawing
            this.mapView.MouseDown -= MapView_MouseDownForRectangleDrawing;
            this.mapView.MouseDown += MapView_MouseDownForRectangleDrawing;

            this.mapView.MouseMove -= MapView_MouseMoveForRectangleDrawing;
            this.mapView.MouseMove += MapView_MouseMoveForRectangleDrawing;

            this.mapView.MouseUp -= MapView_MouseUpForRectangleDrawing;
            this.mapView.MouseUp += MapView_MouseUpForRectangleDrawing;
        }
        else
        {
            // Existing point/polyline/polygon drawing
            this.mapView.MouseMove -= MapView_MouseMoveForPanWhileStartDrawing;
            this.mapView.MouseMove += MapView_MouseMoveForPanWhileStartDrawing;

            this.mapView.MouseDown -= MapView_MouseDownForPanWhileStartDrawing;
            this.mapView.MouseDown += MapView_MouseDownForPanWhileStartDrawing;

            this.mapView.MouseUp -= MapView_MouseUpForPanWhileStartDrawing;
            this.mapView.MouseUp += MapView_MouseUpForPanWhileStartDrawing;

            this.mapView.MouseMove -= MapView_MouseMoveForDrawing;
            this.mapView.MouseMove += MapView_MouseMoveForDrawing;
            //this.mapView.MouseDown -= MapView_MouseDownForStartDrawing;
            //this.mapView.MouseDown += MapView_MouseDownForStartDrawing;
        }

        return drawingTcs.Task;

    }

    // todo: validation on geometry
    public async Task<Response<Geometry<sb.Point>>> GetDrawingAsync(DrawMode mode, EditableFeatureLayerOptions? options = null, bool display = false, bool makeValid = true)
    {
        try
        {
            CancelAsyncMapInteractions();

            this.Status = MapStatus.Drawing;

            options = options ?? EditableFeatureLayerOptions.CreateDefault();

            var result = await GetDrawing(mode, options, display);

            this.Status = MapStatus.Idle;

            if (result.HasNotNullResult())
            {
                // todo: validation on geometry
                return ResponseFactory.Create(result.Result/*.AsSqlGeometry().MakeValid().AsGeometry()*/);
            }
            else
            {
                return new Response<Geometry<sb.Point>>();
            }
        }
        catch (TaskCanceledException)
        {
            if (drawingCancellationToken == null)
            {
                this.Status = MapStatus.Idle;

                this.Pan();
            }

            return new Response<Geometry<sb.Point>>() { IsCanceled = true };
        }
        catch (Exception ex)
        {
            this.Status = MapStatus.Idle;

            drawingTcs = null;

            drawingCancellationToken = null;

            this.Pan();

            throw;
        }
        finally
        {
            //this.Status = MapStatus.Idle;

            drawingLayer = null;
            //this.Pan();
        }
    }

    public void CancelDrawing()
    {
        if (this.drawingCancellationToken != null)
        {
            this.drawingCancellationToken.Cancel();
        }
    }



    #endregion


    #region SelectPoint


    double _knownAsPanThreshold = 3;

    public bool itWasPanningWhileSelectThePoint { get; set; }

    CancellationTokenSource selectPointCancelationToken;

    /// <summary>
    /// Returns the point selected by the user in WGS84
    /// </summary>
    /// <returns></returns>
    private Task<sb.Point> SelectThePoint()
    {
        var selectPointTcs = new TaskCompletionSource<sb.Point>();

        selectPointCancelationToken = new CancellationTokenSource();

        ResetMapViewEvents();

        this.SetCursor(Cursors.Cross);

        MouseButtonEventHandler action = null;

        action = (sender, e) =>
        {
            if (e.ChangedButton != MouseButton.Left)
                return;

            e.Handled = true;

            this.prevMouseLocation = e.GetPosition(this.mapView);

            if (itWasPanningWhileSelectThePoint)
            {
                this.ResetMapViewEvents();

                this.Refresh(isNewExtent: true);

                this.SetCursor(Cursors.Cross);

                this.mapView.MouseMove -= MapView_MouseMoveSelectThePoint;
                this.mapView.MouseMove += MapView_MouseMoveSelectThePoint;

                this.mapView.MouseDown -= MapView_MouseDownForPanWhileSelectThePoint;
                this.mapView.MouseDown += MapView_MouseDownForPanWhileSelectThePoint;

                this.mapView.MouseUp -= action;
                this.mapView.MouseUp += action;

                itWasPanningWhileSelectThePoint = false;

                return;
            }
            else
            {
                this.mapView.MouseMove -= MapView_MouseMoveSelectThePoint;
                this.mapView.MouseDown -= MapView_MouseDownForPanWhileSelectThePoint;
                this.mapView.MouseUp -= action;

                this.SetCursor(CursorSettings[_currentMouseAction]);

                selectPointTcs.SetResult(ScreenToGeodetic(Mouse.GetPosition(this.mapView)).AsPoint());
            }
        };

        this.mapView.MouseMove -= MapView_MouseMoveSelectThePoint;
        this.mapView.MouseMove += MapView_MouseMoveSelectThePoint;

        this.mapView.MouseDown -= MapView_MouseDownForPanWhileSelectThePoint;
        this.mapView.MouseDown += MapView_MouseDownForPanWhileSelectThePoint;

        this.mapView.MouseUp -= action;
        this.mapView.MouseUp += action;

        selectPointCancelationToken.Token.Register(() =>
        {
            selectPointTcs.TrySetCanceled();

            this.SetCursor(CursorSettings[_currentMouseAction]);

            this.mapView.MouseMove -= MapView_MouseMoveSelectThePoint;
            this.mapView.MouseDown -= MapView_MouseDownForPanWhileSelectThePoint;
            this.mapView.MouseUp -= action;

            selectPointTcs = null;

            selectPointCancelationToken = null;

        }, useSynchronizationContext: false);

        return selectPointTcs.Task;
    }

    /// <summary>
    /// Returns the point selected by the user in WGS84
    /// </summary>
    /// <returns></returns>
    public async Task<Response<sb.Point>> SelectPointAsync()
    {
        try
        {
            if (selectPointCancelationToken != null)
            {
                selectPointCancelationToken.Cancel();
            }

            var result = await SelectThePoint();

            return new Response<sb.Point>() { Result = result };
        }
        catch (TaskCanceledException)
        {
            if (selectPointCancelationToken == null)
            {
                this.Status = MapStatus.Idle;

                this.Pan();
            }
            return new Response<sb.Point>() { IsCanceled = true };
        }
        catch (Exception ex)
        {
            this.Status = MapStatus.Idle;

            selectPointCancelationToken = null;

            this.Pan();

            throw;
        }
        finally
        {
            this.Pan();
        }
    }

    private void MapView_MouseDownForPanWhileSelectThePoint(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
            return;

        e.Handled = true;

        itWasPanningWhileSelectThePoint = false;

        this.prevMouseLocation = e.GetPosition(this.mapView);
    }

    private void MapView_MouseMoveSelectThePoint(object sender, MouseEventArgs e)
    {
        Point currentMouseLocation = e.GetPosition(this.mapView);

        if (e.LeftButton == MouseButtonState.Pressed)
        {
            double xOffset = currentMouseLocation.X - this.prevMouseLocation.X;

            double yOffset = currentMouseLocation.Y - this.prevMouseLocation.Y;

            if (Math.Abs(xOffset) > _knownAsPanThreshold || Math.Abs(yOffset) > _knownAsPanThreshold)
            {
                this.panTransform.X += xOffset * 1.0 / this.zoomTransform.ScaleX;

                this.panTransform.Y += yOffset * 1.0 / this.zoomTransform.ScaleY;

                this.prevMouseLocation = currentMouseLocation;

                this.panTransformForPoints.X += xOffset;

                this.panTransformForPoints.Y += yOffset;

                UpdateTileInfos();

                this.itWasPanningWhileSelectThePoint = true;
            }
            else { }
        }
        else
        {
            //var mapLocation = ScreenToMap(currentMouseLocation);
            //this.drawingLayer.UpdateLastVertexLocation(mapLocation.AsPoint());
            //onMoveForDrawAction?.Invoke(mapLocation);
        }
    }


    #endregion




    #region Edit

    CancellationTokenSource editingCancellationToken;

    private EditableFeatureLayer? _currentEditingLayer;

    public EditableFeatureLayer? CurrentEditingLayer
    {
        get { return _currentEditingLayer; }
        set
        {
            _currentEditingLayer = value;
            RaisePropertyChanged();
            this.OnEditableFeatureLayerChanged?.Invoke(null, value);
        }
    }

    private Task<Response<Geometry<sb.Point>>> EditGeometry(Geometry<sb.Point> geometry, EditableFeatureLayerOptions options)
    {
        editingCancellationToken = new CancellationTokenSource();

        var tcs = new TaskCompletionSource<Response<Geometry<sb.Point>>>();

        if (CurrentEditingLayer != null)
        {
            this.RemoveEditableFeatureLayer(CurrentEditingLayer);
        }

        options.IsNewDrawing = false;

        CurrentEditingLayer = new EditableFeatureLayer("edit", geometry.Clone(), this.viewTransform, ScreenToMap, options) { ZIndex = int.MaxValue };
        //{
        //    MassEdit = true
        //};

        //1397.08.19
        //var tempCurrentPosition = new Point();

        //this.OnMapMouseMove += (sender, e) =>
        //{
        //    var screenPoint = e.GetPosition(this.mapView);

        //    var mapPoint = ScreenToMap(screenPoint).AsPoint();

        //    var tempDistance = tempCurrentPosition.AsPoint().DistanceTo(screenPoint.AsPoint());

        //    if (tempDistance < 10)
        //    {
        //        return;
        //    }

        //    tempCurrentPosition = screenPoint;

        //    CurrentEditingLayer?.FindNearestPoint(mapPoint);
        //};

        CurrentEditingLayer.RequestRightClickOptions = this.AddRightClickOptions;

        CurrentEditingLayer.RequestRemoveRightClickOptions = this.RemoveRightClickOptions;

        CurrentEditingLayer.RequestRefresh = l =>
        {
            this.RemoveEditableFeatureLayer(l);

            this.SetLayer(l);

            AddEditableFeatureLayer(l);
        };

        CurrentEditingLayer.RequestFinishEditing = (g) =>
        {
            tcs.SetResult(ResponseFactory.Create(g));

            this.RemoveEditableFeatureLayer(CurrentEditingLayer);

            CurrentEditingLayer = null;

            editingCancellationToken = null;
        };

        CurrentEditingLayer.RequestConvertToDrawingItem = (g) =>
        {
            _presenter.AddDrawingItem(g);
        };

        CurrentEditingLayer.RequestShowGeometryDetails = (editableFeatureLayer) =>
        {

            this._presenter.CurrentGeometryDetails = new GeometryDetailsViewModel(editableFeatureLayer, this._presenter.DialogService);

            var dialog = new Views.Dialogs.GeometryDetailsDialogView(/*editableFeatureLayer, this._presenter.DialogService*/)
            {
                Owner = Window.GetWindow(this)
            };

            dialog.DataContext = this._presenter.CurrentGeometryDetails;

            dialog.Show();
        };

        CurrentEditingLayer.RequestCancelEditing = (g) =>
        {
            tcs?.TrySetCanceled();

            this.RemoveEditableFeatureLayer(CurrentEditingLayer);

            CurrentEditingLayer = null;

            editingCancellationToken = null;
        };

        this.SetLayer(CurrentEditingLayer);

        AddEditableFeatureLayer(CurrentEditingLayer);

        editingCancellationToken.Token.Register(() =>
        {
            tcs.TrySetCanceled();

            this.SetCursor(CursorSettings[_currentMouseAction]);

            this.RemoveEditableFeatureLayer(CurrentEditingLayer);

            tcs = null;

        }, useSynchronizationContext: false);

        return tcs.Task;
    }

    public async Task<Response<Geometry<sb.Point>>> EditGeometryAsync(Geometry<sb.Point> originalGeometry, EditableFeatureLayerOptions options)
    {
        Geometry<sb.Point> originalClone = null;

        try
        {
            originalClone = originalGeometry.Clone();

            //if (editingCancellationToken != null)
            //{
            //    editingCancellationToken.Cancel();
            //}
            CancelAsyncMapInteractions();

            this.Status = MapStatus.Editing;

            var result = await EditGeometry(originalGeometry, options);

            this.Status = MapStatus.Idle;

            return result;
        }
        catch (TaskCanceledException)
        {
            if (editingCancellationToken == null)
            {
                this.Status = MapStatus.Idle;
            }

            //97 04 27
            //return originalGeometry;
            return new Response<Geometry<sb.Point>>() { Result = originalGeometry, IsCanceled = true };
        }
        catch (Exception ex)
        {
            this.Status = MapStatus.Idle;

            throw;
        }
    }

    public void CancelEditGeometry()
    {
        if (editingCancellationToken != null)
        {
            editingCancellationToken.Cancel();
        }
    }

    public void FinishEditing()
    {
        CurrentEditingLayer?.FinishEditing();
    }

    #endregion


    #region Panable Path

    private void AddPanablePathToMapView(Path path)
    {
        if (!this.mapView.Children.Contains(path))
        {
            path.RenderTransform = this.panTransformForPoints;

            this.mapView.Children.Add(path);
        }
    }

    private void RemovePathFromMapView(Path path)
    {
        if (this.mapView.Children.Contains(path))
        {
            this.mapView.Children.Remove(path);
        }
    }

    #endregion


    #region Bezier

    CancellationTokenSource bezierCancellationToken;

    private void RegisterPolyBezierLayer(PolyBezierLayer layer)
    {
        layer.RequestRightClickOptions = this.AddRightClickOptions;

        layer.RequestRemoveRightClickOptions = this.RemoveRightClickOptions;

        layer.RequestRefresh = l =>
        {
            this.ClearLayer(l, remove: true, forceRemove: true);

            this.SetLayer(l);

            AddPolyBezierLayer(l);
        };

        layer.RequestAddLayer = l =>
        {
            if (l is SpecialLineLayer)
            {
                AddSpecialLineLayer(l as SpecialLineLayer, () =>
                {
                    layer.IsBezierShown = !layer.IsBezierShown;

                    if (layer.IsBezierShown)
                    {
                        AddComplexLayer(layer.GetMainPointLayer());

                        AddComplexLayer(layer.GetControlPointLayer());

                        AddPanablePathToMapView(layer.GetMainPath());

                        AddPanablePathToMapView(layer.GetControlPath());
                    }
                    else
                    {
                        ClearLayer(layer.GetMainPointLayer(), remove: true, forceRemove: true);

                        ClearLayer(layer.GetControlPointLayer(), remove: true, forceRemove: true);

                        RemovePathFromMapView(layer.GetMainPath());

                        RemovePathFromMapView(layer.GetControlPath());
                    }
                });
            }
            else if (l is SpecialPointLayer)
            {
                AddComplexLayer(l as SpecialPointLayer);
            }
        };

        layer.RequestRemoveLayer = l => this.ClearLayer(l, remove: true, forceRemove: true);

    }

    private async Task<Response<PolyBezierLayer>> GetBezier(Geometry decoration, VisualParameters decorationVisual)
    {
        bezierCancellationToken = new CancellationTokenSource();

        var tcs = new TaskCompletionSource<Response<PolyBezierLayer>>();

        bezierCancellationToken.Token.Register(() =>
        {
            tcs.TrySetCanceled();

            //this.SetCursor(Cursors.Arrow);
            this.SetCursor(CursorSettings[_currentMouseAction]);

        }, useSynchronizationContext: false);

        var drawingResult = await GetDrawingAsync(DrawMode.Polyline);

        if (!drawingResult.HasNotNullResult())
        {
            bezierCancellationToken.Cancel();
        }
        else
        {
            var polyline = drawingResult.Result.Points.Cast<sb.Point>().ToList();

            PolyBezierLayer layer = new PolyBezierLayer(polyline, this.viewTransform, decoration, decorationVisual);

            RegisterPolyBezierLayer(layer);

            layer.RequestFinishEditing = r =>
            {
                tcs.SetResult(ResponseFactory.Create<PolyBezierLayer>(r));
            };

            this.SetLayer(layer);

            this.AddPolyBezierLayer(layer);
        }

        return await tcs.Task;
    }

    public async Task<Response<PolyBezierLayer>> GetBezierAsync(Geometry decoration = null, VisualParameters decorationVisual = null)
    {
        try
        {
            if (bezierCancellationToken != null)
            {
                bezierCancellationToken.Cancel();
            }

            this.Status = MapStatus.Drawing;

            var result = await GetBezier(decoration, decorationVisual);

            this.Status = MapStatus.Idle;

            if (result.HasNotNullResult())
            {
                RemovePolyBezierLayer(result.Result);
            }

            return result;
        }
        catch (TaskCanceledException)
        {
            this.Status = MapStatus.Idle;

            return new Response<PolyBezierLayer>() { Result = null, IsCanceled = true };
        }
        catch (Exception ex)
        {
            this.Status = MapStatus.Idle;

            throw;
        }
    }

    public void CancelGetBezier()
    {
        if (bezierCancellationToken != null)
        {
            bezierCancellationToken.Cancel();
        }
    }

    #endregion


    #region Measure Area/Distance

    SpecialPointLayer measureLayer;

    CancellationTokenSource _measureCancellationToken;

    Guid _measureId;


    // todo: validation on geometry
    private async Task<Response<Geometry<sb.Point>>> Measure(DrawMode mode, EditableFeatureLayerOptions drawingOptions, EditableFeatureLayerOptions editingOptions, Action action, Guid guid)
    {
        this._measureCancellationToken = new CancellationTokenSource();

        this._measureCancellationToken.Token.Register(() =>
        {
            this.ClearLayer(measureLayer, remove: true, forceRemove: true);

            _measureCancellationToken = null;

            CancelDrawing();

            CancelEditGeometry();
        });


        this.ClearLayer(measureLayer, remove: true, forceRemove: true);

        var measureLocatable = new Locateable(new sb.Point(0, 0), AncherFunctionHandlers.BottomCenter) { Element = new IRI.Maptor.Jab.Common.Views.MapMarkers.LabelMarker("test") };

        measureLayer = new SpecialPointLayer("measure", measureLocatable, 1, ScaleInterval.All, LayerType.Complex);

        this.SetLayer(measureLayer);

        onMoveForDrawAction = p =>
        {
            this.AddComplexLayer(measureLayer, true);

            var offset = ScreenToMap(20);

            measureLayer.Items.First().X = p.X;
            measureLayer.Items.First().Y = p.Y + offset;

            var marker = (measureLayer.Items.First().Element as IRI.Maptor.Jab.Common.Views.MapMarkers.LabelMarker);

            try
            {
                var geo = drawingLayer.GetFinalGeometry().Clone();

                geo.InsertLastPoint(p.AsPoint());

                // todo: validation on geometry
                //var geoAsGeodetic = geo.AsSqlGeometry().WebMercatorToGeodeticWgs84().MakeValid();
                //var measureValue = mode == DrawMode.Polygon ? UnitHelper.GetAreaLabel(geoAsGeodetic.STArea().Value) : UnitHelper.GetLengthLabel(geoAsGeodetic.STLength().Value);
                //marker.ToolTip = mode == DrawMode.Polygon ? geoAsGeodetic.STArea().Value : geoAsGeodetic.STLength().Value;

                var measureValue = SpatialUtility.GetEllipsoidMeasureLabel(geo, MapProjects.WebMercatorToGeodeticWgs84);

                marker.ToolTip = SpatialUtility.GetEllipsoidMeasure(geo, MapProjects.WebMercatorToGeodeticWgs84);

                marker.LabelValue = measureValue;


            }
            catch (Exception ex)
            {
                marker.LabelValue = "عارضه نامعتبر";
            }
        };

        var result = await GetDrawingAsync(mode, drawingOptions, true);

        this.ClearLayer(measureLayer, remove: true, forceRemove: true);

        if (result.HasNotNullResult())
        {
            result = await EditGeometryAsync(result.Result, editingOptions);
        }

        if (_measureId == guid)
        {
            onMoveForDrawAction = null;

            _measureCancellationToken = null;

            if (result == null)
            {
                this.Pan();
            }
        }

        return result;
    }

    //public async Task<sb.Geometry> MeasureAsync(DrawMode mode, bool isEdgeLabelVisible, Action action)
    public async Task<Response<Geometry<sb.Point>>> MeasureAsync(DrawMode mode, EditableFeatureLayerOptions drawingOptions, EditableFeatureLayerOptions editingOptions, Action action)
    {
        _measureId = Guid.NewGuid();

        try
        {
            CancelAsyncMapInteractions();

            CancelMeasure();

            return await Measure(mode, drawingOptions, editingOptions, action, _measureId);
        }
        catch (TaskCanceledException)
        {
            this.Status = MapStatus.Idle;

            return new Response<Geometry<sb.Point>>() { Result = null, IsCanceled = true };
        }
        catch (Exception ex)
        {
            this._measureCancellationToken = null;

            this.Status = MapStatus.Idle;

            throw;
        }
    }

    public void CancelMeasure()
    {
        if (_measureCancellationToken != null)
        {
            _measureCancellationToken.Cancel();
        }
    }

    #endregion
}