using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRI.Maptor.Core.Common.Contracts.Google;

public class GoogleDirectionsResult
{
    public Geocoded_Waypoints[] geocoded_waypoints { get; set; }
    public Route[] routes { get; set; }
    public string status { get; set; }
}

public class Geocoded_Waypoints
{
    public string geocoder_status { get; set; }
    public string place_id { get; set; }
    public string[] types { get; set; }
}

public class Route
{
    public Bounds bounds { get; set; }
    public string copyrights { get; set; }
    public Leg[] legs { get; set; }
    public Overview_Polyline overview_polyline { get; set; }
    public string summary { get; set; }
    public object[] warnings { get; set; }
    public object[] waypoint_order { get; set; }
}

public class Bounds
{
    public GoogleLocation northeast { get; set; }
    public GoogleLocation southwest { get; set; }
}

//public class Northeast
//{
//    public float lat { get; set; }
//    public float lng { get; set; }
//}

//public class Southwest
//{
//    public float lat { get; set; }
//    public float lng { get; set; }
//}

public class Overview_Polyline
{
    public string points { get; set; }
}

public class Leg
{
    public Distance distance { get; set; }
    public Duration duration { get; set; }
    public Duration duration_in_traffic { get; set; }
    public string end_address { get; set; }
    public GoogleLocation end_location { get; set; }
    public string start_address { get; set; }
    public GoogleLocation start_location { get; set; }
    public Step[] steps { get; set; }
    public object[] traffic_speed_entry { get; set; }
    public object[] via_waypoint { get; set; }
}

//public class Distance
//{
//    public string text { get; set; }
//    public int value { get; set; }
//}

//public class Duration
//{
//    public string text { get; set; }
//    public int value { get; set; }
//}

//public class End_Location
//{
//    public float lat { get; set; }
//    public float lng { get; set; }
//}

//public class Start_Location
//{
//    public float lat { get; set; }
//    public float lng { get; set; }
//}

public class Step
{
    public Distance distance { get; set; }
    public Duration duration { get; set; }
    public GoogleLocation end_location { get; set; }
    public string html_instructions { get; set; }
    public Polyline polyline { get; set; }
    public GoogleLocation start_location { get; set; }
    public string travel_mode { get; set; }
    public string maneuver { get; set; }
}

//public class Distance1
//{
//    public string text { get; set; }
//    public int value { get; set; }
//}

//public class Duration1
//{
//    public string text { get; set; }
//    public int value { get; set; }
//}

//public class End_Location1
//{
//    public float lat { get; set; }
//    public float lng { get; set; }
//}

public class Polyline
{
    public string points { get; set; }
}

//public class Start_Location1
//{
//    public float lat { get; set; }
//    public float lng { get; set; }
//}
