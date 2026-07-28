using System;
using IRI.Maptor.Sta.Common.Abstractions;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.IO.EsriJson;

namespace IRI.Maptor.Sta.Spatial.IO.OgcSFA;

/// <summary>
/// Provides functions to convert geometry primitives to Well-Known Binary (WKB) format.
/// All methods use little-endian byte order (WkbNdr) as per OGC specification default.
/// </summary>
public static class OgcWkbMapFunctions
{
    // WKB structure size constants (in bytes)
    private const int WkbByteOrderSize = 1;
    private const int WkbGeometryTypeSize = 4;
    private const int WkbCountSize = 4;
    private const int WkbDoubleSize = 8;

    // Pre-calculated WKB sizes for point geometries
    private const int WkbPointSize = WkbByteOrderSize + WkbGeometryTypeSize + (WkbDoubleSize * 2); // 1 + 4 + 8*2 = 21
    private const int WkbPointMSize = WkbByteOrderSize + WkbGeometryTypeSize + (WkbDoubleSize * 3); // 1 + 4 + 8*3 = 29
    private const int WkbPointZMSize = WkbByteOrderSize + WkbGeometryTypeSize + (WkbDoubleSize * 4); // 1 + 4 + 8*4 = 37

    /// <summary>
    /// Normalizes a measure value, converting EsriConstants.NoDataValue to double.NaN.
    /// </summary>
    private static double NormalizeMeasure(double measure) => measure == EsriConstants.NoDataValue ? double.NaN : measure;

    /// <summary>
    /// Writes the byte order byte to the destination span.
    /// </summary>
    private static void WriteByteOrder(Span<byte> destination, WkbByteOrder byteOrder = WkbByteOrder.WkbNdr) => destination[0] = (byte)byteOrder;

    /// <summary>
    /// Writes the geometry type as a uint32 to the destination span.
    /// </summary>
    private static void WriteGeometryType(Span<byte> destination, WkbGeometryType geometryType) => BitConverter.TryWriteBytes(destination, (uint)geometryType);

    /// <summary>
    /// Writes a count value as a uint32 to the destination span.
    /// </summary>
    private static void WriteCount(Span<byte> destination, int count) => BitConverter.TryWriteBytes(destination, (uint)count);

    /// <summary>
    /// Writes a double value to the destination span.
    /// </summary>
    private static void WriteDouble(Span<byte> destination, double value) => BitConverter.TryWriteBytes(destination, value);

    /// <summary>
    /// Converts a point to WKB Point format.
    /// </summary>
    /// <typeparam name="T">The type implementing IPoint.</typeparam>
    /// <param name="point">The point to convert.</param>
    /// <returns>A byte array representing the point in WKB format.</returns>
    /// <exception cref="ArgumentNullException">Thrown when point is null.</exception>
    public static byte[] ToWkbPoint<T>(T point) where T : IPoint
    {
        if (point == null)
            throw new ArgumentNullException(nameof(point));

        byte[] result = new byte[WkbPointSize];
        Span<byte> span = result;

        WriteByteOrder(span);
        WriteGeometryType(span.Slice(WkbByteOrderSize), WkbGeometryType.Point);
        WriteDouble(span.Slice(WkbByteOrderSize + WkbGeometryTypeSize), point.X);
        WriteDouble(span.Slice(WkbByteOrderSize + WkbGeometryTypeSize + WkbDoubleSize), point.Y);

        return result;
    }

    /// <summary>
    /// Converts a point with measure to WKB PointM format.
    /// </summary>
    /// <typeparam name="T">The type implementing IPoint.</typeparam>
    /// <param name="point">The point to convert.</param>
    /// <param name="measure">The measure value. EsriConstants.NoDataValue will be converted to double.NaN.</param>
    /// <returns>A byte array representing the point in WKB PointM format.</returns>
    /// <exception cref="ArgumentNullException">Thrown when point is null.</exception>
    public static byte[] ToWkbPointM<T>(T point, double measure) where T : IPoint
    {
        if (point == null)
            throw new ArgumentNullException(nameof(point));

        byte[] result = new byte[WkbPointMSize];
        Span<byte> span = result;

        WriteByteOrder(span);
        WriteGeometryType(span.Slice(WkbByteOrderSize), WkbGeometryType.PointM);
        WriteDouble(span.Slice(WkbByteOrderSize + WkbGeometryTypeSize), point.X);
        WriteDouble(span.Slice(WkbByteOrderSize + WkbGeometryTypeSize + WkbDoubleSize), point.Y);
        WriteDouble(span.Slice(WkbByteOrderSize + WkbGeometryTypeSize + (WkbDoubleSize * 2)), NormalizeMeasure(measure));

        return result;
    }

