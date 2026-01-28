using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Jab.Common;

public enum BaseMapType
{
    None,

    Bing_Hybrid,
    Bing_Street,
    Bing_Satellite,

    Carto_Dark,
    Carto_Light,
     
    Google_BlackWhite,
    Google_CleanGrey,
    Google_Hybrid,
    Google_Light,
    Google_Nature,
    Google_NeutralBlue,
    Google_RoadMap,
    Google_Satellite,
    Google_Terrain,
    Google_Traffic,

    Mapbox_Comic,
    Mapbox_Satellite,

    OSM_OpenStreetMap,
    OSM_OpenTopoMap,
    OSM_MapyWinter,
    OSM_MapyTourist,
    OSM_OsmHikeBike,
    OSM_StamenWatercolor,

    Waze_Street,
}
