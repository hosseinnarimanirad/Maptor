using System.IO;
using System;
using IRI.Maptor.Extensions; 
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Sta.Spatial.IO.OgcSFA;

public static class WkbReader
{
    #region Wkb To Geometry

    #region Byte Order Helpers

    /// <summary>
    /// Reads an Int32 value from the BinaryReader respecting the WKB byte order.
    /// </summary>
    private static int ReadInt32(BinaryReader reader, WkbByteOrder byteOrder)
    {
        if (byteOrder == WkbByteOrder.WkbNdr)
        {
            // Little-endian (default for BinaryReader)
            return reader.ReadInt32();
        }
        else
        {
            // Big-endian - read bytes into stack-allocated span and reverse
            Span<byte> bytes = stackalloc byte[4];
            reader.Read(bytes);
            bytes.Reverse();
            return BitConverter.ToInt32(bytes);
        }
    }

    /// <summary>
    /// Reads a Double value from the BinaryReader respecting the WKB byte order.
    /// </summary>
    private static double ReadDouble(BinaryReader reader, WkbByteOrder byteOrder)
    {
        if (byteOrder == WkbByteOrder.WkbNdr)
        {
            // Little-endian (default for BinaryReader)
            return reader.ReadDouble();
        }
        else
        {
            // Big-endian - read bytes into stack-allocated span and reverse
            Span<byte> bytes = stackalloc byte[8];
            reader.Read(bytes);
            bytes.Reverse();
            return BitConverter.ToDouble(bytes);
        }
    }

    #endregion

    #region LineString/Ring Reading Helper

    /// <summary>
    /// Reads a linestring or ring from the BinaryReader.
    /// For rings, the last point (which repeats the first point) is skipped.
    /// </summary>
    private static List<T> ReadLineStringOrRing<T>(BinaryReader reader, WkbByteOrder byteOrder, bool isRing, bool hasZ, bool hasM) where T : IPoint, new()
    {
        var numberOfPoints = ReadInt32(reader, byteOrder);

        var result = new List<T>(numberOfPoints);

        for (int i = 0; i < numberOfPoints; i++)
        {
            var x = ReadDouble(reader, byteOrder);
            var y = ReadDouble(reader, byteOrder);

            var point = new T { X = x, Y = y };

            // Optimize: Use single pattern match per branch based on known hasZ/hasM combination
            // This eliminates multiple pattern matching checks per point
            if (hasZ && hasM)
            {
                // PointZM: X, Y, Z, M
                var z = ReadDouble(reader, byteOrder);
                var m = ReadDouble(reader, byteOrder);
                if (point is PointZM pointZM)
                {
                    pointZM.Z = z;
                    pointZM.M = m;
                }
            }
            else if (hasZ)
            {
                // PointZ: X, Y, Z
                var z = ReadDouble(reader, byteOrder);
                if (point is PointZ pointZ)
                {
                    pointZ.Z = z;
                }
            }
            else if (hasM)
            {
                // PointM: X, Y, M
                var m = ReadDouble(reader, byteOrder);
                if (point is PointM pointM)
                {
                    pointM.M = m;
                }
            }
            // else: Point (2D) - no additional coordinates to read

            // last point is repeated in rings, so skip it
            if (isRing && i == numberOfPoints - 1)
                break;

            result.Add(point);
        }

        return result;
    }

    #endregion

    #region Main Parse Method

