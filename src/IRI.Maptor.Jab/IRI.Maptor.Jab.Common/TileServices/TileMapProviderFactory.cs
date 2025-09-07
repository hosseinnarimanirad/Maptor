using System;
using System.Text;
using System.Collections.Generic;

using IRI.Maptor.Sta.Spatial.Model;
using IRI.Maptor.Jab.Common.Helpers;
using static IRI.Maptor.Jab.Common.Properties.Resources;
using System.Linq;

namespace IRI.Maptor.Jab.Common.TileServices;

public static class TileMapProviderFactory
{
    public static readonly char[] _serverChar = new char[] { 'a', 'b', 'c', 'd' };

    public static string GoogleProvider = "GOOGLE";
    public static string BingProvider = "BING";
    public static string NokiaProvider = "NOKIA";
    public static string OsmProvider = "OPENSTREETMAP";
    public static string WazeProvider = "WAZE";
    public static string CartoProvider = "CARTO";
    public static string Yandex = "YANDEX";
    public static string Mapbox = "MAPBOX";

    private static readonly string baseMapUri = "IRI.Maptor.Jab.Common;component/Assets/Images/BaseMaps";
    //this can be used in the case of interanet network without internet connection. samples:
    //this.AddProvider(TileMapProviderFactory.CreateInteranetProvider("localGoogle", "roadMap", t => $@"http://v-gisserver2/Google/Road/{t.ZoomLevel}/gm_{t.ColumnNumber}_{t.RowNumber}_{t.ZoomLevel}.png"));
    // this.AddProvider(TileMapProviderFactory.CreateInteranetProvider("localGoogle", "terrainMap", t => $@"http://v-gisserver2/Google/TerrainWithRoad/{t.ZoomLevel}/gtr_{t.ColumnNumber}_{t.RowNumber}_{t.ZoomLevel}.jpg"));
    // this.AddProvider(TileMapProviderFactory.CreateInteranetProvider("localGoogle", "satelliteMap", t => $@"http://v-gisserver2/Google/Satellite/{t.ZoomLevel}/gs_{t.ColumnNumber}_{t.RowNumber}_{t.ZoomLevel}.jpg"));
    // this.AddProvider(TileMapProviderFactory.CreateInteranetProvider("localGoogle", "hybridMap", t => $@"http://v-gisserver2/Google/Satellite/{t.ZoomLevel}/gs_{t.ColumnNumber}_{t.RowNumber}_{t.ZoomLevel}.jpg"));
    public static TileMapProvider CreateInteranetProvider(string providerName, string subTitle, Func<TileInfo, string> interanetUrlFunc)
    {
        TileMapProvider result = new TileMapProvider(providerName, subTitle, interanetUrlFunc, null, null)
        {
            RequireInternetConnection = false
        };

        return result;
    }

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

    private static TileMapProvider? _bingSatellite;
    public static TileMapProvider BingSatellite
    {
        get
        {
            if (_bingSatellite is null)
            {
                _bingSatellite = new TileMapProvider( 
                    nameof(tile_provider_bing),
                    nameof(tile_mapType_Satellite),
                    tile => MakeBingSatelliteUrl(tile, GetServer()),
                    ResourceHelper.ReadBinaryStreamFromResource($"{baseMapUri}/bingSatellite.jpg"),
                    ResourceHelper.ReadBinaryStreamFromResource($"{baseMapUri}/72/bingSatellite72.jpg"));
            }

            return _bingSatellite;
        }
    }

    private static TileMapProvider? _bingStreet;
    public static TileMapProvider BingStreet
    {
        get
        {
            if (_bingStreet is null)
            {
                _bingStreet = new TileMapProvider( 
                    nameof(tile_provider_bing),
                    nameof(tile_mapType_Street),
                    tile => MakeBingStreetUrl(tile, GetServer()),
                    ResourceHelper.ReadBinaryStreamFromResource($"{baseMapUri}/bingStreet.jpg"),
                    ResourceHelper.ReadBinaryStreamFromResource($"{baseMapUri}/72/bingStreet72.jpg"));

            }

            return _bingStreet;
        }
    }