    /// <summary>
    /// Converts a point with Z and measure to WKB PointZM format.
    /// </summary>
    /// <typeparam name="T">The type implementing IPoint.</typeparam>
    /// <param name="point">The point to convert.</param>
    /// <param name="z">The Z coordinate value.</param>
    /// <param name="measure">The measure value. EsriConstants.NoDataValue will be converted to double.NaN.</param>
    /// <returns>A byte array representing the point in WKB PointZM format.</returns>
    /// <exception cref="ArgumentNullException">Thrown when point is null.</exception>
    public static byte[] ToWkbPointZM<T>(T point, double z, double measure) where T : IPoint
    {
        if (point == null)
            throw new ArgumentNullException(nameof(point));

        byte[] result = new byte[WkbPointZMSize];
        Span<byte> span = result;

        WriteByteOrder(span);
        WriteGeometryType(span.Slice(WkbByteOrderSize), WkbGeometryType.PointZM);
        WriteDouble(span.Slice(WkbByteOrderSize + WkbGeometryTypeSize), point.X);
        WriteDouble(span.Slice(WkbByteOrderSize + WkbGeometryTypeSize + WkbDoubleSize), point.Y);
        WriteDouble(span.Slice(WkbByteOrderSize + WkbGeometryTypeSize + (WkbDoubleSize * 2)), z);
        WriteDouble(span.Slice(WkbByteOrderSize + WkbGeometryTypeSize + (WkbDoubleSize * 3)), NormalizeMeasure(measure));

        return result;
    }

    /// <summary>
    /// Converts a collection of points to WKB MultiPoint format.
    /// </summary>
    /// <typeparam name="T">The type implementing IPoint.</typeparam>
    /// <param name="points">The collection of points to convert.</param>
    /// <returns>A byte array representing the points in WKB MultiPoint format.</returns>
    /// <exception cref="ArgumentNullException">Thrown when points is null.</exception>
    /// <exception cref="ArgumentException">Thrown when points collection is empty.</exception>
    public static byte[] ToWkbMultiPoint<T>(IReadOnlyList<T> points) where T : IPoint
    {
        if (points == null)
            throw new ArgumentNullException(nameof(points));
        if (points.Count == 0)
            throw new ArgumentException("Points collection cannot be empty.", nameof(points));

        int headerSize = WkbByteOrderSize + WkbGeometryTypeSize + WkbCountSize;
        int totalSize = headerSize + (points.Count * WkbPointSize);
        byte[] result = new byte[totalSize];
        Span<byte> span = result;

        WriteByteOrder(span);
        WriteGeometryType(span.Slice(WkbByteOrderSize), WkbGeometryType.MultiPoint);
        WriteCount(span.Slice(WkbByteOrderSize + WkbGeometryTypeSize), points.Count);

        int pointOffset = headerSize;
        for (int i = 0; i < points.Count; i++)
        {
            if (points[i] == null)
                throw new ArgumentException($"Point at index {i} is null.", nameof(points));

            Span<byte> pointSpan = span.Slice(pointOffset, WkbPointSize);
            WriteByteOrder(pointSpan);
            WriteGeometryType(pointSpan.Slice(WkbByteOrderSize), WkbGeometryType.Point);
            WriteDouble(pointSpan.Slice(WkbByteOrderSize + WkbGeometryTypeSize), points[i].X);
            WriteDouble(pointSpan.Slice(WkbByteOrderSize + WkbGeometryTypeSize + WkbDoubleSize), points[i].Y);
            pointOffset += WkbPointSize;
        }

        return result;
    }

