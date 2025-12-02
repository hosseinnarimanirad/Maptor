using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
//using System.Windows;
using System.Windows.Media.Imaging;
using WpfPoint = System.Windows.Point;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.GeoJsonFormat;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Common.Services;
using IRI.Maptor.Sta.Spatial.Model;
using IRI.Maptor.Sta.Spatial.Helpers;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.Persistence.DataSources;
using IRI.Maptor.Sta.Persistence.RasterDataSources;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;

using IRI.Maptor.Ket.GdiPersistence;
using IRI.Maptor.Ket.KmlFormat;
using IRI.Maptor.Sta.Spatial.IO.Dxf;
using IRI.Maptor.Jab.Common.Models;
using IRI.Maptor.Jab.Common.Helpers;
using IRI.Maptor.Jab.Common.Models.Map;
using IRI.Maptor.Jab.Common.TileServices;
using IRI.Maptor.Jab.Common.Assets.Commands;
using IRI.Maptor.Jab.Common.Views.MapMarkers;
using IRI.Maptor.Jab.Common.Models.Spatialable;
using IRI.Maptor.Jab.Common.Models.Legend;
using IRI.Maptor.Jab.Common.Events;
using IRI.Maptor.Jab.Common.Cartography.Symbologies;
using IRI.Maptor.Jab.Common.Abstractions;
using IRI.Maptor.Sta.Ogc.WMS;
using IRI.Maptor.Jab.Common.ViewModels.CoordinateEditor;
using IRI.Maptor.Jab.Common.ViewModels.Map;


namespace IRI.Maptor.Jab.Common.ViewModels;

public abstract class MapViewModelBase : ViewModelBase
{
    #region Properties

    private ProxySettingsModel _proxy;
    public ProxySettingsModel Proxy
    {
        get { return _proxy; }
        set
        {
            _proxy = value;
            value.FireProxyChanged = p => SetProxy(p.GetProxy());
            RaisePropertyChanged();
        }
    }


    private MapSettingsModel _mapSettings = new MapSettingsModel();
    public MapSettingsModel MapSettings
    {
        get { return _mapSettings; }
        private set
        {
            _mapSettings = value;
            RaisePropertyChanged();
        }
    }


    private MapInfoViewModel _mapPanel;
    public MapInfoViewModel MapPanel
    {
        get { return _mapPanel; }
        set
        {
            _mapPanel = value;
            RaisePropertyChanged();
        }
    }


    private CoordinatePanelViewModel _coordinatePanel;
    public CoordinatePanelViewModel CoordinatePanel
    {
        get { return _coordinatePanel; }
        set
        {
            _coordinatePanel = value;
            RaisePropertyChanged();
        }
    }


    private List<EnvelopeMarkupLabelTriple> _ostanha;
    public List<EnvelopeMarkupLabelTriple> Ostanha
    {
        get { return _ostanha; }
        set
        {
            _ostanha = value;
            RaisePropertyChanged();
        }
    }

    private double _baseMapOpacity = 1;
    public double BaseMapOpacity
    {
        get { return _baseMapOpacity; }
        set
        {
            _baseMapOpacity = value;
            RaisePropertyChanged();

            foreach (var layer in Layers.Where(i => i.Type == LayerType.BaseMap))
            {
                layer./*VisualParameters.*/Opacity = value;
            }
        }
    }

    private double _legendFontSize = 12;
    public double LegendFontSize
    {
        get { return _legendFontSize; }
        set
        {
            _legendFontSize = value;
            RaisePropertyChanged();
        }
    }

    private GeometryDetailsViewModel _geometryDetails;
    public GeometryDetailsViewModel GeometryDetails
    {
        get { return _geometryDetails; }
        set
        {
            _geometryDetails = value;
            RaisePropertyChanged();
        }
    }


    private EditableFeatureLayer _currentEditingLayer;
    public EditableFeatureLayer CurrentEditingLayer
    {
        get { return _currentEditingLayer; }
        set
        {
            _currentEditingLayer = value;
            RaisePropertyChanged();

            if (_currentEditingLayer != null)
            {
                _currentEditingLayer.RequestSelectedLocatableChanged = (l) =>
                {
                    UpdateCurrentEditingPoint(new Point(l.X, l.Y));
                };

                _currentEditingLayer.RequestZoomToPoint = (p) =>
                {
                    Zoom(WebMercatorUtility.GetGoogleMapScale(14), p);
                };

                _currentEditingLayer.RequestZoomToGeometry = g =>
                {
                    ZoomToExtent(g.GetBoundingBox(), isExactExtent: false, isNewExtent: true);
                };
            }

        }
    }


    private ObservableCollection<SelectedLayer> _selectedLayers = new ObservableCollection<SelectedLayer>();
    public ObservableCollection<SelectedLayer> SelectedLayers
    {
        get { return _selectedLayers; }
        set
        {
            _selectedLayers = value;
            RaisePropertyChanged();
        }
    }


    private SelectedLayer _currentLayer;
    public SelectedLayer CurrentLayer
    {
        get { return _currentLayer; }
        set
        {
            _currentLayer = value;
            RaisePropertyChanged();

            if (value?.ShowSelectedOnMap == true)
            {
                ShowSelectedFeatures(value?.GetSelectedFeatures(), value?.AssociatedLayer?.DefaultSymbology?.StrokeThickness);
            }

            if (_currentLayer is null)
            {
                ClearLayer("__$selection", true);
                ClearLayer("__$highlight", true);
            }
        }
    }


    private ObservableCollection<ILayer> _layers;
    public ObservableCollection<ILayer> Layers
    {
        get { return _layers; }
        set
        {
            _layers = value;
            RaisePropertyChanged();

            //if (_layers == null)
            //    return;

            //_layers.CollectionChanged += (sender, e) =>
            //{
            //    switch (e.Action)
            //    {
            //        case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
            //            foreach (var item in e.NewItems.Cast<ILayer>())
            //            {
            //                if (item.Type == LayerType.BaseMap)
            //                {
            //                    return;
            //                }

            //                var layer = item;

            //                var model = new Model.Legend.MapLegendItemWithOptionsModel(layer);
            //                model.Commands = new List<Model.Legend.ILegendCommand>()
            //                {
            //                    LegendCommand.CreateZoomToExtentCommand(this, layer),
            //                };

            //                LegendLayers.Add(model);
            //            }
            //            break;
            //        case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
            //            foreach (var item in e.OldItems.Cast<ILayer>())
            //            {
            //                if (item.Type == LayerType.BaseMap)
            //                {
            //                    return;
            //                }

            //                LegendLayers.Remove(LegendLayers.First(ll => ll.Id == item.Id));
            //            }
            //            break;
            //        case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
            //            break;
            //        case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
            //            break;
            //        case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
            //            this.LegendLayers.Clear();
            //            break;
            //        default:
            //            break;
            //    }
            //};
        }
    }


    //LegendCommand.CreateZoomToExtentCommand(this, layer),
    //                LegendCommand.CreateSelectByDrawing<T>(this, (VectorLayer) layer),
    //                LegendCommand.CreateShowAttributeTable<T>(this, (VectorLayer) layer),
    //                LegendCommand.CreateClearSelected(this, (VectorLayer) layer),
    //                LegendCommand.CreateRemoveLayer(this, layer),

    private List<Func<MapViewModelBase, IFeatureTableCommand>> _defaultVectorLayerFeatureTableCommands = FeatureTableCommands.GetDefaultVectorLayerCommands/*<Feature<Point>>*/();
    public List<Func<MapViewModelBase, IFeatureTableCommand>> DefaultVectorLayerFeatureTableCommands
    {
        get { return _defaultVectorLayerFeatureTableCommands; }
        set
        {
            _defaultVectorLayerFeatureTableCommands = value;
            RaisePropertyChanged();
        }
    }


    //private List<Func<MapPresenter, ILayer, ILegendCommand>> _defaultVectorLayerCommands = LegendCommand.GetDefaultVectorLayerCommands<Feature<Point>>();

    //public List<Func<MapPresenter, ILayer, ILegendCommand>> DefaultVectorLayerCommands
    //{
    //    get { return _defaultVectorLayerCommands; }
    //    set { value = _defaultVectorLayerCommands; }
    //}

    public ILayer GetSelectedLayerInToc()
    {
        return Layers.SingleOrDefault(l => l.IsSelectedInToc);
    }

    private ObservableCollection<DrawingItemLayer> _drawingItems = new ObservableCollection<DrawingItemLayer>();
    public ObservableCollection<DrawingItemLayer> DrawingItems
    {
        get { return _drawingItems; }
        set
        {
            _drawingItems = value;
            RaisePropertyChanged();
        }
    }


