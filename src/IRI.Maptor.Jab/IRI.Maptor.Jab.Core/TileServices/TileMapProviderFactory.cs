using IRI.Maptor.Jab.Core;
using IRI.Maptor.Sta.Spatial.Model;

using static IRI.Maptor.Jab.Core.Properties.Resources;

namespace IRI.Maptor.Jab.Core.TileServices;

public static class TileMapProviderFactory
{
    private static byte[]? ReadEmbeddedImage(string fileAddress)
    {
        // MSBuild prefixes path segments that start with a digit with '_' (e.g. "72/" → "_72.")
        var name = string.Join(".", fileAddress.Replace('/', '.').Split('.')
            .Select(s => s.Length > 0 && char.IsDigit(s[0]) ? "_" + s : s));
        using var stream = typeof(TileMapProviderFactory).Assembly.GetManifestResourceStream(name);
        if (stream is null) return null;
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    private static readonly string baseMapUri = "IRI.Maptor.Jab.Core.Assets.Images.BaseMaps";
    //this can be used in the case of interanet network without internet connection. samples:
    //this.AddProvider(TileMapProviderFactory.CreateInteranetProvider("localGoogle", "roadMap", t => $@"http://v-gisserver2/Google/Road/{t.ZoomLevel}/gm_{t.ColumnNumber}_{t.RowNumber}_{t.ZoomLevel}.png"));
    // this.AddProvider(TileMapProviderFactory.CreateInteranetProvider("localGoogle", "terrainMap", t => $@"http://v-gisserver2/Google/TerrainWithRoad/{t.ZoomLevel}/gtr_{t.ColumnNumber}_{t.RowNumber}_{t.ZoomLevel}.jpg"));
    // this.AddProvider(TileMapProviderFactory.CreateInteranetProvider("localGoogle", "satelliteMap", t => $@"http://v-gisserver2/Google/Satellite/{t.ZoomLevel}/gs_{t.ColumnNumber}_{t.RowNumber}_{t.ZoomLevel}.jpg"));
    // this.AddProvider(TileMapProviderFactory.CreateInteranetProvider("localGoogle", "hybridMap", t => $@"http://v-gisserver2/Google/Satellite/{t.ZoomLevel}/gs_{t.ColumnNumber}_{t.RowNumber}_{t.ZoomLevel}.jpg"));
    public static TileMapProvider CreateInteranetProvider(BaseMapType type, string providerName, string subTitle, Func<TileInfo, string> interanetUrlFunc)
    {
        return TileMapProvider.CreateLocalNetwork(type, providerName, subTitle, null, null, interanetUrlFunc);
    }

    #region Bing

    private static TileMapProvider? _bingSatellite;
    public static TileMapProvider BingSatellite
    {
        get
        {
            if (_bingSatellite is null)
            {
                _bingSatellite = TileMapProvider.Create(
                    BaseMapType.Bing_Satellite,
                    nameof(tile_provider_bing),
                    nameof(tile_mapType_satellite),
                    //tile => MakeBingSatelliteUrl(tile, GetServer()),
                    ReadEmbeddedImage($"{baseMapUri}/bingSatellite.jpg"),
                    ReadEmbeddedImage($"{baseMapUri}/72/bingSatellite72.jpg"));
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
                _bingStreet = TileMapProvider.Create(
                    BaseMapType.Bing_Street,
                    nameof(tile_provider_bing),
                    nameof(tile_mapType_street),
                    //tile => MakeBingStreetUrl(tile, GetServer()),
                    ReadEmbeddedImage($"{baseMapUri}/bingStreet.jpg"),
                    ReadEmbeddedImage($"{baseMapUri}/72/bingStreet72.jpg"));
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
                _bingHybrid = TileMapProvider.Create(
                    BaseMapType.Bing_Hybrid,
                    nameof(tile_provider_bing),
                    nameof(tile_mapType_hybrid),
                    //tile => MakeBingHybridUrl(tile, GetServer()),
                    ReadEmbeddedImage($"{baseMapUri}/bingHybrid.jpg"),
                    ReadEmbeddedImage($"{baseMapUri}/72/bingHybrid72.jpg"));
            }

            return _bingHybrid;
        }
    }

    #endregion


    #region Google

    private static TileMapProvider? _googleCleanGrey;
    public static TileMapProvider GoogleCleanGrey
    {
        get
        {
            if (_googleCleanGrey is null)
            {
                _googleCleanGrey = TileMapProvider.Create(
                    BaseMapType.Google_CleanGrey,
                    nameof(tile_provider_google),
                    nameof(tile_mapType_cleanGrey),
                    //MakeGoogleCleanGreyUrl,
                    ReadEmbeddedImage($"{baseMapUri}/googleTerrain.png"),
                    ReadEmbeddedImage($"{baseMapUri}/72/googleGrey72.jpg"));
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
                _googleBlackWhite = TileMapProvider.Create(
                    BaseMapType.Google_BlackWhite,
                    nameof(tile_provider_google),
                    nameof(tile_mapType_blackWhite),
                    //MakeGoogleBlackWhiteUrl,
                    ReadEmbeddedImage($"{baseMapUri}/googleTerrain.png"),
                    ReadEmbeddedImage($"{baseMapUri}/72/googleBlackWhite72.jpg"));
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
                _googleTraffic = TileMapProvider.Create(
                    BaseMapType.Google_Traffic,
                    nameof(tile_provider_google),
                    nameof(tile_mapType_traffic),
                    //tile => MakeGoogleTerafficUrl(tile, GetServer()),
                    ReadEmbeddedImage($"{baseMapUri}/googleTerrain.png"),
                    ReadEmbeddedImage($"{baseMapUri}/72/googleTraffic72.jpg"));

                _googleTraffic.AllowCache = false;
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
                _googleSatellite = TileMapProvider.Create(
                    BaseMapType.Google_Satellite,
                    nameof(tile_provider_google),
                    nameof(tile_mapType_satellite),
                    //tile => MakeGoogleSatelliteUrl(tile, GetServer()),
                    ReadEmbeddedImage($"{baseMapUri}/googleSatellite.jpg"),
                    ReadEmbeddedImage($"{baseMapUri}/72/googleSatellite72.jpg"));
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
                _googleHybrid = TileMapProvider.Create(
                    BaseMapType.Google_Hybrid,
                    nameof(tile_provider_google),
                    nameof(tile_mapType_hybrid),
                    //tile => MakeGoogleHybridUrl(tile, GetServer()),
                    ReadEmbeddedImage($"{baseMapUri}/googleHybrid.jpg"),
                    ReadEmbeddedImage($"{baseMapUri}/72/googleHybrid72.jpg"));
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
                _googleRoadMap = TileMapProvider.Create(
                    BaseMapType.Google_RoadMap,
                    nameof(tile_provider_google),
                    nameof(tile_mapType_roadMap),
                    //tile => MakeGoogleRoadMapUrl(tile, GetServer()),
                    ReadEmbeddedImage($"{baseMapUri}/googleRoadmap.jpg"),
                    ReadEmbeddedImage($"{baseMapUri}/72/googleRoadMap72.jpg"));
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
                _googleTerrain = TileMapProvider.Create(
                    BaseMapType.Google_Terrain,
                    nameof(tile_provider_google),
                    nameof(tile_mapType_terrain),
                    //tile => MakeGoogleTerrainUrl(tile, GetServer()),
                    ReadEmbeddedImage($"{baseMapUri}/googleTerrain.png"),
                    ReadEmbeddedImage($"{baseMapUri}/72/googleTerrain72.jpg"));
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
            //if (_googleLight is null)
            //{
            //    _googleLight = CreateFromXyzUrlIntServer(
            //        nameof(tile_provider_google),
            //        nameof(tile_mapType_light),
            //        "https://mt{@server}.google.com/vt/lyrs=r&x={x}&y={y}&z={z}",
            //       $"{baseMapUri}/googleTerrain.png",
            //       $"{baseMapUri}/72/googleLight72.jpg");
            //}

            if (_googleLight is null)
            {
                _googleLight = TileMapProvider.Create(
                    BaseMapType.Google_Light,
                    nameof(tile_provider_google),
                    nameof(tile_mapType_light),
                    //"https://mt{@server}.google.com/vt/lyrs=r&x={x}&y={y}&z={z}",
                    ReadEmbeddedImage($"{baseMapUri}/googleTerrain.png"),
                    ReadEmbeddedImage($"{baseMapUri}/72/googleLight72.jpg"));
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
                _googleNature = TileMapProvider.Create(
                    BaseMapType.Google_Nature,
                    nameof(tile_provider_google),
                    nameof(tile_mapType_nature),
                    //"https://maps.googleapis.com/maps/vt?pb=!1m5!1m4!1i{z}!2i{x}!3i{y}!4i256!2m3!1e0!2sm!3i{y}!3m14!2snl!3sUS!5e18!12m1!1e68!12m3!1e37!2m1!1ssmartmaps!12m4!1e26!2m2!1sstyles!2zcy50OjV8cC5oOiNGRkE4MDB8cC5nOjEscy50OjQ5fHAuaDojNTNGRjAwfHAuczotNzN8cC5sOjQwfHAuZzoxLHMudDo1MHxwLmg6I0ZCRkYwMHxwLmc6MSxzLnQ6NTF8cC5oOiMwMEZGRkR8cC5sOjMwfHAuZzoxLHMudDo2fHAuaDojMDBCRkZGfHAuczo2fHAubDo4fHAuZzoxLHMudDoyfHAuaDojNjc5NzE0fHAuczozMy40fHAubDotMjUuNHxwLmc6MQ!4e0!23i1301875",
                    ReadEmbeddedImage($"{baseMapUri}/googleTerrain.png"),
                    ReadEmbeddedImage($"{baseMapUri}/72/googleNature72.jpg"));

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
                _googleNeutralBlue = TileMapProvider.Create(
                    BaseMapType.Google_NeutralBlue,
                    nameof(tile_provider_google),
                    nameof(tile_mapType_neutralBlue),
                    //"https://maps.googleapis.com/maps/vt?pb=!1m5!1m4!1i{z}!2i{x}!3i{y}!4i256!2m3!1e0!2sm!3i{y}!3m14!2snl!3sUS!5e18!12m1!1e68!12m3!1e37!2m1!1ssmartmaps!12m4!1e26!2m2!1sstyles!2zcy50OjZ8cy5lOmd8cC5jOiNmZjE5MzM0MSxzLnQ6NXxzLmU6Z3xwLmM6I2ZmMmM1YTcxLHMudDozfHMuZTpnfHAuYzojZmYyOTc2OGF8cC5sOi0zNyxzLnQ6MnxzLmU6Z3xwLmM6I2ZmNDA2ZDgwLHMudDo0fHMuZTpnfHAuYzojZmY0MDZkODAscy5lOmwudC5zfHAudjpvbnxwLmM6I2ZmM2U2MDZmfHAudzoyfHAuZzowLjg0LHMuZTpsLnQuZnxwLmM6I2ZmZmZmZmZmLHMudDoxfHMuZTpnfHAudzowLjZ8cC5jOiNmZjFhMzU0MSxzLmU6bC5pfHAudjpvZmYscy50OjQwfHMuZTpnfHAuYzojZmYyYzVhNzE!4e0!23i1301875",
                    ReadEmbeddedImage($"{baseMapUri}/googleTerrain.png"),
                    ReadEmbeddedImage($"{baseMapUri}/72/googleNeutralBlue72.jpg"));

            }

            return _googleNeutralBlue;
        }
    }

    #endregion


    #region Nokia

    //public static TileMapProvider NokiaSatellite { get; private set; }

    //public static TileMapProvider NokiaHybrid { get; private set; }

    //public static TileMapProvider NokiaRoadMap { get; private set; }

    //public static TileMapProvider NokiaTerrain { get; private set; }

    #endregion


    #region Osm

    private static TileMapProvider? _openStreetMap;
    public static TileMapProvider OpenStreetMap
    {
        get
        {
            if (_openStreetMap is null)
            {
                _openStreetMap = TileMapProvider.Create(
                    BaseMapType.OSM_OpenStreetMap,
                    nameof(tile_provider_osm),
                    nameof(tile_mapType_street),
                    //tile => MakeOpenStreetMapUrl(tile, GetServerCharacter()),
                    ReadEmbeddedImage($"{baseMapUri}/openStreetMap.png"),
                    ReadEmbeddedImage($"{baseMapUri}/72/osmOpenStreetMap72.jpg"));
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
                _openTopoMap = TileMapProvider.Create(
                    BaseMapType.OSM_OpenTopoMap,
                    nameof(tile_provider_osm),
                    nameof(tile_mapType_topo),
                    //tile => MakeOpenTopoMapUrl(tile, GetServerCharacter()),
                    ReadEmbeddedImage($"{baseMapUri}/openTopoMap.png"),
                    ReadEmbeddedImage($"{baseMapUri}/72/osmOpenTopoMap72.jpg"));
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
                _mapyWinter = TileMapProvider.Create(
                    BaseMapType.OSM_MapyWinter,
                    nameof(tile_provider_osm),
                    nameof(tile_mapType_mapyWinter),
                    //MakeMapyWinterUrl,
                    ReadEmbeddedImage($"{baseMapUri}/mapyWinter.jpg"),
                    ReadEmbeddedImage($"{baseMapUri}/72/osmMapyWinter72.jpg"));
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
                _mapyTourist = TileMapProvider.Create(
                    BaseMapType.OSM_MapyTourist,
                    nameof(tile_provider_osm),
                    nameof(tile_mapType_mapyTourist),
                    //MakeMapyTouristUrl,
                    ReadEmbeddedImage($"{baseMapUri}/mapyTourism.jpg"),
                    ReadEmbeddedImage($"{baseMapUri}/72/osmMapyTourist72.jpg"));
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
                _osmHikeBike = TileMapProvider.Create(
                    BaseMapType.OSM_OsmHikeBike,
                    nameof(tile_provider_osm),
                    nameof(tile_mapType_hikeBike),
                    //MakeOsmHikeBikeUrl,
                    ReadEmbeddedImage($"{baseMapUri}/osmHikeBike.jpg"),
                    ReadEmbeddedImage($"{baseMapUri}/72/osmHikeBike72.jpg"));
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
                _stamentWatercolor = TileMapProvider.Create(
                    BaseMapType.OSM_StamenWatercolor,
                    nameof(tile_provider_osm),
                    nameof(tile_mapType_watercolor),
                    //MakeStamenWatercolorUrl,
                    ReadEmbeddedImage($"{baseMapUri}/stamenWatercolor.jpg"),
                    ReadEmbeddedImage($"{baseMapUri}/stamenWatercolor.jpg"));
            }

            return _stamentWatercolor;
        }
    }

    #endregion


    #region Waze

    private static TileMapProvider? _wazeStreet;
    public static TileMapProvider WazeStreet
    {
        get
        {
            if (_wazeStreet is null)
            {
                _wazeStreet = TileMapProvider.Create(
                    BaseMapType.Waze_Street,
                    nameof(tile_provider_waze),
                    nameof(tile_mapType_street),
                    //MakeWazeRoadMapUrl,
                    ReadEmbeddedImage($"{baseMapUri}/waze.png"),
                    ReadEmbeddedImage($"{baseMapUri}/72/wazeStreet72.jpg"));
            }

            return _wazeStreet;
        }
    }

    #endregion


    #region Carto

    private static TileMapProvider? _cartoDark;
    public static TileMapProvider CartoDark
    {
        get
        {
            if (_cartoDark is null)
            {
                _cartoDark = TileMapProvider.Create(
                    BaseMapType.Carto_Dark,
                    nameof(tile_provider_carto),
                    nameof(tile_mapType_dark),
                    //MakeCartoDarkUrl,
                    ReadEmbeddedImage($"{baseMapUri}/cartoDark.jpg"),
                    ReadEmbeddedImage($"{baseMapUri}/72/cartoDark72.jpg"));
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
                _cartoLight = TileMapProvider.Create(
                    BaseMapType.Carto_Light,
                    nameof(tile_provider_carto),
                    nameof(tile_mapType_light),
                    //MakeCartoLightUrl,
                    ReadEmbeddedImage($"{baseMapUri}/cartoLight.jpg"),
                    ReadEmbeddedImage($"{baseMapUri}/72/cartoLight72.jpg"));
            }

            return _cartoLight;
        }
    }

    #endregion

    #region Mapbox

    private static TileMapProvider? _mapboxComic;
    public static TileMapProvider MapboxComic
    {
        get
        {
            if (_mapboxComic is null)
            {
                _mapboxComic = TileMapProvider.Create(
                    BaseMapType.Mapbox_Comic,
                    nameof(tile_provider_mapbox),
                    nameof(tile_mapType_comic),
                    //"https://{@server}.tiles.mapbox.com/v4/mapbox.comic/{z}/{x}/{y}.jpg?access_token=pk.eyJ1IjoibW9ob2tvZW1haWxob3N0aW5mbyIsImEiOiJjanU5bmFlbDcxYjNkNDRuenB1cHF6YXo0In0.sdTlXpsCH35pTyzOGK3K8w",
                    ReadEmbeddedImage($"{baseMapUri}/72/mapboxComic72.jpg"),
                    ReadEmbeddedImage($"{baseMapUri}/72/mapboxComic72.jpg"));
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
                _mapboxSatellite = TileMapProvider.Create(
                    BaseMapType.Mapbox_Satellite,
                    nameof(tile_provider_mapbox),
                    nameof(tile_mapType_satellite),
                    //"https://{@server}.tiles.mapbox.com/v4/mapbox.light/{z}/{x}/{y}.jpg?access_token=pk.eyJ1IjoibW9ob2tvZW1haWxob3N0aW5mbyIsImEiOiJjanU5bmFlbDcxYjNkNDRuenB1cHF6YXo0In0.sdTlXpsCH35pTyzOGK3K8w",
                    ReadEmbeddedImage($"{baseMapUri}/72/mapboxComic72.jpg"),
                    ReadEmbeddedImage($"{baseMapUri}/72/mapboxComic72.jpg"));
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
    //            _yandexMap = TileMapProvider.Create(
    //                new PersianEnglishItem("یاندکس", Yandex),
    //                new PersianEnglishItem("راه", "Road"),
    //                tile => MakeYandexMapUrl(tile))
    //            {
    //                Thumbnail = ReadEmbeddedImage($"{baseMapUri}/cartoDark.jpg")
    //            };
    //        }

    //        return _yandexMap;
    //    }
    //}

    #endregion



    //public static TileMapProvider CreateFromXyzUrl(string providerResourceKey, string mapTypeResourceKey, string url, string thumbnailAddress, string thumbnailAddress72)
    //{
    //    var mapUrl = url.Replace("{x}", "{0}").Replace("{y}", "{1}").Replace("{z}", "{2}");

    //    return TileMapProvider.Create(
    //                providerResourceKey,
    //                mapTypeResourceKey,
    //                tile => string.Format(System.Globalization.CultureInfo.InvariantCulture,
    //                                    mapUrl,
    //                                    tile.ColumnNumber,
    //                                    tile.RowNumber,
    //                                    tile.ZoomLevel),
    //                ReadEmbeddedImage(thumbnailAddress),
    //                ReadEmbeddedImage(thumbnailAddress72));
    //}

    //public static TileMapProvider CreateFromXyzUrlIntServer(string providerResourceKey, string mapTypeResourceKey, string url, string thumbnailAddress, string thumbnail72Address, int minServer = 0, int maxServer = 3)
    //{
    //    var mapUrl = url.Replace("{x}", "{0}").Replace("{y}", "{1}").Replace("{z}", "{2}").Replace("{@server}", "{3}");

    //    return TileMapProvider.Create(
    //                providerResourceKey,
    //                mapTypeResourceKey,
    //                tile => string.Format(System.Globalization.CultureInfo.InvariantCulture,
    //                                                                        mapUrl,
    //                                                                        tile.ColumnNumber,
    //                                                                        tile.RowNumber,
    //                                                                        tile.ZoomLevel,
    //                                                                        GetServer(minServer, maxServer)),
    //                ReadEmbeddedImage(thumbnailAddress),
    //                ReadEmbeddedImage(thumbnail72Address));
    //}

    //public static TileMapProvider CreateFromXyzUrlCharServer(string providerResourceKey, string mapTypeResourceKey, string url, string thumbnailAddress, string thumbnail72Address, int minServer = 0, int maxServer = 2)
    //{
    //    var mapUrl = url.Replace("{x}", "{0}").Replace("{y}", "{1}").Replace("{z}", "{2}").Replace("{@server}", "{3}");

    //    return TileMapProvider.Create(
    //                providerResourceKey,
    //                mapTypeResourceKey,
    //                tile => string.Format(System.Globalization.CultureInfo.InvariantCulture,
    //                                                                        mapUrl,
    //                                                                        tile.ColumnNumber,
    //                                                                        tile.RowNumber,
    //                                                                        tile.ZoomLevel,
    //                                                                        GetServerCharacter(minServer, maxServer)),
    //                ReadEmbeddedImage(thumbnailAddress),
    //                ReadEmbeddedImage(thumbnail72Address));
    //}


    public static List<TileMapProvider> GetAll(string? localNetworkBaseUrl = null)
    {
        var providers = new List<TileMapProvider>()
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

        if (!string.IsNullOrWhiteSpace(localNetworkBaseUrl))
        {
            foreach (var item in providers)
            {
                item.ChangeMode(TileMapAccessMode.LocalNetwork, localNetworkBaseUrl, null);
            }
        }

        return providers;
    }

    public static List<TileMapProvider> GetDefault(string? localNetworkBaseUrl = null)
    {
        var providers = new List<TileMapProvider>()
        {
            GoogleRoadMap, GoogleTerrain, GoogleSatellite, GoogleHybrid,
            BingHybrid, BingSatellite,
            OpenStreetMap, OpenTopoMap
        };

        if (!string.IsNullOrWhiteSpace(localNetworkBaseUrl))
        {
            foreach (var item in providers)
            {
                item.ChangeMode(TileMapAccessMode.LocalNetwork, localNetworkBaseUrl, null);
            }
        }

        return providers;
    }

}
