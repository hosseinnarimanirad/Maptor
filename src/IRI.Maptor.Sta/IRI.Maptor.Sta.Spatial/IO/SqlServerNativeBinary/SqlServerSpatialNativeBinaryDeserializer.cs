using System;
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Primitives;

namespace IRI.Maptor.Sta.Spatial.IO;

public static partial class SqlServerSpatialNativeBinary
{


    private static SqlServerSpatialNativeBinaryTypes DetermineGeometryType(byte typeByte, BinaryReader reader)
    {
        // Type byte 4 can be MultiPoint, LineString, MultiLineString, or MultiPolygon
        // Type byte 5 can be MultiPointZ or Polygon
        // We need to peek at the structure to determine the actual type
        
        if (typeByte == 4)
        {
            // Peek at the point count and metadata structure
            var position = reader.BaseStream.Position;
            var pointCount = reader.ReadInt32();
            
            // Read ahead to check metadata structure
            // Skip all points (pointCount * 16 bytes for X, Y)
            reader.BaseStream.Position = position + 4 + (pointCount * 16);
            
            // Read metadata to determine type
            var firstMetadataValue = reader.ReadInt32();
            var secondMetadataValue = reader.ReadInt32();
            
            // Reset position
            reader.BaseStream.Position = position;
            
            // MultiPoint has specific metadata pattern: pointCount, then flags+indices
            // LineString metadata: 1, 1, then pattern
            // MultiLineString metadata: linestringCount, 1, then point counts
            // MultiPolygon metadata: polygonCount, 2, then ring counts
            
            if (firstMetadataValue == pointCount)
            {
                // This looks like MultiPoint (point count repeated)
                return SqlServerSpatialNativeBinaryTypes.MultiPoint;
            }
            else if (firstMetadataValue == 1 && secondMetadataValue == 1)
            {
                // LineString pattern
                return SqlServerSpatialNativeBinaryTypes.LineString;
            }
            else if (secondMetadataValue == 1)
            {
                // MultiLineString pattern
                return SqlServerSpatialNativeBinaryTypes.MultiLineString;
            }
            else if (secondMetadataValue == 2)
            {
                // MultiPolygon pattern
                return SqlServerSpatialNativeBinaryTypes.MultiPolygon;
            }
            
            // Default to MultiPoint if uncertain
            return SqlServerSpatialNativeBinaryTypes.MultiPoint;
        }
        else if (typeByte == 5)
        {
            // Peek at structure to distinguish Polygon/PolygonZ from MultiPointZ
            var position = reader.BaseStream.Position;
            var pointCount = reader.ReadInt32();
            
            // Check if points have Z (24 bytes) or just X,Y (16 bytes)
            // Skip first point to check size
            reader.BaseStream.Position = position + 4;
            var x = reader.ReadDouble();
            var y = reader.ReadDouble();
            var nextValue = reader.ReadDouble();
            
            // Check if this is Z value (reasonable range) or metadata
            bool hasZ = !double.IsNaN(nextValue) && Math.Abs(nextValue) < 1e100;
            
            // Skip points (adjust size based on Z presence)
            int pointSize = hasZ ? 24 : 16;
            reader.BaseStream.Position = position + 4 + (pointCount * pointSize);
            
            var firstMetadataValue = reader.ReadInt32();
            var secondMetadataValue = reader.ReadInt32();
            
            reader.BaseStream.Position = position;
            
            // Polygon has metadata: ringCount, 2, then ring point counts
            // MultiPointZ would have pointCount repeated like MultiPoint
            if (firstMetadataValue == pointCount)
            {
                return SqlServerSpatialNativeBinaryTypes.MultiPointZ;
            }
            else
            {
                return hasZ ? SqlServerSpatialNativeBinaryTypes.PolygonZ : SqlServerSpatialNativeBinaryTypes.Polygon;
            }
        }
        else if (typeByte == 0x17) // LineStringZM
        {
            return SqlServerSpatialNativeBinaryTypes.LineStringZM;
        }
        
        // For other type bytes, cast directly
        return (SqlServerSpatialNativeBinaryTypes)typeByte;
    }

