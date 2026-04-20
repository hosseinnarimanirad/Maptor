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
using IRI.Maptor.Jab.Common.Assets.Commands;
using IRI.Maptor.Sta.Persistence.Abstractions;
using System.Linq;

namespace IRI.Maptor.Jab.Common;

public abstract class BaseLayer : Notifier, ILayer
{
    public BaseLayer()
    {
        this.LayerId = Guid.NewGuid();

        //this.ParentLayerId = Guid.Empty;

        //this.ParentLayerName = string.Empty;

        this.IsMovable = false;
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

            this._onLayerNameChanged?.Invoke(this, new CustomEventArgs<string>(value));
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
            RaisePropertyChanged(nameof(IsLoaded));
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

    public ObservableCollection<ILayer> SubLayers { get; set; } = new();

    //public bool IsValid { get; set; } = true;

    public int ZIndex { get; set; }

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
            //ChangeSymbologyCommand?.CanExecute(null);

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
            //RaisePropertyChanged(nameof(ShowOptions));
            //ChangeSymbologyCommand?.CanExecute(null);

            //OnIsSelectedInTocChanged?.Invoke(this, new CustomEventArgs<BaseLayer>(this));

            //if (this.IsGroupLayer && !this.SubLayers.IsNullOrEmpty())
            //{
            //    foreach (var subLayer in SubLayers)
            //    {
            //        subLayer.IsExpandedInToc = value;
            //    }
            //}
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
            return this.SpatialModelMode == SpatialModelMode.Point ||
                    this.SpatialModelMode == SpatialModelMode.Polyline ||
                    this.SpatialModelMode == SpatialModelMode.Polygon;
        }
    }

    public virtual bool HasMultiSymbolizers => false;

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

            if (!this.IsGroupLayer)
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
        get { return this._element; }

        set
        {
            this._element = value;

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
    public Action<ILayer>? RequestUndoAllChanges { get; set; }

    public Action<ILayer>? RequestClearSelectedLayer { get; set; }

    /// <summary>
    /// Used to determine if Undo is available. Set by MapViewModel.
    /// </summary>
    //public Func<ILayer, bool>? CanUndoChangesProvider { get; set; }

    /// <summary>
    /// Whether Undo can be performed (delegates to CanUndoChangesProvider when set).
    /// </summary>
    //public bool CanUndoChanges => CanUndoChangesProvider?.Invoke(this) ?? false;

    protected virtual void BindWithFrameworkElement(FrameworkElement? element)
    {
        if (element is null)
            return;

        Binding binding4 = new Binding() { Source = this, Path = new PropertyPath(nameof(Visibility)), Mode = BindingMode.TwoWay };
        element.SetBinding(FrameworkElement.VisibilityProperty, binding4);

        Binding binding5 = new Binding() { Source = this, Path = new PropertyPath(nameof(Opacity)), Mode = BindingMode.TwoWay };
        element.SetBinding(FrameworkElement.OpacityProperty, binding5);
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

        if (this.Element is null && visibility == Visibility.Visible)
        {
            this.RequestChangeVisibility?.Invoke(this);
        }
    }

    public void ToggleVisibility()
    {
        if (this.Visibility == Visibility.Visible)
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
        return this.Visibility == Visibility.Visible && this.VisibleRange.IsInRange(1.0 / mapScale);
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

    public void UndoAllChanges() => RequestUndoAllChanges?.Invoke(this);

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
                _changeSymbologyCommand = new RelayCommand(param => { this.RequestChangeSymbology?.Invoke(this); });
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
                _toggleExpandCommand = new RelayCommand(param => { this.IsExpandedInToc = !this.IsExpandedInToc; });
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
                    param => UndoAllChanges(),
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


    #region Events

    private event EventHandler<CustomEventArgs<VisualParameters>>? _onVisibilityChanged;
    public event EventHandler<CustomEventArgs<VisualParameters>> OnVisibilityChanged
    {
        remove { this._onVisibilityChanged -= value; }
        add
        {
            if (this._onVisibilityChanged == null)
            {
                this._onVisibilityChanged += value;
            }
        }
    }


    private event EventHandler<CustomEventArgs<string>>? _onLayerNameChanged;
    public event EventHandler<CustomEventArgs<string>> OnLayerNameChanged
    {
        remove { this._onLayerNameChanged -= value; }
        add
        {
            if (this._onLayerNameChanged == null)
            {
                this._onLayerNameChanged += value;
            }
        }
    }


    private event EventHandler<CustomEventArgs<BaseLayer>>? _onIsSelectedInTocChanged;
    public event EventHandler<CustomEventArgs<BaseLayer>> OnIsSelectedInTocChanged
    {
        remove { this._onIsSelectedInTocChanged -= value; }
        add
        {
            if (this._onIsSelectedInTocChanged == null)
            {
                this._onIsSelectedInTocChanged += value;
            }
        }
    }

    private event EventHandler<ILayer>? _onLayerInitilized;
    public event EventHandler<ILayer> OnLayerInitilized
    {
        remove { this._onLayerInitilized -= value; }
        add
        {
            if (this._onLayerInitilized == null)
            {
                this._onLayerInitilized += value;
            }
        }
    }


    #endregion


}
