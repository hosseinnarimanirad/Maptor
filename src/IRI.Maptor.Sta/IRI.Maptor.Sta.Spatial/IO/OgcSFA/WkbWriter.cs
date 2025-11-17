using System;
using System.Collections.Generic;
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Sta.Spatial.IO.OgcSFA;

public static class WkbWriter
{
    #region Constants

    private const int WkbByteOrderSize = 1;
    private const int WkbGeometryTypeSize = 4;
    private const int WkbCountSize = 4;
    private const int WkbDoubleSize = 8;
    private const int WkbHeaderSize = WkbByteOrderSize + WkbGeometryTypeSize + WkbCountSize; // 9 bytes
    private const int WkbPointZSize = WkbByteOrderSize + WkbGeometryTypeSize + (WkbDoubleSize * 3); // 29 bytes

    #endregion

    #region Geometry To Wkb

    #region Helper Methods

    /// <summary>
    /// Extracts Z values from a geometry by casting to typed geometry.
    /// Returns null if geometry doesn't have Z coordinates.
    /// </summary>
    private static double[]? ExtractZValues(IGeometry geometry)
    {
        if (geometry == null || !geometry.HasZ())
            return null;

        // Cast geometry to typed version to avoid pattern matching on each point
        return geometry switch
        {
            Geometry<PointZ> g => ExtractZFromPoints(g.Points),
            Geometry<PointZM> g => ExtractZFromPoints(g.Points),
            _ => null
        };
    }

    /// <summary>
    /// Extracts Z values from a typed point list (PointZ or PointZM).
    /// </summary>
    private static double[] ExtractZFromPoints<T>(IReadOnlyList<T> points) where T : IPoint, IHasZ
    {
        var zValues = new double[points.Count];
        for (int i = 0; i < points.Count; i++)
        {
            zValues[i] = points[i].Z;
        }
        return zValues;
    }

    /// <summary>
    /// Extracts M values from a geometry by casting to typed geometry.
    /// Returns null if geometry doesn't have M coordinates.
    /// </summary>
    private static double[]? ExtractMValues(IGeometry geometry)
    {
        if (geometry == null || !geometry.HasM())
            return null;

        // Cast geometry to typed version to avoid pattern matching on each point
        return geometry switch
        {
            Geometry<PointM> g => ExtractMFromPoints(g.Points),
            Geometry<PointZM> g => ExtractMFromPoints(g.Points),
            _ => null
        };
    }

    /// <summary>
    /// Extracts M values from a typed point list (PointM or PointZM).
    /// </summary>
    private static double[] ExtractMFromPoints<T>(IReadOnlyList<T> points) where T : IPoint, IHasM
    {
        var mValues = new double[points.Count];
        for (int i = 0; i < points.Count; i++)
        {
            mValues[i] = points[i].M;
        }
        return mValues;
    }

    /// <summary>
    /// Calculates the size of a linear ring in WKB format.
    /// </summary>
    private static int CalculateRingSize(int pointCount, bool hasZ, bool hasM)
    {
        // Account for ring closure: Geometry<T> doesn't repeat last point, but WKB requires it
        int actualPointCount = pointCount + 1; // +1 for closure point
        
        int coordinatesPerPoint = 2; // X, Y
        if (hasZ) coordinatesPerPoint++;
        if (hasM) coordinatesPerPoint++;
        
        return WkbCountSize + (actualPointCount * coordinatesPerPoint * WkbDoubleSize);
    }

    /// <summary>
    /// Creates a closed ring point list by appending the first point.
    /// </summary>
    private static List<T> CreateClosedRing<T>(IReadOnlyList<T> points) where T : IPoint, new()
    {
        var ringPoints = new List<T>(points);
        ringPoints.Add(points[0]); // Close the ring
        return ringPoints;
    }

