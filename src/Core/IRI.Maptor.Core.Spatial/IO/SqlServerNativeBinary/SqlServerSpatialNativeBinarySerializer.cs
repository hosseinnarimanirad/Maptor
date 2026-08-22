using System;
using System.IO;
using System.Linq;
using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Spatial.IO.OgcSFA;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Common.Helpers;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Abstractions;

namespace IRI.Maptor.Core.Spatial.IO.SqlServerNativeBinary;

public static partial class SqlServerSpatialNativeBinary
{

    /// <summary>
    /// Serializes a <see cref="Geometry{T}"/> into the SQL Server native binary (MS-SSCLRT) format,
    /// suitable for storing directly into a SQL Server <c>geometry</c> or <c>geography</c> column.
    /// </summary>
    /// <param name="geometry">The geometry to serialize.</param>
    /// <param name="isGeography">
    /// When true, the output targets a SQL Server <c>geography</c> column: each point is written as
    /// (Latitude, Longitude) per [MS-SSCLRT] §2.1.5, polygon rings are oriented to the geography rule
    /// (exterior counter-clockwise / holes clockwise, §2.1.3), and an SRID of 0 is emitted as 4326
    /// (geography's default; §2.1.1 requires SRID 4120–4999). When false (default), the output targets a
    /// <c>geometry</c> column, storing (X, Y) with the SRID unchanged.
    /// </param>
    public static byte[]? Serialize<T>(Geometry<T> geometry, bool isGeography = false) where T : IPoint, new()
    {
        if (geometry == null)
            return null;

        bool hasZ = geometry.HasZ();
        bool hasM = geometry.HasM();

        // [MS-SSCLRT] §2.1.1: geography SRID must be in 4120-4999; default is 4326. A geometry with SRID 0
        // (unspecified) would be rejected as a geography, so substitute the geography default.
        int srid = (isGeography && geometry.Srid == 0) ? 4326 : geometry.Srid;

        using (var ms = new MemoryStream())
        using (var bw = new BinaryWriter(ms))
        {
            // Header: SRID + Version + Serialization Properties
            bw.Write(srid);                   // SRID (little-endian)
            bw.Write((byte)0x01);             // Version = 1

            // Handle geometry types
            if (geometry.Type == GeometryType.Point)
            {
                SerializePoint(bw, geometry, hasZ: hasZ, hasM: hasM, isGeography: isGeography);
            }
            else if (geometry.Type == GeometryType.LineString)
            {
                SerializeLineString(bw, geometry, hasZ: hasZ, hasM: hasM, isGeography: isGeography);
            }
            else if (geometry.Type == GeometryType.MultiPoint)
            {
                SerializeMultiPoint(bw, geometry, hasZ: hasZ, hasM: hasM, isGeography: isGeography);
            }
            else if (geometry.Type == GeometryType.MultiLineString)
            {
                SerializeMultiLineString(bw, geometry, hasZ: hasZ, hasM: hasM, isGeography: isGeography);
            }
            else if (geometry.Type == GeometryType.Polygon)
            {
                SerializePolygon(bw, geometry, hasZ: hasZ, hasM: hasM, isGeography: isGeography);
            }
            else if (geometry.Type == GeometryType.MultiPolygon)
            {
                SerializeMultiPolygon(bw, geometry, hasZ: hasZ, hasM: hasM, isGeography: isGeography);
            }
            else if (geometry.Type == GeometryType.GeometryCollection)
            {
                SerializeGeometryCollection(bw, geometry, hasZ: hasZ, hasM: hasM, isGeography: isGeography);
            }
            else
            {
                throw new NotImplementedException($"Serialization for geometry type {geometry.Type} is not yet implemented");
            }

            return ms.ToArray();
        }
    }

    /// <summary>
    /// Writes a single coordinate pair, swapping into (Latitude, Longitude) order for geography instances
    /// ([MS-SSCLRT] §2.1.5: geography stores Latitude then Longitude; §2.1.6: geometry stores X then Y).
    /// </summary>
    private static void WriteXY(BinaryWriter writer, IPoint point, bool isGeography)
    {
        if (isGeography)
        {
            writer.Write(point.Y); // Latitude
            writer.Write(point.X); // Longitude
        }
        else
        {
            writer.Write(point.X);
            writer.Write(point.Y);
        }
    }

