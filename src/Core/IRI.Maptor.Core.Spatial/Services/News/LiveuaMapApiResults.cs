using IRI.Maptor.Core.SpatialReferenceSystem.MapProjections;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Common.Enums;

namespace IRI.Maptor.Core.Spatial.Services.News;

public class LiveuaMapApiResults
{
    public int last { get; set; }
    public Venue[] venues { get; set; }
    public LiveuaMapField[] fields { get; set; }
    public int datats { get; set; }
    public string datac { get; set; }
    public string datamn { get; set; }
    public string datam { get; set; }
    public string datay { get; set; }
    public int amount { get; set; }
    public int globaltime { get; set; }
}

public class Venue
{
    public string picpath { get; set; }
    public string picpath_over { get; set; }
    public int id { get; set; }
    public string time { get; set; }
    public string name { get; set; }
    public string source { get; set; }
    public string lat { get; set; }
    public string lng { get; set; }
    public string picture { get; set; }
    public string twitpic { get; set; }
    public string lang { get; set; }
    public string video { get; set; }
    public int gi { get; set; }
    public int videotype { get; set; }
    public int timestamp { get; set; }
    public int color_id { get; set; }
    public int status_id { get; set; }
    public string description { get; set; }
    public int picw { get; set; }
    public int pich { get; set; }
    public int picwo { get; set; }
    public int picho { get; set; }
    public int picx { get; set; }
    public int picy { get; set; }
    public int picxo { get; set; }
    public int picyo { get; set; }
    public string[] ps { get; set; }
    public object[] points { get; set; }
    public string link { get; set; }
    public Image[] images { get; set; }
    public string otherregions { get; set; }
    public string location { get; set; }
    public object videocode { get; set; }
    public object target { get; set; }
    public object sel_link { get; set; }
    public object img_share { get; set; }
    public object city { get; set; }
}

public class Image
{
}

public class LiveuaMapField
{
    public string description { get; set; }
    public int type_id { get; set; }
    public string strokeweight { get; set; }
    public string strokeopacity { get; set; }
    public string strokecolor { get; set; }
    public string symbolpath { get; set; }
    public string fillcolor { get; set; }
    public string fillopacity { get; set; }
    public double[][] points { get; set; }

    public Geometry<Point> ParseToGeometry(GeometryType geometryType)
    { 
        return Geometry<Point>.ParseToGeodeticGeometry(this.points, geometryType);
    }
     
}
