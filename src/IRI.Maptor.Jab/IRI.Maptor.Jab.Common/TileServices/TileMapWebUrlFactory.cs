using IRI.Maptor.Sta.Spatial.Model;
using System;
using System.Collections.Generic;
using static IRI.Maptor.Jab.Common.Properties.Resources;
using System.Text;
using IRI.Maptor.Jab.Common.Localization;

namespace IRI.Maptor.Jab.Common.TileServices;

public static class TileMapWebUrlFactory
{
    public const string GoogleProvider = "GOOGLE";
    public const string BingProvider = "BING";
    public const string NokiaProvider = "NOKIA";
    public const string OsmProvider = "OPENSTREETMAP";
    public const string WazeProvider = "WAZE";
    public const string CartoProvider = "CARTO";
    public const string Yandex = "YANDEX";
    public const string Mapbox = "MAPBOX";

    public static readonly char[] _serverChar = ['a', 'b', 'c', 'd'];

    #region Bing

    //this is used for bing maps
    public static string TileXYToQuadKey(int tileX, int tileY, int levelOfDetail)
    {
        StringBuilder quadKey = new StringBuilder();
        for (int i = levelOfDetail; i > 0; i--)
        {
            char digit = '0';
            int mask = 1 << (i - 1);
            if ((tileX & mask) != 0)
            {
                digit++;
            }
            if ((tileY & mask) != 0)
            {
                digit++;
                digit++;
            }
            quadKey.Append(digit);
        }
        return quadKey.ToString();
    }

    private static string MakeBingSatelliteUrl(TileInfo tile, string server) => $@"http://a{server}.ortho.tiles.virtualearth.net/tiles/a{TileXYToQuadKey(tile.ColumnNumber, tile.RowNumber, tile.ZoomLevel)}.jpeg?g=5925";
    private static string MakeBingHybridUrl(TileInfo tile, string server) => $@"http://h{server}.ortho.tiles.virtualearth.net/tiles/h{TileXYToQuadKey(tile.ColumnNumber, tile.RowNumber, tile.ZoomLevel)}.jpeg?g=5978";
    private static string MakeBingStreetUrl(TileInfo tile, string server) => $@"http://r{server}.ortho.tiles.virtualearth.net/tiles/r{TileXYToQuadKey(tile.ColumnNumber, tile.RowNumber, tile.ZoomLevel)}.jpeg?g=5978";


    #endregion

    #region Google


    // http://mt1.google.com/vt/lyrs=s@901000000&hl=en&x=4&y=10&z=5&s=Ga
    // http://khm0.google.com/kh/v=748&s=&x=1354740&y=825228&z=21

    private static string MakeGoogleRoadMapUrl(TileInfo tile, string server) => $@"https://mt{server}.google.com/vt?x={tile.ColumnNumber}&y={tile.RowNumber}&z={tile.ZoomLevel}";

    private static string MakeGoogleTerrainUrl(TileInfo tile, string server) => $@"http://mt{server}.google.com/vt/lyrs=t@131,r@176163100&hl=en&x={tile.ColumnNumber}&y={tile.RowNumber}&z={tile.ZoomLevel}";

    private static string MakeGoogleSatelliteUrl(TileInfo tile, string server) => $@"http://mt{server}.google.com/vt/lyrs=s@901000000&hl=en&x={tile.ColumnNumber}&y={tile.RowNumber}&z={tile.ZoomLevel}&s=Gal";

    private static string MakeGoogleHybridUrl(TileInfo tile, string server) => $@"http://mt{server}.google.com/vt/lyrs=y@901000000&hl=en&x={tile.ColumnNumber}&y={tile.RowNumber}&z={tile.ZoomLevel}&s=Gal";

    //https://mt1.google.com/vt/lyrs=m,traffic&x={x}&y={y}&z={z}
    private static string MakeGoogleTerafficUrl(TileInfo tile, string server) => $@"http://mt{server}.google.com/vt/lyrs=m,traffic&x={tile.ColumnNumber}&y={tile.RowNumber}&z={tile.ZoomLevel}";


