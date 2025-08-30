using System;
using System.Linq;
using System.Collections.ObjectModel;

using IRI.Maptor.Jab.Common.Models.Legend;
using IRI.Maptor.Sta.Persistence.RasterDataSources;
using IRI.Maptor.Jab.Common.Models;


namespace IRI.Maptor.Jab.Common.Utility.Legend;

public class TocHelper
{
    public static ObservableCollection<LegendItem> LoadFiles(string rootDirectory, Func<string, ILayer> vectorLayerLoader, Func<string, RasterLayer> rasterLayerLoader)
    {
        if (!System.IO.Directory.Exists(rootDirectory))
            return null;


        //Load Shapefiles in the directory
        var shapefiles = (new System.IO.DirectoryInfo(rootDirectory)).GetFiles("*.shp");

        var result = new ObservableCollection<LegendItem>();

        foreach (var item in shapefiles)
        {
            var legend = new LegendItem() { IsChecked = false, IsGroupLayer = false };

            legend.Layer = vectorLayerLoader(item.FullName);

            legend.LayerName = System.IO.Path.GetFileNameWithoutExtension(item.FullName);

            result.Add(legend);
        }

        //Load Worldfiles in the directory
        var geoRasters = System.IO.Directory.EnumerateFiles(rootDirectory, "*.*", System.IO.SearchOption.TopDirectoryOnly)
                        .Where(s => s.EndsWith(".bmp") || s.EndsWith(".tif"));

        foreach (var item in geoRasters)
        {
            var legend = new LegendItem() { IsChecked = false, IsGroupLayer = false };

            legend.Layer = rasterLayerLoader(item);

            legend.LayerName = System.IO.Path.GetFileNameWithoutExtension(item);

            result.Add(legend);
        }


        //Load Image pyramids in the directory
        var pyramids = System.IO.Directory.EnumerateFiles(rootDirectory, "*.*", System.IO.SearchOption.TopDirectoryOnly)
                        .Where(s => s.EndsWith(".pyrmd"));

        foreach (var item in pyramids)
        {
            var legend = new LegendItem() { IsChecked = false, IsGroupLayer = false };

            var layerName = System.IO.Path.GetFileNameWithoutExtension(item);

            legend.Layer = new RasterLayer(new ZippedImagePyramidDataSource(item), layerName, ScaleInterval.All, false, true, System.Windows.Visibility.Collapsed, 1);

            legend.LayerName = layerName;

            result.Add(legend);
        }

        //Load sub folders in the directory
        var directories = (new System.IO.DirectoryInfo(rootDirectory)).GetDirectories();

        foreach (var item in directories)
        {
            var legend = new LegendItem() { IsChecked = false, IsGroupLayer = true };

            legend.LayerName = System.IO.Path.GetFileNameWithoutExtension(item.FullName);

            legend.SubLayers = LoadFiles(item.FullName, vectorLayerLoader, rasterLayerLoader);

            result.Add(legend);
        }

        return result;



    }

}