    public static Geometry<Point> Deserialize(byte[] nativeBinary)
    {
        if (nativeBinary.IsNullOrEmpty())
            return null;

        using (var stream = new BinaryReader(new MemoryStream(nativeBinary)))
        {
            var srid = stream.ReadInt32();

            var version = stream.ReadByte();

            var typeByte = stream.ReadByte();
            var type = DetermineGeometryType(typeByte, stream);

            switch (type)
            {
                case SqlServerSpatialNativeBinaryTypes.Point:
                    return ParsePoint(stream, srid);

                case SqlServerSpatialNativeBinaryTypes.PointZ:
                    return ParsePointZ(stream, srid);

                case SqlServerSpatialNativeBinaryTypes.PointM:
                    return ParsePointM(stream, srid);

                case SqlServerSpatialNativeBinaryTypes.PointZM:
                    return ParsePointZM(stream, srid);

                case SqlServerSpatialNativeBinaryTypes.LineString:
                    return ParseLineString(stream, srid);

                case SqlServerSpatialNativeBinaryTypes.Polygon:
                    return ParsePolygon(stream, srid);

                case SqlServerSpatialNativeBinaryTypes.MultiPoint:
                    return ParseMultiPoint(stream, srid);

                case SqlServerSpatialNativeBinaryTypes.MultiPointZ:
                    return ParseMultiPointZ(stream, srid);

                case SqlServerSpatialNativeBinaryTypes.MultiPointM:
                    return ParseMultiPointM(stream, srid);
                    
                case SqlServerSpatialNativeBinaryTypes.MultiPointZM:
                    return ParseMultiPointZM(stream, srid);

                case SqlServerSpatialNativeBinaryTypes.MultiLineString:
                    return ParseMultiLineString(stream, srid);

                case SqlServerSpatialNativeBinaryTypes.MultiPolygon:
                    return ParseMultiPolygon(stream, srid);

                case SqlServerSpatialNativeBinaryTypes.LineStringZM:
                    return ParseLineStringZM(stream, srid);

                case SqlServerSpatialNativeBinaryTypes.PolygonZ:
                    return ParsePolygonZ(stream, srid);
                    
                default:
                    break;
            }

            //switch (type)
            //{
            //    case WkbGeometryType.Point:
            //        return FromWkbPoint(stream);

            //    case WkbGeometryType.LineString:
            //        return FromWkbLineString(stream);

            //    case WkbGeometryType.Polygon:
            //        return FromWkbPolygon(stream);

            //    case WkbGeometryType.MultiPoint:
            //        return FromWkbMultiPoint(stream);

            //    case WkbGeometryType.MultiLineString:
            //        return FromWkbMultiLineString(stream);

            //    case WkbGeometryType.MultiPolygon:
            //        return FromWkbMultiPolygon(stream);

            //    case WkbGeometryType.PointZ:
            //    case WkbGeometryType.LineStringZ:
            //    case WkbGeometryType.PolygonZ:
            //    case WkbGeometryType.MultiPointZ:
            //    case WkbGeometryType.MultiLineStringZ:
            //    case WkbGeometryType.MultiPolygonZ:

            //    case WkbGeometryType.PointM:
            //    case WkbGeometryType.LineStringM:
            //    case WkbGeometryType.PolygonM:
            //    case WkbGeometryType.MultiPointM:
            //    case WkbGeometryType.MultiLineStringM:
            //    case WkbGeometryType.MultiPolygonM:

            //    case WkbGeometryType.PointZM:
            //    case WkbGeometryType.LineStringZM:
            //    case WkbGeometryType.PolygonZM:
            //    case WkbGeometryType.MultiPointZM:
            //    case WkbGeometryType.MultiLineStringZM:
            //    case WkbGeometryType.MultiPolygonZM:
            //    default:
            //        throw new NotImplementedException();
            //}
        }

        throw new NotImplementedException();
    }

    private static Geometry<Point> ParsePoint(BinaryReader reader, int srid)
    {
        var x = reader.ReadDouble();
        var y = reader.ReadDouble();

        return Geometry<Point>.Create(x, y, srid);
    }

    private static Geometry<Point> ParsePointM(BinaryReader reader, int srid)
    {
        var x = reader.ReadDouble();
        var y = reader.ReadDouble();
        var m = reader.ReadDouble();

        return Geometry<Point>.Create(x, y, /*m, */srid);
    }

    private static Geometry<Point> ParsePointZ(BinaryReader reader, int srid)
    {
        var x = reader.ReadDouble();
        var y = reader.ReadDouble();
        var z = reader.ReadDouble();

        return Geometry<Point>.Create(x, y, /*z, */srid);
    }

