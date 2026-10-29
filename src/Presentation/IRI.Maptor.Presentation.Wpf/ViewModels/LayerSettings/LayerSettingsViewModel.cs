using IRI.Maptor.Presentation.Wpf.ViewModels.Dialogs;
using IRI.Maptor.Presentation.Core.Layers;

namespace IRI.Maptor.Presentation.Wpf.ViewModels.LayerSettings;

public class LayerSettingsViewModel : DialogViewModelBase
{
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

    private LayerSettings_VectorExportViewModel _export;
    public LayerSettings_VectorExportViewModel Export
    {
        get { return _export; }
        set
        {
            _export = value;
            RaisePropertyChanged();
        }
    }

    private LayerSettings_StatisticsViewModel _statistics;
    public LayerSettings_StatisticsViewModel Statistics
    {
        get { return _statistics; }
        set
        {
            _statistics = value;
            RaisePropertyChanged();
        }
    }


    /// <summary>The Symbology tab: the layer's styles, switching, and the user's own changes.</summary>
    public LayerSettings_SymbologyViewModel? Symbology { get; init; }

    /// <summary>The read-only layer facts shown on the General tab.</summary>
    public LayerSettings_GeneralSummaryViewModel Summary { get; }

    public LayerSettingsViewModel(ILayer layer, LayerSettings_VectorExportViewModel exportViewModel, LayerSettings_StatisticsViewModel statisticsViewModel)
    {
        this.Layer = layer;
        this.Summary = new LayerSettings_GeneralSummaryViewModel(layer);
        this.Export = exportViewModel;
        this.Statistics = statisticsViewModel;
    }

}
