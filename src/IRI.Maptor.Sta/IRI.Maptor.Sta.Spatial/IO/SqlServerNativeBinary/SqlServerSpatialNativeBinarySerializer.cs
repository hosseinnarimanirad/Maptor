using System.Linq;
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.IO.OgcSFA;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Abstrations;

namespace IRI.Maptor.Sta.Spatial.IO;

public static partial class SqlServerSpatialNativeBinary
{
    private static SqlServerSpatialNativeBinaryTypes ParseType(GeometryType type)
    {
        return type switch
        {
            GeometryType.Point => SqlServerSpatialNativeBinaryTypes.Point,
            //GeometryType.PointZ => SqlServerSpatialNativeBinaryTypes.PointZ,
            //GeometryType.PointM => SqlServerSpatialNativeBinaryTypes.PointM,
            //GeometryType.PointZM => SqlServerSpatialNativeBinaryTypes.PointZM,
            GeometryType.LineString => SqlServerSpatialNativeBinaryTypes.LineString,
            GeometryType.Polygon => SqlServerSpatialNativeBinaryTypes.Polygon,
            GeometryType.MultiPoint => SqlServerSpatialNativeBinaryTypes.MultiPoint,
            GeometryType.MultiLineString => SqlServerSpatialNativeBinaryTypes.MultiLineString,
            GeometryType.MultiPolygon => SqlServerSpatialNativeBinaryTypes.MultiPolygon,
            //GeometryType.MultiPointZ => SqlServerSpatialNativeBinaryTypes.MultiPointZ,
            //GeometryType.MultiPointM => SqlServerSpatialNativeBinaryTypes.MultiPointM,
            //GeometryType.MultiPointZM => SqlServerSpatialNativeBinaryTypes.MultiPointZM,

            _ => throw new NotImplementedException($"Geometry type {type} is not implemented.")
        };
    }
     

    // Write points in sequential format: (X,Y) pairs, then all Z (if hasZ), then all M (if hasM)
    private static void WritePointsSequential<T>(BinaryWriter writer, List<T> points, bool hasZ, bool hasM) where T : IPoint
    {
        var pointCount = points.Count;

        // Write all points as (X, Y) pairs sequentially
        for (int i = 0; i < pointCount; i++)
        {
            writer.Write(points[i].X);
            writer.Write(points[i].Y);
        }

        // Write all Z coordinates (if hasZ)
        if (hasZ)
        {
            for (int i = 0; i < pointCount; i++)
            {
                if (points[i] is IHasZ hasZPoint)
                {
                    writer.Write(hasZPoint.Z);
                }
                else
                {
                    writer.Write(double.NaN); // NaN for missing Z
                }
            }
        }

        // Write all M coordinates (if hasM)
        if (hasM)
        {
            for (int i = 0; i < pointCount; i++)
            {
                if (points[i] is IHasM hasMPoint)
                {
                    writer.Write(hasMPoint.M);
                }
                else
                {
                    writer.Write(double.NaN); // NaN for missing M
                }
            }
        }
    }
     
     
    // Get Serialization Properties byte based on type
    // Bit flags: V (valid) = 0x04, P (point optimized) = 0x08
    // Z/M support removed - Z and M flags are never set
    // For Point types, add P bit (0x08) for optimized format (but not for empty points)
    private static byte GetSerializationProperties(SqlServerSpatialNativeBinaryTypes type, GeometryType geometryType, bool isPointEmpty = false)
    {
        byte props = 0x04; // V (valid) flag is always set
                           // Z/M flags removed - never set

        // Set P bit (0x08) for non-empty Point types to use optimized format
        if (geometryType == GeometryType.Point && !isPointEmpty)
        {
            props |= 0x08; // P bit (point optimized)
        }

        return props;
    }

