using System.Windows.Media;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Spatial.MapIndexes;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.Persistence.DataSources;

using IRI.Maptor.Jab.Common;
using IRI.Maptor.Jab.Common.Layers;
using IRI.Maptor.Jab.Common.Models;
using IRI.Maptor.Jab.Common.ViewModels;
using IRI.Maptor.Jab.Common.Models.Legend;
using IRI.Maptor.Jab.Common.Cartography.Symbologies;

namespace IRI.Maptor.Jab.IranRepo;


public static class IndexLayers
{
    static FontFamily fontFamily = new FontFamily("Times New Roman");

    public const string TocGroup = "INDEX";

    public static async System.Threading.Tasks.Task<VectorLayer> GetLayerFromShapefileAsync(string layerName, string filePath, string color)
    {
        var features = ShapefileDataSourceFactory.Create(filePath/*, SrsBases.WebMercator*//*new WebMercator()*/);

        var featureSet = await features.GetAsFeatureSetAsync();
        var geo = featureSet.Features.Select(f => f.TheGeometry).ToList();

        return new VectorLayer(
            layerName,
            geo,
            new VisualParameters(null, color, 1, 1),
            LayerType.VectorLayer,
            RenderMode.Default,
            RasterizationMethod.DrawingVisual,
            TocGroup)
        {
            Visibility = System.Windows.Visibility.Collapsed
        };
    }

    public static VectorLayer GetIndex250kLayer()
    {
        var jsonString = ZipFileHelper.OpenAndReadAsString("iriRepo.dll", "IriIndex250k");

        var source = JsonListDataSource.CreateFromJsonString<Index250k>(jsonString!, i => i.AsFeature());

        var symbolizer = SimpleSymbolizer.Create(null, "#FFEA4333", 5, .9);

        var index250kLabels = LabelSymbolizer.Create(12, symbolizer!.Param!.Stroke!, fontFamily, i => i.GetCentroidPlusPoint(), ScaleInterval.Create(7), isRtl: false);

        //"اندکس ۲۵۰ هزار", 
        return new VectorLayer(
            IRI.Maptor.Jab.Common.Properties.Resources.index_250k_title,
            source,
            [symbolizer, index250kLabels],
            LayerType.VectorLayer,
            RenderMode.Default,
            RasterizationMethod.DrawingVisual,
            ScaleInterval.Create(4),
            TocGroup)
        { 
            CanUserDelete = false,
            Visibility = System.Windows.Visibility.Collapsed,
            TocOrder = 10
        };
    }

    public static VectorLayer GetIndex100kLayer()
    {
        var jsonString = ZipFileHelper.OpenAndReadAsString("iriRepo.dll", "IriIndex100k");

        var source = JsonListDataSource.CreateFromJsonString<Index100k>(jsonString, i => i.AsFeature()/*, i => i.SheetNameEn*/);

        var symbolizer = SimpleSymbolizer.Create(null, "#FFEA4333", 3, .9);

        var index100kLabels = LabelSymbolizer.Create(12, symbolizer!.Param!.Stroke!, fontFamily, i => i.GetCentroidPlusPoint(), ScaleInterval.Create(9), isRtl: false);

        return new VectorLayer(
            //"اندکس ۱۰۰ هزار", 
            IRI.Maptor.Jab.Common.Properties.Resources.index_100k_title,
            source,
             [symbolizer, index100kLabels],
            LayerType.VectorLayer,
            RenderMode.Default,
            RasterizationMethod.GdiPlus,
            ScaleInterval.Create(5),
            TocGroup)
        { 
            CanUserDelete = false,
            Visibility = System.Windows.Visibility.Collapsed,
            TocOrder = 9
        };

    }

    public static VectorLayer GetIndex50kLayer()
    {
        var jsonString = ZipFileHelper.OpenAndReadAsString("iriRepo.dll", "IriIndex50k");

        var source = JsonListDataSource.CreateFromJsonString<Index50k>(jsonString, i => i.AsFeature()/*, i => i.SheetNumber*/);

        var symbolizer = SimpleSymbolizer.Create(null, "#88EA4333", 2, .8);

        return new VectorLayer(
            //"اندکس ۵۰ هزار", 
            IRI.Maptor.Jab.Common.Properties.Resources.index_50k_title,
            source,
            [symbolizer,],
            LayerType.VectorLayer,
            RenderMode.Default,
            RasterizationMethod.GdiPlus,
            ScaleInterval.Create(9),
            TocGroup)
        { 
            CanUserDelete = false,
            Visibility = System.Windows.Visibility.Collapsed,
            TocOrder = 8,
        };

    }

