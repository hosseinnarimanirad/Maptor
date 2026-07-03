using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.Common.Enums;

namespace IRI.Maptor.Sta.Spatial.IO.SqlServerNativeBinary;

public static partial class SqlServerSpatialNativeBinary
{
    private static SqlServerSpatialNativeBinaryTypes DetermineTypeFromOpenGisType(byte openGisType, byte serializationProps)
    {
        return openGisType switch
        {
            1 => SqlServerSpatialNativeBinaryTypes.Point,
            2 => SqlServerSpatialNativeBinaryTypes.LineString,
            3 => SqlServerSpatialNativeBinaryTypes.Polygon,
            4 => SqlServerSpatialNativeBinaryTypes.MultiPoint,
            5 => SqlServerSpatialNativeBinaryTypes.MultiLineString,
            6 => SqlServerSpatialNativeBinaryTypes.MultiPolygon,
            _ => SqlServerSpatialNativeBinaryTypes.Point
        };
    }

    /// <summary>
    /// Deserializes a SQL Server native binary (MS-SSCLRT) geometry/geography instance into an <see cref="IGeometry"/>.
    /// </summary>
    /// <param name="nativeBinary">The raw MS-SSCLRT bytes as stored by SQL Server (geometry or geography UDT).</param>
    /// <param name="isGeography">
    /// When true, the payload is interpreted as a SQL Server <c>geography</c> instance: each point is stored as
    /// (Latitude, Longitude) per [MS-SSCLRT] §2.1.5, so coordinates are swapped into the (X = Longitude, Y = Latitude)
    /// order used by <see cref="Geometry{T}"/>. When false (default), the payload is a <c>geometry</c> instance stored
    /// as (X, Y).
    /// </param>
    public static IGeometry Deserialize(byte[] nativeBinary, bool isGeography = false)
    {
        if (nativeBinary.IsNullOrEmpty())
            return null;

        using (var stream = new BinaryReader(new MemoryStream(nativeBinary)))
        {
            // Header: SRID + Version + Serialization Properties
            var srid = stream.ReadInt32();

            // [MS-SSCLRT] §2.1.1: SRID -1 indicates a null instance; all other fields are omitted.
            if (srid == -1)
                return null;

            var version = stream.ReadByte();

            // Only version 1 of the serialization format is supported. Version 2 (SQL Server 2012+) is only
            // emitted for curves (CircularString/CompoundCurve/CurvePolygon), FullGlobe, or larger-than-a-hemisphere
            // geographies ([MS-SSCLRT] §2.1.2/§2.1.4); those constructs are not representable here.
            if (version != 1)
                throw new NotSupportedException(
                    $"SQL Server spatial serialization format version {version} is not supported (only version 1). " +
                    "Version 2 is used for curved geometries, FullGlobe, or larger-than-hemisphere geographies.");

            var serializationPropsByte = stream.ReadByte();
            var serializationProps = SerializationPropHelper.ParseFlags(serializationPropsByte);

            bool hasZ = serializationProps.HasFlag(SerializationProp.Z);
            bool hasM = serializationProps.HasFlag(SerializationProp.M);

            // Handle P flag case (optimized single point)
            if (serializationProps.HasFlag(SerializationProp.P))
            {
                return DeserializeOptimizedPoint(stream, srid, hasZ: hasZ, hasM: hasM, isGeography: isGeography);
            }

            // Handle L flag case (optimized single line segment)
            if (serializationProps.HasFlag(SerializationProp.L))
            {
                return DeserializeOptimizedLineString(stream, srid, hasZ: hasZ, hasM: hasM, isGeography: isGeography);
            }

            // Handle non-optimized case - need to determine geometry type from structure
            return DeserializeNonOptimizedGeometry(stream, srid, hasZ: hasZ, hasM: hasM, isGeography: isGeography);
        }
    }

    /// <summary>
    /// Deserializes a SQL Server native binary instance and returns it as a 2D <see cref="Geometry{Point}"/>.
    /// This is the entry point used by the EF Core type mapping. Returns null for a null instance (SRID -1).
    /// Throws <see cref="NotSupportedException"/> if the instance carries Z or M values, since those materialize
    /// as <see cref="Geometry{PointZ}"/>/<see cref="Geometry{PointM}"/>/<see cref="Geometry{PointZM}"/> rather than
    /// <see cref="Geometry{Point}"/>.
    /// </summary>
    public static Geometry<Point>? DeserializeGeometryPoint(byte[] nativeBinary, bool isGeography = false)
    {
        var geometry = Deserialize(nativeBinary, isGeography);

        if (geometry == null)
            return null;

        if (geometry is Geometry<Point> point)
            return point;

        throw new NotSupportedException(
            $"The spatial instance contains Z and/or M values (materialized as {geometry.GetType().Name}) and cannot be " +
            "read as a 2D Geometry<Point>. Store the column as 2D data or read it via Deserialize(...) with the matching point type.");
    }

    /// <summary>
    /// Reads a single coordinate pair, swapping latitude/longitude order for geography instances
    /// ([MS-SSCLRT] §2.1.5: geography stores Latitude then Longitude; §2.1.6: geometry stores X then Y).
    /// </summary>
    private static (double x, double y) ReadXY(BinaryReader reader, bool isGeography)
    {
        var first = reader.ReadDouble();
        var second = reader.ReadDouble();

        // geography: first = Latitude (Y), second = Longitude (X)
        return isGeography ? (second, first) : (first, second);
    }

    private static IGeometry DeserializeOptimizedPoint(BinaryReader reader, int srid, bool hasZ, bool hasM, bool isGeography)
    {
        // Read X, Y coordinates directly (P flag optimization)
        var (x, y) = ReadXY(reader, isGeography);

        if (hasZ && hasM)
        {
            var z = reader.ReadDouble();
            var m = reader.ReadDouble();

            return Geometry<PointZM>.Create([new PointZM() { X = x, Y = y, Z = z, M = m }], GeometryType.Point, srid);
        }
        else if (hasZ)
        {
            var z = reader.ReadDouble();

            return Geometry<PointZ>.Create([new PointZ() { X = x, Y = y, Z = z }], GeometryType.Point, srid);
        }
        else if (hasM)
        {
            var m = reader.ReadDouble();

            return Geometry<PointM>.Create([new PointM() { X = x, Y = y, M = m }], GeometryType.Point, srid);
        }

        // Point (2D) 
        return Geometry<Point>.Create([new Point(x, y)], GeometryType.Point, srid);
    }