    /// <summary>
    /// Returns the ring's points oriented per the geography ring rule ([MS-SSCLRT] §2.1.3): exterior rings
    /// counter-clockwise (left-hand rule), interior rings (holes) clockwise. For geometry (non-geography) the
    /// points are returned unchanged, since SQL Server geometry does not constrain ring orientation.
    /// </summary>
    private static List<T> OrientRingForGeography<T>(List<T> ringPoints, bool isExterior, bool isGeography) where T : IPoint
    {
        if (!isGeography || ringPoints.Count < 3)
            return ringPoints;

        bool isClockwise = SpatialUtility.IsClockwise(ringPoints);

        // exterior must be counter-clockwise; holes must be clockwise
        bool needsReverse = isExterior ? isClockwise : !isClockwise;

        if (!needsReverse)
            return ringPoints;

        var reversed = new List<T>(ringPoints);
        reversed.Reverse();
        return reversed;
    }

    private static void SerializePoint<T>(BinaryWriter writer, Geometry<T> geometry, bool hasZ, bool hasM, bool isGeography) where T : IPoint, new()
    {
        // Check if point is empty
        bool isEmpty = geometry.Points == null || geometry.Points.Count == 0;

        if (isEmpty)
        {
            // Empty point: Write serialization properties (V flag only, no P flag)
            byte serializationProps = (byte)SerializationProp.V;
            writer.Write(serializationProps);

            // Write Number of Points = 0
            writer.Write(0);

            // Write Number of Figures = 0
            writer.Write(0);

            // Write Number of Shapes = 1
            writer.Write(1);

            // Write Shape structure
            writer.Write(-1);              // Parent Offset (-1 means no parent)
            writer.Write(0);                // Figure Offset
            writer.Write((byte)1);          // OpenGIS Type (1 = Point)
        }
        else
        {
            // Non-empty single point: Use P flag optimization
            if (geometry.Points.Count != 1)
                throw new ArgumentException("Point geometry must contain exactly one point for P flag optimization");

            var point = geometry.Points[0];

            // Build serialization properties byte
            SerializationProp serializationProps = SerializationProp.V;  // V flag always set
            serializationProps |= SerializationProp.P;                   // P flag for optimization

            if (hasZ)
            {
                serializationProps |= SerializationProp.Z;
            }

            if (hasM)
            {
                serializationProps |= SerializationProp.M;
            }

            writer.Write((byte)serializationProps);

            // Write X, Y coordinates
            WriteXY(writer, point, isGeography);

            // Write Z coordinate if present
            if (hasZ && point is IHasZ zPoint)
            {
                double zValue = zPoint.Z;

                WriteDouble(writer, zValue);
            }

            // Write M coordinate if present
            if (hasM && point is IHasM mPoint)
            {
                double mValue = mPoint.M;

                WriteDouble(writer, mValue);
            }
        }
    }

