using System;
using System.Net;
using System.Windows;
using System.Diagnostics;
using System.Windows.Media;
using System.Threading.Tasks;

using IRI.Maptor.Core.Common.Model;
using IRI.Maptor.Core.Spatial.Model;
using IRI.Maptor.Presentation.Wpf.Helpers;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Presentation.Wpf.ViewModels.Map;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Core.TileServices;


namespace IRI.Maptor.Presentation.Wpf.Layers;

public class TileServiceLayer : BaseLayer
{
    public int GroupId { get; set; } = 1;

    public static readonly byte[] notFoundImage;

    static TileServiceLayer()
    {
        //notFoundImage = IRI.Maptor.Presentation.Wpf.Helpers.ImageUtility.AsByteArray(Properties.Resources.imageNotFound);
        notFoundImage = ImageUtility.AsByteArray(Properties.Resources.whiteImage);
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
            _isCacheEnabled = value;
            RaisePropertyChanged();
        }
    }

    public string ProviderFullName
    {
        get { return _mapProvider.FullName; }
    }

    public bool IsOffline { get; set; }

    public override string TocGroup
    {
        get => LegendViewModel.NoneTocGroup;
        set => throw new NotImplementedException();
    }

    public TileServiceLayer(TileMapProvider mapProvider, double opacity, Func<TileInfo, string>? getFileName = null)
    {
        _cache = new TileCacheAddress(mapProvider.ProviderEn, mapProvider.MapTypeEn, getFileName);

        Opacity = opacity;

        _mapProvider = mapProvider;
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

                //Do not save imageNotFound
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

    private async Task<GeoReferencedImage> DownloadTileAsync(TileInfo tile, WebProxy proxy)
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

            var url = _mapProvider.GetUrl(tile);

            if (url == null)
                return GetNotFoundImage(tile);

            //System.Diagnostics.Debug.WriteLine("Getting Tile at " + url);

            var byteImage = await client.DownloadDataTaskAsync(url);

            if (ImageUtility.CreateBitmapImage(byteImage) == null)
                return GetNotFoundImage(tile);

            return new GeoReferencedImage(byteImage, tile.GeodeticExtent);
        }
        catch (Exception)
        {
            return GetNotFoundImage(tile);
        }
    }

    public async Task<GeoReferencedImage> GetTileAsync(TileInfo tile, IHttpProtocol client)
    {
        GeoReferencedImage result;

        if (IsCacheEnabled && _mapProvider.AllowCache)
        {
            result = _cache.GetTile(tile);

            if (!result.IsValid)
            {
                result = await DownloadTileAsync(tile, client);

                //Do not save imageNotFound
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

    public GeoReferencedImage GetCachedTileAsync(TileInfo tile) => _cache?.GetTile(tile) ?? GeoReferencedImage.NaN;

    public async Task<GeoReferencedImage> DownloadTileAsync(TileInfo tile, IHttpProtocol client)
    {
        try
        {
            if (IsOffline && _mapProvider.ShouldBeConnectedToInternet())
                return GetNotFoundImage(tile);

            var url = _mapProvider.GetUrl(tile);

            if (url == null)
                return GetNotFoundImage(tile);

            //System.Diagnostics.Debug.WriteLine("Getting Tile at " + url);

            //var response = await client.GetAsync(url);
            //var byteImage = await response.Content.ReadAsByteArrayAsync();
            var byteImage = await client.GetByteArrayAsync(url);

            if (ImageUtility.CreateBitmapImage(byteImage) == null)
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

        _cache.BaseDirectory = baseDirectory;
    }

    private GeoReferencedImage GetNotFoundImage(TileInfo tile)
    {
        return new GeoReferencedImage(notFoundImage, tile.GeodeticExtent, false);
    }

    public bool HasTheSameMapProvider(TileMapProvider provider)
    {
        return _mapProvider == provider;
    }

    public DrawingVisual? AsDrawingVisual(BoundingBox extent, int zoomLevel, int imageWidth, int imageHeight)
    {
        if (extent.IsNaN() || !extent.IsValid() || imageWidth <= 0 || imageHeight <= 0)
            return null;

        var tiles = WebMercatorUtility.WebMercatorBoundingBoxToGoogleTileRegions(extent, zoomLevel);

        double scaleX = imageWidth / extent.Width;

        double scaleY = imageHeight / extent.Height;

        var clip = new RectangleGeometry(new Rect(0, 0, imageWidth, imageHeight));

        var baseMapVisual = new DrawingVisual();

        using (var drawingContext = baseMapVisual.RenderOpen())
        {
            drawingContext.PushClip(clip);

            foreach (var tile in tiles)
            {
                try
                {
                    var image = GetCachedTileAsync(tile);

                    if (!image.IsValid) continue;

                    var tileExtent = image.GeodeticWgs84BoundingBox.Transform(MapProjects.GeodeticWgs84ToWebMercator);

                    var rect = new Rect(
                                    (tileExtent.XMin - extent.XMin) * scaleX,
                                    (extent.YMax - tileExtent.YMax) * scaleY,
                                    tileExtent.Width * scaleX,
                                    tileExtent.Height * scaleY);

                    drawingContext.DrawImage(ImageUtility.CreateBitmapImage(image.Image), rect);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"MapViewer; CaptureThumbnailAsync tile error: {ex.Message}");
                }
            }

            drawingContext.Pop();
        }

        return baseMapVisual;
    }
}
