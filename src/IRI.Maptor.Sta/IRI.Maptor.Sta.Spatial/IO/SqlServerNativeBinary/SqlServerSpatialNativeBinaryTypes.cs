namespace IRI.Maptor.Sta.Spatial.IO.SqlServerNativeBinary;


public enum SqlServerSpatialNativeBinaryTypes : byte
{
    Point = 12,           // Serializes as byte 0x0C (12)
    PointZ = 13,          // Serializes as byte 0x0D (13)
    PointM = 14,          // Serializes as byte 0x0E (14)
    PointZM = 15,         // Serializes as byte 0x0F (15)

    MultiPoint = 4,       // Serializes as byte 0x04 (4)
    MultiPointZ = 5,       // Serializes as byte 0x05 (5)
    MultiPointM = 6,      // Serializes as byte 0x06 (6)
    MultiPointZM = 7,     // Serializes as byte 0x07 (7)

    // Note: These types share byte values with MultiPoint types above
    // They are distinguished by their metadata structure during deserialization
    LineString = 20,      // Serializes as byte 0x14 (20)
    LineStringZ = 21,     // Serializes as byte 0x15 (21)
    LineStringZM = 23,    // Serializes as byte 0x17 (23)
    
    Polygon = 24,         // Serializes as byte 0x04 (4) - shares with MultiPoint
    PolygonZ = 25,        // Serializes as byte 0x05 (5) - shares with MultiPointZ
    PolygonZM = 26,       // Serializes as byte 0x07 (7) - shares with MultiPointZM
    
    MultiLineString = 27, // Serializes as byte 0x04 (4) - shares with MultiPoint
    MultiLineStringZ = 28, // Serializes as byte 0x05 (5) - shares with MultiPointZ
    MultiLineStringZM = 29, // Serializes as byte 0x07 (7) - shares with MultiPointZM
    
    MultiPolygon = 30,    // Serializes as byte 0x04 (4) - shares with MultiPoint
    MultiPolygonZ = 31,   // Serializes as byte 0x05 (5) - shares with MultiPointZ
    MultiPolygonZM = 32,  // Serializes as byte 0x07 (7) - shares with MultiPointZM
}
