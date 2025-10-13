using System;
using System.Net;
using System.Net.Http;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Data;
using System.Threading.Tasks;

using IRI.Maptor.Sta.Common.Model;
using IRI.Maptor.Sta.Spatial.Model;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Jab.Common.TileServices;


namespace IRI.Maptor.Jab.Common;

public class TileServiceLayer : BaseLayer
{
    public int GroupId { get; set; } = 1;

    public static readonly byte[] notFoundImage;

    static TileServiceLayer()
    {
        //notFoundImage = IRI.Maptor.Jab.Common.Helpers.ImageUtility.AsByteArray(Properties.Resources.imageNotFound);
        notFoundImage = IRI.Maptor.Jab.Common.Helpers.ImageUtility.AsByteArray(Properties.Resources.whiteImage);
    }

    private readonly TileCacheAddress _cache;

    private readonly TileMapProvider _mapProvider;
     
    public override RenderMode RenderMode => RenderMode.Tiled;

    public override LayerType Type => LayerType.BaseMap;
     
    public override BoundingBox Extent
    {
        get => BoundingBox.NaN;
        protected set => throw new NotImplementedException();
    }

    private bool _isCacheEnabled;
    public bool IsCacheEnabled
    {
        get { return _isCacheEnabled; }
        set
        {
            this._isCacheEnabled = value;
            RaisePropertyChanged();
        }
    }

    public string ProviderFullName
    {
        get { return _mapProvider.FullName; }
    }
     
    public bool IsOffline { get; set; }


    public TileServiceLayer(TileMapProvider mapProvider, double opacity, Func<TileInfo, string>? getFileName = null)
    { 
        this._cache = new TileCacheAddress(mapProvider.ProviderEn, mapProvider.MapTypeEn, getFileName);
         
        this.Opacity = opacity;

        this._mapProvider = mapProvider;
    }


    //private FrameworkElement? _element;
    //public FrameworkElement? Element
    //{
    //    get { return this._element; }

    //    set
    //    {
    //        this._element = value;

    //        this.BindWithFrameworkElement(value);

    //        RaisePropertyChanged();
    //    }
    //}

    //public void BindWithFrameworkElement(FrameworkElement? element)
    //{
    //    if (element is null)
    //        return;
        
    //    if (element is Path || element is Rectangle)
    //    {
    //        //Binding binding1 = new Binding() { Source = this, Path = new PropertyPath("VisualParameters.Stroke"), Mode = BindingMode.TwoWay };
    //        //element.SetBinding(Path.StrokeProperty, binding1);

    //        //Binding binding2 = new Binding() { Source = this._parent, Path = new PropertyPath("VisualParameters.Fill"), Mode = BindingMode.TwoWay };
    //        //element.SetBinding(Path.FillProperty, binding2);

    //        //Binding binding3 = new Binding() { Source = this, Path = new PropertyPath("VisualParameters.StrokeThickness"), Mode = BindingMode.TwoWay };
    //        //element.SetBinding(Path.StrokeThicknessProperty, binding3);

    //        Binding binding4 = new Binding() { Source = this, Path = new PropertyPath("Visibility"), Mode = BindingMode.TwoWay };
    //        element.SetBinding(Path.VisibilityProperty, binding4);

    //        Binding binding5 = new Binding() { Source = this, Path = new PropertyPath("Opacity"), Mode = BindingMode.TwoWay };
    //        element.SetBinding(Path.OpacityProperty, binding5);
    //    }
    //    else
    //    {
    //        throw new NotImplementedException();
    //    }
    //}

    public async Task<GeoReferencedImage> GetTileAsync(TileInfo tile, WebProxy proxy)
    {
        GeoReferencedImage result;

        if (IsCacheEnabled && _mapProvider.AllowCache)
        {
            result = _cache.GetTile(tile);

            if (!result.IsValid)
            {
                result = await DownloadTileAsync(tile, proxy);

                //Do not save imageNotFounds
                if (result.IsValid)
                {
                    await _cache.SaveAsync(result, tile);
                }
            }
        }
        else
        {
            result = await DownloadTileAsync(tile, proxy);
        }

        return result;
    }

    public async Task<GeoReferencedImage> DownloadTileAsync(TileInfo tile, WebProxy proxy)
    {
        try
        {
            if (IsOffline && _mapProvider.ShouldBeConnectedToInternet())
                return GetNotFoundImage(tile);

            WiseWebClient client = new WiseWebClient(3000);

            // 1401.10.27
            // by default webClient try to get proxy from IE
            // and it makes it very slow
            // https://stackoverflow.com/a/4420429/1468295
            //if (proxy != null)
            //{
            client.Proxy = proxy;
            //}

            client.Headers.Add(HttpRequestHeader.UserAgent, "App!");

            var url = this._mapProvider.GetUrl(tile);

            if (url == null)
                return GetNotFoundImage(tile);

            //System.Diagnostics.Debug.WriteLine("Getting Tile at " + url);

            var byteImage = await client.DownloadDataTaskAsync(url);

            if (IRI.Maptor.Jab.Common.Helpers.ImageUtility.CreateBitmapImage(byteImage) == null)
                return GetNotFoundImage(tile);

            return new GeoReferencedImage(byteImage, tile.GeodeticExtent);
        }
        catch (Exception)
        {
            return GetNotFoundImage(tile);
        }
    }

    public async Task<GeoReferencedImage> GetTileAsync(TileInfo tile, HttpClient client)
    {
        GeoReferencedImage result;

        if (IsCacheEnabled && _mapProvider.AllowCache)
        {
            result = _cache.GetTile(tile);

            if (!result.IsValid)
            {
                result = await DownloadTileAsync(tile, client);

                //Do not save imageNotFounds
                if (result.IsValid)
                {
                    await _cache.SaveAsync(result, tile);
                }
            }
        }
        else
        {
            result = await DownloadTileAsync(tile, client);
        }

        return result;
    }

    public async Task<GeoReferencedImage> DownloadTileAsync(TileInfo tile, HttpClient client)
    {
        try
        {
            if (IsOffline && _mapProvider.ShouldBeConnectedToInternet())
                return GetNotFoundImage(tile);

            var url = this._mapProvider.GetUrl(tile);

            if (url == null)
                return GetNotFoundImage(tile);

            //System.Diagnostics.Debug.WriteLine("Getting Tile at " + url);

            var response = await client.GetAsync(url);

            var byteImage = await response.Content.ReadAsByteArrayAsync();

            if (IRI.Maptor.Jab.Common.Helpers.ImageUtility.CreateBitmapImage(byteImage) == null)
                return GetNotFoundImage(tile);

            return new GeoReferencedImage(byteImage, tile.GeodeticExtent);
        }
        catch (Exception)
        {
            return GetNotFoundImage(tile);
        }
    }

    public void EnableCaching(string baseDirectory)
    {
        IsCacheEnabled = true;

        this._cache.BaseDirectory = baseDirectory;
    }

    private GeoReferencedImage GetNotFoundImage(TileInfo tile)
    {
        return new GeoReferencedImage(notFoundImage, tile.GeodeticExtent, false);
    }

    public bool HasTheSameMapProvider(TileMapProvider provider)
    {
        return _mapProvider == provider;
    }
}