    private static IGeometry DeserializeOptimizedLineString(BinaryReader reader, int srid, bool hasZ, bool hasM, bool isGeography)
    {
        // Read 2 points directly (L flag optimization - single line segment)
        var (x1, y1) = ReadXY(reader, isGeography);
        var (x2, y2) = ReadXY(reader, isGeography);

        if (hasZ && hasM)
        {
            var z1 = reader.ReadDouble();
            var z2 = reader.ReadDouble();
            var m1 = reader.ReadDouble();
            var m2 = reader.ReadDouble();

            var points = new List<PointZM>
            {
                new PointZM() { X = x1, Y = y1, Z = z1, M = m1 },
                new PointZM() { X = x2, Y = y2, Z = z2, M = m2 }
            };
            return Geometry<PointZM>.Create(points, GeometryType.LineString, srid);
        }
        else if (hasZ)
        {
            var z1 = reader.ReadDouble();
            var z2 = reader.ReadDouble();

            var points = new List<PointZ>
            {
                new PointZ() { X = x1, Y = y1, Z = z1 },
                new PointZ() { X = x2, Y = y2, Z = z2 }
            };
            return Geometry<PointZ>.Create(points, GeometryType.LineString, srid);
        }
        else if (hasM)
        {
            var m1 = reader.ReadDouble();
            var m2 = reader.ReadDouble();

            var points = new List<PointM>
            {
                new PointM() { X = x1, Y = y1, M = m1 },
                new PointM() { X = x2, Y = y2, M = m2 }
            };
            return Geometry<PointM>.Create(points, GeometryType.LineString, srid);
        }

        // LineString (2D)
        var points2D = new List<Point>
        {
            new Point(x1, y1),
            new Point(x2, y2)
        };
        return Geometry<Point>.Create(points2D, GeometryType.LineString, srid);
    }

    private static IGeometry DeserializeNonOptimizedGeometry(BinaryReader reader, int srid, bool hasZ, bool hasM, bool isGeography)
    {
        // Save position to read ahead for OpenGIS Type
        var position = reader.BaseStream.Position;

        // Read Number of Points
        var pointCount = reader.ReadInt32();

        // If pointCount is 0, this is an empty geometry - need to determine type from Shapes
        if (pointCount == 0)
        {
            // Read Number of Figures
            var emptyFigureCount = reader.ReadInt32();

            // Read Number of Shapes
            var emptyShapeCount = reader.ReadInt32();

            // Read Shapes section to determine geometry type
            if (emptyShapeCount > 0)
            {
                reader.ReadInt32(); // Parent Offset
                reader.ReadInt32(); // Figure Offset
                var openGisType = reader.ReadByte(); // OpenGIS Type

                // Route to appropriate empty geometry creator
                return openGisType switch
                {
                    1 => Geometry<Point>.CreateEmpty(GeometryType.Point, srid),
                    2 => Geometry<Point>.CreateEmpty(GeometryType.LineString, srid),
                    3 => Geometry<Point>.CreateEmpty(GeometryType.Polygon, srid),
                    4 => Geometry<Point>.CreateEmpty(GeometryType.MultiPoint, srid),
                    5 => Geometry<Point>.CreateEmpty(GeometryType.MultiLineString, srid),
                    6 => Geometry<Point>.CreateEmpty(GeometryType.MultiPolygon, srid),
                    7 => Geometry<Point>.CreateEmpty(GeometryType.GeometryCollection, srid),
                    _ => throw new NotImplementedException($"Empty geometry type {openGisType} is not yet implemented")
                };
            }

            throw new InvalidDataException("Empty geometry must have at least one shape");
        }

        // For non-empty geometries, read ahead to determine type
        // Calculate point data size
        int pointSize = 16; // X, Y (8 bytes each)
        if (hasZ) pointSize += 8;
        if (hasM) pointSize += 8;

        // Skip points, Z values, and M values to get to Figures section
        var pointsEndPosition = position + 4 + (pointCount * pointSize);
        reader.BaseStream.Position = pointsEndPosition;

        // Read Number of Figures
        var figureCount = reader.ReadInt32();

        // Skip Figures section
        for (int i = 0; i < figureCount; i++)
        {
            reader.ReadByte(); // Figure attribute
            reader.ReadInt32(); // Figure point offset
        }

        // Read Shapes section to get OpenGIS Type
        var shapeCount = reader.ReadInt32();
        if (shapeCount > 0)
        {
            reader.ReadInt32(); // Parent Offset
            reader.ReadInt32(); // Figure Offset
            var openGisType = reader.ReadByte(); // OpenGIS Type

            // Reset position to start reading points
            reader.BaseStream.Position = position;

            // Route to appropriate deserializer based on OpenGIS Type
            return openGisType switch
            {
                //1 => DeserializeNonOptimizedPoint(reader, srid, SerializationProp.V), // Point - but should use P flag, handle anyway
                2 => DeserializeLineString(reader, srid, hasZ, hasM, isGeography), // LineString
                3 => DeserializePolygon(reader, srid, hasZ, hasM, isGeography), // Polygon
                4 => DeserializeMultiPoint(reader, srid, hasZ, hasM, isGeography), // MultiPoint
                5 => DeserializeMultiLineString(reader, srid, hasZ, hasM, isGeography), // MultiLineString
                6 => DeserializeMultiPolygon(reader, srid, hasZ, hasM, isGeography), // MultiPolygon
                7 => DeserializeGeometryCollection(reader, srid, hasZ, hasM, isGeography), // GeometryCollection
                _ => throw new NotImplementedException($"Geometry type {openGisType} is not yet implemented")
            };
        }

        throw new InvalidDataException("Non-empty geometry must have at least one shape");
    }

    private static IGeometry DeserializeLineString(BinaryReader reader, int srid, bool hasZ, bool hasM, bool isGeography)
    {
        // Read Number of Points
        var pointCount = reader.ReadInt32();

        // Handle empty LineString
        if (pointCount == 0)
        {
            // Read Number of Figures
            var emptyFigureCount = reader.ReadInt32();

            // Read Number of Shapes
            var emptyShapeCount = reader.ReadInt32();

            // Read Shapes section
            if (emptyShapeCount > 0)
            {
                reader.ReadInt32(); // Parent Offset (should be -1)
                reader.ReadInt32(); // Figure Offset
                reader.ReadByte();  // OpenGIS Type (should be 2 for LineString)
            }

            return Geometry<Point>.CreateEmpty(GeometryType.LineString, srid);
        }

        // Read all X, Y pairs sequentially
        var points = new List<IPoint>(pointCount);
        for (int i = 0; i < pointCount; i++)
        {
            var (x, y) = ReadXY(reader, isGeography);
            points.Add(new Point(x, y));
        }

        // Read all Z values sequentially if Z flag is set
        List<double>? zValues = null;
        if (hasZ)
        {
            zValues = new List<double>(pointCount);
            for (int i = 0; i < pointCount; i++)
            {
                zValues.Add(reader.ReadDouble());
            }
        }

        // Read all M values sequentially if M flag is set
        List<double>? mValues = null;
        if (hasM)
        {
            mValues = new List<double>(pointCount);
            for (int i = 0; i < pointCount; i++)
            {
                mValues.Add(reader.ReadDouble());
            }
        }

        // Read Number of Figures
        var figureCount = reader.ReadInt32();
        if (figureCount > 0)
        {
            reader.ReadByte(); // Figure Attribute (should be 0x01 for stroke)
            reader.ReadInt32(); // Figure Point Offset (should be 0 for LineString)
        }

        // Read Number of Shapes
        var shapeCount = reader.ReadInt32();
        if (shapeCount > 0)
        {
            reader.ReadInt32(); // Parent Offset (should be -1)
            reader.ReadInt32(); // Figure Offset
            reader.ReadByte();  // OpenGIS Type (should be 2 for LineString)
        }

        // Create appropriate LineString type based on Z/M flags
        if (hasZ && hasM)
        {
            var pointZMList = new List<PointZM>(pointCount);
            for (int i = 0; i < pointCount; i++)
            {
                pointZMList.Add(new PointZM
                {
                    X = points[i].X,
                    Y = points[i].Y,
                    Z = zValues![i],
                    M = mValues![i]
                });
            }
            return Geometry<PointZM>.Create(pointZMList, GeometryType.LineString, srid);
        }
        else if (hasZ)
        {
            var pointZList = new List<PointZ>(pointCount);
            for (int i = 0; i < pointCount; i++)
            {
                pointZList.Add(new PointZ
                {
                    X = points[i].X,
                    Y = points[i].Y,
                    Z = zValues![i]
                });
            }
            return Geometry<PointZ>.Create(pointZList, GeometryType.LineString, srid);
        }
        else if (hasM)
        {
            var pointMList = new List<PointM>(pointCount);
            for (int i = 0; i < pointCount; i++)
            {
                pointMList.Add(new PointM
                {
                    X = points[i].X,
                    Y = points[i].Y,
                    M = mValues![i]
                });
            }
            return Geometry<PointM>.Create(pointMList, GeometryType.LineString, srid);
        }
        else
        {
            var pointList = new List<Point>(pointCount);
            for (int i = 0; i < pointCount; i++)
            {
                pointList.Add(new Point(points[i].X, points[i].Y));
            }
            return Geometry<Point>.Create(pointList, GeometryType.LineString, srid);
        }
    }