    private static Geometry<Point> ParsePointZM(BinaryReader reader, int srid)
    {
        var x = reader.ReadDouble();
        var y = reader.ReadDouble();
        var z = reader.ReadDouble();
        var m = reader.ReadDouble();

        return Geometry<Point>.Create(x, y, /*z, */srid);
    }



    private static Geometry<Point> ParseLineString(BinaryReader reader, int srid)
    {
        var pointCount = reader.ReadInt32();
        var points = new List<Point>(pointCount);

        // Read all points
        for (int i = 0; i < pointCount; i++)
        {
            var x = reader.ReadDouble();
            var y = reader.ReadDouble();
            points.Add(new Point(x, y));
        }

        // Skip metadata: int32(1) + int32(1) + 9 bytes pattern = 17 bytes
        reader.ReadInt32(); // First value
        reader.ReadInt32(); // Second value
        reader.ReadBytes(9); // Fixed pattern

        return Geometry<Point>.CreatePointOrLineString(points, srid);
    }

    private static Geometry<Point> ParsePolygon(BinaryReader reader, int srid)
    {
        var totalPointCount = reader.ReadInt32();
        
        // Read all points first
        var allPoints = new List<Point>(totalPointCount);
        for (int i = 0; i < totalPointCount; i++)
        {
            var x = reader.ReadDouble();
            var y = reader.ReadDouble();
            allPoints.Add(new Point(x, y));
        }

        // Read metadata to determine ring structure
        var ringCount = reader.ReadInt32();
        reader.ReadInt32(); // Skip second value
        
        // Read ring point counts
        var ringPointCounts = new List<int>(ringCount);
        for (int i = 0; i < ringCount; i++)
        {
            ringPointCounts.Add(reader.ReadInt32());
        }
        
        reader.ReadInt32(); // Skip additional value
        reader.ReadBytes(9); // Skip fixed pattern

        // Split points into rings
        var rings = new List<Geometry<Point>>(ringCount);
        int pointIndex = 0;
        for (int i = 0; i < ringCount; i++)
        {
            var ringPoints = new List<Point>(ringPointCounts[i]);
            for (int j = 0; j < ringPointCounts[i]; j++)
            {
                ringPoints.Add(allPoints[pointIndex++]);
            }
            rings.Add(Geometry<Point>.CreatePointOrLineString(ringPoints, srid));
        }

        return Geometry<Point>.CreatePolygonOrMultiPolygon(rings, srid);
    }

    private static Geometry<Point> ParseLineStringZM(BinaryReader reader, int srid)
    {
        var pointCount = reader.ReadInt32();
        var points = new List<Point>(pointCount);

        // Read all points (X, Y, Z, M)
        for (int i = 0; i < pointCount; i++)
        {
            var x = reader.ReadDouble();
            var y = reader.ReadDouble();
            var z = reader.ReadDouble();
            var m = reader.ReadDouble();
            // Note: Point doesn't support Z/M, so we only store X, Y
            points.Add(new Point(x, y));
        }

        // Skip metadata: int32(1) + int32(1) + 9 bytes pattern = 17 bytes
        reader.ReadInt32(); // First value
        reader.ReadInt32(); // Second value
        reader.ReadBytes(9); // Fixed pattern

        return Geometry<Point>.CreatePointOrLineString(points, srid);
    }

    private static Geometry<Point> ParsePolygonZ(BinaryReader reader, int srid)
    {
        var totalPointCount = reader.ReadInt32();
        
        // Read all points first (X, Y, Z)
        var allPoints = new List<Point>(totalPointCount);
        for (int i = 0; i < totalPointCount; i++)
        {
            var x = reader.ReadDouble();
            var y = reader.ReadDouble();
            var z = reader.ReadDouble(); // Read but don't store Z
            allPoints.Add(new Point(x, y));
        }

        // Read metadata to determine ring structure
        var ringCount = reader.ReadInt32();
        reader.ReadInt32(); // Skip second value
        
        // Read ring point counts
        var ringPointCounts = new List<int>(ringCount);
        for (int i = 0; i < ringCount; i++)
        {
            ringPointCounts.Add(reader.ReadInt32());
        }
        
        reader.ReadInt32(); // Skip additional value
        reader.ReadBytes(9); // Skip fixed pattern

        // Split points into rings
        var rings = new List<Geometry<Point>>(ringCount);
        int pointIndex = 0;
        for (int i = 0; i < ringCount; i++)
        {
            var ringPoints = new List<Point>(ringPointCounts[i]);
            for (int j = 0; j < ringPointCounts[i]; j++)
            {
                ringPoints.Add(allPoints[pointIndex++]);
            }
            rings.Add(Geometry<Point>.CreatePointOrLineString(ringPoints, srid));
        }

        return Geometry<Point>.CreatePolygonOrMultiPolygon(rings, srid);
    }

