using System;
using System.Web;
using IRI.Maptor.Sta.Spatial.Model;
using IRI.Maptor.Jab.Common.Localization;


namespace IRI.Maptor.Jab.Common.TileServices;

public class TileServiceUrlStrategy_ProxyApp : TileServiceUrlStrategy
{
    private string _proxyBaseUrl;
    private string _providerResourceKey;
    private string _mapTypeResourceKey;


    public TileServiceUrlStrategy_ProxyApp(string proxyBaseUrl, string providerResourceKey, string mapTypeResourceKey)
    {
        _proxyBaseUrl = proxyBaseUrl;
        _providerResourceKey = providerResourceKey;
        _mapTypeResourceKey = mapTypeResourceKey;
    }

    public override string? GetUrl(TileInfo tile)
    {
        // Get the original tile URL from the web URL factory
        var originalUrlFunc = TileMapWebUrlFactory.GetMakeUrlFunc(_providerResourceKey, _mapTypeResourceKey);
        
        if (originalUrlFunc == null)
            return null;

        var originalUrl = originalUrlFunc(tile);
        
        if (string.IsNullOrWhiteSpace(originalUrl))
            return null;

        // Get provider and mapType names
        var providerName = LocalizationManager.Instance.GetDefaultValue(_providerResourceKey);
        var mapTypeName = LocalizationManager.Instance.GetDefaultValue(_mapTypeResourceKey);

        // Build the proxy API URL with all parameters
        // Pattern: {baseUrl}/api/tile?url={url}&provider={provider}&mapType={mapType}&z={z}&x={x}&y={y}
        var encodedUrl = HttpUtility.UrlEncode(originalUrl);
        
        return $"{_proxyBaseUrl}/api/tile?url={encodedUrl}&provider={providerName}&mapType={mapTypeName}&z={tile.ZoomLevel}&x={tile.ColumnNumber}&y={tile.RowNumber}";
    }
}