    public static IGeometry? Parse(byte[] wkb, int srid)
    {
        if (wkb.IsNullOrEmpty())
            return null;

        using (var stream = new BinaryReader(new MemoryStream(wkb)))
        {
            var byteOrderByte = stream.ReadByte();
            var byteOrder = (WkbByteOrder)byteOrderByte;
            var type = (WkbGeometryType)ReadInt32(stream, byteOrder);

            switch (type)
            {
                // 2D geometries
                case WkbGeometryType.Point:
                    return FromWkbPoint(stream, byteOrder, srid);

                case WkbGeometryType.LineString:
                    return FromWkbLineString(stream, byteOrder, srid);

                case WkbGeometryType.Polygon:
                    return FromWkbPolygon(stream, byteOrder, srid);

                case WkbGeometryType.MultiPoint:
                    return FromWkbMultiPoint(stream, byteOrder, srid);

                case WkbGeometryType.MultiLineString:
                    return FromWkbMultiLineString(stream, byteOrder, srid);

                case WkbGeometryType.MultiPolygon:
                    return FromWkbMultiPolygon(stream, byteOrder, srid);

                // Z variants
                case WkbGeometryType.PointZ:
                    return FromWkbPointZ(stream, byteOrder, srid);

                case WkbGeometryType.LineStringZ:
                    return FromWkbLineStringZ(stream, byteOrder, srid);

                case WkbGeometryType.PolygonZ:
                    return FromWkbPolygonZ(stream, byteOrder, srid);

                case WkbGeometryType.MultiPointZ:
                    return FromWkbMultiPointZ(stream, byteOrder, srid);

                case WkbGeometryType.MultiLineStringZ:
                    return FromWkbMultiLineStringZ(stream, byteOrder, srid);

                case WkbGeometryType.MultiPolygonZ:
                    return FromWkbMultiPolygonZ(stream, byteOrder, srid);

                // M variants
                case WkbGeometryType.PointM:
                    return FromWkbPointM(stream, byteOrder, srid);

                case WkbGeometryType.LineStringM:
                    return FromWkbLineStringM(stream, byteOrder, srid);

                case WkbGeometryType.PolygonM:
                    return FromWkbPolygonM(stream, byteOrder, srid);

                case WkbGeometryType.MultiPointM:
                    return FromWkbMultiPointM(stream, byteOrder, srid);

                case WkbGeometryType.MultiLineStringM:
                    return FromWkbMultiLineStringM(stream, byteOrder, srid);

                case WkbGeometryType.MultiPolygonM:
                    return FromWkbMultiPolygonM(stream, byteOrder, srid);

                // ZM variants
                case WkbGeometryType.PointZM:
                    return FromWkbPointZM(stream, byteOrder, srid);

                case WkbGeometryType.LineStringZM:
                    return FromWkbLineStringZM(stream, byteOrder, srid);

                case WkbGeometryType.PolygonZM:
                    return FromWkbPolygonZM(stream, byteOrder, srid);

                case WkbGeometryType.MultiPointZM:
                    return FromWkbMultiPointZM(stream, byteOrder, srid);

                case WkbGeometryType.MultiLineStringZM:
                    return FromWkbMultiLineStringZM(stream, byteOrder, srid);

                case WkbGeometryType.MultiPolygonZM:
                    return FromWkbMultiPolygonZM(stream, byteOrder, srid);

                default:
                    throw new NotImplementedException($"WkbReader > Parse > unsupported type: {type.ToString()}");
            }
        }
    }

    #endregion

    #region 2D Geometries

    private static Geometry<Point> FromWkbPoint(BinaryReader reader, WkbByteOrder byteOrder, int srid)
    {
        var x = ReadDouble(reader, byteOrder);
        var y = ReadDouble(reader, byteOrder);

        return Geometry<Point>.Create(x, y, srid);
    }

    private static Geometry<Point> FromWkbLineString(BinaryReader reader, WkbByteOrder byteOrder, int srid)
    {
        var points = ReadLineStringOrRing<Point>(reader, byteOrder, isRing: false, hasZ: false, hasM: false);

        return Geometry<Point>.CreatePointOrLineString(points, srid);
    }

