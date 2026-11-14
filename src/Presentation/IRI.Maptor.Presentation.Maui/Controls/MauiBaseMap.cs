using IRI.Maptor.Presentation.Core.TileServices;
using IRI.Maptor.Core.Spatial.Model;

namespace IRI.Maptor.Presentation.Maui.Controls;

/// <summary>
/// The tile basemaps the MAUI <see cref="MapViewer"/> can display.
/// </summary>
public enum MauiBaseMap
{
    GoogleRoadMap,
    GoogleSatellite,
    GoogleHybrid,
    GoogleTerrain,
    OpenStreetMap,

    /// <summary>No basemap: only the layers are drawn.</summary>
    None,
}

/// <summary>
/// Maps a <see cref="MauiBaseMap"/> to a tile-URL function, reusing the existing
/// <see cref="TileMapWebUrlFactory"/> from IRI.Maptor.Presentation.Core.
/// </summary>
public static class BaseMapUrlResolver
{
    /// <summary>The provider's own tile URLs; null for <see cref="MauiBaseMap.None"/>.</summary>
    public static Func<TileInfo, string>? GetUrlFunc(MauiBaseMap baseMap)
    {
        if (GetResourceKeys(baseMap) is not { } keys)
        {
            return null;
        }

        return TileMapWebUrlFactory.GetMakeUrlFunc(keys.Provider, keys.MapType);
    }

    /// <summary>
    /// Tile URLs through a tile proxy application (<c>{proxyBaseUrl}/api/tile?url=&amp;provider=&amp;mapType=&amp;z=&amp;x=&amp;y=</c>,
    /// the contract of <see cref="TileServiceUrlStrategy_ProxyApp"/>); null for <see cref="MauiBaseMap.None"/>
    /// or an empty address.
    /// </summary>
    public static Func<TileInfo, string>? GetProxyUrlFunc(MauiBaseMap baseMap, string? proxyBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(proxyBaseUrl) || GetResourceKeys(baseMap) is not { } keys)
        {
            return null;
        }

        var strategy = new TileServiceUrlStrategy_ProxyApp(proxyBaseUrl.Trim().TrimEnd('/'), keys.Provider, keys.MapType);

        return tile => strategy.GetUrl(tile) ?? string.Empty;
    }

    // The factory keys are the resx key names used by TileMapWebUrlFactory.
    private static (string Provider, string MapType)? GetResourceKeys(MauiBaseMap baseMap) => baseMap switch
    {
        MauiBaseMap.GoogleRoadMap => ("tile_provider_google", "tile_mapType_roadMap"),
        MauiBaseMap.GoogleSatellite => ("tile_provider_google", "tile_mapType_satellite"),
        MauiBaseMap.GoogleHybrid => ("tile_provider_google", "tile_mapType_hybrid"),
        MauiBaseMap.GoogleTerrain => ("tile_provider_google", "tile_mapType_terrain"),
        MauiBaseMap.OpenStreetMap => ("tile_provider_osm", "tile_mapType_street"),
        MauiBaseMap.None => null,
        _ => ("tile_provider_google", "tile_mapType_roadMap"),
    };
}