    private static IGeometry DeserializeMultiPoint(BinaryReader reader, int srid, bool hasZ, bool hasM, bool isGeography)
    {
        // Read Number of Points
        var pointCount = reader.ReadInt32();

        // Handle empty MultiPoint
        if (pointCount == 0)
        {
            // Read Number of Figures
            var emptyFigureCount = reader.ReadInt32();

            // Read Number of Shapes
            var emptyShapeCount = reader.ReadInt32();

            // Read Shapes section
            if (emptyShapeCount > 0)
            {
                reader.ReadInt32(); // Parent Offset (should be -1)
                reader.ReadInt32(); // Figure Offset
                reader.ReadByte();  // OpenGIS Type (should be 4 for MultiPoint)
            }

            return Geometry<Point>.CreateEmpty(GeometryType.MultiPoint, srid);
        }

        // Read all X, Y pairs sequentially
        var points = new List<IPoint>(pointCount);
        for (int i = 0; i < pointCount; i++)
        {
            var (x, y) = ReadXY(reader, isGeography);
            points.Add(new Point(x, y));
        }

        // Read all Z values sequentially if Z flag is set
        List<double>? zValues = null;
        if (hasZ)
        {
            zValues = new List<double>(pointCount);
            for (int i = 0; i < pointCount; i++)
            {
                zValues.Add(reader.ReadDouble());
            }
        }

        // Read all M values sequentially if M flag is set
        List<double>? mValues = null;
        if (hasM)
        {
            mValues = new List<double>(pointCount);
            for (int i = 0; i < pointCount; i++)
            {
                mValues.Add(reader.ReadDouble());
            }
        }

        // Read Number of Figures (should equal pointCount)
        var figureCount = reader.ReadInt32();
        if (figureCount != pointCount)
            throw new InvalidDataException($"MultiPoint figure count ({figureCount}) does not match point count ({pointCount})");

        // Read Figures section - one figure per point
        for (int i = 0; i < figureCount; i++)
        {
            var figureAttribute = reader.ReadByte();
            if (figureAttribute != 0x01)
                throw new InvalidDataException($"MultiPoint figure attribute should be 0x01 (stroke), got {figureAttribute:X2}");

            var pointOffset = reader.ReadInt32();
            if (pointOffset != i)
                throw new InvalidDataException($"MultiPoint figure point offset ({pointOffset}) does not match expected index ({i})");
        }

        // Read Number of Shapes (should equal pointCount + 1: one parent MultiPoint + one per point)
        var shapeCount = reader.ReadInt32();
        if (shapeCount != pointCount + 1)
            throw new InvalidDataException($"MultiPoint shape count ({shapeCount}) should equal point count + 1 ({pointCount + 1})");

        // Read first shape: Parent MultiPoint shape
        var parentMultiPointParentOffset = reader.ReadInt32();
        if (parentMultiPointParentOffset != -1)
            throw new InvalidDataException($"MultiPoint parent shape parent offset should be -1, got {parentMultiPointParentOffset}");

        var parentMultiPointFigureOffset = reader.ReadInt32();
        if (parentMultiPointFigureOffset != 0)
            throw new InvalidDataException($"MultiPoint parent shape figure offset should be 0, got {parentMultiPointFigureOffset}");

        var parentMultiPointOpenGisType = reader.ReadByte();
        if (parentMultiPointOpenGisType != 0x04)
            throw new InvalidDataException($"MultiPoint parent shape OpenGIS type should be 0x04 (MultiPoint), got {parentMultiPointOpenGisType:X2}");

        // Read Shapes section - one shape per point (children of MultiPoint)
        for (int i = 0; i < pointCount; i++)
        {
            var parentOffset = reader.ReadInt32();
            if (parentOffset != 0)
                throw new InvalidDataException($"MultiPoint child shape parent offset should be 0 (pointing to MultiPoint parent), got {parentOffset}");

            var figureOffset = reader.ReadInt32();
            if (figureOffset != i)
                throw new InvalidDataException($"MultiPoint child shape figure offset ({figureOffset}) does not match expected index ({i})");

            var openGisType = reader.ReadByte();
            if (openGisType != 0x01)
                throw new InvalidDataException($"MultiPoint child shape OpenGIS type should be 0x01 (Point), got {openGisType:X2}");
        }

        // Create Point geometries for each point
        if (hasZ && hasM)
        {
            var pointGeometries = new List<Geometry<PointZM>>(pointCount);
            for (int i = 0; i < pointCount; i++)
            {
                var pointZM = new PointZM
                {
                    X = points[i].X,
                    Y = points[i].Y,
                    Z = zValues![i],
                    M = mValues![i]
                };
                pointGeometries.Add(Geometry<PointZM>.Create([pointZM], GeometryType.Point, srid));
            }
            return Geometry<PointZM>.Create(pointGeometries, GeometryType.MultiPoint, srid);
        }
        else if (hasZ)
        {
            var pointGeometries = new List<Geometry<PointZ>>(pointCount);
            for (int i = 0; i < pointCount; i++)
            {
                var pointZ = new PointZ
                {
                    X = points[i].X,
                    Y = points[i].Y,
                    Z = zValues![i]
                };
                pointGeometries.Add(Geometry<PointZ>.Create([pointZ], GeometryType.Point, srid));
            }
            return Geometry<PointZ>.Create(pointGeometries, GeometryType.MultiPoint, srid);
        }
        else if (hasM)
        {
            var pointGeometries = new List<Geometry<PointM>>(pointCount);
            for (int i = 0; i < pointCount; i++)
            {
                var pointM = new PointM
                {
                    X = points[i].X,
                    Y = points[i].Y,
                    M = mValues![i]
                };
                pointGeometries.Add(Geometry<PointM>.Create([pointM], GeometryType.Point, srid));
            }
            return Geometry<PointM>.Create(pointGeometries, GeometryType.MultiPoint, srid);
        }
        else
        {
            var pointGeometries = new List<Geometry<Point>>(pointCount);
            for (int i = 0; i < pointCount; i++)
            {
                pointGeometries.Add(Geometry<Point>.Create([new Point(points[i].X, points[i].Y)], GeometryType.Point, srid));
            }
            return Geometry<Point>.Create(pointGeometries, GeometryType.MultiPoint, srid);
        }
    }