    private static Geometry<Point> FromWkbPolygon(BinaryReader reader, WkbByteOrder byteOrder, int srid)
    {
        var numberOfRings = ReadInt32(reader, byteOrder);

        var geometries = new List<Geometry<Point>>();

        for (int i = 0; i < numberOfRings; i++)
        {
            var ring = ReadLineStringOrRing<Point>(reader, byteOrder, isRing: true, hasZ: false, hasM: false);

            geometries.Add(Geometry<Point>.CreatePointOrLineString(ring, srid));
        }

        return Geometry<Point>.CreatePolygonOrMultiPolygon(geometries, srid);
    }

    private static Geometry<Point> FromWkbMultiPoint(BinaryReader reader, WkbByteOrder byteOrder, int srid)
    {
        var numberOfGeometries = ReadInt32(reader, byteOrder);

        var geometries = new List<Geometry<Point>>(numberOfGeometries);

        for (int i = 0; i < numberOfGeometries; i++)
        {
            var childByteOrder = (WkbByteOrder)reader.ReadByte();
            var type = (WkbGeometryType)ReadInt32(reader, childByteOrder);

            if (type == WkbGeometryType.Point)
            {
                geometries.Add(FromWkbPoint(reader, childByteOrder, srid));
            }
            else
            {
                throw new InvalidDataException($"Expected Point in MultiPoint, but found {type}");
            }
        }

        return new Geometry<Point>(geometries, GeometryType.MultiPoint, srid);
    }

    private static Geometry<Point> FromWkbMultiLineString(BinaryReader reader, WkbByteOrder byteOrder, int srid)
    {
        var numberOfGeometries = ReadInt32(reader, byteOrder);

        var geometries = new List<Geometry<Point>>(numberOfGeometries);

        for (int i = 0; i < numberOfGeometries; i++)
        {
            var childByteOrder = (WkbByteOrder)reader.ReadByte();
            var type = (WkbGeometryType)ReadInt32(reader, childByteOrder);

            if (type == WkbGeometryType.LineString)
            {
                geometries.Add(FromWkbLineString(reader, childByteOrder, srid));
            }
            else
            {
                throw new InvalidDataException($"Expected LineString in MultiLineString, but found {type}");
            }
        }

        return new Geometry<Point>(geometries, GeometryType.MultiLineString, srid);
    }

    private static Geometry<Point> FromWkbMultiPolygon(BinaryReader reader, WkbByteOrder byteOrder, int srid)
    {
        var numberOfGeometries = ReadInt32(reader, byteOrder);

        var geometries = new List<Geometry<Point>>(numberOfGeometries);

        for (int i = 0; i < numberOfGeometries; i++)
        {
            var childByteOrder = (WkbByteOrder)reader.ReadByte();
            var type = (WkbGeometryType)ReadInt32(reader, childByteOrder);

            if (type == WkbGeometryType.Polygon)
            {
                geometries.Add(FromWkbPolygon(reader, childByteOrder, srid));
            }
            else
            {
                throw new InvalidDataException($"Expected Polygon in MultiPolygon, but found {type}");
            }
        }

        return new Geometry<Point>(geometries, GeometryType.MultiPolygon, srid);
    }

    #endregion

    #region Z Variants

    private static Geometry<PointZ> FromWkbPointZ(BinaryReader reader, WkbByteOrder byteOrder, int srid)
    {
        var x = ReadDouble(reader, byteOrder);
        var y = ReadDouble(reader, byteOrder);
        var z = ReadDouble(reader, byteOrder);

        var point = new PointZ { X = x, Y = y, Z = z };
        return Geometry<PointZ>.Create(new List<PointZ> { point }, GeometryType.Point, srid);
    }

    private static Geometry<PointZ> FromWkbLineStringZ(BinaryReader reader, WkbByteOrder byteOrder, int srid)
    {
        var points = ReadLineStringOrRing<PointZ>(reader, byteOrder, isRing: false, hasZ: true, hasM: false);

        return Geometry<PointZ>.CreatePointOrLineString(points, srid);
    }

