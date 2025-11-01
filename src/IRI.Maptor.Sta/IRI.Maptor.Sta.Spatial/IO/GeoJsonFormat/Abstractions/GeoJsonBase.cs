using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem; 

namespace IRI.Maptor.Sta.Spatial.GeoJsonFormat;

public abstract class GeoJsonBase : IGeoJsonGeometry
{
    public abstract string Type { get; set; }

    public abstract GeometryType GeometryType { get; }

    public abstract bool IsNullOrEmpty();

    public abstract int NumberOfGeometries();

    public abstract int NumberOfPoints();

    public abstract Geometry<Point> Parse(bool isLongitudeFirst = true, int srid = 0);

    public string Serialize(bool indented, bool removeSpaces = false)
    {
        return GeoJson.Serialize(this, indented, removeSpaces);
    }

    public Geometry<Point> TransformToWeMercator(bool isLongitudeFirst = true)
    {
        return this.Parse(isLongitudeFirst, SridHelper.GeodeticWGS84)
                    .Transform(MapProjects.GeodeticWgs84ToWebMercator, SridHelper.WebMercator);
    }

    public GeoJsonFeature AsFeature() => GeoJson.AsFeature(this);

    public GeoJsonFeatureSet AsFeatureSet() => GeoJson.AsFeatureSet(this);

}