    //private static IGeometry DeserializeNonOptimizedPoint(BinaryReader reader, int srid, SerializationProp flags)
    //{
    //    // Read Number of Points
    //    var pointCount = reader.ReadInt32();

    //    // If pointCount is 0, this is an empty point
    //    if (pointCount == 0)
    //    {
    //        // Read Number of Figures
    //        var figureCount = reader.ReadInt32();

    //        // Read Number of Shapes
    //        var shapeCount = reader.ReadInt32();

    //        // Read Shapes section (should have one shape for empty point)
    //        if (shapeCount > 0)
    //        {
    //            reader.ReadInt32(); // Parent Offset (should be -1)
    //            reader.ReadInt32(); // Figure Offset
    //            reader.ReadByte();  // OpenGIS Type (should be 1 for Point)
    //        }

    //        return Geometry<Point>.CreateEmpty(GeometryType.Point, srid);
    //    }

    //    // For non-empty, non-optimized points, read the full structure
    //    // This case is less common for points (usually uses P flag optimization)
    //    throw new NotImplementedException("Non-optimized non-empty point deserialization is not yet implemented");
    //}

    private static IGeometry DeserializeMultiLineString(BinaryReader reader, int srid, bool hasZ, bool hasM, bool isGeography)
    {
        // Read Number of Points (total across all LineStrings)
        var pointCount = reader.ReadInt32();

        // Handle empty MultiLineString
        if (pointCount == 0)
        {
            // Read Number of Figures
            var emptyFigureCount = reader.ReadInt32();

            // Read Number of Shapes
            var emptyShapeCount = reader.ReadInt32();

            // Read Shapes section
            if (emptyShapeCount > 0)
            {
                reader.ReadInt32(); // Parent Offset (should be -1)
                reader.ReadInt32(); // Figure Offset
                reader.ReadByte();  // OpenGIS Type (should be 5 for MultiLineString)
            }

            return Geometry<Point>.CreateEmpty(GeometryType.MultiLineString, srid);
        }

        // Read all X, Y pairs sequentially
        var points = new List<IPoint>(pointCount);
        for (int i = 0; i < pointCount; i++)
        {
            var (x, y) = ReadXY(reader, isGeography);
            points.Add(new Point(x, y));
        }

        // Read all Z values sequentially if Z flag is set
        List<double>? zValues = null;
        if (hasZ)
        {
            zValues = new List<double>(pointCount);
            for (int i = 0; i < pointCount; i++)
            {
                zValues.Add(reader.ReadDouble());
            }
        }

        // Read all M values sequentially if M flag is set
        List<double>? mValues = null;
        if (hasM)
        {
            mValues = new List<double>(pointCount);
            for (int i = 0; i < pointCount; i++)
            {
                mValues.Add(reader.ReadDouble());
            }
        }

        // Read Number of Figures (should equal number of LineStrings)
        var figureCount = reader.ReadInt32();
        if (figureCount == 0)
            throw new InvalidDataException("MultiLineString must have at least one figure");

        // Read Figures section - store Point Offsets to determine LineString boundaries
        var figurePointOffsets = new List<int>(figureCount);
        for (int i = 0; i < figureCount; i++)
        {
            var figureAttribute = reader.ReadByte();
            if (figureAttribute != 0x01)
                throw new InvalidDataException($"MultiLineString figure attribute should be 0x01 (stroke), got {figureAttribute:X2}");

            var pointOffset = reader.ReadInt32();
            figurePointOffsets.Add(pointOffset);
        }

        // Read Number of Shapes (should equal LineString count + 1: parent + children)
        var shapeCount = reader.ReadInt32();
        if (shapeCount != figureCount + 1)
            throw new InvalidDataException($"MultiLineString shape count ({shapeCount}) should equal figure count + 1 ({figureCount + 1})");

        // Read first shape: Parent MultiLineString shape
        var parentMultiLineStringParentOffset = reader.ReadInt32();
        if (parentMultiLineStringParentOffset != -1)
            throw new InvalidDataException($"MultiLineString parent shape parent offset should be -1, got {parentMultiLineStringParentOffset}");

        var parentMultiLineStringFigureOffset = reader.ReadInt32();
        if (parentMultiLineStringFigureOffset != 0)
            throw new InvalidDataException($"MultiLineString parent shape figure offset should be 0, got {parentMultiLineStringFigureOffset}");

        var parentMultiLineStringOpenGisType = reader.ReadByte();
        if (parentMultiLineStringOpenGisType != 0x05)
            throw new InvalidDataException($"MultiLineString parent shape OpenGIS type should be 0x05 (MultiLineString), got {parentMultiLineStringOpenGisType:X2}");

        // Read Shapes section - one shape per LineString (children of MultiLineString)
        for (int i = 0; i < figureCount; i++)
        {
            var parentOffset = reader.ReadInt32();
            if (parentOffset != 0)
                throw new InvalidDataException($"MultiLineString child shape parent offset should be 0 (pointing to MultiLineString parent), got {parentOffset}");

            var figureOffset = reader.ReadInt32();
            if (figureOffset != i)
                throw new InvalidDataException($"MultiLineString child shape figure offset ({figureOffset}) does not match expected index ({i})");

            var openGisType = reader.ReadByte();
            if (openGisType != 0x02)
                throw new InvalidDataException($"MultiLineString child shape OpenGIS type should be 0x02 (LineString), got {openGisType:X2}");
        }

        // Group points by LineString using Figure Point Offsets
        var lineStringGeometries = new List<IGeometry>(figureCount);

        for (int i = 0; i < figureCount; i++)
        {
            var startOffset = figurePointOffsets[i];
            var endOffset = (i < figureCount - 1) ? figurePointOffsets[i + 1] : pointCount;
            var lineStringPointCount = endOffset - startOffset;

            if (lineStringPointCount < 2)
                throw new InvalidDataException($"LineString must have at least 2 points, got {lineStringPointCount}");

            // Extract points for this LineString
            if (hasZ && hasM)
            {
                var pointZMList = new List<PointZM>(lineStringPointCount);
                for (int j = startOffset; j < endOffset; j++)
                {
                    pointZMList.Add(new PointZM
                    {
                        X = points[j].X,
                        Y = points[j].Y,
                        Z = zValues![j],
                        M = mValues![j]
                    });
                }
                lineStringGeometries.Add(Geometry<PointZM>.Create(pointZMList, GeometryType.LineString, srid));
            }
            else if (hasZ)
            {
                var pointZList = new List<PointZ>(lineStringPointCount);
                for (int j = startOffset; j < endOffset; j++)
                {
                    pointZList.Add(new PointZ
                    {
                        X = points[j].X,
                        Y = points[j].Y,
                        Z = zValues![j]
                    });
                }
                lineStringGeometries.Add(Geometry<PointZ>.Create(pointZList, GeometryType.LineString, srid));
            }
            else if (hasM)
            {
                var pointMList = new List<PointM>(lineStringPointCount);
                for (int j = startOffset; j < endOffset; j++)
                {
                    pointMList.Add(new PointM
                    {
                        X = points[j].X,
                        Y = points[j].Y,
                        M = mValues![j]
                    });
                }
                lineStringGeometries.Add(Geometry<PointM>.Create(pointMList, GeometryType.LineString, srid));
            }
            else
            {
                var pointList = new List<Point>(lineStringPointCount);
                for (int j = startOffset; j < endOffset; j++)
                {
                    pointList.Add(new Point(points[j].X, points[j].Y));
                }
                lineStringGeometries.Add(Geometry<Point>.Create(pointList, GeometryType.LineString, srid));
            }
        }

        // Create appropriate MultiLineString type based on Z/M flags
        if (hasZ && hasM)
        {
            return Geometry<PointZM>.Create(lineStringGeometries.Cast<Geometry<PointZM>>().ToList(), GeometryType.MultiLineString, srid);
        }
        else if (hasZ)
        {
            return Geometry<PointZ>.Create(lineStringGeometries.Cast<Geometry<PointZ>>().ToList(), GeometryType.MultiLineString, srid);
        }
        else if (hasM)
        {
            return Geometry<PointM>.Create(lineStringGeometries.Cast<Geometry<PointM>>().ToList(), GeometryType.MultiLineString, srid);
        }
        else
        {
            return Geometry<Point>.Create(lineStringGeometries.Cast<Geometry<Point>>().ToList(), GeometryType.MultiLineString, srid);
        }
    }

