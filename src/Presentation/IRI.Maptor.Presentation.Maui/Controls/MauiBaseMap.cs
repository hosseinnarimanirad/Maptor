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
}

/// <summary>
/// Maps a <see cref="MauiBaseMap"/> to a tile-URL function, reusing the existing
/// <see cref="TileMapWebUrlFactory"/> from IRI.Maptor.Presentation.Core.
/// </summary>
public static class BaseMapUrlResolver
{
    public static Func<TileInfo, string>? GetUrlFunc(MauiBaseMap baseMap)
    {
        // The factory keys are the resx key names used by TileMapWebUrlFactory.
        (string provider, string mapType) = baseMap switch
        {
            MauiBaseMap.GoogleRoadMap => ("tile_provider_google", "tile_mapType_roadMap"),
            MauiBaseMap.GoogleSatellite => ("tile_provider_google", "tile_mapType_satellite"),
            MauiBaseMap.GoogleHybrid => ("tile_provider_google", "tile_mapType_hybrid"),
            MauiBaseMap.GoogleTerrain => ("tile_provider_google", "tile_mapType_terrain"),
            MauiBaseMap.OpenStreetMap => ("tile_provider_osm", "tile_mapType_street"),
            _ => ("tile_provider_google", "tile_mapType_roadMap"),
        };

        return TileMapWebUrlFactory.GetMakeUrlFunc(provider, mapType);
    }
}
