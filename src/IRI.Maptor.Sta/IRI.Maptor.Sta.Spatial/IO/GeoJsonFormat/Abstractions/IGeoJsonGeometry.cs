using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives; 
using System.Text.Json.Serialization;

namespace IRI.Maptor.Sta.Spatial.GeoJsonFormat;

//[JsonConverter(typeof(GeoJsonGeometryConverter))]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(GeoJsonPoint), "Point")]
[JsonDerivedType(typeof(GeoJsonMultiPoint), "MultiPoint")]
[JsonDerivedType(typeof(GeoJsonLineString), "LineString")]
[JsonDerivedType(typeof(GeoJsonMultiLineString), "MultiLineString")]
[JsonDerivedType(typeof(GeoJsonPolygon), "Polygon")]
[JsonDerivedType(typeof(GeoJsonMultiPolygon), "MultiPolygon")]
public interface IGeoJsonGeometry
{
    string Type { get; set; }
     
    GeometryType GeometryType { get; }

    Geometry<Point> Parse(bool isLongitudeFirst = true, int srid = 0);

    bool IsNullOrEmpty();

    int NumberOfGeometries();

    int NumberOfPoints();

    string Serialize(bool indented, bool removeSpaces = false);

    Geometry<Point> TransformToWeMercator(bool isLongitudeFirst = true);

    GeoJsonFeature AsFeature();

    GeoJsonFeatureSet AsFeatureSet();
}