    private static IGeometry DeserializePolygon(BinaryReader reader, int srid, bool hasZ, bool hasM, bool isGeography)
    {
        // Read Number of Points (total across all rings)
        var pointCount = reader.ReadInt32();

        // Handle empty Polygon
        if (pointCount == 0)
        {
            // Read Number of Figures
            var emptyFigureCount = reader.ReadInt32();

            // Read Number of Shapes
            var emptyShapeCount = reader.ReadInt32();

            // Read Shapes section
            if (emptyShapeCount > 0)
            {
                reader.ReadInt32(); // Parent Offset (should be -1)
                reader.ReadInt32(); // Figure Offset
                reader.ReadByte();  // OpenGIS Type (should be 3 for Polygon)
            }

            return Geometry<Point>.CreateEmpty(GeometryType.Polygon, srid);
        }

        // Read all X, Y pairs sequentially
        var points = new List<IPoint>(pointCount);
        for (int i = 0; i < pointCount; i++)
        {
            var (x, y) = ReadXY(reader, isGeography);
            points.Add(new Point(x, y));
        }

        // Read all Z values sequentially if Z flag is set
        List<double>? zValues = null;
        if (hasZ)
        {
            zValues = new List<double>(pointCount);
            for (int i = 0; i < pointCount; i++)
            {
                zValues.Add(reader.ReadDouble());
            }
        }

        // Read all M values sequentially if M flag is set
        List<double>? mValues = null;
        if (hasM)
        {
            mValues = new List<double>(pointCount);
            for (int i = 0; i < pointCount; i++)
            {
                mValues.Add(reader.ReadDouble());
            }
        }

        // Read Number of Figures (should equal number of rings: 1 exterior + N interior)
        var figureCount = reader.ReadInt32();
        if (figureCount == 0)
            throw new InvalidDataException("Polygon must have at least one figure (exterior ring)");

        // Read Figures section - store Point Offsets and Attributes to determine ring boundaries
        var figurePointOffsets = new List<int>(figureCount);
        var figureAttributes = new List<byte>(figureCount);

        for (int i = 0; i < figureCount; i++)
        {
            var figureAttribute = reader.ReadByte();
            var pointOffset = reader.ReadInt32();

            figureAttributes.Add(figureAttribute);
            figurePointOffsets.Add(pointOffset);

            // Validate first figure is exterior ring
            if (i == 0)
            {
                if (figureAttribute != 0x02)
                    throw new InvalidDataException($"Polygon first figure attribute should be 0x02 (exterior ring), got {figureAttribute:X2}");
                if (pointOffset != 0)
                    throw new InvalidDataException($"Polygon exterior ring point offset should be 0, got {pointOffset}");
            }
            else
            {
                // Validate remaining figures are interior rings
                if (figureAttribute != 0x00)
                    throw new InvalidDataException($"Polygon interior ring figure attribute should be 0x00 (interior ring), got {figureAttribute:X2}");
            }
        }

        // Read Number of Shapes (should equal 1 for Polygon)
        var shapeCount = reader.ReadInt32();
        if (shapeCount != 1)
            throw new InvalidDataException($"Polygon shape count should be 1, got {shapeCount}");

        // Read Shapes section
        var parentOffset = reader.ReadInt32();
        if (parentOffset != -1)
            throw new InvalidDataException($"Polygon shape parent offset should be -1, got {parentOffset}");

        var figureOffset = reader.ReadInt32();
        if (figureOffset != 0)
            throw new InvalidDataException($"Polygon shape figure offset should be 0 (pointing to exterior ring), got {figureOffset}");

        var openGisType = reader.ReadByte();
        if (openGisType != 0x03)
            throw new InvalidDataException($"Polygon shape OpenGIS type should be 0x03 (Polygon), got {openGisType:X2}");

        // Group points by ring using Figure Point Offsets
        var rings = new List<IGeometry>(figureCount);

        for (int i = 0; i < figureCount; i++)
        {
            var startOffset = figurePointOffsets[i];
            var endOffset = (i < figureCount - 1) ? figurePointOffsets[i + 1] : pointCount;
            var ringPointCount = endOffset - startOffset;

            if (ringPointCount < 3)
                throw new InvalidDataException($"Polygon ring must have at least 3 points, got {ringPointCount}");

            // Check if ring is explicitly closed (last point equals first point)
            // If so, exclude the last point since isClosed=true will handle closure
            var actualEndOffset = endOffset;
            if (ringPointCount > 0)
            {
                var firstPointIndex = startOffset;
                var lastPointIndex = endOffset - 1;
                if (points[firstPointIndex].X == points[lastPointIndex].X &&
                    points[firstPointIndex].Y == points[lastPointIndex].Y)
                {
                    actualEndOffset = endOffset - 1; // Exclude the duplicate closing point
                }
            }

            // Extract points for this ring (excluding duplicate closing point if present)
            if (hasZ && hasM)
            {
                var pointZMList = new List<PointZM>(actualEndOffset - startOffset);
                for (int j = startOffset; j < actualEndOffset; j++)
                {
                    pointZMList.Add(new PointZM
                    {
                        X = points[j].X,
                        Y = points[j].Y,
                        Z = zValues![j],
                        M = mValues![j]
                    });
                }
                rings.Add(Geometry<PointZM>.CreatePolygonRing(pointZMList, /*GeometryType.LineString, true, */srid)); // isClosed = true for rings
            }
            else if (hasZ)
            {
                var pointZList = new List<PointZ>(actualEndOffset - startOffset);
                for (int j = startOffset; j < actualEndOffset; j++)
                {
                    pointZList.Add(new PointZ
                    {
                        X = points[j].X,
                        Y = points[j].Y,
                        Z = zValues![j]
                    });
                }
                rings.Add(Geometry<PointZ>.CreatePolygonRing(pointZList, /*GeometryType.LineString, true,*/ srid)); // isClosed = true for rings
            }
            else if (hasM)
            {
                var pointMList = new List<PointM>(actualEndOffset - startOffset);
                for (int j = startOffset; j < actualEndOffset; j++)
                {
                    pointMList.Add(new PointM
                    {
                        X = points[j].X,
                        Y = points[j].Y,
                        M = mValues![j]
                    });
                }
                rings.Add(Geometry<PointM>.CreatePolygonRing(pointMList, /*GeometryType.LineString, true,*/ srid)); // isClosed = true for rings
            }
            else
            {
                var pointList = new List<Point>(actualEndOffset - startOffset);
                for (int j = startOffset; j < actualEndOffset; j++)
                {
                    pointList.Add(new Point(points[j].X, points[j].Y));
                }
                rings.Add(Geometry<Point>.CreatePolygonRing(pointList, /*GeometryType.LineString, true,*/ srid)); // isClosed = true for rings
            }
        }

        // Create appropriate Polygon type based on Z/M flags
        // rings[0] is exterior ring, rings[1..] are interior rings
        if (hasZ && hasM)
        {
            return Geometry<PointZM>.Create(rings.Cast<Geometry<PointZM>>().ToList(), GeometryType.Polygon, srid);
        }
        else if (hasZ)
        {
            return Geometry<PointZ>.Create(rings.Cast<Geometry<PointZ>>().ToList(), GeometryType.Polygon, srid);
        }
        else if (hasM)
        {
            return Geometry<PointM>.Create(rings.Cast<Geometry<PointM>>().ToList(), GeometryType.Polygon, srid);
        }
        else
        {
            return Geometry<Point>.Create(rings.Cast<Geometry<Point>>().ToList(), GeometryType.Polygon, srid);
        }
    }

