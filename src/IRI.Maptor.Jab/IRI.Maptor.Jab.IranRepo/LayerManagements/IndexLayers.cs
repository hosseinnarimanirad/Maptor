using System.Windows.Media;

using IRI.Maptor.Extensions;
using IRI.Maptor.Jab.Common;
using IRI.Maptor.Jab.Common.Models;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.MapIndexes;
using IRI.Maptor.Jab.Common.Models.Legend;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.Persistence.DataSources;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;
using IRI.Maptor.Jab.Common.Presenters;

namespace IRI.Maptor.Jab.IranRepo;


public static class IndexLayers
{
    static FontFamily fontFamily = new FontFamily("Times New Roman");

    public static VectorLayer GetLayerFromShapefile(string layerName, string filePath, string color)
    {
        var features = ShapefileDataSourceFactory.Create(filePath, new WebMercator());

        var geo = features.GetAsFeatureSet().Features.Select(f => f.TheGeometry).ToList();

        return new VectorLayer(layerName, geo, new VisualParameters(null, color, 1, 1), LayerType.VectorLayer, RenderMode.Default, RasterizationMethod.DrawingVisual)
        {
            Visibility = System.Windows.Visibility.Collapsed
        };
    }

    public static VectorLayer GetIndex250kLayer()
    {
        var jsonString = ZipFileHelper.OpenAndReadAsString("iriRepo.dll", "IriIndex250k");

        var source = OrdinaryJsonListSource.CreateFromJsonString<Index250k>(jsonString, i => i.AsFeature()/*, i => i.SheetNameEn*/);

        VisualParameters parameters = new VisualParameters(null, "#FFEA4333", 5, .9);

        var index250kLabels = VisualParameters.CreateLabel(ScaleInterval.Create(7), 12, parameters.Stroke, fontFamily, i => i.GetCentroidPlusPoint(), isRtl: false);

        return new VectorLayer("اندکس ۲۵۰ هزار", source, parameters, LayerType.VectorLayer, RenderMode.Default, RasterizationMethod.DrawingVisual, ScaleInterval.Create(4), index250kLabels)
        {
            ShowInToc = false,
            CanUserDelete = false,
            Visibility = System.Windows.Visibility.Collapsed
            //Labels = index250kLabels
        };

    }

    public static VectorLayer GetIndex100kLayer()
    {
        var jsonString = ZipFileHelper.OpenAndReadAsString("iriRepo.dll", "IriIndex100k");

        var source = OrdinaryJsonListSource.CreateFromJsonString<Index100k>(jsonString, i => i.AsFeature()/*, i => i.SheetNameEn*/);

        VisualParameters parameters = new VisualParameters(null, "#FFEA4333", 3, .9);

        var index100kLabels = VisualParameters.CreateLabel(ScaleInterval.Create(9), 12, parameters.Stroke, fontFamily, i => i.GetCentroidPlusPoint(), isRtl: false);

        return new VectorLayer("اندکس ۱۰۰ هزار", source, parameters, LayerType.VectorLayer, RenderMode.Default, RasterizationMethod.GdiPlus, ScaleInterval.Create(5), index100kLabels)
        {
            ShowInToc = false,
            CanUserDelete = false,
            Visibility = System.Windows.Visibility.Collapsed
            //Labels = index100kLabels
        };

    }

    public static VectorLayer GetIndex50kLayer()
    {
        var jsonString = ZipFileHelper.OpenAndReadAsString("iriRepo.dll", "IriIndex50k");

        var source = OrdinaryJsonListSource.CreateFromJsonString<Index50k>(jsonString, i => i.AsFeature()/*, i => i.SheetNumber*/);

        VisualParameters parameters = new VisualParameters(null, "#88EA4333", 2, .8);

        return new VectorLayer("اندکس ۵۰ هزار", source, parameters, LayerType.VectorLayer, RenderMode.Default, RasterizationMethod.GdiPlus, ScaleInterval.Create(9))
        {
            ShowInToc = false,
            CanUserDelete = false,
            Visibility = System.Windows.Visibility.Collapsed
        };

    }

