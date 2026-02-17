using System;
using System.Linq;
using System.Collections.ObjectModel;

using IRI.Maptor.Jab.Common.Models;
using IRI.Maptor.Sta.Persistence.RasterDataSources;
using System.Threading.Tasks;


namespace IRI.Maptor.Jab.Common.Helpers;

public class TocHelper
{
    public static async Task<ObservableCollection<ILayer>> LoadFiles(
        string rootDirectory,
        Func<string, ILayer> vectorLayerLoader,
        Func<string, Task<RasterLayer>> rasterLayerLoader)
    {
        if (!System.IO.Directory.Exists(rootDirectory))
            return [];

        //Load Shapefiles in the directory
        var shapefiles = new System.IO.DirectoryInfo(rootDirectory).GetFiles("*.shp");

        var result = new ObservableCollection<ILayer>();

        foreach (var item in shapefiles)
        {
            var layer = vectorLayerLoader(item.FullName);

            //layer.LayerName = System.IO.Path.GetFileNameWithoutExtension(item.FullName);

            result.Add(layer);
        }

        //Load Worldfiles in the directory
        var geoRasters = System.IO.Directory.EnumerateFiles(rootDirectory, "*.*", System.IO.SearchOption.TopDirectoryOnly)
                        .Where(s => s.EndsWith(".bmp") || s.EndsWith(".tif"));

        foreach (var item in geoRasters)
        {
            var layer = await rasterLayerLoader(item);

            layer.LayerName = System.IO.Path.GetFileNameWithoutExtension(item);

            result.Add(layer);
        }


        //Load Image pyramids in the directory
        var pyramids = System.IO.Directory.EnumerateFiles(rootDirectory, "*.*", System.IO.SearchOption.TopDirectoryOnly)
                        .Where(s => s.EndsWith(".pyrmd"));

        foreach (var item in pyramids)
        {
            var layerName = System.IO.Path.GetFileNameWithoutExtension(item);

            var layer = new RasterLayer(new ZippedImagePyramidDataSource(item), layerName, LayerType.ImagePyramid /*false, true*/, 1, System.Windows.Visibility.Collapsed, ScaleInterval.All);

            //layer.LayerName = layerName;

            result.Add(layer);
        }

        //Load sub folders in the directory
        var directories = (new System.IO.DirectoryInfo(rootDirectory)).GetDirectories();

        foreach (var item in directories)
        {
            var layerName = System.IO.Path.GetFileNameWithoutExtension(item.FullName);

            GroupLayer layer = new GroupLayer(layerName);

            var subLayers = await LoadFiles(item.FullName, vectorLayerLoader, rasterLayerLoader);

            foreach (var subLayer in subLayers)
            {
                layer.AddSubLayer(subLayer);
            }

            result.Add(layer);
        }

        return result;



    }

}