    public static VectorLayer GetIndex25kLayer()
    {
        var jsonString = ZipFileHelper.OpenAndReadAsString("iriRepo.dll", "IriIndex25k");

        var source = JsonListDataSource.CreateFromJsonString<Index25k>(jsonString, i => i.AsFeature()/*, i => i.SheetNumber*/);

        var symbolizer = SimpleSymbolizer.Create(null, "#88FF8130", 2, .8);
        symbolizer!.Param!.DashStyle = DashStyles.Dot;

        return new VectorLayer(
            //"اندکس ۲۵ هزار", 
            IRI.Maptor.Jab.Common.Properties.Resources.index_25k_title,
            source,
            [symbolizer,],
            LayerType.VectorLayer,
            RenderMode.Default,
            RasterizationMethod.GdiPlus,
            ScaleInterval.Create(10),
            TocGroup)
        { 
            CanUserDelete = false,
            Visibility = System.Windows.Visibility.Collapsed,
            TocOrder = 7,
        };

    }

    public static List<ILayer> GetLayers(MapViewModelBase map)
    {
        var index250k = IndexLayers.GetIndex250kLayer();

        index250k.Commands = GetCommands(map, index250k);

        //100k
        var index100k = IndexLayers.GetIndex100kLayer();

        index100k.Commands = GetCommands(map, index100k);

        return new List<ILayer>() { index250k, index100k };
    }



    public static async System.Threading.Tasks.Task<FeatureSet<Point>> GetIndex250kSourceAsync(Point geodeticPoint)
    {
        var geometry = MapProjects.GeodeticWgs84ToWebMercator(geodeticPoint).AsGeometry(SridHelper.WebMercator);

        var jsonString = ZipFileHelper.OpenAndReadAsString("iriRepo.dll", "IriIndex250k");

        var source = JsonListDataSource.CreateFromJsonString<Index250k>(jsonString, i => i.AsFeature()/*, i => i.SheetNameEn*/);
        return await source.GetAsFeatureSetAsync(geometry);
    }

    public static async System.Threading.Tasks.Task<FeatureSet<Point>> GetIndex100kSourceAsync(Point geodeticPoint)
    {
        var geometry = MapProjects.GeodeticWgs84ToWebMercator(geodeticPoint).AsGeometry(SridHelper.WebMercator);

        var jsonString = ZipFileHelper.OpenAndReadAsString("iriRepo.dll", "IriIndex100k");

        var source = JsonListDataSource.CreateFromJsonString<Index100k>(jsonString, i => i.AsFeature()/*, i => i.SheetNameEn*/);
        return await source.GetAsFeatureSetAsync(geometry);
    }

    public static async System.Threading.Tasks.Task<FeatureSet<Point>> GetIndex50kSourceAsync(Point geodeticPoint)
    {
        var geometry = MapProjects.GeodeticWgs84ToWebMercator(geodeticPoint).AsGeometry(SridHelper.WebMercator);

        var jsonString = ZipFileHelper.OpenAndReadAsString("iriRepo.dll", "IriIndex50k");

        var source = JsonListDataSource.CreateFromJsonString<Index50k>(jsonString, i => i.AsFeature()/*, i => i.SheetNameEn*/);
        return await source.GetAsFeatureSetAsync(geometry);
    }



    //
    public static List<ILayer> Get2kAndLowerIndexLayers(MapViewModelBase map, int utmZone)
    {
        return [Get2kDynamicIndexBlock(map, utmZone), Get2kDynamicIndexSheet(map, utmZone), Get1kDynamicIndex(map, utmZone), Get500DynamicIndex(map, utmZone)];
    }

    public static VectorLayer Get2kDynamicIndexBlock(MapViewModelBase map, int utmZone)
    {
        var fontFamily = new FontFamily("Times New Roman");

        UtmGridDataSource source = UtmGridDataSource.Create(UtmIndexType.Ncc2kBlock, utmZone);

        var label = LabelSymbolizer.Create(14, Brushes.Red, fontFamily, i => i.GetCentroidPlusPoint(), ScaleInterval.Create(8), isRtl: false);

        var symbolizer = SimpleSymbolizer.Create(null, "#88EA4333", 2, .8);

        var layer =
            new VectorLayer(
                //"بلوک‌های ۲ هزار",
                IRI.Maptor.Jab.Common.Properties.Resources.index_2kblocks_title,
                source,
                [symbolizer, label],
                LayerType.VectorLayer,
                RenderMode.Default,
                RasterizationMethod.DrawingVisual,
                ScaleInterval.Create(6),
                TocGroup)
            { 
                CanUserDelete = false,
                Visibility = System.Windows.Visibility.Collapsed,
                TocOrder = 4
            };

        layer.Commands = GetCommands/*<UtmSheet>*/(map, layer/*, label*/);

        return layer;
    }