    private static void SerializeLineString<T>(BinaryWriter writer, Geometry<T> geometry, bool hasZ, bool hasM, bool isGeography) where T : IPoint, new()
    {
        // Check if LineString is empty
        bool isEmpty = geometry.Points == null || geometry.Points.Count == 0;

        if (isEmpty)
        {
            // Empty LineString: Write serialization properties (V flag only, no L flag)
            byte serializationProps = (byte)SerializationProp.V;
            writer.Write(serializationProps);

            // Write Number of Points = 0
            writer.Write(0);

            // Write Number of Figures = 0
            writer.Write(0);

            // Write Number of Shapes = 1
            writer.Write(1);

            // Write Shape structure
            writer.Write(-1);              // Parent Offset (-1 means no parent)
            writer.Write(0);              // Figure Offset
            writer.Write((byte)2);        // OpenGIS Type (2 = LineString)

            return;
        }


        if (geometry.Points.Count == 2)
        {
            // Use L flag optimization for single line segment (2 points)
            var point1 = geometry.Points[0];
            var point2 = geometry.Points[1];

            // Build serialization properties byte
            SerializationProp serializationProps = SerializationProp.V;  // V flag always set
            serializationProps |= SerializationProp.L;                    // L flag for optimization

            if (hasZ) serializationProps |= SerializationProp.Z;

            if (hasM) serializationProps |= SerializationProp.M;
             
            writer.Write((byte)serializationProps);

            // Write 2 points (X, Y for each)
            WriteXY(writer, point1, isGeography);
            WriteXY(writer, point2, isGeography);

            // Write Z coordinates if present (2 values sequentially)
            if (hasZ && point1 is IHasZ zPoint1 && point2 is IHasZ zPoint2)
            {
                WriteDouble(writer, zPoint1.Z);
                WriteDouble(writer, zPoint2.Z);
            }

            // Write M coordinates if present (2 values sequentially)
            if (hasM && point1 is IHasM mPoint1 && point2 is IHasM mPoint2)
            {
                WriteDouble(writer, mPoint1.M);
                WriteDouble(writer, mPoint2.M);
            }
        }
        else
        {
            // Full LineString structure (multiple points)
            var pointCount = geometry.Points.Count;

            // Build serialization properties byte
            SerializationProp serializationProps = SerializationProp.V;  // V flag always set
            // No L flag for multi-point LineString

            if (hasZ)
            {
                serializationProps |= SerializationProp.Z;
            }

            if (hasM)
            {
                serializationProps |= SerializationProp.M;
            }

            writer.Write((byte)serializationProps);

            // Write Number of Points
            writer.Write(pointCount);

            // Write all X, Y pairs sequentially
            foreach (var point in geometry.Points)
            {
                WriteXY(writer, point, isGeography);
            }

            // Write all Z values sequentially if present
            if (hasZ)
            {
                foreach (var point in geometry.Points)
                {
                    if (point is IHasZ zPoint)
                    {
                        WriteDouble(writer, zPoint.Z);
                    }
                    else
                    {
                        WriteDouble(writer, double.NaN); // Write QNaN if point doesn't have Z
                    }
                }
            }

            // Write all M values sequentially if present
            if (hasM)
            {
                foreach (var point in geometry.Points)
                {
                    if (point is IHasM mPoint)
                    {
                        WriteDouble(writer, mPoint.M);
                    }
                    else
                    {
                        WriteDouble(writer, double.NaN); // Write QNaN if point doesn't have M
                    }
                }
            }

            // Write Number of Figures = 1
            writer.Write(1);

            // Write Figure structure
            writer.Write((byte)0x01);  // Figure Attribute (0x01 = stroke)
            writer.Write(0);           // Point Offset (0 for LineString starting at first point)

            // Write Number of Shapes = 1
            writer.Write(1);

            // Write Shape structure
            writer.Write(-1);          // Parent Offset (-1 means no parent)
            writer.Write(0);           // Figure Offset (0 for first figure)
            writer.Write((byte)2);    // OpenGIS Type (2 = LineString)
        }
    }

    private static void SerializeMultiPoint<T>(BinaryWriter writer, Geometry<T> geometry, bool hasZ, bool hasM, bool isGeography) where T : IPoint, new()
    {
        // Check if MultiPoint is empty
        bool isEmpty = geometry.Geometries == null || geometry.Geometries.Count == 0;

        if (isEmpty)
        {
            // Empty MultiPoint: Write serialization properties (V flag only)
            byte emptySerializationProps = (byte)SerializationProp.V;
            writer.Write(emptySerializationProps);

            // Write Number of Points = 0
            writer.Write(0);

            // Write Number of Figures = 0
            writer.Write(0);

            // Write Number of Shapes = 1
            writer.Write(1);

            // Write Shape structure
            writer.Write(-1);              // Parent Offset (-1 means no parent)
            writer.Write(0);               // Figure Offset
            writer.Write((byte)4);        // OpenGIS Type (4 = MultiPoint)
            return;
        }

        // Extract all points from Geometries (each geometry is a Point)
        var pointCount = geometry.Geometries.Count;
        var allPoints = new List<T>(pointCount);
        foreach (var pointGeometry in geometry.Geometries)
        {
            if (pointGeometry.Type != GeometryType.Point)
                throw new ArgumentException("MultiPoint geometry must contain only Point geometries");
            
            if (pointGeometry.Points == null || pointGeometry.Points.Count != 1)
                throw new ArgumentException("Each Point geometry in MultiPoint must contain exactly one point");

            allPoints.Add(pointGeometry.Points[0]);
        }

        // Build serialization properties byte
        SerializationProp serializationProps = SerializationProp.V;  // V flag always set
        // No P or L flag for MultiPoint

        if (hasZ)
        {
            serializationProps |= SerializationProp.Z;
        }

        if (hasM)
        {
            serializationProps |= SerializationProp.M;
        }

        writer.Write((byte)serializationProps);

        // Write Number of Points
        writer.Write(pointCount);

        // Write all X, Y pairs sequentially
        foreach (var point in allPoints)
        {
            WriteXY(writer, point, isGeography);
        }

        // Write all Z values sequentially if present
        if (hasZ)
        {
            foreach (var point in allPoints)
            {
                if (point is IHasZ zPoint)
                {
                    WriteDouble(writer, zPoint.Z);
                }
                else
                {
                    WriteDouble(writer, double.NaN); // Write QNaN if point doesn't have Z
                }
            }
        }

        // Write all M values sequentially if present
        if (hasM)
        {
            foreach (var point in allPoints)
            {
                if (point is IHasM mPoint)
                {
                    WriteDouble(writer, mPoint.M);
                }
                else
                {
                    WriteDouble(writer, double.NaN); // Write QNaN if point doesn't have M
                }
            }
        }

        // Write Number of Figures = point count (one figure per point)
        writer.Write(pointCount);

        // Write Figures section - one figure per point
        for (int i = 0; i < pointCount; i++)
        {
            writer.Write((byte)0x01);  // Figure Attribute (0x01 = stroke)
            writer.Write(i);           // Point Offset (index of the point)
        }

        // Write Number of Shapes = pointCount + 1 (one parent MultiPoint + one per point)
        writer.Write(pointCount + 1);

        // Write first shape: Parent MultiPoint shape
        writer.Write(-1);          // Parent Offset (-1 means no parent)
        writer.Write(0);          // Figure Offset (0 for MultiPoint)
        writer.Write((byte)0x04); // OpenGIS Type (0x04 = MultiPoint)

        // Write Shapes section - one shape per point (children of MultiPoint)
        for (int i = 0; i < pointCount; i++)
        {
            writer.Write(0);          // Parent Offset (0 = index of MultiPoint shape)
            writer.Write(i);          // Figure Offset (index of the figure)
            writer.Write((byte)0x01); // OpenGIS Type (0x01 = Point)
        }
    }