    public static VectorLayer GetIndex25kLayer()
    {
        var jsonString = ZipFileHelper.OpenAndReadAsString("iriRepo.dll", "IriIndex25k");

        var source = OrdinaryJsonListSource.CreateFromJsonString<Index25k>(jsonString, i => i.AsFeature()/*, i => i.SheetNumber*/);

        VisualParameters parameters = new VisualParameters(null, "#88FF8130", 2, .8) { DashStyle = DashStyles.Dot };

        return new VectorLayer("اندکس ۲۵ هزار", source, parameters, LayerType.VectorLayer, RenderMode.Default, RasterizationMethod.GdiPlus, ScaleInterval.Create(10))
        {
            ShowInToc = false,
            CanUserDelete = false,
            Visibility = System.Windows.Visibility.Collapsed
        };

    }

    public static List<ILayer> GetLayers(MapPresenter map)
    {
        var index250k = IndexLayers.GetIndex250kLayer();

        //var fontFamily = new FontFamily("Times New Roman");

        index250k.Commands = GetCommands/*<Index250k>*/(map, index250k/*, index250kLabels*/);

        //100k
        var index100k = IndexLayers.GetIndex100kLayer();

        //var index100kLegend = new MapLegendItemWithOptionsModel(index100k);

        index100k.Commands = GetCommands/*<Index100k>*/(map, index100k/*, index100kLabels*/);

        return new List<ILayer>() { index250k, index100k };

    }



    public static FeatureSet<Point> GetIndex250kSource(Point geodeticPoint)
    {
        var geometry = MapProjects.GeodeticWgs84ToWebMercator(geodeticPoint).AsGeometry(SridHelper.WebMercator);

        var jsonString = ZipFileHelper.OpenAndReadAsString("iriRepo.dll", "IriIndex250k");

        //return OrdinaryJsonListSource.CreateFromJsonString<Index250k>(jsonString, i => i.AsFeature()/*, i => i.SheetNameEn*/).GetGeometryAwares(geometry);
        return OrdinaryJsonListSource.CreateFromJsonString<Index250k>(jsonString, i => i.AsFeature()/*, i => i.SheetNameEn*/).GetAsFeatureSet(geometry);
    }

    public static FeatureSet<Point> GetIndex100kSource(Point geodeticPoint)
    {
        var geometry = MapProjects.GeodeticWgs84ToWebMercator(geodeticPoint).AsGeometry(SridHelper.WebMercator);

        var jsonString = ZipFileHelper.OpenAndReadAsString("iriRepo.dll", "IriIndex100k");

        //return OrdinaryJsonListSource.CreateFromJsonString<Index100k>(jsonString, i => i.AsFeature(), i => i.SheetNameEn).GetGeometryAwares(geometry);

        return OrdinaryJsonListSource.CreateFromJsonString<Index100k>(jsonString, i => i.AsFeature()/*, i => i.SheetNameEn*/).GetAsFeatureSet(geometry);
    }

    public static FeatureSet<Point> GetIndex50kSource(Point geodeticPoint)
    {
        var geometry = MapProjects.GeodeticWgs84ToWebMercator(geodeticPoint).AsGeometry(SridHelper.WebMercator);

        var jsonString = ZipFileHelper.OpenAndReadAsString("iriRepo.dll", "IriIndex50k");

        //return OrdinaryJsonListSource.CreateFromJsonString<Index50k>(jsonString, i => i.AsFeature(), i => i.SheetNameEn).GetGeometryAwares(geometry);
        return OrdinaryJsonListSource.CreateFromJsonString<Index50k>(jsonString, i => i.AsFeature()/*, i => i.SheetNameEn*/).GetAsFeatureSet(geometry);
    }



    //
    public static List<ILayer> Get2kAndLowerIndexLayers(MapPresenter map, int utmZone)
    {
        return new List<ILayer>() { Get2kDynamicIndexBlock(map, utmZone), Get2kDynamicIndexSheet(map, utmZone), Get1kDynamicIndex(map, utmZone), Get500DynamicIndex(map, utmZone) };
    }

    public static VectorLayer Get2kDynamicIndexBlock(MapPresenter map, int utmZone)
    {
        var fontFamily = new FontFamily("Times New Roman");

        UtmGridDataSource source = UtmGridDataSource.Create(UtmIndexType.Ncc2kBlock, utmZone);

        var label = VisualParameters.CreateLabel(ScaleInterval.Create(8), 14, Brushes.Red, fontFamily, i => i.GetCentroidPlusPoint(), isRtl: false);

        VisualParameters parameters = new VisualParameters(null, "#88EA4333", 2, .8);

        var layer =
            new VectorLayer(
                "بلوک‌های ۲ هزار",
                source,
                parameters,
                LayerType.VectorLayer,
                RenderMode.Default,
                RasterizationMethod.DrawingVisual,
                ScaleInterval.Create(6),
                label)
            {
                ShowInToc = false,
                CanUserDelete = false,
                Visibility = System.Windows.Visibility.Collapsed
            };

        layer.Commands = GetCommands/*<UtmSheet>*/(map, layer/*, label*/);

        return layer;
    }

