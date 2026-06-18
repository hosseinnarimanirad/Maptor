using IRI.Maptor.Sta.Spatial.Model;

namespace IRI.Maptor.Jab.Core.TileServices;

public class TileServiceUrlStrategy_LocalNetwork : TileServiceUrlStrategy
{
    private string? _localNetworkBaseUrl;
    private string? _providerEn;
    private string? _mapTyeEn;

    public Func<TileInfo, string>? _makeUrlFunc { get; protected set; }

    public TileServiceUrlStrategy_LocalNetwork(string localNetworkBaseUrl, string providerEn, string mapTyeEn)
    {
        _localNetworkBaseUrl = localNetworkBaseUrl;
        _providerEn = providerEn;
        _mapTyeEn = mapTyeEn;
    }

    public TileServiceUrlStrategy_LocalNetwork(Func<TileInfo, string> makeUrlFunc)
    {
        _makeUrlFunc = makeUrlFunc;
    }

    public override string? GetUrl(TileInfo tile)
    {
        return _makeUrlFunc is null
            ? $"{_localNetworkBaseUrl}/{_providerEn}/{_mapTyeEn}/{tile.ZoomLevel}/{tile.RowNumber}/{tile.RowNumber}_{tile.ColumnNumber}.png"
            : _makeUrlFunc(tile);
    }
}
