using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Input;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
using WpfPoint = System.Windows.Point;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Pdf;
using IRI.Maptor.Sta.KmlFormat;
using IRI.Maptor.Sta.Spatial.Model;
using IRI.Maptor.Sta.Spatial.IO.Dxf;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.IO.Gpx;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Common.Services;
using IRI.Maptor.Sta.Common.Exceptions;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Helpers;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.Spatial.GeoJsonFormat;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.Persistence.DataSources;
using IRI.Maptor.Sta.Persistence.Abstractions;
using IRI.Maptor.Sta.Persistence.RasterDataSources;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;

using IRI.Maptor.Ket.GdiPersistence;

using IRI.Maptor.Jab.Common.Events;
using IRI.Maptor.Jab.Common.Models;
using IRI.Maptor.Jab.Common.Helpers;
using IRI.Maptor.Jab.Common.TileServices;
using IRI.Maptor.Jab.Common.Models.Legend;
using IRI.Maptor.Jab.Common.ViewModels.Map;
using IRI.Maptor.Jab.Controls.MapMarkers;
using IRI.Maptor.Jab.Common.Cartography.Symbologies;
using IRI.Maptor.Jab.Common.ViewModels.CoordinateEditor;
using IRI.Maptor.Jab.Common.Models.Settings;
using IRI.Maptor.Jab.Common.Localization;
using IRI.Maptor.Jab.Common.Layers;
using IRI.Maptor.Jab.Common.Data.Settings;
using IRI.Maptor.Jab.Common.Services;

namespace IRI.Maptor.Jab.Common.ViewModels;

public abstract class MapViewModelBase : ViewModelBase
{
    #region Settings

    public IHttpProtocol HttpClient { get; set; } = new HttpProtocol(null);

    public Action<ProxySettingsModel>? FireProxySettingsChanged;

    public Action<bool>? FireIsDoubleClickZoomEnabledChanged;
    public Action<bool>? FireIsMouseWheelZoomEnabledChanged;

    private ProxySettingsModel _proxySettings;
    public ProxySettingsModel ProxySettings
    {
        get { return _proxySettings; }
        protected set
        {
            if (_proxySettings != null)
                _proxySettings.PropertyChanged -= ProxySettings_OnProxyChanged;

            _proxySettings = value ?? new ProxySettingsModel(Data.ProxySettings.Default);

            RaisePropertyChanged();

            HttpClient?.ConfigHttpClient(value);

            _proxySettings.OnProxyChanged -= ProxySettings_OnProxyChanged;
            _proxySettings.OnProxyChanged += ProxySettings_OnProxyChanged;
        }
    }


    private BaseMapSettingsModel _baseMapSettings;
    public BaseMapSettingsModel BaseMapSettings
    {
        get => _baseMapSettings;
        set
        {
            if (_baseMapSettings != null)
            {
                _baseMapSettings.OnOpacityChanged -= BaseMapSettings_OnOpacityChanged;
                _baseMapSettings.OnBaseMapUrlChanged -= BaseMapSettings_OnBaseMapUrlChanged;
            }

            _baseMapSettings = value ?? new BaseMapSettingsModel(IRI.Maptor.Jab.Common.Data.BaseMapSettings.Default/*, UpdateBaseMapOpacity*/);

            RaisePropertyChanged();

            _baseMapSettings.OnOpacityChanged -= BaseMapSettings_OnOpacityChanged;
            _baseMapSettings.OnOpacityChanged += BaseMapSettings_OnOpacityChanged;

            _baseMapSettings.OnBaseMapUrlChanged -= BaseMapSettings_OnBaseMapUrlChanged;
            _baseMapSettings.OnBaseMapUrlChanged += BaseMapSettings_OnBaseMapUrlChanged;
        }
    }


    private MapSettingsModel _mapSettings;
    public MapSettingsModel MapSettings
    {
        get => _mapSettings;
        private set
        {
            if (_mapSettings != null)
            {
                _mapSettings.OnIsDoubleClickZoomEnabledChanged -= MapSettings_OnIsDoubleClickZoomEnabledChanged;
                _mapSettings.OnIsMouseWheelZoomEnabledChanged -= MapSettings_OnIsMouseWheelZoomEnabledChanged;
            }

            _mapSettings = value;

            RaisePropertyChanged();

            _mapSettings.OnIsDoubleClickZoomEnabledChanged -= MapSettings_OnIsDoubleClickZoomEnabledChanged;
            _mapSettings.OnIsDoubleClickZoomEnabledChanged += MapSettings_OnIsDoubleClickZoomEnabledChanged;

            _mapSettings.OnIsMouseWheelZoomEnabledChanged -= MapSettings_OnIsMouseWheelZoomEnabledChanged;
            _mapSettings.OnIsMouseWheelZoomEnabledChanged += MapSettings_OnIsMouseWheelZoomEnabledChanged;
        }
    }


    private GeneralSettingsModel _generalSettings;
    public GeneralSettingsModel GeneralSettings
    {
        get { return _generalSettings; }
        private set
        {
            _generalSettings = value ?? new GeneralSettingsModel(IRI.Maptor.Jab.Common.Data.GeneralSettings.Default);

            RaisePropertyChanged();
        }
    }


    private async void ProxySettings_OnProxyChanged(object? sender, EventArgs e)
    {
        HttpClient.ConfigHttpClient(this.ProxySettings);

        this.FireProxySettingsChanged?.Invoke(this.ProxySettings);

        await CheckNetAccess();
    }

    private void BaseMapSettings_OnBaseMapUrlChanged(object? sender, EventArgs e) => UpdateTilesServices();

    private void BaseMapSettings_OnOpacityChanged(object? sender, double e) => UpdateBaseMapOpacity(e);

    private void MapSettings_OnIsDoubleClickZoomEnabledChanged(object? sender, bool e) => FireIsDoubleClickZoomEnabledChanged?.Invoke(e);

    private void MapSettings_OnIsMouseWheelZoomEnabledChanged(object? sender, bool e) => FireIsMouseWheelZoomEnabledChanged?.Invoke(e);


    #endregion


    #region Properties


    private LanguageSelectorViewModel? _languageSelector;
    public LanguageSelectorViewModel? LanguageSelector
    {
        get { return _languageSelector; }
        set
        {
            _languageSelector = value;
            RaisePropertyChanged();
        }
    }


    public List<GoogleScale> GoogleScales => GoogleScale.GoogleScales;


    private GoogleScale _selectedGoogleScale;
    public GoogleScale SelectedGoogleScale
    {
        get { return _selectedGoogleScale; }
        set
        {
            _selectedGoogleScale = value;
            RaisePropertyChanged();
        }
    }


    public List<ScaleModel> StandardScales => ScaleModel.Scales;

    private MapExtentPanelViewModel _mapExtentPanel;
    public MapExtentPanelViewModel MapExtentPanel
    {
        get { return _mapExtentPanel; }
        set
        {
            _mapExtentPanel = value;
            RaisePropertyChanged();
        }
    }


    private bool _isMapExtentPanelShown;
    public bool IsMapExtentPanelShown
    {
        get { return _isMapExtentPanelShown; }
        set
        {
            _isMapExtentPanelShown = value;
            RaisePropertyChanged();
        }
    }


    private MapPanelViewModel _mapPanel;
    public MapPanelViewModel MapPanel
    {
        get { return _mapPanel; }
        set
        {
            _mapPanel = value;
            RaisePropertyChanged();
        }
    }


    private LegendViewModel _legendViewModel;
    public LegendViewModel LegendViewModel
    {
        get { return _legendViewModel; }
        set
        {
            _legendViewModel = value;
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



    private EditableFeatureLayer? _currentEditingLayer;
    public EditableFeatureLayer? CurrentEditingLayer
    {
        get { return _currentEditingLayer; }
        set
        {
            _currentEditingLayer = value;
            RaisePropertyChanged();

            if (_currentEditingLayer != null)
            {
                _currentEditingLayer.RequestSelectedLocatableChanged = this.RequestSelectedLocatableChanged;
                //    (l, index) =>
                //{
                //    if (l is null)
                //        return;

                //    UpdateCurrentEditingPoint(new Point(l.X, l.Y));

                //    if (CurrentGeometryDetails is not null)
                //    {
                //        CurrentGeometryDetails.GeometryEditor.UpdateSelectedPoint(l, index);
                //    }
                //}; 
                _currentEditingLayer.RequestZoomToPoint = (p) =>
                {
                    //Zoom(WebMercatorUtility.GetGoogleMapScale(14), p);
                    ZoomToExtent(p.AsGeometry(SridHelper.WebMercator).GetBoundingBox(), isExactExtent: false, isNewExtent: true);                    
                };

                _currentEditingLayer.RequestZoomToGeometry = g =>
                {
                    ZoomToExtent(g.GetBoundingBox(), isExactExtent: false, isNewExtent: true);
                };

                _currentEditingLayer.RequestGetCoordinateDisplayMode = () => this.MapPanel.SpatialReference;

                _currentEditingLayer.RequestGetMapSettings = () => this.MapSettings;
            }

        }
    }


    private GeometryDetailsViewModel _currentGeometryDetails;
    public GeometryDetailsViewModel CurrentGeometryDetails
    {
        get { return _currentGeometryDetails; }
        set
        {
            _currentGeometryDetails = value;

            // Set the actions to handle coordinate editor requests
            if (_currentGeometryDetails != null)
            {
                _currentGeometryDetails.RequestUpdateCurrentEditingPoint = UpdateCurrentEditingPoint;

                // Wire up zoom, pan, and copy actions
                _currentGeometryDetails.RequestZoomToPoint = (point) =>
                {
                    Zoom(WebMercatorUtility.GetGoogleMapScale(14), point);
                };

                _currentGeometryDetails.RequestZoomToGeometry = geometry =>
                {
                    if (geometry is null)
                        return;

                    ZoomToExtent(geometry.GetBoundingBox(), false, true);
                };

                _currentGeometryDetails.RequestPanToPoint = (point) =>
                {
                    RequestPanTo?.Invoke(point, null);
                };

                _currentGeometryDetails.RequestCopyCoordinate = (locatable, mode) =>
                {
                    //var geodetic = MapProjects.WebMercatorToGeodeticWgs84(locatable);
                    //System.Windows.Clipboard.SetDataObject($"{geodetic.X.ToString("n4")},{geodetic.Y.ToString("n4")}");

                    Point point = new(locatable.X, locatable.Y);

                    var options = CopyCoordinateOptions.Create(this.MapSettings.Clipboard_LatLongPrecision, this.MapSettings.Clipboard_XyPrecision);

                    ClipboardHelper.CopyToClipboard(point, mode, options, this.MapSettings.Clipboard_IsLatitudeFirst/*null, null, null, null*/);
                };
            }

            RaisePropertyChanged();
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
            // Unsubscribe from old collection
            if (_layers != null)
            {
                _layers.CollectionChanged -= Layers_CollectionChanged;
            }

            _layers = value;
            RaisePropertyChanged();

            // Subscribe to new collection
            if (_layers != null)
            {
                _layers.CollectionChanged += Layers_CollectionChanged;
            }

            // Update AllNonGroupLayers
            UpdateAllNonGroupLayers();
        }
    }


    private ObservableCollection<ILayer> _allNonGroupLayers = new ObservableCollection<ILayer>();
    public ObservableCollection<ILayer> AllNonGroupLayers => _allNonGroupLayers;

    public bool HasPendingChanges => AllNonGroupLayers.Any(l => l.HasPendingChanges);

    private void Layers_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        UpdateAllNonGroupLayers();

        var newItems = e.NewItems?.OfType<ILayer>()?.Where(l => /*l.ShowInToc && */l.CanReorderInToc) ?? Enumerable.Empty<ILayer>();

        var oldItems = e.OldItems?.OfType<ILayer>()?.Where(l => /*l.ShowInToc && */l.CanReorderInToc) ?? Enumerable.Empty<ILayer>();

        var affectedGroups = newItems.Concat(oldItems)
                                     .Select(layer => layer.TocGroup)
                                     .Distinct()
                                     .ToList();

        foreach (var group in affectedGroups)
        {
            UpdateLayerCanMoveUpDown(Layers, group);
        }
    }

    public void UpdateLayerCanMoveUpDown(IEnumerable<ILayer> layers, string tocGroup)
    {
        var orderedLayers = layers.Where(l => l.CanReorderInToc && l.TocGroup == tocGroup /*&& LegendViewModel.IsFilterPassed(l)*/)
                                    .OrderByDescending(l => l.TocOrder)
                                    .ToList();

        foreach (var item in orderedLayers)
        {
            item.CanMoveLayerDown = orderedLayers.IndexOf(item) < orderedLayers.Count - 1;
            item.CanMoveLayerUp = orderedLayers.IndexOf(item) > 0;

            if (item.IsGroupLayer)
            {
                UpdateLayerCanMoveUpDown(item.SubLayers, tocGroup);
            }
        }
    }

    private void UpdateAllNonGroupLayers()
    {
        var newLayers = GetAllLayers(Layers);

        _allNonGroupLayers.Clear();

        foreach (var layer in newLayers)
        {
            _allNonGroupLayers.Add(layer);
        }

        RaisePropertyChanged(nameof(AllNonGroupLayers));
        RaisePropertyChanged(nameof(HasPendingChanges));
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


    //#region BaseMap Mode

    //private string _localNetworkBaseMapBaseUrl;
    //public string LocalNetworkBaseMapBaseUrl
    //{
    //    get { return _localNetworkBaseMapBaseUrl; }
    //    set
    //    {
    //        _localNetworkBaseMapBaseUrl = value;
    //        RaisePropertyChanged();

    //        UpdateTilesServices();
    //    }
    //}

    //private string _proxyAppBaseMapBaseUrl;
    //public string ProxyAppBaseMapBaseUrl
    //{
    //    get { return _proxyAppBaseMapBaseUrl; }
    //    set
    //    {
    //        _proxyAppBaseMapBaseUrl = value;
    //        RaisePropertyChanged();

    //        UpdateTilesServices();
    //    }
    //}



    //private TileMapProviderMode _selectedTileMapProviderMode = TileMapProviderMode.Internet;
    //public virtual TileMapProviderMode SelectedTileMapAccessMode
    //{
    //    get { return _selectedTileMapProviderMode; }
    //    set
    //    {
    //        _selectedTileMapProviderMode = value;
    //        RaisePropertyChanged();

    //        UpdateTilesServices();
    //    }
    //}

    private void UpdateTilesServices()
    {
        foreach (var item in this.MapProviders)
        {
            item.ChangeMode(BaseMapSettings.SelectedTileMapAccessMode, BaseMapSettings.LocalNetworkUrl, BaseMapSettings.ProxyAppUrl);
        }
    }

    //#endregion

    private List<TileMapProvider> _mapProviders = new List<TileMapProvider>();
    public List<TileMapProvider> MapProviders
    {
        get { return _mapProviders; }
        set
        {
            _mapProviders = value;
            RaisePropertyChanged();
        }
    }

    private TileMapProvider? _selectedMapProvider;
    public TileMapProvider? SelectedMapProvider
    {
        get { return _selectedMapProvider; }
        set
        {
            _selectedMapProvider = value;
            RaisePropertyChanged();

            if (value != null)
                BaseMapSettings.InitialBaseMap = value.Type;

            _ = SetTileService(value, BaseMapSettings.BaseMapOpacity, BaseMapSettings.GetLocalFileName);
        }
    }


    private async Task ChangeBaseMap(string? mapProviderFullName)
    {
        try
        {
            var provider = MapProviders.FirstOrDefault(m => m.Is(mapProviderFullName));

            SetTileBaseMap(provider/*, BaseMapOpacity*/);
        }
        catch (Exception ex)
        {
            await ShowExceptionMessageAsync(ex);
            //Debug.WriteLine("exception at ChangeBaseMapCommand: " + ex);
        }
    }

    public void SetTileBaseMap(TileMapProvider? provider)
    {
        if (provider is null)
            return;

        if (provider == SelectedMapProvider)
            return;

        if (!MapProviders.Contains(provider))
            throw new MaptorMapProviderNotAvailableException();

        SelectedMapProvider = provider;
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
        get => _isConnected == true;
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
                return;

            _mapStatus = value;
            RaisePropertyChanged();

            IsDrawMode = _mapStatus == MapStatus.Drawing;
            IsEditMode = _mapStatus == MapStatus.Editing;

            //switch (_mapStatus)
            //{
            //    case MapStatus.Drawing:
            //        IsDrawMode = true;
            //        IsEditMode = false;
            //        break;
            //    case MapStatus.Editing:
            //        IsEditMode = true;
            //        IsDrawMode = false;
            //        break;
            //    //case MapStatus.Measuring:
            //    //    this.IsMeasureMode = true;
            //    //    break;
            //    case MapStatus.Idle:
            //        IsDrawMode = false;
            //        IsEditMode = false;
            //        break;
            //    default:
            //        break;
            //}
        }
    }


    private MapAction _mapAction;
    public MapAction MapAction
    {
        get { return _mapAction; }
        set
        {
            if (_mapAction == value)
                return;

            var previous = _mapAction;

            _mapAction = value;

            RaisePropertyChanged();

            //RaiseMapActionModeProperties();

            if (previous.IsDrawAction())
                StopDrawModeLoop();

            if (value.IsDrawAction())
                StartDrawModeLoop(value);

            //else if (value == MapAction.Pan)
            //    RequestPan?.Invoke();

            //else if (value == MapAction.ZoomIn || value == MapAction.ZoomInRectangle)
            //    RequestEnableRectangleZoom?.Invoke();

            //else if (value == MapAction.ZoomOut || value == MapAction.ZoomOutRectangle)
            //    RequestEnableZoomOut?.Invoke();
        }
    }

    private CancellationTokenSource? _drawModeCts;

    //private void RaiseMapActionModeProperties()
    //{
    //    RaisePropertyChanged(nameof(IsPanMode));
    //    RaisePropertyChanged(nameof(IsZoomInMode));
    //    RaisePropertyChanged(nameof(IsZoomOutMode));
    //    RaisePropertyChanged(nameof(IsDrawPointMode));
    //    RaisePropertyChanged(nameof(IsDrawPolylineMode));
    //    RaisePropertyChanged(nameof(IsDrawPolygonMode));
    //    RaisePropertyChanged(nameof(IsDrawRectangleMode));
    //}


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


    /// <summary>
    /// Current web mercator scale for the whole map
    /// </summary>
    public double MapScale => RequestMapScale?.Invoke() ?? 1;

    //public double InverseMapScale => 1.0 / MapScale;


    /// <summary>
    /// The actual ground scale at the point (latitude effect applied on web mercator scale)
    /// </summary>
    private double _mapScale_CurrentPoint;
    public double MapScale_CurrentPoint
    {
        get { return _mapScale_CurrentPoint; }
        set
        {
            _mapScale_CurrentPoint = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(InverseMapScale_CurrentPoint));
        }
    }


