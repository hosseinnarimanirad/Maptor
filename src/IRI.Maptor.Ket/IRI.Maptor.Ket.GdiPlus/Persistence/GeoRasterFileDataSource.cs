using System;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Model;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.Persistence.Abstractions;
using System.Threading.Tasks;
using IRI.Maptor.Sta.Persistence.DataSources;

namespace IRI.Maptor.Ket.GdiPersistence;

public class GeoRasterFileDataSource : RasterDataSource
{
    GeoReferencedImage geoRaster = GeoReferencedImage.NaN;

    private string? _imageFileName;

    private int _srid;

    public override int Srid => _srid;

    private DataSourceKind _dataSourceKind = DataSourceKind.Worldfile;
    public override DataSourceKind DataSourceKind => _dataSourceKind;

    public GeoRasterFileDataSource()
    {
    }

    //private GeoRasterFileDataSource(string imageFileName, int srid)
    //{
    //    this.geoRaster = IRI.Maptor.Ket.GdiPlus.WorldfileFormat.WorldfileManager.ReadWorldfile(imageFileName, srid);

    //    this.WebMercatorExtent = geoRaster.GeodeticWgs84BoundingBox.Transform(i => MapProjects.GeodeticWgs84ToWebMercator(i));
    //}

    public GeoRasterFileDataSource(GeoReferencedImage image, DataSourceKind kind)
    {
        this.geoRaster = image;

        this._dataSourceKind = kind;

        this.WebMercatorExtent = geoRaster.GeodeticWgs84BoundingBox.Transform(i => MapProjects.GeodeticWgs84ToWebMercator(i));

        // Constructed directly from an already loaded image.
        IsLoaded = true;
    }

    //public static GeoRasterFileDataSource Create(string imageFileName, int srid)
    //{
    //    try
    //    {
    //        return new GeoRasterFileDataSource(imageFileName, srid);
    //    }
    //    catch (Exception ex)
    //    {
    //        return null;
    //    }
    //}

    public GeoRasterFileDataSource(string imageFileName, DataSourceKind kind, int srid)
    {
        this._dataSourceKind = kind;

        _imageFileName = imageFileName;
        _srid = srid;

    }

    /// <summary>
    /// Asynchronously loads the worldfile and initializes the raster.
    /// Sets IsBusy while loading, HasError on failure, and IsLoaded on success.
    /// </summary>
    public override async Task<bool> LoadAsync()
    {
        if (IsLoaded)
            return true;

        if (string.IsNullOrWhiteSpace(_imageFileName))
            throw new InvalidOperationException("Image file name is not specified for GeoRasterFileDataSource.");

        IsInitializing = true;
        HasError = false;

        try
        {
            var geo = await IRI.Maptor.Ket.GdiPlus.WorldfileFormat.WorldfileManager.ReadWorldfileAsync(_imageFileName, _srid);

            if (geo is null)
            {
                HasError = true;
                return false;
            }

            geoRaster = geo;
            WebMercatorExtent = geo.GeodeticWgs84BoundingBox.Transform(i => MapProjects.GeodeticWgs84ToWebMercator(i));

            IsLoaded = true;

            return true;
        }
        catch
        {
            HasError = true;
            throw;
        }
        finally
        {
            IsInitializing = false;
        }
    }

    public static async Task<GeoRasterFileDataSource?> CreateAsync(string imageFileName, DataSourceKind kind, int srid)
    {
        var result = new GeoRasterFileDataSource(imageFileName, kind, srid);

        var loaded = await result.LoadAsync();

        return loaded ? result : null;
    }

    public GeoReferencedImage Get(BoundingBox boundingBox)//, Func<string, GeoReferencedImage> func)
    {
        if (this.WebMercatorExtent.Intersects(boundingBox))
            return geoRaster;

        else
            return GeoReferencedImage.NaN;

        //try
        //{
        //    var result = IRI.Maptor.Ket.WorldfileFormat.WorldfileManager.ReadWorldfile(this.imageFileName);

        //    this.Extent = result.GeodeticWgs84BoundingBox.Transform(i => IRI.Maptor.Sta.SpatialReferenceSystem.Projection.GeodeticToMercator(i));

        //    return result;
        //}
        //catch (Exception ex)
        //{
        //    throw new NotImplementedException();
        //}
    }

}