    private static Geometry<PointZ> FromWkbPolygonZ(BinaryReader reader, WkbByteOrder byteOrder, int srid)
    {
        var numberOfRings = ReadInt32(reader, byteOrder);

        var geometries = new List<Geometry<PointZ>>();

        for (int i = 0; i < numberOfRings; i++)
        {
            var ring = ReadLineStringOrRing<PointZ>(reader, byteOrder, isRing: true, hasZ: true, hasM: false);

            geometries.Add(Geometry<PointZ>.CreatePointOrLineString(ring, srid));
        }

        return Geometry<PointZ>.CreatePolygonOrMultiPolygon(geometries, srid);
    }

    private static Geometry<PointZ> FromWkbMultiPointZ(BinaryReader reader, WkbByteOrder byteOrder, int srid)
    {
        var numberOfGeometries = ReadInt32(reader, byteOrder);

        var geometries = new List<Geometry<PointZ>>(numberOfGeometries);

        for (int i = 0; i < numberOfGeometries; i++)
        {
            var childByteOrder = (WkbByteOrder)reader.ReadByte();
            var type = (WkbGeometryType)ReadInt32(reader, childByteOrder);

            if (type == WkbGeometryType.PointZ)
            {
                geometries.Add(FromWkbPointZ(reader, childByteOrder, srid));
            }
            else
            {
                throw new InvalidDataException($"Expected PointZ in MultiPointZ, but found {type}");
            }
        }

        return new Geometry<PointZ>(geometries, GeometryType.MultiPoint, srid);
    }

    private static Geometry<PointZ> FromWkbMultiLineStringZ(BinaryReader reader, WkbByteOrder byteOrder, int srid)
    {
        var numberOfGeometries = ReadInt32(reader, byteOrder);

        var geometries = new List<Geometry<PointZ>>(numberOfGeometries);

        for (int i = 0; i < numberOfGeometries; i++)
        {
            var childByteOrder = (WkbByteOrder)reader.ReadByte();
            var type = (WkbGeometryType)ReadInt32(reader, childByteOrder);

            if (type == WkbGeometryType.LineStringZ)
            {
                geometries.Add(FromWkbLineStringZ(reader, childByteOrder, srid));
            }
            else
            {
                throw new InvalidDataException($"Expected LineStringZ in MultiLineStringZ, but found {type}");
            }
        }

        return new Geometry<PointZ>(geometries, GeometryType.MultiLineString, srid);
    }

    private static Geometry<PointZ> FromWkbMultiPolygonZ(BinaryReader reader, WkbByteOrder byteOrder, int srid)
    {
        var numberOfGeometries = ReadInt32(reader, byteOrder);

        var geometries = new List<Geometry<PointZ>>(numberOfGeometries);

        for (int i = 0; i < numberOfGeometries; i++)
        {
            var childByteOrder = (WkbByteOrder)reader.ReadByte();
            var type = (WkbGeometryType)ReadInt32(reader, childByteOrder);

            if (type == WkbGeometryType.PolygonZ)
            {
                geometries.Add(FromWkbPolygonZ(reader, childByteOrder, srid));
            }
            else
            {
                throw new InvalidDataException($"Expected PolygonZ in MultiPolygonZ, but found {type}");
            }
        }

        return new Geometry<PointZ>(geometries, GeometryType.MultiPolygon, srid);
    }

    #endregion

    #region M Variants

    private static Geometry<PointM> FromWkbPointM(BinaryReader reader, WkbByteOrder byteOrder, int srid)
    {
        var x = ReadDouble(reader, byteOrder);
        var y = ReadDouble(reader, byteOrder);
        var m = ReadDouble(reader, byteOrder);

        var point = new PointM { X = x, Y = y, M = m };
        return Geometry<PointM>.Create(new List<PointM> { point }, GeometryType.Point, srid);
    }