    public static VectorLayer Get2kDynamicIndexSheet(MapViewModelBase map, int utmZone)
    {
        var fontFamily = new FontFamily("Times New Roman");

        UtmGridDataSource source = UtmGridDataSource.Create(UtmIndexType.Ncc2kSheet, utmZone);

        var label = LabelSymbolizer.Create(13, Brushes.Red, fontFamily, i => i.GetCentroidPlusPoint(), ScaleInterval.Create(11), isRtl: false);

        var symbolizer = SimpleSymbolizer.Create(null, "#88EA4333", 2, .8);

        var layer =
            new VectorLayer(
                //"اندکس ۲ هزار",
                IRI.Maptor.Jab.Common.Properties.Resources.index_2kUtm_title,
                source,
                [symbolizer, label],
                LayerType.VectorLayer,
                RenderMode.Default,
                RasterizationMethod.DrawingVisual,
                ScaleInterval.Create(11),
                TocGroup)
            { 
                CanUserDelete = false,
                Visibility = System.Windows.Visibility.Collapsed,
                TocOrder = 3
            };

        layer.Commands = GetCommands(map, layer);

        return layer;
    }

    public static VectorLayer Get1kDynamicIndex(MapViewModelBase map, int utmZone)
    {
        var fontFamily = new FontFamily("Times New Roman");

        UtmGridDataSource source = UtmGridDataSource.Create(UtmIndexType.Ncc1k, utmZone);

        var label = LabelSymbolizer.Create(14, Brushes.Red, fontFamily, i => i.GetCentroidPlusPoint(), ScaleInterval.Create(14), isRtl: false);

        var symbolizer = SimpleSymbolizer.Create(null, "#88EA4333", 2, .8);

        var layer =
            new VectorLayer(
                //"اندکس ۱ هزار",
                IRI.Maptor.Jab.Common.Properties.Resources.index_1kUtm_title,
                source,
                [symbolizer, label],
                LayerType.VectorLayer,
                RenderMode.Default,
                RasterizationMethod.DrawingVisual,
                ScaleInterval.Create(13),
                TocGroup)
            { 
                CanUserDelete = false,
                Visibility = System.Windows.Visibility.Collapsed,
                TocOrder = 2
            };

        layer.Commands = GetCommands(map, layer);

        return layer;
    }

    public static VectorLayer Get500DynamicIndex(MapViewModelBase map, int utmZone)
    {
        var fontFamily = new FontFamily("Times New Roman");

        UtmGridDataSource source = UtmGridDataSource.Create(UtmIndexType.Ncc500, utmZone);

        var label = LabelSymbolizer.Create(14, Brushes.Red, fontFamily, i => i.GetCentroidPlusPoint(), ScaleInterval.Create(15), isRtl: false);

        var symbolizer = SimpleSymbolizer.Create(null, "#88EA4333", 2, .8);

        var layer =
            new VectorLayer(
                //"اندکس ۵۰۰",
                IRI.Maptor.Jab.Common.Properties.Resources.index_500UtmTitle,
                source,
                [symbolizer, label],
                LayerType.VectorLayer,
                RenderMode.Default,
                RasterizationMethod.DrawingVisual,
                ScaleInterval.Create(14),
                TocGroup)
            { 
                CanUserDelete = false,
                Visibility = System.Windows.Visibility.Collapsed,
                TocOrder = 1
            };

        layer.Commands = GetCommands/*<UtmSheet>*/(map, layer/*, label*/);

        return layer;
    }



    public static List<ILayer> Get50kAndHigherIndexLayers(MapViewModelBase map) => [Get50kDynamicIndex(map), Get25kDynamicIndex(map), Get10kDynamicIndex(map), Get5kDynamicIndex(map)];

    public static VectorLayer Get50kDynamicIndex(MapViewModelBase map)
    {
        var fontFamily = new FontFamily("Times New Roman");

        GeodeticGridDataSource source50k = GeodeticGridDataSource.Create(GeodeticIndexType.Ncc50k);

        var label = LabelSymbolizer.Create(14, Brushes.Red, fontFamily, i => i.GetCentroidPlusPoint(), ScaleInterval.Create(9), isRtl: false);

        var symbolizer = SimpleSymbolizer.Create(null, "#88EA4333", 2, .8);

        var layer50k =
            new VectorLayer(
                //"اندکس ۵۰ هزار",
                IRI.Maptor.Jab.Common.Properties.Resources.index_50k_title,
                source50k,
                [symbolizer, label],
                LayerType.VectorLayer,
                RenderMode.Default,
                RasterizationMethod.DrawingVisual,
                ScaleInterval.Create(7),
                TocGroup)
            { 
                CanUserDelete = false,
                Visibility = System.Windows.Visibility.Collapsed,
                TocOrder = 8,

            };

        layer50k.Commands = GetCommands/*<GeodeticSheet>*/(map, layer50k/*, label*/);

        return layer50k;
    }