    public static VectorLayer Get2kDynamicIndexSheet(MapPresenter map, int utmZone)
    {
        var fontFamily = new FontFamily("Times New Roman");

        UtmGridDataSource source = UtmGridDataSource.Create(UtmIndexType.Ncc2kSheet, utmZone);

        var label = VisualParameters.CreateLabel(ScaleInterval.Create(11), 13, Brushes.Red, fontFamily, i => i.GetCentroidPlusPoint(), isRtl: false);

        VisualParameters parameters = new VisualParameters(null, "#88EA4333", 2, .8);

        var layer =
            new VectorLayer(
                "اندکس ۲ هزار",
                source,
                parameters,
                LayerType.VectorLayer,
                RenderMode.Default,
                RasterizationMethod.DrawingVisual,
                ScaleInterval.Create(11),
                label)
            {
                ShowInToc = false,
                CanUserDelete = false,
                Visibility = System.Windows.Visibility.Collapsed
            };

        layer.Commands = GetCommands(map, layer);

        return layer;
    }

    public static VectorLayer Get1kDynamicIndex(MapPresenter map, int utmZone)
    {
        var fontFamily = new FontFamily("Times New Roman");

        UtmGridDataSource source = UtmGridDataSource.Create(UtmIndexType.Ncc1k, utmZone);

        var label = VisualParameters.CreateLabel(ScaleInterval.Create(14), 14, Brushes.Red, fontFamily, i => i.GetCentroidPlusPoint(), isRtl: false);

        VisualParameters parameters = new VisualParameters(null, "#88EA4333", 2, .8);

        var layer =
            new VectorLayer(
                "اندکس ۱ هزار",
                source,
                parameters,
                LayerType.VectorLayer,
                RenderMode.Default,
                RasterizationMethod.DrawingVisual,
                ScaleInterval.Create(13),
                label)
            {
                ShowInToc = false,
                CanUserDelete = false,
                Visibility = System.Windows.Visibility.Collapsed
            };

        layer.Commands = GetCommands(map, layer);

        return layer;
    }

    public static VectorLayer Get500DynamicIndex(MapPresenter map, int utmZone)
    {
        var fontFamily = new FontFamily("Times New Roman");

        UtmGridDataSource source = UtmGridDataSource.Create(UtmIndexType.Ncc500, utmZone);

        var label = VisualParameters.CreateLabel(ScaleInterval.Create(15), 14, Brushes.Red, fontFamily, i => i.GetCentroidPlusPoint(), isRtl: false);

        VisualParameters parameters = new VisualParameters(null, "#88EA4333", 2, .8);

        var layer =
            new VectorLayer(
                "اندکس ۵۰۰",
                source,
                parameters,
                LayerType.VectorLayer,
                RenderMode.Default,
                RasterizationMethod.DrawingVisual,
                ScaleInterval.Create(14),
                label)
            {
                ShowInToc = false,
                CanUserDelete = false,
                Visibility = System.Windows.Visibility.Collapsed
            };

        layer.Commands = GetCommands/*<UtmSheet>*/(map, layer/*, label*/);

        return layer;
    }



    public static List<ILayer> Get50kAndHigherIndexLayers(MapPresenter map)
    {
        return new List<ILayer>() { Get50kDynamicIndex(map), Get25kDynamicIndex(map), Get10kDynamicIndex(map), Get5kDynamicIndex(map) };
    }