    private DrawingItemLayer? _SelectedDrawingItem;
    public DrawingItemLayer? SelectedDrawingItem
    {
        get { return _SelectedDrawingItem; }
        set
        {
            _SelectedDrawingItem = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(CanMoveDrawingItemDown));
            RaisePropertyChanged(nameof(CanMoveDrawingItemUp));
        }
    }


    private BoundingBox _printArea = BoundingBox.NaN;

    // in map coordinates. determinse the area used for save as png
    public BoundingBox PrintArea
    {
        get { return _printArea; }
        set
        {
            _printArea = value;
            RaisePropertyChanged();
        }
    }


    private Point _currentPoint;
    public Point CurrentPoint
    {
        get { return _currentPoint; }
        set
        {
            _currentPoint = value;
            RaisePropertyChanged();
        }
    }


    #region BaseMap Mode

    private string _localNetworkBaseMapBaseUrl;
    public string LocalNetworkBaseMapBaseUrl
    {
        get { return _localNetworkBaseMapBaseUrl; }
        set
        {
            _localNetworkBaseMapBaseUrl = value;
            RaisePropertyChanged();

            UpdateTilesServices();
        }
    }

    private string _proxyAppBaseMapBaseUrl;
    public string ProxyAppBaseMapBaseUrl
    {
        get { return _proxyAppBaseMapBaseUrl; }
        set
        {
            _proxyAppBaseMapBaseUrl = value;
            RaisePropertyChanged();

            UpdateTilesServices();
        }
    }



    private TileMapProviderMode _selectedTileMapProviderMode = TileMapProviderMode.Internet;
    public virtual TileMapProviderMode SelectedTileMapProviderMode
    {
        get { return _selectedTileMapProviderMode; }
        set
        {
            _selectedTileMapProviderMode = value;
            RaisePropertyChanged();

            UpdateTilesServices();
        }
    }

    private void UpdateTilesServices()
    {
        foreach (var item in this.MapProviders)
        {
            item.ChangeMode(SelectedTileMapProviderMode, LocalNetworkBaseMapBaseUrl, ProxyAppBaseMapBaseUrl/*IRI.App.MakanNegarSaba.Properties.Settings.Default.baseMapUrl*/);
        }
    }

    //private bool _isLocalBaseMapMode;
    //public bool IsLocalBaseMapMode
    //{
    //    get { return _isLocalBaseMapMode; }
    //    set
    //    {
    //        _isLocalBaseMapMode = value;
    //        RaisePropertyChanged();

    //        //Properties.Settings.Default.IsLocalBaseMapMode = value;

    //        foreach (var item in this.MapProviders)
    //        {
    //            item.ChangeMode(value ? TileMapProviderMode.LocalNetwork : TileMapProviderMode.Internet, LocalNetworkBaseMapBaseUrl/*IRI.App.MakanNegarSaba.Properties.Settings.Default.baseMapUrl*/);
    //        }

    //    }
    //}

    //private bool _isOnlineBaseMapMode;
    //public bool IsOnlineBaseMapMode
    //{
    //    get { return _isOnlineBaseMapMode; }
    //    set
    //    {
    //        _isOnlineBaseMapMode = value;
    //        RaisePropertyChanged();
    //    }
    //}

    //private bool _isProxyAppBaseMapMode;
    //public bool IsProxyAppBaseMapMode
    //{
    //    get { return _isProxyAppBaseMapMode; }
    //    set
    //    {
    //        _isProxyAppBaseMapMode = value;
    //        RaisePropertyChanged();
    //    }
    //}


    #endregion

    //private Dictionary<string, Func<TileType, IMapProvider>> _mapProviders;
    //public Dictionary<string, Func<TileType, IMapProvider>> MapProviders
    //{
    //    get { return _mapProviders; }
    //    set
    //    {
    //        _mapProviders = value;
    //        RaisePropertyChanged();
    //    }
    //}
    private List<TileMapProvider> _mapProviders;

    public List<TileMapProvider> MapProviders
    {
        get { return _mapProviders; }
        set
        {
            _mapProviders = value;
            RaisePropertyChanged();
        }
    }

    private TileMapProvider _selectedMapProvider;

    public TileMapProvider SelectedMapProvider
    {
        get { return _selectedMapProvider; }
        set
        {
            _selectedMapProvider = value;
            RaisePropertyChanged();

            _ = SetTileService(value, BaseMapOpacity, MapSettings.GetLocalFileName);
        }
    }


    //private string _providerTypeFullName = string.Empty;
    //public string ProviderTypeFullName
    //{
    //    get { return _providerTypeFullName; }
    //}

    //public async Task SetTileBaseMap(string tileMapFullName)
    //{
    //    if (string.IsNullOrEmpty(tileMapFullName))
    //    {
    //        RemoveAllTileServices();

    //        return;
    //    }

    //    var tileMapFullNameToUpper = tileMapFullName?.ToUpper();

    //    //if (_providerTypeFullName == tileMapFullNameToUpper)
    //    //{
    //    //    return;
    //    //}

    //    //_providerTypeFullName = tileMapFullNameToUpper;

    //    //RaisePropertyChanged(nameof(ProviderTypeFullName));

    //    var provider = this.MapProviders.SingleOrDefault(m => m.FullName?.EqualsIgnoreCase(tileMapFullNameToUpper) == true);

    //    if (provider == null)
    //    {
    //        return;
    //    }

    //    this.SelectedMapProvider = provider;

    //    await SetTileService(provider, MapSettings.GetLocalFileName);//, MapSettings.GetFileName);             
    //}

    public void SetTileBaseMap(TileMapProvider? provider/*, double opacity*/)
    {
        if (provider is null)
            return;

        //if (provider.FullName == SelectedMapProvider?.FullName)
        if (provider == SelectedMapProvider)
            return;

        if (!MapProviders.Contains(provider))
            throw new NotImplementedException("MapPresenter > SetTileBaseMap");
        //this.MapProviders.Add(provider);

        SelectedMapProvider = provider;

        //await SetTileService(provider, opacity, MapSettings.GetLocalFileName);
    }

    private bool _doNotCheckInternet = false;
    public bool DoNotCheckInternet
    {
        get { return _doNotCheckInternet; }
        set
        {
            _doNotCheckInternet = value;
            RaisePropertyChanged();
        }
    }


    private bool? _isConnected = null;
    public bool IsConnected
    {
        get { return _isConnected.HasValue && _isConnected.Value; }
        set
        {
            if (_isConnected == value)
                return;

            _isConnected = value;
            RaisePropertyChanged();

            RequestSetConnectedState?.Invoke(value);
        }
    }


    private MapStatus _mapStatus;
    public MapStatus MapStatus
    {
        get { return _mapStatus; }
        set
        {
            if (_mapStatus == value)
            {
                return;
            }

            _mapStatus = value;
            RaisePropertyChanged();

            switch (_mapStatus)
            {
                case MapStatus.Drawing:
                    IsDrawMode = true;
                    break;
                case MapStatus.Editing:
                    IsEditMode = true;
                    break;
                //case MapStatus.Measuring:
                //    this.IsMeasureMode = true;
                //    break;
                case MapStatus.Idle:
                    IsDrawMode = false;
                    IsEditMode = false;
                    break;
                default:
                    break;
            }

        }
    }


    private MapAction _mapAction;
    public MapAction MapAction
    {
        get { return _mapAction; }
        set
        {
            _mapAction = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsPanMode));
            RaisePropertyChanged(nameof(IsZoomInMode));
            RaisePropertyChanged(nameof(IsZoomOutMode));
        }
    }


    private bool _isBusy;
    public bool IsBusy
    {
        get { return _isBusy; }
        set
        {
            //SetIsBusy(value);
            _isBusy = value;
            RaisePropertyChanged();
        }
    }

    public double InverseMapScale
    {
        get
        {
            return 1.0 / MapScale;
        }
    }

    public double MapScale
    {
        get
        {
            return RequestMapScale?.Invoke() ?? 1;
        }
    }

    public double CurrentPointInverseMapScale
    {
        get
        {
            var scale = RequestCurrentPointScale?.Invoke() ?? 1;

            return Math.Round(1.0 / scale, 2);
        }
    }

    public double CurrentPointGroundResolution
    {
        get
        {
            return RequestCurrentPointGroundResolution?.Invoke() ?? 1;
        }
    }

    public int CurrentZoomLevel { get { return RequestCurrentZoomLevel?.Invoke() ?? 1; } }

    //public string CurrentScaleText => Localization.LocalizationManager.GetLocalizedNumberString($"1:{CurrentPointInverseMapScale}");

    public BoundingBox CurrentExtent
    {
        get
        {
            return RequestCurrentExtent?.Invoke() ?? BoundingBoxes.Mercator_Iran;
        }
    }


    public double ActualWidth
    {
        get { return RequestGetActualWidth?.Invoke() ?? 1; }
    }


    public double ActualHeight
    {
        get { return RequestGetActualHeight?.Invoke() ?? 1; ; }
    }


    private bool _isDrawMode;
    public bool IsDrawMode
    {
        get { return _isDrawMode; }
        set
        {
            _isDrawMode = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsDrawEditMeasureMode));
        }
    }


    private bool _isEditMode;
    public bool IsEditMode
    {
        get { return _isEditMode; }
        set
        {
            _isEditMode = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsDrawEditMeasureMode));
        }
    }


    public bool IsDrawEditMeasureMode
    {
        get
        {
            return IsEditMode || IsDrawMode;
        }
    }


    public bool IsPanMode
    {
        get { return MapAction == MapAction.Pan; }
        set
        {
            if (value)
            {
                Pan();
            }

            RaisePropertyChanged();
        }
    }


    public bool IsZoomInMode
    {
        get { return MapAction == MapAction.ZoomIn || MapAction == MapAction.ZoomInRectangle; }
        set
        {
            if (value)
            {
                EnableRectangleZoomIn();
            }

            RaisePropertyChanged();
        }
    }


    public bool IsZoomOutMode
    {
        get { return MapAction == MapAction.ZoomOut || MapAction == MapAction.ZoomOutRectangle; }
        set
        {
            if (value)
            {
                EnableZoomOut();
            }

            RaisePropertyChanged();
        }
    }


    #endregion


    #region Extent Manager

    public List<BoundingBox> Extents { get; set; } = new List<BoundingBox>();

    public bool NextExtentEnabled => CurrentExtentIndex > 0;

    public bool PreviousExtentEnabled => CurrentExtentIndex < Extents.Count - 1;

    public int ExtentHistoryLength => Extents.Count;

    private int _currentExtentIndex = 0;
    public int CurrentExtentIndex
    {
        get { return _currentExtentIndex; }
        set
        {
            _currentExtentIndex = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(PreviousExtentEnabled));
            RaisePropertyChanged(nameof(NextExtentEnabled));
        }
    }


    public void GoToPreviousExtent()
    {
        CurrentExtentIndex = Math.Min(Extents.Count - 1, CurrentExtentIndex + 1);

        ZoomToExtent(Extents[CurrentExtentIndex], isExactExtent: true, isNewExtent: false);
    }

    public void GoToNextExtent()
    {
        CurrentExtentIndex = Math.Max(0, CurrentExtentIndex - 1);

        ZoomToExtent(Extents[CurrentExtentIndex], isExactExtent: true, isNewExtent: false);
    }

    #endregion

    public MapViewModelBase()
    {
        //this.MapProviders = new Dictionary<string, Func<TileType, IMapProvider>>()
        //{
        //    {"GOOGLE", tileType => new GoogleMapProvider(tileType) },
        //    {"BING", tileType => new BingMapProvider(tileType) },
        //    {"NOKIA", tileType => new NokiaMapProvider(tileType) },
        //    {"OPENSTREETMAP", tileType => new OsmMapProvider(tileType) },
        //    {"WAZE", tileType => new WazeMapProvider(tileType) },
        //};

        _drawingItems.CollectionChanged += (sender, e) =>
        {
            RaisePropertyChanged(nameof(CanMoveDrawingItemDown));
            RaisePropertyChanged(nameof(CanMoveDrawingItemUp));
        };

        MapProviders = TileMapProviderFactory.GetDefault();

        MapPanel = new MapInfoViewModel();

        MapPanel.CurrentEditingPoint = new NotifiablePoint(0, 0, param =>
          {
              if (CurrentEditingLayer == null)
              {
                  Debug.WriteLine($"Exception at map presenter. current editing layer is null!");
                  return;
              }

              if (MapPanel.CurrentWebMercatorEditingPoint.IsNaN())
                  return;

              CurrentEditingLayer.ChangeCurrentEditingPoint(MapPanel.CurrentWebMercatorEditingPoint);

          });

        CoordinatePanel = new CoordinatePanelViewModel();
    }


    public virtual Task InitializeAsync() => Task.CompletedTask;

    public virtual void Initialize(IDialogService dialogService, Action<Point> requestShowGoToView, Action<ILayer> requestShowSymbologyView)
    {
        //this.DialogService = new IRI.Maptor.Jab.Controls.Services.Dialog.DefaultDialogService(ownerWindow);
        this.DialogService = dialogService;

        //this.RequestShowGoToView = IRI.Maptor.Jab.Controls.Common.Defaults.DefaultActions.GetDefaultGoToAction(ownerWindow, this);
        //this.RequestShowSymbologyView = layer => Common.Defaults.DefaultActions.GetDefaultShowSymbologyView(ownerWindow, layer, this);
        this.RequestShowGoToView = requestShowGoToView;
        this.RequestShowSymbologyView = requestShowSymbologyView;

        this.RequestClearAll = this.ClearAll;

        this.MapSettings.BaseMapCacheDirectory = Environment.CurrentDirectory + "\\Data";

        if (this.MapSettings.MinGoogleZoomLevel == 0)
            this.MapSettings.MinGoogleZoomLevel = 2;

        if (this.MapSettings.MaxGoogleZoomLevel == 0)
            this.MapSettings.MaxGoogleZoomLevel = 18;

        this.SetMapCursorSet1();

        this.RegisterMapOptions();

        this.IsPanMode = true;

        //ownerWindow.DataContext = this;
    }



    #region Actions & Funcs

    public Action RequestPrint;

    public Func<BoundingBox, int, int, Task<List<DrawingVisual>>> RequestGetAsDrawingVisual;

    public Action<System.Net.WebProxy> RequestSetProxy;

    public Func<System.Net.WebProxy> RequestGetProxy;

    public Action<MapAction, Cursor> RequestSetDefaultCursor;

    public Action<Cursor> RequestSetCursor;

    public Func<double> RequestGetActualWidth;

    public Func<double> RequestGetActualHeight;

    public Action<MapViewModelBase> RegisterAction;

    public Action<bool> RequestSetConnectedState;

    public Action RequestRefreshBaseMaps;

    public Action<TileMapProvider, bool, string, bool, Func<TileInfo, string>, double> RequestSetTileService;

    //public Action RequestRemoveAllTileServices;

    public Func<double> RequestMapScale;

    public Func<double> RequestCurrentPointScale;

    public Func<double> RequestCurrentPointGroundResolution;

    public Func<int> RequestCurrentZoomLevel;

    public Func<BoundingBox> RequestCurrentExtent;

    public Action<bool> RequestRefresh;

    public Action<ILayer> RequestRefreshLayerVisibility;

    public Action RequestIranExtent;

    public Action RequestFullExtent;

    public Action<double> RequestZoomToScale;

    public Action<Point, double> RequestZoomToPoint;

    public Action<int, Point, Action, bool> RequestZoomAndCenterToGoogleZoomLevel;

    //public Action<int> RequestZoomToGoogleZoomLevel;

    //  bool isExactExtent, bool isNewExtent
    public Action<BoundingBox, bool, bool, Action?> RequestZoomToExtent;

    public Action<Geometry<Point>> RequestZoomToFeature;

    public Action RequestEnableRectangleZoom;

    public Action RequestEnableZoomOut;

    public Action RequestPan;

    public Action<Point, Action> RequestPanTo;

    //public Action<int, Point, Action, bool> RequestZoomToLevelAndCenter;

    public Action<MapOptionsEventArgs<System.Windows.FrameworkElement>> RequestRegisterMapOptions;

    public Action RequestUnregisterMapOptions;

    public Action RequestRemoveMapOptions;


    public Action RequestCopyCurrentLocationToClipboard;

    //public Action<ILayer, int> RequestChangeLayerZIndex;


    //presenter.RequestRemoveLayer = (layer, forceRemove) =>
    //{
    //   this.ClearLayer(layer, true, forceRemove);
    //};

    //presenter.RequestRemoveLayerByName = (i) =>
    //{
    //   this.ClearLayer(i, true);
    //};

    //public Action<ILayer, bool> RequestRemoveLayer;

    //public Action<string> RequestRemoveLayerByName;

    //public Action<LayerType, bool> RequestClearLayerByType;

    //public Action<string, bool> RequestClearLayerByName;

    public Action<ILayer, bool> RequestClearLayer;

    public Action<Predicate<ILayer>, bool, bool> RequestClearLayerByCriteria;

    public Action<Predicate<LayerTag>, bool, bool> RequestClearLayerByTag;

    public Action RequestRemovePolyBezierLayers;



    public Action<List<Point>> RequestFlashPoints;

    public Action<Point> RequestFlashPoint;


    public Func<List<Geometry<Point>>, VisualParameters, string, /*Geometry, */Task> RequestSelectGeometries;

    public Func<string, List<Geometry<Point>>, VisualParameters, Task>? RequestAddGeometries;

    //public Func<GeometryLabelPairs, string, VisualParameters, LabelParameters, Task> RequestDrawGeometryLablePairs;

    public Action<SpecialPointLayer> RequestAddSpecialPointLayer;

    public Action<ILayer> RequestSetLayer;

    public Action<ILayer> RequestRemoveLayer;

    public Action<string> RequestRemoveLayerByName;

    //public Func<ILayer, Task> RequestAddLayer;
    public Action<ILayer> RequestAddLayer;

    public Func<Geometry<Point>, Geometry<Point>> RequestTransformScreenGeometryToWebMercatorGeometry;

    //public Action<string> RequestRemoveLayer;

    public Action<string, List<Point>, Geometry, bool, VisualParameters> RequestAddPolyBezier;

    public Func<DrawMode, EditableFeatureLayerOptions, bool, Task<Response<Geometry<Point>>>> RequestGetDrawingAsync;

    public Action RequestClearAll;

    public Action RequestCancelNewDrawing;

    public Action RequestFinishDrawingPart;

    public Action RequestFinishNewDrawing;

    public Action RequestCancelEdit;

    public Action RequestFinishEdit;

    public Action OnRequestShowAboutMe;

    //public Func<DrawMode, bool, Task<Geometry>> RequestMeasure;
    public Func<DrawMode, EditableFeatureLayerOptions, EditableFeatureLayerOptions, Action, Task<Response<Geometry<Point>>>> RequestMeasure;

    //public Func<string, Task> RequestAddText;

    public Action RequestCancelMeasure;

    public Action<Point> RequestShowGoToView;

    public Action<ILayer> RequestShowSymbologyView;

    public Action<IPoint> RequestAddPointToNewDrawing;

    public Func<Geometry<Point>, EditableFeatureLayerOptions, Task<Response<Geometry<Point>>>> RequestEdit;

    public Func<Geometry, VisualParameters, Task<Response<PolyBezierLayer>>> RequestGetBezier;


    public Func<Point, ObservableCollection<FeatureSet<Point>>> RequestIdentify;

    public Func<string, ObservableCollection<FeatureSet<Point>>> RequestSearch;

    public Func<Task<Response<Point>>> RequestGetPoint;

    //public Func<Func<IPoint, IPoint>> RequestGetToScreenMap;

    public Func<Matrix> RequestGetMapToScreenMatrix;

    public Func<Matrix?> RequestGetScreenToMapMatrix;

    public Func<double, double> RequestToScreenMap;


    #endregion

    public Func<Point, Point> CreateMapToScreenFunc()
    {
        var matrix = RequestGetMapToScreenMatrix?.Invoke();

        return CreateMapFunc(matrix);
        //var m11 = matrix.Value.M11;
        //var m12 = matrix.Value.M12;
        //var m21 = matrix.Value.M21;
        //var m22 = matrix.Value.M22;

        //return p => new Point(m11 * p.X + m12 * p.Y + matrix.Value.OffsetX, m21 * p.X + m22 * p.Y + matrix.Value.OffsetY);
    }

    public Func<Point, Point> CreateScreenToMapFunc()
    {
        var matrix = RequestGetScreenToMapMatrix?.Invoke();

        return CreateMapFunc(matrix);
    }

    private Func<Point, Point> CreateMapFunc(Matrix? matrix)
    {

        var m11 = matrix.Value.M11;
        var m12 = matrix.Value.M12;
        var m21 = matrix.Value.M21;
        var m22 = matrix.Value.M22;

        return p => new Point(m11 * p.X + m12 * p.Y + matrix.Value.OffsetX, m21 * p.X + m22 * p.Y + matrix.Value.OffsetY);
    }

    //*****************************************Map Providers & TileServices***********************************************
    #region Map Providers & TileServices            



    public void RemoveAllTileServices()
    {
        Clear(l => l.Type == LayerType.BaseMap, true, true);

        RefreshBaseMaps();
    }

    public void RefreshBaseMaps()
    {
        RequestRefreshBaseMaps?.Invoke();
    }

    public void AddProvider(TileMapProvider mapProvider)
    {
        //var nameInUpper = mapProviderFullName?.Provider?.EnglishTitle?.ToUpper();

        if (!MapProviders.Any(m => m == mapProvider))
        {
            MapProviders.Add(mapProvider);

            //this.MapProviders.Add(nameInUpper, t =>
            //{
            //    mapProvider.TileType = t;

            //    return mapProvider;
            //});
        }
    }

    public void RemoveAllProviders()
    {
        MapProviders = new List<TileMapProvider>();

        RemoveAllTileServices();
    }

    //public void RemoveProvider(string name)
    //{
    //    var nameInUpper = name?.ToUpper();

    //    if (MapProviders.ContainsKey(nameInUpper))
    //    {
    //        this.MapProviders.Remove(nameInUpper);
    //    }
    //}


    public async Task SetTileService(TileMapProvider baseMap, double opacity, Func<TileInfo, string> getLocalFileName)
    {
        //98.01.18
        //if (MapProviders.ContainsKey(baseMap.ProviderName))
        //{
        //    this.MapProviders.Add(baseMap.ProviderName, t => { baseMap.TileType = t; return baseMap; });
        //}

        if (!IsConnected)
        {
            await CheckInternetAccess();
        }

        RequestSetTileService?.Invoke(baseMap, MapSettings.IsBaseMapCacheEnabled, MapSettings.BaseMapCacheDirectory, !IsConnected, getLocalFileName, opacity);
    }

    //public async Task SetTileService(string provider, TileType tileType, Func<TileInfo, string> getFileName = null)
    //{
    //    provider = provider.ToUpper();

    //    if (!IsConnected)
    //    {
    //        await CheckInternetAccess();
    //    }

    //    if (!MapProviders.ContainsKey(provider))
    //    {
    //        return;
    //    }

    //    var mapProvider = MapProviders[provider](tileType);

    //    this.RequestSetTileService?.Invoke(mapProvider, MapSettings.IsBaseMapCacheEnabled, MapSettings.BaseMapCacheDirectory, !IsConnected, getFileName);

    //}

    #endregion


    //*****************************************Selected Layers & Select Geometries & DrawGeometries & Identify & FlashPoints ******************
    #region Selected Layers & Select/Draw Geometries & Identify & FlashPoints 

    public void AddSelectedLayer(SelectedLayer selectedLayer)
    {
        if (selectedLayer == null)
        {
            return;
        }

        selectedLayer.AssociatedLayer.NumberOfSelectedFeatures = selectedLayer.CountOfSelectedFeatures();

        var existingLayer = SelectedLayers?.SingleOrDefault(l => l.Id == selectedLayer?.Id);

        if (existingLayer == null)
        {
            selectedLayer.FeaturesChangedAction = ShowSelectedFeatures;

            selectedLayer.HighlightFeaturesChangedAction = ShowHighlightedFeatures;

            selectedLayer.RequestFlashSinglePoint = FlashHighlightedFeatures;

            selectedLayer.RequestZoomTo = (features, callback) =>
            {
                var extent = BoundingBox.GetMergedBoundingBox(features.Select(f => f.TheGeometry.GetBoundingBox()));

                ZoomToExtent(extent, false, true, callback);
            };

            selectedLayer.RequestRemove = () => { RemoveSelectedLayer(selectedLayer);/*this.SelectedLayers.Remove(selectedLayer);*/ };

            selectedLayer.RequestEdit = async g =>
            {
                var editResult = await EditAsync(g.TheGeometry, MapSettings.EditingOptions);

                if (editResult.HasNotNullResult())
                {
                    Feature<Point> f = new Feature<Point>(editResult.Result) { Id = g.Id };

                    selectedLayer.Update(g, f);
                }

                //Referesh
                if (selectedLayer.ShowSelectedOnMap)
                {
                    ShowSelectedFeatures(selectedLayer.GetSelectedFeatures(), selectedLayer?.AssociatedLayer?.DefaultSymbology?.StrokeThickness);
                }

                Refresh(isNewExtent: true);
            };

            //selectedLayer.RequestSave = l =>
            //{
            //    l.Save();
            //};

            SelectedLayers.Add(selectedLayer);

            CurrentLayer = selectedLayer;

            if (selectedLayer.ShowSelectedOnMap)
            {
                ShowSelectedFeatures(selectedLayer.GetSelectedFeatures(), selectedLayer.AssociatedLayer?.DefaultSymbology?.StrokeThickness);
            }
        }
        else
        {
            existingLayer.UpdateSelectedFeatures(selectedLayer.GetSelectedFeatures());

            CurrentLayer = existingLayer;

            if (selectedLayer.ShowSelectedOnMap)
            {
                ShowSelectedFeatures(selectedLayer.GetSelectedFeatures(), selectedLayer.AssociatedLayer?.DefaultSymbology?.StrokeThickness);
            }
        }
    }

    public void RemoveSelectedLayer(VectorLayer layer)
    {
        if (layer is null)
            return;

        var selectedLayer = SelectedLayers.SingleOrDefault(sl => sl.Id == layer.LayerId);

        layer.NumberOfSelectedFeatures = 0;

        if (selectedLayer != null)
        {
            SelectedLayers.Remove(selectedLayer);

            ClearLayer("__$selection", true);
            ClearLayer("__$highlight", true);
        }
    }

    private void RemoveSelectedLayer(SelectedLayer selectedLayer)
    {
        if (selectedLayer is null)
            return;

        var layer = selectedLayer.AssociatedLayer;

        layer.NumberOfSelectedFeatures = 0;

        if (selectedLayer != null)
        {
            SelectedLayers.Remove(selectedLayer);

            ClearLayer("__$selection", true);
            ClearLayer("__$highlight", true);
        }
    }

    public void RemoveSelectedLayers(Predicate<ILayer> layersToBeRemoved)
    {
        for (int i = SelectedLayers.Count - 1; i >= 0; i--)
        {
            if (layersToBeRemoved(SelectedLayers[i].AssociatedLayer))
            {
                RemoveSelectedLayer(SelectedLayers[i].AssociatedLayer);
            }
        }
    }

    private async void ShowSelectedFeatures(IEnumerable<Feature<Point>> enumerable, double? strokeThickness)
    {
        ClearLayer("__$selection", true);
        ClearLayer("__$highlight", true);

        if (enumerable == null)
        {
            return;
        }

        await DrawGeometriesAsync(
            enumerable.Select(i => i.TheGeometry).ToList(),
            "__$selection",
            VisualParameters.GetDefaultForSelection(strokeThickness));

    }

    private async void ShowHighlightedFeatures(IEnumerable<Feature<Point>> enumerable, double? strokeThickness)
    {
        ClearLayer("__$highlight", true);

        ClearLayer(LayerType.AnimatingItem, true);

        if (enumerable == null || enumerable.Count() == 0)
        {
            return;
        }

        if (enumerable?.Count() < 10 && enumerable.First().TheGeometry.Type == GeometryType.Point)
        {
            FlashPoints(enumerable.Select(e => e.TheGeometry.AsPoint()).ToList());
        }
        else
        {
            await DrawGeometriesAsync(
                enumerable.Select(i => i.TheGeometry).ToList(),
                "__$highlight",
                VisualParameters.GetDefaultForHighlight(enumerable.FirstOrDefault(), strokeThickness));
        }
    }

    public void FlashHighlightedFeatures(Feature<Point> geometry)
    {
        var point = geometry?.TheGeometry?.AsPoint();

        if (point != null)
        {
            FlashPoint(new Point(point.X, point.Y));
        }
    }


    public async Task SelectDrawingItem(DrawingItemLayer layer)
    {
        var highlightGeo = layer.Geometry;

        if (highlightGeo is null)
            return;

        var visualParameters = VisualParameters.GetDefaultForSelection(layer.DefaultSymbology?.StrokeThickness);

        //visualParameters.Visibility = layer.VisualParameters.Visibility;

        await SelectGeometryAsync(highlightGeo, visualParameters, layer.HighlightGeometryKey.ToString());
    }

    public async Task SelectGeometryAsync(Geometry<Point> geometry)
    {
        await SelectGeometriesAsync(new List<Geometry<Point>>() { geometry });
    }

    public async Task SelectGeometryAsync(Geometry<Point> geometry, VisualParameters visualParameters, string layerName/*, Geometry pointSymbol = null*/)
    {
        await SelectGeometriesAsync(new List<Geometry<Point>>() { geometry }, visualParameters, layerName/*, pointSymbol*/);
    }

    public async Task SelectGeometriesAsync(List<Geometry<Point>> geometries)
    {
        //await this.SelectGeometries(geometries, new VisualParameters(new System.Windows.Media.SolidColorBrush(Aqua), new System.Windows.Media.SolidColorBrush(Aqua), 2, .5));
        await SelectGeometriesAsync(geometries, VisualParameters.GetDefaultForSelection(null), null);
    }

    public async Task SelectGeometriesAsync(List<Geometry<Point>> geometries, VisualParameters visualParameters, string layerName/*, Geometry pointSymbol = null*/)
    {
        await RequestSelectGeometries?.Invoke(geometries, visualParameters, layerName/*, pointSymbol*/);
    }


    //public void DrawGeometryLablePairs(GeometryLabelPairs geometries, string name, VisualParameters parameters, LabelParameters labelParameters)
    //{
    //    RequestDrawGeometryLablePairs?.Invoke(geometries, name, parameters, labelParameters);
    //}

    public async Task DrawGeometriesAsync(List<Geometry<Point>> geometry, string name, VisualParameters parameters)
    {
        await RequestAddGeometries?.Invoke(name, geometry, parameters);
    }

    public async Task DrawGeometryAsync(Geometry<Point> geometry, string name, VisualParameters parameters)
    {
        await DrawGeometriesAsync(new List<Geometry<Point>> { geometry }, name, parameters);
    }


    public void FlashPoints(List<Point> points)
    {
        RequestFlashPoints?.Invoke(points);
    }

    public void FlashPoint(Point point)
    {
        RequestFlashPoint?.Invoke(point);
    }


    public ObservableCollection<FeatureSet<Point>>? Identify(Point arg)
    {
        if (RequestIdentify != null)
        {
            return RequestIdentify(arg);
        }
        else
        {
            return null;
        }
    }


    public ObservableCollection<FeatureSet<Point>>? Search(string? searchTest)
    {
        if (RequestSearch is null || string.IsNullOrWhiteSpace(searchTest))
        {
            return null;
        }
        else
        {
            return RequestSearch(searchTest);
        }

    }

    public virtual void SearchByAttribute(string? searchTest)
    {
        RemoveAllDrawingItems();

        if (string.IsNullOrWhiteSpace(searchTest))
            return;

        var result = Search(searchTest);

        SelectedLayers = new ObservableCollection<SelectedLayer>();

        foreach (var item in result)
        {
            var layer = FindLayer(item.LayerId) as VectorLayer;

            if (layer is null)
                continue;

            var fields = layer.GetFields();

            var newLayer = new SelectedLayer/*<Feature<Point>>*/(layer, fields)
            {
                ShowSelectedOnMap = true,
                Features = new ObservableCollection<Feature<Point>>(item.Features)
            };

            AddSelectedLayer(newLayer);
        }

        RemoveMapOptions();
    }

    /// <summary>
    /// Returns the point selected by the user in WGS84
    /// </summary>
    /// <returns>Selected point in WGS84</returns>
    public Task<Response<Point>> GetPoint()
    {
        return RequestGetPoint != null
            ? RequestGetPoint()
            : new Task<Response<Point>>(() => new Response<Point>() { Result = Point.NaN, IsFailed = true });
    }

    #endregion


    //*****************************************Drawing Items*********************************************************
    #region Drawing Items

    //private (DrawingItemLayer layer, int index) TryGetSingleSelectedDrawingItem()
    //{
    //    var items = this.DrawingItems?.Select((d, index) => (item: d, index: index))?.Where(d => d.item.IsSelectedInToc)?.ToList();

    //    if (items.Count != 1)
    //    {
    //        return (null, 0);
    //    }

    //    return items.First();
    //}

    public bool CanMoveDrawingItemUp
    {
        get
        {
            if (SelectedDrawingItem == null)
                return false;

            return SelectedDrawingItem.IsSelectedInToc && DrawingItems.IndexOf(SelectedDrawingItem) > 0;
        }
    }

    public bool CanMoveDrawingItemDown
    {
        get
        {
            if (SelectedDrawingItem == null)
                return false;

            return SelectedDrawingItem.IsSelectedInToc && DrawingItems.IndexOf(SelectedDrawingItem) < DrawingItems.Count - 1;
        }
    }




    List<Func<DrawingItemLayer, ILegendCommand>>? drawingItemCommands = null;

    public List<Func<DrawingItemLayer, ILegendCommand>>? DrawingItemCommands
    {
        get
        {
            if (drawingItemCommands == null)
            {
                drawingItemCommands = new List<Func<DrawingItemLayer, ILegendCommand>>()
                {
                   layer => LegendCommand.CreateZoomToExtentCommand(this, layer),
                   layer => LegendCommand.CreateRemoveDrawingItemLayer(this, layer),
                   layer => LegendCommand.CreateEditDrawingItemLayer(this, layer),
                   layer => LegendCommand.CreateExportDrawingItemLayerAsShapefile(this, layer),
                   layer => LegendCommand.CreateExportDrawingItemLayerAsGeoJson(this,layer),
                   layer => LegendToggleCommand.CreateToggleLayerLabelCommand(this, layer/*, layer.Labels*/)
                };
            }

            return drawingItemCommands;
        }
        set { drawingItemCommands = value; }
    }

    //public void AddDrawingItemCommand(Func<DrawingItemLayer, ILegendCommand> drawingItemCommandFunc)
    //{
    //    if (DrawingItemCommands is null)
    //        DrawingItemCommands = new List<Func<DrawingItemLayer, ILegendCommand>>();

    //    DrawingItemCommands.Add(drawingItemCommandFunc);
    //}


    private async Task DrawAsync(DrawMode mode)
    {
        IsPanMode = true;
        //ResetMode(mode);

        var drawingResult = await GetDrawingAsync(mode, MapSettings.DrawingOptions, true);

        if (!drawingResult.HasNotNullResult())
        {
            return;
        }

        var featureName = $"DRAWING {DrawingItems?.Count}";

        AddDrawingItem(drawingResult.Result, featureName);
    }

    public void AddDrawingItem(
        Geometry<Point> drawing,
        string? name = null,
        VisualParameters? visualParameters = null)
    {
        if (drawing.IsNullOrEmpty())
            return;

        var featureName = name ?? $"Drawing {DrawingItems?.Count}";

        var feature = new Feature<Point>(drawing, featureName);

        var simpleVisualParameters = visualParameters ?? VisualParameters.GetDefaultForDrawingItems();

        var labelParameters = VisualParameters.GetDefaultForDrawingItemLabels(simpleVisualParameters.Stroke);

        List<ISymbolizer> symbolizers = [new SimpleSymbolizer(simpleVisualParameters), new LabelSymbolizer(labelParameters, feature.LabelAttribute)];

        var drawingItemLayer = CreateDrawingItemLayer(feature, featureName, symbolizers/*, id*//*, source*/);

        if (drawingItemLayer != null)
        {
            //this.SetLayer(shapeItem.AssociatedLayer);
            AddDrawingItem(drawingItemLayer);
            //this.Refresh();
        }
    }

    public void AddDrawingItem(DrawingItemLayer item)
    {
        DrawingItems.Add(item);

        //this.AddLayer(item.AssociatedLayer);
        AddLayer(item);
    }

    public void InsertDrawingItem(int index, DrawingItemLayer item)
    {
        if (DrawingItems.Count < index)
        {
            DrawingItems.Add(item);
        }
        else
        {
            DrawingItems.Insert(index, item);
        }

        //this.AddLayer(item.AssociatedLayer);
        AddLayer(item);
    }

    public void RemoveDrawingItem(DrawingItemLayer item)
    {
        DrawingItems.Remove(item);

        //this.RemoveLayer(item.AssociatedLayer);
        ClearLayer(item, true);

        ClearLayer(item.HighlightGeometryKey.ToString(), true, true);
    }

    public void RemoveDrawingItem(string name)
    {
        var layer = DrawingItems.FirstOrDefault(d => d.LayerName == name);

        if (layer is not null)
            RemoveDrawingItem(layer);
    }

    public void RemoveAllDrawingItems()
    {
        for (int i = DrawingItems.Count - 1; i >= 0; i--)
        {
            RemoveDrawingItem(DrawingItems[i]);
        }
    }

    protected DrawingItemLayer CreateDrawingItemLayer(
        Feature<Point> drawing,
        string name,
        //VisualParameters? visualParameters = null)
        IEnumerable<ISymbolizer> symbolizers)
    {
        var drawingItemLayer = DrawingItemLayer.Create(name, drawing, symbolizers/*, id*//*, source*/);

        //drawingItemLayer.OnIsSelectedInTocChanged += (sender, e) =>
        //{
        //    if (drawingItemLayer.IsSelectedInToc)
        //    {
        //        SelectedDrawingItem = drawingItemLayer;
        //    }
        //    else if (SelectedDrawingItem == drawingItemLayer)
        //    {
        //        SelectedDrawingItem = null;
        //    }
        //};

        TrySetCommandsForDrawingItemLayer(drawingItemLayer);

        IsPanMode = true;

        return drawingItemLayer;
    }

    public void MoveDrawingItemDown()
    {
        //var item = TryGetSingleSelectedDrawingItem();

        if (SelectedDrawingItem == null)
        {
            return;
        }

        var index = DrawingItems.IndexOf(SelectedDrawingItem);

        var otherLayer = DrawingItems[index + 1];

        ReorderDrawingItems(SelectedDrawingItem, otherLayer);
    }

    public void MoveDrawingItemUp()
    {
        //var item = TryGetSingleSelectedDrawingItem();

        //if (item.layer == null)
        //    return;

        if (SelectedDrawingItem == null)
        {
            return;
        }

        var index = DrawingItems.IndexOf(SelectedDrawingItem);

        var otherLayer = DrawingItems[index - 1];

        ReorderDrawingItems(SelectedDrawingItem, otherLayer);
    }

    public void ReorderDrawingItems(DrawingItemLayer first, DrawingItemLayer second)
    {
        var newFirstIndex = DrawingItems.IndexOf(second);

        var newSecondIndex = DrawingItems.IndexOf(first);

        var tempZIndex = first.ZIndex;

        first.ZIndex = second.ZIndex;

        second.ZIndex = tempZIndex;

        RemoveDrawingItem(first);
        RemoveDrawingItem(second);

        //AddDrawingItem(first);
        //AddDrawingItem(second);

        //RemoveDrawingItem(first);

        //RemoveDrawingItem(second);

        //var newFirstLayer = CreateDrawingItemLayer(first.Feature, first.LayerName, first.Symbolizers);

        //var newSecondLayer = CreateDrawingItemLayer(second.Feature, second.LayerName, second.Symbolizers);

        if (first.ZIndex < second.ZIndex)
        {
            InsertDrawingItem(newFirstIndex, first);

            InsertDrawingItem(newSecondIndex, second);
        }
        else
        {
            InsertDrawingItem(newSecondIndex, second);

            InsertDrawingItem(newFirstIndex, first);
        }
    }

    #endregion

    //private void ChangeLayerZIndex(ILayer layer, int newZIndex)
    //{
    //    this.RequestChangeLayerZIndex?.Invoke(layer, newZIndex);
    //}

    //*****************************************General***************************************************************
    #region General

    public async Task SetIsBusy(bool isBusy)
    {
        _isBusy = isBusy;
        RaisePropertyChanged(nameof(IsBusy));

        await Wait();
    }

    private async Task Wait()
    {
        await Task.Run(async () =>
        {
            await Task.Delay(1000);
        });
    }

    public async void SetProxy(System.Net.WebProxy proxy)
    {
        RequestSetProxy?.Invoke(proxy);

        await CheckInternetAccess();
    }

    public void SetMapCursorSet1()
    {
        var zoomInCursor = new Cursor(System.Windows.Application.GetResourceStream(new Uri("/IRI.Maptor.Jab.Common;component/Assets/Cursors/MapCursorSet1/MagnifyPlusRightHanded.cur", UriKind.Relative)).Stream, false);
        SetDefaultCursor(MapAction.ZoomInRectangle, zoomInCursor);
        SetDefaultCursor(MapAction.ZoomIn, zoomInCursor);

        var zoomOutCursor = new Cursor(System.Windows.Application.GetResourceStream(new Uri("/IRI.Maptor.Jab.Common;component/Assets/Cursors/MapCursorSet1/MagnifyMinusRightHanded.cur", UriKind.Relative)).Stream, false);
        SetDefaultCursor(MapAction.ZoomOutRectangle, zoomOutCursor);
        SetDefaultCursor(MapAction.ZoomOut, zoomOutCursor);
    }

    public void SetDefaultCursor(MapAction action, Cursor cursor)
    {
        RequestSetDefaultCursor?.Invoke(action, cursor);
    }

    public void SetCursor(Cursor cursor)
    {
        RequestSetCursor?.Invoke(cursor);
    }

    public async Task CheckInternetAccess()
    {
        if (DoNotCheckInternet)
        {
            return;
        }

        var proxy = RequestGetProxy?.Invoke();

        IsConnected = await NetHelper.IsConnectedToInternet(proxy);
    }


    public void ClearLayer(ILayer layer, bool remove = true, bool forceRemove = false)
    {
        RequestClearLayer?.Invoke(layer, remove);

        RemoveSelectedLayers(l => l.LayerId == layer.LayerId);
    }

    public void ClearLayer(LayerType type, bool remove, bool forceRemove = false)
    {
        Clear(tag => tag.LayerType.HasFlag(type), remove, forceRemove);
        //this.RequestClearLayerByType?.Invoke(type, remove);
    }

    public void ClearLayer(string layerName, bool remove = true, bool forceRemove = false)
    {
        Clear(layer => layer.LayerName == layerName, remove, forceRemove);
        //this.RequestClearLayerByName?.Invoke(layerName, remove);
    }

    public void ClearAll()
    {
        Clear(new Predicate<ILayer>(l => l.CanUserDelete == true), true);

        DrawingItems.Clear();
    }


    private void Clear(Predicate<ILayer> layersToBeRemoved, bool remove, bool forceRemove = false)
    {
        RequestClearLayerByCriteria?.Invoke(layersToBeRemoved, remove, forceRemove);

        RemoveSelectedLayers(layersToBeRemoved);
    }

    //1397.08.17: potentionally error prone, do not consider removing SelectedLayers associated with the input criteria
    public void Clear(Predicate<LayerTag> criteria, bool remove, bool forceRemove = false)
    {
        RequestClearLayerByTag?.Invoke(criteria, remove, forceRemove);
    }

    //public void RemoveLayer(string layerName)
    //{
    //    this.RequestRemoveLayerByName?.Invoke(layerName);
    //}

    //public void RemoveLayer(ILayer layer, bool forceRemove = false)
    //{
    //    this.RequestRemoveLayer?.Invoke(layer, forceRemove);
    //}


    public void FireMapStatusChanged(MapStatus status)
    {
        MapStatus = status;
    }

    public void FireMapActionChanged(MapAction action)
    {
        MapAction = action;
    }

    public void FireMapExtentChanged(BoundingBox currentExtent, bool isNewExtent)
    {
        RaisePropertyChanged(nameof(CurrentExtent));

        OnMapExtentChanged?.Invoke(null, EventArgs.Empty);

        if (!isNewExtent)
            return;

        var lastExtentIndex = ExtentHistoryLength - 1;

        if (CurrentExtentIndex > 0)
        {
            // remove all newer extents
            Extents.RemoveRange(0, CurrentExtentIndex);
        }

        Extents.Insert(0, currentExtent);

        CurrentExtentIndex = 0;

        if (ExtentHistoryLength > 11)
            Extents.RemoveAt(lastExtentIndex);
    }

    public void FireMouseMove(WpfPoint currentPoint)
    {
        CurrentPoint = new Point(currentPoint.X, currentPoint.Y);

        RaisePropertyChanged(nameof(CurrentPointInverseMapScale));
        RaisePropertyChanged(nameof(CurrentPointGroundResolution));

        OnMouseMove?.Invoke(this, currentPoint);
    }

    public void FireMapMouseUp(WpfPoint currentPoint)
    {
        OnMapMouseUp?.Invoke(this, currentPoint);
    }

    public void FireZoomChanged(double mapScale)
    {
        RaisePropertyChanged(nameof(CurrentZoomLevel));

        OnZoomChanged?.Invoke(this, mapScale);

        RaisePropertyChanged(nameof(InverseMapScale));
    }

    #endregion


    //*****************************************Editing***************************************************************
    #region Editing

    public async Task<Response<Geometry<Point>>> EditAsync(Geometry<Point> geometry, EditableFeatureLayerOptions options)
    {
        //this.IsEditMode = true;

        Response<Geometry<Point>> result = null;

        options = options ?? MapSettings.EditingOptions;

        MapPanel.Options = options;

        if (RequestEdit != null)
        {
            result = await RequestEdit(geometry, options);
        }
        else
        {
            result = await new Task<Response<Geometry<Point>>>(() => ResponseFactory.Create<Geometry<Point>>(null));
        }

        //this.IsEditMode = false;

        return result;
    }

    public Task<Response<Geometry<Point>>> EditAsync(List<Point> points, bool isClosed, int srid, EditableFeatureLayerOptions options = null)
    {
        if (points == null || points.Count < 1)
        {
            return new Task<Response<Geometry<Point>>>(null);
        }

        //1397.08.15.this is already done in EditAsync(geometry,options)
        //options = options ?? this.MapSettings.EditingOptions;
        //this.MapPanel.Options = options;

        var type = points.Count == 1 ? GeometryType.Point : isClosed ? GeometryType.Polygon : GeometryType.LineString;

        Geometry<Point> geometry = new Geometry<Point>(points/*.ToArray()*/, type, srid);

        return EditAsync(geometry, options);
    }

    protected void CancelEdit()
    {
        //this.IsEditMode = false;

        RequestCancelEdit?.Invoke(); //this is called in MapViewer

        OnCancelEdit?.Invoke(null, EventArgs.Empty); //this is called in the apps
    }

    protected void FinishEdit()
    {
        //this.IsEditMode = false;

        RequestFinishEdit?.Invoke(); //this is called in MapViewer

        OnFinishEdit?.Invoke(null, EventArgs.Empty); //this is called in the apps
    }

    #endregion


    //*****************************************Layer Management******************************************************
    #region Layer Management

    public void RefreshLayerVisibility(ILayer layer)
    {
        RequestRefreshLayerVisibility?.Invoke(layer);
    }

    public void AddLayer(SpecialPointLayer layer)
    {
        RequestAddSpecialPointLayer?.Invoke(layer);
    }

    public void SetLayer(ILayer layer)
    {
        TrySetCommands(layer);

        RequestSetLayer?.Invoke(layer);
    }

    public void UnSetLayer(ILayer layer)
    {
        RequestRemoveLayer?.Invoke(layer);
    }

    public void RegisterLayerWidthMap(VectorLayer layer)
    {
        layer.RequestChangeSymbology = l => RequestShowSymbologyView?.Invoke(l);
    }

    protected void TrySetCommands(ILayer layer)
    {
        if (layer.IsGroupLayer && !layer.SubLayers.IsNullOrEmpty())
        {
            foreach (var subLayer in layer.SubLayers)
            {
                TrySetCommands(subLayer);
            }

            return;
        }

        if (layer is VectorLayer)
        {
            if (layer is DrawingItemLayer drawingItemLayer)
            {
                if (drawingItemLayer.IsSpecialLayer())
                {
                    var commands = new List<ILegendCommand>();

                    foreach (var item in LegendCommand.GetDefaultTextLayerCommands())
                    {
                        commands.Add(item(this, drawingItemLayer));
                    }

                    layer.Commands = commands;

                    return;
                }
            }

            if (!(layer?.Commands?.Count > 0))
            {
                var commands = new List<ILegendCommand>();

                foreach (var item in LegendCommand.GetDefaultVectorLayerCommands/*<Feature<Point>>*/())
                {
                    commands.Add(item(this, layer));
                }

                layer.Commands = commands;
            }

            if (!(layer?.FeatureTableCommands?.Count > 0))
            {
                var commands = new List<IFeatureTableCommand>();

                foreach (var item in DefaultVectorLayerFeatureTableCommands)
                {
                    commands.Add(item(this));
                }

                layer.FeatureTableCommands = commands;
            }

            if ((layer as VectorLayer).RequestChangeSymbology == null)
            {
                (layer as VectorLayer).RequestChangeSymbology = l => RequestShowSymbologyView?.Invoke(l);
            }
        }
        else if (layer.Type == LayerType.Raster || layer.Type == LayerType.ImagePyramid)
        {
            if (!(layer?.Commands?.Count > 0))
            {
                layer.Commands = new List<ILegendCommand>()
                {
                    LegendCommand.CreateZoomToExtentCommand(this, layer),
                    LegendCommand.CreateRemoveLayer(this, layer),
                };
            }
        }
    }

    protected void TrySetCommandsForDrawingItemLayer(DrawingItemLayer layer)
    {
        layer.Commands = DrawingItemCommands?.Select(dic => dic(layer))?.ToList();

        //1398.11.14
        //layer.Commands = new List<ILegendCommand>()
        //        {
        //            LegendCommand.CreateZoomToExtentCommand(this, layer),
        //            LegendCommand.CreateRemoveDrawingItemLayer(this, layer),
        //            LegendCommand.CreateEditDrawingItemLayer(this, layer),
        //            LegendCommand.CreateExportDrawingItemLayerAsShapefile(this, layer),
        //        };

        layer.RequestHighlightGeometry = async di =>
         {
             if (di.CanShowHighlightGeometry())
             {
                 await SelectDrawingItem(di);
             }
             else
             {
                 // 1399.12.27
                 //ClearLayer(LayerType.Selection, true, true);

                 ClearLayer(di.HighlightGeometryKey.ToString(), true, true);
             }
         };

        layer.RequestChangeVisibility = async di =>
        {
            DrawingItemLayer drawingLayer = (DrawingItemLayer)di;

            RefreshLayerVisibility(di);

            if (drawingLayer.CanShowHighlightGeometry())
            {
                await SelectDrawingItem(drawingLayer);
            }
            else
            {
                ClearLayer(drawingLayer.HighlightGeometryKey.ToString(), true, true);
            }
        };

        if (layer.RequestChangeSymbology == null)
        {
            layer.RequestChangeSymbology = l => RequestShowSymbologyView?.Invoke(l);
        }
    }


    public void AddLayer(ILayer layer)
    {
        TrySetCommands(layer);

        RequestAddLayer?.Invoke(layer);
    }


    public void RemoveLayer(string layerName)
    {
        RequestRemoveLayerByName?.Invoke(layerName);
    }


    // 1400.03.23
    /// <summary>
    /// Add Screen Geometry as drawing item and returns map geometry
    /// </summary>
    /// <param name="screenGeometry"></param>
    /// <returns></returns>
    public Geometry<Point> TransformScreenGeometryToWebMercatorGeometry(Geometry<Point> screenGeometry)
    {
        return RequestTransformScreenGeometryToWebMercatorGeometry?.Invoke(screenGeometry);
    }

    public ILayer FindLayer(Guid layerId)
    {
        var result = GetAllLayers(Layers).SingleOrDefault(l => l.LayerId == layerId);

        return result;
    }

    public ILayer? FindLayerByAuxilaryId(int? layerId)
    {
        if (!layerId.HasValue)
            return null;

        var allLayers = GetAllLayers(Layers);

        return allLayers.FirstOrDefault(l => l.AuxilaryId == layerId);
    }


    public List<ILayer> GetAllLayers(IEnumerable<ILayer>? layers)
    {
        if (layers is null || layers.IsNullOrEmpty())
            return new List<ILayer>();

        var result = layers.Where(l => l.IsGroupLayer == false).ToList();

        result.AddRange(layers.Where(l => l.IsGroupLayer).SelectMany(l => GetAllLayers((l as GroupLayer)?.SubLayers)));

        return result;
    }

    #endregion


    //*****************************************PolyBezier************************************************************
    #region PolyBezier

    public void AddPolyBezierLayer(string name, List<Point> bezierPoints, Geometry symbol, VisualParameters decorationVisuals, bool showSymbolOnly)
    {
        RequestAddPolyBezier?.Invoke(name, bezierPoints, symbol, showSymbolOnly, decorationVisuals);
    }

    public void RemovePolyBezierLayers()
    {
        RequestRemovePolyBezierLayers?.Invoke();
    }

    protected Task<Response<PolyBezierLayer>> GetBezier(Geometry symbol, VisualParameters decorationVisual)
    {
        if (RequestGetBezier != null)
        {
            return RequestGetBezier(symbol, decorationVisual);
        }
        else
        {
            return new Task<Response<PolyBezierLayer>>(() => new Response<PolyBezierLayer>() { IsFailed = true });
        }
    }

    #endregion


    //*****************************************Zoom******************************************************************
    #region Zoom

    public void ZoomAndCenterToGoogleZoomLevel(int zoomLevel, Point centerMapPoint, Action callback = null, bool withAnimation = true)
    {
        RequestZoomAndCenterToGoogleZoomLevel?.Invoke(zoomLevel, centerMapPoint, callback, withAnimation);
    }

    public void EnableRectangleZoomIn()
    {
        RequestEnableRectangleZoom?.Invoke();
    }

    public void EnableZoomOut()
    {
        RequestEnableZoomOut?.Invoke();
    }

    public void GoToIranExtent()
    {
        RequestIranExtent?.Invoke();
    }

    public void FullExtent()
    {
        RequestFullExtent?.Invoke();
    }

    public void Zoom(double mapScale)
    {
        RequestZoomToScale?.Invoke(mapScale);
    }

    public void Zoom(double mapScale, Point center)
    {
        RequestZoomToPoint?.Invoke(center, mapScale);
    }

    //public void ZoomToGoogleZoomLevel(int googleZoomLevel)
    //{
    //    this.RequestZoomToGoogleZoomLevel?.Invoke(googleZoomLevel);
    //}

    //public void ZoomToGoogleScale(int googleZoomLevel, Point mapCenter, Action callback)
    //{
    //    this.RequestZoomAndCenterToGoogleZoomLevel?.Invoke(googleZoomLevel, mapCenter, callback, false);
    //}

    public void ZoomToExtent(BoundingBox boundingBox, bool isExactExtent, bool isNewExtent, Action? callback = null)
    {
        RequestZoomToExtent?.Invoke(boundingBox, isExactExtent, isNewExtent, callback);
    }

    public void Zoom(Geometry<Point> geometry)
    {
        RequestZoomToFeature?.Invoke(geometry);
    }

    #endregion


    //*****************************************Pan*******************************************************************
    #region Pan

    public void Pan()
    {
        RequestPan?.Invoke();
    }

    public void PanTo(Point point, Action callback)
    {
        RequestPanTo?.Invoke(point, callback);
    }

    public void PanToGeographicPoint(Point point, Action callback = null)
    {
        var webMercatorPoint = MapProjects.GeodeticWgs84ToWebMercator(point);

        PanTo(webMercatorPoint, callback);
    }

    #endregion


    //*****************************************Drawing***************************************************************
    #region Drawing

    public async Task<Response<Geometry<Point>>> GetDrawingAsync(DrawMode mode, EditableFeatureLayerOptions options = null, bool display = true)
    {
        //this.IsDrawMode = true;

        options = options ?? MapSettings.DrawingOptions;

        MapPanel.Options = options;

        var result = await RequestGetDrawingAsync?.Invoke(mode, options, display);

        //this.IsDrawMode = false;

        return result;
        //return tcs.Task;
    }

    protected void CancelNewDrawing()
    {
        RequestCancelNewDrawing?.Invoke(); //this is called in MapViewer

        OnCancelNewDrawing?.Invoke(null, EventArgs.Empty); //this is called in the apps
    }

    private void FinishNewDrawing()
    {
        RequestFinishNewDrawing?.Invoke(); //this is called in MapViewer

        OnFinishNewDrawing?.Invoke(null, EventArgs.Empty); //this is called in the apps
    }

    private void FinishDrawingPart()
    {
        RequestFinishDrawingPart?.Invoke();
    }

    protected void DeleteDrawing()
    {
        //this.IsEditMode = false;

        RequestCancelEdit?.Invoke(); //this is called in MapViewer

        OnDeleteDrawing?.Invoke(null, EventArgs.Empty); //this is called in the apps
    }

    private void AddPointToNewDrawing()
    {
        if (MapPanel.CurrentWebMercatorEditingPoint.IsNaN())
            return;

        RequestAddPointToNewDrawing?.Invoke(MapPanel.CurrentWebMercatorEditingPoint);
    }

    #endregion


    //*****************************************RightClickOptions*****************************************************
    #region RightClickOptions

    protected void RegisterRightClickMapOptions(System.Windows.FrameworkElement view, ILocateable dataContext)
    {
        RequestRegisterMapOptions?.Invoke(new MapOptionsEventArgs<System.Windows.FrameworkElement>(view, dataContext));
    }

    protected void UnregisterRightClickMapOptions()
    {
        RequestUnregisterMapOptions?.Invoke();
    }

    protected void RemoveMapOptions()
    {
        RequestRemoveMapOptions?.Invoke();
    }



    #endregion


    //*****************************************Measure***************************************************************
    #region Measure

    protected async Task<Response<Geometry<Point>>> Measure(DrawMode mode, Action action = null)
    {
        //this.IsMeasureMode = true;
        try
        {
            MapPanel.Options = MapSettings.DrawingMeasureOptions;

            var result = await RequestMeasure?.Invoke(mode, MapSettings.DrawingMeasureOptions, MapSettings.EditingMeasureOptions, action);

            //this.IsMeasureMode = false;

            return result;


        }
        catch (Exception ex)
        {
            throw;
        }
    }

    protected void CancelMeasure()
    {
        //this.IsMeasureMode = false;

        RequestCancelMeasure?.Invoke();
    }

    #endregion


    #region AddText

    protected async Task AddTextToMap()
    {
        try
        {
            var response = await GetPoint();

            if (!response.HasNotNullResult())
                return;

            var text = "sample text!";

            var drawingItemLayer = DrawingItemLayer.CreateSpecialLayer("Text",
            [
                new Locateable(response.Result, AncherFunctionHandlers.BottomCenter){ Element = new TextboxMarker(){ LabelValue=text} }
            ]);

            //drawingItemLayer.OnIsSelectedInTocChanged += (sender, e) =>
            //{
            //    if (drawingItemLayer.IsSelectedInToc)
            //    {
            //        SelectedDrawingItem = drawingItemLayer;
            //    }
            //    else if (SelectedDrawingItem == drawingItemLayer)
            //    {
            //        SelectedDrawingItem = null;
            //    }
            //};

            //TrySetCommandsForDrawingItemLayer(drawingItemLayer);

            AddDrawingItem(drawingItemLayer);

            return;
        }
        catch (Exception)
        {
            throw;
        }
    }

    #endregion

    public void UpdateCurrentEditingPoint(Point webMercatorPoint)
    {
        MapPanel.UpdateCurrentEditingPoint(webMercatorPoint);
    }

    public void Print()
    {
        RequestPrint?.Invoke();
    }

    public async Task ClipAndExportMapAsPngAsync(object owner)
    {
        // select a rectangle 
        var polygon = await GetDrawingAsync(DrawMode.Polygon);

        if (polygon.IsNullOrEmpty())
            return;

        var boundingBox = polygon.Result.GetBoundingBox();

        await ExportMapAsPngAsync(owner, boundingBox);
    }

    public async Task ExportMapAsPngAsync(object owner)
    {
        var boundingBos = PrintArea.IsNaN() ? CurrentExtent : PrintArea;

        await ExportMapAsPngAsync(owner, boundingBos);
    }

    protected async Task ExportMapAsPngAsync(object owner, BoundingBox boundingBox)
    {
        if (RequestGetAsDrawingVisual is null)
            return;

        var fileName = await DialogService.ShowSaveFileDialogAsync("*.png|*.png", owner);

        if (string.IsNullOrWhiteSpace(fileName))
            return;

        var toScreenMap = CreateMapToScreenFunc();

        var width = (int)RequestToScreenMap(boundingBox.Width);
        var height = (int)RequestToScreenMap(boundingBox.Height);

        var visuals = await RequestGetAsDrawingVisual(boundingBox, width, height);

        ImageUtility.MergeAndSave(fileName, visuals, width, height, new TiffBitmapEncoder());

        //RenderTargetBitmap image = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        //foreach (var drawingVisual in visuals)
        //{
        //    image.Render(drawingVisual);
        //}
        //var frame = BitmapFrame.Create(image);
        ////PngBitmapEncoder pngImage = new PngBitmapEncoder();
        //TiffBitmapEncoder tiffImage = new TiffBitmapEncoder();
        //tiffImage.Frames.Add(frame);
        //using (System.IO.Stream stream = System.IO.File.Create(fileName))
        //{
        //    tiffImage.Save(stream);
        //}
    }

    public async Task SetPrintAreaAsync()
    {
        // select a rectangle 
        var polygon = await GetDrawingAsync(DrawMode.Polygon);

        if (polygon.IsNullOrEmpty())
            return;

        var boundingBox = polygon.Result.GetBoundingBox();

        PrintArea = boundingBox;
    }

    public void Refresh(bool isNewExtent)
    {
        RequestRefresh?.Invoke(isNewExtent);
    }



    //*****************************************General**************************************************************
    #region Shapefile/Worldfile/GeoJson

    const string _error = "خطا";
    const string _fileLockedError = "فایل در حال استفاده توسط برنامه دیگری است. لطفا فایل را ببندید و دوباره تلاش کنید.";

    public virtual async Task AddShapefile(object owner, int? maxSizeInKB)
    {
        IsBusy = true;

        var fileName = await DialogService.ShowOpenFileDialogAsync("shapefile|*.shp", owner);

        if (!File.Exists(fileName))
        {
            IsBusy = false;

            return;
        }

        FileInfo info = new FileInfo(fileName);

        if (maxSizeInKB.HasValue && info.Length / 10000.0 > maxSizeInKB) //5k
        {
            await DialogService.ShowMessageAsync("حجم فایل انتخابی بیش از حد مجاز است", "خطا", owner);

            return;
        }

        await AddShapefile(fileName, owner);
    }

    public async Task AddShapefile(string fileName, object owner)
    {
        try
        {
            var dataSource = await ShapefileDataSourceFactory.CreateAsync(fileName, new WebMercator());

            var vectorLayer = new VectorLayer(Path.GetFileNameWithoutExtension(fileName),
                                dataSource,
                                [SimpleSymbolizer.Create(null, BrushHelper.PickBrush(), 3, 1)], //new VisualParameters(null, BrushHelper.PickBrush(), 3, 1),
                                LayerType.VectorLayer,
                                RenderMode.Default,
                                RasterizationMethod.GdiPlus,
                                ScaleInterval.All);

            AddLayer(vectorLayer);
        }
        catch (Exception ex)
        {
            await DialogService.ShowMessageAsync(ex.Message, _error, owner);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public virtual async Task AddKmlfile(object owner, int? maxSizeInKB)
    {
        IsBusy = true;

        var fileName = await DialogService.ShowOpenFileDialogAsync("Keyhole Markup Language (KML)|*.kml", owner);

        if (!File.Exists(fileName))
        {
            IsBusy = false;

            return;
        }

        FileInfo info = new FileInfo(fileName);

        if (maxSizeInKB.HasValue && info.Length / 10000.0 > maxSizeInKB) //5k
        {
            await DialogService.ShowMessageAsync("حجم فایل انتخابی بیش از حد مجاز است", "خطا", owner);

            return;
        }

        await AddKmlfile(fileName, owner);
    }

    public async Task AddKmlfile(string fileName, object owner)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fileName) || !File.Exists(fileName))
            {
                throw new FileNotFoundException($"KML file '{fileName}' was not found.", fileName);
            }

            List<Feature<Point>> features;

            try
            {
                var kmlFeatures = KmlReader.ReadFeaturesFromFile(fileName);
                features = kmlFeatures.ToFeatures();
            }
            catch
            {
                features = new List<Feature<Point>>();
            }

            if (features.IsNullOrEmpty())
            {
                try
                {
                    var geometries = KmlReader.ReadFromFile(fileName);
                    features = geometries.ToFeatures();
                }
                catch
                {
                    features = new List<Feature<Point>>();
                }
            }

            if (features.IsNullOrEmpty())
            {
                await DialogService.ShowMessageAsync("هیچ عارضه‌ای در فایل KML یافت نشد.", _error, owner);
                return;
            }

            features = features.Select(f => f.Transform(MapProjects.GeodeticWgs84ToWebMercator<Point>, SridHelper.WebMercator)).ToList();

            var dataSource = new MemoryDataSource(features);
            var geometryType = features.First().TheGeometry.Type;
            var symbolizers = features.CreateSymbolizersFromKml(geometryType);

            var vectorLayer = new VectorLayer(Path.GetFileNameWithoutExtension(fileName),
                                dataSource,
                                symbolizers,
                                LayerType.VectorLayer,
                                RenderMode.Default,
                                RasterizationMethod.GdiPlus,
                                ScaleInterval.All);

            AddLayer(vectorLayer);
        }
        catch (IOException)
        {
            await DialogService.ShowMessageAsync(_fileLockedError, _error, owner);
        }
        catch (UnauthorizedAccessException)
        {
            await DialogService.ShowMessageAsync(_fileLockedError, _error, owner);
        }
        catch (Exception ex)
        {
            await DialogService.ShowMessageAsync(ex.Message, _error, owner);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public virtual async Task AddDxffile(object owner, int? maxSizeInKB)
    {
        IsBusy = true;

        var fileName = await DialogService.ShowOpenFileDialogAsync("Drawing Exchange Format (DXF)|*.dxf", owner);

        if (!File.Exists(fileName))
        {
            IsBusy = false;

            return;
        }

        FileInfo info = new FileInfo(fileName);

        if (maxSizeInKB.HasValue && info.Length / 10000.0 > maxSizeInKB) //5k
        {
            await DialogService.ShowMessageAsync("حجم فایل انتخابی بیش از حد مجاز است", "خطا", owner);

            return;
        }

        await AddDxffile(fileName, owner);
    }

    public async Task AddDxffile(string fileName, object owner)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fileName) || !File.Exists(fileName))
                throw new FileNotFoundException($"DXF file '{fileName}' was not found.", fileName);

            var geometries = DxfReader.ReadFromFile(fileName, SridHelper.GeodeticWGS84);

            if (geometries == null || geometries.IsNullOrEmpty())
            {
                await DialogService.ShowMessageAsync("هیچ عارضه‌ای در فایل DXF یافت نشد.", _error, owner);
                return;
            }

            if (geometries.Count == 0)
            {
                await DialogService.ShowMessageAsync("هیچ عارضه‌ای در فایل DXF یافت نشد.", _error, owner);
                return;
            }

            if (geometries.Any(g => g.Srid == 0))
            {
                await DialogService.ShowMessageAsync("سیستم مختصات DXF یافت نشد.", _error, owner);
                return;
            }

            var groups = geometries.GroupBy(g => g.Type).ToList();

            // create the parent group layer
            GroupLayer groupLayer = new GroupLayer(Path.GetFileNameWithoutExtension(fileName));

            foreach (var group in groups)
            {
                var features = group.Select(g => g.AsFeature()).ToList();

                if (features.IsNullOrEmpty())
                {
                    await DialogService.ShowMessageAsync("هیچ عارضه‌ای در فایل DXF یافت نشد.", _error, owner);
                    return;
                }

                features = features.Select(f => f.Project(new WebMercator())).ToList();

                var dataSource = new MemoryDataSource(features);
                var geometryType = features.First().TheGeometry.Type;
                var symbolizers = new List<ISymbolizer> { SimpleSymbolizer.Create(null, BrushHelper.PickBrush(), 3, 1) };

                var vectorLayer = new VectorLayer($"{Path.GetFileNameWithoutExtension(fileName)}-{group.Key}",
                                    dataSource,
                                    symbolizers,
                                    LayerType.VectorLayer,
                                    RenderMode.Default,
                                    RasterizationMethod.GdiPlus,
                                    ScaleInterval.All);

                //AddLayer(vectorLayer);

                groupLayer.AddSubLayer(vectorLayer);
            }

            AddLayer(groupLayer);
        }
        catch (IOException)
        {
            await DialogService.ShowMessageAsync(_fileLockedError, _error, owner);
        }
        catch (UnauthorizedAccessException)
        {
            await DialogService.ShowMessageAsync(_fileLockedError, _error, owner);
        }
        catch (Exception ex)
        {
            await DialogService.ShowMessageAsync(ex.Message, _error, owner);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public virtual async Task AddWebMercatorWorldfile(object owner)
    {
        IsBusy = true;

        try
        {
            var fileName = await DialogService.ShowOpenFileDialogAsync("Worldfile|*.bmp;*.jpg;*.jpeg;*.png;*.tif;*.tiff", owner);

            if (!File.Exists(fileName))
            {
                IsBusy = false;

                return;
            }

            await AddWorldfile(fileName, SridHelper.WebMercator, owner);
        }
        catch (IOException)
        {
            await DialogService.ShowMessageAsync(_fileLockedError, _error, owner);
        }
        catch (UnauthorizedAccessException)
        {
            await DialogService.ShowMessageAsync(_fileLockedError, _error, owner);
        }
        catch (Exception ex)
        {
            await DialogService.ShowMessageAsync(ex.Message, _error, owner);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public virtual async Task AddWorldfile(object owner)
    {
        IsBusy = true;

        var fileName = await DialogService.ShowOpenFileDialogAsync("Worldfile|*.bmp;*.jpg;*.jpeg;*.png;*.tif;*.tiff", owner);

        if (!File.Exists(fileName))
        {
            IsBusy = false;

            return;
        }

        await AddWorldfile(fileName, SridHelper.GeodeticWGS84, owner);
    }

    public virtual async Task AddWorldfile(string fileName, int srid, object owner)
    {
        try
        {
            var dataSource = GeoRasterFileDataSource.Create(fileName, srid);

            if (dataSource == null)
            {
                return;
            }

            var rasterLayer = new RasterLayer(
                dataSource,
                Path.GetFileNameWithoutExtension(fileName),
                ScaleInterval.All,
                false,
                false,
                System.Windows.Visibility.Visible,
                .9);

            //this.SetLayer(rasterLayer);

            AddLayer(rasterLayer);

            //this.Refresh();

        }
        catch (IOException)
        {
            await DialogService.ShowMessageAsync(_fileLockedError, _error, owner);
        }
        catch (UnauthorizedAccessException)
        {
            await DialogService.ShowMessageAsync(_fileLockedError, _error, owner);
        }
        catch (Exception ex)
        {
            await DialogService.ShowMessageAsync(ex.Message, _error, owner);
        }
        finally
        {
            IsBusy = false;
        }

    }

    public virtual async Task AddZippedImagePyramid(object owner)
    {
        try
        {
            IsBusy = true;

            var fileName = await DialogService.ShowOpenFileDialogAsync("Image Pyramid file|*.pyrmd", owner);

            if (!File.Exists(fileName))
            {
                IsBusy = false;

                return;
            }

            var rasterLayer = new RasterLayer(new ZippedImagePyramidDataSource(fileName),
                Path.GetFileNameWithoutExtension(fileName),
                ScaleInterval.All,
                false,
                true,
                System.Windows.Visibility.Visible,
                1);

            AddLayer(rasterLayer);
        }
        catch (IOException)
        {
            await DialogService.ShowMessageAsync(_fileLockedError, _error, owner);
        }
        catch (UnauthorizedAccessException)
        {
            await DialogService.ShowMessageAsync(_fileLockedError, _error, owner);
        }
        catch (Exception ex)
        {
            await DialogService.ShowMessageAsync(ex.Message, _error, owner);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// ستون اول و دوم طول و عرض جغرافیایی
    /// باقی ستون‌ها هم اطلاعات توصیفی
    /// </summary>
    /// <param name="owner"></param>
    /// <returns></returns>
    public virtual async Task AddCsvFile(object owner, bool isLongitudeFirst)
    {
        try
        {
            IsBusy = true;

            var fileName = await DialogService.ShowOpenFileDialogAsync("CSV file (*.csv)|*.csv|Text file (*.txt)|*.txt", owner);

            if (!File.Exists(fileName))
            {
                IsBusy = false;

                return;
            }

            GeoJsonFeatureSet featureSet = GeoJsonFeatureSet.CsvToPointGeoJson(fileName, false);

            if (featureSet.Features.IsNullOrEmpty())
                return;

            var features = featureSet.Features.Select(f => f.AsFeature(true, SrsBases.WebMercator)).ToList();

            var dataSource = new MemoryDataSource(features);

            AddLayer(new VectorLayer("", dataSource, VisualParameters.CreateNew(0.9), LayerType.VectorLayer, RenderMode.Default, RasterizationMethod.GdiPlus, ScaleInterval.All));

        }
        catch (IOException)
        {
            await DialogService.ShowMessageAsync(_fileLockedError, _error, owner);
        }
        catch (UnauthorizedAccessException)
        {
            await DialogService.ShowMessageAsync(_fileLockedError, _error, owner);
        }
        catch (Exception ex)
        {
            await DialogService.ShowMessageAsync(ex.Message, _error, owner);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// ستون اول و دوم طول و عرض جغرافیایی
    /// باقی ستون‌ها هم اطلاعات توصیفی
    /// </summary>
    /// <param name="owner"></param>
    /// <param name="isLongitudeFirst"></param>
    /// <returns></returns>
    public virtual async Task AddTsvFile(object owner, bool isLongitudeFirst)
    {
        try
        {
            IsBusy = true;

            var fileName = await DialogService.ShowOpenFileDialogAsync("TSV file (*.tsv)|*.tsv|Text file (*.txt)|*.txt", owner);

            if (!File.Exists(fileName))
            {
                IsBusy = false;

                return;
            }

            GeoJsonFeatureSet featureSet = GeoJsonFeatureSet.TsvToPointGeoJson(fileName, false);

            if (featureSet.Features.IsNullOrEmpty())
                return;

            var features = featureSet.Features.Select(f => f.AsFeature(true, SrsBases.WebMercator)).ToList();

            // todo: provide parameter instead of `null`
            var dataSource = new MemoryDataSource(features);

            AddLayer(new VectorLayer("", dataSource, VisualParameters.CreateNew(0.9), LayerType.VectorLayer, RenderMode.Default, RasterizationMethod.GdiPlus, ScaleInterval.All));

        }
        catch (IOException)
        {
            await DialogService.ShowMessageAsync(_fileLockedError, _error, owner);
        }
        catch (UnauthorizedAccessException)
        {
            await DialogService.ShowMessageAsync(_fileLockedError, _error, owner);
        }
        catch (Exception ex)
        {
            await DialogService.ShowMessageAsync(ex.Message, _error, owner);
        }
        finally
        {
            IsBusy = false;
        }
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="geoJsonFeatureSetFileName">path to GeoJsonFeatureSet file</param>
    /// <returns></returns>
    public async Task AddGeoJson(string geoJsonFeatureSetFileName, object owner)
    {
        try
        {
            //Msh.Common.Model.GeoJson.GeoJsonFeatureSet.Load(geoJsonFeatureSetFileName);

            var dataSource = OrdinaryJsonListSource.CreateFromFile<Feature<Point>>(geoJsonFeatureSetFileName, f => f);

            var vectorLayer = new VectorLayer(Path.GetFileNameWithoutExtension(geoJsonFeatureSetFileName), dataSource,
                [SimpleSymbolizer.Create(null, BrushHelper.PickBrush(), 3, 1)],
                LayerType.VectorLayer,
                RenderMode.Default,
                RasterizationMethod.GdiPlus, ScaleInterval.All);

            AddLayer/*<Feature<Point>>*/(vectorLayer);
        }
        catch (Exception ex)
        {
            await DialogService.ShowMessageAsync(ex.Message, _error, owner);
        }
        finally
        {
            IsBusy = false;
        }
    }

    #endregion


    #region Command

    #region -   Zoom/Pan Commands

    private RelayCommand _goToIranExtentCommand;
    public RelayCommand GoToIranExtentCommand
    {
        get
        {
            if (_goToIranExtentCommand == null)
            {
                _goToIranExtentCommand = new RelayCommand(param => GoToIranExtent());
            }

            return _goToIranExtentCommand;
        }
    }

    private RelayCommand _fullExtentCommand;
    public RelayCommand FullExtentCommand
    {
        get
        {
            if (_fullExtentCommand == null)
            {
                _fullExtentCommand = new RelayCommand(param => FullExtent());
            }

            return _fullExtentCommand;
        }
    }

    private RelayCommand _clearAllCommand;
    public RelayCommand ClearAllCommand

    {
        get
        {
            if (_clearAllCommand == null)
            {
                _clearAllCommand = new RelayCommand(param =>
                {
                    RequestClearAll?.Invoke();
                });
            }

            return _clearAllCommand;
        }
    }

    private RelayCommand _rectangleZoomCommand;
    public RelayCommand RectangleZoomCommand
    {
        get
        {
            if (_rectangleZoomCommand == null)
            {
                _rectangleZoomCommand = new RelayCommand(param => EnableRectangleZoomIn());
            }

            return _rectangleZoomCommand;
        }
    }

    private RelayCommand _zoomOutCommand;
    public RelayCommand ZoomOutCommand
    {
        get
        {
            if (_zoomOutCommand == null)
            {
                _zoomOutCommand = new RelayCommand(param => EnableZoomOut());
            }

            return _zoomOutCommand;
        }
    }

    private RelayCommand _panCommand;
    public RelayCommand PanCommand
    {
        get
        {
            if (_panCommand == null)
            {
                _panCommand = new RelayCommand(param => Pan());
            }

            return _panCommand;
        }
    }


    private RelayCommand _previousExtentCommand;
    public RelayCommand PreviousExtentCommand
    {
        get
        {
            if (_previousExtentCommand == null)
            {
                _previousExtentCommand = new RelayCommand(param =>
                {
                    GoToPreviousExtent();
                });
            }

            return _previousExtentCommand;
        }
    }

    private RelayCommand _nextExtentCommand;

    public RelayCommand NextExtentCommand
    {
        get
        {
            if (_nextExtentCommand == null)
            {
                _nextExtentCommand = new RelayCommand(param =>
                {
                    GoToNextExtent();
                });
            }

            return _nextExtentCommand;
        }
    }


    #endregion

    #region -   Layer Management Commands

    private RelayCommand _addShapefileCommand;
    public RelayCommand AddShapefileCommand
    {
        get
        {
            if (_addShapefileCommand == null)
            {
                _addShapefileCommand = new RelayCommand(async param =>
                {
                    await AddShapefile(param, 2000);
                });
            }
            return _addShapefileCommand;
        }
    }

    private RelayCommand _addKmlfileCommand;

    public RelayCommand AddKmlfileCommand
    {
        get
        {
            if (_addKmlfileCommand == null)
            {
                _addKmlfileCommand = new RelayCommand(async param =>
                {
                    await AddKmlfile(param, null);
                });
            }

            return _addKmlfileCommand;
        }
    }

    private RelayCommand _addDxffileCommand;

    public RelayCommand AddDxffileCommand
    {
        get
        {
            if (_addDxffileCommand == null)
            {
                _addDxffileCommand = new RelayCommand(async param =>
                {
                    await AddDxffile(param, null);
                });
            }

            return _addDxffileCommand;
        }
    }

    private RelayCommand _addWgs84WorldfileCommand;
    public RelayCommand AddWgs84WorldfileCommand
    {
        get
        {
            if (_addWgs84WorldfileCommand == null)
            {
                _addWgs84WorldfileCommand = new RelayCommand(async param => { await AddWorldfile(param); });
            }
            return _addWgs84WorldfileCommand;
        }
    }

    private RelayCommand _addWebMercatorWorldfileCommand;
    public RelayCommand AddWebMercatorWorldfileCommand
    {
        get
        {
            if (_addWebMercatorWorldfileCommand == null)
            {
                _addWebMercatorWorldfileCommand = new RelayCommand(async param => { await AddWebMercatorWorldfile(param); });
            }
            return _addWebMercatorWorldfileCommand;
        }
    }

    private RelayCommand _addZippedImagePyramidCommand;
    public RelayCommand AddZippedImagePyramidCommand
    {
        get
        {
            if (_addZippedImagePyramidCommand == null)
            {
                _addZippedImagePyramidCommand = new RelayCommand(async param => await AddZippedImagePyramid(param));
            }

            return _addZippedImagePyramidCommand;
        }
    }

    private RelayCommand _addTsvCommand;
    public RelayCommand AddTsvCommand
    {
        get
        {
            if (_addTsvCommand == null)
            {
                _addTsvCommand = new RelayCommand(async param => { await AddTsvFile(param, true); });
            }

            return _addTsvCommand;
        }
    }

    private RelayCommand _addCsvCommand;
    public RelayCommand AddCsvCommand
    {
        get
        {
            if (_addCsvCommand == null)
            {
                _addCsvCommand = new RelayCommand(async param => { await AddCsvFile(param, true); });
            }

            return _addCsvCommand;
        }
    }


    private RelayCommand _changeBaseMapCommand;

    public RelayCommand ChangeBaseMapCommand
    {
        get
        {
            if (_changeBaseMapCommand == null)
            {
                _changeBaseMapCommand = new RelayCommand(/*async*/ param =>
                {
                    try
                    {
                        //var args = param.ToString().Split(',');

                        //var provider = args[0];

                        //TileType tileType = (TileType)Enum.Parse(typeof(TileType), args[1]);

                        //this.ProviderTypeFullName = provider;
                        var provider = MapProviders.FirstOrDefault(m => m.Is(param?.ToString()));

                        /*await*/
                        SetTileBaseMap(provider/*, BaseMapOpacity*/);

                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("exception at ChangeBaseMapCommand: " + ex);
                    }
                });
            }

            return _changeBaseMapCommand;
        }
    }

    #endregion

    #region -   Measurement Commands

    private RelayCommand _measureLengthCommand;

    public RelayCommand MeasureLengthCommand
    {
        get
        {
            if (_measureLengthCommand == null)
            {
                _measureLengthCommand = new RelayCommand(async param =>
                {
                    //this.MapSettings.DrawingMeasureOptions.IsEdgeLabelVisible = param == null ? true : (bool)param;

                    await Measure(DrawMode.Polyline);
                });
            }

            return _measureLengthCommand;
        }
    }

    private RelayCommand _measureAreaCommand;

    public RelayCommand MeasureAreaCommand
    {
        get
        {
            if (_measureAreaCommand == null)
            {
                _measureAreaCommand = new RelayCommand(async param =>
                {
                    _ = await Measure(DrawMode.Polygon);
                });
            }

            return _measureAreaCommand;
        }
    }

    private RelayCommand _cancelMeasureCommand;

    public RelayCommand CancelMeasureCommand
    {
        get
        {
            if (_cancelMeasureCommand == null)
            {
                _cancelMeasureCommand = new RelayCommand(param => CancelMeasure());
            }

            return _cancelMeasureCommand;
        }
    }

    #endregion

    #region -   Drawing Commands


    private RelayCommand _addTextToMapCommand;
    public RelayCommand AddTextToMapCommand
    {
        get
        {
            if (_addTextToMapCommand == null)
            {
                _addTextToMapCommand = new RelayCommand(async param => await AddTextToMap());
            }

            return _addTextToMapCommand;
        }
    }

    private RelayCommand _drawPointCommand;
    public RelayCommand DrawPointCommand
    {
        get
        {
            if (_drawPointCommand == null)
            {
                _drawPointCommand = new RelayCommand(async param => await DrawAsync(DrawMode.Point));
            }
            return _drawPointCommand;
        }
    }

    private RelayCommand _drawPolygonCommand;
    public RelayCommand DrawPolygonCommand
    {
        get
        {
            if (_drawPolygonCommand == null)
            {
                _drawPolygonCommand = new RelayCommand(async param => await DrawAsync(DrawMode.Polygon));
            }
            return _drawPolygonCommand;
        }
    }

    private RelayCommand _drawPolylineCommand;
    public RelayCommand DrawPolylineCommand
    {
        get
        {
            if (_drawPolylineCommand == null)
            {
                _drawPolylineCommand = new RelayCommand(async param => await DrawAsync(DrawMode.Polyline));
            }
            return _drawPolylineCommand;
        }
    }


    private RelayCommand _addPointToNewDrawingCommand;
    public RelayCommand AddPointToNewDrawingCommand
    {
        get
        {
            if (_addPointToNewDrawingCommand == null)
            {
                _addPointToNewDrawingCommand = new RelayCommand(param => AddPointToNewDrawing());
            }

            return _addPointToNewDrawingCommand;
        }
    }

    private RelayCommand _cancelNewDrawingCommand;
    public RelayCommand CancelNewDrawingCommand
    {
        get
        {
            if (_cancelNewDrawingCommand == null)
            {
                _cancelNewDrawingCommand = new RelayCommand(param => CancelNewDrawing());
            }
            return _cancelNewDrawingCommand;
        }
    }

    private RelayCommand _finishDrawingPartCommand;
    public RelayCommand FinishDrawingPartCommand
    {
        get
        {
            if (_finishDrawingPartCommand == null)
            {
                _finishDrawingPartCommand = new RelayCommand(param => FinishDrawingPart());
            }
            return _finishDrawingPartCommand;
        }
    }

    private RelayCommand _finishNewDrawingCommand;
    public RelayCommand FinishNewDrawingCommand
    {
        get
        {
            if (_finishNewDrawingCommand == null)
            {
                _finishNewDrawingCommand = new RelayCommand(param => FinishNewDrawing());
            }
            return _finishNewDrawingCommand;
        }
    }

    private RelayCommand _cancelEditDrawingCommand;
    public RelayCommand CancelEditDrawingCommand
    {
        get
        {
            if (_cancelEditDrawingCommand == null)
            {
                _cancelEditDrawingCommand = new RelayCommand(param => CancelEdit());
            }
            return _cancelEditDrawingCommand;
        }
    }

    private RelayCommand _deleteDrawingCommand;
    public RelayCommand DeleteDrawingCommand
    {
        get
        {
            if (_deleteDrawingCommand == null)
            {
                _deleteDrawingCommand = new RelayCommand(param => DeleteDrawing());
            }
            return _deleteDrawingCommand;
        }

    }

    private RelayCommand _finishEditDrawingCommand;
    public RelayCommand FinishEditDrawingCommand
    {
        get
        {

            if (_finishEditDrawingCommand == null)
            {
                _finishEditDrawingCommand = new RelayCommand(param => FinishEdit());
            }
            return _finishEditDrawingCommand;
        }
    }

    #endregion

    #region -   Print & Export Commands

    private RelayCommand _printCommand;
    public RelayCommand PrintCommand
    {
        get
        {
            if (_printCommand == null)
            {
                _printCommand = new RelayCommand(param => Print());
            }

            return _printCommand;
        }
    }

    private RelayCommand _clipAndExportMapAsPngCommand;
    public RelayCommand ClipAndExportMapAsPngCommand
    {
        get
        {
            if (_clipAndExportMapAsPngCommand == null)
            {
                _clipAndExportMapAsPngCommand = new RelayCommand(async param => await ClipAndExportMapAsPngAsync(param));
            }

            return _clipAndExportMapAsPngCommand;
        }
    }

    private RelayCommand _exportMapAsPngCommand;
    public RelayCommand ExportMapAsPngCommand
    {
        get
        {
            if (_exportMapAsPngCommand == null)
            {
                _exportMapAsPngCommand = new RelayCommand(async param => await ExportMapAsPngAsync(param));
            }

            return _exportMapAsPngCommand;
        }
    }

    private RelayCommand _setPrintAreaCommand;
    public RelayCommand SetPrintAreaCommand
    {
        get
        {
            if (_setPrintAreaCommand == null)
            {
                _setPrintAreaCommand = new RelayCommand(async param => { await SetPrintAreaAsync(); });
            }

            return _setPrintAreaCommand;
        }
    }

    #endregion


    // ******************** Others *********************
    // *************************************************
    private RelayCommand _searchByAttributeCommand;
    public RelayCommand SearchByAttributeCommand
    {
        get
        {
            if (_searchByAttributeCommand == null)
            {
                _searchByAttributeCommand = new RelayCommand(param => SearchByAttribute(param?.ToString()));
            }

            return _searchByAttributeCommand;
        }
    }

    private RelayCommand _goToCommand;
    public RelayCommand GoToCommand
    {
        get
        {
            if (_goToCommand == null)
            {
                _goToCommand = new RelayCommand(param => RequestShowGoToView?.Invoke(CurrentExtent.Center));
            }

            return _goToCommand;
        }
    }

    private RelayCommand _checkInternetAccessCommand;
    public RelayCommand CheckInternetAccessCommand
    {
        get
        {
            if (_checkInternetAccessCommand == null)
            {
                _checkInternetAccessCommand = new RelayCommand(async param => { await CheckInternetAccess(); });
            }

            return _checkInternetAccessCommand;
        }
    }


    #endregion


    #region Drawing Items Commands

    private RelayCommand _addGeoJsonToDrawingItemsCommand;
    public RelayCommand AddGeoJsonToDrawingItemsCommand
    {
        get
        {
            if (_addGeoJsonToDrawingItemsCommand == null)
            {
                _addGeoJsonToDrawingItemsCommand = new RelayCommand(async param =>
                {
                    try
                    {
                        var fileName = await DialogService.ShowOpenFileDialogAsync("*.json|*.json", param);

                        if (string.IsNullOrWhiteSpace(fileName))
                            return;

                        var featureSet = GeoJsonFeatureSet.Load(fileName);

                        if (featureSet.Features.IsNullOrEmpty())
                            return;

                        var features = featureSet.Features.Select(f => f.AsFeature(true, SrsBases.WebMercator)).ToList();

                        //var dataSource = GeoJsonSource<SqlFeature>.CreateFromFile(fileName, f => f);
                        //var dataSource = new MemoryDataSource(
                        //    features/*,f => f.Label,null*/);

                        //var geometries = dataSource.GetAsFeatureSet()?.Features;

                        if (features.IsNullOrEmpty())
                            return;

                        if (features.Count != 1)
                        {
                            await DialogService.ShowMessageAsync("فایل جی‌سان حاوی تک عارضه باید باشد", _error, param);

                            return;
                        }

                        AddDrawingItem(features.First().TheGeometry, Path.GetFileNameWithoutExtension(fileName)/*, null, int.MinValue*//*, dataSource*/);
                    }
                    catch (IOException)
                    {
                        await DialogService.ShowMessageAsync(_fileLockedError, _error, param);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        await DialogService.ShowMessageAsync(_fileLockedError, _error, param);
                    }
                    catch (Exception ex)
                    {
                        await DialogService.ShowMessageAsync(ex.Message, _error, param);
                    }
                });
            }

            return _addGeoJsonToDrawingItemsCommand;
        }
    }

    private RelayCommand _addLongLatTxtToDrawingItemsCommand;
    public RelayCommand AddLongLatTxtToDrawingItemsCommand
    {
        get
        {
            if (_addLongLatTxtToDrawingItemsCommand == null)
            {
                _addLongLatTxtToDrawingItemsCommand = new RelayCommand(async param =>
                {
                    try
                    {
                        var fileName = await DialogService.ShowOpenFileDialogAsync("*.csv|*.csv", param);

                        if (string.IsNullOrWhiteSpace(fileName))
                            return;

                        var wgsPoints = IOHelper.ReadAllPoints(fileName, IOHelper.CsvDelimiterChar);

                        if (wgsPoints.IsNullOrEmpty())
                            return;

                        var webMercatorPoints = wgsPoints.Select(p => p.Project(SrsBases.GeodeticWgs84, SrsBases.WebMercator)).ToList();

                        var geometry = Geometry<Point>.CreatePointOrLineStringOrRing(webMercatorPoints, SridHelper.WebMercator);

                        //Feature<Point> feature = new Feature<Point>(geometry, "test label");

                        //var dataSource = new MemoryDataSource([feature]);

                        //var geometries = dataSource.GetAsFeatureSet()?.Features;

                        AddDrawingItem(geometry, Path.GetFileNameWithoutExtension(fileName)/*, null, int.MinValue*//*, dataSource*/);
                    }
                    catch (IOException)
                    {
                        await DialogService.ShowMessageAsync(_fileLockedError, _error, param);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        await DialogService.ShowMessageAsync(_fileLockedError, _error, param);
                    }
                    catch (Exception ex)
                    {
                        await DialogService.ShowMessageAsync(ex.Message, _error, param);
                    }
                });
            }

            return _addLongLatTxtToDrawingItemsCommand;
        }
    }


    private RelayCommand _addShapefileToDrawingItemsCommand;

    public RelayCommand AddShapefileToDrawingItemsCommand
    {
        get
        {
            if (_addShapefileToDrawingItemsCommand == null)
            {
                _addShapefileToDrawingItemsCommand = new RelayCommand(async param =>
                {
                    try
                    {
                        var fileName = await DialogService.ShowOpenFileDialogAsync("*.shp|*.shp", param);

                        if (string.IsNullOrWhiteSpace(fileName))
                            return;

                        var dataSource = ShapefileDataSourceFactory.Create(fileName, new WebMercator());

                        var geometries = dataSource.GetAsFeatureSet()?.Features;

                        if (geometries.IsNullOrEmpty())
                            return;

                        if (geometries.Count != 1)
                        {
                            await DialogService.ShowMessageAsync("شیپ فایل حاوی تک عارضه باید باشد", _error, param);

                            return;
                        }

                        AddDrawingItem(geometries.First().TheGeometry, Path.GetFileNameWithoutExtension(fileName)/*, null, int.MinValue*//*, dataSource*/);
                    }
                    catch (IOException)
                    {
                        await DialogService.ShowMessageAsync(_fileLockedError, _error, param);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        await DialogService.ShowMessageAsync(_fileLockedError, _error, param);
                    }
                    catch (Exception ex)
                    {
                        await DialogService.ShowMessageAsync(ex.Message, _error, param);
                    }
                });
            }

            return _addShapefileToDrawingItemsCommand;
        }
    }


    private RelayCommand _removeAllDrawingItemsCommand;
    public RelayCommand RemoveAllDrawingItemsCommand
    {
        get
        {
            if (_removeAllDrawingItemsCommand == null)
            {
                _removeAllDrawingItemsCommand = new RelayCommand(param =>
                {
                    RemoveAllDrawingItems();
                });
            }

            return _removeAllDrawingItemsCommand;
        }
    }


    private RelayCommand _moveDrawingItemUpCommand;
    public RelayCommand MoveDrawingItemUpCommand
    {
        get
        {
            if (_moveDrawingItemUpCommand == null)
            {
                _moveDrawingItemUpCommand = new RelayCommand(param =>
                {
                    MoveDrawingItemUp();
                });
            }

            return _moveDrawingItemUpCommand;
        }
    }


    private RelayCommand _moveDrawingItemDownCommand;
    public RelayCommand MoveDrawingItemDownCommand
    {
        get
        {
            if (_moveDrawingItemDownCommand == null)
            {
                _moveDrawingItemDownCommand = new RelayCommand(param =>
                {
                    MoveDrawingItemDown();
                });
            }

            return _moveDrawingItemDownCommand;
        }
    }


    #endregion


    #region Events

    public event EventHandler<WpfPoint> OnMouseMove;

    public event EventHandler<double> OnZoomChanged;

    public event EventHandler<WpfPoint> OnMapMouseUp;

    public event EventHandler OnMapExtentChanged;

    public event EventHandler OnCancelEdit;

    public event EventHandler OnFinishEdit;

    public event EventHandler OnCancelNewDrawing;

    public event EventHandler OnFinishNewDrawing;

    public event EventHandler OnDeleteDrawing;

    #endregion


    public virtual void RegisterMapOptions()
    {

    }
}
