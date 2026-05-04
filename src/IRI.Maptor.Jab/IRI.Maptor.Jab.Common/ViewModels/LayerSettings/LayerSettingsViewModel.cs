using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Jab.Common.ViewModels.LayerSettings;

public class LayerSettingsViewModel : Notifier
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