    private static IGeometry DeserializeMultiPolygon(BinaryReader reader, int srid, bool hasZ, bool hasM, bool isGeography)
    {
        // Read Number of Points (total across all rings of all polygons)
        var pointCount = reader.ReadInt32();

        // Handle empty MultiPolygon
        if (pointCount == 0)
        {
            // Read Number of Figures
            var emptyFigureCount = reader.ReadInt32();

            // Read Number of Shapes
            var emptyShapeCount = reader.ReadInt32();

            // Read Shapes section
            if (emptyShapeCount > 0)
            {
                reader.ReadInt32(); // Parent Offset (should be -1)
                reader.ReadInt32(); // Figure Offset
                reader.ReadByte();  // OpenGIS Type (should be 6 for MultiPolygon)
            }

            return Geometry<Point>.CreateEmpty(GeometryType.MultiPolygon, srid);
        }

        // Read all X, Y pairs sequentially
        var points = new List<IPoint>(pointCount);
        for (int i = 0; i < pointCount; i++)
        {
            var (x, y) = ReadXY(reader, isGeography);
            points.Add(new Point(x, y));
        }

        // Read all Z values sequentially if Z flag is set
        List<double>? zValues = null;
        if (hasZ)
        {
            zValues = new List<double>(pointCount);
            for (int i = 0; i < pointCount; i++)
            {
                zValues.Add(reader.ReadDouble());
            }
        }

        // Read all M values sequentially if M flag is set
        List<double>? mValues = null;
        if (hasM)
        {
            mValues = new List<double>(pointCount);
            for (int i = 0; i < pointCount; i++)
            {
                mValues.Add(reader.ReadDouble());
            }
        }

        // Read Number of Figures (should equal total number of rings across all polygons)
        var figureCount = reader.ReadInt32();
        if (figureCount == 0)
            throw new InvalidDataException("MultiPolygon must have at least one figure (exterior ring)");

        // Read Figures section - store Attributes and Point Offsets
        var figureAttributes = new List<byte>(figureCount);
        var figurePointOffsets = new List<int>(figureCount);

        for (int i = 0; i < figureCount; i++)
        {
            var figureAttribute = reader.ReadByte();
            var pointOffset = reader.ReadInt32();

            figureAttributes.Add(figureAttribute);
            figurePointOffsets.Add(pointOffset);

            // Validate first figure is exterior ring
            if (i == 0)
            {
                if (figureAttribute != 0x02)
                    throw new InvalidDataException($"MultiPolygon first figure attribute should be 0x02 (exterior ring), got {figureAttribute:X2}");
                if (pointOffset != 0)
                    throw new InvalidDataException($"MultiPolygon first figure point offset should be 0, got {pointOffset}");
            }
        }

        // Read Number of Shapes (should equal Polygon count + 1: parent + children)
        var shapeCount = reader.ReadInt32();
        if (shapeCount < 2)
            throw new InvalidDataException($"MultiPolygon shape count should be at least 2 (parent + at least one Polygon), got {shapeCount}");

        // Read first shape: Parent MultiPolygon shape
        var parentMultiPolygonParentOffset = reader.ReadInt32();
        if (parentMultiPolygonParentOffset != -1)
            throw new InvalidDataException($"MultiPolygon parent shape parent offset should be -1, got {parentMultiPolygonParentOffset}");

        var parentMultiPolygonFigureOffset = reader.ReadInt32();
        if (parentMultiPolygonFigureOffset != 0)
            throw new InvalidDataException($"MultiPolygon parent shape figure offset should be 0, got {parentMultiPolygonFigureOffset}");

        var parentMultiPolygonOpenGisType = reader.ReadByte();
        if (parentMultiPolygonOpenGisType != 0x06)
            throw new InvalidDataException($"MultiPolygon parent shape OpenGIS type should be 0x06 (MultiPolygon), got {parentMultiPolygonOpenGisType:X2}");

        // Read remaining shapes - one per Polygon
        var polygonCount = shapeCount - 1;
        var polygonFigureOffsets = new List<int>(polygonCount);

        for (int i = 0; i < polygonCount; i++)
        {
            var parentOffset = reader.ReadInt32();
            if (parentOffset != 0)
                throw new InvalidDataException($"MultiPolygon child shape parent offset should be 0 (pointing to MultiPolygon parent), got {parentOffset}");

            var figureOffset = reader.ReadInt32();
            polygonFigureOffsets.Add(figureOffset);

            var openGisType = reader.ReadByte();
            if (openGisType != 0x03)
                throw new InvalidDataException($"MultiPolygon child shape OpenGIS type should be 0x03 (Polygon), got {openGisType:X2}");
        }

        // Group figures by Polygon using Shape Figure Offsets
        // Each Polygon shape's Figure Offset points to the first figure (exterior ring) of that polygon
        var polygonGeometries = new List<IGeometry>(polygonCount);

        for (int polyIndex = 0; polyIndex < polygonCount; polyIndex++)
        {
            var polygonFirstFigureIndex = polygonFigureOffsets[polyIndex];
            var polygonLastFigureIndex = (polyIndex < polygonCount - 1) ? polygonFigureOffsets[polyIndex + 1] : figureCount;

            // Determine which figures belong to this polygon
            var polygonRingCount = polygonLastFigureIndex - polygonFirstFigureIndex;
            if (polygonRingCount == 0)
                throw new InvalidDataException($"Polygon {polyIndex} has no rings");

            // Group points by ring for this polygon
            var rings = new List<IGeometry>(polygonRingCount);

            for (int ringIndex = 0; ringIndex < polygonRingCount; ringIndex++)
            {
                var figureIndex = polygonFirstFigureIndex + ringIndex;
                var startOffset = figurePointOffsets[figureIndex];
                var endOffset = (figureIndex < figureCount - 1) ? figurePointOffsets[figureIndex + 1] : pointCount;
                var ringPointCount = endOffset - startOffset;

                if (ringPointCount < 3)
                    throw new InvalidDataException($"Polygon ring must have at least 3 points, got {ringPointCount}");

                // Validate figure attribute
                var figureAttribute = figureAttributes[figureIndex];
                if (ringIndex == 0 && figureAttribute != 0x02)
                    throw new InvalidDataException($"Polygon exterior ring figure attribute should be 0x02, got {figureAttribute:X2}");
                if (ringIndex > 0 && figureAttribute != 0x00)
                    throw new InvalidDataException($"Polygon interior ring figure attribute should be 0x00, got {figureAttribute:X2}");

                // Check if ring is explicitly closed (last point equals first point)
                // If so, exclude the last point since isClosed=true will handle closure
                var actualEndOffset = endOffset;
                if (ringPointCount > 0)
                {
                    var firstPointIndex = startOffset;
                    var lastPointIndex = endOffset - 1;
                    if (points[firstPointIndex].X == points[lastPointIndex].X &&
                        points[firstPointIndex].Y == points[lastPointIndex].Y)
                    {
                        actualEndOffset = endOffset - 1; // Exclude the duplicate closing point
                    }
                }

                // Extract points for this ring (excluding duplicate closing point if present)
                if (hasZ && hasM)
                {
                    var pointZMList = new List<PointZM>(actualEndOffset - startOffset);
                    for (int j = startOffset; j < actualEndOffset; j++)
                    {
                        pointZMList.Add(new PointZM
                        {
                            X = points[j].X,
                            Y = points[j].Y,
                            Z = zValues![j],
                            M = mValues![j]
                        });
                    }
                    rings.Add(Geometry<PointZM>.CreatePolygonRing(pointZMList, /*GeometryType.LineString, true,*/ srid)); // isClosed = true for rings
                }
                else if (hasZ)
                {
                    var pointZList = new List<PointZ>(actualEndOffset - startOffset);
                    for (int j = startOffset; j < actualEndOffset; j++)
                    {
                        pointZList.Add(new PointZ
                        {
                            X = points[j].X,
                            Y = points[j].Y,
                            Z = zValues![j]
                        });
                    }
                    rings.Add(Geometry<PointZ>.CreatePolygonRing(pointZList, /*GeometryType.LineString, true,*/ srid)); // isClosed = true for rings
                }
                else if (hasM)
                {
                    var pointMList = new List<PointM>(actualEndOffset - startOffset);
                    for (int j = startOffset; j < actualEndOffset; j++)
                    {
                        pointMList.Add(new PointM
                        {
                            X = points[j].X,
                            Y = points[j].Y,
                            M = mValues![j]
                        });
                    }
                    rings.Add(Geometry<PointM>.CreatePolygonRing(pointMList, /*GeometryType.LineString, true,*/ srid)); // isClosed = true for rings
                }
                else
                {
                    var pointList = new List<Point>(actualEndOffset - startOffset);
                    for (int j = startOffset; j < actualEndOffset; j++)
                    {
                        pointList.Add(new Point(points[j].X, points[j].Y));
                    }
                    rings.Add(Geometry<Point>.CreatePolygonRing(pointList, /*GeometryType.LineString, true,*/ srid)); // isClosed = true for rings
                }
            }

            // Create Polygon geometry from rings (rings[0] is exterior, rings[1..] are interior)
            if (hasZ && hasM)
            {
                polygonGeometries.Add(Geometry<PointZM>.Create(rings.Cast<Geometry<PointZM>>().ToList(), GeometryType.Polygon, srid));
            }
            else if (hasZ)
            {
                polygonGeometries.Add(Geometry<PointZ>.Create(rings.Cast<Geometry<PointZ>>().ToList(), GeometryType.Polygon, srid));
            }
            else if (hasM)
            {
                polygonGeometries.Add(Geometry<PointM>.Create(rings.Cast<Geometry<PointM>>().ToList(), GeometryType.Polygon, srid));
            }
            else
            {
                polygonGeometries.Add(Geometry<Point>.Create(rings.Cast<Geometry<Point>>().ToList(), GeometryType.Polygon, srid));
            }
        }

        // Create appropriate MultiPolygon type based on Z/M flags
        if (hasZ && hasM)
        {
            return Geometry<PointZM>.Create(polygonGeometries.Cast<Geometry<PointZM>>().ToList(), GeometryType.MultiPolygon, srid);
        }
        else if (hasZ)
        {
            return Geometry<PointZ>.Create(polygonGeometries.Cast<Geometry<PointZ>>().ToList(), GeometryType.MultiPolygon, srid);
        }
        else if (hasM)
        {
            return Geometry<PointM>.Create(polygonGeometries.Cast<Geometry<PointM>>().ToList(), GeometryType.MultiPolygon, srid);
        }
        else
        {
            return Geometry<Point>.Create(polygonGeometries.Cast<Geometry<Point>>().ToList(), GeometryType.MultiPolygon, srid);
        }
    }