    private static Geometry<Point> ParseMultiLineString(BinaryReader reader, int srid)
    {
        var totalPointCount = reader.ReadInt32();
        
        // Read all points first
        var allPoints = new List<Point>(totalPointCount);
        for (int i = 0; i < totalPointCount; i++)
        {
            var x = reader.ReadDouble();
            var y = reader.ReadDouble();
            allPoints.Add(new Point(x, y));
        }

        // Read metadata to determine linestring structure
        var linestringCount = reader.ReadInt32();
        reader.ReadInt32(); // Skip second value
        
        // Read linestring point counts
        var linestringPointCounts = new List<int>(linestringCount);
        for (int i = 0; i < linestringCount; i++)
        {
            linestringPointCounts.Add(reader.ReadInt32());
        }
        
        reader.ReadBytes(9); // Skip fixed pattern
        reader.ReadInt32(); // Skip additional value
        reader.ReadInt32(); // Skip additional value
        reader.ReadInt32(); // Skip additional value
        reader.ReadInt16(); // Skip short value

        // Split points into linestrings
        var linestrings = new List<Geometry<Point>>(linestringCount);
        int pointIndex = 0;
        for (int i = 0; i < linestringCount; i++)
        {
            var linestringPoints = new List<Point>(linestringPointCounts[i]);
            for (int j = 0; j < linestringPointCounts[i]; j++)
            {
                linestringPoints.Add(allPoints[pointIndex++]);
            }
            linestrings.Add(Geometry<Point>.CreatePointOrLineString(linestringPoints, srid));
        }

        return new Geometry<Point>(linestrings, GeometryType.MultiLineString, srid);
    }

    private static Geometry<Point> ParseMultiPolygon(BinaryReader reader, int srid)
    {
        var totalPointCount = reader.ReadInt32();
        
        // Read all points first
        var allPoints = new List<Point>(totalPointCount);
        for (int i = 0; i < totalPointCount; i++)
        {
            var x = reader.ReadDouble();
            var y = reader.ReadDouble();
            allPoints.Add(new Point(x, y));
        }

        // Read metadata to determine polygon structure
        var polygonCount = reader.ReadInt32();
        reader.ReadInt32(); // Skip second value
        
        // Read polygon ring counts and ring point counts
        var polygonRingCounts = new List<int>(polygonCount);
        var ringPointCounts = new List<List<int>>(polygonCount);
        
        for (int i = 0; i < polygonCount; i++)
        {
            var ringCount = reader.ReadInt32();
            polygonRingCounts.Add(ringCount);
            var ringCounts = new List<int>(ringCount);
            for (int j = 0; j < ringCount; j++)
            {
                ringCounts.Add(reader.ReadInt32());
            }
            ringPointCounts.Add(ringCounts);
        }
        
        reader.ReadBytes(9); // Skip fixed pattern
        reader.ReadInt32(); // Skip additional value
        reader.ReadInt32(); // Skip additional value
        reader.ReadInt32(); // Skip additional value
        reader.ReadInt16(); // Skip short value

        // Split points into polygons
        var polygons = new List<Geometry<Point>>(polygonCount);
        int pointIndex = 0;
        for (int i = 0; i < polygonCount; i++)
        {
            var rings = new List<Geometry<Point>>(polygonRingCounts[i]);
            for (int j = 0; j < polygonRingCounts[i]; j++)
            {
                var ringPoints = new List<Point>(ringPointCounts[i][j]);
                for (int k = 0; k < ringPointCounts[i][j]; k++)
                {
                    ringPoints.Add(allPoints[pointIndex++]);
                }
                rings.Add(Geometry<Point>.CreatePointOrLineString(ringPoints, srid));
            }
            polygons.Add(Geometry<Point>.CreatePolygonOrMultiPolygon(rings, srid));
        }

        return new Geometry<Point>(polygons, GeometryType.MultiPolygon, srid);
    }