    //blackwhite
    //https://maps.googleapis.com/maps/vt?pb=!1m5!1m4!1i{z}!2i{x}!3i{y}!4i256!2m3!1e0!2sm!3i{y}!3m14!2snl!3sUS!5e18!12m1!1e68!12m3!1e37!2m1!1ssmartmaps!12m4!1e26!2m2!1sstyles!2zcy50OjN8cy5lOmx8cC52Om9uLHMudDoyfHAudjpvZmYscy50OjF8cC52Om9mZixzLnQ6M3xzLmU6Zy5mfHAuYzojZmYwMDAwMDB8cC53OjEscy50OjN8cy5lOmcuc3xwLmM6I2ZmMDAwMDAwfHAudzowLjgscy50OjV8cC5jOiNmZmZmZmZmZixzLnQ6NnxwLnY6b2ZmLHMudDo0fHAudjpvZmYscy5lOmx8cC52Om9mZixzLmU6bC50fHAudjpvbixzLmU6bC50LnN8cC5jOiNmZmZmZmZmZixzLmU6bC50LmZ8cC5jOiNmZjAwMDAwMCxzLmU6bC5pfHAudjpvbg!4e0!23i1301875       
    private static string MakeGoogleBlackWhiteUrl(TileInfo tile) => $@"https://maps.googleapis.com/maps/vt?pb=!1m5!1m4!1i{tile.ZoomLevel}!2i{tile.ColumnNumber}!3i{tile.RowNumber}!4i256!2m3!1e0!2sm!3i{tile.RowNumber}!3m14!2snl!3sUS!5e18!12m1!1e68!12m3!1e37!2m1!1ssmartmaps!12m4!1e26!2m2!1sstyles!2zcy50OjN8cy5lOmx8cC52Om9uLHMudDoyfHAudjpvZmYscy50OjF8cC52Om9mZixzLnQ6M3xzLmU6Zy5mfHAuYzojZmYwMDAwMDB8cC53OjEscy50OjN8cy5lOmcuc3xwLmM6I2ZmMDAwMDAwfHAudzowLjgscy50OjV8cC5jOiNmZmZmZmZmZixzLnQ6NnxwLnY6b2ZmLHMudDo0fHAudjpvZmYscy5lOmx8cC52Om9mZixzLmU6bC50fHAudjpvbixzLmU6bC50LnN8cC5jOiNmZmZmZmZmZixzLmU6bC50LmZ8cC5jOiNmZjAwMDAwMCxzLmU6bC5pfHAudjpvbg!4e0!23i1301875";

    //clean gray
    //https://maps.googleapis.com/maps/vt?pb=!1m5!1m4!1i{z}!2i{x}!3i{y}!4i256!2m3!1e0!2sm!3i{y}!3m14!2snl!3sUS!5e18!12m1!1e68!12m3!1e37!2m1!1ssmartmaps!12m4!1e26!2m2!1sstyles!2zcy50OjF8cy5lOmx8cC52Om9mZixzLnQ6MTd8cy5lOmcuc3xwLnY6b2ZmLHMudDoxOHxzLmU6Zy5zfHAudjpvZmYscy50OjV8cy5lOmd8cC52Om9ufHAuYzojZmZlM2UzZTMscy50OjgyfHMuZTpsfHAudjpvZmYscy50OjJ8cC52Om9mZixzLnQ6M3xwLmM6I2ZmY2NjY2NjLHMudDozfHMuZTpsfHAudjpvZmYscy50OjR8cy5lOmwuaXxwLnY6b2ZmLHMudDo2NXxzLmU6Z3xwLnY6b2ZmLHMudDo2NXxzLmU6bC50fHAudjpvZmYscy50OjEwNTl8cy5lOmd8cC52Om9mZixzLnQ6MTA1OXxzLmU6bHxwLnY6b2ZmLHMudDo2fHMuZTpnfHAuYzojZmZGRkZGRkYscy50OjZ8cy5lOmx8cC52Om9mZg!4e0!23i1301875
    //
    private static string MakeGoogleCleanGreyUrl(TileInfo tile) => $@"https://maps.googleapis.com/maps/vt?pb=!1m5!1m4!1i{tile.ZoomLevel}!2i{tile.ColumnNumber}!3i{tile.RowNumber}!4i256!2m3!1e0!2sm!3i{tile.RowNumber}!3m14!2snl!3sUS!5e18!12m1!1e68!12m3!1e37!2m1!1ssmartmaps!12m4!1e26!2m2!1sstyles!2zcy50OjF8cy5lOmx8cC52Om9mZixzLnQ6MTd8cy5lOmcuc3xwLnY6b2ZmLHMudDoxOHxzLmU6Zy5zfHAudjpvZmYscy50OjV8cy5lOmd8cC52Om9ufHAuYzojZmZlM2UzZTMscy50OjgyfHMuZTpsfHAudjpvZmYscy50OjJ8cC52Om9mZixzLnQ6M3xwLmM6I2ZmY2NjY2NjLHMudDozfHMuZTpsfHAudjpvZmYscy50OjR8cy5lOmwuaXxwLnY6b2ZmLHMudDo2NXxzLmU6Z3xwLnY6b2ZmLHMudDo2NXxzLmU6bC50fHAudjpvZmYscy50OjEwNTl8cy5lOmd8cC52Om9mZixzLnQ6MTA1OXxzLmU6bHxwLnY6b2ZmLHMudDo2fHMuZTpnfHAuYzojZmZGRkZGRkYscy50OjZ8cy5lOmx8cC52Om9mZg!4e0!23i1301875";


