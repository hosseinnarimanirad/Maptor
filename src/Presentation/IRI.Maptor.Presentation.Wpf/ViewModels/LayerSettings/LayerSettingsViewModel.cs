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
     

    public LayerSettingsViewModel(ILayer layer, LayerSettings_VectorExportViewModel exportViewModel)
    {
        this.Layer = layer;
        this.Export = exportViewModel;
    }

}
