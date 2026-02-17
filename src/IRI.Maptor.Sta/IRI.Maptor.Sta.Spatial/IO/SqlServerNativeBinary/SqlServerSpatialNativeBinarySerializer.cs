using System;
using System.IO;
using System.Linq;
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Spatial.IO.OgcSFA;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Abstrations;

namespace IRI.Maptor.Sta.Spatial.IO.SqlServerNativeBinary;

public static partial class SqlServerSpatialNativeBinary
{

    public static byte[]? Serialize<T>(Geometry<T> geometry) where T : IPoint, new()
    {
        if (geometry == null)
            return null;

        bool hasZ = geometry.HasZ();
        bool hasM = geometry.HasM();

        using (var ms = new MemoryStream())
        using (var bw = new BinaryWriter(ms))
        {
            // Header: SRID + Version + Serialization Properties
            bw.Write(geometry.Srid);          // SRID (little-endian)
            bw.Write((byte)0x01);             // Version = 1

            // Handle geometry types
            if (geometry.Type == GeometryType.Point)
            {
                SerializePoint(bw, geometry, hasZ: hasZ, hasM: hasM);
            }
            else if (geometry.Type == GeometryType.LineString)
            {
                SerializeLineString(bw, geometry, hasZ: hasZ, hasM: hasM);
            }
            else if (geometry.Type == GeometryType.MultiPoint)
            {
                SerializeMultiPoint(bw, geometry, hasZ: hasZ, hasM: hasM);
            }
            else if (geometry.Type == GeometryType.MultiLineString)
            {
                SerializeMultiLineString(bw, geometry, hasZ: hasZ, hasM: hasM);
            }
            else if (geometry.Type == GeometryType.Polygon)
            {
                SerializePolygon(bw, geometry, hasZ: hasZ, hasM: hasM);
            }
            else if (geometry.Type == GeometryType.MultiPolygon)
            {
                SerializeMultiPolygon(bw, geometry, hasZ: hasZ, hasM: hasM);
            }
            else
            {
                throw new NotImplementedException($"Serialization for geometry type {geometry.Type} is not yet implemented");
            }

            return ms.ToArray();
        }
    }

    private static void SerializePoint<T>(BinaryWriter writer, Geometry<T> geometry, bool hasZ, bool hasM) where T : IPoint, new()
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
            writer.Write(point.X);
            writer.Write(point.Y);

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

    private static void SerializeLineString<T>(BinaryWriter writer, Geometry<T> geometry, bool hasZ, bool hasM) where T : IPoint, new()
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
            writer.Write(point1.X);
            writer.Write(point1.Y);
            writer.Write(point2.X);
            writer.Write(point2.Y);

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
                writer.Write(point.X);
                writer.Write(point.Y);
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

    private static void SerializeMultiPoint<T>(BinaryWriter writer, Geometry<T> geometry, bool hasZ, bool hasM) where T : IPoint, new()
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
            writer.Write(point.X);
            writer.Write(point.Y);
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

    private static void SerializeMultiLineString<T>(BinaryWriter writer, Geometry<T> geometry, bool hasZ, bool hasM) where T : IPoint, new()
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
            writer.Write(point.X);
            writer.Write(point.Y);
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

    private static void SerializePolygon<T>(BinaryWriter writer, Geometry<T> geometry, bool hasZ, bool hasM) where T : IPoint, new()
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

        foreach (var ringGeometry in geometry.Geometries)
        {
            if (ringGeometry.Type != GeometryType.LineString)
                throw new ArgumentException("Polygon geometry must contain only LineString geometries (rings)");

            if (ringGeometry.Points == null || ringGeometry.Points.Count == 0)
                throw new ArgumentException("Each ring geometry in Polygon must contain at least one point");

            var ringPoints = ringGeometry.Points;
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
            writer.Write(point.X);
            writer.Write(point.Y);
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

    private static void SerializeMultiPolygon<T>(BinaryWriter writer, Geometry<T> geometry, bool hasZ, bool hasM) where T : IPoint, new()
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

                var ringPoints = ringGeometry.Points;
                var firstPoint = ringPoints[0];
                var lastPoint = ringPoints[ringPoints.Count - 1];
                
                // SQL Server format requires explicit closing point (first point repeated)
                // Check if ring is already closed
                bool isAlreadyClosed = (firstPoint.X == lastPoint.X && firstPoint.Y == lastPoint.Y);
                
                // Determine ring type (exterior or interior)
                // First ring of each polygon is exterior, rest are interior
                bool isExteriorRing = (ringAttributes.Count == currentPolygonFirstFigureIndex);
                byte ringAttribute = isExteriorRing ? (byte)0x02 : (byte)0x00;
                ringAttributes.Add(ringAttribute);

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
            writer.Write(point.X);
            writer.Write(point.Y);
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

    private static void WriteDouble(BinaryWriter writer, double value)
    {
        // Write QNaN for NULL values
        if (double.IsNaN(value))
            writer.Write(BitConverter.Int64BitsToDouble(0x7FF8000000000000)); // QNaN

        else
            writer.Write(value);
    }



}
