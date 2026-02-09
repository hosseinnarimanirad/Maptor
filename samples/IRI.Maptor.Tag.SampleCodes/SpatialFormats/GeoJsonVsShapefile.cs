using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Spatial.GeoJsonFormat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Tag.SampleCodes.SpatialFormats;

public static class GeoJsonVsShapefile
{
    public static void CompareGeoJsonVsShapefile(string geoJsonFileName, string outputFolder)
    {  
        // read entire GeoJson features (with attributes)
        var geoJsonGeometries = GeoJson.LoadFromFile(geoJsonFileName).Select(g => g.Geometry).ToList();

        // serilize only the GeoJson geometries (exclude attributes)
        File.WriteAllText($"{outputFolder}\\geojson.json", JsonHelper.Serialize(geoJsonGeometries));

        // convert to esri shape type
        var esriShape = geoJsonGeometries.Select(g => g.AsEsriShape()).ToList();

        // save the esri shapes as shapefile (exclude attributes)
        IRI.Maptor.Sta.ShapefileFormat.Shapefile.Save($"{outputFolder}\\shapefile.shp", esriShape);


        // RESULT:
        // file             size
        // geojson.json     6,625,971   bytes
        // shapefile.shp    5,301,068   bytes
        // shapefile.shx    4,476       bytes

        // CONCLUSION:
        // shapefile 8% more compact
    }
     
}