    private static TileMapProvider? _bingHybrid;
    public static TileMapProvider BingHybrid
    {
        get
        {
            if (_bingHybrid is null)
            {
                _bingHybrid = new TileMapProvider( 
                    nameof(tile_provider_bing),
                    nameof(tile_mapType_Hybrid),
                    tile => MakeBingHybridUrl(tile, GetServer()),
                    ResourceHelper.ReadBinaryStreamFromResource($"{baseMapUri}/bingHybrid.jpg"),
                    ResourceHelper.ReadBinaryStreamFromResource($"{baseMapUri}/72/bingHybrid72.jpg"));
            }

            return _bingHybrid;
        }
    }

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


    private static TileMapProvider? _googleCleanGrey;
    public static TileMapProvider GoogleCleanGrey
    {
        get
        {
            if (_googleCleanGrey is null)
            {
                _googleCleanGrey = new TileMapProvider( 
                    nameof(tile_provider_google),
                    nameof(tile_mapType_CleanGrey),
                    MakeGoogleCleanGreyUrl,
                    ResourceHelper.ReadBinaryStreamFromResource($"{baseMapUri}/googleTerrain.png"),
                    ResourceHelper.ReadBinaryStreamFromResource($"{baseMapUri}/72/googleGrey72.jpg"));
            }

            return _googleCleanGrey;
        }
    }


    private static TileMapProvider? _googleBlackWhite;
    public static TileMapProvider GoogleBlackWhite
    {
        get
        {
            if (_googleBlackWhite is null)
            {
                _googleBlackWhite = new TileMapProvider( 
                    nameof(tile_provider_google),
                    nameof(tile_mapType_BlackWhite),
                    MakeGoogleBlackWhiteUrl,
                    ResourceHelper.ReadBinaryStreamFromResource($"{baseMapUri}/googleTerrain.png"),
                    ResourceHelper.ReadBinaryStreamFromResource($"{baseMapUri}/72/googleBlackWhite72.jpg"));
            }

            return _googleBlackWhite;
        }
    }


    private static TileMapProvider? _googleTraffic;
    public static TileMapProvider GoogleTraffic
    {
        get
        {
            if (_googleTraffic is null)
            {
                _googleTraffic = new TileMapProvider( 
                    nameof(tile_provider_google),
                    nameof(tile_mapType_Traffic),
                    tile => MakeGoogleTerafficUrl(tile, GetServer()),
                    ResourceHelper.ReadBinaryStreamFromResource($"{baseMapUri}/googleTerrain.png"),
                    ResourceHelper.ReadBinaryStreamFromResource($"{baseMapUri}/72/googleTraffic72.jpg"))
                {
                    AllowCache = false
                };
            }

            return _googleTraffic;
        }
    }


    private static TileMapProvider? _googleSatellite;
    public static TileMapProvider GoogleSatellite
    {
        get
        {
            if (_googleSatellite is null)
            {
                _googleSatellite = new TileMapProvider( 
                    nameof(tile_provider_google),
                    nameof(tile_mapType_Satellite),
                    tile => MakeGoogleSatelliteUrl(tile, GetServer()),
                    ResourceHelper.ReadBinaryStreamFromResource($"{baseMapUri}/googleSatellite.jpg"),
                    ResourceHelper.ReadBinaryStreamFromResource($"{baseMapUri}/72/googleSatellite72.jpg"));
            }

            return _googleSatellite;
        }
    }


    private static TileMapProvider? _googleHybrid;
    public static TileMapProvider GoogleHybrid
    {
        get
        {
            if (_googleHybrid is null)
            {
                _googleHybrid = new TileMapProvider( 
                    nameof(tile_provider_google),
                    nameof(tile_mapType_Hybrid),
                    tile => MakeGoogleHybridUrl(tile, GetServer()),
                    ResourceHelper.ReadBinaryStreamFromResource($"{baseMapUri}/googleHybrid.jpg"),
                    ResourceHelper.ReadBinaryStreamFromResource($"{baseMapUri}/72/googleHybrid72.jpg"));
            }

            return _googleHybrid;
        }
    }


