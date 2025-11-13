namespace IRI.Maptor.Sta.Spatial.IO;

public enum SqlServerSpatialNativeBinaryTypes : byte
{
    Point = 12,
    PointZ = 13,
    PointM = 14,
    PointZM = 15,

    MultiPoint = 4,
    MultiPointZ = 5,
    MultiPointM = 6,
    MultiPointZM = 7,

    // Note: These types share byte values with MultiPoint types above
    // They are distinguished by their metadata structure during deserialization
    LineString = 20,      // Serializes as byte 4, but needs unique enum value
    Polygon = 21,        // Serializes as byte 5, but needs unique enum value
    MultiLineString = 22, // Serializes as byte 4, but needs unique enum value
    MultiPolygon = 23,    // Serializes as byte 4, but needs unique enum value
}
