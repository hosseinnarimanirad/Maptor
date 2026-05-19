using System;
using System.Windows;
using System.Windows.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using IRI.Maptor.Extensions;
using IRI.Maptor.Jab.Common.Events;
using IRI.Maptor.Jab.Common.Models;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Jab.Common.Models.Legend;
using IRI.Maptor.Sta.Persistence.Abstractions;
using System.Linq;
using System.Windows.Input;

namespace IRI.Maptor.Jab.Common.Layers;

public abstract class BaseLayer : Notifier, ILayer
{
    public BaseLayer()
    {
        LayerId = Guid.NewGuid();

        //this.ParentLayerId = Guid.Empty;

        //this.ParentLayerName = string.Empty;

        IsMovable = false;
    }

    #region Layer Id, Name

    /// <summary>
    /// Id of layer in datasource or api response
    /// to manage sublayers
    /// </summary>
    public int AuxilaryId { get; set; }

    public Guid LayerId { get; protected set; }

    public Guid ParentLayerId => Parent?.LayerId ?? Guid.Empty;

    public string ParentLayerName => Parent?.LayerName ?? string.Empty;

    private string _layerName = string.Empty;
    public string LayerName
    {
        get { return _layerName; }
        set
        {
            _layerName = value;
            RaisePropertyChanged();

            _onLayerNameChanged?.Invoke(this, new CustomEventArgs<string>(value));
        }
    }