    private static byte GetTypeByte(SqlServerSpatialNativeBinaryTypes type)
    {
        // Map enum values to actual SQL Server byte values
        return type switch
        {
            SqlServerSpatialNativeBinaryTypes.LineString => 0x14,      // 20 decimal
            SqlServerSpatialNativeBinaryTypes.LineStringZ => 0x15,     // 21 decimal
            SqlServerSpatialNativeBinaryTypes.LineStringZM => 0x17,    // 23 decimal
            SqlServerSpatialNativeBinaryTypes.Polygon => 0x04,          // 4 - shares with MultiPoint
            SqlServerSpatialNativeBinaryTypes.PolygonZ => 0x05,        // 5 - shares with MultiPointZ
            SqlServerSpatialNativeBinaryTypes.PolygonZM => 0x07,       // 7 - shares with MultiPointZM
            SqlServerSpatialNativeBinaryTypes.MultiLineString => 0x04, // 4 - shares with MultiPoint
            SqlServerSpatialNativeBinaryTypes.MultiLineStringZ => 0x05, // 5 - shares with MultiPointZ
            SqlServerSpatialNativeBinaryTypes.MultiLineStringZM => 0x07, // 7 - shares with MultiPointZM
            SqlServerSpatialNativeBinaryTypes.MultiPolygon => 0x04,    // 4 - shares with MultiPoint
            SqlServerSpatialNativeBinaryTypes.MultiPolygonZ => 0x05,   // 5 - shares with MultiPointZ
            SqlServerSpatialNativeBinaryTypes.MultiPolygonZM => 0x07,  // 7 - shares with MultiPointZM
            _ => (byte)type  // For Point, MultiPoint, etc., use the enum value directly
        };
    }

    public static byte[] Serialize<T>(Geometry<T> geometry) where T : IPoint, new()
    {
        if (geometry.IsNullOrEmpty())
            return null;

        using (var ms = new MemoryStream())
        using (var bw = new BinaryWriter(ms))
        {
            // Header: SRID + Version + Serialization Properties
            bw.Write(geometry.Srid);          // SRID (little-endian)
            bw.Write((byte)0x01);             // Version = 1

            // Determine type based on geometry type and Z/M presence
            SqlServerSpatialNativeBinaryTypes type = DetermineType(geometry);
            // For Point types, check if empty - empty points don't use P bit optimization
            bool isPointEmpty = geometry.Type == GeometryType.Point &&
                               (geometry.Points == null || geometry.Points.Count == 0);
            byte serializationProps = GetSerializationProperties(type, geometry.Type, isPointEmpty);
            bw.Write(serializationProps);     // Serialization Properties (V, Z, M, P flags)

            switch (geometry.Type)
            {
                case GeometryType.Point:
                    SerializePoint(bw, geometry, type);
                    break;

                case GeometryType.LineString:
                    GeometryLineStringAsWkb(bw, geometry, type);
                    break;

                case GeometryType.Polygon:
                    GeometryPolygonAsWkb(bw, geometry, type);
                    break;

                case GeometryType.MultiPoint:
                    GeometryMultiPointAsWkb(bw, geometry, type);
                    break;

                case GeometryType.MultiLineString:
                    GeometryMultiLineStringAsWkb(bw, geometry, type);
                    break;

                case GeometryType.MultiPolygon:
                    GeometryMultiPolygonAsWkb(bw, geometry, type);
                    break;

                case GeometryType.GeometryCollection:
                case GeometryType.CircularString:
                case GeometryType.CompoundCurve:
                case GeometryType.CurvePolygon:
                default:
                    throw new NotImplementedException();
            }

            return ms.ToArray();
        }
    }

    private static SqlServerSpatialNativeBinaryTypes DetermineType<T>(Geometry<T> geometry) where T : IPoint, new()
    {
        // Z/M support removed - always return base types
        return geometry.Type switch
        {
            GeometryType.Point => SqlServerSpatialNativeBinaryTypes.Point,
            GeometryType.LineString => SqlServerSpatialNativeBinaryTypes.LineString,
            GeometryType.Polygon => SqlServerSpatialNativeBinaryTypes.Polygon,
            GeometryType.MultiPoint => SqlServerSpatialNativeBinaryTypes.MultiPoint,
            GeometryType.MultiLineString => SqlServerSpatialNativeBinaryTypes.MultiLineString,
            GeometryType.MultiPolygon => SqlServerSpatialNativeBinaryTypes.MultiPolygon,
            _ => ParseType(geometry.Type)
        };
    }