    private static TileMapProvider? _googleRoadMap;
    public static TileMapProvider GoogleRoadMap
    {
        get
        {
            if (_googleRoadMap is null)
            {
                _googleRoadMap = new TileMapProvider( 
                    nameof(tile_provider_google),
                    nameof(tile_mapType_RoadMap),
                    tile => MakeGoogleRoadMapUrl(tile, GetServer()),
                    ResourceHelper.ReadBinaryStreamFromResource($"{baseMapUri}/googleRoadmap.jpg"),
                    ResourceHelper.ReadBinaryStreamFromResource($"{baseMapUri}/72/googleRoadMap72.jpg"));
            }

            return _googleRoadMap;
        }
    }


    private static TileMapProvider? _googleTerrain;
    public static TileMapProvider GoogleTerrain
    {
        get
        {
            if (_googleTerrain is null)
            {
                _googleTerrain = new TileMapProvider( 
                    nameof(tile_provider_google),
                    nameof(tile_mapType_Terrain),
                    tile => MakeGoogleTerrainUrl(tile, GetServer()),
                    ResourceHelper.ReadBinaryStreamFromResource($"{baseMapUri}/googleTerrain.png"),
                    ResourceHelper.ReadBinaryStreamFromResource($"{baseMapUri}/72/googleTerrain72.jpg"));
            }

            return _googleTerrain;
        }
    }


    //private static TileMapProvider _googleBike;
    //public static TileMapProvider GoogleBike
    //{
    //    get
    //    {
    //        if (_googleBike == null)
    //        {
    //            _googleBike = CreateFromXyzUrlIntServer(
    //                new PersianEnglishItem("گوگل", GoogleProvider),
    //                new PersianEnglishItem("دوچرخه", "Bike"),
    //                "https://mt{@server}.google.com/vt/lyrs=m,bike&x={x}&y={y}&z={z}",
    //               $"{baseMapUri}/googleTerrain.png",
    //               $"{baseMapUri}/72/bingStreet72.jpg");

    //        }

    //        return _googleBike;
    //    }
    //}


    private static TileMapProvider? _googleLight;
    public static TileMapProvider GoogleLight
    {
        get
        {
            if (_googleLight is null)
            {
                _googleLight = CreateFromXyzUrlIntServer( 
                    nameof(tile_provider_google),
                    nameof(tile_mapType_Light),
                    "https://mt{@server}.google.com/vt/lyrs=r&x={x}&y={y}&z={z}",
                   $"{baseMapUri}/googleTerrain.png",
                   $"{baseMapUri}/72/googleLight72.jpg");

            }

            return _googleLight;
        }
    }



    private static TileMapProvider? _googleNature;
    public static TileMapProvider GoogleNature
    {
        get
        {
            if (_googleNature is null)
            {
                _googleNature = CreateFromXyzUrlIntServer( 
                    nameof(tile_provider_google),
                    nameof(tile_mapType_Nature),
                   "https://maps.googleapis.com/maps/vt?pb=!1m5!1m4!1i{z}!2i{x}!3i{y}!4i256!2m3!1e0!2sm!3i{y}!3m14!2snl!3sUS!5e18!12m1!1e68!12m3!1e37!2m1!1ssmartmaps!12m4!1e26!2m2!1sstyles!2zcy50OjV8cC5oOiNGRkE4MDB8cC5nOjEscy50OjQ5fHAuaDojNTNGRjAwfHAuczotNzN8cC5sOjQwfHAuZzoxLHMudDo1MHxwLmg6I0ZCRkYwMHxwLmc6MSxzLnQ6NTF8cC5oOiMwMEZGRkR8cC5sOjMwfHAuZzoxLHMudDo2fHAuaDojMDBCRkZGfHAuczo2fHAubDo4fHAuZzoxLHMudDoyfHAuaDojNjc5NzE0fHAuczozMy40fHAubDotMjUuNHxwLmc6MQ!4e0!23i1301875",
                   $"{baseMapUri}/googleTerrain.png",
                   $"{baseMapUri}/72/googleNature72.jpg");

            }

            return _googleNature;
        }
    }


