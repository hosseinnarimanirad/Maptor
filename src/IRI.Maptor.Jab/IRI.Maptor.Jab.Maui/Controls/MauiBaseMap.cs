using IRI.Maptor.Jab.Common.TileServices;
using IRI.Maptor.Sta.Spatial.Model;

namespace IRI.Maptor.Jab.Maui.Controls;

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
}

/// <summary>
/// Maps a <see cref="MauiBaseMap"/> to a tile-URL function, reusing the existing
/// <see cref="TileMapWebUrlFactory"/> from IRI.Maptor.Jab.Core.
/// </summary>
public static class BaseMapUrlResolver
{
    public static Func<TileInfo, string>? GetUrlFunc(MauiBaseMap baseMap)
    {
        // The factory keys are the resx key names used by TileMapWebUrlFactory.
        (string provider, string mapType) = baseMap switch
        {
            MauiBaseMap.GoogleRoadMap => ("tile_provider_google", "tile_mapType_RoadMap"),
            MauiBaseMap.GoogleSatellite => ("tile_provider_google", "tile_mapType_Satellite"),
            MauiBaseMap.GoogleHybrid => ("tile_provider_google", "tile_mapType_Hybrid"),
            MauiBaseMap.GoogleTerrain => ("tile_provider_google", "tile_mapType_Terrain"),
            MauiBaseMap.OpenStreetMap => ("tile_provider_osm", "tile_mapType_Street"),
            _ => ("tile_provider_google", "tile_mapType_RoadMap"),
        };

        return TileMapWebUrlFactory.GetMakeUrlFunc(provider, mapType);
    }
}
