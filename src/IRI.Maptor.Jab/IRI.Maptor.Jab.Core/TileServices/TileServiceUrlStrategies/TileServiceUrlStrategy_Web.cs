using IRI.Maptor.Sta.Spatial.Model;

namespace IRI.Maptor.Jab.Core.TileServices;

public class TileServiceUrlStrategy_Web : TileServiceUrlStrategy
{
    private string _providerResourceKey;
    private string _mapTypeResourceKey;

    public TileServiceUrlStrategy_Web(string providerResourceKey, string mapTypeResourceKey)
    {
        _providerResourceKey = providerResourceKey;
        _mapTypeResourceKey = mapTypeResourceKey;
    }

    public override string? GetUrl(TileInfo tile)
    {
        var func = TileMapWebUrlFactory.GetMakeUrlFunc(_providerResourceKey, _mapTypeResourceKey);

        return func != null ? func(tile) : null;
    }
}