    private static TileMapProvider? _googleNeutralBlue;
    public static TileMapProvider GoogleNeutralBlue
    {
        get
        {
            if (_googleNeutralBlue is null)
            {
                _googleNeutralBlue = CreateFromXyzUrlIntServer( 
                    nameof(tile_provider_google),
                    nameof(tile_mapType_NeutralBlue),
                    "https://maps.googleapis.com/maps/vt?pb=!1m5!1m4!1i{z}!2i{x}!3i{y}!4i256!2m3!1e0!2sm!3i{y}!3m14!2snl!3sUS!5e18!12m1!1e68!12m3!1e37!2m1!1ssmartmaps!12m4!1e26!2m2!1sstyles!2zcy50OjZ8cy5lOmd8cC5jOiNmZjE5MzM0MSxzLnQ6NXxzLmU6Z3xwLmM6I2ZmMmM1YTcxLHMudDozfHMuZTpnfHAuYzojZmYyOTc2OGF8cC5sOi0zNyxzLnQ6MnxzLmU6Z3xwLmM6I2ZmNDA2ZDgwLHMudDo0fHMuZTpnfHAuYzojZmY0MDZkODAscy5lOmwudC5zfHAudjpvbnxwLmM6I2ZmM2U2MDZmfHAudzoyfHAuZzowLjg0LHMuZTpsLnQuZnxwLmM6I2ZmZmZmZmZmLHMudDoxfHMuZTpnfHAudzowLjZ8cC5jOiNmZjFhMzU0MSxzLmU6bC5pfHAudjpvZmYscy50OjQwfHMuZTpnfHAuYzojZmYyYzVhNzE!4e0!23i1301875",
                    $"{baseMapUri}/googleTerrain.png",
                    $"{baseMapUri}/72/googleNeutralBlue72.jpg");

            }

            return _googleNeutralBlue;
        }
    }



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


    //public static TileMapProvider NokiaSatellite { get; private set; }

    //public static TileMapProvider NokiaHybrid { get; private set; }

    //public static TileMapProvider NokiaRoadMap { get; private set; }

    //public static TileMapProvider NokiaTerrain { get; private set; }

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


    private static TileMapProvider? _openStreetMap;
    public static TileMapProvider OpenStreetMap
    {
        get
        {
            if (_openStreetMap is null)
            {
                _openStreetMap = new TileMapProvider( 
                    nameof(tile_provider_osm),
                    nameof(tile_mapType_Street),
                    tile => MakeOpenStreetMapUrl(tile, GetServerCharacter()),
                    ResourceHelper.ReadBinaryStreamFromResource($"{baseMapUri}/openStreetMap.png"),
                    ResourceHelper.ReadBinaryStreamFromResource($"{baseMapUri}/72/osmOpenStreetMap72.jpg"));
            }

            return _openStreetMap;
        }
    }


    private static TileMapProvider? _openTopoMap;
    public static TileMapProvider OpenTopoMap
    {
        get
        {
            if (_openTopoMap is null)
            {
                _openTopoMap = new TileMapProvider( 
                    nameof(tile_provider_osm),
                    nameof(tile_mapType_Topo),
                    tile => MakeOpenTopoMapUrl(tile, GetServerCharacter()),
                    ResourceHelper.ReadBinaryStreamFromResource($"{baseMapUri}/openTopoMap.png"),
                    ResourceHelper.ReadBinaryStreamFromResource($"{baseMapUri}/72/osmOpenTopoMap72.jpg"));
            }

            return _openTopoMap;
        }
    }


    private static TileMapProvider? _mapyWinter;
    public static TileMapProvider MapyWinter
    {
        get
        {
            if (_mapyWinter is null)
            {
                _mapyWinter = new TileMapProvider( 
                    nameof(tile_provider_osm),
                    nameof(tile_mapType_MapyWinter),
                    MakeMapyWinterUrl,
                    ResourceHelper.ReadBinaryStreamFromResource($"{baseMapUri}/mapyWinter.jpg"),
                    ResourceHelper.ReadBinaryStreamFromResource($"{baseMapUri}/72/osmMapyWinter72.jpg"));
            }

            return _mapyWinter;
        }
    }


