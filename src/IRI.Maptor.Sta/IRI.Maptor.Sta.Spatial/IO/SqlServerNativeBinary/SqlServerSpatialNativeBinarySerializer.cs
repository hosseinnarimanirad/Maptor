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

    private static bool HasZ(SqlServerSpatialNativeBinaryTypes type)
    {
        return type switch
        {
            SqlServerSpatialNativeBinaryTypes.PointZ => true,
            SqlServerSpatialNativeBinaryTypes.PointZM => true,
            SqlServerSpatialNativeBinaryTypes.MultiPointZ => true,
            SqlServerSpatialNativeBinaryTypes.MultiPointZM => true,
            _ => false
        };
    }

    private static bool HasM(SqlServerSpatialNativeBinaryTypes type)
    {
        return type switch
        {
            SqlServerSpatialNativeBinaryTypes.PointM => true,
            SqlServerSpatialNativeBinaryTypes.PointZM => true,
            SqlServerSpatialNativeBinaryTypes.MultiPointM => true,
            SqlServerSpatialNativeBinaryTypes.MultiPointZM => true,
            _ => false
        };
    }

    // Helper methods to detect Z/M in points
    private static bool PointHasZ<T>(T point) where T : IPoint
    {
        return point is IHasZ;
    }

    private static bool PointHasM<T>(T point) where T : IPoint
    {
        return point is IHasM;
    }

    private static bool GeometryHasZ<T>(Geometry<T> geometry) where T : IPoint, new()
    {
        if (geometry.Points != null && geometry.Points.Count > 0)
        {
            return geometry.Points.Any(p => PointHasZ(p));
        }
        if (geometry.Geometries != null && geometry.Geometries.Count > 0)
        {
            return geometry.Geometries.Any(g => GeometryHasZ(g));
        }
        return false;
    }

    private static bool GeometryHasM<T>(Geometry<T> geometry) where T : IPoint, new()
    {
        if (geometry.Points != null && geometry.Points.Count > 0)
        {
            return geometry.Points.Any(p => PointHasM(p));
        }
        if (geometry.Geometries != null && geometry.Geometries.Count > 0)
        {
            return geometry.Geometries.Any(g => GeometryHasM(g));
        }
        return false;
    }

    private static bool GeometryHasAllZ<T>(Geometry<T> geometry) where T : IPoint, new()
    {
        if (geometry.Points != null && geometry.Points.Count > 0)
        {
            return geometry.Points.All(p => PointHasZ(p));
        }
        if (geometry.Geometries != null && geometry.Geometries.Count > 0)
        {
            return geometry.Geometries.All(g => GeometryHasAllZ(g));
        }
        return false;
    }

    private static bool GeometryHasAllZM<T>(Geometry<T> geometry) where T : IPoint, new()
    {
        if (geometry.Points != null && geometry.Points.Count > 0)
        {
            return geometry.Points.All(p => PointHasZ(p) && PointHasM(p));
        }
        if (geometry.Geometries != null && geometry.Geometries.Count > 0)
        {
            return geometry.Geometries.All(g => GeometryHasAllZM(g));
        }
        return false;
    }

    // Write point with Z/M support
    private static void WritePoint<T>(BinaryWriter writer, T point, bool hasZ, bool hasM) where T : IPoint
    {
        writer.Write(point.X);
        writer.Write(point.Y);
        
        if (hasZ)
        {
            if (point is IHasZ hasZPoint)
            {
                writer.Write(hasZPoint.Z);
            }
            else
            {
                writer.Write(double.NaN); // NaN for missing Z
            }
        }
        
        if (hasM)
        {
            if (point is IHasM hasMPoint)
            {
                writer.Write(hasMPoint.Measure);
            }
            else
            {
                writer.Write(double.NaN); // NaN for missing M
            }
        }
    }




    private static byte GetTypeByte(SqlServerSpatialNativeBinaryTypes type)
    {
        // Map enum values to actual SQL Server byte values
        return type switch
        {
            SqlServerSpatialNativeBinaryTypes.LineString => 4,
            SqlServerSpatialNativeBinaryTypes.Polygon => 5,
            SqlServerSpatialNativeBinaryTypes.MultiLineString => 4,
            SqlServerSpatialNativeBinaryTypes.MultiPolygon => 4,
            SqlServerSpatialNativeBinaryTypes.LineStringZM => 0x17, // 23 decimal
            SqlServerSpatialNativeBinaryTypes.PolygonZ => 5, // Same as Polygon, distinguished by metadata
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
            bw.Write(geometry.Srid);          // SRID (little-endian)
            bw.Write((byte)0x01);    // Version marker
            
            // Determine type based on geometry type and Z/M presence
            SqlServerSpatialNativeBinaryTypes type = DetermineType(geometry);
            bw.Write(GetTypeByte(type));    // Type byte

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
                    GeometryMultiLineStringAsWkb(bw, geometry);
                    break;

                case GeometryType.MultiPolygon:
                    GeometryMultiPolygonAsWkb(bw, geometry);
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
        bool hasZ = GeometryHasZ(geometry);
        bool hasM = GeometryHasM(geometry);
        bool allZ = GeometryHasAllZ(geometry);
        bool allZM = GeometryHasAllZM(geometry);

        return geometry.Type switch
        {
            GeometryType.Point =>
                (allZM) ? SqlServerSpatialNativeBinaryTypes.PointZM :
                (allZ) ? SqlServerSpatialNativeBinaryTypes.PointZ :
                (hasM) ? SqlServerSpatialNativeBinaryTypes.PointM :
                SqlServerSpatialNativeBinaryTypes.Point,

            GeometryType.LineString =>
                (allZM) ? SqlServerSpatialNativeBinaryTypes.LineStringZM :
                SqlServerSpatialNativeBinaryTypes.LineString,

            GeometryType.Polygon =>
                (allZ) ? SqlServerSpatialNativeBinaryTypes.PolygonZ :
                SqlServerSpatialNativeBinaryTypes.Polygon,

            GeometryType.MultiPoint =>
                (hasZ || hasM) ? SqlServerSpatialNativeBinaryTypes.MultiPointZM :
                SqlServerSpatialNativeBinaryTypes.MultiPoint,

            _ => ParseType(geometry.Type)
        };
    }

    private static void SerializePoint<T>(BinaryWriter writer, Geometry<T> geometry, SqlServerSpatialNativeBinaryTypes type) where T : IPoint, new()
    {
        var point = geometry.Points[0];
        bool hasZ = HasZ(type) || type == SqlServerSpatialNativeBinaryTypes.PointZM;
        bool hasM = HasM(type) || type == SqlServerSpatialNativeBinaryTypes.PointZM;
        
        WritePoint(writer, point, hasZ, hasM);
    }


    private static void GeometryLineStringAsWkb<T>(BinaryWriter writer, Geometry<T> lineString, SqlServerSpatialNativeBinaryTypes type) where T : IPoint, new()
    {
        var pointCount = lineString.Points.Count;
        bool hasZ = type == SqlServerSpatialNativeBinaryTypes.LineStringZM;
        bool hasM = type == SqlServerSpatialNativeBinaryTypes.LineStringZM;
        
        // Write point count
        writer.Write(pointCount);

        // Write all points
        for (int i = 0; i < lineString.Points.Count; i++)
        {
            WritePoint(writer, lineString.Points[i], hasZ, hasM);
        }

        // Write metadata section
        writer.Write((int)1);  // First value
        writer.Write((int)1);  // Second value
        writer.Write(HexStringHelper.ToByteArray("0xFFFFFFFF0000000002"));  // Fixed pattern (9 bytes)
    }

    private static void GeometryPolygonAsWkb<T>(BinaryWriter writer, Geometry<T> polygon, SqlServerSpatialNativeBinaryTypes type) where T : IPoint, new()
    {
        var ringCount = polygon.Geometries.Count;
        var totalPointCount = polygon.Points.Count;
        bool hasZ = type == SqlServerSpatialNativeBinaryTypes.PolygonZ;
        
        // Write total point count (all rings combined)
        writer.Write(totalPointCount);

        // Write all points from all rings
        for (int i = 0; i < polygon.Geometries.Count; i++)
        {
            var ring = polygon.Geometries[i];
            for (int j = 0; j < ring.Points.Count; j++)
            {
                WritePoint(writer, ring.Points[j], hasZ, false);
            }
        }

        // Write metadata section
        writer.Write(ringCount);  // Ring count
        writer.Write((int)2);  // Second value (always 2?)
        
        // Write ring point counts
        for (int i = 0; i < ringCount; i++)
        {
            writer.Write(polygon.Geometries[i].Points.Count);
        }
        
        // Write additional value (always 1?)
        writer.Write((int)1);
        
        // Fixed pattern (9 bytes)
        writer.Write(HexStringHelper.ToByteArray("0xFFFFFFFF0000000003"));
    }

    private static void GeometryMultiPointAsWkb<T>(BinaryWriter writer, Geometry<T> multipoint, SqlServerSpatialNativeBinaryTypes type) where T : IPoint, new()
    {
        var pointCount = multipoint.NumberOfGeometries;
        bool hasZ = type == SqlServerSpatialNativeBinaryTypes.MultiPointZM || type == SqlServerSpatialNativeBinaryTypes.MultiPointZ;
        bool hasM = type == SqlServerSpatialNativeBinaryTypes.MultiPointZM || type == SqlServerSpatialNativeBinaryTypes.MultiPointM;
        
        // For MultiPointZM (type 0x07), we need to check if any point has Z/M
        if (type == SqlServerSpatialNativeBinaryTypes.MultiPointZM)
        {
            hasZ = GeometryHasZ(multipoint);
            hasM = GeometryHasM(multipoint);
        }
        
        // Write point count
        writer.Write(pointCount);

        // Write all points (with Z/M if needed, using NaN for missing values)
        for (int i = 0; i < multipoint.Geometries.Count; i++)
        {
            WritePoint(writer, multipoint.Geometries[i].Points[0], hasZ, hasM);
        }

        // Write metadata section
        // Point count again
        writer.Write(pointCount);

        // For each point: byte flag (0x01) + int32 index
        for (int i = 0; i < pointCount; i++)
        {
            writer.Write((byte)0x01);
            writer.Write(i);
        }

        // Additional count = pointCount + 1
        writer.Write(pointCount + 1);

        // Fixed pattern (17 bytes)
        writer.Write(HexStringHelper.ToByteArray("0xFFFFFFFF0000000004000000000000000001"));

        // For points 1..N-1: int32(0) + int32(index) + byte(0x01)
        for (int i = 1; i < pointCount; i++)
        {
            writer.Write((int)0);
            writer.Write(i);
            writer.Write((byte)0x01);
        }
    }

    private static void GeometryMultiLineStringAsWkb<T>(BinaryWriter writer, Geometry<T> multiLineString) where T : IPoint, new()
    {
        var linestringCount = multiLineString.Geometries.Count;
        var totalPointCount = multiLineString.Points.Count;
        
        // Write total point count (all linestrings combined)
        writer.Write(totalPointCount);

        // Write all points from all linestrings
        for (int i = 0; i < multiLineString.Geometries.Count; i++)
        {
            var linestring = multiLineString.Geometries[i];
            for (int j = 0; j < linestring.Points.Count; j++)
            {
                writer.Write(linestring.Points[j].AsByteArray());
            }
        }

        // Write metadata section
        writer.Write(linestringCount);  // Linestring count
        writer.Write((int)1);  // Second value
        
        // Write linestring point counts
        for (int i = 0; i < linestringCount; i++)
        {
            writer.Write(multiLineString.Geometries[i].Points.Count);
        }
        
        // Fixed pattern (9 bytes)
        writer.Write(HexStringHelper.ToByteArray("0xFFFFFFFF0000000005"));
        
        // Additional metadata
        writer.Write((int)0);
        writer.Write(linestringCount);
        writer.Write((int)1);
        writer.Write((short)2);  // 2 bytes
    }

    private static void GeometryMultiPolygonAsWkb<T>(BinaryWriter writer, Geometry<T> multiPolygon) where T : IPoint, new()
    {
        var polygonCount = multiPolygon.Geometries.Count;
        var totalPointCount = multiPolygon.Points.Count;
        
        // Write total point count (all polygons combined)
        writer.Write(totalPointCount);

        // Write all points from all polygons
        for (int i = 0; i < multiPolygon.Geometries.Count; i++)
        {
            var polygon = multiPolygon.Geometries[i];
            for (int j = 0; j < polygon.Geometries.Count; j++)
            {
                var ring = polygon.Geometries[j];
                for (int k = 0; k < ring.Points.Count; k++)
                {
                    writer.Write(ring.Points[k].AsByteArray());
                }
            }
        }

        // Write metadata section
        writer.Write(polygonCount);  // Polygon count
        writer.Write((int)2);  // Second value
        
        // Write polygon ring counts and ring point counts
        for (int i = 0; i < polygonCount; i++)
        {
            var polygon = multiPolygon.Geometries[i];
            writer.Write(polygon.Geometries.Count);  // Ring count for this polygon
            for (int j = 0; j < polygon.Geometries.Count; j++)
            {
                writer.Write(polygon.Geometries[j].Points.Count);  // Point count for this ring
            }
        }
        
        // Fixed pattern (9 bytes)
        writer.Write(HexStringHelper.ToByteArray("0xFFFFFFFF0000000006"));
        
        // Additional metadata
        writer.Write((int)0);
        writer.Write(polygonCount);
        writer.Write((int)2);
        writer.Write((short)3);  // 2 bytes
    }
}