    /// <summary>
    /// Writes a linear ring to the WKB buffer.
    /// </summary>
    private static int WriteLinearRing<T>(Span<byte> buffer, int offset, IReadOnlyList<T> points, bool hasZ, bool hasM) where T : IPoint, new()
    {
        var ringPoints = CreateClosedRing(points);
        int pointCount = ringPoints.Count;

        // Write point count
        BitConverter.TryWriteBytes(buffer.Slice(offset), (uint)pointCount);
        offset += WkbCountSize;

        // Extract Z/M values if needed
        double[]? zValues = null;
        double[]? mValues = null;

        if (hasZ)
        {
            if (ringPoints[0] is IHasZ)
            {
                zValues = new double[pointCount];
                for (int i = 0; i < pointCount; i++)
                {
                    if (ringPoints[i] is IHasZ hasZPoint)
                        zValues[i] = hasZPoint.Z;
                }
            }
        }

        if (hasM)
        {
            if (ringPoints[0] is IHasM)
            {
                mValues = new double[pointCount];
                for (int i = 0; i < pointCount; i++)
                {
                    if (ringPoints[i] is IHasM hasMPoint)
                        mValues[i] = hasMPoint.M;
                }
            }
        }

        // Write coordinates
        for (int i = 0; i < pointCount; i++)
        {
            BitConverter.TryWriteBytes(buffer.Slice(offset), ringPoints[i].X);
            offset += WkbDoubleSize;
            BitConverter.TryWriteBytes(buffer.Slice(offset), ringPoints[i].Y);
            offset += WkbDoubleSize;

            if (hasZ && zValues != null)
            {
                BitConverter.TryWriteBytes(buffer.Slice(offset), zValues[i]);
                offset += WkbDoubleSize;
            }

            if (hasM && mValues != null)
            {
                BitConverter.TryWriteBytes(buffer.Slice(offset), mValues[i]);
                offset += WkbDoubleSize;
            }
        }

        return offset;
    }

    /// <summary>
    /// Generic helper for Multi-geometry types (MultiPoint, MultiLineString, MultiPolygon).
    /// </summary>
    private static byte[] GeometryMultiAsWkb<T>(IGeometry geometry, WkbGeometryType wkbType) where T : IPoint, new()
    {
        if (geometry is not Geometry<T> typedGeometry)
            throw new ArgumentException($"Geometry must be of type Geometry<{typeof(T).Name}>", nameof(geometry));

        var geometryCount = typedGeometry.Geometries.Count;

        // Pre-calculate all child WKB sizes in single pass
        var childWkbs = new byte[geometryCount][];
        var totalSize = WkbHeaderSize;
        
        for (int i = 0; i < geometryCount; i++)
        {
            var childWkb = AsWkb(typedGeometry.Geometries[i]);
            if (childWkb != null)
            {
                childWkbs[i] = childWkb;
                totalSize += childWkb.Length;
            }
        }

        var result = new byte[totalSize];
        var span = new Span<byte>(result);

        // Write header
        span[0] = (byte)WkbByteOrder.WkbNdr;
        BitConverter.TryWriteBytes(span.Slice(WkbByteOrderSize), (uint)wkbType);
        BitConverter.TryWriteBytes(span.Slice(WkbByteOrderSize + WkbGeometryTypeSize), (uint)geometryCount);

        // Write child geometries using pre-calculated WKB arrays
        int offset = WkbHeaderSize;
        for (int i = 0; i < geometryCount; i++)
        {
            if (childWkbs[i] != null)
            {
                childWkbs[i].CopyTo(span.Slice(offset));
                offset += childWkbs[i].Length;
            }
        }

        return result;
    }

    #endregion

    #region Main AsWkb Method

