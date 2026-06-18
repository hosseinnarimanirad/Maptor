
using IRI.Maptor.Sta.Persistence.DataSources;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Jab.Common;
using IRI.Maptor.Jab.Common.Cartography.Symbologies;
using IRI.Maptor.Jab.Common.Layers;
using IRI.Maptor.Jab.Core;
using IRI.Maptor.Jab.Core.Models;

namespace IRI.Maptor.Jab.IranRepo;

public static class NccRepository
{
    public static VectorLayer? GetLayer(string layerName, string layerTitle, VisualParameters visualParameters, VisualParameters? label)
    {
        var jsonString = ZipFileHelper.OpenAndReadAsString("iriRepo.dll", layerName);

        if (jsonString == null)
            return null;

        var features = JsonListDataSource.CreateFromJsonString<NccPoint>(jsonString, i => i.AsFeature()/*, p => p.Name*/);

        List<ISymbolizer> symbolizers = [new SimpleSymbolizer(visualParameters)];

        if (label is not null)
            symbolizers.Add(new LabelSymbolizer(label));

        var vectorLayer = new VectorLayer(
            layerTitle,
            features,
            symbolizers,
            LayerType.VectorLayer,
            RenderMode.Default,
            RasterizationMethod.GdiPlus,
            ScaleInterval.All,
            NccLayers.TocGroup)
        {
            //ShowInToc = false,
            CanUserDelete = false,
            Visibility = System.Windows.Visibility.Collapsed
        };

        //if (label is not null)
        //{
        //    //vectorLayer.Labels = label;
        //    vectorLayer.SetSymbolizer(new LabelSymbolizer(label, string.Empty));
        //}

        return vectorLayer;
    }
}