    /// <summary>
    /// Converts a collection of points with measures to WKB MultiPointM format.
    /// </summary>
    /// <typeparam name="T">The type implementing IPoint.</typeparam>
    /// <param name="points">The collection of points to convert.</param>
    /// <param name="measures">The measure values for each point. Must have the same length as points.</param>
    /// <returns>A byte array representing the points in WKB MultiPointM format.</returns>
    /// <exception cref="ArgumentNullException">Thrown when points or measures is null.</exception>
    /// <exception cref="ArgumentException">Thrown when points collection is empty or when measures length doesn't match points count.</exception>
    public static byte[] ToWkbMultiPointM<T>(IReadOnlyList<T> points, double[] measures) where T : IPoint
    {
        if (points == null)
            throw new ArgumentNullException(nameof(points));
        if (measures == null)
            throw new ArgumentNullException(nameof(measures));
        if (points.Count == 0)
            throw new ArgumentException("Points collection cannot be empty.", nameof(points));
        if (measures.Length != points.Count)
            throw new ArgumentException($"Measures array length ({measures.Length}) must match points count ({points.Count}).", nameof(measures));

        int headerSize = WkbByteOrderSize + WkbGeometryTypeSize + WkbCountSize;
        int totalSize = headerSize + (points.Count * WkbPointMSize);
        byte[] result = new byte[totalSize];
        Span<byte> span = result;

        WriteByteOrder(span);
        WriteGeometryType(span.Slice(WkbByteOrderSize), WkbGeometryType.MultiPointM);
        WriteCount(span.Slice(WkbByteOrderSize + WkbGeometryTypeSize), points.Count);

        int pointOffset = headerSize;
        for (int i = 0; i < points.Count; i++)
        {
            if (points[i] == null)
                throw new ArgumentException($"Point at index {i} is null.", nameof(points));

            Span<byte> pointSpan = span.Slice(pointOffset, WkbPointMSize);
            WriteByteOrder(pointSpan);
            WriteGeometryType(pointSpan.Slice(WkbByteOrderSize), WkbGeometryType.PointM);
            WriteDouble(pointSpan.Slice(WkbByteOrderSize + WkbGeometryTypeSize), points[i].X);
            WriteDouble(pointSpan.Slice(WkbByteOrderSize + WkbGeometryTypeSize + WkbDoubleSize), points[i].Y);
            WriteDouble(pointSpan.Slice(WkbByteOrderSize + WkbGeometryTypeSize + (WkbDoubleSize * 2)), NormalizeMeasure(measures[i]));
            pointOffset += WkbPointMSize;
        }

        return result;
    }

    /// <summary>
    /// Converts a collection of points with Z coordinates and measures to WKB MultiPointZM format.
    /// </summary>
    /// <typeparam name="T">The type implementing IPoint.</typeparam>
    /// <param name="points">The collection of points to convert.</param>
    /// <param name="zValues">The Z coordinate values for each point. Must have the same length as points.</param>
    /// <param name="measures">The measure values for each point. Must have the same length as points.</param>
    /// <returns>A byte array representing the points in WKB MultiPointZM format.</returns>
    /// <exception cref="ArgumentNullException">Thrown when points, zValues, or measures is null.</exception>
    /// <exception cref="ArgumentException">Thrown when points collection is empty or when array lengths don't match points count.</exception>
    public static byte[] ToWkbMultiPointZM<T>(IReadOnlyList<T> points, double[] zValues, double[] measures) where T : IPoint
    {
        if (points == null)
            throw new ArgumentNullException(nameof(points));
        if (zValues == null)
            throw new ArgumentNullException(nameof(zValues));
        if (measures == null)
            throw new ArgumentNullException(nameof(measures));
        if (points.Count == 0)
            throw new ArgumentException("Points collection cannot be empty.", nameof(points));
        if (zValues.Length != points.Count)
            throw new ArgumentException($"ZValues array length ({zValues.Length}) must match points count ({points.Count}).", nameof(zValues));
        if (measures.Length != points.Count)
            throw new ArgumentException($"Measures array length ({measures.Length}) must match points count ({points.Count}).", nameof(measures));

        int headerSize = WkbByteOrderSize + WkbGeometryTypeSize + WkbCountSize;
        int totalSize = headerSize + (points.Count * WkbPointZMSize);
        byte[] result = new byte[totalSize];
        Span<byte> span = result;

        WriteByteOrder(span);
        WriteGeometryType(span.Slice(WkbByteOrderSize), WkbGeometryType.MultiPointZM);
        WriteCount(span.Slice(WkbByteOrderSize + WkbGeometryTypeSize), points.Count);

        int pointOffset = headerSize;
        for (int i = 0; i < points.Count; i++)
        {
            if (points[i] == null)
                throw new ArgumentException($"Point at index {i} is null.", nameof(points));

            Span<byte> pointSpan = span.Slice(pointOffset, WkbPointZMSize);
            WriteByteOrder(pointSpan);
            WriteGeometryType(pointSpan.Slice(WkbByteOrderSize), WkbGeometryType.PointZM);
            WriteDouble(pointSpan.Slice(WkbByteOrderSize + WkbGeometryTypeSize), points[i].X);
            WriteDouble(pointSpan.Slice(WkbByteOrderSize + WkbGeometryTypeSize + WkbDoubleSize), points[i].Y);
            WriteDouble(pointSpan.Slice(WkbByteOrderSize + WkbGeometryTypeSize + (WkbDoubleSize * 2)), zValues[i]);
            WriteDouble(pointSpan.Slice(WkbByteOrderSize + WkbGeometryTypeSize + (WkbDoubleSize * 3)), NormalizeMeasure(measures[i]));
            pointOffset += WkbPointZMSize;
        }

        return result;
    }