    private static TileMapProvider? _mapyTourist;
    public static TileMapProvider MapyTourist
    {
        get
        {
            if (_mapyTourist is null)
            {
                _mapyTourist = new TileMapProvider( 
                    nameof(tile_provider_osm),
                    nameof(tile_mapType_MapyTourist),
                    MakeMapyTouristUrl,
                    ResourceHelper.ReadBinaryStreamFromResource($"{baseMapUri}/mapyTourism.jpg"),
                    ResourceHelper.ReadBinaryStreamFromResource($"{baseMapUri}/72/osmMapyTourist72.jpg"));
            }

            return _mapyTourist;
        }
    }


    private static TileMapProvider? _osmHikeBike;
    public static TileMapProvider OsmHikeBike
    {
        get
        {
            if (_osmHikeBike is null)
            {
                _osmHikeBike = new TileMapProvider( 
                    nameof(tile_provider_osm),
                    nameof(tile_mapType_HikeBike),
                    MakeOsmHikeBikeUrl,
                    ResourceHelper.ReadBinaryStreamFromResource($"{baseMapUri}/osmHikeBike.jpg"),
                    ResourceHelper.ReadBinaryStreamFromResource($"{baseMapUri}/72/osmHikeBike72.jpg"));
            }

            return _osmHikeBike;
        }
    }


    private static TileMapProvider? _stamentWatercolor;
    public static TileMapProvider StamenWatercolor
    {
        get
        {
            if (_stamentWatercolor is null)
            {
                _stamentWatercolor = new TileMapProvider( 
                    nameof(tile_provider_osm),
                    nameof(tile_mapType_Watercolor),
                    MakeStamenWatercolorUrl,
                    ResourceHelper.ReadBinaryStreamFromResource($"{baseMapUri}/stamenWatercolor.jpg"),
                    ResourceHelper.ReadBinaryStreamFromResource($"{baseMapUri}/stamenWatercolor.jpg"));
            }

            return _stamentWatercolor;
        }
    }



    #endregion


    #region Waze

    //https://worldtiles3.waze.com/tiles/11/1313/805.png
    //server: 1, 2, 3, 4
    private static string MakeWazeRoadMapUrl(TileInfo tile) => $@"https://worldtiles{GetServer(1, 4)}.waze.com/tiles/{tile.ZoomLevel}/{tile.ColumnNumber}/{tile.RowNumber}.png";

    private static TileMapProvider? _wazeStreet;
    public static TileMapProvider WazeStreet
    {
        get
        {
            if (_wazeStreet is null)
            {
                _wazeStreet = new TileMapProvider( 
                    nameof(tile_provider_waze),
                    nameof(tile_mapType_Street),
                    MakeWazeRoadMapUrl,
                    ResourceHelper.ReadBinaryStreamFromResource($"{baseMapUri}/waze.png"),
                    ResourceHelper.ReadBinaryStreamFromResource($"{baseMapUri}/72/wazeStreet72.jpg"));
            }

            return _wazeStreet;
        }
    }

    #endregion


    #region Carto

    //@2x parameter say image should be twise in size
    //servers: a, b, c, d
    //https://cartodb-basemaps-c.global.ssl.fastly.net/light_all/14/10525/6444@2x.png

    private static string MakeCartoLightUrl(TileInfo tile) => $@"https://cartodb-basemaps-{GetServerCharacter()}.global.ssl.fastly.net/light_all/{tile.ZoomLevel}/{tile.ColumnNumber}/{tile.RowNumber}.png";

    private static string MakeCartoDarkUrl(TileInfo tile) => $@"https://cartodb-basemaps-{GetServerCharacter()}.global.ssl.fastly.net/dark_all/{tile.ZoomLevel}/{tile.ColumnNumber}/{tile.RowNumber}.png";