    private static Geometry<Point> ParseMultiPoint(BinaryReader reader, int srid, Func<BinaryReader, int, Geometry<Point>> parsePoint)
    {
        var numberOfGeometries = reader.ReadInt32();

        var geometries = new List<Geometry<Point>>(numberOfGeometries);

        for (int i = 0; i < numberOfGeometries; i++)
        {
            geometries.Add(parsePoint(reader, srid));
        }

        // Skip metadata section
        var pointCount = reader.ReadInt32(); // Point count again
        
        // Skip point metadata: for each point, byte flag + int32 index
        for (int i = 0; i < pointCount; i++)
        {
            reader.ReadByte(); // Flag
            reader.ReadInt32(); // Index
        }
        
        reader.ReadInt32(); // Additional count (pointCount + 1)
        reader.ReadBytes(17); // Fixed pattern
        
        // Skip additional metadata for points 1..N-1
        for (int i = 1; i < pointCount; i++)
        {
            reader.ReadInt32(); // Zero
            reader.ReadInt32(); // Index
            reader.ReadByte(); // Flag
        }

        return new Geometry<Point>(geometries, GeometryType.MultiPoint, srid);
    }


    private static Geometry<Point> ParseMultiPoint(BinaryReader reader, int srid) => ParseMultiPoint(reader, srid, ParsePoint);

    private static Geometry<Point> ParseMultiPointM(BinaryReader reader, int srid) => ParseMultiPoint(reader, srid, ParsePointM);
    private static Geometry<Point> ParseMultiPointZ(BinaryReader reader, int srid) => ParseMultiPoint(reader, srid, ParsePointZ);
    private static Geometry<Point> ParseMultiPointZM(BinaryReader reader, int srid) => ParseMultiPoint(reader, srid, ParsePointZM);


    private static Geometry<Point> FromWkbLineString(BinaryReader reader, int srid)
    {
        var points = ReadLineStringOrRing(reader, isRing: false);

        return Geometry<Point>.CreatePointOrLineString(points, srid);
    }

    private static Geometry<Point> FromWkbPolygon(BinaryReader reader, int srid)
    {
        var numberOfRings = reader.ReadInt32();

        var geometries = new List<Geometry<Point>>();

        for (int i = 0; i < numberOfRings; i++)
        {
            var ring = ReadLineStringOrRing(reader, isRing: true);

            geometries.Add(Geometry<Point>.CreatePointOrLineString(ring, srid));
        }

        return Geometry<Point>.CreatePolygonOrMultiPolygon(geometries, srid);
    }

    private static Geometry<Point> FromWkbMultiLineString(BinaryReader reader, int srid)
    {
        var numberOfGeometries = reader.ReadInt32();

        var geometries = new List<Geometry<Point>>(numberOfGeometries);

        for (int i = 0; i < numberOfGeometries; i++)
        {
            var byteOrder = reader.ReadByte();

            var type = reader.ReadInt32();

            geometries.Add(FromWkbLineString(reader, srid));
        }

        return new Geometry<Point>(geometries, GeometryType.MultiLineString, srid);
    }

    private static Geometry<Point> FromWkbMultiPolygon(BinaryReader reader, int srid)
    {
        var numberOfGeometries = reader.ReadInt32();

        var geometries = new List<Geometry<Point>>(numberOfGeometries);

        for (int i = 0; i < numberOfGeometries; i++)
        {
            var byteOrder = reader.ReadByte();

            var type = reader.ReadInt32();

            geometries.Add(FromWkbPolygon(reader, srid));
        }

        return new Geometry<Point>(geometries, GeometryType.MultiPolygon, srid);
    }


    private static List<Point> ReadLineStringOrRing(BinaryReader reader, bool isRing)
    {
        var numberOfPoints = reader.ReadInt32();

        var result = new List<Point>(numberOfPoints);

        for (int i = 0; i < numberOfPoints; i++)
        {
            var x = reader.ReadDouble();

            var y = reader.ReadDouble();

            // last point is repeated in rings
            if (isRing && i == numberOfPoints - 1)
                break;

            result.Add(new Point(x, y));
        }

        return result;
    }



}