    /// <summary>
    /// Converts a collection of points to WKB LinearRing format (used as part of Polygon geometries).
    /// Note: LinearRing does not include byte order or geometry type header.
    /// </summary>
    /// <typeparam name="T">The type implementing IPoint.</typeparam>
    /// <param name="points">The collection of points forming the linear ring.</param>
    /// <returns>A byte array representing the linear ring in WKB format.</returns>
    /// <exception cref="ArgumentNullException">Thrown when points is null.</exception>
    /// <exception cref="ArgumentException">Thrown when points collection is empty or contains null points.</exception>
    public static byte[] ToWkbLinearRing<T>(IReadOnlyList<T> points) where T : IPoint
    {
        if (points == null)
            throw new ArgumentNullException(nameof(points));
        if (points.Count == 0)
            throw new ArgumentException("Points collection cannot be empty.", nameof(points));

        int totalSize = WkbCountSize + (points.Count * WkbDoubleSize * 2);
        byte[] result = new byte[totalSize];
        Span<byte> span = result;

        WriteCount(span, points.Count);

        int offset = WkbCountSize;
        for (int i = 0; i < points.Count; i++)
        {
            if (points[i] == null)
                throw new ArgumentException($"Point at index {i} is null.", nameof(points));

            WriteDouble(span.Slice(offset), points[i].X);
            offset += WkbDoubleSize;

            WriteDouble(span.Slice(offset), points[i].Y);
            offset += WkbDoubleSize;
        }

        return result;
    }

    /// <summary>
    /// Converts a collection of points with measures to WKB LinearRingM format (used as part of PolygonM geometries).
    /// Note: LinearRingM does not include byte order or geometry type header.
    /// </summary>
    /// <typeparam name="T">The type implementing IPoint.</typeparam>
    /// <param name="points">The collection of points forming the linear ring.</param>
    /// <param name="measures">The measure values for each point. Must have the same length as points.</param>
    /// <returns>A byte array representing the linear ring in WKB LinearRingM format.</returns>
    /// <exception cref="ArgumentNullException">Thrown when points or measures is null.</exception>
    /// <exception cref="ArgumentException">Thrown when points collection is empty, contains null points, or when measures length doesn't match points count.</exception>
    public static byte[] ToWkbLinearRingM<T>(IReadOnlyList<T> points, double[] measures) where T : IPoint
    {
        if (points == null)
            throw new ArgumentNullException(nameof(points));
        if (measures == null)
            throw new ArgumentNullException(nameof(measures));
        if (points.Count == 0)
            throw new ArgumentException("Points collection cannot be empty.", nameof(points));
        if (measures.Length != points.Count)
            throw new ArgumentException($"Measures array length ({measures.Length}) must match points count ({points.Count}).", nameof(measures));

        int totalSize = WkbCountSize + (points.Count * WkbDoubleSize * 3);
        byte[] result = new byte[totalSize];
        Span<byte> span = result;

        WriteCount(span, points.Count);

        int offset = WkbCountSize;
        for (int i = 0; i < points.Count; i++)
        {
            if (points[i] == null)
                throw new ArgumentException($"Point at index {i} is null.", nameof(points));

            WriteDouble(span.Slice(offset), points[i].X);
            offset += WkbDoubleSize;

            WriteDouble(span.Slice(offset), points[i].Y);
            offset += WkbDoubleSize;

            WriteDouble(span.Slice(offset), NormalizeMeasure(measures[i]));
            offset += WkbDoubleSize;
        }

        return result;
    }