    internal static byte[]? AsWkb(IGeometry geometry)
    {
        if (geometry is null)
            return null;

        var hasZ = geometry.HasZ();
        var hasM = geometry.HasM();

        return geometry.Type switch
        {
            GeometryType.Point => hasZ && hasM ? GeometryPointZMAsWkb(geometry)
                              : hasZ ? GeometryPointZAsWkb(geometry)
                              : hasM ? GeometryPointMAsWkb(geometry)
                              : GeometryPointAsWkb(geometry),

            GeometryType.LineString => hasZ && hasM ? GeometryLineStringZMAsWkb(geometry)
                                    : hasZ ? GeometryLineStringZAsWkb(geometry)
                                    : hasM ? GeometryLineStringMAsWkb(geometry)
                                    : GeometryLineStringAsWkb(geometry),

            GeometryType.Polygon => hasZ && hasM ? GeometryPolygonZMAsWkb(geometry)
                                 : hasZ ? GeometryPolygonZAsWkb(geometry)
                                 : hasM ? GeometryPolygonMAsWkb(geometry)
                                 : GeometryPolygonAsWkb(geometry),

            GeometryType.MultiPoint => hasZ && hasM ? GeometryMultiAsWkb<PointZM>(geometry, WkbGeometryType.MultiPointZM)
                                      : hasZ ? GeometryMultiAsWkb<PointZ>(geometry, WkbGeometryType.MultiPointZ)
                                      : hasM ? GeometryMultiAsWkb<PointM>(geometry, WkbGeometryType.MultiPointM)
                                      : GeometryMultiAsWkb<Point>(geometry, WkbGeometryType.MultiPoint),

            GeometryType.MultiLineString => hasZ && hasM ? GeometryMultiAsWkb<PointZM>(geometry, WkbGeometryType.MultiLineStringZM)
                                          : hasZ ? GeometryMultiAsWkb<PointZ>(geometry, WkbGeometryType.MultiLineStringZ)
                                          : hasM ? GeometryMultiAsWkb<PointM>(geometry, WkbGeometryType.MultiLineStringM)
                                          : GeometryMultiAsWkb<Point>(geometry, WkbGeometryType.MultiLineString),

            GeometryType.MultiPolygon => hasZ && hasM ? GeometryMultiAsWkb<PointZM>(geometry, WkbGeometryType.MultiPolygonZM)
                                        : hasZ ? GeometryMultiAsWkb<PointZ>(geometry, WkbGeometryType.MultiPolygonZ)
                                        : hasM ? GeometryMultiAsWkb<PointM>(geometry, WkbGeometryType.MultiPolygonM)
                                        : GeometryMultiAsWkb<Point>(geometry, WkbGeometryType.MultiPolygon),

            GeometryType.GeometryCollection
            or GeometryType.CircularString
            or GeometryType.CompoundCurve
            or GeometryType.CurvePolygon
            or _ => throw new NotImplementedException($"WKB conversion not supported for geometry type: {geometry.Type}")
        };
    }

    #endregion

    #region 2D Geometries

    private static byte[] GeometryPointAsWkb(IGeometry geometry)
    {
        if (geometry is not Geometry<Point> typedGeometry)
            throw new ArgumentException("Geometry must be Geometry<Point>", nameof(geometry));

        return OgcWkbMapFunctions.ToWkbPoint(typedGeometry.Points[0]);
    }

    private static byte[] GeometryLineStringAsWkb(IGeometry geometry)
    {
        if (geometry is not Geometry<Point> typedGeometry)
            throw new ArgumentException("Geometry must be Geometry<Point>", nameof(geometry));

        return OgcWkbMapFunctions.ToWkbLineString(typedGeometry.Points);
    }

    private static byte[] GeometryPolygonAsWkb(IGeometry geometry)
    {
        if (geometry is not Geometry<Point> typedGeometry)
            throw new ArgumentException("Geometry must be Geometry<Point>", nameof(geometry));

        var ringCount = typedGeometry.Geometries.Count;
        var headerSize = WkbByteOrderSize + WkbGeometryTypeSize + WkbCountSize;

        // Calculate total size: header + sum of all ring sizes (accounting for closure point)
        var totalSize = headerSize;
        foreach (var ring in typedGeometry.Geometries)
        {
            totalSize += CalculateRingSize(ring.Points.Count, hasZ: false, hasM: false);
        }

        var result = new byte[totalSize];
        var span = new Span<byte>(result);

        // Write header
        span[0] = (byte)WkbByteOrder.WkbNdr;
        BitConverter.TryWriteBytes(span.Slice(WkbByteOrderSize), (uint)WkbGeometryType.Polygon);
        BitConverter.TryWriteBytes(span.Slice(WkbByteOrderSize + WkbGeometryTypeSize), (uint)ringCount);

        // Write rings
        int offset = headerSize;
        foreach (var ring in typedGeometry.Geometries)
        {
            offset = WriteLinearRing(span, offset, ring.Points, hasZ: false, hasM: false);
        }

        return result;
    }

