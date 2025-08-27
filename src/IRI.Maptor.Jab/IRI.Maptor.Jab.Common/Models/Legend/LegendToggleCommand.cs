using System;
using IRI.Maptor.Jab.Common.Assets.Commands;
using IRI.Maptor.Jab.Common.Localization;
using MahApps.Metro.IconPacks;
using IRI.Maptor.Jab.Common.Presenters;
using IRI.Maptor.Jab.Common.Events;

namespace IRI.Maptor.Jab.Common.Models.Legend;

public class LegendToggleCommand : Notifier, ILegendCommand
{
    private RelayCommand _command;
    public RelayCommand Command
    {
        get { return _command; }
        set
        {
            _command = value;
            RaisePropertyChanged();
        }
    }

    private string _pathMarkup;
    public string PathMarkup
    {
        get { return _pathMarkup; }
        set
        {
            _pathMarkup = value;
            RaisePropertyChanged();
        }
    }

    private string _notCheckedPathMarkup;
    public string NotCheckedPathMarkup
    {
        get { return _notCheckedPathMarkup; }
        set
        {
            _notCheckedPathMarkup = value;
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

            Command?.Execute(value);
        }
    }

    private bool _isEnabled = true;
    public bool IsEnabled
    {
        get { return _isEnabled; }
        set
        {
            _isEnabled = value;
            RaisePropertyChanged();
        }
    }

    //private string _toolTip;
    //public string ToolTip
    //{
    //    get { return _toolTip; }
    //    set
    //    {
    //        _toolTip = value;
    //        RaisePropertyChanged();
    //    }
    //}

    private string ToolTipResourceKey { get; set; }
    public string ToolTip => LocalizationManager.Instance[ToolTipResourceKey];


    private bool _isCommandVisible = true;
    public bool IsCommandVisible
    {
        get { return _isCommandVisible; }
        set
        {
            _isCommandVisible = value;
            RaisePropertyChanged();
        }
    }

    public ILayer Layer { get; set; }


    public LegendToggleCommand()
    {
        Localization.LocalizationManager.Instance.LanguageChanged += Instance_LanguageChanged;
    }

    private void Instance_LanguageChanged()
    {
        RaisePropertyChanged(nameof(ToolTip));
    }

    public static LegendToggleCommand CreateToggleLayerLabelCommand(MapPresenter map, SymbolizableLayer layer/*, LabelParameters labels*/)
    {
        LegendToggleCommand result = new LegendToggleCommand
        {
            PathMarkup = new PackIconModern() { Kind = PackIconModernKind.TextSerif }.Data,// IRI.Maptor.Jab.Common.Assets.ShapeStrings.Appbar.appbarTextSerif;
            NotCheckedPathMarkup = IRI.Maptor.Jab.Common.Assets.ShapeStrings.AppbarExtension.appbarTextSerifNone,
            ToolTipResourceKey = LocalizationResourceKeys.cmd_legend_toggleLayerLabel.ToString(),
            Layer = layer,
            //IsSelected = layer.Labels?.IsOn == true
            IsSelected = layer.GetDefaultLabelParams()?.IsOn == true
        };

        EventHandler<CustomEventArgs<VisualParameters>> labels_IsInScaleRangeChanged = (sender, e) =>
        {
            if (e.Arg != null)
            {
                result.IsEnabled = e.Arg.IsEnabled;
            }
        };

        EventHandler<CustomEventArgs<VisualParameters>> layer_OnLabelChanged = (sender, e) =>
        {
            if (e.Arg != null)
            {
                e.Arg.OnIsInScaleRangeChanged -= labels_IsInScaleRangeChanged;
                e.Arg.OnIsInScaleRangeChanged += labels_IsInScaleRangeChanged;
            }
        };

        layer.OnLabelChanged -= layer_OnLabelChanged;
        layer.OnLabelChanged += layer_OnLabelChanged;

        //layer.Labels = labels;

        result.Command = new RelayCommand(param =>
         {
             if (layer is null)
                 return;
                 
             var label = layer.GetDefaultLabelParams();

             if (label is null)
                 return;
              
             label.IsOn = result.IsSelected;

             map.RefreshLayerVisibility(result.Layer);
         });

        return result;
    }



}