    #endregion

    #region Nokia

    //Nokia
    /// <summary>
    /// 
    /// </summary>
    /// <param name="tile"></param>
    /// <param name="server">1, 2, 3 or 5</param>
    /// <returns></returns>
    private static string MakeNokiaRoadMapUrl(TileInfo tile, int server) => $@"http://{server}.maps.nlp.nokia.com/maptile/2.1/maptile/newest/normal.day/{tile.ZoomLevel}/{tile.ColumnNumber}/{tile.RowNumber}/256/png8?app_id=SqE1xcSngCd3m4a1zEGb&token=r0sR1DzqDkS6sDnh902FWQ&lg=ENG";

    private static string MakeNokiaTerrainUrl(TileInfo tile, int server) => $@"http://{server}.maps.nlp.nokia.com/maptile/2.1/maptile/newest/terrain.day/{tile.ZoomLevel}/{tile.ColumnNumber}/{tile.RowNumber}/256/png8?app_id=SqE1xcSngCd3m4a1zEGb&token=r0sR1DzqDkS6sDnh902FWQ&lg=ENG";

    private static string MakeNokiaSatelliteUrl(TileInfo tile, int server) => $@"http://{server}.maps.nlp.nokia.com/maptile/2.1/maptile/newest/satellite.day/{tile.ZoomLevel}/{tile.ColumnNumber}/{tile.RowNumber}/256/png8?app_id=SqE1xcSngCd3m4a1zEGb&token=r0sR1DzqDkS6sDnh902FWQ&lg=ENG";

    private static string MakeNokiaHybridUrl(TileInfo tile, int server) => $@"http://{server}.maps.nlp.nokia.com/maptile/2.1/maptile/newest/hybrid.day/{tile.ZoomLevel}/{tile.ColumnNumber}/{tile.RowNumber}/256/png8?app_id=SqE1xcSngCd3m4a1zEGb&token=r0sR1DzqDkS6sDnh902FWQ&lg=ENG";

    #endregion

    #region Osm


    private static string MakeOpenStreetMapUrl(TileInfo tile, char serverChar) => $@"http://{serverChar}.tile.openstreetmap.org/{tile.ZoomLevel}/{tile.ColumnNumber}/{tile.RowNumber}.png";

    //https://{a|b|c}.tile.opentopomap.org/{z}/{x}/{y}.png 
    private static string MakeOpenTopoMapUrl(TileInfo tile, char serverChar) => $@"http://{serverChar}.tile.opentopomap.org/{tile.ZoomLevel}/{tile.ColumnNumber}/{tile.RowNumber}.png";

    //https://tiles.wmflabs.org/hikebike/11/1103/669.png
    private static string MakeOsmHikeBikeUrl(TileInfo tile) => $@"https://tiles.wmflabs.org/hikebike/{tile.ZoomLevel}/{tile.ColumnNumber}/{tile.RowNumber}.png";

    //https://m1.mapserver.mapy.cz/winter-m/13-5264-3223
    //server: 1, 2, 3, 4
    private static string MakeMapyWinterUrl(TileInfo tile) => $@"https://m{GetServer(1, 4)}.mapserver.mapy.cz/winter-m/{tile.ZoomLevel}-{tile.ColumnNumber}-{tile.RowNumber}";

    private static string MakeMapyTouristUrl(TileInfo tile) => $@"https://m{GetServer(1, 4)}.mapserver.mapy.cz/turist-m/{tile.ZoomLevel}-{tile.ColumnNumber}-{tile.RowNumber}";

    //http://c.tile.stamen.com/watercolor/${z}/${x}/${y}.jpg 
    private static string MakeStamenWatercolorUrl(TileInfo tile) => $@"http://{GetServerCharacter()}.tile.stamen.com/watercolor/{tile.ZoomLevel}/{tile.ColumnNumber}/{tile.RowNumber}.jpg";


    #endregion

    #region Waze

    //https://worldtiles3.waze.com/tiles/11/1313/805.png
    //server: 1, 2, 3, 4
    private static string MakeWazeRoadMapUrl(TileInfo tile) => $@"https://worldtiles{GetServer(1, 4)}.waze.com/tiles/{tile.ZoomLevel}/{tile.ColumnNumber}/{tile.RowNumber}.png";

