
using IRI.Maptor.Sta.Persistence.DataSources;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Jab.Common;
using IRI.Maptor.Jab.Common.Models;
using IRI.Maptor.Jab.Common.Cartography.Symbologies;

namespace IRI.Maptor.Jab.IranRepo;

public static class NccRepository
{
    public static VectorLayer? GetLayer(string layerName, string layerTitle, VisualParameters visualParameters, VisualParameters? label)
    {
        var jsonString = ZipFileHelper.OpenAndReadAsString("iriRepo.dll", layerName);

        if (jsonString == null)
            return null;

        var features = OrdinaryJsonListSource.CreateFromJsonString<NccPoint>(jsonString, i => i.AsFeature()/*, p => p.Name*/);

        var vectorLayer = new VectorLayer(layerTitle, features, visualParameters, LayerType.VectorLayer, RenderMode.Default, RasterizationMethod.GdiPlus, ScaleInterval.All)
        {
            ShowInToc = false,
            CanUserDelete = false,
            Visibility = System.Windows.Visibility.Collapsed
        };

        if (label is not null)
        {
            //vectorLayer.Labels = label;
            vectorLayer.SetSymbolizer(new LabelSymbolizer(label, string.Empty));
        }

        return vectorLayer;
    }
}
