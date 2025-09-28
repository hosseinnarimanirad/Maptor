using System.Data;

using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Spatial.AdvancedStructures;
using IRI.Maptor.Ket.GdiPlus.Model;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Analysis;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.Persistence.Abstractions;

namespace IRI.Maptor.Ket.GdiPersistence;

public class ClusteredGeoTaggedImageSource : IDataSource
{
    object _lock = new object();

    string _imageDirectory;

    public string ImageDirectory { get { return _imageDirectory; } }

    public int Srid => SridHelper.WebMercator;

    List<GeoTaggedImage> _images;

    private ClusteredGeoTaggedImageSource(string imageDirectory)
    {
        this._imageDirectory = imageDirectory;

        this._images = new List<GeoTaggedImage>();
    }

    public static ClusteredGeoTaggedImageSource Create(string imageDirectory)
    {
        ClusteredGeoTaggedImageSource result = new ClusteredGeoTaggedImageSource(imageDirectory);

        result.Load();

        return result;
    }

    private void Load()
    {
        //await Task.Factory.StartNew(() =>
        //{
        lock (_lock)
        {
            if (!System.IO.Directory.Exists(_imageDirectory))
            {
                return;
            }

            var files = new System.IO.DirectoryInfo(_imageDirectory).GetFiles("*.jpg");

            foreach (var file in files)
            {
                try
                {
                    var geoTaggedImage = new GeoTaggedImage(file.FullName);

                    if (double.IsNaN(geoTaggedImage.WebMercatorLocation.X))
                        continue;

                    this._images.Add(geoTaggedImage);
                }
                catch (Exception)
                {
                    continue;
                }

            }
        }
        //});

    }

    public List<Group<GeoTaggedImage>> Get(double scale)
    {
        lock (_lock)
        {
            var cluster = new PointClusters<GeoTaggedImage>(_images);

            var logic = new Func<GeoTaggedImage, GeoTaggedImage, bool>((first, second) =>
            {
                var distance = SpatialUtility.GetEuclideanDistance(first.WebMercatorLocation, second.WebMercatorLocation);

                var tolerance = 50 * ConversionHelper.InchToMeterFactor / 96.0;

                //var zoomLevel = GoogleMapsUtility.GetGoogleZoomLevel(scale);

                //var groundResolution = WebMercatorUtility.CalculateGroundResolution(zoomLevel, 35);

                //var tolerance2 = groundResolution * 50;

                return distance * scale < tolerance;
            });

            return cluster.GetClusters(logic);
        }
    }

    public BoundingBox WebMercatorExtent
    {
        get
        {
            if (_images == null)
            {
                return BoundingBox.NaN;
            }

            return BoundingBox.CalculateBoundingBox(_images.Select(i => i.WebMercatorLocation));
        }
    }

}