    #endregion

    #region Carto

    //@2x parameter say image should be twise in size
    //servers: a, b, c, d
    //https://cartodb-basemaps-c.global.ssl.fastly.net/light_all/14/10525/6444@2x.png

    private static string MakeCartoLightUrl(TileInfo tile) => $@"https://cartodb-basemaps-{GetServerCharacter()}.global.ssl.fastly.net/light_all/{tile.ZoomLevel}/{tile.ColumnNumber}/{tile.RowNumber}.png";

    private static string MakeCartoDarkUrl(TileInfo tile) => $@"https://cartodb-basemaps-{GetServerCharacter()}.global.ssl.fastly.net/dark_all/{tile.ZoomLevel}/{tile.ColumnNumber}/{tile.RowNumber}.png";

    #endregion



    public static string GetServer(int min = 0, int max = 3)
    {
        //first bound is inclusive second bound is exclusive
        return IRI.Maptor.Sta.Common.Helpers.RandomHelper.Get(min, max + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    public static char GetServerCharacter(int min = 0, int max = 2)
    {
        //first bound is inclusive second bound is exclusive
        var random = IRI.Maptor.Sta.Common.Helpers.RandomHelper.Get(min, max + 1);

        return _serverChar[random];
    }

    public static Func<TileInfo, string>? CreateFromXyzUrlIntServer(string url, int minServer = 0, int maxServer = 3)
    {
        var mapUrl = url.Replace("{x}", "{0}").Replace("{y}", "{1}").Replace("{z}", "{2}").Replace("{@server}", "{3}");

        return tile => string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                                                            mapUrl,
                                                                            tile.ColumnNumber,
                                                                            tile.RowNumber,
                                                                            tile.ZoomLevel,
                                                                            GetServer(minServer, maxServer));
    }

    public static Func<TileInfo, string>? CreateFromXyzUrlCharServer(string url, int minServer = 0, int maxServer = 2)
    {
        var mapUrl = url.Replace("{x}", "{0}").Replace("{y}", "{1}").Replace("{z}", "{2}").Replace("{@server}", "{3}");

        return tile => string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                                                            mapUrl,
                                                                            tile.ColumnNumber,
                                                                            tile.RowNumber,
                                                                            tile.ZoomLevel,
                                                                            GetServerCharacter(minServer, maxServer));
    }