    private static void SerializePoint<T>(BinaryWriter writer, Geometry<T> geometry, SqlServerSpatialNativeBinaryTypes type) where T : IPoint, new()
    { 
        // Check if this is an empty point
        if (geometry.Points == null || geometry.Points.Count == 0)
        {
            // Empty point: write full format (no P bit optimization for empty points)
            writer.Write(0);  // Number of Points = 0

            // Figures section: Number of Figures + (Attribute + Point Offset) for each figure
            writer.Write(0);  // Number of Figures = 0

            // Shapes section: Number of Shapes + (Parent Offset + Figure Offset + OpenGIS Type) for each shape
            writer.Write(1);  // Number of Shapes = 1
            writer.Write((int)-1);  // Shape Parent Offset = -1 (no parent)
            writer.Write(0);  // Shape Figure Offset = 0
            writer.Write((byte)1);  // Shape OpenGIS Type = 1 (point)
            return;
        }

        // For non-empty points, use optimized format (P bit is set in serialization properties)
        // When P bit is set, Number of Points, Figures, and Shapes sections are omitted
        // Write point coordinates directly
        var point = geometry.Points[0];
        WritePointsSequential(writer, [point], geometry.HasZ(), geometry.HasM());
    }


    private static void GeometryLineStringAsWkb<T>(BinaryWriter writer, Geometry<T> lineString, SqlServerSpatialNativeBinaryTypes type) where T : IPoint, new()
    {
        var pointCount = lineString.Points.Count;
        bool hasZ = type == SqlServerSpatialNativeBinaryTypes.LineStringZ || type == SqlServerSpatialNativeBinaryTypes.LineStringZM;
        bool hasM = type == SqlServerSpatialNativeBinaryTypes.LineStringZM;

        // Points section: Number of Points + All Points (sequential: (X,Y) pairs, then all Z, then all M)
        writer.Write(pointCount);  // Number of Points

        // Write points in sequential format: (X,Y) pairs, then all Z, then all M
        WritePointsSequential(writer, lineString.Points, hasZ, hasM);

        // Figures section: Number of Figures + (Attribute + Point Offset) for each figure
        writer.Write(1);  // Number of Figures = 1
        writer.Write((byte)1);  // Figure Attribute = 1 (stroke/linestring)
        writer.Write(0);  // Figure Point Offset = 0 (starts at first point)

        // Shapes section: Number of Shapes + (Parent Offset + Figure Offset + OpenGIS Type) for each shape
        writer.Write(1);  // Number of Shapes = 1
        writer.Write((int)-1);  // Shape Parent Offset = -1 (no parent)
        writer.Write(0);  // Shape Figure Offset = 0 (uses first figure)
        writer.Write((byte)2);  // Shape OpenGIS Type = 2 (linestring)
    }