    /// <summary>
    /// Converts a collection of points with Z coordinates and measures to WKB LinearRingZM format (used as part of PolygonZM geometries).
    /// Note: LinearRingZM does not include byte order or geometry type header.
    /// </summary>
    /// <typeparam name="T">The type implementing IPoint.</typeparam>
    /// <param name="points">The collection of points forming the linear ring.</param>
    /// <param name="zValues">The Z coordinate values for each point. Must have the same length as points.</param>
    /// <param name="measures">The measure values for each point. Must have the same length as points.</param>
    /// <returns>A byte array representing the linear ring in WKB LinearRingZM format.</returns>
    /// <exception cref="ArgumentNullException">Thrown when points, zValues, or measures is null.</exception>
    /// <exception cref="ArgumentException">Thrown when points collection is empty, contains null points, or when array lengths don't match points count.</exception>
    public static byte[] ToWkbLinearRingZM<T>(IReadOnlyList<T> points, double[] zValues, double[] measures) where T : IPoint
    {
        if (points == null)
            throw new ArgumentNullException(nameof(points));
        if (zValues == null)
            throw new ArgumentNullException(nameof(zValues));
        if (measures == null)
            throw new ArgumentNullException(nameof(measures));
        if (points.Count == 0)
            throw new ArgumentException("Points collection cannot be empty.", nameof(points));
        if (zValues.Length != points.Count)
            throw new ArgumentException($"ZValues array length ({zValues.Length}) must match points count ({points.Count}).", nameof(zValues));
        if (measures.Length != points.Count)
            throw new ArgumentException($"Measures array length ({measures.Length}) must match points count ({points.Count}).", nameof(measures));

        int totalSize = WkbCountSize + (points.Count * WkbDoubleSize * 4);
        byte[] result = new byte[totalSize];
        Span<byte> span = result;

        WriteCount(span, points.Count);

        int offset = WkbCountSize;
        for (int i = 0; i < points.Count; i++)
        {
            if (points[i] == null)
                throw new ArgumentException($"Point at index {i} is null.", nameof(points));

            WriteDouble(span.Slice(offset), points[i].X);
            offset += WkbDoubleSize;

            WriteDouble(span.Slice(offset), points[i].Y);
            offset += WkbDoubleSize;

            WriteDouble(span.Slice(offset), zValues[i]);
            offset += WkbDoubleSize;

            WriteDouble(span.Slice(offset), NormalizeMeasure(measures[i]));
            offset += WkbDoubleSize;
        }

        return result;
    }

    /// <summary>
    /// Converts a collection of points to WKB LineString format.
    /// </summary>
    /// <typeparam name="T">The type implementing IPoint.</typeparam>
    /// <param name="points">The collection of points forming the line string.</param>
    /// <returns>A byte array representing the line string in WKB format.</returns>
    /// <exception cref="ArgumentNullException">Thrown when points is null.</exception>
    /// <exception cref="ArgumentException">Thrown when points collection is empty or contains null points.</exception>
    public static byte[] ToWkbLineString<T>(IReadOnlyList<T> points) where T : IPoint
    {
        if (points == null)
            throw new ArgumentNullException(nameof(points));
        if (points.Count == 0)
            throw new ArgumentException("Points collection cannot be empty.", nameof(points));

        int headerSize = WkbByteOrderSize + WkbGeometryTypeSize + WkbCountSize;
        int totalSize = headerSize + (points.Count * WkbDoubleSize * 2);
        byte[] result = new byte[totalSize];
        Span<byte> span = result;

        WriteByteOrder(span);
        WriteGeometryType(span.Slice(WkbByteOrderSize), WkbGeometryType.LineString);
        WriteCount(span.Slice(WkbByteOrderSize + WkbGeometryTypeSize), points.Count);

        int offset = headerSize;
        for (int i = 0; i < points.Count; i++)
        {
            if (points[i] == null)
                throw new ArgumentException($"Point at index {i} is null.", nameof(points));

            WriteDouble(span.Slice(offset), points[i].X);
            offset += WkbDoubleSize;

            WriteDouble(span.Slice(offset), points[i].Y);
            offset += WkbDoubleSize;
        }

        return result;
    }