    public static VectorLayer Get50kDynamicIndex(MapPresenter map)
    {
        var fontFamily = new FontFamily("Times New Roman");

        GridDataSource source50k = GridDataSource.Create(GeodeticIndexType.Ncc50k);

        var label = VisualParameters.CreateLabel(ScaleInterval.Create(9), 14, Brushes.Red, fontFamily, i => i.GetCentroidPlusPoint(), isRtl: false);

        VisualParameters parameters = new VisualParameters(null, "#88EA4333", 2, .8);

        var layer50k =
            new VectorLayer(
                "اندکس ۵۰ هزار",
                source50k,
                parameters,
                LayerType.VectorLayer,
                RenderMode.Default,
                RasterizationMethod.DrawingVisual,
                ScaleInterval.Create(7),
                label)
            {
                ShowInToc = false,
                CanUserDelete = false,
                Visibility = System.Windows.Visibility.Collapsed
            };

        layer50k.Commands = GetCommands/*<GeodeticSheet>*/(map, layer50k/*, label*/);

        return layer50k;
    }

    public static VectorLayer Get25kDynamicIndex(MapPresenter map)
    {
        var fontFamily = new FontFamily("Times New Roman");

        GridDataSource source25k = GridDataSource.Create(GeodeticIndexType.Ncc25k);

        var label = VisualParameters.CreateLabel(ScaleInterval.Create(10, 19), 14, Brushes.Red, fontFamily, i => i.GetCentroidPlusPoint(), isRtl: false);

        VisualParameters parameters = new VisualParameters(null, "#88EA4333", 1, .8);

        var layer25k =
            new VectorLayer(
                "اندکس ۲۵ هزار",
                source25k,
                parameters,
                LayerType.VectorLayer,
                RenderMode.Default,
                RasterizationMethod.DrawingVisual,
                ScaleInterval.Create(8),
                label)
            {
                ShowInToc = false,
                CanUserDelete = false,
                Visibility = System.Windows.Visibility.Collapsed
            };

        layer25k.Commands = GetCommands/*<GeodeticSheet>*/(map, layer25k/*, label*/);

        return layer25k;
    }

    public static VectorLayer Get10kDynamicIndex(MapPresenter map)
    {
        var fontFamily = new FontFamily("Times New Roman");

        var label = VisualParameters.CreateLabel(ScaleInterval.Create(11, 19), 14, Brushes.Red, fontFamily, i => i.GetCentroidPlusPoint(), isRtl: false);

        VisualParameters parameters = new VisualParameters(null, "#88EA4333", 1, .8);

        var layer10k =
            new VectorLayer(
                "اندکس ۱۰ هزار",
                GridDataSource.Create(GeodeticIndexType.Ncc10k),
                parameters,
                LayerType.VectorLayer,
                RenderMode.Default,
                RasterizationMethod.DrawingVisual,
                ScaleInterval.Create(9),
                label)
            {
                ShowInToc = false,
                CanUserDelete = false,
                Visibility = System.Windows.Visibility.Collapsed
            };

        layer10k.Commands = GetCommands/*<GeodeticSheet>*/(map, layer10k/*, label*/);

        return layer10k;
    }

    public static VectorLayer Get5kDynamicIndex(MapPresenter map)
    {
        var fontFamily = new FontFamily("Times New Roman");

        var label = VisualParameters.CreateLabel(ScaleInterval.Create(12, 19), 14, Brushes.Red, fontFamily, i => i?.GetCentroidPlusPoint(), isRtl: false);

        VisualParameters parameters = new VisualParameters(null, "#88EA4333", 1, .8);

        var layer5k =
            new VectorLayer(
                "اندکس ۵ هزار",
                GridDataSource.Create(GeodeticIndexType.Ncc5k),
                parameters,
                LayerType.VectorLayer,
                RenderMode.Default,
                RasterizationMethod.DrawingVisual,
                ScaleInterval.Create(10),
                label)
            {
                ShowInToc = false,
                CanUserDelete = false,
                Visibility = System.Windows.Visibility.Collapsed
            };

        layer5k.Commands = GetCommands/*<GeodeticSheet>*/(map, layer5k/*, label*/);

        return layer5k;
    }


    private static List<ILegendCommand> GetCommands/*<T>*/(MapPresenter map, VectorLayer layer/*, VisualParameters.CreateLabel label*/)
    //where T : class, IGeometryAware<Point>
    {
        return new List<ILegendCommand>()
        {
            LegendCommand.CreateZoomToExtentCommand(map, layer),
            LegendCommand.CreateShowAttributeTable/*<T>*/(map,layer),
            LegendCommand.CreateSelectByDrawing/*<T>*/(map,layer),
            LegendCommand.CreateClearSelected(map,layer),
            LegendToggleCommand.CreateToggleLayerLabelCommand(map, layer/*, label*/)
        };
    }

}