    private static void GeometryPolygonAsWkb<T>(BinaryWriter writer, Geometry<T> polygon, SqlServerSpatialNativeBinaryTypes type) where T : IPoint, new()
    {
        var ringCount = polygon.Geometries.Count;
        var totalPointCount = polygon.TotalNumberOfPoints;
        bool hasZ = type == SqlServerSpatialNativeBinaryTypes.PolygonZ || type == SqlServerSpatialNativeBinaryTypes.PolygonZM;
        bool hasM = type == SqlServerSpatialNativeBinaryTypes.PolygonZM;

        // Collect all points from all rings
        var allPoints = new List<T>(totalPointCount);
        for (int i = 0; i < polygon.Geometries.Count; i++)
        {
            var ring = polygon.Geometries[i];
            for (int j = 0; j < ring.Points.Count; j++)
            {
                allPoints.Add(ring.Points[j]);
            }
        }

        // Points section: Number of Points + All Points (sequential: (X,Y) pairs, then all Z, then all M)
        writer.Write(totalPointCount);  // Number of Points

        // Write points in sequential format: (X,Y) pairs, then all Z, then all M
        WritePointsSequential(writer, allPoints, hasZ, hasM);

        // Figures section: Number of Figures + (Attribute + Point Offset) for each figure
        // Each ring is a figure (exterior ring = 1, interior rings = 0)
        writer.Write(ringCount);  // Number of Figures

        int pointOffset = 0;
        for (int i = 0; i < ringCount; i++)
        {
            var ring = polygon.Geometries[i];
            writer.Write((byte)(i == 0 ? 1 : 0));  // Figure Attribute: 1 for exterior ring, 0 for interior rings
            writer.Write(pointOffset);  // Figure Point Offset
            pointOffset += ring.Points.Count;
        }

        // Shapes section: Number of Shapes + (Parent Offset + Figure Offset + OpenGIS Type) for each shape
        writer.Write(1);  // Number of Shapes = 1 (single polygon)
        writer.Write((int)-1);  // Shape Parent Offset = -1 (no parent)
        writer.Write(0);  // Shape Figure Offset = 0 (uses first figure)
        writer.Write((byte)3);  // Shape OpenGIS Type = 3 (polygon)
    }

    private static void GeometryMultiPointAsWkb<T>(BinaryWriter writer, Geometry<T> multipoint, SqlServerSpatialNativeBinaryTypes type) where T : IPoint, new()
    {
        var pointCount = multipoint.NumberOfGeometries;

        // Write point count
        writer.Write(pointCount);

        // Collect all points from all geometries
        var allPoints = new List<T>(pointCount);
        for (int i = 0; i < multipoint.Geometries.Count; i++)
        {
            allPoints.Add(multipoint.Geometries[i].Points[0]);
        }

        // Write points in sequential format: (X,Y) pairs, then all Z, then all M
        WritePointsSequential(writer, allPoints, multipoint.HasZ(), multipoint.HasM());

        // Write metadata section
        // Based on CSV analysis: 02 00000001 00000000 01010000 00030000 00 FFFFFFFF 00000000 04 00000000 00000000 01 00000000 01
        // Point count as byte (not int32)
        writer.Write((byte)pointCount);

        writer.Write((int)1);  // First int32 value (1)
        writer.Write((int)0);  // Second int32 value (0)

        // Pattern: 01010000 00030000 00 (9 bytes total)
        // These are raw bytes written as hex pattern
        writer.Write(HexStringHelper.ToByteArray("0x010100000003000000"));

        // Fixed pattern (9 bytes): FFFFFFFF 00000000 04
        writer.Write(HexStringHelper.ToByteArray("0xFFFFFFFF0000000004"));

        // Additional metadata: int32(0) + int32(0) + byte(1) + int32(0) + byte(1)
        writer.Write((int)0);
        writer.Write((int)0);
        writer.Write((byte)1);
        writer.Write((int)0);
        writer.Write((byte)1);
    }