    private static Geometry<PointM> FromWkbLineStringM(BinaryReader reader, WkbByteOrder byteOrder, int srid)
    {
        var points = ReadLineStringOrRing<PointM>(reader, byteOrder, isRing: false, hasZ: false, hasM: true);

        return Geometry<PointM>.CreatePointOrLineString(points, srid);
    }

    private static Geometry<PointM> FromWkbPolygonM(BinaryReader reader, WkbByteOrder byteOrder, int srid)
    {
        var numberOfRings = ReadInt32(reader, byteOrder);

        var geometries = new List<Geometry<PointM>>();

        for (int i = 0; i < numberOfRings; i++)
        {
            var ring = ReadLineStringOrRing<PointM>(reader, byteOrder, isRing: true, hasZ: false, hasM: true);

            geometries.Add(Geometry<PointM>.CreatePointOrLineString(ring, srid));
        }

        return Geometry<PointM>.CreatePolygonOrMultiPolygon(geometries, srid);
    }

    private static Geometry<PointM> FromWkbMultiPointM(BinaryReader reader, WkbByteOrder byteOrder, int srid)
    {
        var numberOfGeometries = ReadInt32(reader, byteOrder);

        var geometries = new List<Geometry<PointM>>(numberOfGeometries);

        for (int i = 0; i < numberOfGeometries; i++)
        {
            var childByteOrder = (WkbByteOrder)reader.ReadByte();
            var type = (WkbGeometryType)ReadInt32(reader, childByteOrder);

            if (type == WkbGeometryType.PointM)
            {
                geometries.Add(FromWkbPointM(reader, childByteOrder, srid));
            }
            else
            {
                throw new InvalidDataException($"Expected PointM in MultiPointM, but found {type}");
            }
        }

        return new Geometry<PointM>(geometries, GeometryType.MultiPoint, srid);
    }

    private static Geometry<PointM> FromWkbMultiLineStringM(BinaryReader reader, WkbByteOrder byteOrder, int srid)
    {
        var numberOfGeometries = ReadInt32(reader, byteOrder);

        var geometries = new List<Geometry<PointM>>(numberOfGeometries);

        for (int i = 0; i < numberOfGeometries; i++)
        {
            var childByteOrder = (WkbByteOrder)reader.ReadByte();
            var type = (WkbGeometryType)ReadInt32(reader, childByteOrder);

            if (type == WkbGeometryType.LineStringM)
            {
                geometries.Add(FromWkbLineStringM(reader, childByteOrder, srid));
            }
            else
            {
                throw new InvalidDataException($"Expected LineStringM in MultiLineStringM, but found {type}");
            }
        }

        return new Geometry<PointM>(geometries, GeometryType.MultiLineString, srid);
    }

    private static Geometry<PointM> FromWkbMultiPolygonM(BinaryReader reader, WkbByteOrder byteOrder, int srid)
    {
        var numberOfGeometries = ReadInt32(reader, byteOrder);

        var geometries = new List<Geometry<PointM>>(numberOfGeometries);

        for (int i = 0; i < numberOfGeometries; i++)
        {
            var childByteOrder = (WkbByteOrder)reader.ReadByte();
            var type = (WkbGeometryType)ReadInt32(reader, childByteOrder);

            if (type == WkbGeometryType.PolygonM)
            {
                geometries.Add(FromWkbPolygonM(reader, childByteOrder, srid));
            }
            else
            {
                throw new InvalidDataException($"Expected PolygonM in MultiPolygonM, but found {type}");
            }
        }

        return new Geometry<PointM>(geometries, GeometryType.MultiPolygon, srid);
    }

    #endregion

    #region ZM Variants

    private static Geometry<PointZM> FromWkbPointZM(BinaryReader reader, WkbByteOrder byteOrder, int srid)
    {
        var x = ReadDouble(reader, byteOrder);
        var y = ReadDouble(reader, byteOrder);
        var z = ReadDouble(reader, byteOrder);
        var m = ReadDouble(reader, byteOrder);

        var point = new PointZM { X = x, Y = y, Z = z, M = m };
        return Geometry<PointZM>.Create(new List<PointZM> { point }, GeometryType.Point, srid);
    }