    private static void SerializeMultiLineString<T>(BinaryWriter writer, Geometry<T> geometry, bool hasZ, bool hasM, bool isGeography) where T : IPoint, new()
    {
        // Check if MultiLineString is empty
        bool isEmpty = geometry.Geometries == null || geometry.Geometries.Count == 0;

        if (isEmpty)
        {
            // Empty MultiLineString: Write serialization properties (V flag only)
            byte emptySerializationProps = (byte)SerializationProp.V;
            writer.Write(emptySerializationProps);

            // Write Number of Points = 0
            writer.Write(0);

            // Write Number of Figures = 0
            writer.Write(0);

            // Write Number of Shapes = 1
            writer.Write(1);

            // Write Shape structure
            writer.Write(-1);              // Parent Offset (-1 means no parent)
            writer.Write(0);               // Figure Offset
            writer.Write((byte)5);         // OpenGIS Type (5 = MultiLineString)
            return;
        }

        // Extract all points from Geometries (each geometry is a LineString)
        var lineStringCount = geometry.Geometries.Count;
        var allPoints = new List<T>();
        var lineStringPointCounts = new List<int>(lineStringCount);
        var cumulativeOffsets = new List<int>(lineStringCount + 1) { 0 };

        foreach (var lineStringGeometry in geometry.Geometries)
        {
            if (lineStringGeometry.Type != GeometryType.LineString)
                throw new ArgumentException("MultiLineString geometry must contain only LineString geometries");

            if (lineStringGeometry.Points == null || lineStringGeometry.Points.Count == 0)
                throw new ArgumentException("Each LineString geometry in MultiLineString must contain at least one point");

            var pointCount = lineStringGeometry.Points.Count;
            lineStringPointCounts.Add(pointCount);
            cumulativeOffsets.Add(cumulativeOffsets[cumulativeOffsets.Count - 1] + pointCount);

            foreach (var point in lineStringGeometry.Points)
            {
                allPoints.Add(point);
            }
        }

        var totalPointCount = allPoints.Count;

        // Build serialization properties byte
        SerializationProp serializationProps = SerializationProp.V;  // V flag always set
        // No P or L flag for MultiLineString

        if (hasZ)
        {
            serializationProps |= SerializationProp.Z;
        }

        if (hasM)
        {
            serializationProps |= SerializationProp.M;
        }

        writer.Write((byte)serializationProps);

        // Write Number of Points
        writer.Write(totalPointCount);

        // Write all X, Y pairs sequentially (from all LineStrings)
        foreach (var point in allPoints)
        {
            WriteXY(writer, point, isGeography);
        }

        // Write all Z values sequentially if present
        if (hasZ)
        {
            foreach (var point in allPoints)
            {
                if (point is IHasZ zPoint)
                {
                    WriteDouble(writer, zPoint.Z);
                }
                else
                {
                    WriteDouble(writer, double.NaN); // Write QNaN if point doesn't have Z
                }
            }
        }

        // Write all M values sequentially if present
        if (hasM)
        {
            foreach (var point in allPoints)
            {
                if (point is IHasM mPoint)
                {
                    WriteDouble(writer, mPoint.M);
                }
                else
                {
                    WriteDouble(writer, double.NaN); // Write QNaN if point doesn't have M
                }
            }
        }

        // Write Number of Figures = LineString count (one figure per LineString)
        writer.Write(lineStringCount);

        // Write Figures section - one figure per LineString
        for (int i = 0; i < lineStringCount; i++)
        {
            writer.Write((byte)0x01);  // Figure Attribute (0x01 = stroke)
            writer.Write(cumulativeOffsets[i]); // Point Offset (cumulative point index)
        }

        // Write Number of Shapes = LineString count + 1 (one parent + one per LineString)
        writer.Write(lineStringCount + 1);

        // Write first shape: Parent MultiLineString shape
        writer.Write(-1);          // Parent Offset (-1 means no parent)
        writer.Write(0);          // Figure Offset (0 for MultiLineString)
        writer.Write((byte)0x05); // OpenGIS Type (0x05 = MultiLineString)

        // Write Shapes section - one shape per LineString (children of MultiLineString)
        for (int i = 0; i < lineStringCount; i++)
        {
            writer.Write(0);          // Parent Offset (0 = index of MultiLineString shape)
            writer.Write(i);          // Figure Offset (index of the figure)
            writer.Write((byte)0x02); // OpenGIS Type (0x02 = LineString)
        }
    }