    /// <summary>
    /// Deserializes a GeometryCollection (OpenGIS type 7, [MS-SSCLRT] §2.1.4 / example §3.1.4). The collection's
    /// members share the whole structure's Points/Figures/Shapes arrays; the Shapes array forms a tree (each member
    /// shape's Parent Offset points at the collection shape). The tree is walked recursively so members may themselves
    /// be multi-geometries or nested collections.
    /// </summary>
    private static IGeometry DeserializeGeometryCollection(BinaryReader reader, int srid, bool hasZ, bool hasM, bool isGeography)
    {
        // Read Number of Points (total across all members)
        var pointCount = reader.ReadInt32();

        // Handle empty GeometryCollection
        if (pointCount == 0)
        {
            reader.ReadInt32(); // Number of Figures (0)
            var emptyShapeCount = reader.ReadInt32();
            for (int i = 0; i < emptyShapeCount; i++)
            {
                reader.ReadInt32(); // Parent Offset
                reader.ReadInt32(); // Figure Offset
                reader.ReadByte();  // OpenGIS Type
            }

            return Geometry<Point>.CreateEmpty(GeometryType.GeometryCollection, srid);
        }

        // Read all X, Y pairs (swapped for geography), then Z values, then M values
        var xs = new double[pointCount];
        var ys = new double[pointCount];
        for (int i = 0; i < pointCount; i++)
        {
            var (x, y) = ReadXY(reader, isGeography);
            xs[i] = x;
            ys[i] = y;
        }

        double[]? zs = null;
        if (hasZ)
        {
            zs = new double[pointCount];
            for (int i = 0; i < pointCount; i++)
                zs[i] = reader.ReadDouble();
        }

        double[]? ms = null;
        if (hasM)
        {
            ms = new double[pointCount];
            for (int i = 0; i < pointCount; i++)
                ms[i] = reader.ReadDouble();
        }

        // Read Figures (attribute is unused when reading; point offsets partition the Points sequence)
        var figureCount = reader.ReadInt32();
        var figurePointOffsets = new int[figureCount];
        for (int i = 0; i < figureCount; i++)
        {
            reader.ReadByte();                     // Figure Attribute
            figurePointOffsets[i] = reader.ReadInt32(); // Point Offset
        }

        // Read Shapes (parent offset, figure offset, OpenGIS type)
        var shapeCount = reader.ReadInt32();
        var shapeParentOffsets = new int[shapeCount];
        var shapeFigureOffsets = new int[shapeCount];
        var shapeOpenGisTypes = new byte[shapeCount];
        for (int i = 0; i < shapeCount; i++)
        {
            shapeParentOffsets[i] = reader.ReadInt32();
            shapeFigureOffsets[i] = reader.ReadInt32();
            shapeOpenGisTypes[i] = reader.ReadByte();
        }

        var context = new ShapeTreeContext(pointCount, figureCount, figurePointOffsets, shapeParentOffsets, shapeFigureOffsets, shapeOpenGisTypes);

        // The root (shape 0) is the collection itself. Build members with the point type implied by the Z/M flags.
        if (hasZ && hasM)
            return BuildShape(0, context, srid, i => new PointZM { X = xs[i], Y = ys[i], Z = zs![i], M = ms![i] });
        else if (hasZ)
            return BuildShape(0, context, srid, i => new PointZ { X = xs[i], Y = ys[i], Z = zs![i] });
        else if (hasM)
            return BuildShape(0, context, srid, i => new PointM { X = xs[i], Y = ys[i], M = ms![i] });
        else
            return BuildShape(0, context, srid, i => new Point(xs[i], ys[i]));
    }