    /// <summary>
    /// Converts a collection of points with measures to WKB LineStringM format.
    /// </summary>
    /// <typeparam name="T">The type implementing IPoint.</typeparam>
    /// <param name="points">The collection of points forming the line string.</param>
    /// <param name="measures">The measure values for each point. Must have the same length as points.</param>
    /// <returns>A byte array representing the line string in WKB LineStringM format.</returns>
    /// <exception cref="ArgumentNullException">Thrown when points or measures is null.</exception>
    /// <exception cref="ArgumentException">Thrown when points collection is empty, contains null points, or when measures length doesn't match points count.</exception>
    public static byte[] ToWkbLineStringM<T>(IReadOnlyList<T> points, double[] measures) where T : IPoint
    {
        if (points == null)
            throw new ArgumentNullException(nameof(points));
        if (measures == null)
            throw new ArgumentNullException(nameof(measures));
        if (points.Count == 0)
            throw new ArgumentException("Points collection cannot be empty.", nameof(points));
        if (measures.Length != points.Count)
            throw new ArgumentException($"Measures array length ({measures.Length}) must match points count ({points.Count}).", nameof(measures));

        int headerSize = WkbByteOrderSize + WkbGeometryTypeSize + WkbCountSize;
        int totalSize = headerSize + (points.Count * WkbDoubleSize * 3);
        byte[] result = new byte[totalSize];
        Span<byte> span = result;

        WriteByteOrder(span);
        WriteGeometryType(span.Slice(WkbByteOrderSize), WkbGeometryType.LineStringM);
        WriteCount(span.Slice(WkbByteOrderSize + WkbGeometryTypeSize), points.Count);

        int offset = headerSize;
        for (int i = 0; i < points.Count; i++)
        {
            if (points[i] == null)
                throw new ArgumentException($"Point at index {i} is null.", nameof(points));

            WriteDouble(span.Slice(offset), points[i].X);
            offset += WkbDoubleSize;

            WriteDouble(span.Slice(offset), points[i].Y);
            offset += WkbDoubleSize;

            WriteDouble(span.Slice(offset), NormalizeMeasure(measures[i]));
            offset += WkbDoubleSize;
        }

        return result;
    }

    /// <summary>
    /// Converts a collection of points with Z coordinates and measures to WKB LineStringZM format.
    /// </summary>
    /// <typeparam name="T">The type implementing IPoint.</typeparam>
    /// <param name="points">The collection of points forming the line string.</param>
    /// <param name="zValues">The Z coordinate values for each point. Must have the same length as points.</param>
    /// <param name="measures">The measure values for each point. Must have the same length as points.</param>
    /// <returns>A byte array representing the line string in WKB LineStringZM format.</returns>
    /// <exception cref="ArgumentNullException">Thrown when points, zValues, or measures is null.</exception>
    /// <exception cref="ArgumentException">Thrown when points collection is empty, contains null points, or when array lengths don't match points count.</exception>
    public static byte[] ToWkbLineStringZM<T>(IReadOnlyList<T> points, double[] zValues, double[] measures) where T : IPoint
    {
        if (points == null)
            throw new ArgumentNullException(nameof(points));
        if (zValues == null)
            throw new ArgumentNullException(nameof(zValues));
        if (measures == null)
            throw new ArgumentNullException(nameof(measures));
        if (points.Count == 0)
            throw new ArgumentException("Points collection cannot be empty.", nameof(points));
        if (zValues.Length != points.Count)
            throw new ArgumentException($"ZValues array length ({zValues.Length}) must match points count ({points.Count}).", nameof(zValues));
        if (measures.Length != points.Count)
            throw new ArgumentException($"Measures array length ({measures.Length}) must match points count ({points.Count}).", nameof(measures));

        int headerSize = WkbByteOrderSize + WkbGeometryTypeSize + WkbCountSize;
        int totalSize = headerSize + (points.Count * WkbDoubleSize * 4);
        byte[] result = new byte[totalSize];
        Span<byte> span = result;

        WriteByteOrder(span);
        WriteGeometryType(span.Slice(WkbByteOrderSize), WkbGeometryType.LineStringZM);
        WriteCount(span.Slice(WkbByteOrderSize + WkbGeometryTypeSize), points.Count);

        int offset = headerSize;
        for (int i = 0; i < points.Count; i++)
        {
            if (points[i] == null)
                throw new ArgumentException($"Point at index {i} is null.", nameof(points));

            WriteDouble(span.Slice(offset), points[i].X);
            offset += WkbDoubleSize;

            WriteDouble(span.Slice(offset), points[i].Y);
            offset += WkbDoubleSize;

            WriteDouble(span.Slice(offset), zValues[i]);
            offset += WkbDoubleSize;

            WriteDouble(span.Slice(offset), NormalizeMeasure(measures[i]));
            offset += WkbDoubleSize;
        }

        return result;
    }
}