    #endregion

    #region Z Variants

    private static byte[] GeometryPointZAsWkb(IGeometry geometry)
    {
        if (geometry is Geometry<PointZ> typedGeometry)
        {
            var point = typedGeometry.Points[0];
            var result = new byte[WkbPointZSize];
            var span = new Span<byte>(result);

            span[0] = (byte)WkbByteOrder.WkbNdr;
            BitConverter.TryWriteBytes(span.Slice(WkbByteOrderSize), (uint)WkbGeometryType.PointZ);
            BitConverter.TryWriteBytes(span.Slice(WkbByteOrderSize + WkbGeometryTypeSize), point.X);
            BitConverter.TryWriteBytes(span.Slice(WkbByteOrderSize + WkbGeometryTypeSize + WkbDoubleSize), point.Y);
            BitConverter.TryWriteBytes(span.Slice(WkbByteOrderSize + WkbGeometryTypeSize + (WkbDoubleSize * 2)), point.Z);

            return result;
        }

        return GeometryPointAsWkb(geometry);
    }

    private static byte[] GeometryLineStringZAsWkb(IGeometry geometry)
    {
        if (geometry is Geometry<PointZ> typedGeometry)
        {
            var zValues = ExtractZFromPoints(typedGeometry.Points);
            
            // Create Z-only LineString manually to avoid NaN array allocation
            var headerSize = WkbByteOrderSize + WkbGeometryTypeSize + WkbCountSize;
            var pointCount = typedGeometry.Points.Count;
            var totalSize = headerSize + (pointCount * 3 * WkbDoubleSize); // X, Y, Z

            var result = new byte[totalSize];
            var span = new Span<byte>(result);

            span[0] = (byte)WkbByteOrder.WkbNdr;
            BitConverter.TryWriteBytes(span.Slice(WkbByteOrderSize), (uint)WkbGeometryType.LineStringZ);
            BitConverter.TryWriteBytes(span.Slice(WkbByteOrderSize + WkbGeometryTypeSize), (uint)pointCount);

            int offset = headerSize;
            for (int i = 0; i < pointCount; i++)
            {
                BitConverter.TryWriteBytes(span.Slice(offset), typedGeometry.Points[i].X);
                offset += WkbDoubleSize;
                BitConverter.TryWriteBytes(span.Slice(offset), typedGeometry.Points[i].Y);
                offset += WkbDoubleSize;
                BitConverter.TryWriteBytes(span.Slice(offset), zValues[i]);
                offset += WkbDoubleSize;
            }

            return result;
        }

        return GeometryLineStringAsWkb(geometry);
    }

    private static byte[] GeometryPolygonZAsWkb(IGeometry geometry)
    {
        if (geometry is not Geometry<PointZ> typedGeometry)
            throw new ArgumentException("Geometry must be Geometry<PointZ>", nameof(geometry));

        var ringCount = typedGeometry.Geometries.Count;
        var headerSize = WkbByteOrderSize + WkbGeometryTypeSize + WkbCountSize;

        // Calculate total size: header + sum of all ring sizes (accounting for closure point)
        var totalSize = headerSize;
        foreach (var ring in typedGeometry.Geometries)
        {
            totalSize += CalculateRingSize(ring.Points.Count, hasZ: true, hasM: false);
        }

        var result = new byte[totalSize];
        var span = new Span<byte>(result);

        // Write header
        span[0] = (byte)WkbByteOrder.WkbNdr;
        BitConverter.TryWriteBytes(span.Slice(WkbByteOrderSize), (uint)WkbGeometryType.PolygonZ);
        BitConverter.TryWriteBytes(span.Slice(WkbByteOrderSize + WkbGeometryTypeSize), (uint)ringCount);

        // Write rings
        int offset = headerSize;
        foreach (var ring in typedGeometry.Geometries)
        {
            offset = WriteLinearRing(span, offset, ring.Points, hasZ: true, hasM: false);
        }

        return result;
    }

    #endregion

    #region M Variants

    private static byte[] GeometryPointMAsWkb(IGeometry geometry)
    {
        if (geometry is Geometry<PointM> typedGeometry)
        {
            var point = typedGeometry.Points[0];
            return OgcWkbMapFunctions.ToWkbPointM(point, point.M);
        }

        return GeometryPointAsWkb(geometry);
    }