    /// <summary>
    /// Immutable view over a decoded MS-SSCLRT Points/Figures/Shapes structure, shared across the recursive
    /// <see cref="BuildShape{T}"/> walk.
    /// </summary>
    private sealed class ShapeTreeContext
    {
        public ShapeTreeContext(int pointCount, int figureCount, int[] figurePointOffsets,
            int[] shapeParentOffsets, int[] shapeFigureOffsets, byte[] shapeOpenGisTypes)
        {
            PointCount = pointCount;
            FigureCount = figureCount;
            FigurePointOffsets = figurePointOffsets;
            ShapeParentOffsets = shapeParentOffsets;
            ShapeFigureOffsets = shapeFigureOffsets;
            ShapeOpenGisTypes = shapeOpenGisTypes;
        }

        public int PointCount { get; }
        public int FigureCount { get; }
        public int[] FigurePointOffsets { get; }
        public int[] ShapeParentOffsets { get; }
        public int[] ShapeFigureOffsets { get; }
        public byte[] ShapeOpenGisTypes { get; }
    }

    /// <summary>
    /// Recursively reconstructs the geometry rooted at <paramref name="shapeIndex"/> from a decoded shape tree.
    /// </summary>
    private static Geometry<T> BuildShape<T>(int shapeIndex, ShapeTreeContext ctx, int srid, Func<int, T> makePoint) where T : IPoint, new()
    {
        byte openGisType = ctx.ShapeOpenGisTypes[shapeIndex];

        switch (openGisType)
        {
            case 1: // Point
            {
                var figureIndex = ctx.ShapeFigureOffsets[shapeIndex];
                return Geometry<T>.Create(new List<T> { makePoint(ctx.FigurePointOffsets[figureIndex]) }, GeometryType.Point, srid);
            }
            case 2: // LineString
            {
                var figureIndex = ctx.ShapeFigureOffsets[shapeIndex];
                return Geometry<T>.Create(GetFigurePoints(ctx, figureIndex, makePoint, stripClosingPoint: false), GeometryType.LineString, srid);
            }
            case 3: // Polygon
            {
                var (firstFigure, endFigure) = GetShapeFigureRange(ctx, shapeIndex);
                var rings = new List<Geometry<T>>(endFigure - firstFigure);
                for (int f = firstFigure; f < endFigure; f++)
                    rings.Add(Geometry<T>.CreatePolygonRing(GetFigurePoints(ctx, f, makePoint, stripClosingPoint: true), srid));
                return Geometry<T>.Create(rings, GeometryType.Polygon, srid);
            }
            case 4: // MultiPoint
            case 5: // MultiLineString
            case 6: // MultiPolygon
            case 7: // GeometryCollection
            {
                var collectionType = openGisType switch
                {
                    4 => GeometryType.MultiPoint,
                    5 => GeometryType.MultiLineString,
                    6 => GeometryType.MultiPolygon,
                    _ => GeometryType.GeometryCollection
                };

                var members = new List<Geometry<T>>();
                for (int j = 0; j < ctx.ShapeParentOffsets.Length; j++)
                {
                    if (ctx.ShapeParentOffsets[j] == shapeIndex)
                        members.Add(BuildShape(j, ctx, srid, makePoint));
                }

                return Geometry<T>.Create(members, collectionType, srid);
            }
            default:
                throw new NotSupportedException($"OpenGIS type {openGisType} inside a GeometryCollection is not supported.");
        }
    }

    /// <summary>
    /// Returns the [firstFigure, endFigure) range of figures owned by a leaf shape. Figures are contiguous per leaf
    /// and laid out in shape order, so the range ends at the next larger shape figure offset (or the total figure count).
    /// </summary>
    private static (int firstFigure, int endFigure) GetShapeFigureRange(ShapeTreeContext ctx, int shapeIndex)
    {
        int firstFigure = ctx.ShapeFigureOffsets[shapeIndex];
        int endFigure = ctx.FigureCount;

        for (int j = 0; j < ctx.ShapeFigureOffsets.Length; j++)
        {
            int offset = ctx.ShapeFigureOffsets[j];
            if (offset > firstFigure && offset < endFigure)
                endFigure = offset;
        }

        return (firstFigure, endFigure);
    }

    /// <summary>
    /// Materializes the points of a single figure. Point offsets in the Figures array partition the Points sequence,
    /// so a figure's points run to the next figure's offset (or the total point count).
    /// </summary>
    private static List<T> GetFigurePoints<T>(ShapeTreeContext ctx, int figureIndex, Func<int, T> makePoint, bool stripClosingPoint) where T : IPoint, new()
    {
        int start = ctx.FigurePointOffsets[figureIndex];
        int end = (figureIndex + 1 < ctx.FigureCount) ? ctx.FigurePointOffsets[figureIndex + 1] : ctx.PointCount;

        // Rings are stored closed (last point repeats the first); Geometry<T> keeps rings open, so drop the duplicate.
        if (stripClosingPoint && end - start >= 2)
        {
            var first = makePoint(start);
            var last = makePoint(end - 1);
            if (first.X == last.X && first.Y == last.Y)
                end--;
        }

        var points = new List<T>(end - start);
        for (int k = start; k < end; k++)
            points.Add(makePoint(k));

        return points;
    }

}
