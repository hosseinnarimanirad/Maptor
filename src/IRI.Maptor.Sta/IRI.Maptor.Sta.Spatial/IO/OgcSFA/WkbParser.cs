using IRI.Maptor.Extensions; 
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Sta.Spatial.IO.OgcSFA;

public static class WkbParser
{
    #region Wkb To Geometry

    public static Geometry<Point>? Parse(byte[] wkb, int srid)
    {
        if (wkb.IsNullOrEmpty())
            return null;

        using (var stream = new BinaryReader(new MemoryStream(wkb)))
        {
            var byteOrder = stream.ReadByte();
            var type = (WkbGeometryType)stream.ReadInt32();

            switch (type)
            {
                case WkbGeometryType.Point:
                    return FromWkbPoint(stream, srid);

                case WkbGeometryType.LineString:
                    return FromWkbLineString(stream, srid);

                case WkbGeometryType.Polygon:
                    return FromWkbPolygon(stream, srid);

                case WkbGeometryType.MultiPoint:
                    return FromWkbMultiPoint(stream, srid);

                case WkbGeometryType.MultiLineString:
                    return FromWkbMultiLineString(stream, srid);

                case WkbGeometryType.MultiPolygon:
                    return FromWkbMultiPolygon(stream, srid);

                case WkbGeometryType.PointZ:
                case WkbGeometryType.LineStringZ:
                case WkbGeometryType.PolygonZ:
                case WkbGeometryType.MultiPointZ:
                case WkbGeometryType.MultiLineStringZ:
                case WkbGeometryType.MultiPolygonZ:

                case WkbGeometryType.PointM:
                case WkbGeometryType.LineStringM:
                case WkbGeometryType.PolygonM:
                case WkbGeometryType.MultiPointM:
                case WkbGeometryType.MultiLineStringM:
                case WkbGeometryType.MultiPolygonM:

                case WkbGeometryType.PointZM:
                case WkbGeometryType.LineStringZM:
                case WkbGeometryType.PolygonZM:
                case WkbGeometryType.MultiPointZM:
                case WkbGeometryType.MultiLineStringZM:
                case WkbGeometryType.MultiPolygonZM:
                default:
                    throw new NotImplementedException();
            }
        }

        throw new NotImplementedException();
    }

    private static Geometry<Point> FromWkbPoint(BinaryReader reader, int srid)
    {
        var x = reader.ReadDouble();
        var y = reader.ReadDouble();

        return Geometry<Point>.Create(x, y, srid);
    }

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

    private static Geometry<Point> FromWkbMultiPoint(BinaryReader reader, int srid)
    {
        var numberOfGeometries = reader.ReadInt32();

        var geometries = new List<Geometry<Point>>(numberOfGeometries);

        for (int i = 0; i < numberOfGeometries; i++)
        {
            var byteOrder = reader.ReadByte();

            var type = reader.ReadInt32();

            geometries.Add(FromWkbPoint(reader, srid));
        }

        return new Geometry<Point>(geometries, GeometryType.MultiPoint, srid);
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

    #endregion



    #region Geometry To Wkb

    internal static byte[]? AsWkb<T>(Geometry<T> geometry) where T : IPoint, new()
    {
        if (geometry.IsNullOrEmpty())
            return null;

        switch (geometry.Type)
        {
            case GeometryType.Point:
                return OgcWkbMapFunctions.ToWkbPoint(geometry.Points[0]);

            case GeometryType.LineString:
                return GeometryLineStringAsWkb(geometry);

            case GeometryType.Polygon:
                return GeometryPolygonAsWkb(geometry);

            case GeometryType.MultiPoint:
                return GeometryMultiPointAsWkb(geometry);

            case GeometryType.MultiLineString:
                return GeometryMultiLineStringAsWkb(geometry);

            case GeometryType.MultiPolygon:
                return GeometryMultiPolygonAsWkb(geometry);

            case GeometryType.GeometryCollection:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException();
        }
    }

    private static byte[] GeometryLineStringAsWkb<T>(Geometry<T> lineString) where T : IPoint, new()
    {
        List<byte> result = new List<byte>();

        result.AddRange(OgcWkbMapFunctions.ToWkbLineString(lineString.Points));

        return result.ToArray();
    }

    // todo: do not modify input polygon. consider add/remove points as a bad practice!
    private static byte[] GeometryPolygonAsWkb<T>(Geometry<T> polygon) where T : IPoint, new()
    {
        List<byte> result = new List<byte>
        {
            (byte)WkbByteOrder.WkbNdr
        };

        result.AddRange(BitConverter.GetBytes((uint)WkbGeometryType.Polygon));

        result.AddRange(BitConverter.GetBytes((uint)polygon.Geometries.Count));

        for (int i = 0; i < polygon.Geometries.Count; i++)
        {
            var points = polygon.Geometries[i].Points;
            // add first point
            points.Add(polygon.Geometries[i].Points[0]);

            result.AddRange(OgcWkbMapFunctions.ToWkbLinearRing(points));

            // to enforce idempotency
            points.RemoveAt(points.Count - 1);
        }

        return result.ToArray();
    }

    private static byte[] GeometryMultiPointAsWkb<T>(Geometry<T> multipoint) where T : IPoint, new()
    {
        List<byte> result = new List<byte>
        {
            (byte)WkbByteOrder.WkbNdr
        };

        result.AddRange(BitConverter.GetBytes((uint)WkbGeometryType.MultiPoint));

        result.AddRange(BitConverter.GetBytes((uint)multipoint.Geometries.Count));

        for (int i = 0; i < multipoint.Geometries.Count; i++)
        {
            result.AddRange(AsWkb(multipoint.Geometries[i]));
        }

        return result.ToArray();
    }

    private static byte[] GeometryMultiLineStringAsWkb<T>(Geometry<T> multiLineString) where T : IPoint, new()
    {
        List<byte> result = new List<byte>
        {
            (byte)WkbByteOrder.WkbNdr
        };

        result.AddRange(BitConverter.GetBytes((uint)WkbGeometryType.MultiLineString));

        result.AddRange(BitConverter.GetBytes((uint)multiLineString.Geometries.Count));

        for (int i = 0; i < multiLineString.Geometries.Count; i++)
        {
            result.AddRange(AsWkb(multiLineString.Geometries[i]));
        }

        return result.ToArray();
    }

    private static byte[] GeometryMultiPolygonAsWkb<T>(Geometry<T> multiPolygon) where T : IPoint, new()
    {
        List<byte> result = new List<byte>
        {
            (byte)WkbByteOrder.WkbNdr
        };

        result.AddRange(BitConverter.GetBytes((uint)WkbGeometryType.MultiPolygon));

        result.AddRange(BitConverter.GetBytes((uint)multiPolygon.Geometries.Count));

        for (int i = 0; i < multiPolygon.Geometries.Count; i++)
        {
            result.AddRange(AsWkb(multiPolygon.Geometries[i]));
        }

        return result.ToArray();
    }

    #endregion
}