    public static Func<TileInfo, string>? GetMakeUrlFunc(string providerResourceKey, string mapTypeResourceKey)
    {

        return (providerResourceKey, mapTypeResourceKey) switch
        {
            // BING
            (nameof(tile_provider_bing), nameof(tile_mapType_Satellite)) => tile => MakeBingSatelliteUrl(tile, GetServer()),
            (nameof(tile_provider_bing), nameof(tile_mapType_Street)) => tile => MakeBingStreetUrl(tile, GetServer()),
            (nameof(tile_provider_bing), nameof(tile_mapType_Hybrid)) => tile => MakeBingHybridUrl(tile, GetServer()),

            // GOOGLE
            (nameof(tile_provider_google), nameof(tile_mapType_CleanGrey)) => MakeGoogleCleanGreyUrl,
            (nameof(tile_provider_google), nameof(tile_mapType_BlackWhite)) => MakeGoogleBlackWhiteUrl,
            (nameof(tile_provider_google), nameof(tile_mapType_Traffic)) => tile => MakeGoogleTerafficUrl(tile, GetServer()),
            (nameof(tile_provider_google), nameof(tile_mapType_Satellite)) => tile => MakeGoogleSatelliteUrl(tile, GetServer()),
            (nameof(tile_provider_google), nameof(tile_mapType_Hybrid)) => tile => MakeGoogleHybridUrl(tile, GetServer()),
            (nameof(tile_provider_google), nameof(tile_mapType_RoadMap)) => tile => MakeGoogleRoadMapUrl(tile, GetServer()),
            (nameof(tile_provider_google), nameof(tile_mapType_Terrain)) => tile => MakeGoogleTerrainUrl(tile, GetServer()),
            (nameof(tile_provider_google), nameof(tile_mapType_Light)) => CreateFromXyzUrlIntServer("https://mt{@server}.google.com/vt/lyrs=r&x={x}&y={y}&z={z}"),
            (nameof(tile_provider_google), nameof(tile_mapType_Nature)) => CreateFromXyzUrlIntServer("https://maps.googleapis.com/maps/vt?pb=!1m5!1m4!1i{z}!2i{x}!3i{y}!4i256!2m3!1e0!2sm!3i{y}!3m14!2snl!3sUS!5e18!12m1!1e68!12m3!1e37!2m1!1ssmartmaps!12m4!1e26!2m2!1sstyles!2zcy50OjV8cC5oOiNGRkE4MDB8cC5nOjEscy50OjQ5fHAuaDojNTNGRjAwfHAuczotNzN8cC5sOjQwfHAuZzoxLHMudDo1MHxwLmg6I0ZCRkYwMHxwLmc6MSxzLnQ6NTF8cC5oOiMwMEZGRkR8cC5sOjMwfHAuZzoxLHMudDo2fHAuaDojMDBCRkZGfHAuczo2fHAubDo4fHAuZzoxLHMudDoyfHAuaDojNjc5NzE0fHAuczozMy40fHAubDotMjUuNHxwLmc6MQ!4e0!23i1301875"),
            (nameof(tile_provider_google), nameof(tile_mapType_NeutralBlue)) => CreateFromXyzUrlIntServer("https://maps.googleapis.com/maps/vt?pb=!1m5!1m4!1i{z}!2i{x}!3i{y}!4i256!2m3!1e0!2sm!3i{y}!3m14!2snl!3sUS!5e18!12m1!1e68!12m3!1e37!2m1!1ssmartmaps!12m4!1e26!2m2!1sstyles!2zcy50OjZ8cy5lOmd8cC5jOiNmZjE5MzM0MSxzLnQ6NXxzLmU6Z3xwLmM6I2ZmMmM1YTcxLHMudDozfHMuZTpnfHAuYzojZmYyOTc2OGF8cC5sOi0zNyxzLnQ6MnxzLmU6Z3xwLmM6I2ZmNDA2ZDgwLHMudDo0fHMuZTpnfHAuYzojZmY0MDZkODAscy5lOmwudC5zfHAudjpvbnxwLmM6I2ZmM2U2MDZmfHAudzoyfHAuZzowLjg0LHMuZTpsLnQuZnxwLmM6I2ZmZmZmZmZmLHMudDoxfHMuZTpnfHAudzowLjZ8cC5jOiNmZjFhMzU0MSxzLmU6bC5pfHAudjpvZmYscy50OjQwfHMuZTpnfHAuYzojZmYyYzVhNzE!4e0!23i1301875"),

            // OSM
            (nameof(tile_provider_osm), nameof(tile_mapType_Street)) => tile => MakeOpenStreetMapUrl(tile, GetServerCharacter()),
            (nameof(tile_provider_osm), nameof(tile_mapType_Topo)) => tile => MakeOpenTopoMapUrl(tile, GetServerCharacter()),
            (nameof(tile_provider_osm), nameof(tile_mapType_MapyWinter)) => MakeMapyWinterUrl,
            (nameof(tile_provider_osm), nameof(tile_mapType_MapyTourist)) => MakeMapyTouristUrl,
            (nameof(tile_provider_osm), nameof(tile_mapType_HikeBike)) => MakeOsmHikeBikeUrl,
            (nameof(tile_provider_osm), nameof(tile_mapType_Watercolor)) => MakeStamenWatercolorUrl,

            // WAZE
            (nameof(tile_provider_waze), nameof(tile_mapType_Street)) => MakeWazeRoadMapUrl,

            // CARTO
            (nameof(tile_provider_carto), nameof(tile_mapType_Dark)) => MakeCartoDarkUrl,
            (nameof(tile_provider_carto), nameof(tile_mapType_Light)) => MakeCartoLightUrl,

            // MAPBOX
            (nameof(tile_provider_mapbox), nameof(tile_mapType_Comic)) => CreateFromXyzUrlCharServer("https://{@server}.tiles.mapbox.com/v4/mapbox.comic/{z}/{x}/{y}.jpg?access_token=pk.eyJ1IjoibW9ob2tvZW1haWxob3N0aW5mbyIsImEiOiJjanU5bmFlbDcxYjNkNDRuenB1cHF6YXo0In0.sdTlXpsCH35pTyzOGK3K8w"),
            (nameof(tile_provider_mapbox), nameof(tile_mapType_Satellite)) => CreateFromXyzUrlCharServer("https://{@server}.tiles.mapbox.com/v4/mapbox.light/{z}/{x}/{y}.jpg?access_token=pk.eyJ1IjoibW9ob2tvZW1haWxob3N0aW5mbyIsImEiOiJjanU5bmFlbDcxYjNkNDRuenB1cHF6YXo0In0.sdTlXpsCH35pTyzOGK3K8w"),

            (_, _) => tile => null
        }; ; 
    }
}