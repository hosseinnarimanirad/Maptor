using System.IO;
using System.Linq;
using System.Collections.Generic;

using IRI.Maptor.Sta.Common.Model;
using IRI.Maptor.Sta.Spatial.Helpers;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.Persistence.Abstractions;
using System;
using IRI.Maptor.Sta.Persistence.DataSources;

namespace IRI.Maptor.Sta.Persistence.RasterDataSources;

public class OfflineGoogleMapDataSource : RasterDataSource
{
    public override BoundingBox WebMercatorExtent => BoundingBox.NaN;

    private List<ImageSource> _imageSources;
     
    public List<ImageSource> ImageSources { get => _imageSources; private set => _imageSources = value; }

    //public int Srid => SridHelper.WebMercator;

    public override string SourceAddress => $"Offline Google Map Data Source";


    public OfflineGoogleMapDataSource(List<ImageSource> imageSources)
    { 
        _imageSources = imageSources;
    }

    public List<GeoReferencedImage> GetTiles(BoundingBox geographicBoundingBox, double mapScale)
    {
        //94.12.17
        //int zoomLevel = GetZoomLevel(mapScale);
        int zoomLevel = WebMercatorUtility.GetZoomLevel(mapScale);

        var result = new List<GeoReferencedImage>();

        //What if there were no imagesource for this zoom level
        if (!ImageSources.Any(i => i.ZoomLevel == zoomLevel))
        {
            return result;
        }

        var lowerLeft = WebMercatorUtility.LatLonToImageNumber(geographicBoundingBox.YMin, geographicBoundingBox.XMin, zoomLevel);

        var upperRight = WebMercatorUtility.LatLonToImageNumber(geographicBoundingBox.YMax, geographicBoundingBox.XMax, zoomLevel);

        var imageSource = ImageSources.Single(i => i.ZoomLevel == zoomLevel);

        for (int i = (int)lowerLeft.X; i <= upperRight.X; i++)
        {
            for (int j = (int)upperRight.Y; j <= lowerLeft.Y; j++)
            {
                string fileName = imageSource.GetFileName(j, i);

                if (File.Exists(fileName))
                {
                    result.Add(new GeoReferencedImage(
                        File.ReadAllBytes(fileName),
                        WebMercatorUtility.GetWgs84ImageBoundingBox(j, i, zoomLevel)));
                }
            }
        }

        System.Diagnostics.Trace.WriteLine(string.Format("{0} Images founded; zoom level = {1}", result.Count, zoomLevel));

        return result;
    }



    public List<GeoReferencedImage> GetTilesForGoogleEarth(BoundingBox geographicBoundingBox, double mapScale)
    {
        //94.12.17
        //int zoomLevel = GetZoomLevel(mapScale);
        int zoomLevel = WebMercatorUtility.GetZoomLevel(mapScale);

        var result = new List<GeoReferencedImage>();

        //What if there were no imagesource for this zoom level
        if (!ImageSources.Any(i => i.ZoomLevel == zoomLevel))
        {
            return result;
        }

        var lowerLeft = WebMercatorUtility.LatLonToImageNumber(geographicBoundingBox.YMin, geographicBoundingBox.XMin, zoomLevel);

        var upperRight = WebMercatorUtility.LatLonToImageNumber(geographicBoundingBox.YMax, geographicBoundingBox.XMax, zoomLevel);

        var imageSource = ImageSources.Single(i => i.ZoomLevel == zoomLevel);

        for (int i = (int)lowerLeft.X; i <= upperRight.X; i++)
        {
            for (int j = (int)upperRight.Y; j <= lowerLeft.Y; j++)
            {
                string fileName = imageSource.GetFileName(j, i);

                if (File.Exists(fileName))
                {
                    result.Add(new GeoReferencedImage(
                        File.ReadAllBytes(fileName),
                        WebMercatorUtility.GetWgs84ImageBoundingBox(j, i, zoomLevel)));
                }
            }
        }

        System.Diagnostics.Trace.WriteLine(string.Format("{0} Images founded; zoom level = {1}", result.Count, zoomLevel));

        return result;
    }

}