    private static byte[] GeometryLineStringMAsWkb(IGeometry geometry)
    {
        if (geometry is Geometry<PointM> typedGeometry)
        {
            var mValues = ExtractMFromPoints(typedGeometry.Points);
            return OgcWkbMapFunctions.ToWkbLineStringM(typedGeometry.Points, mValues);
        }

        return GeometryLineStringAsWkb(geometry);
    }

    private static byte[] GeometryPolygonMAsWkb(IGeometry geometry)
    {
        if (geometry is not Geometry<PointM> typedGeometry)
            throw new ArgumentException("Geometry must be Geometry<PointM>", nameof(geometry));

        var ringCount = typedGeometry.Geometries.Count;
        var headerSize = WkbByteOrderSize + WkbGeometryTypeSize + WkbCountSize;

        // Calculate total size: header + sum of all ring sizes (accounting for closure point)
        var totalSize = headerSize;
        foreach (var ring in typedGeometry.Geometries)
        {
            totalSize += CalculateRingSize(ring.Points.Count, hasZ: false, hasM: true);
        }

        var result = new byte[totalSize];
        var span = new Span<byte>(result);

        // Write header
        span[0] = (byte)WkbByteOrder.WkbNdr;
        BitConverter.TryWriteBytes(span.Slice(WkbByteOrderSize), (uint)WkbGeometryType.PolygonM);
        BitConverter.TryWriteBytes(span.Slice(WkbByteOrderSize + WkbGeometryTypeSize), (uint)ringCount);

        // Write rings
        int offset = headerSize;
        foreach (var ring in typedGeometry.Geometries)
        {
            offset = WriteLinearRing(span, offset, ring.Points, hasZ: false, hasM: true);
        }

        return result;
    }

    #endregion

    #region ZM Variants

    private static byte[] GeometryPointZMAsWkb(IGeometry geometry)
    {
        if (geometry is Geometry<PointZM> typedGeometry)
        {
            var point = typedGeometry.Points[0];
            return OgcWkbMapFunctions.ToWkbPointZM(point, point.Z, point.M);
        }

        return GeometryPointAsWkb(geometry);
    }

    private static byte[] GeometryLineStringZMAsWkb(IGeometry geometry)
    {
        if (geometry is Geometry<PointZM> typedGeometry)
        {
            var zValues = ExtractZFromPoints(typedGeometry.Points);
            var mValues = ExtractMFromPoints(typedGeometry.Points);
            return OgcWkbMapFunctions.ToWkbLineStringZM(typedGeometry.Points, zValues, mValues);
        }

        return GeometryLineStringAsWkb(geometry);
    }

    private static byte[] GeometryPolygonZMAsWkb(IGeometry geometry)
    {
        if (geometry is not Geometry<PointZM> typedGeometry)
            throw new ArgumentException("Geometry must be Geometry<PointZM>", nameof(geometry));

        var ringCount = typedGeometry.Geometries.Count;
        var headerSize = WkbByteOrderSize + WkbGeometryTypeSize + WkbCountSize;

        // Calculate total size: header + sum of all ring sizes (accounting for closure point)
        var totalSize = headerSize;
        foreach (var ring in typedGeometry.Geometries)
        {
            totalSize += CalculateRingSize(ring.Points.Count, hasZ: true, hasM: true);
        }

        var result = new byte[totalSize];
        var span = new Span<byte>(result);

        // Write header
        span[0] = (byte)WkbByteOrder.WkbNdr;
        BitConverter.TryWriteBytes(span.Slice(WkbByteOrderSize), (uint)WkbGeometryType.PolygonZM);
        BitConverter.TryWriteBytes(span.Slice(WkbByteOrderSize + WkbGeometryTypeSize), (uint)ringCount);

        // Write rings
        int offset = headerSize;
        foreach (var ring in typedGeometry.Geometries)
        {
            offset = WriteLinearRing(span, offset, ring.Points, hasZ: true, hasM: true);
        }

        return result;
    }

    #endregion

    #endregion
}