    private static void SerializePolygon<T>(BinaryWriter writer, Geometry<T> geometry, bool hasZ, bool hasM, bool isGeography) where T : IPoint, new()
    {
        // Check if Polygon is empty
        bool isEmpty = geometry.Geometries == null || geometry.Geometries.Count == 0;

        if (isEmpty)
        {
            // Empty Polygon: Write serialization properties (V flag only)
            byte emptySerializationProps = (byte)SerializationProp.V;
            writer.Write(emptySerializationProps);

            // Write Number of Points = 0
            writer.Write(0);

            // Write Number of Figures = 0
            writer.Write(0);

            // Write Number of Shapes = 1
            writer.Write(1);

            // Write Shape structure
            writer.Write(-1);              // Parent Offset (-1 means no parent)
            writer.Write(0);               // Figure Offset
            writer.Write((byte)3);         // OpenGIS Type (3 = Polygon)
            return;
        }

        // Extract rings from Geometries
        // First geometry is exterior ring, remaining geometries are interior rings (holes)
        var ringCount = geometry.Geometries.Count;
        var allPoints = new List<T>();
        var ringPointCounts = new List<int>(ringCount);
        var cumulativeOffsets = new List<int>(ringCount + 1) { 0 };

        for (int ringIndex = 0; ringIndex < ringCount; ringIndex++)
        {
            var ringGeometry = geometry.Geometries[ringIndex];

            if (ringGeometry.Type != GeometryType.LineString)
                throw new ArgumentException("Polygon geometry must contain only LineString geometries (rings)");

            if (ringGeometry.Points == null || ringGeometry.Points.Count == 0)
                throw new ArgumentException("Each ring geometry in Polygon must contain at least one point");

            // Geography requires exterior rings counter-clockwise and holes clockwise ([MS-SSCLRT] §2.1.3);
            // reorient here so writes succeed regardless of the source winding. No-op for geometry.
            var ringPoints = OrientRingForGeography(ringGeometry.Points, isExterior: ringIndex == 0, isGeography);
            var firstPoint = ringPoints[0];
            var lastPoint = ringPoints[ringPoints.Count - 1];
            
            // SQL Server format requires explicit closing point (first point repeated)
            // Check if ring is already closed
            bool isAlreadyClosed = (firstPoint.X == lastPoint.X && firstPoint.Y == lastPoint.Y);
            
            // Count includes closing point
            var pointCount = isAlreadyClosed ? ringPoints.Count : ringPoints.Count + 1;
            ringPointCounts.Add(pointCount);
            cumulativeOffsets.Add(cumulativeOffsets[cumulativeOffsets.Count - 1] + pointCount);

            // Add all points
            foreach (var point in ringPoints)
            {
                allPoints.Add(point);
            }
            
            // Add closing point if not already closed
            if (!isAlreadyClosed)
            {
                allPoints.Add(firstPoint);
            }
        }

        var totalPointCount = allPoints.Count;

        // Build serialization properties byte
        SerializationProp serializationProps = SerializationProp.V;  // V flag always set
        // No P or L flag for Polygon

        if (hasZ)
        {
            serializationProps |= SerializationProp.Z;
        }

        if (hasM)
        {
            serializationProps |= SerializationProp.M;
        }

        writer.Write((byte)serializationProps);

        // Write Number of Points
        writer.Write(totalPointCount);

        // Write all X, Y pairs sequentially (from all rings)
        foreach (var point in allPoints)
        {
            WriteXY(writer, point, isGeography);
        }

        // Write all Z values sequentially if present
        if (hasZ)
        {
            foreach (var point in allPoints)
            {
                if (point is IHasZ zPoint)
                {
                    WriteDouble(writer, zPoint.Z);
                }
                else
                {
                    WriteDouble(writer, double.NaN); // Write QNaN if point doesn't have Z
                }
            }
        }

        // Write all M values sequentially if present
        if (hasM)
        {
            foreach (var point in allPoints)
            {
                if (point is IHasM mPoint)
                {
                    WriteDouble(writer, mPoint.M);
                }
                else
                {
                    WriteDouble(writer, double.NaN); // Write QNaN if point doesn't have M
                }
            }
        }

        // Write Number of Figures = ring count (one figure per ring)
        writer.Write(ringCount);

        // Write Figures section - one figure per ring
        for (int i = 0; i < ringCount; i++)
        {
            if (i == 0)
            {
                // Exterior ring
                writer.Write((byte)0x02);  // Figure Attribute (0x02 = exterior ring)
            }
            else
            {
                // Interior ring
                writer.Write((byte)0x00);  // Figure Attribute (0x00 = interior ring)
            }
            writer.Write(cumulativeOffsets[i]); // Point Offset (cumulative point index)
        }

        // Write Number of Shapes = 1
        writer.Write(1);

        // Write Shapes section - one shape for Polygon
        writer.Write(-1);          // Parent Offset (-1 means no parent)
        writer.Write(0);          // Figure Offset (0 for exterior ring)
        writer.Write((byte)0x03); // OpenGIS Type (0x03 = Polygon)
    }

