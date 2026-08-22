using System;

using IRI.Maptor.Presentation.Core.Localization;
using MahApps.Metro.IconPacks;
using IRI.Maptor.Presentation.Wpf.ViewModels;
using IRI.Maptor.Presentation.Wpf.Events;
using IRI.Maptor.Presentation.Wpf.Layers;

namespace IRI.Maptor.Presentation.Wpf.Models.Legend;

public class LegendToggleCommand : LegendCommandBase
{
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


    private string _notSelectedPathMarkup;
    public string NotSelectedPathMarkup
    {
        get { return _notSelectedPathMarkup; }
        set
        {
            _notSelectedPathMarkup = value;
            RaisePropertyChanged();
        }
    }


    public LegendToggleCommand()
    {
        LocalizationManager.Instance.LanguageChanged += Instance_LanguageChanged;
    }

    private void Instance_LanguageChanged()
    {
        RaisePropertyChanged(nameof(ToolTip));
    }

    public static LegendToggleCommand CreateToggleLayerLabelCommand(MapViewModelBase map, SymbolizableLayer layer/*, LabelParameters labels*/)
    {
        LegendToggleCommand result = new LegendToggleCommand
        {
            PathMarkup = new PackIconMaterial() { Kind = PackIconMaterialKind.FormatTitle }.Data,
            NotSelectedPathMarkup = new PackIconMaterial() { Kind = PackIconMaterialKind.FormatStrikethrough}.Data,
            ToolTipResourceKey = nameof(IRI.Maptor.Presentation.Core.Properties.Resources.cmd_legendItem_toggleLayerLabel),
            Layer = layer,
            //IsSelected = layer.Labels?.IsOn == true
            IsSelected = layer.GetDefaultLabelParams()?.IsSelected == true
        };

        EventHandler<double> map_OnZoomChanged = (sender, e) => { result.IsEnabled = layer?.GetDefaultLabelParams()?.IsInScaleRange(1.0 / e) == true; };

        map.OnZoomChanged -= map_OnZoomChanged;
        map.OnZoomChanged += map_OnZoomChanged;

        //EventHandler<CustomEventArgs<VisualParameters>> _onIsOnChanged = (sender, e) => map.RefreshLayerVisibility(result.Layer);

        //EventHandler<CustomEventArgs<VisualParameters>> layer_OnLabelChanged = (sender, e) =>
        //{
        //    if (e.Arg != null)
        //    {
        //        e.Arg.OnIsOnChanged -= _onIsOnChanged;
        //        e.Arg.OnIsOnChanged += _onIsOnChanged;
        //    }
        //};

        //layer.OnLabelChanged -= layer_OnLabelChanged;
        //layer.OnLabelChanged += layer_OnLabelChanged;

        //layer.Labels = labels;

        result.Command = new RelayCommand(param =>
         {
             if (layer is null)
                 return;

             var label = layer.GetDefaultLabelParams();

             if (label is null)
                 return;

             //label.OnIsOnChanged -= _onIsOnChanged;
             //label.OnIsOnChanged += _onIsOnChanged;

             label.IsSelected = result.IsSelected;

             map.RefreshLayerVisibility(result.Layer);
         });

        return result;
    }

    //private static void Map_OnZoomChanged(object? sender, double e)
    //{
    //    throw new NotImplementedException();
    //}
}