    private static TileMapProvider? _cartoDark;
    public static TileMapProvider CartoDark
    {
        get
        {
            if (_cartoDark is null)
            {
                _cartoDark = new TileMapProvider( 
                    nameof(tile_provider_carto),
                    nameof(tile_mapType_Dark),
                    MakeCartoDarkUrl,
                    ResourceHelper.ReadBinaryStreamFromResource($"{baseMapUri}/cartoDark.jpg"),
                    ResourceHelper.ReadBinaryStreamFromResource($"{baseMapUri}/72/cartoDark72.jpg"));
            }

            return _cartoDark;
        }
    }

    private static TileMapProvider? _cartoLight;
    public static TileMapProvider CartoLight
    {
        get
        {
            if (_cartoLight is null)
            {
                _cartoLight = new TileMapProvider( 
                    nameof(tile_provider_carto),
                    nameof(tile_mapType_Light),
                    MakeCartoLightUrl,
                    ResourceHelper.ReadBinaryStreamFromResource($"{baseMapUri}/cartoLight.jpg"),
                    ResourceHelper.ReadBinaryStreamFromResource($"{baseMapUri}/72/cartoLight72.jpg"));
            }

            return _cartoLight;
        }
    }

    #endregion

    #region Mapbox Commic


    private static TileMapProvider? _mapboxComic;
    public static TileMapProvider MapboxComic
    {
        get
        {
            if (_mapboxComic is null)
            {
                _mapboxComic = CreateFromXyzUrlCharServer( 
                    nameof(tile_provider_mapbox),
                    nameof(tile_mapType_Comic),
                    "https://{@server}.tiles.mapbox.com/v4/mapbox.comic/{z}/{x}/{y}.jpg?access_token=pk.eyJ1IjoibW9ob2tvZW1haWxob3N0aW5mbyIsImEiOiJjanU5bmFlbDcxYjNkNDRuenB1cHF6YXo0In0.sdTlXpsCH35pTyzOGK3K8w",
                    $"{baseMapUri}/72/mapboxComic72.jpg",
                    $"{baseMapUri}/72/mapboxComic72.jpg");
            }

            return _mapboxComic;
        }
    }
    //http://b.tiles.mapbox.com/v4/mapbox.satellite/6/44/26.png?access_token=pk.eyJ1IjoibW9ob2tvZW1haWxob3N0aW5mbyIsImEiOiJjanU5bmFlbDcxYjNkNDRuenB1cHF6YXo0In0.sdTlXpsCH35pTyzOGK3K8w
    private static TileMapProvider? _mapboxSatellite;
    public static TileMapProvider MapboxSatellite
    {
        get
        {
            if (_mapboxSatellite is null)
            {
                _mapboxSatellite = CreateFromXyzUrlCharServer( 
                    nameof(tile_provider_mapbox),
                    nameof(tile_mapType_Satellite),
                    "https://{@server}.tiles.mapbox.com/v4/mapbox.light/{z}/{x}/{y}.jpg?access_token=pk.eyJ1IjoibW9ob2tvZW1haWxob3N0aW5mbyIsImEiOiJjanU5bmFlbDcxYjNkNDRuenB1cHF6YXo0In0.sdTlXpsCH35pTyzOGK3K8w",
                    $"{baseMapUri}/72/mapboxComic72.jpg",
                    $"{baseMapUri}/72/mapboxComic72.jpg");
            }

            return _mapboxSatellite;
        }
    }

    #endregion


    #region Yandex

    ////در راستای y شیفت داره
    ////https://vec01.maps.yandex.net/tiles?l=map&x=10529&y=6455&z=14
    ////servers: 01,02,03,04
    //private static string MakeYandexMapUrl(TileInfo tile) => $@"https://vec0{GetServer(1, 4)}.maps.yandex.net/tiles?l=map&x={tile.ColumnNumber}&y={tile.RowNumber}&z={tile.ZoomLevel}";