    private static void SerializeMultiPolygon<T>(BinaryWriter writer, Geometry<T> geometry, bool hasZ, bool hasM, bool isGeography) where T : IPoint, new()
    {
        // Check if MultiPolygon is empty
        bool isEmpty = geometry.Geometries == null || geometry.Geometries.Count == 0;

        if (isEmpty)
        {
            // Empty MultiPolygon: Write serialization properties (V flag only)
            byte emptySerializationProps = (byte)SerializationProp.V;
            writer.Write(emptySerializationProps);

            // Write Number of Points = 0
            writer.Write(0);

            // Write Number of Figures = 0
            writer.Write(0);

            // Write Number of Shapes = 1
            writer.Write(1);

            // Write Shape structure
            writer.Write(-1);              // Parent Offset (-1 means no parent)
            writer.Write(0);               // Figure Offset
            writer.Write((byte)6);         // OpenGIS Type (6 = MultiPolygon)
            return;
        }

        // Extract all rings from all polygons
        // Each geometry in Geometries is a Polygon
        // Each Polygon's rings are in its Geometries property (Geometries[0] = exterior, Geometries[1..] = interior)
        var polygonCount = geometry.Geometries.Count;
        var allPoints = new List<T>();
        var ringAttributes = new List<byte>(); // 0x02 for exterior, 0x00 for interior
        var cumulativePointOffsets = new List<int>() { 0 };
        var polygonFirstFigureIndices = new List<int>(polygonCount + 1) { 0 }; // First figure index for each polygon (index 0 is always 0)

        foreach (var polygonGeometry in geometry.Geometries)
        {
            if (polygonGeometry.Type != GeometryType.Polygon)
                throw new ArgumentException("MultiPolygon geometry must contain only Polygon geometries");

            if (polygonGeometry.Geometries == null || polygonGeometry.Geometries.Count == 0)
                throw new ArgumentException("Each Polygon geometry in MultiPolygon must contain at least one ring");

            // Store the first figure index for this polygon (before processing its rings)
            int currentPolygonFirstFigureIndex = ringAttributes.Count;
            polygonFirstFigureIndices.Add(currentPolygonFirstFigureIndex);

            // Process each ring in this polygon
            foreach (var ringGeometry in polygonGeometry.Geometries)
            {
                if (ringGeometry.Type != GeometryType.LineString)
                    throw new ArgumentException("Polygon geometry must contain only LineString geometries (rings)");

                if (ringGeometry.Points == null || ringGeometry.Points.Count == 0)
                    throw new ArgumentException("Each ring geometry in Polygon must contain at least one point");

                // Determine ring type (exterior or interior) up front so geography orientation can use it.
                // First ring of each polygon is exterior, rest are interior.
                bool isExteriorRing = (ringAttributes.Count == currentPolygonFirstFigureIndex);
                byte ringAttribute = isExteriorRing ? (byte)0x02 : (byte)0x00;
                ringAttributes.Add(ringAttribute);

                // Geography requires exterior rings CCW / holes CW ([MS-SSCLRT] §2.1.3); reorient. No-op for geometry.
                var ringPoints = OrientRingForGeography(ringGeometry.Points, isExteriorRing, isGeography);
                var firstPoint = ringPoints[0];
                var lastPoint = ringPoints[ringPoints.Count - 1];

                // SQL Server format requires explicit closing point (first point repeated)
                // Check if ring is already closed
                bool isAlreadyClosed = (firstPoint.X == lastPoint.X && firstPoint.Y == lastPoint.Y);

                // Count includes closing point
                var pointCount = isAlreadyClosed ? ringPoints.Count : ringPoints.Count + 1;
                cumulativePointOffsets.Add(cumulativePointOffsets[cumulativePointOffsets.Count - 1] + pointCount);

                // Add all points
                foreach (var point in ringPoints)
                {
                    allPoints.Add(point);
                }
                
                // Add closing point if not already closed
                if (!isAlreadyClosed)
                {
                    allPoints.Add(firstPoint);
                }
            }
        }

        var totalPointCount = allPoints.Count;
        var totalRingCount = ringAttributes.Count;

        // Build serialization properties byte
        SerializationProp serializationProps = SerializationProp.V;  // V flag always set
        // No P or L flag for MultiPolygon

        if (hasZ)
        {
            serializationProps |= SerializationProp.Z;
        }

        if (hasM)
        {
            serializationProps |= SerializationProp.M;
        }

        writer.Write((byte)serializationProps);

        // Write Number of Points
        writer.Write(totalPointCount);

        // Write all X, Y pairs sequentially (from all rings of all polygons)
        foreach (var point in allPoints)
        {
            WriteXY(writer, point, isGeography);
        }

        // Write all Z values sequentially if present
        if (hasZ)
        {
            foreach (var point in allPoints)
            {
                if (point is IHasZ zPoint)
                {
                    WriteDouble(writer, zPoint.Z);
                }
                else
                {
                    WriteDouble(writer, double.NaN); // Write QNaN if point doesn't have Z
                }
            }
        }

        // Write all M values sequentially if present
        if (hasM)
        {
            foreach (var point in allPoints)
            {
                if (point is IHasM mPoint)
                {
                    WriteDouble(writer, mPoint.M);
                }
                else
                {
                    WriteDouble(writer, double.NaN); // Write QNaN if point doesn't have M
                }
            }
        }

        // Write Number of Figures = total ring count (one figure per ring)
        writer.Write(totalRingCount);

        // Write Figures section - one figure per ring
        for (int i = 0; i < totalRingCount; i++)
        {
            writer.Write(ringAttributes[i]);  // Figure Attribute (0x02 for exterior, 0x00 for interior)
            writer.Write(cumulativePointOffsets[i]); // Point Offset (cumulative point index)
        }

        // Write Number of Shapes = polygonCount + 1 (one parent + one per Polygon)
        writer.Write(polygonCount + 1);

        // Write first shape: Parent MultiPolygon shape
        writer.Write(-1);          // Parent Offset (-1 means no parent)
        writer.Write(0);          // Figure Offset (0 for MultiPolygon)
        writer.Write((byte)0x06); // OpenGIS Type (0x06 = MultiPolygon)

        // Write Shapes section - one shape per Polygon (children of MultiPolygon)
        for (int i = 0; i < polygonCount; i++)
        {
            writer.Write(0);                      // Parent Offset (0 = index of MultiPolygon shape)
            writer.Write(polygonFirstFigureIndices[i + 1]); // Figure Offset (first figure index of this polygon, index 0 is always 0)
            writer.Write((byte)0x03);             // OpenGIS Type (0x03 = Polygon)
        }
    }

