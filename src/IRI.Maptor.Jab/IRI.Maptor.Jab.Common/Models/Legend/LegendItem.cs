using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using IRI.Maptor.Jab.Common.Assets.Commands;
using IRI.Maptor.Jab.Common.Cartography;
using IRI.Maptor.Jab.Common.Events;

namespace IRI.Maptor.Jab.Common.Models.Legend;

public class LegendItem : Notifier
{
    //private ISymbol _symbol;

    //public ISymbol Symbol
    //{
    //    get { return _symbol; }
    //    set
    //    {
    //        _symbol = value;
    //        RaisePropertyChanged();
    //    }
    //}


    private LegendItem _parent;

    public LegendItem Parent
    {
        get { return _parent; }
        private set
        {
            _parent = value;
            RaisePropertyChanged();
        }
    }

    private ILayer _layer;

    public ILayer Layer
    {
        get { return _layer; }
        set
        {
            _layer = value;
            RaisePropertyChanged();
        }
    }

    private bool _isChecked;

    public bool IsChecked
    {
        get { return _isChecked; }
        set
        {
            if (_isChecked == value)
            {
                return;
            }

            _isChecked = value;
            RaisePropertyChanged();

            this.OnVisibilityChanged?.Invoke(this, new CustomEventArgs<bool>(value));
        }
    }

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

    private bool _isSelected;

    public bool IsSelected
    {
        get { return _isSelected; }
        set
        {
            _isSelected = value;
            RaisePropertyChanged();
        }
    }


    private bool _isExpanded;

    public bool IsExpanded
    {
        get { return _isExpanded; }
        set
        {
            _isExpanded = value;
            RaisePropertyChanged();
        }
    }


    private bool _haveDetails;

    public bool HaveDetails
    {
        get { return _haveDetails; }
        set
        {
            _haveDetails = value;
            RaisePropertyChanged();
        }
    }

    private string _layerName;

    public string LayerName
    {
        get { return _layerName; }
        set
        {
            this._layerName = value;
            RaisePropertyChanged();
        }
    }

    public bool HasSelectedFeature
    {
        get { return this.SelectedFeatures?.Count > 0; }
    }

    public string Caption
    {
        get { return $"{LayerName}{(SelectedFeatures?.Count > 0 ? $"({SelectedFeatures.Count})" : string.Empty)}"; }

    }

    private bool _isGroupLayer;

    public bool IsGroupLayer
    {
        get { return _isGroupLayer; }
        set
        {
            _isGroupLayer = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsSimpleLayer));
        }
    }

    public bool IsSimpleLayer
    {
        get { return !IsGroupLayer; }
    }

    private Guid _layerId = Guid.NewGuid();

    public Guid LayerId
    {
        get { return _layerId; }
    }

    private ObservableCollection<LegendItem> _subLayers;

    public ObservableCollection<LegendItem> SubLayers
    {
        get { return _subLayers; }
        set
        {
            _subLayers = value;
            RaisePropertyChanged();

            foreach (var item in value)
            {
                item.Parent = this;
            }
        }
    }

    private ObservableCollection<object> _selectedFeatures;

    public ObservableCollection<object> SelectedFeatures
    {
        get { return _selectedFeatures; }
        set
        {
            _selectedFeatures = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(HasSelectedFeature));
            RaisePropertyChanged(nameof(Caption));
        }
    }

    public event EventHandler<CustomEventArgs<bool>> OnVisibilityChanged;

    public List<ILayer> GetLayers()
    {
        List<ILayer> result = new List<ILayer>();

        if (this.IsSimpleLayer)
        {
            result.Add(Layer);
        }
        else
        {
            result.AddRange(this.SubLayers?.SelectMany(i => i.GetLayers()));
        }


        return result;
    }

    #region Events

    public event EventHandler<LegendItemEventArgs> OnRequestForSelectByDrawing;

    public event EventHandler<LegendItemEventArgs> OnRequestShowAll;

    #endregion

    #region Commands

    private RelayCommand _selectByDrawingCommand;

    public RelayCommand SelectByDrawingCommand
    {
        get
        {
            if (_selectByDrawingCommand == null)
            {
                _selectByDrawingCommand = new RelayCommand(param => this.OnRequestForSelectByDrawing?.Invoke(this, new LegendItemEventArgs(this)));
            }

            return _selectByDrawingCommand;
        }
    }


    private RelayCommand _showAllCommand;

    public RelayCommand ShowAllCommand
    {
        get
        {
            if (_showAllCommand == null)
            {
                _showAllCommand = new RelayCommand(param => this.OnRequestShowAll?.Invoke(this, new LegendItemEventArgs(this)));
            }

            return _showAllCommand;
        }
    }

    #endregion
}