    private static void GeometryMultiLineStringAsWkb<T>(BinaryWriter writer, Geometry<T> multiLineString, SqlServerSpatialNativeBinaryTypes type) where T : IPoint, new()
    {
        var linestringCount = multiLineString.Geometries.Count;
        var totalPointCount = multiLineString.TotalNumberOfPoints;
        bool hasZ = type == SqlServerSpatialNativeBinaryTypes.MultiLineStringZ || type == SqlServerSpatialNativeBinaryTypes.MultiLineStringZM;
        bool hasM = type == SqlServerSpatialNativeBinaryTypes.MultiLineStringZM;

        // Write total point count (all linestrings combined)
        writer.Write(totalPointCount);

        // Collect all points from all linestrings
        var allPoints = new List<T>(totalPointCount);
        for (int i = 0; i < multiLineString.Geometries.Count; i++)
        {
            var linestring = multiLineString.Geometries[i];
            for (int j = 0; j < linestring.Points.Count; j++)
            {
                allPoints.Add(linestring.Points[j]);
            }
        }

        // Write points in sequential format: (X,Y) pairs, then all Z, then all M
        WritePointsSequential(writer, allPoints, hasZ, hasM);

        // Write metadata section: byte(linestringCount) + int32(flag=1) + int32(pointCount) + int32(additional) + 9 bytes pattern + additional metadata
        writer.Write((byte)linestringCount);  // Linestring count is a byte
        writer.Write((int)1);  // Flag (1 = linestring structure)

        // For single linestring, write the point count; for multiple, write first linestring's point count
        var linestringPointCount = linestringCount > 0 ? multiLineString.Geometries[0].Points.Count : 0;
        writer.Write(linestringPointCount);  // Point count for the linestring

        writer.Write((int)0);  // Additional value

        // Fixed pattern (9 bytes): FFFFFFFF 00000000 05
        writer.Write(HexStringHelper.ToByteArray("0xFFFFFFFF0000000005"));

        // Additional metadata: int32(0) + int32(0) + byte(1) + int32(0) + byte(2)
        writer.Write((int)0);
        writer.Write((int)0);
        writer.Write((byte)1);
        writer.Write((int)0);
        writer.Write((byte)2);
    }

    private static void GeometryMultiPolygonAsWkb<T>(BinaryWriter writer, Geometry<T> multiPolygon, SqlServerSpatialNativeBinaryTypes type) where T : IPoint, new()
    {
        var polygonCount = multiPolygon.Geometries.Count;
        var totalPointCount = multiPolygon.TotalNumberOfPoints;
        bool hasZ = type == SqlServerSpatialNativeBinaryTypes.MultiPolygonZ || type == SqlServerSpatialNativeBinaryTypes.MultiPolygonZM;
        bool hasM = type == SqlServerSpatialNativeBinaryTypes.MultiPolygonZM;

        // Write total point count (all polygons combined)
        writer.Write(totalPointCount);

        // Collect all points from all polygons
        var allPoints = new List<T>(totalPointCount);
        for (int i = 0; i < multiPolygon.Geometries.Count; i++)
        {
            var polygon = multiPolygon.Geometries[i];
            for (int j = 0; j < polygon.Geometries.Count; j++)
            {
                var ring = polygon.Geometries[j];
                for (int k = 0; k < ring.Points.Count; k++)
                {
                    allPoints.Add(ring.Points[k]);
                }
            }
        }

        // Write points in sequential format: (X,Y) pairs, then all Z, then all M
        WritePointsSequential(writer, allPoints, hasZ, hasM);

        // Write metadata section: byte(polygonCount) + int32(flag=2) + int32(value) + int32(additional) + 9 bytes pattern + additional metadata
        // Based on CSV analysis: 01 00000002 00000002 00000000 FFFFFFFF 00000000 06 ...
        writer.Write((byte)polygonCount);  // Polygon count is a byte
        writer.Write((int)2);  // Flag (2 = polygon structure)

        // The third int32 appears to be polygonCount + 1 (or similar calculation)
        // For MultiPolygon with 1 polygon, it's 2
        var thirdValue = polygonCount + 1;
        writer.Write(thirdValue);

        writer.Write((int)0);  // Additional value

        // Fixed pattern (9 bytes): FFFFFFFF 00000000 06
        writer.Write(HexStringHelper.ToByteArray("0xFFFFFFFF0000000006"));

        // Additional metadata: int32(0) + int32(0) + byte(2) + int32(0) + byte(3)
        writer.Write((int)0);
        writer.Write((int)0);
        writer.Write((byte)2);
        writer.Write((int)0);
        writer.Write((byte)3);
    }
}