    private static void SerializeGeometryCollection<T>(BinaryWriter writer, Geometry<T> geometry, bool hasZ, bool hasM, bool isGeography) where T : IPoint, new()
    {
        // Flatten the collection tree into the shared Points/Figures/Shapes arrays ([MS-SSCLRT] §2.1.4).
        var points = new List<T>();
        var figureAttributes = new List<byte>();
        var figurePointOffsets = new List<int>();
        var shapes = new List<(int parentOffset, int figureOffset, byte openGisType)>();

        AppendShape(geometry, parentShapeIndex: -1, points, figureAttributes, figurePointOffsets, shapes, isGeography);

        // Serialization properties (no P/L optimization for collections)
        SerializationProp serializationProps = SerializationProp.V;
        if (hasZ) serializationProps |= SerializationProp.Z;
        if (hasM) serializationProps |= SerializationProp.M;
        writer.Write((byte)serializationProps);

        // Points
        writer.Write(points.Count);
        foreach (var point in points)
            WriteXY(writer, point, isGeography);

        if (hasZ)
        {
            foreach (var point in points)
                WriteDouble(writer, point is IHasZ zPoint ? zPoint.Z : double.NaN);
        }

        if (hasM)
        {
            foreach (var point in points)
                WriteDouble(writer, point is IHasM mPoint ? mPoint.M : double.NaN);
        }

        // Figures
        writer.Write(figureAttributes.Count);
        for (int i = 0; i < figureAttributes.Count; i++)
        {
            writer.Write(figureAttributes[i]);
            writer.Write(figurePointOffsets[i]);
        }

        // Shapes
        writer.Write(shapes.Count);
        foreach (var shape in shapes)
        {
            writer.Write(shape.parentOffset);
            writer.Write(shape.figureOffset);
            writer.Write(shape.openGisType);
        }
    }

