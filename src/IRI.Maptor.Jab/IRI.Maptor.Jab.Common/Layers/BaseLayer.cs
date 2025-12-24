using System;
using System.Windows;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using IRI.Maptor.Extensions;
using IRI.Maptor.Jab.Common.Events;
using IRI.Maptor.Jab.Common.Models;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Jab.Common.Models.Legend;
using IRI.Maptor.Jab.Common.Assets.Commands;
using System.Windows.Data;
using System.Windows.Shapes;
using IRI.Maptor.Sta.Common.Contracts.Google;

namespace IRI.Maptor.Jab.Common;

public abstract class BaseLayer : Notifier, ILayer
{
    public BaseLayer()
    {
        this.LayerId = Guid.NewGuid();

        this.ParentLayerId = Guid.Empty;

        this.ParentLayerName = string.Empty;
    }

    #region Layer Id

    /// <summary>
    /// Id of layer in datasource or api response
    /// to manage sublayers
    /// </summary>
    public int AuxilaryId { get; set; }

    public Guid LayerId { get; protected set; }

    public Guid ParentLayerId { get; set; }

    public string ParentLayerName { get; set; }

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

    #endregion

    public abstract LayerType Type { get; /*protected set;*/ }

    public virtual BoundingBox Extent { get; protected set; }

    public virtual RenderMode RenderMode { get => RenderMode.Default; /*protected set { } */}

    public virtual RasterizationMethod RasterizationMethod { get => RasterizationMethod.None;/* protected set { }*/ }

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
        }
    }


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

            OnIsSelectedInTocChanged?.Invoke(this, new CustomEventArgs<BaseLayer>(this));
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

    public bool ShowOptions
    {
        get { return IsSelectedInToc && Commands?.Count > 0 && !IsGroupLayer; }
    }

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
            return this.Type.HasFlag(LayerType.Point) || this.Type.HasFlag(LayerType.Polyline) || this.Type.HasFlag(LayerType.Polygon);
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


    public Action<ILayer>? RequestChangeSymbology;

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

    //public event EventHandler<CustomEventArgs<VisualParameters>> OnLabelChanged;

    public event EventHandler<CustomEventArgs<BaseLayer>>? OnIsSelectedInTocChanged;

    #endregion

    //protected void RaiseVisibilityChanged(object? sender, CustomEventArgs<VisualParameters> e)
    //{
    //    this._onVisibilityChanged?.Invoke(this, e);
    //}

}
