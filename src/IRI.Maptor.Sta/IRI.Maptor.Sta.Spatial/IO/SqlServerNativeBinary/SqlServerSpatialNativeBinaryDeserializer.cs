using System;
using System.Linq;
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Primitives;

namespace IRI.Maptor.Sta.Spatial.IO;

public static partial class SqlServerSpatialNativeBinary
{ 
    // Determine geometry type from serialization properties and structure
    private static SqlServerSpatialNativeBinaryTypes DetermineGeometryTypeFromProps(byte serializationProps, BinaryReader reader)
    {
        bool hasZ = (serializationProps & 0x01) != 0;
        bool hasM = (serializationProps & 0x02) != 0;
        bool isPoint = (serializationProps & 0x08) != 0; // P bit
        bool isLineSegment = (serializationProps & 0x10) != 0; // L bit
        
        // Handle optimized cases (P and L bits) before reading counts
        if (isPoint)
        {
            return SqlServerSpatialNativeBinaryTypes.Point;
        }
        
        if (isLineSegment)
        {
            return SqlServerSpatialNativeBinaryTypes.LineString;
        }
        
        // Read number of points to determine structure
        var position = reader.BaseStream.Position;
        var pointCount = reader.ReadInt32();
        
        // Determine point size based on Z/M flags
        int pointSize = 16; // X, Y
        if (hasZ && !hasM) pointSize = 24; // X, Y, Z
        else if (hasZ && hasM) pointSize = 32; // X, Y, Z, M
        else if (!hasZ && hasM) pointSize = 24; // X, Y, M
        
        // Read ahead to check Figures and Shapes structure
        var pointsEndPosition = position + 4 + (pointCount * pointSize);
        reader.BaseStream.Position = pointsEndPosition;
        
        // Read Figures section
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
            reader.ReadInt32(); // Parent offset
            reader.ReadInt32(); // Figure offset
            var openGisType = reader.ReadByte(); // OpenGIS Type
            
            // Reset position
            reader.BaseStream.Position = position;
            
            // Determine type from OpenGIS type
            return DetermineTypeFromOpenGisType(openGisType, serializationProps);
        }
        
        // Reset position
        reader.BaseStream.Position = position;
        
        // Default: try to infer from point count (fallback)
        if (pointCount == 1)
        {
            return SqlServerSpatialNativeBinaryTypes.Point;
        }
        