    /// <summary>
    /// Recursively appends a geometry (and its descendants) to the flat Points/Figures/Shapes buffers used by the
    /// GeometryCollection serializer. The shape is added before its descendants so its Figure Offset correctly points
    /// at the first figure of its subtree ([MS-SSCLRT] §2.1.4 / §3.1.4).
    /// </summary>
    private static void AppendShape<T>(
        Geometry<T> geometry,
        int parentShapeIndex,
        List<T> points,
        List<byte> figureAttributes,
        List<int> figurePointOffsets,
        List<(int parentOffset, int figureOffset, byte openGisType)> shapes,
        bool isGeography) where T : IPoint, new()
    {
        int myIndex = shapes.Count;
        byte openGisType = ToOpenGisType(geometry.Type);

        // Figure Offset = index of the first figure this shape (or its subtree) will contribute.
        shapes.Add((parentShapeIndex, figurePointOffsets.Count, openGisType));

        switch (geometry.Type)
        {
            case GeometryType.Point:
                figureAttributes.Add(0x01); // stroke
                figurePointOffsets.Add(points.Count);
                points.Add(geometry.Points[0]);
                break;

            case GeometryType.LineString:
                figureAttributes.Add(0x01); // stroke
                figurePointOffsets.Add(points.Count);
                points.AddRange(geometry.Points);
                break;

            case GeometryType.Polygon:
                for (int r = 0; r < geometry.Geometries.Count; r++)
                {
                    var ring = OrientRingForGeography(geometry.Geometries[r].Points, isExterior: r == 0, isGeography);
                    figureAttributes.Add(r == 0 ? (byte)0x02 : (byte)0x00); // exterior / interior ring
                    figurePointOffsets.Add(points.Count);
                    AppendRingWithClosure(points, ring);
                }
                break;

            case GeometryType.MultiPoint:
            case GeometryType.MultiLineString:
            case GeometryType.MultiPolygon:
            case GeometryType.GeometryCollection:
                foreach (var member in geometry.Geometries)
                    AppendShape(member, myIndex, points, figureAttributes, figurePointOffsets, shapes, isGeography);
                break;

            default:
                throw new NotSupportedException($"Serialization of {geometry.Type} inside a GeometryCollection is not supported.");
        }
    }

    /// <summary>
    /// Appends a polygon ring's points, adding the explicit closing point (first point repeated) the SQL Server
    /// format requires when the ring is not already closed.
    /// </summary>
    private static void AppendRingWithClosure<T>(List<T> points, List<T> ring) where T : IPoint
    {
        points.AddRange(ring);

        if (ring.Count > 0)
        {
            var first = ring[0];
            var last = ring[ring.Count - 1];
            if (!(first.X == last.X && first.Y == last.Y))
                points.Add(first);
        }
    }

    private static byte ToOpenGisType(GeometryType type) => type switch
    {
        GeometryType.Point => 0x01,
        GeometryType.LineString => 0x02,
        GeometryType.Polygon => 0x03,
        GeometryType.MultiPoint => 0x04,
        GeometryType.MultiLineString => 0x05,
        GeometryType.MultiPolygon => 0x06,
        GeometryType.GeometryCollection => 0x07,
        _ => throw new NotSupportedException($"Geometry type {type} has no MS-SSCLRT OpenGIS type mapping.")
    };

    private static void WriteDouble(BinaryWriter writer, double value)
    {
        // Write QNaN for NULL values
        if (double.IsNaN(value))
            writer.Write(BitConverter.Int64BitsToDouble(0x7FF8000000000000)); // QNaN

        else
            writer.Write(value);
    }



}
