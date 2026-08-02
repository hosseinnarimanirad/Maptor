using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.ComponentModel;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Persistence.Abstractions;
using IRI.Maptor.Jab.Core.Models;

namespace IRI.Maptor.Jab.Core.Layers;

/// <summary>
/// Platform-neutral abstract base class implementing ILayer without WPF dependencies.
/// WPF-specific subclass lives in Jab.Common.Layers.BaseLayer (inherits this, implements IWpfLayer).
/// </summary>
public abstract class BaseLayer : Notifier, ILayer
{
    private readonly SynchronizationContext? _uiSyncContext = SynchronizationContext.Current;

    public BaseLayer()
    {
        LayerId = Guid.NewGuid();
        IsMovable = false;
    }

    #region Layer Id, Name

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

    public abstract LayerType Type { get; }

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

    public virtual RenderMode RenderMode { get => RenderMode.Default; }

    public virtual RasterizationMethod RasterizationMethod { get => RasterizationMethod.None; }

    #region Data source / status flags

    public virtual bool IsNotBusy => !IsBusy;

    private bool _isBusy = false;
    public virtual bool IsBusy
    {
        get => IsInitializing || IsProcessing || _isBusy;
        set
        {
            _isBusy = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsNotBusy));
        }
    }

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

    #region Data source status event handlers

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

    protected virtual void InvokeOnUiThread(Action action)
    {
        if (_uiSyncContext != null && _uiSyncContext != SynchronizationContext.Current)
            _uiSyncContext.Post(_ => action(), null);
        else
            action();
    }

    private void DataSource_IsInitializingChanged(object? sender, bool e) => InvokeOnUiThread(() =>
    {
        RaisePropertyChanged(nameof(IsInitializing));
        RaisePropertyChanged(nameof(IsBusy));
        RaisePropertyChanged(nameof(IsNotBusy));
        OnIsInitializingChanged();
    });

    protected virtual void OnIsInitializingChanged() { }

    private void DataSource_IsProcessingChanged(object? sender, bool e) => InvokeOnUiThread(() =>
    {
        RaisePropertyChanged(nameof(IsProcessing));
        RaisePropertyChanged(nameof(IsBusy));
        RaisePropertyChanged(nameof(IsNotBusy));
    });

    protected virtual void DataSource_IsLoadedChanged(object? sender, bool e)
    {
        InvokeOnUiThread(() =>
        {
            RaisePropertyChanged(nameof(IsLoaded));
            if (e)
                RequestRefreshWhenDataLoaded?.Invoke(this);
        });
    }

    protected void DataSource_HasPendingChangesChanged(object? sender, bool e) => InvokeOnUiThread(() =>
    {
        RaisePropertyChanged(nameof(HasPendingChanges));
        RaisePropertyChanged(nameof(NumberOfAddedFeatures));
        RaisePropertyChanged(nameof(NumberOfDeletedFeatures));
        RaisePropertyChanged(nameof(NumberOfUpdatedFeatures));
    });

    protected void DataSource_IsClientFilteredChanged(object? sender, bool e) => InvokeOnUiThread(() => RaisePropertyChanged(nameof(IsClientFiltered)));

    protected void DataSource_HasErrorChanged(object? sender, bool e) => InvokeOnUiThread(() => RaisePropertyChanged(nameof(HasError)));

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
                value = false;

            _isSelectedInToc = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(ShowOptions));

            if (this.IsGroupLayer)
                this.IsExpandedInToc = value;

            _onIsSelectedInTocChanged?.Invoke(this, new CustomEventArgs<ILayer>(this));
        }
    }

    private bool _isExpandedInToc;
    public bool IsExpandedInToc
    {
        get { return _isExpandedInToc; }
        set
        {
            if (value && _isExpandedInToc == value)
                value = false;

            _isExpandedInToc = value;
            RaisePropertyChanged();

            if (this.IsGroupLayer)
                _isSelectedInToc = value;
        }
    }

    public bool CanReorderInToc =>
        Type == LayerType.ImagePyramid ||
        Type == LayerType.Raster ||
        Type == LayerType.VectorLayer ||
        Type == LayerType.GroupLayer;

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

    protected string _tocGroup = "DEFAULT";
    public virtual string TocGroup
    {
        get { return _tocGroup; }
        set
        {
            _tocGroup = value;
            RaisePropertyChanged();
        }
    }

    public bool ShowOptions => IsSelectedInToc && !IsGroupLayer;

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

    private bool _isVisible = true;
    public virtual bool IsVisible
    {
        get { return _isVisible; }
        set
        {
            _isVisible = value;
            RaisePropertyChanged();
            SetIsVisible(value);
        }
    }

    private bool? _allChildrenVisible;
    public bool? AllChildrenVisible
    {
        get { return _allChildrenVisible; }
        set
        {
            if (_allChildrenVisible == value)
                return;

            if (value is null && GetAllChildVisible() != value)
                _allChildrenVisible = false;
            else
                _allChildrenVisible = value;

            RaisePropertyChanged();

            if (!IsGroupLayer)
                return;

            if (_allChildrenVisible is null)
                return;

            SetIsVisible(_allChildrenVisible == true);
        }
    }

    public bool? GetAllChildVisible()
    {
        if (SubLayers.IsNullOrEmpty())
            return null;
        else if (SubLayers.All(s => s.IsVisible == true))
            return true;
        else if (SubLayers.All(s => s.IsVisible == false))
            return false;
        else
            return null;
    }

    public void UpdateAllChildrenVisible() => AllChildrenVisible = GetAllChildVisible();

    protected virtual void SetIsVisible(bool isVisible)
    {
        if (!SubLayers.IsNullOrEmpty())
        {
            foreach (var item in SubLayers)
                item.IsVisible = isVisible;
        }

        if (Parent is not null)
            Parent.UpdateAllChildrenVisible();

        OnIsVisibleChanged(isVisible);
    }

    protected virtual void OnIsVisibleChanged(bool isVisible)
    {
        if (isVisible)
            RequestChangeVisibility?.Invoke(this);
    }

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

    public Action<ILayer>? RequestChangeVisibility { get; set; }

    public Action<ILayer>? RequestChangeSymbology;

    public Action<ILayer>? RequestRefreshWhenDataLoaded { get; set; }

    public Func<ILayer, Task>? RequestSaveChanges { get; set; }

    public Func<ILayer, Task>? RequestUndoAllChanges { get; set; }

    public Action<ILayer>? RequestClearSelectedLayer { get; set; }

    #region Methods

    public void TurnOff() => IsVisible = false;

    public void TurnOn() => IsVisible = true;

    public bool CanRenderLayer(double mapScale)
    {
        return IsVisible && VisibleRange.IsInRange(1.0 / mapScale);
    }

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

    #region Events

    private event EventHandler<CustomEventArgs<string>>? _onLayerNameChanged;
    public event EventHandler<CustomEventArgs<string>> OnLayerNameChanged
    {
        remove { _onLayerNameChanged -= value; }
        add
        {
            if (_onLayerNameChanged == null)
                _onLayerNameChanged += value;
        }
    }

    private event EventHandler<CustomEventArgs<ILayer>>? _onIsSelectedInTocChanged;
    public event EventHandler<CustomEventArgs<ILayer>> OnIsSelectedInTocChanged
    {
        remove { _onIsSelectedInTocChanged -= value; }
        add
        {
            if (_onIsSelectedInTocChanged == null)
                _onIsSelectedInTocChanged += value;
        }
    }

    private event EventHandler<ILayer>? _onLayerInitialized;
    public event EventHandler<ILayer> OnLayerInitialized
    {
        remove { _onLayerInitialized -= value; }
        add
        {
            if (_onLayerInitialized == null)
                _onLayerInitialized += value;
        }
    }

    #endregion
}