        // Default to LineString for multiple points
        return SqlServerSpatialNativeBinaryTypes.LineString;
    }
    
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
     
    public static Geometry<Point> Deserialize(byte[] nativeBinary)
    {
        if (nativeBinary.IsNullOrEmpty())
            return null;

        using (var stream = new BinaryReader(new MemoryStream(nativeBinary)))
        {
            // Header: SRID + Version + Serialization Properties
            var srid = stream.ReadInt32();
            var version = stream.ReadByte();
            var serializationProps = stream.ReadByte();
            
            // Determine type from serialization properties and structure
            // For simple geometries, we can determine type from serialization props
            // But for complex ones (0x04, 0x05, 0x07), we need to peek at structure
            var type = DetermineGeometryTypeFromProps(serializationProps, stream);

            switch (type)
            {
                case SqlServerSpatialNativeBinaryTypes.Point:
                    return ParsePoint(stream, srid, serializationProps);

                case SqlServerSpatialNativeBinaryTypes.LineString:
                    return ParseLineString(stream, srid, serializationProps);

                case SqlServerSpatialNativeBinaryTypes.Polygon:
                    return ParsePolygon(stream, srid, serializationProps);

                case SqlServerSpatialNativeBinaryTypes.MultiPoint:
                    return ParseMultiPoint(stream, srid, serializationProps);

                case SqlServerSpatialNativeBinaryTypes.MultiLineString:
                    return ParseMultiLineString(stream, srid, serializationProps);

                case SqlServerSpatialNativeBinaryTypes.MultiPolygon:
                    return ParseMultiPolygon(stream, srid, serializationProps);
                    
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

    private static Geometry<Point> ParsePoint(BinaryReader reader, int srid, byte serializationProps)
    {
        bool hasZ = (serializationProps & 0x01) != 0;
        bool hasM = (serializationProps & 0x02) != 0;
        bool isPointOptimized = (serializationProps & 0x08) != 0; // P bit
        
        if (isPointOptimized)
        {
            // When P bit is set, Number of Points, Figures, and Shapes are omitted
            // Read point coordinates directly
            var optimizedX = reader.ReadDouble();
            var optimizedY = reader.ReadDouble();
            if (hasZ) reader.ReadDouble(); // Skip Z
            if (hasM) reader.ReadDouble(); // Skip M
            return Geometry<Point>.Create(optimizedX, optimizedY, srid);
        }
        
        // Points section: Number of Points + Points
        var pointCount = reader.ReadInt32();
        if (pointCount == 0)
        {
            // Empty point - read through Figures and Shapes sections
            var emptyFigureCount = reader.ReadInt32();
            var emptyShapeCount = reader.ReadInt32();
            if (emptyShapeCount > 0)
            {
                reader.ReadInt32(); // Parent offset
                reader.ReadInt32(); // Figure offset
                reader.ReadByte(); // OpenGIS type
            }
            return Geometry<Point>.CreateEmpty(GeometryType.Point, srid);
        }
        
        // Read point coordinates
        var x = reader.ReadDouble();
        var y = reader.ReadDouble();
        if (hasZ) reader.ReadDouble(); // Skip Z
        if (hasM) reader.ReadDouble(); // Skip M
        
        // Read Figures section
        var figureCount = reader.ReadInt32();
        if (figureCount > 0)
        {
            reader.ReadByte(); // Figure attribute
            reader.ReadInt32(); // Figure point offset
        }
        
        // Read Shapes section
        var shapeCount = reader.ReadInt32();
        if (shapeCount > 0)
        {
            reader.ReadInt32(); // Parent offset
            reader.ReadInt32(); // Figure offset
            reader.ReadByte(); // OpenGIS type
        }

        return Geometry<Point>.Create(x, y, srid);
    }

    private static Geometry<Point> ParseLineString(BinaryReader reader, int srid, byte serializationProps)
    {
        bool hasZ = (serializationProps & 0x01) != 0;
        bool hasM = (serializationProps & 0x02) != 0;
        
        // Points section: Number of Points + All Points (sequential: (X,Y) pairs, then all Z, then all M)
        var pointCount = reader.ReadInt32();
        var points = new List<Point>(pointCount);

        // Read points as (X, Y) pairs sequentially
        for (int i = 0; i < pointCount; i++)
        {
            var x = reader.ReadDouble();
            var y = reader.ReadDouble();
            points.Add(new Point(x, y));
        }
        
        // Skip all Z coordinates (if hasZ)
        if (hasZ)
        {
            for (int i = 0; i < pointCount; i++)
            {
                reader.ReadDouble(); // Skip Z
            }
        }
        
        // Skip all M coordinates (if hasM)
        if (hasM)
        {
            for (int i = 0; i < pointCount; i++)
            {
                reader.ReadDouble(); // Skip M
            }
        }
        
        // Figures section: Number of Figures + (Attribute + Point Offset) for each figure
        var figureCount = reader.ReadInt32();
        if (figureCount > 0)
        {
            reader.ReadByte(); // Figure attribute
            reader.ReadInt32(); // Figure point offset
        }
        
        // Shapes section: Number of Shapes + (Parent Offset + Figure Offset + OpenGIS Type) for each shape
        var shapeCount = reader.ReadInt32();
        if (shapeCount > 0)
        {
            reader.ReadInt32(); // Parent offset
            reader.ReadInt32(); // Figure offset
            reader.ReadByte(); // OpenGIS type
        }

        return Geometry<Point>.CreatePointOrLineString(points, srid);
    }

    private static Geometry<Point> ParsePolygon(BinaryReader reader, int srid, byte serializationProps)
    {
        bool hasZ = (serializationProps & 0x01) != 0;
        bool hasM = (serializationProps & 0x02) != 0;
        
        // Points section: Number of Points + All Points (sequential: (X,Y) pairs, then all Z, then all M)
        var totalPointCount = reader.ReadInt32();
        
        // Read points as (X, Y) pairs sequentially
        var allPoints = new List<Point>(totalPointCount);
        for (int i = 0; i < totalPointCount; i++)
        {
            var x = reader.ReadDouble();
            var y = reader.ReadDouble();
            allPoints.Add(new Point(x, y));
        }
        
        // Skip all Z coordinates (if hasZ)
        if (hasZ)
        {
            for (int i = 0; i < totalPointCount; i++)
            {
                reader.ReadDouble(); // Skip Z
            }
        }
        
        // Skip all M coordinates (if hasM)
        if (hasM)
        {
            for (int i = 0; i < totalPointCount; i++)
            {
                reader.ReadDouble(); // Skip M
            }
        }

        // Figures section: Number of Figures + (Attribute + Point Offset) for each figure
        var figureCount = reader.ReadInt32();
        var figureOffsets = new List<int>(figureCount);
        for (int i = 0; i < figureCount; i++)
        {
            reader.ReadByte(); // Figure attribute (1 for exterior, 0 for interior)
            var pointOffset = reader.ReadInt32(); // Figure Point Offset
            figureOffsets.Add(pointOffset);
        }
        
        // Shapes section: Number of Shapes + (Parent Offset + Figure Offset + OpenGIS Type) for each shape
        var shapeCount = reader.ReadInt32();
        if (shapeCount > 0)
        {
            reader.ReadInt32(); // Parent offset
            reader.ReadInt32(); // Figure offset
            reader.ReadByte(); // OpenGIS type
        }

        // Split points into rings based on figure offsets
        var rings = new List<Geometry<Point>>(figureCount);
        for (int i = 0; i < figureCount; i++)
        {
            var startOffset = figureOffsets[i];
            var endOffset = (i < figureCount - 1) ? figureOffsets[i + 1] : totalPointCount;
            var pointsInRing = endOffset - startOffset;
            
            var ringPoints = new List<Point>(pointsInRing);
            for (int j = 0; j < pointsInRing && (startOffset + j) < allPoints.Count; j++)
            {
                ringPoints.Add(allPoints[startOffset + j]);
            }
            rings.Add(Geometry<Point>.CreatePointOrLineString(ringPoints, srid));
        }

        return Geometry<Point>.CreatePolygonOrMultiPolygon(rings, srid);
    }

    private static Geometry<Point> ParseMultiLineString(BinaryReader reader, int srid, byte serializationProps)
    {
        bool hasZ = (serializationProps & 0x01) != 0;
        bool hasM = (serializationProps & 0x02) != 0;
        
        var totalPointCount = reader.ReadInt32();
        
        // Read all points as (X, Y) pairs sequentially
        var allPoints = new List<Point>(totalPointCount);
        for (int i = 0; i < totalPointCount; i++)
        {
            var x = reader.ReadDouble();
            var y = reader.ReadDouble();
            allPoints.Add(new Point(x, y));
        }
        
        // Skip all Z coordinates (if hasZ)
        if (hasZ)
        {
            for (int i = 0; i < totalPointCount; i++)
            {
                reader.ReadDouble(); // Skip Z
            }
        }
        
        // Skip all M coordinates (if hasM)
        if (hasM)
        {
            for (int i = 0; i < totalPointCount; i++)
            {
                reader.ReadDouble(); // Skip M
            }
        }

        // Read Figures section: Number of Figures + (Attribute + Point Offset) for each figure
        var figureCount = reader.ReadInt32();
        var figureOffsets = new List<int>(figureCount);
        for (int i = 0; i < figureCount; i++)
        {
            reader.ReadByte(); // Figure attribute (should be 1 for stroke/linestring)
            var pointOffset = reader.ReadInt32(); // Figure Point Offset
            figureOffsets.Add(pointOffset);
        }
        
        // Read Shapes section: Number of Shapes + (Parent Offset + Figure Offset + OpenGIS Type) for each shape
        var shapeCount = reader.ReadInt32();
        var shapes = new List<(int parentOffset, int figureOffset, byte openGisType)>(shapeCount);
        for (int i = 0; i < shapeCount; i++)
        {
            var parentOffset = reader.ReadInt32();
            var figureOffset = reader.ReadInt32();
            var openGisType = reader.ReadByte();
            shapes.Add((parentOffset, figureOffset, openGisType));
        }

        // Build linestrings from figures
        // Each figure represents a linestring
        var linestrings = new List<Geometry<Point>>(figureCount);
        for (int i = 0; i < figureCount; i++)
        {
            var startOffset = figureOffsets[i];
            var endOffset = (i < figureCount - 1) ? figureOffsets[i + 1] : totalPointCount;
            var pointsInLinestring = endOffset - startOffset;
            
            var linestringPoints = new List<Point>(pointsInLinestring);
            for (int j = 0; j < pointsInLinestring && (startOffset + j) < allPoints.Count; j++)
            {
                linestringPoints.Add(allPoints[startOffset + j]);
            }
            linestrings.Add(Geometry<Point>.CreatePointOrLineString(linestringPoints, srid));
        }

        return new Geometry<Point>(linestrings, GeometryType.MultiLineString, srid);
    }

    private static Geometry<Point> ParseMultiPolygon(BinaryReader reader, int srid, byte serializationProps)
    {
        bool hasZ = (serializationProps & 0x01) != 0;
        bool hasM = (serializationProps & 0x02) != 0;
        
        var totalPointCount = reader.ReadInt32();
        
        // Read all points as (X, Y) pairs sequentially
        var allPoints = new List<Point>(totalPointCount);
        for (int i = 0; i < totalPointCount; i++)
        {
            var x = reader.ReadDouble();
            var y = reader.ReadDouble();
            allPoints.Add(new Point(x, y));
        }
        
        // Skip all Z coordinates (if hasZ)
        if (hasZ)
        {
            for (int i = 0; i < totalPointCount; i++)
            {
                reader.ReadDouble(); // Skip Z
            }
        }
        
        // Skip all M coordinates (if hasM)
        if (hasM)
        {
            for (int i = 0; i < totalPointCount; i++)
            {
                reader.ReadDouble(); // Skip M
            }
        }

        // Read Figures section: Number of Figures + (Attribute + Point Offset) for each figure
        var figureCount = reader.ReadInt32();
        var figureOffsets = new List<int>(figureCount);
        var figureAttributes = new List<byte>(figureCount);
        for (int i = 0; i < figureCount; i++)
        {
            var attribute = reader.ReadByte(); // Figure attribute (2 for exterior ring, 0 for interior ring)
            var pointOffset = reader.ReadInt32(); // Figure Point Offset
            figureAttributes.Add(attribute);
            figureOffsets.Add(pointOffset);
        }
        
        // Read Shapes section: Number of Shapes + (Parent Offset + Figure Offset + OpenGIS Type) for each shape
        var shapeCount = reader.ReadInt32();
        var shapes = new List<(int parentOffset, int figureOffset, byte openGisType)>(shapeCount);
        for (int i = 0; i < shapeCount; i++)
        {
            var parentOffset = reader.ReadInt32();
            var figureOffset = reader.ReadInt32();
            var openGisType = reader.ReadByte();
            shapes.Add((parentOffset, figureOffset, openGisType));
        }

        // Build polygons from shapes
        // Shapes with parentOffset == -1 are top-level polygons (OpenGIS Type = 3)
        // Shapes with parentOffset >= 0 are child shapes (rings) of those polygons
        var polygons = new List<Geometry<Point>>();
        for (int i = 0; i < shapeCount; i++)
        {
            if (shapes[i].parentOffset == -1 && shapes[i].openGisType == 3) // Polygon shape
            {
                // Find all rings (figures) that belong to this polygon
                var polygonRings = new List<Geometry<Point>>();
                var startFigureIndex = shapes[i].figureOffset;
                
                // Find the end figure index (next polygon's start or end of figures)
                int endFigureIndex = figureCount;
                for (int j = i + 1; j < shapeCount; j++)
                {
                    if (shapes[j].parentOffset == -1 && shapes[j].openGisType == 3)
                    {
                        endFigureIndex = shapes[j].figureOffset;
                        break;
                    }
                }
                
                // Extract rings for this polygon
                for (int figIdx = startFigureIndex; figIdx < endFigureIndex; figIdx++)
                {
                    var startOffset = figureOffsets[figIdx];
                    var endOffset = (figIdx < figureCount - 1) ? figureOffsets[figIdx + 1] : totalPointCount;
                    var pointsInRing = endOffset - startOffset;
                    
                    var ringPoints = new List<Point>(pointsInRing);
                    for (int j = 0; j < pointsInRing && (startOffset + j) < allPoints.Count; j++)
                    {
                        ringPoints.Add(allPoints[startOffset + j]);
                    }
                    polygonRings.Add(Geometry<Point>.CreatePointOrLineString(ringPoints, srid));
                }
                
                if (polygonRings.Count > 0)
                {
                    polygons.Add(Geometry<Point>.CreatePolygonOrMultiPolygon(polygonRings, srid));
                }
            }
        }

        return new Geometry<Point>(polygons, GeometryType.MultiPolygon, srid);
    }

    private static Geometry<Point> ParseMultiPoint(BinaryReader reader, int srid, byte serializationProps)
    {
        bool hasZ = (serializationProps & 0x01) != 0;
        bool hasM = (serializationProps & 0x02) != 0;
        var pointCount = reader.ReadInt32();

        // Read all points as (X, Y) pairs sequentially
        var points = new List<Point>(pointCount);
        for (int i = 0; i < pointCount; i++)
        {
            var x = reader.ReadDouble();
            var y = reader.ReadDouble();
            points.Add(new Point(x, y));
        }
        
        // Read all Z coordinates (if hasZ)
        if (hasZ)
        {
            for (int i = 0; i < pointCount; i++)
            {
                reader.ReadDouble(); // Skip Z
            }
        }
        
        // Read all M coordinates (if hasM)
        if (hasM)
        {
            for (int i = 0; i < pointCount; i++)
            {
                reader.ReadDouble(); // Skip M
            }
        }

        // Read Figures section: Number of Figures + (Attribute + Point Offset) for each figure
        var figureCount = reader.ReadInt32();
        var figureOffsets = new List<int>(figureCount);
        for (int i = 0; i < figureCount; i++)
        {
            reader.ReadByte(); // Figure attribute (should be 1 for stroke/point)
            var pointOffset = reader.ReadInt32(); // Figure Point Offset
            figureOffsets.Add(pointOffset);
        }
        
        // Read Shapes section: Number of Shapes + (Parent Offset + Figure Offset + OpenGIS Type) for each shape
        var shapeCount = reader.ReadInt32();
        if (shapeCount > 0)
        {
            reader.ReadInt32(); // Parent offset (should be -1 for MultiPoint)
            reader.ReadInt32(); // Figure offset
            reader.ReadByte(); // OpenGIS type (should be 4 for MultiPoint)
        }

        // Create Point geometries from points
        // Each point is a separate geometry in MultiPoint
        var geometries = new List<Geometry<Point>>(pointCount);
        for (int i = 0; i < pointCount; i++)
        {
            geometries.Add(Geometry<Point>.Create(points[i].X, points[i].Y, srid));
        }

        return new Geometry<Point>(geometries, GeometryType.MultiPoint, srid);
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
