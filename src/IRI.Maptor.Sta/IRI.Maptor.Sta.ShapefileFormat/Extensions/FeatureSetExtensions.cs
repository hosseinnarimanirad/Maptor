using IRI.Maptor.Sta.ShapefileFormat;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;
using System;
using System.Collections.Generic;
using System.Text;
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.ShapefileFormat.Dbf;
using System.Linq;
using IRI.Maptor.Sta.Spatial.GeoJsonFormat;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem; 

namespace IRI.Maptor.Extensions;

public static class FeatureSetExtensions
{
    public static void SaveAsShapefile(this FeatureSet<Point> featureSet, string shpFileName, Encoding encoding, SrsBase srs, bool overwrite = false)
    {
        Shapefile.SaveAsShapefile(shpFileName, featureSet.Features, f => f.TheGeometry.AsEsriShape(f.TheGeometry.Srid), false, srs, overwrite);

        DbfFile.Write(Shapefile.GetDbfFileName(shpFileName), featureSet.Features.Select(f => f.Attributes).ToList(), encoding, overwrite);
    }

    public static void SaveAsGeoJson(this FeatureSet<Point> featureSet, string geoJsonFileName, bool isLongitudeFirst)
    {
        var srsBase = SridHelper.AsSrsBase(featureSet.Srid);

        var features = featureSet.Features.Select(f => f.AsGeoJsonFeature(p => srsBase.ToWgs84Geodetic(p), isLongitudeFirst)).ToList();

        GeoJsonFeatureSet jsonFeatureSet = new GeoJsonFeatureSet()
        {
            Features = features,
            TotalFeatures = features.Count,
        };

        jsonFeatureSet.Save(geoJsonFileName, false, true);
    }

}