    public static VectorLayer Get25kDynamicIndex(MapViewModelBase map)
    {
        var fontFamily = new FontFamily("Times New Roman");

        GeodeticGridDataSource source25k = GeodeticGridDataSource.Create(GeodeticIndexType.Ncc25k);

        var label = LabelSymbolizer.Create(14, Brushes.Red, fontFamily, i => i.GetCentroidPlusPoint(), ScaleInterval.Create(10, 19), isRtl: false);

        var symbolizer = SimpleSymbolizer.Create(null, "#88EA4333", 1, .8);

        var layer25k =
            new VectorLayer(
                //"اندکس ۲۵ هزار",
                IRI.Maptor.Jab.Common.Properties.Resources.index_25k_title,
                source25k,
                [symbolizer, label],
                LayerType.VectorLayer,
                RenderMode.Default,
                RasterizationMethod.DrawingVisual,
                ScaleInterval.Create(8),
                TocGroup)
            { 
                CanUserDelete = false,
                Visibility = System.Windows.Visibility.Collapsed,
                TocOrder = 7,
            };

        layer25k.Commands = GetCommands/*<GeodeticSheet>*/(map, layer25k/*, label*/);

        return layer25k;
    }

    public static VectorLayer Get10kDynamicIndex(MapViewModelBase map)
    {
        var fontFamily = new FontFamily("Times New Roman");

        var label = LabelSymbolizer.Create(14, Brushes.Red, fontFamily, i => i.GetCentroidPlusPoint(), ScaleInterval.Create(11, 19), isRtl: false);

        var symbolizer = SimpleSymbolizer.Create(null, "#88EA4333", 1, .8);

        var layer10k =
            new VectorLayer(
                //"اندکس ۱۰ هزار",
                IRI.Maptor.Jab.Common.Properties.Resources.index_10k_title,
                GeodeticGridDataSource.Create(GeodeticIndexType.Ncc10k),
                [symbolizer, label],
                LayerType.VectorLayer,
                RenderMode.Default,
                RasterizationMethod.DrawingVisual,
                ScaleInterval.Create(9),
                TocGroup)
            { 
                CanUserDelete = false,
                Visibility = System.Windows.Visibility.Collapsed,
                TocOrder = 6,
            };

        layer10k.Commands = GetCommands/*<GeodeticSheet>*/(map, layer10k/*, label*/);

        return layer10k;
    }

    public static VectorLayer Get5kDynamicIndex(MapViewModelBase map)
    {
        var fontFamily = new FontFamily("Times New Roman");

        var label = LabelSymbolizer.Create(14, Brushes.Red, fontFamily, i => i?.GetCentroidPlusPoint(), ScaleInterval.Create(12, 19), isRtl: false);

        var symbolizer = SimpleSymbolizer.Create(null, "#88EA4333", 1, .8);

        var layer5k =
            new VectorLayer(
                //"اندکس ۵ هزار",
                IRI.Maptor.Jab.Common.Properties.Resources.index_5k_title,
                GeodeticGridDataSource.Create(GeodeticIndexType.Ncc5k),
                [symbolizer, label],
                LayerType.VectorLayer,
                RenderMode.Default,
                RasterizationMethod.DrawingVisual,
                ScaleInterval.Create(10),
                TocGroup)
            { 
                CanUserDelete = false,
                Visibility = System.Windows.Visibility.Collapsed,
                TocOrder = 5,
            };

        layer5k.Commands = GetCommands/*<GeodeticSheet>*/(map, layer5k/*, label*/);

        return layer5k;
    }


    private static List<ILegendCommand> GetCommands/*<T>*/(MapViewModelBase map, VectorLayer layer/*, VisualParameters.CreateLabel label*/) =>
        [
            LegendCommand.CreateZoomToExtentCommand(map, layer),
            LegendCommand.CreateShowAttributeTable/*<T>*/(map,layer),
            LegendCommand.CreateSelectByDrawing/*<T>*/(map,layer),
            LegendCommand.CreateClearSelected(map,layer),
            LegendToggleCommand.CreateToggleLayerLabelCommand(map, layer/*, label*/)
        ];

}