    private static Geometry<PointZM> FromWkbLineStringZM(BinaryReader reader, WkbByteOrder byteOrder, int srid)
    {
        var points = ReadLineStringOrRing<PointZM>(reader, byteOrder, isRing: false, hasZ: true, hasM: true);

        return Geometry<PointZM>.CreatePointOrLineString(points, srid);
    }

    private static Geometry<PointZM> FromWkbPolygonZM(BinaryReader reader, WkbByteOrder byteOrder, int srid)
    {
        var numberOfRings = ReadInt32(reader, byteOrder);

        var geometries = new List<Geometry<PointZM>>();

        for (int i = 0; i < numberOfRings; i++)
        {
            var ring = ReadLineStringOrRing<PointZM>(reader, byteOrder, isRing: true, hasZ: true, hasM: true);

            geometries.Add(Geometry<PointZM>.CreatePointOrLineString(ring, srid));
        }

        return Geometry<PointZM>.CreatePolygonOrMultiPolygon(geometries, srid);
    }

    private static Geometry<PointZM> FromWkbMultiPointZM(BinaryReader reader, WkbByteOrder byteOrder, int srid)
    {
        var numberOfGeometries = ReadInt32(reader, byteOrder);

        var geometries = new List<Geometry<PointZM>>(numberOfGeometries);

        for (int i = 0; i < numberOfGeometries; i++)
        {
            var childByteOrder = (WkbByteOrder)reader.ReadByte();
            var type = (WkbGeometryType)ReadInt32(reader, childByteOrder);

            if (type == WkbGeometryType.PointZM)
            {
                geometries.Add(FromWkbPointZM(reader, childByteOrder, srid));
            }
            else
            {
                throw new InvalidDataException($"Expected PointZM in MultiPointZM, but found {type}");
            }
        }

        return new Geometry<PointZM>(geometries, GeometryType.MultiPoint, srid);
    }

    private static Geometry<PointZM> FromWkbMultiLineStringZM(BinaryReader reader, WkbByteOrder byteOrder, int srid)
    {
        var numberOfGeometries = ReadInt32(reader, byteOrder);

        var geometries = new List<Geometry<PointZM>>(numberOfGeometries);

        for (int i = 0; i < numberOfGeometries; i++)
        {
            var childByteOrder = (WkbByteOrder)reader.ReadByte();
            var type = (WkbGeometryType)ReadInt32(reader, childByteOrder);

            if (type == WkbGeometryType.LineStringZM)
            {
                geometries.Add(FromWkbLineStringZM(reader, childByteOrder, srid));
            }
            else
            {
                throw new InvalidDataException($"Expected LineStringZM in MultiLineStringZM, but found {type}");
            }
        }

        return new Geometry<PointZM>(geometries, GeometryType.MultiLineString, srid);
    }

    private static Geometry<PointZM> FromWkbMultiPolygonZM(BinaryReader reader, WkbByteOrder byteOrder, int srid)
    {
        var numberOfGeometries = ReadInt32(reader, byteOrder);

        var geometries = new List<Geometry<PointZM>>(numberOfGeometries);

        for (int i = 0; i < numberOfGeometries; i++)
        {
            var childByteOrder = (WkbByteOrder)reader.ReadByte();
            var type = (WkbGeometryType)ReadInt32(reader, childByteOrder);

            if (type == WkbGeometryType.PolygonZM)
            {
                geometries.Add(FromWkbPolygonZM(reader, childByteOrder, srid));
            }
            else
            {
                throw new InvalidDataException($"Expected PolygonZM in MultiPolygonZM, but found {type}");
            }
        }

        return new Geometry<PointZM>(geometries, GeometryType.MultiPolygon, srid);
    }

    #endregion

    #endregion
}

