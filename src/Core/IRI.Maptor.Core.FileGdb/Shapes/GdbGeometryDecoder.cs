using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.FileGdb.IO;
using IRI.Maptor.Core.FileGdb.Table;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Core.FileGdb.Shapes;

/// <summary>
/// Decodes the compressed shape buffer of a geometry field into <see cref="Geometry{Point}"/>.
/// </summary>
/// <remarks>
/// Layout (FGDB-Spec "Geometry field", cross-checked with GDAL's FileGDBOGRGeometryConverterImpl):
/// a varuint shape type, then for multi-part shapes the point count, part count, an optional curve
/// count, a bounding box and the per-part point counts, followed by the XY coordinates as signed
/// varint deltas. Z values, M values and curve descriptions all come after the XY array. Z/M are
/// not carried by <see cref="Point"/> and curves are out of scope, so decoding stops after XY: a
/// curved segment becomes the straight line between its end vertices and
/// <see cref="GdbDecodedGeometry.HasCurves"/> is set.
/// </remarks>
internal static class GdbGeometryDecoder
{
    private const uint CurveFlag = 0x20000000;

    // Shape type codes (low byte), as in ESRI's shapefile and extended shape buffer specs.
    private const int NullShape = 0;
    private const int Point = 1, PointZ = 9, PointZM = 11, PointM = 21, GeneralPoint = 52;
    private const int MultiPoint = 8, MultiPointZM = 18, MultiPointZ = 20, MultiPointM = 28, GeneralMultiPoint = 53;
    private const int Polyline = 3, PolylineZ = 10, PolylineZM = 13, PolylineM = 23, GeneralPolyline = 50;
    private const int Polygon = 5, PolygonZM = 15, PolygonZ = 19, PolygonM = 25, GeneralPolygon = 51;
    private const int MultiPatchM = 31, MultiPatch = 32, GeneralMultiPatch = 54;

    /// <summary>
    /// Decodes a geometry value as returned by <see cref="GdbRow"/> (the bytes after the row's
    /// length prefix). Returns <c>null</c> for a null shape.
    /// </summary>
    /// <exception cref="InvalidDataException">The blob is truncated or malformed.</exception>
    /// <exception cref="NotSupportedException">Multipatch shapes.</exception>
    public static GdbDecodedGeometry? Decode(byte[] blob, GdbGeometryFieldInfo field, int srid)
    {
        var reader = new GdbByteReader(blob);

        ulong rawType = reader.ReadVarUInt64();

        if (rawType > uint.MaxValue)
            throw new InvalidDataException($"Invalid shape type {rawType}.");

        uint shapeType = (uint)rawType;

        switch ((int)(shapeType & 0xFF))
        {
            case NullShape:
                return null;

            case Point:
            case PointZ:
            case PointZM:
            case PointM:
            case GeneralPoint:
                return new GdbDecodedGeometry(ReadPoint(ref reader, field, srid), false);

            case MultiPoint:
            case MultiPointZM:
            case MultiPointZ:
            case MultiPointM:
                return new GdbDecodedGeometry(ReadMultiPoint(ref reader, field, srid), false);

            case Polyline:
            case PolylineZ:
            case PolylineZM:
            case PolylineM:
            case GeneralPolyline:
                return ReadMultiPart(ref reader, field, srid, shapeType, isPolygon: false);

            case Polygon:
            case PolygonZM:
            case PolygonZ:
            case PolygonM:
            case GeneralPolygon:
                return ReadMultiPart(ref reader, field, srid, shapeType, isPolygon: true);

            case MultiPatchM:
            case MultiPatch:
            case GeneralMultiPatch:
                throw new NotSupportedException("Multipatch geometries are not supported.");

            case GeneralMultiPoint:
                // layout not documented; GDAL does not read it either
                throw new NotSupportedException("General multipoint geometries are not supported.");

            default:
                throw new InvalidDataException($"Unknown shape type {shapeType}.");
        }
    }

    private static Geometry<Point> ReadPoint(ref GdbByteReader reader, GdbGeometryFieldInfo field, int srid)
    {
        // Point coordinates are stored as (value + 1); 0 marks an empty point.
        ulong x = reader.ReadVarUInt64();
        ulong y = reader.ReadVarUInt64();

        if (x == 0 || y == 0)
            return Geometry<Point>.CreateEmpty(GeometryType.Point, srid);

        return Geometry<Point>.Create((x - 1) / field.XYScale + field.XOrigin, (y - 1) / field.XYScale + field.YOrigin, srid);
    }