    // ****************** NearestGoogleZoomLevel *************************
    public int NearestGoogleZoomLevel => WebMercatorUtility.GetZoomLevel(this.MapScale);

    /// <summary>
    /// Web Mercator scale for the nearest google zoom level
    /// </summary>
    public double MapScale_NearestGoogleZoomLevel => WebMercatorUtility.CalculateMapScale(NearestGoogleZoomLevel, 0);


    /// <summary>
    /// The actual ground scale for nearest google zoom level at the point
    /// </summary>
    private double _mapScale_NearestGoogleZoomLevel_CurrentPoint;
    public double MapScale_NearestGoogleZoomLevel_CurrentPoint
    {
        get { return _mapScale_NearestGoogleZoomLevel_CurrentPoint; }
        set
        {
            _mapScale_NearestGoogleZoomLevel_CurrentPoint = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(InverseMapScale_NearestGoogleZoomLevel_CurrentPoint));
        }
    }


    // ********************************************************************


    /// <summary>
    /// The ground distance for 1 pixel on the screen
    /// </summary>
    private double _currentPointGroundResolution;
    public double CurrentPointGroundResolution
    {
        get { return _currentPointGroundResolution; }
        set
        {
            _currentPointGroundResolution = Math.Round(value, 2);
            RaisePropertyChanged();
        }
    }


    // ground scale
    //public double CurrentPointInverseNearestGoogleScale => Math.Round(1.0 / CurrentPointNearestGoogleScale, 2);

    public string InverseMapScale_CurrentPoint => $"1:{Math.Round(1.0 / MapScale_CurrentPoint, 0)}";

    public string InverseMapScale_NearestGoogleZoomLevel => $"1:{Math.Round(1.0 / MapScale_NearestGoogleZoomLevel, 0)}";

    public string InverseMapScale_NearestGoogleZoomLevel_CurrentPoint => $"1:{Math.Round(1.0 / MapScale_NearestGoogleZoomLevel_CurrentPoint, 0)}";

    /// <summary>
    /// Current web mercator inverse scale for the whole map
    /// </summary>
    public string InverseMapScale => $"1:{Math.Round(1.0 / MapScale, 0)}";

     
    public BoundingBox CurrentExtent => RequestCurrentExtent?.Invoke() ?? BoundingBoxes.Mercator_Iran;


    public double ActualWidth => RequestGetActualWidth?.Invoke() ?? 1;


    public double ActualHeight => RequestGetActualHeight?.Invoke() ?? 1;


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


    public bool IsDrawEditMeasureMode => IsEditMode || IsDrawMode;


    //public bool IsPanMode
    //{
    //    get { return MapAction == MapAction.Pan; }
    //    set
    //    {
    //        if (value)
    //            MapAction = MapAction.Pan;

    //        RaisePropertyChanged();
    //    }
    //}
    //public bool IsDrawPointMode
    //{
    //    get { return MapAction == MapAction.DrawPoint; }
    //    set
    //    {
    //        if (value)
    //            MapAction = MapAction.DrawPoint;
    //        else if (MapAction == MapAction.DrawPoint)
    //            MapAction = MapAction.Pan;

    //        RaisePropertyChanged();
    //    }
    //}
    //public bool IsDrawPolylineMode
    //{
    //    get { return MapAction == MapAction.DrawPolyline; }
    //    set
    //    {
    //        if (value)
    //            MapAction = MapAction.DrawPolyline;
    //        else if (MapAction == MapAction.DrawPolyline)
    //            MapAction = MapAction.Pan;

    //        RaisePropertyChanged();
    //    }
    //}
    //public bool IsDrawPolygonMode
    //{
    //    get { return MapAction == MapAction.DrawPolygon; }
    //    set
    //    {
    //        if (value)
    //            MapAction = MapAction.DrawPolygon;
    //        else if (MapAction == MapAction.DrawPolygon)
    //            MapAction = MapAction.Pan;

    //        RaisePropertyChanged();
    //    }
    //}
    //public bool IsDrawRectangleMode
    //{
    //    get { return MapAction == MapAction.DrawRectangle; }
    //    set
    //    {
    //        if (value)
    //            MapAction = MapAction.DrawRectangle;

    //        else if (MapAction == MapAction.DrawRectangle)
    //            MapAction = MapAction.Pan;

    //        RaisePropertyChanged();
    //    }
    //}


    //public bool IsZoomInMode
    //{
    //    get { return MapAction == MapAction.ZoomIn || MapAction == MapAction.ZoomInRectangle; }
    //    set
    //    {
    //        if (value)
    //        {
    //            this.MapAction = MapAction.ZoomInRectangle;
    //            //EnableRectangleZoomIn();
    //        }

    //        RaisePropertyChanged();
    //    }
    //}


    //public bool IsZoomOutMode
    //{
    //    get { return MapAction == MapAction.ZoomOut || MapAction == MapAction.ZoomOutRectangle; }
    //    set
    //    {
    //        if (value)
    //        {
    //            //EnableZoomOut();
    //            this.MapAction = MapAction.ZoomOut;
    //        }

    //        RaisePropertyChanged();
    //    }
    //}


    private bool _showAttributeTable = false;
    public bool ShowAttributeTable
    {
        get { return _showAttributeTable; }
        set
        {
            _showAttributeTable = value;
            RaisePropertyChanged();
        }
    }

    private bool _showFeatureTablesOptions;
    public bool ShowFeatureTablesOptions
    {
        get { return _showFeatureTablesOptions; }
        set
        {
            _showFeatureTablesOptions = value;
            RaisePropertyChanged();
        }
    }


    private bool _showMapInfoPanel;
    public bool ShowMapInfoPanel
    {
        get { return _showMapInfoPanel; }
        set
        {
            _showMapInfoPanel = value;
            RaisePropertyChanged();
        }
    }



    private double _currentHeight;
    public double CurrentHeight
    {
        get { return _currentHeight; }
        set
        {
            _currentHeight = value;
            RaisePropertyChanged();
        }
    }


    #endregion


    #region Extent Manager

    // extents used for fast zoom to extent by users. such as provinces etc.
    private ObservableCollection<EnvelopeMarkupLabelTriple> _predefinedExtents = new ObservableCollection<EnvelopeMarkupLabelTriple>();
    public ObservableCollection<EnvelopeMarkupLabelTriple> PredefinedExtents
    {
        get { return _predefinedExtents; }
        set
        {
            _predefinedExtents = value;
            RaisePropertyChanged();
        }
    }

    public void RemovePredefinedExtent(Guid id)
    {
        if (id == Guid.Empty)
            return;

        var item = PredefinedExtents.FirstOrDefault(p => p.Id == id);

        if (item is null || !item.IsUserDefined)
            return;

        PredefinedExtents.Remove(item);
    }

    public List<BoundingBox> MapExtentHistory { get; set; } = new List<BoundingBox>();

    public bool NextExtentEnabled => CurrentExtentIndex > 0;

    public bool PreviousExtentEnabled => CurrentExtentIndex < MapExtentHistory.Count - 1;

    public int ExtentHistoryLength => MapExtentHistory.Count;

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
        CurrentExtentIndex = Math.Min(MapExtentHistory.Count - 1, CurrentExtentIndex + 1);

        ZoomToExtent(MapExtentHistory[CurrentExtentIndex], isExactExtent: true, isNewExtent: false);
    }

    public void GoToNextExtent()
    {
        CurrentExtentIndex = Math.Max(0, CurrentExtentIndex - 1);

        ZoomToExtent(MapExtentHistory[CurrentExtentIndex], isExactExtent: true, isNewExtent: false);
    }

    #endregion

    public MapViewModelBase()
    {
        //HttpClient?.ConfigHttpClient(null);

        _drawingItems.CollectionChanged += (sender, e) =>
        {
            RaisePropertyChanged(nameof(CanMoveDrawingItemDown));
            RaisePropertyChanged(nameof(CanMoveDrawingItemUp));

            UpdateDrawingItems();
        };

        //MapProviders = TileMapProviderFactory.GetDefault();

        MapExtentPanel = new MapExtentPanelViewModel(this);

        LegendViewModel = new LegendViewModel();

        LegendViewModel.RequestNotifyFilterChanged = () => UpdateLayerCanMoveUpDown(Layers, LegendViewModel.TocGroup);

        MapPanel = new MapPanelViewModel();

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

              if (CurrentGeometryDetails is not null)
                  CurrentGeometryDetails.ChangeCurrentEditingPoint(MapPanel.CurrentWebMercatorEditingPoint);
          });

        CoordinatePanel = new CoordinatePanelViewModel();

        RequestSelectedLocatableChanged = (l, index) =>
        {
            if (l is null)
                return;

            UpdateCurrentEditingPoint(new Point(l.X, l.Y));

            if (CurrentGeometryDetails is not null)
            {
                CurrentGeometryDetails.GeometryEditor.UpdateSelectedPoint(l, index);
            }
        };
    }

    public virtual void InitializeSettings(
        IProxySettings? proxySettings,
        IBaseMapSettings? baseMapSettings,
        IMapSettings? mapSettings,
        IGeneralSettings? generalSettings)
    {
        this.ProxySettings = new ProxySettingsModel(proxySettings ?? IRI.Maptor.Jab.Common.Data.ProxySettings.Default);

        this.BaseMapSettings = new BaseMapSettingsModel(baseMapSettings ?? IRI.Maptor.Jab.Common.Data.BaseMapSettings.Default);

        this.MapSettings = new MapSettingsModel(mapSettings ?? IRI.Maptor.Jab.Common.Data.MapSettings.Default);

        this.GeneralSettings = new GeneralSettingsModel(generalSettings ?? IRI.Maptor.Jab.Common.Data.GeneralSettings.Default);

        this.MapProviders = BaseMapSettings.MapProviders;

        this.SelectedMapProvider = this.MapProviders?.FirstOrDefault(m => m.Type == BaseMapSettings.InitialBaseMap);

        this.LanguageSelector = new LanguageSelectorViewModel(
            this.GeneralSettings.AvailableLanguages,
            languageItem => { this.GeneralSettings.CurrentLanguage = languageItem.LanguageType; });

        this.UpdateTilesServices();
    }

    public virtual Task InitializeAsync() => Task.CompletedTask;

    public virtual void Initialize(
        IDialogService dialogService,
        Action<Point> requestShowGoToView,
        Action<ILayer> requestShowSymbologyView,
        Action<ILayer> requestShowLayerSettingsView)
    {
        this.DialogService = dialogService;

        this.RequestShowGoToView = requestShowGoToView;

        this.RequestShowSymbologyView = requestShowSymbologyView;

        this.RequestShowLayerSettingsView = requestShowLayerSettingsView;

        //this.RequestClearAll = this.ClearAll;

        //this.SetMapCursorSet1();

        //this.RegisterMapOptions();

        //this.IsPanMode = true; 
    }

    #region Actions & Funcs

    public Action RequestPrint;

    //public Func<BoundingBox, int, int, Task<List<DrawingVisual>>> RequestGetAsDrawingVisual;

    //public Func<BoundingBox, int, int, Task<BitmapSource?>> RequestCaptureThumbnailAsync;

    public Func<List<ILayer>> RequestGetOrderedLayers;

    public Action<MapAction, Cursor> RequestSetDefaultCursor;

    public Action<IReadOnlyDictionary<MapAction, Cursor>>? RequestApplyCursorSet;

    public Action<Cursor> RequestSetCursor;

    public Func<double> RequestGetActualWidth;

    public Func<double> RequestGetActualHeight;

    public Action<MapViewModelBase> RegisterAction;

    public Action<bool> RequestSetConnectedState;

    public Action RequestRefreshBaseMaps;

    public Action<TileMapProvider, bool, string, bool, Func<TileInfo, string>, double> RequestSetTileService;

    public Func<double> RequestMapScale;

    //public Func<double> RequestCurrentPointScale;
    //public Func<double> RequestCurrentPointScale2;

    //public Func<double> RequestCurrentPointGroundResolution;

    //public Func<int> RequestCurrentZoomLevel;

    public Func<BoundingBox> RequestCurrentExtent;

    public Action<bool> RequestRefresh;

    public Action<ILayer> RequestRefreshLayerVisibility;

    public Action RequestIranExtent;

    public Action RequestFullExtent;

    public Action<double> RequestZoomToScale;

    /// <summary>Wheel-style zoom in/out at the center of the map view (see <see cref="ZoomInAtCenterCommand"/>).</summary>
    public Action<bool> RequestZoomAtViewCenter;

    public Action<Point, double> RequestZoomToPoint;

    public Action<int, Point, Action, bool> RequestZoomAndCenterToGoogleZoomLevel;

    //public Action<int> RequestZoomToGoogleZoomLevel;

    //  bool isExactExtent, bool isNewExtent
    public Action<BoundingBox, bool, bool, Action?> RequestZoomToExtent;

    public Action<Geometry<Point>> RequestZoomToFeature;

    public Action<Geometry<Point>?, Geometry<Point>?>? RequestShowGeometryComparison;

    public Action? RequestClearGeometryComparison;

    public Action<Feature<Point>, List<Field>?>? RequestShowFeatureChangesDialog;

    //public Action RequestEnableRectangleZoom;

    //public Action RequestEnableZoomOut;

    //public Action RequestPan;

    public Action<Point, Action?> RequestPanTo;

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

    public Action<ILayer, bool, bool, bool> RequestClearLayer;

    public Action<Predicate<ILayer>, bool, bool, bool> RequestClearLayerByCriteria;

    public Action<Predicate<LayerTag>, bool, bool, bool> RequestClearLayerByTag;

    public Action RequestRemovePolyBezierLayers;



    public Action<List<Point>> RequestFlashPoints;

    public Action<Point> RequestFlashPoint;


    public Func<string, List<Geometry<Point>>, VisualParameters, Task>? RequestSelectGeometries;

    public Func<string, List<Geometry<Point>>, VisualParameters, Task>? RequestAddGeometries;

    public Func<string, List<Geometry<Point>>, VisualParameters, Task>? RequestHighlightGeometries;

    public Action<SpecialPointLayer> RequestAddSpecialPointLayer;

    public Action<ILayer> RequestSetLayer;

    public Action<ILayer> RequestRemoveLayer;

    //public Action<string> RequestRemoveLayerByName;

    public Action<ILayer> RequestAddLayer;

    /// <summary>
    /// Optional. When set, passed to LayerManager for LoadAsync when adding layers. Used to cancel loads on sign out.
    /// </summary>
    public CancellationToken LoadCancellationToken { get; set; }

    public Func<Geometry<Point>, Geometry<Point>> RequestTransformScreenGeometryToWebMercatorGeometry;

    public Action<string, List<Point>, Geometry, bool, VisualParameters> RequestAddPolyBezier;

    public Func<DrawMode, bool, Task<Response<Geometry<Point>>>> RequestGetDrawingAsync;

    //public Action RequestClearAll;

    public Action RequestCancelNewDrawing;

    public Action RequestFinishDrawingPart;

    public Action RequestFinishNewDrawing;

    public Action RequestCancelEdit;

    public Action RequestFinishEdit;

    public Action OnRequestShowAboutMe;

    public Func<DrawMode, /*EditableFeatureLayerOptions, EditableFeatureLayerOptions, */Action, Task<Response<Geometry<Point>>>> RequestMeasure;

    public Action RequestCancelMeasure;

    public Action<Point> RequestShowGoToView;

    public Action<ILayer> RequestShowSymbologyView;

    public Action<ILayer> RequestShowLayerSettingsView;

    public Action<IPoint> RequestAddPointToNewDrawing;

    public Action<ILayer>? RequestUpdateZIndex;


    public Action<Locateable?, int> RequestSelectedLocatableChanged;


    public Func<Geometry<Point>, /*EditableFeatureLayerOptions, */Task<Response<Geometry<Point>>>> RequestEdit;

    public Func<Geometry, VisualParameters, Task<Response<PolyBezierLayer>>> RequestGetBezier;


    //public Func<Point, IdentifyOptions, ObservableCollection<FeatureSet<Point>>> RequestIdentify;

    //public Func<string, ObservableCollection<FeatureSet<Point>>> RequestSearch;

    public Func<Task<Response<Point>>> RequestGetPoint;

    public Func<Matrix> RequestGetMapToScreenMatrix;

    public Func<Matrix?> RequestGetScreenToMapMatrix;

    public Func<double, double> RequestMapDistanceToScreenDistance;

    public Func<double, double> RequestScreenDistanceMapDistance;


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
            await CheckNetAccess();
        }

        RequestSetTileService?.Invoke(baseMap, BaseMapSettings.IsBaseMapCacheEnabled, BaseMapSettings.BaseMapCacheDirectory, !IsConnected, getLocalFileName, opacity);
    }

    #endregion


    //*****************************************Selected Layers & Select Geometries & DrawGeometries & Identify & FlashPoints ******************
    #region Selected Layers & Select/Draw Geometries & Identify & FlashPoints 

    public async Task AddSelectedLayer(SelectedLayer selectedLayer)
    {
        if (selectedLayer is null)
            return;

        selectedLayer.AssociatedLayer.NumberOfSelectedFeatures = selectedLayer.CountOfSelectedFeatures;

        var existingLayer = SelectedLayers?.SingleOrDefault(l => l.Id == selectedLayer?.Id);

        if (existingLayer == null)
        {
            selectedLayer.RequestFeaturesChangedAsync = ShowSelectedFeatures;

            selectedLayer.RequestHighlightFeaturesChangedAsync = ShowHighlightedFeatures;

            selectedLayer.RequestFlashSinglePoint = FlashHighlightedFeatures;

            selectedLayer.RequestZoomTo = (features, callback) =>
            {
                var extent = BoundingBox.GetMergedBoundingBox(features.Select(f => f.TheGeometry.GetBoundingBox()));

                ZoomToExtent(extent, false, true, callback);
            };

            selectedLayer.RequestRemoveSelectedLayer = () => RemoveSelectedLayer(selectedLayer);

            selectedLayer.RequestRefreshLayer = RefreshLayerVisibility;

            selectedLayer.RequestShowErrorMessageAsync = async (error) => await DialogService.ShowErrorMessage(error);

            //selectedLayer.PropertyChanged += (s, e) =>
            //{
            //    if (e.PropertyName == nameof(SelectedLayer.CanUndo))
            //    {
            //        (selectedLayer.AssociatedLayer as BaseLayer)?.RaisePropertyChanged(nameof(BaseLayer.CanUndoChanges));
            //        CommandManager.InvalidateRequerySuggested();
            //    }
            //};

            selectedLayer.RequestDrawAsync = async (geometryType) =>
            {
                var result = await GetDrawingAsync(geometryType.AsDrawMode());

                return result.HasNotNullResult() ? result.Result : null;
            };

            selectedLayer.RequestEditAsync = async oldFeature =>
            {
                selectedLayer.AssociatedLayer.IsBusy = true;

                var editResult = await EditAsync(oldFeature.TheGeometry, MapSettings.EditingOptions);

                if (!editResult.HasNotNullResult())
                {
                    selectedLayer.AssociatedLayer.IsBusy = false;

                    return;
                }

                if (oldFeature.TheGeometry.AsWkt() == editResult.Result.AsWkt())
                {
                    selectedLayer.AssociatedLayer.IsBusy = false;

                    return;
                }

                if (!selectedLayer.UpdateGeometry(oldFeature, editResult.Result))
                {
                    selectedLayer.AssociatedLayer.IsBusy = false;

                    return;
                }

                //Referesh
                if (selectedLayer.ShowSelectedOnMap)
                {
                    await ShowSelectedFeatures(selectedLayer.GetSelectedFeatures(), selectedLayer?.AssociatedLayer?.DefaultSymbology?.StrokeThickness);
                }

                selectedLayer.AssociatedLayer.IsBusy = false;

                Refresh(isNewExtent: true);
            };

            selectedLayer.RequestViewChanges = feature =>
            {
                RequestShowFeatureChangesDialog?.Invoke(feature, selectedLayer.Fields);
            };

            SelectedLayers.Add(selectedLayer);

            CurrentLayer = selectedLayer;

            if (selectedLayer.ShowSelectedOnMap)
            {
                await ShowSelectedFeatures(selectedLayer.GetSelectedFeatures(), selectedLayer.AssociatedLayer?.DefaultSymbology?.StrokeThickness);
            }
        }
        else
        {
            existingLayer.UpdateSelectedFeatures(selectedLayer.GetSelectedFeatures(includeRemoved: true));

            CurrentLayer = existingLayer;

            if (selectedLayer.ShowSelectedOnMap)
            {
                await ShowSelectedFeatures(selectedLayer.GetSelectedFeatures(), selectedLayer.AssociatedLayer?.DefaultSymbology?.StrokeThickness);
            }
        }

        this.ShowAttributeTable = true;
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

            ClearLayer(LayerType.Selection, true);
            ClearLayer(LayerType.Highlight, true);
            //ClearLayer("__$selection", true);
            //ClearLayer("__$highlight", true);
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

            ClearLayer(LayerType.Selection, true);
            ClearLayer(LayerType.Highlight, true);
            //ClearLayer("__$selection", true);
            //ClearLayer("__$highlight", true);
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

    private async Task ShowSelectedFeatures(IEnumerable<Feature<Point>> enumerable, double? strokeThickness)
    {
        ClearLayer(LayerType.Selection, true);
        ClearLayer(LayerType.Highlight, true);
        //ClearLayer("__$selection", true);
        //ClearLayer("__$highlight", true);

        if (enumerable.IsNullOrEmpty())
            return;

        await SelectGeometriesAsync(
            "__$selection",
            enumerable/*.Where(i => i.Status != FeatureStatus.Removed && i.Status != FeatureStatus.CanceledNew)*/.Select(i => i.TheGeometry).ToList(),
            VisualParameters.GetDefaultForSelection(strokeThickness));
    }

    private async Task ShowHighlightedFeatures(IEnumerable<Feature<Point>> enumerable, double? strokeThickness)
    {
        ClearLayer(LayerType.Highlight, true);
        //ClearLayer("__$highlight", true);
        ClearLayer(LayerType.AnimatingItem, true);

        if (enumerable.IsNullOrEmpty())
            return;

        if (enumerable?.Count() < 10 && enumerable.First().GeometryType/*TheGeometry.Type*/ == GeometryType.Point)
        {
            FlashPoints(enumerable.Select(e => e.TheGeometry.AsPoint()).ToList());
        }
        else
        {
            await HighlightGeometriesAsync(
                "__$highlight",
                enumerable.Select(i => i.TheGeometry).ToList(),
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

        await SelectGeometriesAsync(layer.HighlightGeometryKey.ToString(), [highlightGeo], visualParameters);
    }

    public async Task SelectGeometriesAsync(string layerName, List<Geometry<Point>> geometries, VisualParameters parameters)
    {
        await RequestSelectGeometries?.Invoke(layerName, geometries, parameters);
    }

    public async Task HighlightGeometriesAsync(string layerName, List<Geometry<Point>> geometry, VisualParameters parameters)
    {
        await RequestHighlightGeometries?.Invoke(layerName, geometry, parameters);
    }

    public async Task DrawGeometriesAsync(string layerName, List<Geometry<Point>> geometry, VisualParameters parameters)
    {
        await RequestAddGeometries?.Invoke(layerName, geometry, parameters);
    }


    public async Task DrawGeometryAsync(string name, Geometry<Point> geometry, VisualParameters parameters)
    {
        await DrawGeometriesAsync(name, [geometry], parameters);
    }


    public void FlashPoints(List<Point> points)
    {
        RequestFlashPoints?.Invoke(points);
    }

    public void FlashPoint(Point point)
    {
        RequestFlashPoint?.Invoke(point);
    }


    /// <summary>
    /// Returns the point selected by the user in WGS84
    /// </summary>
    /// <returns>Selected point in WGS84</returns>
    public async Task<Response<Point>> GetPoint()
    {
        return RequestGetPoint != null ?
            await RequestGetPoint() :
            Response<Point>.CreateFailed();

        //return RequestGetPoint != null ?
        //    RequestGetPoint() :
        //    //: new Task<Response<Point>>(() => Response<Point>.CreateFailed(Point.NaN));
        //    Task.FromResult(Response<Point>.CreateFailed());
    }

    #endregion


    #region Search  

    public virtual async void SearchByAttribute(string? searchText)
    {
        RemoveAllDrawingItems();

        if (string.IsNullOrWhiteSpace(searchText))
            return;

        var result = await SearchAsync(searchText);

        if (result == null)
            return;

        SelectedLayers = new ObservableCollection<SelectedLayer>();

        foreach (var item in result)
        {
            var layer = FindLayer(item.LayerId) as VectorLayer;

            if (layer is null)
                continue;

            var fields = layer.GetFields();

            var newLayer = new SelectedLayer/*<Feature<Point>>*/(this.DialogService, layer, fields)
            {
                ShowSelectedOnMap = true,
                Features = new ObservableCollection<Feature<Point>>(item.Features)
            };

            AddSelectedLayer(newLayer);
        }

        RemoveMapOptions();
    }

    public async Task<ObservableCollection<FeatureSet<Point>>?> SearchAsync(string? searchText)
    {
        var result = new ObservableCollection<FeatureSet<Point>>();

        if (string.IsNullOrWhiteSpace(searchText))
            return result;

        foreach (var layer in GetAllVectorLayers(this.Layers))
        {
            if (layer.Type != LayerType.VectorLayer)
                continue;

            if (!layer.IsSearchable)
                continue;

            var features = await (layer.DataSource as IVectorDataSource)!.SearchAsync(searchText);

            if (features is not null && !features.Features.IsNullOrEmpty())
            {
                features.Title = layer.LayerName;

                features.LayerId = layer.LayerId;

                result.Add(features);
            }
        }

        return result;
    }

    #endregion


    #region Identify

    public virtual async Task IdentifyAsync(Point point)
    {
        var identify = await this.IdentifyFeaturesAsync(point, new IdentifyOptions()
        {
            IncludeNotInScaleRangeLayers = this.MapSettings.Identify_IncludeNotInScaleRangeLayers,
            IncludeInvisibleLayers = this.MapSettings.Identify_IncludeInvisibleLayers,
            SelectionTolerance = this.MapSettings.Identify_SelectionTolerance
        });

        if (identify == null)
            return;

        this.SelectedLayers = new ObservableCollection<SelectedLayer>();

        this.ShowAttributeTable = true;

        foreach (var item in identify)
        {
            var layer = this.FindLayer(item.LayerId) as VectorLayer;

            if (layer is null)
                continue;

            var fields = layer.GetFields();

            var newLayer = new SelectedLayer(this.DialogService, layer, fields)
            {
                ShowSelectedOnMap = true,
                Features = new ObservableCollection<Feature<Point>>(item.Features),
                Fields = fields
            };

            this.AddSelectedLayer(newLayer);
        }

        RemoveMapOptions();

        if (SelectedLayers.Any())
        {
            this.CurrentLayer = this.SelectedLayers.FirstOrDefault()!;
        }
    }

    public async Task<ObservableCollection<FeatureSet<Point>>?> IdentifyFeaturesAsync(Point point, IdentifyOptions options)
    {
        var result = new ObservableCollection<FeatureSet<Point>>();

        var offset = this.RequestScreenDistanceMapDistance(options.SelectionTolerance);

        var geometryBoundary = new BoundingBox(point, offset).AsGeometry<Point>(SridHelper.WebMercator);

        foreach (var layer in GetAllVectorLayers(this.Layers))
        {
            if (layer.Type != LayerType.VectorLayer)
                continue;

            if (!layer.IsSearchable)
                continue;

            if (layer.Visibility != System.Windows.Visibility.Visible && !options.IncludeInvisibleLayers)
                continue;

            if (!layer.IsInScaleRange && !options.IncludeNotInScaleRangeLayers)
                continue;

            var features = await (layer.DataSource as IVectorDataSource)!.GetAsFeatureSetAsync(geometryBoundary);

            if (features is not null && !features.Features.IsNullOrEmpty())
            {
                features.Title = layer.LayerName;

                features.LayerId = layer.LayerId;

                features.Fields = layer.GetFields();

                result.Add(features);
            }
        }

        return result;
    }

    public List<VectorLayer> GetAllVectorLayers(IEnumerable<ILayer>? layers)
    {
        var result = new List<VectorLayer>();

        if (layers.IsNullOrEmpty())
        {
            return result;
        }

        result.AddRange(layers.Where(l => l.IsSearchable).OfType<VectorLayer>());

        result.AddRange(layers.Where(l => l.IsGroupLayer && l.IsSearchable).SelectMany(l => GetAllVectorLayers((l as GroupLayer).SubLayers)));

        return result;
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

            return SelectedDrawingItem.IsSelectedInToc && CanMoveUpForDrawingItem(SelectedDrawingItem);
            //DrawingItems.IndexOf(SelectedDrawingItem) > 0;
        }
    }

    public bool CanMoveDrawingItemDown
    {
        get
        {
            if (SelectedDrawingItem == null)
                return false;

            return SelectedDrawingItem.IsSelectedInToc && CanMoveDownForDrawingItem(SelectedDrawingItem);
            // DrawingItems.IndexOf(SelectedDrawingItem) < DrawingItems.Count - 1;
        }
    }

    private void UpdateDrawingItems()
    {
        foreach (var item in DrawingItems)
        {
            item.CanMoveLayerDown = CanMoveDownForDrawingItem(item);
            item.CanMoveLayerUp = CanMoveUpForDrawingItem(item);
        }
    }


    private bool _isDrawingLegendExpanded = false;
    public bool IsDrawingLegendExpanded
    {
        get { return _isDrawingLegendExpanded; }
        set
        {
            _isDrawingLegendExpanded = value;
            RaisePropertyChanged();
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
                   layer => LegendCommand.CreateExportDrawingItemLayerAsGeoJson(this, layer),
                   layer => LegendCommand.CreateExportDrawingItemLayerAsCsv(this, layer/*, CoordinatePanel?.SelectedItem?.CoordinateDisplayMode*/),
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

        var drawingItemLayer = CreateDrawingItemLayer(featureName, feature, symbolizers/*, id*//*, source*/);

        if (drawingItemLayer != null)
            AddDrawingItem(drawingItemLayer);
    }

    public void AddDrawingItem(DrawingItemLayer item)
    {
        DrawingItems.Insert(0, item);

        //this.AddLayer(item.AssociatedLayer);
        AddLayer(item);

        this.IsDrawingLegendExpanded = true;
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

    protected DrawingItemLayer? CreateDrawingItemLayer(string layerName, Feature<Point> drawing, IEnumerable<ISymbolizer> symbolizers)
    {
        var drawingItemLayer = DrawingItemLayer.Create(layerName, drawing, symbolizers);

        if (drawingItemLayer != null)
            TrySetCommandsForDrawingItemLayer(drawingItemLayer);

        return drawingItemLayer;
    }

    private bool CanMoveUpForDrawingItem(DrawingItemLayer? layer)
    {
        if (layer == null)
            return false;

        return DrawingItems.IndexOf(layer) > 0;
    }

    private bool CanMoveDownForDrawingItem(DrawingItemLayer? layer)
    {
        if (layer == null)
            return false;

        return DrawingItems.IndexOf(layer) < DrawingItems.Count - 1;
    }

    public void MoveDrawingItemDown(DrawingItemLayer? layer)
    {
        if (layer == null)
            return;

        var index = DrawingItems.IndexOf(layer);

        var otherLayer = DrawingItems[index + 1];

        SwapDrawingItems(layer, otherLayer);
    }

    public void MoveDrawingItemUp(DrawingItemLayer? layer)
    {
        if (layer == null)
            return;

        var index = DrawingItems.IndexOf(layer);

        var otherLayer = DrawingItems[index - 1];

        SwapDrawingItems(layer, otherLayer);
    }

    public void SwapDrawingItems(DrawingItemLayer first, DrawingItemLayer second)
    {
        // 1. Guard against null or same reference
        if (first == null || second == null) return;
        if (first == second) return;

        var newFirstIndex = DrawingItems.IndexOf(second);
        var newSecondIndex = DrawingItems.IndexOf(first);

        // Validate that both items exist
        if (newFirstIndex == -1 || newSecondIndex == -1)
            throw new InvalidOperationException("One of the items was not found in the collection.");

        // Swap the ZIndex values
        var tempZIndex = first.ZIndex;

        first.ZIndex = second.ZIndex;

        second.ZIndex = tempZIndex;

        DrawingItems.Move(newFirstIndex, newSecondIndex);
        // consider
        //DrawingItems.Move(newSecondIndex, newFirstIndex);

        // update layers
        //first.CanMoveLayerDown = CanMoveDownForDrawingItem(first);
        //first.CanMoveLayerUp = CanMoveUpForDrawingItem(first);

        //second.CanMoveLayerDown = CanMoveDownForDrawingItem(second);
        //second.CanMoveLayerUp = CanMoveUpForDrawingItem(second);

        RequestUpdateZIndex?.Invoke(first);

        RequestUpdateZIndex?.Invoke(second);

        //Refresh(isNewExtent: false);

        //RemoveDrawingItem(first);
        //RemoveDrawingItem(second);

        //if (newSecondIndex < newFirstIndex)
        //{
        //    InsertDrawingItem(newSecondIndex, second);

        //    InsertDrawingItem(newFirstIndex, first);
        //}
        //else
        //{
        //    InsertDrawingItem(newFirstIndex, first);

        //    InsertDrawingItem(newSecondIndex, second);
        //}
    }

    private const string maptorDrawingFileExtension = "mtd";

    public async Task LoadDrawingItemFile(object owner)
    {
        var fileNames = await DialogService.ShowOpenFilesDialogAsync($"*.{maptorDrawingFileExtension}|*.{maptorDrawingFileExtension}", owner);

        if (fileNames.IsNullOrEmpty())
            return;

        foreach (var fileName in fileNames)
        {
            // deserialize the geojsonfeature
            var geoJsonFeatureString = System.IO.File.ReadAllText(fileName);

            var layerName = System.IO.Path.GetFileNameWithoutExtension(fileName);

            var layer = DrawingItemLayer.Deserialize(layerName, geoJsonFeatureString);

            AddDrawingItem(layer);
        }
    }

    public async Task SaveDrawingItemFile(object owner)
    {
        // mtd: maptor drawings
        var folderName = await DialogService.ShowOpenFolderDialogAsync(owner);

        if (string.IsNullOrWhiteSpace(folderName))
            return;

        List<GeoJsonFeatureSet> geoJsonFeatures = [];

        foreach (var item in DrawingItems)
        {
            // todo: add support for texts, 
            if (item.IsTextLayer)
                continue;

            var sld = item.GetSld();

            if (item.Feature is null)
                continue;

            var geoJson = item.Feature.AsGeoJsonFeature();

            geoJson.AddSldAttribute(XmlHelper.Parse(sld));

            var fileName = GetUniqueFileName(folderName, item.LayerName);

            System.IO.File.WriteAllText(fileName, JsonHelper.Serialize(geoJson));
        }
    }

    private string GetUniqueFileName(string folderPath, string desiredFileName)
    {
        string extension = Path.GetExtension(desiredFileName);

        string uniqueFileName = desiredFileName;

        int counter = 1;

        while (File.Exists(Path.Combine(folderPath, uniqueFileName)))
        {
            uniqueFileName = $"{desiredFileName} ({counter++}){extension}";
        }

        // mtd: maptor drawing
        return Path.Combine(folderPath, $"{uniqueFileName}.mtd");
    }

    #endregion

    //*****************************************General***************************************************************

    #region General

    //private ILayer? GetNextTocReorderableLayer(ObservableCollection<ILayer> layers, int currentIndex, bool asc)
    //{
    //    var startIndex = asc ? currentIndex + 1 : currentIndex - 1;

    //    if (asc)
    //    {
    //        for (int i = startIndex; i < layers.Count; i++)
    //        {
    //            if (layers[i].CanReorderInToc)
    //                return layers[i];
    //        }
    //    }
    //    else
    //    {
    //        for (int i = startIndex; i >= 0; i--)
    //        {
    //            if (layers[i].CanReorderInToc)
    //                return layers[i];
    //        }
    //    }

    //    return null;
    //}

    public async Task MoveLayerDown(ILayer? layer, ICollectionView? collectionView)
    {
        if (layer is null)
            return;

        if (layer is DrawingItemLayer dil)
        {
            MoveDrawingItemDown(dil);
        }
        else
        {
            if (layer.Parent is null && collectionView is not null)
            {
                await MoveLayerDown(collectionView.OfType<ILayer>().ToList(), layer);
            }
            else
            {
                await MoveLayerDown(layer.Parent.SubLayers, layer);
            }
        }
    }

    public async Task MoveLayerDown(IList<ILayer> layers, ILayer layer)
    {
        var nextLayer = layers.OrderByDescending(l => l.TocOrder)
                                .FirstOrDefault(l => LegendViewModel.IsFilterPassed(l) && l.CanReorderInToc && l.TocOrder < layer.TocOrder);
        //var nextIndex = currentIndex + 1;

        //var nextLayer = GetNextTocReorderableLayer(layers, currentIndex, asc: true);

        if (nextLayer is null)
            return;

        await SwapLayerOrders(layer, nextLayer/*layers[nextIndex]*/);

        UpdateLayerCanMoveUpDown(layers, layer.TocGroup);
    }


    public async Task MoveLayerUp(ILayer? layer, ICollectionView? collectionView)
    {
        if (layer is null)
            return;

        //if (LegendViewModel.HasActiveFilter)
        //    return;

        if (layer is DrawingItemLayer dil)
        {
            MoveDrawingItemUp(dil);
        }
        else
        {
            if (layer.Parent is null && collectionView is not null)
            {
                await MoveLayerUp(collectionView.OfType<ILayer>().ToList(), layer);
            }
            else
            {
                await MoveLayerUp(layer.Parent.SubLayers, layer);
            }
        }
    }

    public async Task MoveLayerUp(IList<ILayer> layers, ILayer layer)
    {
        var nextLayer = layers.OrderBy(l => l.TocOrder).FirstOrDefault(l => LegendViewModel.IsFilterPassed(l) && l.CanReorderInToc && l.TocOrder > layer.TocOrder);
        //var nextIndex = currentIndex - 1;

        //var nextLayer = GetNextTocReorderableLayer(layers, currentIndex, asc: false);

        if (nextLayer is null)
            return;

        await SwapLayerOrders(layer, nextLayer/*layers[nextIndex]*/);

        UpdateLayerCanMoveUpDown(layers, layer.TocGroup);

    }

    private async Task SwapLayerOrders(ILayer first, ILayer second)
    {
        var tempTocOrder = first.TocOrder;
        first.TocOrder = second.TocOrder;
        second.TocOrder = tempTocOrder;

        var tempZIndex = first.ZIndex;
        first.ZIndex = second.ZIndex;
        second.ZIndex = tempZIndex;

        //first.LayerName = $"{first.LayerName}: {first.TocOrder}";
        //second.LayerName = $"{second.LayerName}: {second.TocOrder}";

        if (LegendViewModel.RequestRefreshView is not null)
        {
            await LegendViewModel.RequestRefreshView.Invoke();
        }

        RequestUpdateZIndex?.Invoke(first);

        RequestUpdateZIndex?.Invoke(second);

        //Refresh(isNewExtent: false);
    }

    public void UpdateBaseMapOpacity(double opacity)
    {
        foreach (var layer in Layers.Where(i => i.Type == LayerType.BaseMap))
            layer.Opacity = opacity;
    }

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

    //private void ConfigHttpClient(System.Net.WebProxy? proxy)
    //{

    //    if (proxy?.Address != null)
    //    {
    //        HttpClientHandler handler = new HttpClientHandler();
    //        handler.Proxy = proxy;
    //        handler.UseProxy = true;
    //        HttpClient = new System.Net.Http.HttpClient(handler) { Timeout = new TimeSpan(0, 0, seconds: 10) };
    //        HttpClient.DefaultRequestHeaders.Add("User-Agent", "app!");
    //    }
    //    else
    //    {
    //        HttpClientHandler handler = new HttpClientHandler();
    //        handler.Proxy = null;
    //        handler.UseProxy = false;
    //        HttpClient = new System.Net.Http.HttpClient(handler) { Timeout = new TimeSpan(0, 0, seconds: 10) };
    //        HttpClient.DefaultRequestHeaders.Add("User-Agent", "app!");
    //    }

    //}


    public async virtual Task CheckNetAccess()
    {
        if (DoNotCheckInternet)
            return;

        var proxy = ProxySettings is null ? null : this.ProxySettings.GetProxy();// RequestGetProxy?.Invoke();

        IsConnected = await NetworkUtilities.IsConnectedToInternet(proxy);
    }

    public void SetMapCursors()
    {
        RequestApplyCursorSet?.Invoke(MapCursorHelper.CreateDefaultSet());
    }

    [Obsolete("Use SetMapCursors() instead.")]
    public void SetMapCursorSet1() => SetMapCursors();

    public void SetDefaultCursor(MapAction action, Cursor cursor)
    {
        RequestSetDefaultCursor?.Invoke(action, cursor);
    }

    public void SetCursor(Cursor cursor)
    {
        RequestSetCursor?.Invoke(cursor);
    }


    public void ClearLayer(ILayer layer, bool remove = true, bool forceRemove = false, bool keepEmptyParentGroup = false)
    {
        RequestClearLayer?.Invoke(layer, remove, forceRemove, keepEmptyParentGroup);

        RemoveSelectedLayers(l => l.LayerId == layer.LayerId);
    }

    public void ClearLayer(LayerType type, bool remove, bool forceRemove = false, bool keepEmptyParentGroup = false)
    {
        //Clear(tag => tag.LayerType.HasFlag(type), remove, forceRemove);
        Clear(tag => tag.LayerType == type, remove, forceRemove, keepEmptyParentGroup);
    }

    public void ClearLayer(string layerName, bool remove = true, bool forceRemove = false, bool keepEmptyParentGroup = false)
    {
        Clear(layer => layer.LayerName == layerName, remove, forceRemove, keepEmptyParentGroup);
        //this.RequestClearLayerByName?.Invoke(layerName, remove);
    }

    public void ClearAll()
    {
        Clear(new Predicate<ILayer>(l => l.CanUserDelete == true), true);

        DrawingItems.Clear();
    }


    public void Clear(Predicate<ILayer> layersToBeRemoved, bool remove, bool forceRemove = false, bool keepEmptyParentGroup = false)
    {
        RequestClearLayerByCriteria?.Invoke(layersToBeRemoved, remove, forceRemove, keepEmptyParentGroup);

        RemoveSelectedLayers(layersToBeRemoved);
    }

    //1397.08.17: potentionally error prone, do not consider removing SelectedLayers associated with the input criteria
    public void Clear(Predicate<LayerTag> criteria, bool remove, bool forceRemove = false, bool keepEmptyParentGroup = false)
    {
        RequestClearLayerByTag?.Invoke(criteria, remove, forceRemove, keepEmptyParentGroup);
    }

    //public void RemoveLayer(string layerName)
    //{
    //    this.RequestRemoveLayerByName?.Invoke(layerName);
    //}

    //public void RemoveLayer(ILayer layer, bool forceRemove = false)
    //{
    //    this.RequestRemoveLayer?.Invoke(layer, forceRemove);
    //}




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
            MapExtentHistory.RemoveRange(0, CurrentExtentIndex);
        }

        MapExtentHistory.Insert(0, currentExtent);

        CurrentExtentIndex = 0;

        if (ExtentHistoryLength > 11)
            MapExtentHistory.RemoveAt(lastExtentIndex);
    }

    public void FireMouseMove(WpfPoint currentPoint)
    {
        CurrentPoint = new Point(currentPoint.X, currentPoint.Y);

        if (this.CurrentPoint == null || double.IsNaN(this.CurrentPoint.Y))
            return;

        this.MapScale_NearestGoogleZoomLevel_CurrentPoint = WebMercatorUtility.CalculateMapScale(this.NearestGoogleZoomLevel, currentPoint.Y);

        this.MapScale_CurrentPoint = WebMercatorUtility.WebMercatorScaleToMapScale(this.MapScale, CurrentPoint.Y);

        this.CurrentPointGroundResolution = WebMercatorUtility.CalculateGroundResolution(this.NearestGoogleZoomLevel, CurrentPoint.Y);
        var currentPointGroundResolution = WebMercatorUtility.CalculateGroundResolution(MapScale_NearestGoogleZoomLevel_CurrentPoint /*CurrentPointNearestGoogleScale*/);

        var theScale1_local = WebMercatorUtility.CalculateMapScale(this.NearestGoogleZoomLevel, CurrentPoint.Y);
        var theScale_equator = WebMercatorUtility.CalculateMapScale(this.NearestGoogleZoomLevel, 0);


        var groundRes = WebMercatorUtility.CalculateGroundResolution(theScale_equator);
        var groundRes2 = WebMercatorUtility.CalculateGroundResolution(this.NearestGoogleZoomLevel, 0);

        if (this.MapExtentPanel != null)
        {
            MapExtentPanel.RefreshMetrics();
            //RaisePropertyChanged(nameof(MapExtentPanel.ScaleText));
            //RaisePropertyChanged(nameof(MapExtentPanel.GroundResolutionText));
        }

        OnMouseMove?.Invoke(this, currentPoint);
    }

    public void FireMapMouseUp(WpfPoint currentPoint)
    {
        OnMapMouseUp?.Invoke(this, currentPoint);
    }

    public void FireZoomChanged(double mapScale)
    {
        OnZoomChanged?.Invoke(this, mapScale);

        RaisePropertyChanged(nameof(MapScale));
        RaisePropertyChanged(nameof(InverseMapScale));

        RaisePropertyChanged(nameof(NearestGoogleZoomLevel));
        RaisePropertyChanged(nameof(MapScale_NearestGoogleZoomLevel));
        RaisePropertyChanged(nameof(InverseMapScale_NearestGoogleZoomLevel));

        if (CurrentPoint is null)
            return;

        this.MapScale_CurrentPoint = WebMercatorUtility.WebMercatorScaleToMapScale(this.MapScale, CurrentPoint.Y);
        this.CurrentPointGroundResolution = WebMercatorUtility.CalculateGroundResolution(this.NearestGoogleZoomLevel, CurrentPoint.Y);
    }

    #endregion


    //*****************************************Editing***************************************************************
    #region Editing

    public Task<Response<Geometry<Point>>> EditAsync(Geometry<Point> geometry, EditableFeatureLayerOptions? options)
    {
        this.ShowMapInfoPanel = true;

        //options = options ?? MapSettings.EditingOptions;

        MapPanel.Options = options ?? MapSettings.EditingOptions;

        if (RequestEdit != null)
        {
            return RequestEdit(geometry/*, options*/);
        }
        else
        {
            return Task.FromResult(Response<Geometry<Point>>.CreateFailed());
            //return new Task<Response<Geometry<Point>>>(() => ResponseFactory.Create<Geometry<Point>>(null));

        }
    }

    public async Task<Response<Geometry<Point>>> EditAsync(List<Point> points, bool isClosed, int srid, EditableFeatureLayerOptions? options = null)
    {
        if (points == null || points.Count < 1)
        {
            //return new Task<Response<Geometry<Point>>>(null);
            return Response<Geometry<Point>>.CreateFailed();
        }

        //1397.08.15.this is already done in EditAsync(geometry,options)
        //options = options ?? this.MapSettings.EditingOptions;
        //this.MapPanel.Options = options;

        var type = points.Count == 1 ? GeometryType.Point : isClosed ? GeometryType.Polygon : GeometryType.LineString;

        Geometry<Point> geometry = Geometry<Point>.Create(points, type, srid);

        return await EditAsync(geometry, options);
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

    public void UpdateCurrentEditingPoint(Point webMercatorPoint)
    {
        MapPanel.UpdateCurrentEditingPoint(webMercatorPoint);
    }

    #endregion


    //*****************************************Layer Management******************************************************
    #region Layer Management

    public void RefreshLayerVisibility(ILayer layer)
    {
        RequestRefreshLayerVisibility?.Invoke(layer);
    }

    private async Task HandleRequestSaveChanges(ILayer layer)
    {
        try
        {
            var selectedLayer = SelectedLayers?.SingleOrDefault(sl => sl.Id == layer.LayerId);

            if (selectedLayer != null)
            {
                await selectedLayer.SaveChangesAsync();
            }
            else
            {
                var dataSource = layer.DataSource as IEditableVectorDataSource;

                if (dataSource != null)
                    await dataSource.SaveChangesAsync();

                await DialogService.ShowMessage_DoneSuccessfully();
            }
        }
        catch (Exception ex)
        {
            await DialogService.ShowErrorMessage(new DomainException(ex.Message));
        }
    }

    private async Task HandleRequestUndoAllChanges(ILayer layer)
    {
        var message = IRI.Maptor.Jab.Common.Properties.Resources.dialog_msg_discardPendingChanges;

        var sure = await DialogService.ShowYesNoDialogAsync(message);

        if (sure == false)
            return;

        var selectedLayer = SelectedLayers?.SingleOrDefault(sl => sl.Id == layer.LayerId);

        if (selectedLayer != null)
        {
            await selectedLayer.UndoAllChangesAsync();
        }
        else
        {
            (layer.DataSource as IEditableVectorDataSource)?.UndoAllChanges();

            RefreshLayerVisibility(layer);

            //if (selectedLayer != null && layer is VectorLayer vectorLayer)
            //{
            //    var features = await vectorLayer.GetFeaturesAsync();

            //    selectedLayer.UpdateSelectedFeatures(features?.Features ?? Enumerable.Empty<Feature<Point>>());
            //}
        }
    }

    //private bool GetCanUndoChanges(ILayer layer)
    //{
    //    var selectedLayer = SelectedLayers?.SingleOrDefault(sl => sl.Id == layer.LayerId);
    //    if (selectedLayer != null)
    //        return selectedLayer.CanUndo;
    //    return layer.HasPendingChanges;
    //}

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
        layer.RequestShowLayerSettings = l => RequestShowLayerSettingsView?.Invoke(l);

        layer.RequestMoveLayerUp = (l, cv) => MoveLayerUp(l, cv);
        layer.RequestMoveLayerDown = (l, cv) => MoveLayerDown(l, cv);
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

        layer.RequestMoveLayerUp = (l, cv) => MoveLayerUp(l, cv);
        layer.RequestMoveLayerDown = (l, cv) => MoveLayerDown(l, cv);

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

            if (layer is BaseLayer baseLayer)
            {
                baseLayer.RequestSaveChanges = async layer => await HandleRequestSaveChanges(layer);
                baseLayer.RequestUndoAllChanges = async layer => await HandleRequestUndoAllChanges(layer);
                baseLayer.RequestClearSelectedLayer = layer => RemoveSelectedLayers(l => l.LayerId == layer.LayerId);
                baseLayer.RequestShowLayerSettings = layer => this.RequestShowLayerSettingsView(layer);

                //baseLayer.CanUndoChangesProvider = GetCanUndoChanges;
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
        layer.Commands = DrawingItemCommands?.Select(dic => dic(layer))?.ToList() ?? [];

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

        //layer.RequestMoveLayerUp = l => this.MoveLayerUp(l);
        //layer.RequestMoveLayerDown = l => this.MoveLayerDown(l);

        if (layer.RequestChangeSymbology == null)
        {
            layer.RequestChangeSymbology = l => RequestShowSymbologyView?.Invoke(l);
        }

        if (layer.RequestShowLayerSettings is null)
        {
            layer.RequestShowLayerSettings = l => RequestShowLayerSettingsView?.Invoke(l);
        }
    }


    public void AddLayer(ILayer layer)
    {
        TrySetCommands(layer);

        RequestAddLayer?.Invoke(layer);
    }


    //public void RemoveLayer(string layerName)
    //{
    //    RequestRemoveLayerByName?.Invoke(layerName);
    //}


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
        if (layers.IsNullOrEmpty())
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

    protected async Task<Response<PolyBezierLayer>> GetBezier(Geometry symbol, VisualParameters decorationVisual)
    {
        if (RequestGetBezier != null)
        {
            return await RequestGetBezier(symbol, decorationVisual);
        }
        else
        {
            //return new Task<Response<PolyBezierLayer>>(() => Response<PolyBezierLayer>.CreateFailed());
            return Response<PolyBezierLayer>.CreateFailed();
        }
    }

    #endregion


    //*****************************************Zoom******************************************************************
    #region Zoom

    public void ZoomAndCenterToGoogleZoomLevel(int zoomLevel, Point centerMapPoint, Action callback = null, bool withAnimation = true)
    {
        RequestZoomAndCenterToGoogleZoomLevel?.Invoke(zoomLevel, centerMapPoint, callback, withAnimation);
    }

    //public void EnableRectangleZoomIn()
    //{
    //    //RequestEnableRectangleZoom?.Invoke();
    //    this.MapAction = MapAction.ZoomInRectangle;
    //}

    //public void EnableZoomOut()
    //{
    //    RequestEnableZoomOut?.Invoke();
    //}

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

    public void ZoomInAtViewCenter()
    {
        RequestZoomAtViewCenter?.Invoke(true);
    }

    public void ZoomOutAtViewCenter()
    {
        RequestZoomAtViewCenter?.Invoke(false);
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
        //RequestPan?.Invoke();
        this.MapAction = MapAction.Pan;
    }

    public void PanTo(Point point, Action? callback)
    {
        RequestPanTo?.Invoke(point, callback);
    }

    public void PanToGeographicPoint(Point point, Action? callback = null)
    {
        var webMercatorPoint = MapProjects.GeodeticWgs84ToWebMercator(point);

        PanTo(webMercatorPoint, callback);
    }

    #endregion


    //*****************************************Drawing***************************************************************
    #region Drawing

    private void StartDrawModeLoop(MapAction action)
    {
        StopDrawModeLoop();

        _drawModeCts = new CancellationTokenSource();
        var ct = _drawModeCts.Token;
        _ = RunDrawModeLoopAsync(action, ct);
    }

    private void StopDrawModeLoop()
    {
        if (_drawModeCts == null)
            return;

        _drawModeCts.Cancel();
        _drawModeCts.Dispose();
        _drawModeCts = null;

        RequestCancelNewDrawing?.Invoke();
    }


    private async Task RunDrawModeLoopAsync(MapAction action, CancellationToken ct)
    {
        try
        {
            var drawMode = action.ToDrawMode();

            while (!ct.IsCancellationRequested && MapAction == action)
            {
                try
                {
                    var result = await GetDrawingAsync(drawMode, MapSettings.DrawingOptions, continuousDrawing: true);

                    if (ct.IsCancellationRequested || MapAction != action)
                        break;

                    if (result.IsCanceled)
                    {
                        // Cancel requested while drawing was active – loop continues, but no new drawing starts immediately
                        continue;
                    }

                    if (result.HasNotNullResult())
                    {
                        var featureName = $"DRAWING {DrawingItems?.Count}";
                        AddDrawingItem(result.Result, featureName);
                        await Task.Delay(400, ct);
                    }
                }
                catch (OperationCanceledException)
                {
                    // If the loop token was cancelled (StopDrawModeLoop), exit completely
                    if (ct.IsCancellationRequested) break;
                    // Otherwise (per-drawing cancellation) just continue
                }
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            if (MapAction == action)
                MapAction = MapAction.Pan;
        }
    }

    public async Task<Response<Geometry<Point>>> GetDrawingAsync(
        DrawMode mode,
        EditableFeatureLayerOptions? options = null,
        bool continuousDrawing = false)
    {
        this.ShowMapInfoPanel = mode != DrawMode.Rectangle;

        options = options ?? MapSettings.DrawingOptions;

        MapPanel.Options = options;

        if (RequestGetDrawingAsync == null)
            return Response<Geometry<Point>>.Empty;

        return await RequestGetDrawingAsync.Invoke(mode, continuousDrawing);
    }

    public void ExitDrawMode()
    {
        StopDrawModeLoop();
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

    public virtual void RegisterMapOptions()
    {

    }

    #endregion


    //*****************************************Measure***************************************************************
    #region Measure

    protected async Task<Response<Geometry<Point>>> Measure(DrawMode mode, Action action = null)
    {
        if (MapAction.IsDrawAction())
            MapAction = MapAction.Pan;

        this.ShowMapInfoPanel = true;

        try
        {
            MapSettings.DrawingMeasureOptions.RequestHandleMeasureVisibilityChanged = null;
            System.Diagnostics.Trace.WriteLine("RequestHandleMeasureVisibilityChanged called #1");

            // only in the case of length measurement show edge lengths by default.
            MapSettings.DrawingMeasureOptions.IsEdgeLabelVisible = mode == DrawMode.Polyline;

            MapPanel.Options = MapSettings.DrawingMeasureOptions;

            var result = await RequestMeasure.Invoke(mode, /*MapSettings.DrawingMeasureOptions, MapSettings.EditingMeasureOptions,*/ action);

            if (result.HasNotNullResult())
            {
                MapPanel.Options = MapSettings.EditingMeasureOptions;

                await RequestEdit.Invoke(result.Result);
            }

            MapSettings.DrawingMeasureOptions.RequestHandleMeasureVisibilityChanged = null;
            System.Diagnostics.Trace.WriteLine("RequestHandleMeasureVisibilityChanged called #3");

            return result;
        }
        catch (Exception ex)
        {
            await ShowExceptionMessageAsync(ex);

            return Response<Geometry<Point>>.Empty;
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

            var text = string.Empty;//"sample text!";

            TextboxMarkerViewModel viewModel = new TextboxMarkerViewModel() { LabelValue = text };

            var drawingItemLayer = DrawingItemLayer.CreateTextLayer("Text",
            [
                new Locateable(response.Result, AncherFunctionHandlers.BottomCenter){ Element = new TextboxMarker(){ DataContext = viewModel} }
            ]);

            viewModel.RequestDelete = () => RemoveDrawingItem(drawingItemLayer);

            AddDrawingItem(drawingItemLayer);
        }
        catch (Exception ex)
        {
            await ShowExceptionMessageAsync(ex);
        }
    }

    #endregion


    #region Save As Png

    public async Task<RenderTargetBitmap?> GetAsMergedDrawingVisual(BoundingBox boundingBox/*, int imageWidth, int imageHeight*/)
    {
        var imageWidth = (int)RequestMapDistanceToScreenDistance(boundingBox.Width);
        var imageHeight = (int)RequestMapDistanceToScreenDistance(boundingBox.Height);

        var visuals = await GetAsDrawingVisuals(boundingBox/*, imageWidth, imageHeight*/);

        if (visuals.IsNullOrEmpty())
            return null;

        return ImageUtility.Merge(visuals, imageWidth, imageHeight);
    }

    public async Task<List<DrawingVisual>> GetAsDrawingVisuals(BoundingBox boundingBox/*, int imageWidth, int imageHeight*/)
    {
        // print all layers in rectangle
        //var layers = this._layerManager.GetOrderedLayers();
        var layers = this.RequestGetOrderedLayers?.Invoke();

        var imageWidth = (int)RequestMapDistanceToScreenDistance(boundingBox.Width);
        var imageHeight = (int)RequestMapDistanceToScreenDistance(boundingBox.Height);

        double scaleX = imageWidth / boundingBox.Width;
        double scaleY = imageHeight / boundingBox.Height;
        var scale = Math.Max(scaleX, scaleY);

        List<DrawingVisual> visuals = new List<DrawingVisual>();

        foreach (var item in layers)
        {
            if (item.Visibility != System.Windows.Visibility.Visible)
                continue;

            if (item.IsNotInScaleRange)
                continue;

            switch (item)
            {
                case TileServiceLayer tsl:
                    visuals.Add(tsl.AsDrawingVisual(boundingBox, NearestGoogleZoomLevel, imageWidth, imageHeight));
                    break;

                case VectorLayer vectorLayer:
                    visuals.AddRange(await vectorLayer.AsDrawingVisual(boundingBox, imageWidth, imageHeight, scale/*this.MapScale*/));
                    break;

                case DrawingLayer drawingLayer:
                    visuals.Add(drawingLayer.AsDrawingVisual(boundingBox, imageWidth, imageHeight, scale/*this.MapScale*/));
                    break;

                //case FeatureLayer featureLayer:
                default:
                    break;
            }
        }

        visuals = visuals.Where(v => v != null).ToList();

        return visuals;
    }

    public async Task<BitmapSource?> CaptureThumbnailAsync(BoundingBox extent, int thumbWidth, int thumbHeight)
    {
        if (extent.IsNaN() || !extent.IsValid())
            return null;

        int zoomLevel = NearestGoogleZoomLevel;

        var tiles = WebMercatorUtility.WebMercatorBoundingBoxToGoogleTileRegions(extent, zoomLevel);

        double scaleX = thumbWidth / extent.Width;

        double scaleY = thumbHeight / extent.Height;

        var clip = new RectangleGeometry(new System.Windows.Rect(0, 0, thumbWidth, thumbHeight));

        var baseMapVisual = new DrawingVisual();

        using (var drawingContext = baseMapVisual.RenderOpen())
        {
            drawingContext.PushClip(clip);

            var basemapLayers = this.RequestGetOrderedLayers?.Invoke() //_layerManager.GetOrderedLayers()
                .OfType<TileServiceLayer>()
                .Where(l => l.Visibility == System.Windows.Visibility.Visible);

            foreach (var layer in basemapLayers)
            {
                foreach (var tile in tiles)
                {
                    try
                    {
                        var image = layer.GetCachedTileAsync(tile);

                        if (!image.IsValid) continue;

                        var tileExtent = image.GeodeticWgs84BoundingBox.Transform(MapProjects.GeodeticWgs84ToWebMercator);

                        var rect = new System.Windows.Rect(
                                        (tileExtent.XMin - extent.XMin) * scaleX,
                                        (extent.YMax - tileExtent.YMax) * scaleY,
                                        tileExtent.Width * scaleX,
                                        tileExtent.Height * scaleY);

                        drawingContext.DrawImage(ImageUtility.CreateBitmapImage(image.Image), rect);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"MapViewer; CaptureThumbnailAsync tile error: {ex.Message}");
                    }
                }
            }

            drawingContext.Pop();
        }

        var vectorImage = await GetAsMergedDrawingVisual(extent/*, thumbWidth, thumbHeight*/);

        var vectorVisual = new DrawingVisual();

        using (var drawingContext = vectorVisual.RenderOpen())
        {
            drawingContext.PushClip(clip);

            var scale_x = thumbWidth / vectorImage.Width;
            var scale_y = thumbHeight / vectorImage.Height;

            var rect = new System.Windows.Rect(0, 0, vectorImage.Width * scale_x, vectorImage.Height * scale_y);

            drawingContext.DrawImage(vectorImage, rect);

            drawingContext.Pop();
        }

        //var vectorMerged = ImageUtility.Merge(vectorVisuals, vectorVisuals);

        var rtb = new RenderTargetBitmap(thumbWidth, thumbHeight, 96, 96, PixelFormats.Pbgra32);

        rtb.Render(baseMapVisual);

        rtb.Render(vectorVisual);
        //foreach (var v in vectorVisuals)
        //    rtb.Render(v);

        rtb.Freeze();

        return rtb;
    }


    #endregion


    #region Printing

    public void Print()
    {
        RequestPrint?.Invoke();
    }

    public async Task ClipAndExportMapAsPngAsync(object owner)
    {
        // select a rectangle 
        var polygon = await GetDrawingAsync(DrawMode.Rectangle);

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
        //if (RequestGetAsDrawingVisual is null)
        //    return;

        var fileName = await DialogService.ShowSaveFileDialogAsync("*.png|*.png", owner);

        if (string.IsNullOrWhiteSpace(fileName))
            return;

        //var toScreenMap = CreateMapToScreenFunc();

        var width = (int)RequestMapDistanceToScreenDistance(boundingBox.Width);
        var height = (int)RequestMapDistanceToScreenDistance(boundingBox.Height);

        var visuals = await GetAsDrawingVisuals(boundingBox/*, width, height*/); /*RequestGetAsDrawingVisual(boundingBox, width, height);*/

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

    public async Task PrintToPdfAsync(object owner, bool supportPdfLayers = true)
    {
        var boundingBox = PrintArea.IsNaN() ? CurrentExtent : PrintArea;

        var mapScale = MapScale;

        // Show save dialog
        var fileName = await DialogService.ShowSaveFileDialogAsync("*.pdf|*.pdf", owner);

        if (string.IsNullOrWhiteSpace(fileName))
            return;

        // Get all layers
        var allLayers = GetAllLayers(Layers);

        // Collect raster (basemap) layers
        var rasterLayerPdfDataList = await PdfHelper.GetTiles(this, mapScale, boundingBox);

        var allVisibleInScaleLayers = allLayers.OfType<SymbolizableLayer>()
                                                .Where(l => l.Visibility == System.Windows.Visibility.Visible &&
                                                            l.CanRenderLayer(mapScale))
            .OrderBy(l => l.ZIndex)
            .ToList();

        // Collect layer data with features and symbology
        var layerPdfDataList = new List<PdfWriter.LayerPdfData>();

        foreach (var layer in allVisibleInScaleLayers)
        {
            try
            {
                // Get features for current extent and scale
                var featureSet = await layer.GetFeatureSet(boundingBox, mapScale);

                if (featureSet == null || featureSet.HasNoGeometry())
                    continue;

                var allFeatures = featureSet.Features
                    .Where(f => f.TheGeometry != null && !f.TheGeometry.IsEmpty())
                    .ToList();

                if (allFeatures.Count == 0)
                    continue;

                // Iterate through ALL symbolizers in the layer
                foreach (var symbolizer in layer.Symbolizers)
                {
                    // Skip LabelSymbolizer (labels not supported in vector PDF)
                    if (symbolizer is LabelSymbolizer)
                        continue;

                    // Check if symbolizer is in scale range
                    if (!symbolizer.IsInScaleRange(mapScale))
                        continue;

                    // Filter features using symbolizer's filter
                    var filteredFeatures = allFeatures.Where(symbolizer.IsFilterPassed).ToList();

                    if (filteredFeatures.Count == 0)
                        continue;

                    // Convert this symbolizer's visual parameters to PDF options
                    var pdfOptions = ConvertSymbolizerToPdfOptions(symbolizer);

                    // Create separate LayerPdfData for this symbolizer's filtered features
                    var layerPdfData = new PdfWriter.LayerPdfData
                    {
                        Features = filteredFeatures,
                        Options = pdfOptions,
                        ZIndex = layer.ZIndex, // Use layer's ZIndex
                        Opacity = layer.Opacity * (symbolizer.Param?.Opacity ?? 1.0),
                        LayerName = layer.LayerName
                    };

                    layerPdfDataList.Add(layerPdfData);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error processing layer {layer.LayerName}: {ex.Message}");
            }
        }

        #region Old code

        //// Get visible vector layers that are in scale
        //var vectorLayers = allLayers.OfType<VectorLayer>()
        //    .Where(layer =>
        //        layer.Type == LayerType.VectorLayer &&
        //        layer.Visibility == System.Windows.Visibility.Visible &&
        //        layer.CanRenderLayer(mapScale))
        //    .OrderBy(layer => layer.ZIndex)
        //    .ToList();

        //// Get EditableFeatureLayer instances
        //var editableFeatureLayers = allLayers.OfType<EditableFeatureLayer>()
        //    .Where(layer =>
        //        layer.Visibility == System.Windows.Visibility.Visible &&
        //        layer.CanRenderLayer(mapScale))
        //    .OrderBy(layer => layer.ZIndex)
        //    .ToList();

        //// Get DrawingLayer instances
        //var drawingLayers = allLayers.OfType<DrawingLayer>()
        //    .Where(layer =>
        //        layer.Visibility == System.Windows.Visibility.Visible &&
        //        layer.CanRenderLayer(mapScale))
        //    .OrderBy(layer => layer.ZIndex)
        //    .ToList();

        //// Get SpecialPointLayer instances
        //var specialPointLayers = allLayers.OfType<SpecialPointLayer>()
        //    .Where(layer =>
        //        layer.Visibility == System.Windows.Visibility.Visible &&
        //        layer.CanRenderLayer(mapScale))
        //    .OrderBy(layer => layer.ZIndex)
        //    .ToList();

        //// Get SpecialLineLayer instances
        //var specialLineLayers = allLayers.OfType<SpecialLineLayer>()
        //    .Where(layer =>
        //        layer.Visibility == System.Windows.Visibility.Visible &&
        //        layer.CanRenderLayer(mapScale))
        //    .OrderBy(layer => layer.ZIndex)
        //    .ToList();

        //// Get GridLayer instances
        //var gridLayers = allLayers.OfType<GridLayer>()
        //    .Where(layer =>
        //        layer.Visibility == System.Windows.Visibility.Visible &&
        //        layer.CanRenderLayer(mapScale))
        //    .OrderBy(layer => layer.ZIndex)
        //    .ToList();

        //foreach (var layer in vectorLayers)
        //{
        //    try
        //    {
        //        // Get features for current extent
        //        var featureSet = layer.DataSource.GetAsFeatureSet(boundingBox);

        //        if (featureSet == null || featureSet.Features == null || featureSet.Features.Count == 0)
        //            continue;

        //        // Filter features that intersect with bounding box
        //        var extentGeometry = boundingBox.AsGeometry<Point>(SridHelper.WebMercator);
        //        var allFeatures = featureSet.Features
        //            .Where(f => f.TheGeometry != null &&
        //                       !f.TheGeometry.IsNullOrEmpty() &&
        //                       f.TheGeometry.Intersects(extentGeometry))
        //            .ToList();

        //        if (allFeatures.Count == 0)
        //            continue;

        //        // Iterate through ALL symbolizers in the layer
        //        foreach (var symbolizer in layer.Symbolizers)
        //        {
        //            // Skip LabelSymbolizer (labels not supported in vector PDF)
        //            if (symbolizer is LabelSymbolizer)
        //                continue;

        //            // Check if symbolizer is in scale range
        //            if (!symbolizer.IsInScaleRange(mapScale))
        //                continue;

        //            // Filter features using symbolizer's filter
        //            var filteredFeatures = allFeatures
        //                .Where(symbolizer.IsFilterPassed)
        //                .ToList();

        //            if (filteredFeatures.Count == 0)
        //                continue;

        //            // Convert this symbolizer's visual parameters to PDF options
        //            var pdfOptions = ConvertSymbolizerToPdfOptions(symbolizer);

        //            // Create separate LayerPdfData for this symbolizer's filtered features
        //            var layerPdfData = new PdfWriter.LayerPdfData
        //            {
        //                Features = filteredFeatures,
        //                Options = pdfOptions,
        //                ZIndex = layer.ZIndex, // Use layer's ZIndex
        //                Opacity = layer.Opacity * (symbolizer.Param?.Opacity ?? 1.0),
        //                LayerName = layer.LayerName
        //            };

        //            layerPdfDataList.Add(layerPdfData);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine($"Error processing layer {layer.LayerName}: {ex.Message}");
        //        // Continue with other layers even if one fails
        //    }
        //}

        // Process EditableFeatureLayer instances
        //foreach (var layer in editableFeatureLayers)
        //{
        //    try
        //    {
        //        var features = ConvertEditableFeatureLayerToFeatures(layer, boundingBox);
        //        if (features.Count == 0)
        //            continue;

        //        // Process symbolizers (same logic as VectorLayer)
        //        foreach (var symbolizer in layer.Symbolizers)
        //        {
        //            if (symbolizer is LabelSymbolizer)
        //                continue;

        //            if (!symbolizer.IsInScaleRange(mapScale))
        //                continue;

        //            var filteredFeatures = features.Where(symbolizer.IsFilterPassed).ToList();
        //            if (filteredFeatures.Count == 0)
        //                continue;

        //            var pdfOptions = ConvertSymbolizerToPdfOptions(symbolizer);
        //            var layerPdfData = new PdfWriter.LayerPdfData
        //            {
        //                Features = filteredFeatures,
        //                Options = pdfOptions,
        //                ZIndex = layer.ZIndex,
        //                Opacity = layer.Opacity * (symbolizer.Param?.Opacity ?? 1.0),
        //                LayerName = layer.LayerName
        //            };

        //            layerPdfDataList.Add(layerPdfData);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine($"Error processing EditableFeatureLayer {layer.LayerName}: {ex.Message}");
        //    }
        //}

        //// Process DrawingLayer instances
        //foreach (var layer in drawingLayers)
        //{
        //    try
        //    {
        //        var features = ConvertDrawingLayerToFeatures(layer, boundingBox);
        //        if (features.Count == 0)
        //            continue;

        //        // Process symbolizers (same logic as VectorLayer)
        //        foreach (var symbolizer in layer.Symbolizers)
        //        {
        //            if (symbolizer is LabelSymbolizer)
        //                continue;

        //            if (!symbolizer.IsInScaleRange(mapScale))
        //                continue;

        //            var filteredFeatures = features.Where(symbolizer.IsFilterPassed).ToList();
        //            if (filteredFeatures.Count == 0)
        //                continue;

        //            var pdfOptions = ConvertSymbolizerToPdfOptions(symbolizer);
        //            var layerPdfData = new PdfWriter.LayerPdfData
        //            {
        //                Features = filteredFeatures,
        //                Options = pdfOptions,
        //                ZIndex = layer.ZIndex,
        //                Opacity = layer.Opacity * (symbolizer.Param?.Opacity ?? 1.0),
        //                LayerName = layer.LayerName
        //            };

        //            layerPdfDataList.Add(layerPdfData);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine($"Error processing DrawingLayer {layer.LayerName}: {ex.Message}");
        //    }
        //}

        // Process SpecialPointLayer instances (no symbolizers - use default symbology)
        //foreach (var layer in specialPointLayers)
        //{
        //    try
        //    {
        //        var features = ConvertSpecialPointLayerToFeatures(layer, boundingBox);
        //        if (features.Count == 0)
        //            continue;

        //        // Use default point symbology since SpecialPointLayer has no symbolizers
        //        var defaultPointOptions = new PdfOptions
        //        {
        //            FillColor = new RgbColor(0, 0, 255, 255), // Blue
        //            StrokeColor = new RgbColor(0, 0, 0, 255), // Black
        //            StrokeWidth = 1,
        //            PointCircleRadius = 3.0,
        //            Opacity = layer.Opacity
        //        };

        //        var layerPdfData = new PdfWriter.LayerPdfData
        //        {
        //            Features = features,
        //            Options = defaultPointOptions,
        //            ZIndex = layer.ZIndex,
        //            Opacity = layer.Opacity,
        //            LayerName = layer.LayerName
        //        };

        //        layerPdfDataList.Add(layerPdfData);
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine($"Error processing SpecialPointLayer {layer.LayerName}: {ex.Message}");
        //    }
        //}

        //// Process SpecialLineLayer instances
        //foreach (var layer in specialLineLayers)
        //{
        //    try
        //    {
        //        var features = ConvertSpecialLineLayerToFeatures(layer, boundingBox);
        //        if (features.Count == 0)
        //            continue;

        //        // Process symbolizers (same logic as VectorLayer)
        //        foreach (var symbolizer in layer.Symbolizers)
        //        {
        //            if (symbolizer is LabelSymbolizer)
        //                continue;

        //            if (!symbolizer.IsInScaleRange(mapScale))
        //                continue;

        //            var filteredFeatures = features.Where(symbolizer.IsFilterPassed).ToList();
        //            if (filteredFeatures.Count == 0)
        //                continue;

        //            var pdfOptions = ConvertSymbolizerToPdfOptions(symbolizer);
        //            var layerPdfData = new PdfWriter.LayerPdfData
        //            {
        //                Features = filteredFeatures,
        //                Options = pdfOptions,
        //                ZIndex = layer.ZIndex,
        //                Opacity = layer.Opacity * (symbolizer.Param?.Opacity ?? 1.0),
        //                LayerName = layer.LayerName
        //            };

        //            layerPdfDataList.Add(layerPdfData);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine($"Error processing SpecialLineLayer {layer.LayerName}: {ex.Message}");
        //    }
        //}

        //// Process GridLayer instances
        //foreach (var layer in gridLayers)
        //{
        //    try
        //    {
        //        var features = ConvertGridLayerToFeatures(layer, boundingBox);
        //        if (features.Count == 0)
        //            continue;

        //        // Filter features that intersect with bounding box
        //        var extentGeometry = boundingBox.AsGeometry<Point>(SridHelper.WebMercator);
        //        var allFeatures = features
        //            .Where(f => f.TheGeometry != null &&
        //                       !f.TheGeometry.IsNullOrEmpty() &&
        //                       f.TheGeometry.Intersects(extentGeometry))
        //            .ToList();

        //        if (allFeatures.Count == 0)
        //            continue;

        //        // Process symbolizers (same logic as VectorLayer)
        //        foreach (var symbolizer in layer.Symbolizers)
        //        {
        //            if (symbolizer is LabelSymbolizer)
        //                continue;

        //            if (!symbolizer.IsInScaleRange(mapScale))
        //                continue;

        //            var filteredFeatures = allFeatures
        //                .Where(symbolizer.IsFilterPassed)
        //                .ToList();

        //            if (filteredFeatures.Count == 0)
        //                continue;

        //            var pdfOptions = ConvertSymbolizerToPdfOptions(symbolizer);
        //            var layerPdfData = new PdfWriter.LayerPdfData
        //            {
        //                Features = filteredFeatures,
        //                Options = pdfOptions,
        //                ZIndex = layer.ZIndex,
        //                Opacity = layer.Opacity * (symbolizer.Param?.Opacity ?? 1.0),
        //                LayerName = layer.LayerName
        //            };

        //            layerPdfDataList.Add(layerPdfData);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine($"Error processing GridLayer {layer.LayerName}: {ex.Message}");
        //    }
        //}


        #endregion

        // Check if we have any layers to export (raster or vector)
        if (layerPdfDataList.Count == 0 && rasterLayerPdfDataList.Count == 0)
        {
            // No layers to export
            return;
        }

        // Create base PDF options
        var baseOptions = new PdfOptions
        {
            Title = "Map Export",
            Creator = "IRI.Maptor",
            PageSize = PdfPageSize.Auto,
            BoundingBoxPadding = 0.05 // 5% padding
        };

        // Generate PDF (pass both vector and raster layers)
        var pdfBytes = PdfWriter.WriteLayers(
            layerPdfDataList,
            boundingBox,
            mapScale,
            baseOptions,
            rasterLayerPdfDataList.Count > 0 ? rasterLayerPdfDataList : null,
            supportPdfLayers);

        // Save to file
        File.WriteAllBytes(fileName, pdfBytes);
    }

    /// <summary>
    /// Converts a single symbolizer's visual parameters to PDF options
    /// </summary>
    private PdfOptions ConvertSymbolizerToPdfOptions(ISymbolizer? symbolizer)
    {
        var options = new PdfOptions();

        if (symbolizer?.Param == null)
            return options;

        var visualParams = symbolizer.Param;

        // Convert fill brush to RgbColor
        if (visualParams.Fill != null)
        {
            var fillColor = visualParams.Fill.AsSolidColor();
            if (fillColor.HasValue)
            {
                options.FillColor = new RgbColor(
                    fillColor.Value.R,
                    fillColor.Value.G,
                    fillColor.Value.B,
                    fillColor.Value.A
                );
            }
        }

        // Convert stroke brush to RgbColor
        if (visualParams.Stroke != null)
        {
            var strokeColor = visualParams.Stroke.AsSolidColor();
            if (strokeColor.HasValue)
            {
                options.StrokeColor = new RgbColor(
                    strokeColor.Value.R,
                    strokeColor.Value.G,
                    strokeColor.Value.B,
                    strokeColor.Value.A
                );
            }
        }

        // Set stroke width and opacity
        options.StrokeWidth = visualParams.StrokeThickness;
        options.Opacity = visualParams.Opacity;

        // Handle point symbol size for SimplePointSymbolizer
        if (symbolizer is SimplePointSymbolizer pointSymbolizer)
        {
            options.PointCircleRadius = Math.Max(pointSymbolizer.SymbolWidth, pointSymbolizer.SymbolHeight) / 2.0;
        }

        return options;
    }

    /// <summary>
    /// Converts EditableFeatureLayer to Feature collection
    /// </summary>
    //private List<Feature<Point>> ConvertEditableFeatureLayerToFeatures(EditableFeatureLayer layer, BoundingBox boundingBox)
    //{
    //    var geometry = layer.GetFinalGeometry();
    //    if (geometry == null || geometry.IsNullOrEmpty())
    //        return new List<Feature<Point>>();

    //    // Check if geometry intersects bounding box
    //    var extentGeometry = boundingBox.AsGeometry<Point>(SridHelper.WebMercator);
    //    if (!geometry.Intersects(extentGeometry))
    //        return new List<Feature<Point>>();

    //    return new List<Feature<Point>> { new Feature<Point>(geometry) };
    //}

    /// <summary>
    /// Converts DrawingLayer to Feature collection
    /// </summary>
    //private List<Feature<Point>> ConvertDrawingLayerToFeatures(DrawingLayer layer, BoundingBox boundingBox)
    //{
    //    var geometry = layer.GetFinalGeometry();
    //    if (geometry == null || geometry.IsNullOrEmpty())
    //        return new List<Feature<Point>>();

    //    var extentGeometry = boundingBox.AsGeometry<Point>(SridHelper.WebMercator);
    //    if (!geometry.Intersects(extentGeometry))
    //        return new List<Feature<Point>>();

    //    return new List<Feature<Point>> { new Feature<Point>(geometry) };
    //}

    /// <summary>
    /// Converts SpecialPointLayer to Feature collection
    /// </summary>
    //private List<Feature<Point>> ConvertSpecialPointLayerToFeatures(SpecialPointLayer layer, BoundingBox boundingBox)
    //{
    //    if (layer.Items == null || layer.Items.Count == 0)
    //        return new List<Feature<Point>>();

    //    // Filter points within bounding box
    //    var points = layer.Items
    //        .Where(item => boundingBox.Contains(new Point(item.X, item.Y)))
    //        .Select(item => new Point(item.X, item.Y))
    //        .ToList();

    //    if (points.Count == 0)
    //        return new List<Feature<Point>>();

    //    // Create single Point or MultiPoint geometry
    //    Geometry<Point> geometry;
    //    if (points.Count == 1)
    //    {
    //        geometry = Geometry<Point>.Create(points, GeometryType.Point, SridHelper.WebMercator);
    //    }
    //    else
    //    {
    //        geometry = Geometry<Point>.Create(points, GeometryType.MultiPoint, SridHelper.WebMercator);
    //    }

    //    return new List<Feature<Point>> { new Feature<Point>(geometry) };
    //}

    /// <summary>
    /// Converts SpecialLineLayer to Feature collection
    /// </summary>
    //private List<Feature<Point>> ConvertSpecialLineLayerToFeatures(SpecialLineLayer layer, BoundingBox boundingBox)
    //{
    //    var pointCollection = layer.PointCollection;
    //    if (pointCollection == null || pointCollection.Count < 2)
    //        return new List<Feature<Point>>();

    //    // Check if line intersects bounding box
    //    var lineGeometry = Geometry<Point>.Create(pointCollection, GeometryType.LineString, SridHelper.WebMercator);
    //    var extentGeometry = boundingBox.AsGeometry<Point>(SridHelper.WebMercator);

    //    if (!lineGeometry.Intersects(extentGeometry))
    //        return new List<Feature<Point>>();

    //    return new List<Feature<Point>> { new Feature<Point>(lineGeometry) };
    //}

    /// <summary>
    /// Converts GridLayer to Feature collection
    /// </summary>
    //private List<Feature<Point>> ConvertGridLayerToFeatures(GridLayer layer, BoundingBox boundingBox)
    //{
    //    if (layer.DataSource == null)
    //        return new List<Feature<Point>>();

    //    var featureSet = layer.DataSource.GetAsFeatureSet(boundingBox);
    //    if (featureSet?.Features == null || featureSet.Features.Count == 0)
    //        return new List<Feature<Point>>();

    //    return featureSet.Features
    //        .Where(f => f.TheGeometry != null && !f.TheGeometry.IsNullOrEmpty())
    //        .ToList();
    //}

    public async Task SetPrintAreaAsync()
    {
        // select a rectangle 
        var polygon = await GetDrawingAsync(DrawMode.Rectangle);

        if (polygon.IsNullOrEmpty())
            return;

        var boundingBox = polygon.Result.GetBoundingBox();

        PrintArea = boundingBox;
    }

    #endregion


    private async Task ShowExceptionMessageAsync(Exception ex)
    {
        if (ex is DomainException domainException)
        {
            await this.DialogService.ShowErrorMessage(domainException);
        }
        else if (ex is IOException)
        {
            await this.DialogService.ShowErrorMessage(MaptorLockedFileException.Instance);
            //await DialogService.ShowMessageAsync(_fileLockedError, _error, owner);
        }
        else if (ex is UnauthorizedAccessException)
        {
            await this.DialogService.ShowErrorMessage(MaptorLockedFileException.Instance);
            //await DialogService.ShowMessageAsync(_fileLockedError, _error, owner);
        }
        else if (ex is FormatException)
        {
            await this.DialogService.ShowErrorMessage(MaptorFormatException.Instance);
        }
        else
        {
            await this.DialogService.ShowErrorMessage(new MaptorUnknownException(ex.Message));
        }
    }


    public void Refresh(bool isNewExtent)
    {
        RequestRefresh?.Invoke(isNewExtent);
    }



    //*****************************************File Formats *********************************************************
    #region Shapefile/Worldfile/GeoJson/KML/KMZ/Csv/Tsv/ZippedImagePyramid/etc.

    public virtual async Task AddShapefile(object owner, int? maxSizeInKB)
    {
        IsBusy = true;

        var fileName = await DialogService.ShowOpenFileDialogAsync(DataSourceKind.Shapefile/*"shapefile|*.shp"*/, owner);

        if (!File.Exists(fileName))
        {
            IsBusy = false;

            return;
        }

        FileInfo info = new FileInfo(fileName);

        if (!this.MapSettings.AllowLargeDataLoading && maxSizeInKB.HasValue && info.Length / 10000.0 > maxSizeInKB) //5k
        {
            await ShowExceptionMessageAsync(MaptorFileSizeExceedToOpenException.Instance);

            //await DialogService.ShowMessageAsync("حجم فایل انتخابی بیش از حد مجاز است", "خطا", owner);
            IsBusy = false;

            return;
        }

        System.Diagnostics.Debug.WriteLine($"***** AddShapefile begin {DateTime.Now.ToLongTimeString()}");

        await AddShapefile(fileName, owner);

        System.Diagnostics.Debug.WriteLine($"***** AddShapefile end {DateTime.Now.ToLongTimeString()}");
    }

    public async Task AddShapefile(string fileName, object owner)
    {
        try
        {
            var dataSource = ShapefileDataSourceFactory.CreateLazy(fileName, /*SrsBases.WebMercator,*/ null);

            var vectorLayer = new VectorLayer(Path.GetFileNameWithoutExtension(fileName),
                                dataSource,
                                [SimpleSymbolizer.Create(null, BrushHelper.PickBrush(), 3, 1)], //new VisualParameters(null, BrushHelper.PickBrush(), 3, 1),
                                LayerType.VectorLayer,
                                RenderMode.Default,
                                RasterizationMethod.GdiPlus,
                                ScaleInterval.All,
                                LegendViewModel.DefaultTocGroup)
            {
                IsSearchable = true
            };

            AddLayer(vectorLayer);
        }
        catch (Exception ex)
        {
            await ShowExceptionMessageAsync(ex);
            //await DialogService.ShowMessageAsync(ex.Message, _error, owner);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public virtual async Task AddKmlfile(object owner, int? maxSizeInKB)
    {
        IsBusy = true;

        var fileName = await DialogService.ShowOpenFileDialogAsync(DataSourceKind.Kml/*"Keyhole Markup Language (KML)|*.kml"*/, owner);

        if (!File.Exists(fileName))
        {
            IsBusy = false;

            return;
        }

        FileInfo info = new FileInfo(fileName);

        if (maxSizeInKB.HasValue && info.Length / 10000.0 > maxSizeInKB) //5k
        {
            await ShowExceptionMessageAsync(MaptorFileSizeExceedToOpenException.Instance);
            //await DialogService.ShowMessageAsync("حجم فایل انتخابی بیش از حد مجاز است", "خطا", owner);
            IsBusy = false;

            return;
        }

        await AddKmlfile(fileName, owner);
    }

    public async Task AddKmlfile(string fileName, object owner)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fileName) || !File.Exists(fileName))
                throw new MaptorFileNotFoundException(fileName);

            List<Feature<Point>> features;

            try
            {
                var kmlFeatures = await KmlReader.ReadFeaturesFromFileAsync(fileName);
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
                    var geometries = await KmlReader.ReadFromFileAsync(fileName);
                    features = geometries.ToFeatures();
                }
                catch
                {
                    features = new List<Feature<Point>>();
                }
            }

            if (features.IsNullOrEmpty())
            {
                throw new MaptorEmptyFileException();
                //await DialogService.ShowMessageAsync("هیچ عارضه‌ای در فایل KML یافت نشد.", _error, owner);
                //return;
            }

            features = features.Select(f => f.Transform(MapProjects.GeodeticWgs84ToWebMercator<Point>, SridHelper.WebMercator)).ToList();

            var dataSource = KmlDataSource.Create(fileName, features);

            var geometryType = features.First().GeometryType/*TheGeometry.Type*/;

            var symbolizers = features.CreateSymbolizersFromKml(geometryType);

            var vectorLayer = new VectorLayer(Path.GetFileNameWithoutExtension(fileName),
                                dataSource,
                                symbolizers,
                                LayerType.VectorLayer,
                                RenderMode.Default,
                                RasterizationMethod.GdiPlus,
                                ScaleInterval.All,
                                LegendViewModel.DefaultTocGroup)
            {
                IsSearchable = true
            };

            AddLayer(vectorLayer);
        }
        //catch (IOException)
        //{
        //    await HandleError(FileIsLockedException.Instance);
        //    //await DialogService.ShowMessageAsync(_fileLockedError, _error, owner);
        //}
        //catch (UnauthorizedAccessException)
        //{
        //    await HandleError(FileIsLockedException.Instance);
        //    //await DialogService.ShowMessageAsync(_fileLockedError, _error, owner);
        //}
        catch (Exception ex)
        {
            await ShowExceptionMessageAsync(ex);
            //await DialogService.ShowMessageAsync(ex.Message, _error, owner);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public virtual async Task AddKmzfile(object owner, int? maxSizeInKB)
    {
        IsBusy = true;

        var fileName = await DialogService.ShowOpenFileDialogAsync(DataSourceKind.Kmz/*"Compressed KML files (KMZ)|*.kmz"*/, owner);

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

        await AddKmzfile(fileName, owner);
    }

    public async Task AddKmzfile(string fileName, object owner)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fileName) || !File.Exists(fileName))
                throw new MaptorFileNotFoundException(fileName);
            //throw new System.IO.FileNotFoundException($"KMZ file '{fileName}' was not found.", fileName);

            List<Feature<Point>> features;

            try
            {
                var kmzFeatures = await KmzReader.ReadFeaturesFromFileAsync(fileName);
                features = kmzFeatures.ToFeatures();
            }
            catch
            {
                features = new List<Feature<Point>>();
            }

            if (features.IsNullOrEmpty())
            {
                try
                {
                    var geometries = await KmzReader.ReadFromFileAsync(fileName);
                    features = geometries.ToFeatures();
                }
                catch
                {
                    features = new List<Feature<Point>>();
                }
            }

            if (features.IsNullOrEmpty())
            {
                //await DialogService.ShowMessageAsync("هیچ عارضه‌ای در فایل KML یافت نشد.", _error, owner);
                await ShowExceptionMessageAsync(new MaptorEmptyFileException());

                return;
            }

            features = features.Select(f => f.Transform(MapProjects.GeodeticWgs84ToWebMercator<Point>, SridHelper.WebMercator)).ToList();

            var dataSource = KmzDataSource.Create(fileName, features);
            var geometryType = features.First().GeometryType/*TheGeometry.Type*/;
            var symbolizers = features.CreateSymbolizersFromKml(geometryType);

            var vectorLayer = new VectorLayer(Path.GetFileNameWithoutExtension(fileName),
                                dataSource,
                                symbolizers,
                                LayerType.VectorLayer,
                                RenderMode.Default,
                                RasterizationMethod.GdiPlus,
                                ScaleInterval.All,
                                LegendViewModel.DefaultTocGroup)
            {
                IsSearchable = true
            };

            AddLayer(vectorLayer);
        }
        catch (Exception ex)
        {
            await ShowExceptionMessageAsync(ex);
        }
        //catch (IOException)
        //{
        //    await DialogService.ShowMessageAsync(_fileLockedError, _error, owner);
        //}
        //catch (UnauthorizedAccessException)
        //{
        //    await DialogService.ShowMessageAsync(_fileLockedError, _error, owner);
        //}
        //catch (Exception ex)
        //{
        //    await DialogService.ShowMessageAsync(ex.Message, _error, owner);
        //}
        finally
        {
            IsBusy = false;
        }
    }

    public virtual async Task AddGpxfile(object owner, int? maxSizeInKB = null)
    {
        IsBusy = true;

        var fileName = await DialogService.ShowOpenFileDialogAsync(DataSourceKind.Gpx/*"GPS Exchange Format (GPX)|*.gpx"*/, owner);

        if (!File.Exists(fileName))
        {
            IsBusy = false;
            return;
        }

        if (maxSizeInKB.HasValue)
        {
            var info = new FileInfo(fileName);
            if (info.Length / 1000.0 > maxSizeInKB)
            {
                //await DialogService.ShowMessageAsync("حجم فایل انتخابی بیش از حد مجاز است", _error, owner);
                await ShowExceptionMessageAsync(MaptorFileSizeExceedToOpenException.Instance);

                IsBusy = false;

                return;
            }
        }

        await AddGpxfile(fileName, owner);
    }

    public async Task AddGpxfile(string fileName, object owner)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fileName) || !File.Exists(fileName))
                throw new MaptorFileNotFoundException(fileName);
            //throw new System.IO.FileNotFoundException($"GPX file '{fileName}' was not found.", fileName);

            var features = new List<Feature<Point>>();

            //try
            //{
            var parsed = GpxFormat.Parse(fileName);

            foreach (var wpt in parsed.Waypoints)
            {
                var point = new Point(wpt.Longitude, wpt.Latitude);
                var geom = Geometry<Point>.CreatePointOrLineString(new List<Point> { point }, SridHelper.GeodeticWGS84);
                var attrs = new Dictionary<string, object?>();
                if (!string.IsNullOrWhiteSpace(wpt.Name)) attrs["name"] = wpt.Name;
                if (!string.IsNullOrWhiteSpace(wpt.Description)) attrs["description"] = wpt.Description;
                if (wpt.Elevation.HasValue) attrs["elevation"] = wpt.Elevation.Value;
                features.Add(new Feature<Point>(geom, attrs));
            }

            foreach (var route in parsed.Routes)
            {
                if (route.RoutePoints == null || route.RoutePoints.Count < 2) continue;
                var points = route.RoutePoints.Select(p => new Point(p.Longitude, p.Latitude)).ToList();
                var geom = Geometry<Point>.CreatePointOrLineString(points, SridHelper.GeodeticWGS84);
                var attrs = new Dictionary<string, object?>();
                if (!string.IsNullOrWhiteSpace(route.Name)) attrs["name"] = route.Name;
                features.Add(new Feature<Point>(geom, attrs));
            }

            foreach (var track in parsed.Tracks)
            {
                foreach (var segment in track.Segments ?? Enumerable.Empty<GpxTrackSegment>())
                {
                    if (segment.TrackPoints == null || segment.TrackPoints.Count < 2) continue;
                    var points = segment.TrackPoints
                        .Select(p => new Point(p.Longitude, p.Latitude))
                        .ToList();
                    var geom = Geometry<Point>.CreatePointOrLineString(points, SridHelper.GeodeticWGS84);
                    var attrs = new Dictionary<string, object?>();
                    if (!string.IsNullOrWhiteSpace(track.Name)) attrs["name"] = track.Name;
                    features.Add(new Feature<Point>(geom, attrs));
                }
            }
            //}
            //catch (Exception ex)
            //{
            //    throw;
            //    //await DialogService.ShowMessageAsync(ex.Message, _error, owner);
            //    //return;
            //}

            if (features.IsNullOrEmpty())
            {
                throw MaptorEmptyFileException.Instance;
                //await DialogService.ShowMessageAsync("هیچ عارضه‌ای در فایل GPX یافت نشد.", _error, owner);
                //return;
            }

            features = features.Select(f => f.Transform(MapProjects.GeodeticWgs84ToWebMercator<Point>, SridHelper.WebMercator)).ToList();

            var dataSource = GpxDataSource.Create(fileName, features);
            var geometryType = features.First().GeometryType/*TheGeometry.Type*/;
            var symbolizers = features.CreateSymbolizersFromKml(geometryType);

            var vectorLayer = new VectorLayer(Path.GetFileNameWithoutExtension(fileName),
                                dataSource,
                                symbolizers,
                                LayerType.VectorLayer,
                                RenderMode.Default,
                                RasterizationMethod.GdiPlus,
                                ScaleInterval.All,
                                LegendViewModel.DefaultTocGroup)
            {
                IsSearchable = true
            };

            AddLayer(vectorLayer);
        }
        //catch (IOException)
        //{
        //    await DialogService.ShowMessageAsync(_fileLockedError, _error, owner);
        //}
        //catch (UnauthorizedAccessException)
        //{
        //    await DialogService.ShowMessageAsync(_fileLockedError, _error, owner);
        //}
        catch (Exception ex)
        {
            await ShowExceptionMessageAsync(ex);

            //await DialogService.ShowMessageAsync(ex.Message, _error, owner);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public virtual async Task AddDxffile(int? maxSizeInKB)
    {
        IsBusy = true;

        var result = await DialogService.ShowDxfOpenDialogAsync(ownerWindow: null);

        if (result == null)
        {
            IsBusy = false;
            return;
        }

        if (!File.Exists(result.FilePath))
        {
            IsBusy = false;
            return;
        }

        FileInfo info = new FileInfo(result.FilePath);

        if (maxSizeInKB.HasValue && info.Length / 10000.0 > maxSizeInKB) //5k
        {
            await ShowExceptionMessageAsync(MaptorFileSizeExceedToOpenException.Instance);
            //await DialogService.ShowMessageAsync("حجم فایل انتخابی بیش از حد مجاز است", "خطا", ownerWindow: null);
            IsBusy = false;

            return;
        }

        await AddDxffile(result.FilePath, owner: null, result.SelectedSrid);
    }

    public async Task AddDxffile(string fileName, object owner, int sourceSrid)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fileName) || !File.Exists(fileName))
                throw new MaptorFileNotFoundException(fileName);
            //throw new System.IO.FileNotFoundException($"DXF file '{fileName}' was not found.", fileName);

            var geometries = await DxfReader.ReadFromFile(fileName, sourceSrid);

            if (geometries.IsNullOrEmpty())
            {
                throw MaptorEmptyFileException.Instance;
                //await DialogService.ShowMessageAsync("هیچ عارضه‌ای در فایل DXF یافت نشد.", _error, owner);
                //return;
            }

            if (geometries.Any(g => g.Srid == 0))
            {
                throw MaptorDxfSrsNotFoundException.Instance;
                //await DialogService.ShowMessageAsync("سیستم مختصات DXF یافت نشد.", _error, owner);
                //return;
            }

            var groups = geometries.GroupBy(g => g.Type).ToList();

            // create the parent group layer
            GroupLayer groupLayer = new GroupLayer(Path.GetFileNameWithoutExtension(fileName));

            foreach (var group in groups)
            {
                var features = group.Select(g => g.AsFeature()).ToList();

                if (features.IsNullOrEmpty())
                {
                    throw MaptorEmptyFileException.Instance;
                    //await DialogService.ShowMessageAsync("هیچ عارضه‌ای در فایل DXF یافت نشد.", _error, owner);
                    //return;
                }

                features = features.Select(f => f.Project(SrsBases.WebMercator/*new WebMercator()*/)).ToList();

                var dataSource = DxfDataSource.Create(fileName, features, sourceSrid);

                //var geometryType = features.First().GeometryType/*TheGeometry.Type*/;

                var symbolizers = new List<ISymbolizer> { SimpleSymbolizer.Create(null, BrushHelper.PickBrush(), 3, 1) };

                var vectorLayer = new VectorLayer($"{Path.GetFileNameWithoutExtension(fileName)}-{group.Key}",
                                    dataSource,
                                    symbolizers,
                                    LayerType.VectorLayer,
                                    RenderMode.Default,
                                    RasterizationMethod.GdiPlus,
                                    ScaleInterval.All,
                                    LegendViewModel.DefaultTocGroup)
                {
                    IsSearchable = true
                };

                groupLayer.AddSubLayer(vectorLayer);
            }

            AddLayer(groupLayer);
        }
        catch (Exception ex)
        {
            await ShowExceptionMessageAsync(ex);
            //await DialogService.ShowMessageAsync(ex.Message, _error, owner);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public virtual async Task AddWgs84Worldfile(object owner)
    {
        IsBusy = true;

        var fileName = await DialogService.ShowOpenFileDialogAsync(DataSourceKind.Worldfile/*"Worldfile|*.bmp;*.jpg;*.jpeg;*.png;*.tif;*.tiff"*/, owner);

        if (!File.Exists(fileName))
        {
            IsBusy = false;

            return;
        }

        await AddWorldfile(fileName, SridHelper.GeodeticWGS84, owner);
    }

    public virtual async Task AddWebMercatorWorldfile(object owner)
    {
        IsBusy = true;

        try
        {
            var fileName = await DialogService.ShowOpenFileDialogAsync(DataSourceKind.Worldfile/*"Worldfile|*.bmp;*.jpg;*.jpeg;*.png;*.tif;*.tiff"*/, owner);

            if (!File.Exists(fileName))
            {
                IsBusy = false;

                return;
            }

            await AddWorldfile(fileName, SridHelper.WebMercator, owner);
        }
        //catch (IOException)
        //{
        //    await DialogService.ShowMessageAsync(_fileLockedError, _error, owner);
        //}
        //catch (UnauthorizedAccessException)
        //{
        //    await DialogService.ShowMessageAsync(_fileLockedError, _error, owner);
        //}
        catch (Exception ex)
        {
            await ShowExceptionMessageAsync(ex);
            //await DialogService.ShowMessageAsync(ex.Message, _error, owner);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public virtual async Task AddWorldfile(string fileName, int srid, object owner)
    {
        try
        {
            var dataSource = await GeoRasterFileDataSource.CreateAsync(fileName, DataSourceKind.Worldfile, srid);

            if (dataSource == null)
                return;

            var rasterLayer = new RasterLayer(
                dataSource,
                Path.GetFileNameWithoutExtension(fileName),
                LayerType.Raster,
                .9,
                System.Windows.Visibility.Visible,
                ScaleInterval.All);

            AddLayer(rasterLayer);
        }
        //catch (IOException)
        //{
        //    await DialogService.ShowMessageAsync(_fileLockedError, _error, owner);
        //}
        //catch (UnauthorizedAccessException)
        //{
        //    await DialogService.ShowMessageAsync(_fileLockedError, _error, owner);
        //}
        catch (Exception ex)
        {
            await ShowExceptionMessageAsync(ex);
            //await DialogService.ShowMessageAsync(ex.Message, _error, owner);
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

            var fileName = await DialogService.ShowOpenFileDialogAsync(DataSourceKind.ZippedImagePyramid/*"Image Pyramid file|*.pyrmd"*/, owner);

            if (!File.Exists(fileName))
            {
                IsBusy = false;

                return;
            }

            var rasterLayer = new RasterLayer(new ZippedImagePyramidDataSource(fileName),
                Path.GetFileNameWithoutExtension(fileName),
                LayerType.ImagePyramid,
                1,
                System.Windows.Visibility.Visible,
                ScaleInterval.All);

            AddLayer(rasterLayer);
        }
        //catch (IOException)
        //{
        //    await DialogService.ShowMessageAsync(_fileLockedError, _error, owner);
        //}
        //catch (UnauthorizedAccessException)
        //{
        //    await DialogService.ShowMessageAsync(_fileLockedError, _error, owner);
        //}
        catch (Exception ex)
        {
            await ShowExceptionMessageAsync(ex);
            //await DialogService.ShowMessageAsync(ex.Message, _error, owner);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// <summary>
    /// Opens the CSV/TSV import dialog and adds the layer.
    /// </summary>
    public virtual async Task AddDelimitedTextFile(object owner, bool initialIsCsv)
    {
        try
        {
            IsBusy = true;

            var result = await DialogService.ShowCsvTsvOpenDialogAsync(ownerWindow: null, initialIsCsv: initialIsCsv, initialSrid: null);

            if (result == null || string.IsNullOrWhiteSpace(result.RawText))
            {
                IsBusy = false;
                return;
            }

            DataSourceKind dataSourceKind = result.IsCsv ? DataSourceKind.Csv : DataSourceKind.Tsv;

            TextDataSource dataSource;

            if (result.IsFileSelected)
            {
                dataSource = await TextDataSource.CreateFromFileAsync(
                                        result.FilePath!,
                                        result.GeometryType,
                                        dataSourceKind,
                                        result.SelectedSrid,
                                        result.IsLongitudeFirst,
                                        result.UseFirstLineAsHeader);
            }
            else
            {
                dataSource = await TextDataSource.CreateFromTextAsync(
                                        result.RawText,
                                        result.GeometryType,
                                        dataSourceKind,
                                        result.SelectedSrid,
                                        result.IsLongitudeFirst,
                                        result.UseFirstLineAsHeader);
            }


            //MemoryDataSource dataSource = result.IsCsv
            //? await CsvDataSource.CreateFromTextAsync(result.RawText, result.SelectedSrid, result.IsLongitudeFirst, result.GeometryType, result.UseFirstLineAsHeader)
            //: await TsvDataSource.CreateFromTextAsync(result.RawText, result.SelectedSrid, result.IsLongitudeFirst, result.GeometryType, result.UseFirstLineAsHeader);

            if (dataSource == null)
                return;

            var layerName = !string.IsNullOrEmpty(result.FilePath)
                ? Path.GetFileNameWithoutExtension(result.FilePath)
                : (result.IsCsv ? "CSV Import" : "TSV Import");

            AddLayer(new VectorLayer(
                            layerName,
                            dataSource,
                            VisualParameters.CreateNew(0.9),
                            LayerType.VectorLayer,
                            RenderMode.Default,
                            RasterizationMethod.GdiPlus,
                            ScaleInterval.All,
                            LegendViewModel.DefaultTocGroup)
            { IsSearchable = true });
        }
        //catch (IOException)
        //{
        //    await DialogService.ShowMessageAsync(_fileLockedError, _error, owner);
        //}
        //catch (UnauthorizedAccessException)
        //{
        //    await DialogService.ShowMessageAsync(_fileLockedError, _error, owner);
        //}
        catch (Exception ex)
        {
            await ShowExceptionMessageAsync(ex);
            //await DialogService.ShowMessageAsync(ex.Message, _error, owner);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public virtual async Task AddGeoJson(object owner)
    {
        try
        {
            IsBusy = true;

            var result = await DialogService.ShowGeoJsonTopoJsonOpenDialogAsync(owner, isGeoJson: true, null);

            if (result == null || string.IsNullOrWhiteSpace(result.RawJson))
            {
                IsBusy = false;
                return;
            }

            MemoryDataSource dataSource = result.IsFileSelected
                ? await GeoJsonDataSource.CreateFromFileAsync(result.FilePath!, result.IsLongitudeFirst, result.SelectedSrid)
                : await GeoJsonDataSource.CreateFromTextAsync(result.RawJson, result.IsLongitudeFirst, result.SelectedSrid);

            var layerName = !string.IsNullOrEmpty(result.FilePath)
                ? Path.GetFileNameWithoutExtension(result.FilePath)
                : "GeoJSON Import";

            AddLayer(new VectorLayer(layerName, dataSource,
                [SimpleSymbolizer.Create(null, BrushHelper.PickBrush(), 3, 1)],
                LayerType.VectorLayer,
                RenderMode.Default,
                RasterizationMethod.GdiPlus,
                ScaleInterval.All,
                LegendViewModel.DefaultTocGroup)
            {
                IsSearchable = true
            });
        }
        catch (Exception ex)
        {
            await ShowExceptionMessageAsync(ex);
            //await DialogService.ShowMessageAsync(ex.Message, _error, owner);
        }
        finally
        {
            IsBusy = false;
        }
    }

    //public async Task AddGeoJson(string geoJsonFeatureSetFileName, object owner)
    //{
    //    try
    //    {
    //        var dataSource = await GeoJsonDataSource.CreateFromFileAsync(geoJsonFeatureSetFileName);

    //        var vectorLayer = new VectorLayer(Path.GetFileNameWithoutExtension(geoJsonFeatureSetFileName), dataSource,
    //            [SimpleSymbolizer.Create(null, BrushHelper.PickBrush(), 3, 1)],
    //            LayerType.VectorLayer,
    //            RenderMode.Default,
    //            RasterizationMethod.GdiPlus, ScaleInterval.All)
    //        {
    //            IsSearchable = true
    //        };

    //        AddLayer(vectorLayer);
    //    }
    //    catch (Exception ex)
    //    {
    //        await ShowExceptionMessageAsync(ex);
    //        //await DialogService.ShowMessageAsync(ex.Message, _error, owner);
    //    }
    //    finally
    //    {
    //        IsBusy = false;
    //    }
    //}

    public virtual async Task AddTopoJson(object owner)
    {
        try
        {
            IsBusy = true;

            var result = await DialogService.ShowGeoJsonTopoJsonOpenDialogAsync(owner, isGeoJson: false, null);

            if (result == null || string.IsNullOrWhiteSpace(result.RawJson))
            {
                IsBusy = false;
                return;
            }

            MemoryDataSource dataSource = result.IsFileSelected
                ? await TopoJsonDataSource.CreateFromFileAsync(result.FilePath!, result.SelectedSrid)
                : await TopoJsonDataSource.CreateFromTextAsync(result.RawJson, result.SelectedSrid);

            var layerName = !string.IsNullOrEmpty(result.FilePath)
                ? Path.GetFileNameWithoutExtension(result.FilePath)
                : "TopoJSON Import";

            AddLayer(new VectorLayer(layerName, dataSource,
                [SimpleSymbolizer.Create(null, BrushHelper.PickBrush(), 3, 1)],
                LayerType.VectorLayer,
                RenderMode.Default,
                RasterizationMethod.GdiPlus,
                ScaleInterval.All,
                LegendViewModel.DefaultTocGroup)
            {
                IsSearchable = true
            });
        }
        catch (Exception ex)
        {
            await ShowExceptionMessageAsync(ex);
            //await DialogService.ShowMessageAsync(ex.Message, _error, owner);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public virtual async Task AddEsriJson(object owner, int? maxSizeInKB)
    {
        IsBusy = true;

        var fileName = await DialogService.ShowOpenFileDialogAsync(DataSourceKind.EsriJson, owner);

        if (!File.Exists(fileName))
        {
            IsBusy = false;

            return;
        }

        FileInfo info = new FileInfo(fileName);

        if (maxSizeInKB.HasValue && info.Length / 10000.0 > maxSizeInKB) //5k
        {
            await ShowExceptionMessageAsync(MaptorFileSizeExceedToOpenException.Instance);

            IsBusy = false;

            return;
        }

        await AddEsriJson(fileName, owner);
    }

    public async Task AddEsriJson(string fileName, object owner)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fileName) || !File.Exists(fileName))
                throw new MaptorFileNotFoundException(fileName);

            //List<Feature<Point>> features;

            var dataSource = await EsriJsonDataSource.CreateFromFileAsync(fileName);

            if (dataSource is null)
                return;

            // read esri geojsons
            //var esriFeatureSet = await EsriJsonFeatureSet.Load(fileName);

            //if (esriFeatureSet is null || esriFeatureSet.Features.IsNullOrEmpty())
            //    throw new MaptorEmptyFileException();

            //var featureSet = esriFeatureSet.AsFeatureSet();

            //features = featureSet.Features.Select(f => f.Project(SrsBases.WebMercator)).ToList();

            //var dataSource = new MemoryDataSource(features, false, DataSourceKind.EsriJson);

            //var geometryType = features.First().GeometryType/*TheGeometry.Type*/;

            //var symbolizers = features.CreateSymbolizersFromKml(geometryType);

            var vectorLayer = new VectorLayer(Path.GetFileNameWithoutExtension(fileName),
                                dataSource,
                                [SimpleSymbolizer.Create(null, BrushHelper.PickBrush(), 3, 1)],
                                LayerType.VectorLayer,
                                RenderMode.Default,
                                RasterizationMethod.GdiPlus,
                                ScaleInterval.All,
                                LegendViewModel.DefaultTocGroup)
            {
                IsSearchable = true
            };

            AddLayer(vectorLayer);
        }
        catch (Exception ex)
        {
            await ShowExceptionMessageAsync(ex);
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


    private RelayCommand _rectangleZoomCommand;
    public RelayCommand RectangleZoomCommand
    {
        get
        {
            if (_rectangleZoomCommand == null)
            {
                _rectangleZoomCommand = new RelayCommand(param => this.MapAction = MapAction.ZoomInRectangle/*EnableRectangleZoomIn()*/);
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
                _zoomOutCommand = new RelayCommand(param => this.MapAction = MapAction.ZoomOut/*EnableZoomOut()*/);
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
                _previousExtentCommand = new RelayCommand(param => GoToPreviousExtent(), _ => PreviousExtentEnabled);
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
                _nextExtentCommand = new RelayCommand(param => GoToNextExtent(), _ => NextExtentEnabled);
            }

            return _nextExtentCommand;
        }
    }


    private RelayCommand _zoomInAtCenterCommand;
    public RelayCommand ZoomInAtCenterCommand
    {
        get
        {
            if (_zoomInAtCenterCommand == null)
            {
                _zoomInAtCenterCommand = new RelayCommand(param => ZoomInAtViewCenter());
            }

            return _zoomInAtCenterCommand;
        }
    }


    private RelayCommand _zoomOutAtCenterCommand;
    public RelayCommand ZoomOutAtCenterCommand
    {
        get
        {
            if (_zoomOutAtCenterCommand == null)
            {
                _zoomOutAtCenterCommand = new RelayCommand(param => ZoomOutAtViewCenter());
            }

            return _zoomOutAtCenterCommand;
        }
    }


    #endregion

    #region -   Layer Management Commands

    //private RelayCommand _moveLayerUpCommand;

    //public RelayCommand MoveLayerUpCommand
    //{
    //    get
    //    {
    //        if (_moveLayerUpCommand == null)
    //        {
    //            _moveLayerUpCommand = new RelayCommand(param =>MoveLayerUp(), this.);
    //        }

    //        return _moveLayerUpCommand;
    //    }
    //}


    //private RelayCommand _moveLayerDownCommand;

    //public RelayCommand MoveLayerDownCommand
    //{
    //    get
    //    {
    //        if (_moveLayerDownCommand == null)
    //        {
    //            _moveLayerDownCommand = new RelayCommand(param =>
    //            {
    //            });
    //        }

    //        return _moveLayerDownCommand;
    //    }
    //}


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


    private RelayCommand _addGeoJSONfileCommand;
    public RelayCommand AddGeoJSONfileCommand
    {
        get
        {
            if (_addGeoJSONfileCommand == null)
            {
                _addGeoJSONfileCommand = new RelayCommand(async param =>
                {
                    await AddGeoJson(param);
                });
            }
            return _addGeoJSONfileCommand;
        }
    }


    private RelayCommand _addTopoJsonCommand;
    public RelayCommand AddTopoJsonCommand
    {
        get
        {
            if (_addTopoJsonCommand == null)
            {
                _addTopoJsonCommand = new RelayCommand(async param =>
                {
                    await AddTopoJson(param);
                });
            }
            return _addTopoJsonCommand;
        }
    }


    private RelayCommand _addEsriJsonCommand;
    public RelayCommand AddEsriJsonCommand
    {
        get
        {
            if (_addEsriJsonCommand == null)
            {
                _addEsriJsonCommand = new RelayCommand(async param =>
                {
                    await AddEsriJson(param, null);
                });
            }
            return _addEsriJsonCommand;
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


    private RelayCommand _addKmzfileCommand;
    public RelayCommand AddKmzfileCommand
    {
        get
        {
            if (_addKmzfileCommand == null)
            {
                _addKmzfileCommand = new RelayCommand(async param =>
                {
                    await AddKmzfile(param, null);
                });
            }

            return _addKmzfileCommand;
        }
    }


    private RelayCommand _addGpxfileCommand;
    public RelayCommand AddGpxfileCommand
    {
        get
        {
            if (_addGpxfileCommand == null)
            {
                _addGpxfileCommand = new RelayCommand(async param =>
                {
                    await AddGpxfile(param, null);
                });
            }

            return _addGpxfileCommand;
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
                    await AddDxffile(null);
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
                _addWgs84WorldfileCommand = new RelayCommand(async param => { await AddWgs84Worldfile(param); });
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
                _addTsvCommand = new RelayCommand(async param => { await AddDelimitedTextFile(param, false); });
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
                _addCsvCommand = new RelayCommand(async param => { await AddDelimitedTextFile(param, true); });
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
                _changeBaseMapCommand = new RelayCommand(async param => await ChangeBaseMap(param?.ToString()));
                //{
                //try
                //{
                //    var provider = MapProviders.FirstOrDefault(m => m.Is(param?.ToString()));

                //    SetTileBaseMap(provider/*, BaseMapOpacity*/);
                //}
                //catch (Exception ex)
                //{
                //    await HandleError(ex);
                //    //Debug.WriteLine("exception at ChangeBaseMapCommand: " + ex);
                //}
                //});
            }

            return _changeBaseMapCommand;
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
                    //RequestClearAll?.Invoke();
                    this.ClearAll();
                });
            }

            return _clearAllCommand;
        }
    }


    private RelayCommand _clearVectorLayersCommand;
    public RelayCommand ClearVectorLayersCommand

    {
        get
        {
            if (_clearVectorLayersCommand == null)
            {
                _clearVectorLayersCommand = new RelayCommand(async param =>
                {
                    var sure = await DialogService.ShowYesNoDialogAsync(string.Empty);

                    if (sure != true)
                        return;

                    this.Clear(l => l.DataSource?.DataSourceKind.GetCategory() == DataSourceCategory.Vector, true);
                });
            }

            return _clearVectorLayersCommand;
        }
    }

    private RelayCommand _clearRasterLayersCommand;
    public RelayCommand ClearRasterLayersCommand

    {
        get
        {
            if (_clearRasterLayersCommand == null)
            {
                _clearRasterLayersCommand = new RelayCommand(param =>
                {
                    this.Clear(l => l.DataSource?.DataSourceKind.GetCategory() == DataSourceCategory.Raster, true);
                });
            }

            return _clearRasterLayersCommand;
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
                _measureLengthCommand = new RelayCommand(async param => _ = await Measure(DrawMode.Polyline));

            return _measureLengthCommand;
        }
    }

    private RelayCommand _measureAreaCommand;
    public RelayCommand MeasureAreaCommand
    {
        get
        {
            if (_measureAreaCommand == null)
                _measureAreaCommand = new RelayCommand(async param => _ = await Measure(DrawMode.Polygon));

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

    private RelayCommand _drawPolygonCommand;
    public RelayCommand DrawPolygonCommand
    {
        get
        {
            if (_drawPolygonCommand == null)
            {
                _drawPolygonCommand = new RelayCommand(param => this.MapAction = MapAction.DrawPolygon /*IsDrawPolygonMode = !IsDrawPolygonMode*/);
            }
            return _drawPolygonCommand;
        }
    }

    private RelayCommand _drawRectangleCommand;
    public RelayCommand DrawRectangleCommand
    {
        get
        {
            if (_drawRectangleCommand == null)
            {
                _drawRectangleCommand = new RelayCommand(param => this.MapAction = MapAction.DrawRectangle /*IsDrawRectangleMode = !IsDrawRectangleMode*/);
            }
            return _drawRectangleCommand;
        }
    }

    private RelayCommand _drawPolylineCommand;
    public RelayCommand DrawPolylineCommand
    {
        get
        {
            if (_drawPolylineCommand == null)
            {
                _drawPolylineCommand = new RelayCommand(param => this.MapAction = MapAction.DrawPolyline /*IsDrawPolylineMode = !IsDrawPolylineMode*/);
            }
            return _drawPolylineCommand;
        }
    }

    private RelayCommand _drawPointCommand;
    public RelayCommand DrawPointCommand
    {
        get
        {
            if (_drawPointCommand == null)
            {
                _drawPointCommand = new RelayCommand(param => this.MapAction = MapAction.DrawPoint /*IsDrawPointMode = !IsDrawPointMode*/);
            }
            return _drawPointCommand;
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

    private RelayCommand _finishNewDrawingPartCommand;
    public RelayCommand FinishNewDrawingPartCommand
    {
        get
        {
            if (_finishNewDrawingPartCommand == null)
            {
                _finishNewDrawingPartCommand = new RelayCommand(param => FinishDrawingPart());
            }
            return _finishNewDrawingPartCommand;
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

    #region -   Drawing Items Commands

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
                        var fileName = await DialogService.ShowOpenFileDialogAsync(DataSourceKind.GeoJson/*"*.json|*.json"*/, param);

                        if (string.IsNullOrWhiteSpace(fileName))
                            return;

                        var featureSet = await GeoJsonFeatureSet.LoadAsync(fileName);

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
                            throw MaptorSingleFeatureFileExpectedException.Instance;
                            //await DialogService.ShowMessageAsync("فایل جی‌سان حاوی تک عارضه باید باشد", _error, param);
                            //return;
                        }

                        AddDrawingItem(features.First().TheGeometry, Path.GetFileNameWithoutExtension(fileName)/*, null, int.MinValue*//*, dataSource*/);
                    }
                    //catch (IOException)
                    //{
                    //    await DialogService.ShowMessageAsync(_fileLockedError, _error, param);
                    //}
                    //catch (UnauthorizedAccessException)
                    //{
                    //    await DialogService.ShowMessageAsync(_fileLockedError, _error, param);
                    //}
                    catch (Exception ex)
                    {
                        await ShowExceptionMessageAsync(ex);
                        //await DialogService.ShowMessageAsync(ex.Message, _error, param);
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
                        var fileName = await DialogService.ShowOpenFileDialogAsync(DataSourceKind.Csv/*"*.csv|*.csv"*/, param);

                        if (string.IsNullOrWhiteSpace(fileName))
                            return;

                        var wgsPoints = IOHelper.ReadAllPoints(fileName, IOHelper.CsvDelimiterChar);

                        if (wgsPoints.IsNullOrEmpty())
                            return;

                        var webMercatorPoints = wgsPoints.Select(p => p.Project(SrsBases.GeodeticWgs84, SrsBases.WebMercator)).ToList();

                        var geometry = Geometry<Point>.CreatePointOrLineStringOrPolygon(webMercatorPoints, SridHelper.WebMercator);

                        AddDrawingItem(geometry, Path.GetFileNameWithoutExtension(fileName)/*, null, int.MinValue*//*, dataSource*/);
                    }
                    //catch (IOException)
                    //{
                    //    await DialogService.ShowMessageAsync(_fileLockedError, _error, param);
                    //}
                    //catch (UnauthorizedAccessException)
                    //{
                    //    await DialogService.ShowMessageAsync(_fileLockedError, _error, param);
                    //}
                    catch (Exception ex)
                    {
                        await ShowExceptionMessageAsync(ex);
                        //await DialogService.ShowMessageAsync(ex.Message, _error, param);
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
                        var fileName = await DialogService.ShowOpenFileDialogAsync(DataSourceKind.Shapefile/*"*.shp|*.shp"*/, param);

                        if (string.IsNullOrWhiteSpace(fileName))
                            return;

                        var dataSource = ShapefileDataSourceFactory.Create(fileName/*, SrsBases.WebMercator*//*new WebMercator()*/);

                        var featureSet = await dataSource.GetAsFeatureSetAsync();
                        var geometries = featureSet?.Features;

                        if (geometries.IsNullOrEmpty())
                            return;

                        if (geometries.Count != 1)
                        {
                            throw MaptorSingleFeatureFileExpectedException.Instance;
                            //await DialogService.ShowMessageAsync("شیپ فایل حاوی تک عارضه باید باشد", _error, param);
                            //return;
                        }

                        AddDrawingItem(geometries.First().TheGeometry, Path.GetFileNameWithoutExtension(fileName)/*, null, int.MinValue*//*, dataSource*/);
                    }
                    //catch (IOException)
                    //{
                    //    await DialogService.ShowMessageAsync(_fileLockedError, _error, param);
                    //}
                    //catch (UnauthorizedAccessException)
                    //{
                    //    await DialogService.ShowMessageAsync(_fileLockedError, _error, param);
                    //}
                    catch (Exception ex)
                    {
                        await ShowExceptionMessageAsync(ex);
                        //await DialogService.ShowMessageAsync(ex.Message, _error, param);
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
                    MoveDrawingItemUp(SelectedDrawingItem);
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
                    MoveDrawingItemDown(SelectedDrawingItem);
                });
            }

            return _moveDrawingItemDownCommand;
        }
    }

    private RelayCommand _openDrawingItemFileCommand;
    public RelayCommand OpenDrawingItemFileCommand
    {
        get
        {
            if (_openDrawingItemFileCommand == null)
            {
                _openDrawingItemFileCommand = new RelayCommand(async param =>
                {
                    await LoadDrawingItemFile(param);
                });
            }

            return _openDrawingItemFileCommand;
        }
    }

    private RelayCommand _saveDrawingItemFileCommand;

    public RelayCommand SaveDrawingItemFileCommand
    {
        get
        {
            if (_saveDrawingItemFileCommand == null)
            {
                _saveDrawingItemFileCommand =
                    new RelayCommand(
                        async param => await SaveDrawingItemFile(param),
                        _ => DrawingItems.Count > 0);
            }

            return _saveDrawingItemFileCommand;
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

    private RelayCommand _printToPdfCommand;
    public RelayCommand PrintToPdfCommand
    {
        get
        {
            if (_printToPdfCommand == null)
            {
                _printToPdfCommand = new RelayCommand(async param => await PrintToPdfAsync(param));
            }

            return _printToPdfCommand;
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
                _checkInternetAccessCommand = new RelayCommand(async param => { await CheckNetAccess(); });
            }

            return _checkInternetAccessCommand;
        }
    }



    private RelayCommand _closeAllTablesCommand;
    public RelayCommand CloseAllTablesCommand
    {
        get
        {
            if (_closeAllTablesCommand == null)
            {
                _closeAllTablesCommand = new RelayCommand(param =>
                {
                    this.RemoveSelectedLayers(l => true);

                    this.ShowAttributeTable = false;

                    this.ShowFeatureTablesOptions = false;
                });
            }

            return _closeAllTablesCommand;
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
}