
using IRI.Maptor.Core.Persistence.DataSources;
using IRI.Maptor.Core.Common.Helpers;
using IRI.Maptor.Presentation.Wpf;
using IRI.Maptor.Presentation.Wpf.Cartography.Symbologies;
using IRI.Maptor.Presentation.Wpf.Layers;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Core.Models;

namespace IRI.Maptor.Presentation.IranRepo;

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