    private ILayer? _parent;
    public ILayer? Parent
    {
        get { return _parent; }
        set
        {
            _parent = value;

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsRootLayer));
            RaisePropertyChanged(nameof(ParentLayerId));
            RaisePropertyChanged(nameof(ParentLayerName));
        }
    }


    public bool IsRootLayer => Parent is null;

    #endregion

    public abstract LayerType Type { get; /*protected set;*/ }


    //public virtual IDataSource? DataSource { get; protected set; }
    protected IDataSource? _dataSource;
    public virtual IDataSource? DataSource
    {
        get => _dataSource;
        protected set
        {
            UnsubscribeFromDataSourceStatusEvents(_dataSource);

            _dataSource = value;

            if (value != null)
            {
                UnsubscribeFromDataSourceStatusEvents(value);
                SubscribeToDataSourceStatusEvents(value);
            }
            //SyncStatusFromDataSource();

            RaisePropertyChanged(nameof(IsLoaded));
            RaisePropertyChanged(nameof(LayerNameCanBeChanged));
        }
    }


    public virtual SpatialModelMode SpatialModelMode { get; protected set; } = SpatialModelMode.None;

    protected BoundingBox _extent;
    public virtual BoundingBox Extent
    {
        get { return _extent; }
        protected set
        {
            _extent = value;
            RaisePropertyChanged();            
        }
    }

    public virtual RenderMode RenderMode { get => RenderMode.Default; /*protected set { } */}

    public virtual RasterizationMethod RasterizationMethod { get => RasterizationMethod.None;/* protected set { }*/ }

    #region Data source / status flags

    public virtual bool IsNotBusy => !IsBusy;

    public virtual bool IsBusy { get => IsInitializing || IsProcessing; }

    public virtual bool IsInitializing => DataSource?.IsInitializing ?? false;

    public virtual bool IsProcessing => DataSource?.IsProcessing ?? false;

    public virtual bool IsLoaded => DataSource?.IsLoaded ?? false;

    public virtual bool HasPendingChanges => DataSource?.HasPendingChanges ?? false;

    public virtual int NumberOfAddedFeatures => (DataSource as IEditableVectorDataSource)?.NumberOfAddedFeatures ?? 0;

    public virtual int NumberOfDeletedFeatures => (DataSource as IEditableVectorDataSource)?.NumberOfDeletedFeatures ?? 0;

    public virtual int NumberOfUpdatedFeatures => (DataSource as IEditableVectorDataSource)?.NumberOfUpdatedFeatures ?? 0;

    public virtual bool IsClientFiltered => DataSource?.HasClientFilter ?? false;

    public virtual bool HasError => DataSource?.HasError ?? false;

    public virtual bool LayerNameCanBeChanged
    {
        get
        {
            var dataSourceKind = DataSource?.DataSourceKind;

            if (dataSourceKind is null)
                return false;

            return dataSourceKind != Sta.Common.Enums.DataSourceKind.WebApi &&
                dataSourceKind != Sta.Common.Enums.DataSourceKind.GRPC;
        }
    }

    #region Data source status bindings

    protected void UnsubscribeFromDataSourceStatusEvents(IDataSource? dataSource)
    {
        if (dataSource == null)
            return;

        dataSource.IsInitializingChanged -= DataSource_IsInitializingChanged;
        dataSource.IsProcessingChanged -= DataSource_IsProcessingChanged;
        dataSource.IsLoadedChanged -= DataSource_IsLoadedChanged;
        dataSource.HasPendingChangesChanged -= DataSource_HasPendingChangesChanged;
        dataSource.IsClientFilteredChanged -= DataSource_IsClientFilteredChanged;
        dataSource.HasErrorChanged -= DataSource_HasErrorChanged;
        dataSource.OnExtentChanged -= DataSource_ExtentChanged;
    }

    protected void SubscribeToDataSourceStatusEvents(IDataSource? dataSource)
    {
        if (dataSource == null)
            return;

        dataSource.IsInitializingChanged += DataSource_IsInitializingChanged;
        dataSource.IsProcessingChanged += DataSource_IsProcessingChanged;
        dataSource.IsLoadedChanged += DataSource_IsLoadedChanged;
        dataSource.HasPendingChangesChanged += DataSource_HasPendingChangesChanged;
        dataSource.IsClientFilteredChanged += DataSource_IsClientFilteredChanged;
        dataSource.HasErrorChanged += DataSource_HasErrorChanged;
        dataSource.OnExtentChanged += DataSource_ExtentChanged; 
    }

    private void DataSource_ExtentChanged(object? sender, BoundingBox e)
    {
        this.Extent = e;
    }

    private static void DispatcherToUi(Action action)
    {
        var app = Application.Current;
        if (app?.Dispatcher == null || app.Dispatcher.HasShutdownStarted || app.Dispatcher.HasShutdownFinished)
        {
            action();
            return;
        }
        if (app.Dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            app.Dispatcher.BeginInvoke(action);
        }
    }

    //protected void DataSource_IsBusyChanged(object? sender, bool e) => DispatcherToUi(() => IsBusy = e);

    private void DataSource_IsInitializingChanged(object? sender, bool e) => DispatcherToUi(() =>
    {
        RaisePropertyChanged(nameof(IsInitializing));
        RaisePropertyChanged(nameof(IsBusy));
        RaisePropertyChanged(nameof(IsNotBusy));

        // Forces all commands to re-query CanExecute
        CommandManager.InvalidateRequerySuggested();
    });

    private void DataSource_IsProcessingChanged(object? sender, bool e) => DispatcherToUi(() =>
    {
        RaisePropertyChanged(nameof(IsProcessing));
        RaisePropertyChanged(nameof(IsBusy));
        RaisePropertyChanged(nameof(IsNotBusy));
    });

    protected virtual void DataSource_IsLoadedChanged(object? sender, bool e)
    {
        DispatcherToUi(() =>
        {
            RaisePropertyChanged(nameof(IsLoaded));

            if (e)
                RequestRefreshWhenDataLoaded?.Invoke(this);
        });
    }

    protected void DataSource_HasPendingChangesChanged(object? sender, bool e) => DispatcherToUi(() =>
    {
        RaisePropertyChanged(nameof(HasPendingChanges));
        RaisePropertyChanged(nameof(NumberOfAddedFeatures));
        RaisePropertyChanged(nameof(NumberOfDeletedFeatures));
        RaisePropertyChanged(nameof(NumberOfUpdatedFeatures));
        //RaisePropertyChanged(nameof(CanUndoChanges));
    });

    protected void DataSource_IsClientFilteredChanged(object? sender, bool e) => DispatcherToUi(() => RaisePropertyChanged(nameof(IsClientFiltered)));

    protected void DataSource_HasErrorChanged(object? sender, bool e) => DispatcherToUi(() => RaisePropertyChanged(nameof(HasError)));

    #endregion


    #endregion

    private bool _isGroupLayer;
    public bool IsGroupLayer
    {
        get { return _isGroupLayer; }
        set
        {
            _isGroupLayer = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(ShowOptions));
        }
    }

    public bool IsMovable { get; set; }

    public bool IsTextLayer { get; protected set; } = false;

    public ObservableCollection<ILayer> SubLayers { get; set; } = new();

    //public bool IsValid { get; set; } = true;

    //public int ZIndex { get; set; }
    private int _zIndex;
    public int ZIndex
    {
        get { return _zIndex; }
        set
        {
            _zIndex = value;
            RaisePropertyChanged();
        }
    }


    // is layer discoverable in identify
    public bool IsSearchable { get; set; } = false;


    private bool _isInScaleRange;
    public bool IsInScaleRange
    {
        get { return _isInScaleRange; }
        set
        {
            _isInScaleRange = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsNotInScaleRange));
        }
    }

    public bool IsNotInScaleRange => !IsInScaleRange;


    #region Toc

    private bool _isSelectedInToc;
    public bool IsSelectedInToc
    {
        get { return _isSelectedInToc; }
        set
        {
            if (value && _isSelectedInToc == value)
            {
                value = false;
            }

            _isSelectedInToc = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(ShowOptions));

            if (this.IsGroupLayer)
            {
                this.IsExpandedInToc = value;
            }

            _onIsSelectedInTocChanged?.Invoke(this, new CustomEventArgs<BaseLayer>(this));
        }
    }

    private bool _isExpandedInToc;
    public bool IsExpandedInToc
    {
        get { return _isExpandedInToc; }
        set
        {
            if (value && _isExpandedInToc == value)
            {
                value = false;
            }

            _isExpandedInToc = value;
            RaisePropertyChanged();

            if (this.IsGroupLayer)
            {
                // in order to sync the isexpanded and isselected for group layer
                _isSelectedInToc = value;
            }
        }
    }

    public bool ShowOptions => IsSelectedInToc && Commands?.Count > 0 && !IsGroupLayer;

    private bool _showInToc = true;
    public bool ShowInToc
    {
        get { return _showInToc; }
        set
        {
            _showInToc = value;
            RaisePropertyChanged();
        }
    }

    private int _tocOrder;
    public int TocOrder
    {
        get { return _tocOrder; }
        set
        {
            _tocOrder = value;
            RaisePropertyChanged();
        }
    }

    public bool CanReorderInToc =>
        Type == LayerType.ImagePyramid ||
        Type == LayerType.Raster ||
        Type == LayerType.VectorLayer ||
        Type == LayerType.GroupLayer;

    //y(i => i.Type == LayerType.RightClickOption)
    //                             //.ThenBy(i => i.Type == (LayerType.MoveableItem))
    //                             .ThenBy(i => i.Type == LayerType.EditableItem)
    //                             .ThenBy(i => i.Type == LayerType.Complex)
    //                             .ThenBy(e => e.Type == LayerType.Highlight)
    //                             .ThenBy(e => e.Type == LayerType.Selection)
    //                             .ThenBy(i => i.Type == LayerType.Drawing)
    //                             .ThenByDescending(i => i.Type == LayerType.BaseMap)

    private bool _canUserDelete = true;
    public bool CanUserDelete
    {
        get { return Type != LayerType.BaseMap && _canUserDelete; }
        set
        {
            _canUserDelete = value;
            RaisePropertyChanged();
        }
    }


    public virtual bool IsSymbolizable
    {
        get
        {
            //return this.Type.HasFlag(LayerType.Point) || this.Type.HasFlag(LayerType.Polyline) || this.Type.HasFlag(LayerType.Polygon);
            return SpatialModelMode == SpatialModelMode.Point ||
                    SpatialModelMode == SpatialModelMode.Polyline ||
                    SpatialModelMode == SpatialModelMode.Polygon;
        }
    }

    public virtual bool HasMultiSymbolizers => false;


    private bool _canMoveLayerUp;
    public bool CanMoveLayerUp
    {
        get { return _canMoveLayerUp; }
        set
        {
            _canMoveLayerUp = value;
            RaisePropertyChanged();
        }
    }


    private bool _canMoveLayerDown;
    public bool CanMoveLayerDown
    {
        get { return _canMoveLayerDown; }
        set
        {
            _canMoveLayerDown = value;
            RaisePropertyChanged();
        }
    }

    #endregion

    protected double _opacity = 1.0;
    public virtual double Opacity
    {
        get { return _opacity; }
        set
        {
            _opacity = value;
            RaisePropertyChanged();
        }
    }


    //private bool triggerVisibilityChagne = true;

    private Visibility _visibility;
    public virtual Visibility Visibility
    {
        get { return _visibility; }
        set
        {
            _visibility = value;
            RaisePropertyChanged();

            SetVisibility(value);
        }
    }

    private bool? _allChildsVisible;

    public bool? AllChildsVisible
    {
        get { return _allChildsVisible; }
        set
        {
            if (_allChildsVisible == value)
                return;

            // null is valid if triggered by sublayers
            if (value is null && GetAllChildVisible() != value)
                _allChildsVisible = false;

            else
                _allChildsVisible = value;

            RaisePropertyChanged();

            if (!IsGroupLayer)
                return;

            if (_allChildsVisible is null)
                return;

            //triggerVisibilityChagne = false;

            SetVisibility(_allChildsVisible == true ? Visibility.Visible : Visibility.Collapsed);

            //triggerVisibilityChagne = true;
        }
    }

    public bool? GetAllChildVisible()
    {
        if (SubLayers.IsNullOrEmpty())
            return null;

        else if (SubLayers.All(s => s.Visibility == Visibility.Visible))
            return true;

        else if (SubLayers.All(s => s.Visibility == Visibility.Collapsed))
            return false;

        else
            return null;
    }

    public void UpdateAllChildsVisible() => AllChildsVisible = GetAllChildVisible();


    private ScaleInterval _visibleRange = ScaleInterval.All;
    public ScaleInterval VisibleRange
    {
        get { return _visibleRange; }
        set
        {
            _visibleRange = value;
            RaisePropertyChanged();
        }
    }

    //private LabelParameters _labels;
    //public LabelParameters Labels
    //{
    //    get { return _labels; }
    //    set
    //    {
    //        _labels = value;
    //        RaisePropertyChanged();

    //        this.OnLabelChanged?.Invoke(this, new CustomEventArgs<LabelParameters>(value));
    //    }
    //}

    //private VisualParameters _visualParameters;
    //public VisualParameters VisualParameters
    //{
    //    get { return _visualParameters; }
    //    set
    //    {
    //        _visualParameters = value;

    //        RaisePropertyChanged();

    //        if (_visualParameters != null)
    //        {
    //            _visualParameters.OnVisibilityChanged -= RaiseVisibilityChanged;
    //            _visualParameters.OnVisibilityChanged += RaiseVisibilityChanged;
    //        }

    //    }
    //}


    private FrameworkElement? _element;
    public FrameworkElement? Element
    {
        get { return _element; }

        set
        {
            _element = value;

            if (value is not null)
            {
                BindWithFrameworkElement(value);
            }

            RaisePropertyChanged();
        }
    }

    public Action<ILayer>? RequestChangeVisibility { get; set; }

    public Action<ILayer>? RequestChangeSymbology;

    /// <summary>
    /// Invoked when the layer's data source finishes loading. Used to trigger map re-render (e.g. via RefreshLayerVisibility).
    /// Set by LayerManager when adding layers with unloaded IDataSource.
    /// </summary>
    public Action<ILayer>? RequestRefreshWhenDataLoaded { get; set; }

    /// <summary>
    /// Invoked when Save is requested from the layer (e.g. legend popup). Set by MapViewModel to delegate to SelectedLayer or DataSource.
    /// </summary>
    public Func<ILayer, Task>? RequestSaveChanges { get; set; }

    /// <summary>
    /// Invoked when Undo is requested from the layer (e.g. legend popup). Set by MapViewModel to delegate to SelectedLayer or DataSource.
    /// </summary>
    public Func<ILayer, Task>? RequestUndoAllChanges { get; set; }

    public Action<ILayer>? RequestClearSelectedLayer { get; set; }

    public Action<ILayer> RequestShowLayerSettings { get; set; }

    public Func<ILayer, Task> RequestMoveLayerDown { get; set; }
    public Func<ILayer, Task> RequestMoveLayerUp { get; set; }

    protected virtual void BindWithFrameworkElement(FrameworkElement? element)
    {
        if (element is null)
            return;

        Binding binding4 = new Binding() { Source = this, Path = new PropertyPath(nameof(Visibility)), Mode = BindingMode.TwoWay };
        element.SetBinding(UIElement.VisibilityProperty, binding4);

        Binding binding5 = new Binding() { Source = this, Path = new PropertyPath(nameof(Opacity)), Mode = BindingMode.TwoWay };
        element.SetBinding(UIElement.OpacityProperty, binding5);
    }



    //public void BindWithFrameworkElement(FrameworkElement? element)
    //{
    //    if (element is null)
    //        return;

    //    if (element is Path || element is Rectangle)
    //    {
    //        Binding binding4 = new Binding() { Source = this._parent, Path = new PropertyPath("Visibility"), Mode = BindingMode.TwoWay };
    //        element.SetBinding(Path.VisibilityProperty, binding4);

    //        Binding binding5 = new Binding() { Source = this._parent, Path = new PropertyPath("Opacity"), Mode = BindingMode.TwoWay };
    //        element.SetBinding(Path.OpacityProperty, binding5);
    //    } 
    //    else
    //        throw new NotImplementedException();
    //} 

    #region Methods

    //public virtual void Invalidate() => IsValid = false;

    public void TurnOff()
    {
        SetVisibility(Visibility.Collapsed);
    }

    public void TurnOn()
    {
        SetVisibility(Visibility.Visible);
    }

    public void SetVisibility(Visibility visibility)
    {
        if (!SubLayers.IsNullOrEmpty())
        {
            foreach (var item in SubLayers)
            {
                item.Visibility = visibility;
            }
        }

        if (Parent is not null)
            Parent.UpdateAllChildsVisible();

        //if (!triggerVisibilityChagne)
        //    return;

        if (Element is null && visibility == Visibility.Visible)
        {
            RequestChangeVisibility?.Invoke(this);
        }
    }

    public void ToggleVisibility()
    {
        if (Visibility == Visibility.Visible)
        {
            TurnOff();
        }
        else
        {
            TurnOn();
        }
    }

    public bool CanRenderLayer(double mapScale)
    {
        return Visibility == Visibility.Visible && VisibleRange.IsInRange(1.0 / mapScale);
    }

    //public bool CanRenderLabels(double mapScale)
    //{
    //    return this.Labels?.IsLabeled(1.0 / mapScale) == true;
    //}

    public async Task SaveChangesAsync()
    {
        if (RequestSaveChanges != null)
        {
            await RequestSaveChanges.Invoke(this);

            return;
        }

        var dataSource = DataSource as IEditableVectorDataSource;
        if (dataSource is null)
            return;

        await dataSource.SaveChangesAsync();
    }

    public async Task UndoAllChanges()
    {
        if (RequestUndoAllChanges is not null)
            await RequestUndoAllChanges.Invoke(this);
    }

    public async Task ReloadDataAsync()
    {
        if (DataSource is null)
            return;

        await DataSource.LoadAsync();

        RequestClearSelectedLayer?.Invoke(this);
    }

    #endregion

    private List<IFeatureTableCommand> _featureTableCommands = new();
    public List<IFeatureTableCommand> FeatureTableCommands
    {
        get => _featureTableCommands;
        set
        {
            _featureTableCommands = value;
            RaisePropertyChanged();
        }
    }

    private List<ILegendCommand> _commands = new();
    public List<ILegendCommand> Commands
    {
        get => _commands;
        set
        {
            _commands = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(ShowOptions));
        }
    }


    private RelayCommand? _changeSymbologyCommand;
    public RelayCommand ChangeSymbologyCommand
    {
        get
        {
            if (_changeSymbologyCommand == null)
            {
                //_changeSymbologyCommand = new RelayCommand(param => { this.RequestChangeSymbology?.Invoke(this); }, param => IsSelectedInToc);
                _changeSymbologyCommand = new RelayCommand(param => { RequestChangeSymbology?.Invoke(this); });
            }

            return _changeSymbologyCommand;
        }
    }

    private RelayCommand? _toggleExpandCommand;
    public RelayCommand ToggleExpandCommand
    {
        get
        {
            if (_toggleExpandCommand == null)
            {
                _toggleExpandCommand = new RelayCommand(param => { IsExpandedInToc = !IsExpandedInToc; });
            }

            return _toggleExpandCommand;
        }
    }


    private RelayCommand? _saveChangesCommand;
    public RelayCommand SaveChangesCommand
    {
        get
        {
            if (_saveChangesCommand == null)
            {
                _saveChangesCommand = new RelayCommand(
                    async param => await SaveChangesAsync(),
                    param => HasPendingChanges && IsNotBusy);
            }

            return _saveChangesCommand;
        }
    }

    private RelayCommand? _undoAllChangesCommand;
    public RelayCommand UndoAllChangesCommand
    {
        get
        {
            if (_undoAllChangesCommand == null)
            {
                _undoAllChangesCommand = new RelayCommand(
                    async param => await UndoAllChanges(),
                    param => HasPendingChanges && IsNotBusy);
            }

            return _undoAllChangesCommand;
        }
    }


    private RelayCommand? _reloadDataCommand;
    public RelayCommand? ReloadDataCommand
    {
        get
        {
            if (_reloadDataCommand == null)
            {
                _reloadDataCommand = new RelayCommand(
                    async param => await ReloadDataAsync(),
                    param => DataSource != null && IsNotBusy);
            }

            return _reloadDataCommand;
        }
    }


    private RelayCommand _showLayerSettingsCommand;
    public RelayCommand ShowLayerSettingsCommand
    {
        get
        {
            if (_showLayerSettingsCommand == null)
            {
                _showLayerSettingsCommand = new RelayCommand(param => RequestShowLayerSettings?.Invoke(this), _ => IsNotBusy);
            }

            return _showLayerSettingsCommand;
        }
    }

    private RelayCommand _moveLayerUpCommand;
    public RelayCommand MoveLayerUpCommand
    {
        get
        {
            if (_moveLayerUpCommand == null)
            {
                _moveLayerUpCommand = new RelayCommand(
                    async param =>
                    {
                        if (RequestMoveLayerUp is not null)
                        {
                            await this.RequestMoveLayerUp.Invoke(this);
                        }
                    },
                    _ => CanMoveLayerUp);
            }

            return _moveLayerUpCommand;
        }
    }


    private RelayCommand _moveLayerDownCommand;
    public RelayCommand MoveLayerDownCommand
    {
        get
        {
            if (_moveLayerDownCommand == null)
            {
                _moveLayerDownCommand = new RelayCommand(
                    async param =>
                    {
                        if (RequestMoveLayerDown is not null)
                        {
                            await this.RequestMoveLayerDown.Invoke(this);
                        }
                    },
                    _ => CanMoveLayerDown);
            }

            return _moveLayerDownCommand;
        }
    }


    #region Events

    private event EventHandler<CustomEventArgs<VisualParameters>>? _onVisibilityChanged;
    public event EventHandler<CustomEventArgs<VisualParameters>> OnVisibilityChanged
    {
        remove { _onVisibilityChanged -= value; }
        add
        {
            if (_onVisibilityChanged == null)
            {
                _onVisibilityChanged += value;
            }
        }
    }


    private event EventHandler<CustomEventArgs<string>>? _onLayerNameChanged;
    public event EventHandler<CustomEventArgs<string>> OnLayerNameChanged
    {
        remove { _onLayerNameChanged -= value; }
        add
        {
            if (_onLayerNameChanged == null)
            {
                _onLayerNameChanged += value;
            }
        }
    }


    private event EventHandler<CustomEventArgs<BaseLayer>>? _onIsSelectedInTocChanged;
    public event EventHandler<CustomEventArgs<BaseLayer>> OnIsSelectedInTocChanged
    {
        remove { _onIsSelectedInTocChanged -= value; }
        add
        {
            if (_onIsSelectedInTocChanged == null)
            {
                _onIsSelectedInTocChanged += value;
            }
        }
    }

    private event EventHandler<ILayer>? _onLayerInitilized;
    public event EventHandler<ILayer> OnLayerInitilized
    {
        remove { _onLayerInitilized -= value; }
        add
        {
            if (_onLayerInitilized == null)
            {
                _onLayerInitilized += value;
            }
        }
    }


    #endregion


}