    //private static TileMapProvider _yandexMap;
    //public static TileMapProvider YandexMap
    //{
    //    get
    //    {
    //        if (_yandexMap == null)
    //        {
    //            _yandexMap = new TileMapProvider(
    //                new PersianEnglishItem("یاندکس", Yandex),
    //                new PersianEnglishItem("راه", "Road"),
    //                tile => MakeYandexMapUrl(tile))
    //            {
    //                Thumbnail = ResourceHelper.ReadBinaryStreamFromResource($"{baseMapUri}/cartoDark.jpg")
    //            };
    //        }

    //        return _yandexMap;
    //    }
    //}

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


    public static TileMapProvider CreateFromXyzUrl(string providerResourceKey, string mapTypeResourceKey, string url, string thumbnailAddress, string thumbnailAddress72)
    {
        var mapUrl = url.Replace("{x}", "{0}").Replace("{y}", "{1}").Replace("{z}", "{2}");

        return new TileMapProvider(
                    providerResourceKey,
                    mapTypeResourceKey,
                    tile => string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                        mapUrl,
                                        tile.ColumnNumber,
                                        tile.RowNumber,
                                        tile.ZoomLevel),
                    ResourceHelper.ReadBinaryStreamFromResource(thumbnailAddress),
                    ResourceHelper.ReadBinaryStreamFromResource(thumbnailAddress72));
    }

    public static TileMapProvider CreateFromXyzUrlIntServer(string providerResourceKey, string mapTypeResourceKey, string url, string thumbnailAddress, string thumbnail72Address, int minServer = 0, int maxServer = 3)
    {
        var mapUrl = url.Replace("{x}", "{0}").Replace("{y}", "{1}").Replace("{z}", "{2}").Replace("{@server}", "{3}");

        return new TileMapProvider(
                    providerResourceKey,
                    mapTypeResourceKey,
                    tile => string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                                                            mapUrl,
                                                                            tile.ColumnNumber,
                                                                            tile.RowNumber,
                                                                            tile.ZoomLevel,
                                                                            GetServer(minServer, maxServer)),
                    ResourceHelper.ReadBinaryStreamFromResource(thumbnailAddress),
                    ResourceHelper.ReadBinaryStreamFromResource(thumbnail72Address));
    }

    public static TileMapProvider CreateFromXyzUrlCharServer(string providerResourceKey, string mapTypeResourceKey, string url, string thumbnailAddress, string thumbnail72Address, int minServer = 0, int maxServer = 2)
    {
        var mapUrl = url.Replace("{x}", "{0}").Replace("{y}", "{1}").Replace("{z}", "{2}").Replace("{@server}", "{3}");

        return new TileMapProvider(
                    providerResourceKey,
                    mapTypeResourceKey,
                    tile => string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                                                            mapUrl,
                                                                            tile.ColumnNumber,
                                                                            tile.RowNumber,
                                                                            tile.ZoomLevel,
                                                                            GetServerCharacter(minServer, maxServer)),
                    ResourceHelper.ReadBinaryStreamFromResource(thumbnailAddress),
                    ResourceHelper.ReadBinaryStreamFromResource(thumbnail72Address));
    }


    public static List<TileMapProvider> GetAll()
    {
        return new List<TileMapProvider>()
        {
            BingHybrid, BingSatellite, BingStreet,
            GoogleHybrid, GoogleRoadMap, GoogleSatellite, GoogleTerrain,
            GoogleCleanGrey, GoogleBlackWhite, GoogleTraffic, GoogleLight, GoogleNature, GoogleNeutralBlue,
            OpenStreetMap, OpenTopoMap,
            WazeStreet,
            OpenStreetMap, OpenTopoMap, OsmHikeBike, MapyTourist, MapyWinter, StamenWatercolor,
            WazeStreet,
            CartoDark, CartoLight,
            MapboxComic, MapboxSatellite
        };
    }

    public static List<TileMapProvider> GetDefault()
    {
        return new List<TileMapProvider>()
        {
            GoogleRoadMap, GoogleTerrain, GoogleSatellite, GoogleHybrid,
            BingHybrid, BingSatellite,
            OpenStreetMap, OpenTopoMap
        };
    }

}