    private static Geometry<Point> ReadMultiPoint(ref GdbByteReader reader, GdbGeometryFieldInfo field, int srid)
    {
        int pointCount = ReadCount(ref reader, "point count");

        if (pointCount == 0)
            return Geometry<Point>.CreateEmpty(GeometryType.MultiPoint, srid);

        SkipBoundingBox(ref reader);

        long dx = 0, dy = 0;
        var points = ReadPoints(ref reader, field, pointCount, ref dx, ref dy);

        return Geometry<Point>.Create(points, GeometryType.MultiPoint, srid);
    }

    private static GdbDecodedGeometry ReadMultiPart(ref GdbByteReader reader, GdbGeometryFieldInfo field, int srid, uint shapeType, bool isPolygon)
    {
        var emptyType = isPolygon ? GeometryType.Polygon : GeometryType.LineString;

        int pointCount = ReadCount(ref reader, "point count");

        if (pointCount == 0)
            return new GdbDecodedGeometry(Geometry<Point>.CreateEmpty(emptyType, srid), false);

        int partCount = ReadCount(ref reader, "part count");

        // Only general (extended) shape types carry a curve count, and only when flagged.
        int curveCount = (shapeType & CurveFlag) != 0 ? ReadCount(ref reader, "curve count") : 0;

        if (partCount == 0)
            return new GdbDecodedGeometry(Geometry<Point>.CreateEmpty(emptyType, srid), curveCount > 0);

        SkipBoundingBox(ref reader);

        var partSizes = ReadPartSizes(ref reader, pointCount, partCount);

        // Deltas run on across parts: they are not reset at a part boundary.
        long dx = 0, dy = 0;
        var parts = new List<List<Point>>(partCount);

        foreach (var size in partSizes)
            parts.Add(ReadPoints(ref reader, field, size, ref dx, ref dy));

        var geometry = isPolygon ? BuildPolygon(parts, srid) : BuildPolyline(parts, srid);

        return new GdbDecodedGeometry(geometry, curveCount > 0);
    }

    private static Geometry<Point> BuildPolyline(List<List<Point>> parts, int srid)
    {
        if (parts.Count == 1)
            return Geometry<Point>.Create(parts[0], GeometryType.LineString, srid);

        var lines = parts.Select(p => Geometry<Point>.Create(p, GeometryType.LineString, srid)).ToList();

        return Geometry<Point>.Create(lines, GeometryType.MultiLineString, srid);
    }

    private static Geometry<Point> BuildPolygon(List<List<Point>> parts, int srid)
    {
        // Esri rings repeat their first vertex at the end; Maptor rings are stored open.
        var rings = parts.Select(p => Geometry<Point>.CreatePolygonRing(p, srid)).ToList();

        return Geometry<Point>.CreatePolygonOrMultiPolygon(rings, srid);
    }

    private static List<Point> ReadPoints(ref GdbByteReader reader, GdbGeometryFieldInfo field, int count, ref long dx, ref long dy)
    {
        var points = new List<Point>(count);

        for (int i = 0; i < count; i++)
        {
            dx += reader.ReadVarInt64();
            dy += reader.ReadVarInt64();

            points.Add(new Point(dx / field.XYScale + field.XOrigin, dy / field.XYScale + field.YOrigin));
        }

        return points;
    }

    private static int[] ReadPartSizes(ref GdbByteReader reader, int pointCount, int partCount)
    {
        var sizes = new int[partCount];
        long sum = 0;

        // The last part's size is not stored: it is whatever remains of the total.
        for (int i = 0; i < partCount - 1; i++)
        {
            sizes[i] = ReadCount(ref reader, "part size");
            sum += sizes[i];
        }

        if (sum > pointCount)
            throw new InvalidDataException($"Part sizes ({sum} points) exceed the total of {pointCount} points.");

        sizes[partCount - 1] = (int)(pointCount - sum);

        return sizes;
    }

    /// <summary>
    /// Reads a count and checks it against the bytes left: every counted item takes at least one byte.
    /// </summary>
    private static int ReadCount(ref GdbByteReader reader, string what)
    {
        ulong count = reader.ReadVarUInt64();

        if (count > (ulong)reader.Remaining)
            throw new InvalidDataException($"Geometry {what} {count} is larger than the remaining {reader.Remaining} bytes.");

        return (int)count;
    }

    private static void SkipBoundingBox(ref GdbByteReader reader)
    {
        for (int i = 0; i < 4; i++)
            reader.ReadVarUInt64();
    }
}

/// <summary>
/// A decoded geometry and whether curved segments were replaced by straight lines.
/// </summary>
internal sealed class GdbDecodedGeometry
{
    public GdbDecodedGeometry(Geometry<Point> geometry, bool hasCurves)
    {
        Geometry = geometry;
        HasCurves = hasCurves;
    }

    public Geometry<Point> Geometry { get; }

    /// <summary>
    /// True when the shape contained true curves (arcs, Béziers, elliptic arcs); they are returned
    /// as straight segments between their end vertices.
    /// </summary>
    public bool HasCurves { get; }
}
