using System.Globalization;
using System.Text;
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Spatial.IO.Prj;
using IRI.Maptor.Sta.SpatialReferenceSystem;

namespace IRI.Maptor.Sta.Spatial.IO.Dxf;

/// <summary>
/// DXF (Drawing Exchange Format) reader for converting DXF files to Geometry types
/// </summary>
public class DxfReader
{
    public static List<Geometry<Point>> ReadFromFile(string filePath, int? srid)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("DXF file not found", filePath);

        var content = File.ReadAllText(filePath);
        return Read(content, srid);
    }

    public static List<Geometry<Point>> Read(string dxfContent, int? srid)
    {
        if (string.IsNullOrWhiteSpace(dxfContent))
            return [Geometry<Point>.Empty];

        var lines = dxfContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        // Extract SRID from DXF if not explicitly provided
        if (srid == 0)
        {
            var detectedSrid = ExtractSridFromDxf(lines);
            if (detectedSrid > 0)
            {
                srid = detectedSrid;
            }
        }

        srid = srid ?? SridHelper.GeodeticWGS84;

        var entities = ParseEntities(lines, srid.Value);

        if (entities.Count == 0)
            return [Geometry<Point>.Empty];

        // ************************************************************************************
        // process polygons with holes
        // in the case of a polygon with holes they should not be returned as separated polygons
        // but they should be returned as a single polygon with holes. but in the case of multi-polygons
        // they should be returned as separated polygons
        var result = entities.Where(e => e.Type != GeometryType.Polygon).ToList();

        var polygonRings = entities.Where(e => e.Type == GeometryType.Polygon).SelectMany(p => p.Geometries).ToList();

        if (!polygonRings.IsNullOrEmpty())
        {
            var polygonOrMultiPolygon = Geometry<Point>.CreatePolygonOrMultiPolygon(polygonRings, srid.Value);

            if (polygonOrMultiPolygon.Type == GeometryType.MultiPolygon)
            {
                result.AddRange(polygonOrMultiPolygon.Geometries);
            }
            else
            {
                result.Add(polygonOrMultiPolygon);
            }
        }
        // ************************************************************************************

        return result;
    }

    /// <summary>
    /// Extracts SRID from spatial reference system information in DXF file
    /// Searches for GEOGCS or PROJCS WKT strings in XRECORD entities
    /// </summary>
    private static int ExtractSridFromDxf(string[] lines)
    {
        if (lines == null || lines.Length == 0)
            return 0;

        // Search for GEOGCS or PROJCS WKT strings
        // These typically appear in XRECORD entities where group code 1 contains the WKT string
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            // Look for WKT strings starting with GEOGCS or PROJCS
            if (line.StartsWith("GEOGCS", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("PROJCS", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    // Parse the WKT string to extract SRID
                    var prjFile = EsriPrjFile.Parse(line);
                    var detectedSrid = prjFile.Srid;

                    if (detectedSrid > 0)
                    {
                        return detectedSrid;
                    }
                }
                catch
                {
                    // If parsing fails, continue searching
                    continue;
                }
            }
        }

        return 0;
    }

    private static List<Geometry<Point>> ParseEntities(string[] lines, int srid)
    {
        var geometries = new List<Geometry<Point>>();

        // Find ENTITIES section
        int entitiesStart = -1;
        for (int i = 0; i < lines.Length - 1; i++)
        {
            if (lines[i].Trim() == "0" && lines[i + 1].Trim() == "SECTION")
            {
                if (i + 3 < lines.Length && lines[i + 2].Trim() == "2" && lines[i + 3].Trim() == "ENTITIES")
                {
                    entitiesStart = i + 4;
                    break;
                }
            }
        }

        if (entitiesStart == -1)
            return geometries;

        // Parse entities
        int i_entity = entitiesStart;
        while (i_entity < lines.Length)
        {
            if (lines[i_entity].Trim() == "0")
            {
                i_entity++;
                if (i_entity >= lines.Length)
                    break;

                var entityType = lines[i_entity].Trim();

                if (entityType == "ENDSEC" || entityType == "EOF")
                    break;

                switch (entityType)
                {
                    case "POINT":
                        var point = ParsePoint(lines, ref i_entity, srid);
                        if (point != null)
                            geometries.Add(point);
                        break;

                    case "LINE":
                        var line = ParseLine(lines, ref i_entity, srid);
                        if (line != null)
                            geometries.Add(line);
                        break;

                    case "LWPOLYLINE":
                        var polyline = ParseLwPolyline(lines, ref i_entity, srid);
                        if (polyline != null)
                            geometries.Add(polyline);
                        break;

                    case "POLYLINE":
                        var poly = ParsePolyline(lines, ref i_entity, srid);
                        if (poly != null)
                            geometries.Add(poly);
                        break;

                    case "CIRCLE":
                        // Circles are approximated as polygons
                        var circle = ParseCircle(lines, ref i_entity, srid);
                        if (circle != null)
                            geometries.Add(circle);
                        break;

                    case "ARC":
                        // Arcs are approximated as line strings
                        var arc = ParseArc(lines, ref i_entity, srid);
                        if (arc != null)
                            geometries.Add(arc);
                        break;

                    default:
                        // Skip unknown entity
                        i_entity++;
                        break;
                }
            }
            else
            {
                i_entity++;
            }
        }

        return geometries;
    }

    private static Geometry<Point>? ParsePoint(string[] lines, ref int index, int srid)
    {
        double x = 0, y = 0;
        bool hasX = false, hasY = false;

        index++; // Move past entity type

        while (index < lines.Length - 1)
        {
            var groupCode = lines[index].Trim();
            index++;

            if (groupCode == "0") // Next entity
            {
                index--; // Back up to let main loop handle it
                break;
            }

            var value = lines[index].Trim();
            index++;

            switch (groupCode)
            {
                case "10": // X coordinate
                    if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out x))
                        hasX = true;
                    break;

                case "20": // Y coordinate
                    if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out y))
                        hasY = true;
                    break;
            }
        }

        if (hasX && hasY)
            return Geometry<Point>.Create(x, y, srid);

        return null;
    }

    private static Geometry<Point>? ParseLine(string[] lines, ref int index, int srid)
    {
        double x1 = 0, y1 = 0, x2 = 0, y2 = 0;
        bool hasStart = false, hasEnd = false;

        index++; // Move past entity type

        while (index < lines.Length - 1)
        {
            var groupCode = lines[index].Trim();
            index++;

            if (groupCode == "0") // Next entity
            {
                index--; // Back up
                break;
            }

            var value = lines[index].Trim();
            index++;

            switch (groupCode)
            {
                case "10": // Start X
                    if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out x1))
                        hasStart = true;
                    break;

                case "20": // Start Y
                    if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out y1))
                        hasStart = true;
                    break;

                case "11": // End X
                    if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out x2))
                        hasEnd = true;
                    break;

                case "21": // End Y
                    if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out y2))
                        hasEnd = true;
                    break;
            }
        }

        if (hasStart && hasEnd)
        {
            var points = new List<Point> { new Point(x1, y1), new Point(x2, y2) };
            return new Geometry<Point>(points, GeometryType.LineString, srid);
        }

        return null;
    }

    private static Geometry<Point>? ParseLwPolyline(string[] lines, ref int index, int srid)
    {
        var points = new List<Point>();
        bool isClosed = false;
        int numVertices = 0;

        index++; // Move past entity type

        while (index < lines.Length - 1)
        {
            var groupCode = lines[index].Trim();
            index++;

            if (groupCode == "0") // Next entity
            {
                index--; // Back up
                break;
            }

            var value = lines[index].Trim();
            index++;

            switch (groupCode)
            {
                case "90": // Number of vertices
                    int.TryParse(value, out numVertices);
                    break;

                case "70": // Polyline flag (1 = closed)
                    isClosed = value == "1";
                    break;

                case "10": // X coordinate
                    if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double x))
                    {
                        // Next should be Y (group code 20)
                        if (index < lines.Length - 1 && lines[index].Trim() == "20")
                        {
                            index++;
                            if (double.TryParse(lines[index].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double y))
                            {
                                points.Add(new Point(x, y));
                                index++;
                            }
                        }
                    }
                    break;
            }
        }

        if (points.Count == 0)
            return null;

        if (points.Count == 1)
            return Geometry<Point>.Create(points[0].X, points[0].Y, srid);

        if (isClosed && points.Count >= 3)
        {
            // Create a polygon
            var ring = new Geometry<Point>(points, GeometryType.LineString, srid);
            return new Geometry<Point>(new List<Geometry<Point>> { ring }, GeometryType.Polygon, srid);
        }
        else
        {
            // Create a line string
            return new Geometry<Point>(points, GeometryType.LineString, srid);
        }
    }

    private static Geometry<Point>? ParsePolyline(string[] lines, ref int index, int srid)
    {
        var points = new List<Point>();
        bool isClosed = false;

        index++; // Move past entity type

        // Parse POLYLINE header
        while (index < lines.Length - 1)
        {
            var groupCode = lines[index].Trim();
            index++;

            if (groupCode == "0")
            {
                var nextEntity = lines[index].Trim();
                if (nextEntity == "VERTEX")
                {
                    // Start parsing vertices
                    break;
                }
                else if (nextEntity == "SEQEND" || nextEntity != "VERTEX")
                {
                    index--;
                    break;
                }
            }

            var value = lines[index].Trim();
            index++;

            if (groupCode == "70") // Polyline flag
            {
                if (int.TryParse(value, out int flag))
                {
                    isClosed = (flag & 1) != 0; // Bit 0 indicates closed
                }
            }
        }

        // Parse VERTEX entities
        while (index < lines.Length - 1)
        {
            if (lines[index].Trim() == "0")
            {
                index++;
                var entityType = lines[index].Trim();

                if (entityType == "VERTEX")
                {
                    index++;
                    double x = 0, y = 0;
                    bool hasX = false, hasY = false;

                    while (index < lines.Length - 1)
                    {
                        var groupCode = lines[index].Trim();
                        index++;

                        if (groupCode == "0")
                        {
                            index--;
                            break;
                        }

                        var value = lines[index].Trim();
                        index++;

                        switch (groupCode)
                        {
                            case "10":
                                if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out x))
                                    hasX = true;
                                break;
                            case "20":
                                if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out y))
                                    hasY = true;
                                break;
                        }
                    }

                    if (hasX && hasY)
                        points.Add(new Point(x, y));
                }
                else if (entityType == "SEQEND")
                {
                    index++;
                    break;
                }
                else
                {
                    index--;
                    break;
                }
            }
            else
            {
                index++;
            }
        }

        if (points.Count == 0)
            return null;

        if (points.Count == 1)
            return Geometry<Point>.Create(points[0].X, points[0].Y, srid);

        if (isClosed && points.Count >= 3)
        {
            var ring = new Geometry<Point>(points, GeometryType.LineString, srid);
            return new Geometry<Point>(new List<Geometry<Point>> { ring }, GeometryType.Polygon, srid);
        }
        else
        {
            return new Geometry<Point>(points, GeometryType.LineString, srid);
        }
    }

    private static Geometry<Point>? ParseCircle(string[] lines, ref int index, int srid, int segments = 32)
    {
        double centerX = 0, centerY = 0, radius = 0;
        bool hasCenter = false, hasRadius = false;

        index++; // Move past entity type

        while (index < lines.Length - 1)
        {
            var groupCode = lines[index].Trim();
            index++;

            if (groupCode == "0") // Next entity
            {
                index--;
                break;
            }

            var value = lines[index].Trim();
            index++;

            switch (groupCode)
            {
                case "10": // Center X
                    if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out centerX))
                        hasCenter = true;
                    break;

                case "20": // Center Y
                    if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out centerY))
                        hasCenter = true;
                    break;

                case "40": // Radius
                    if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out radius))
                        hasRadius = true;
                    break;
            }
        }

        if (hasCenter && hasRadius && radius > 0)
        {
            // Approximate circle as a polygon
            var points = new List<Point>();
            for (int i = 0; i < segments; i++)
            {
                double angle = 2 * Math.PI * i / segments;
                double x = centerX + radius * Math.Cos(angle);
                double y = centerY + radius * Math.Sin(angle);
                points.Add(new Point(x, y));
            }

            var ring = new Geometry<Point>(points, GeometryType.LineString, srid);
            return new Geometry<Point>(new List<Geometry<Point>> { ring }, GeometryType.Polygon, srid);
        }

        return null;
    }

    private static Geometry<Point>? ParseArc(string[] lines, ref int index, int srid, int segments = 32)
    {
        double centerX = 0, centerY = 0, radius = 0;
        double startAngle = 0, endAngle = 0;
        bool hasCenter = false, hasRadius = false;
        bool hasStartAngle = false, hasEndAngle = false;

        index++; // Move past entity type

        while (index < lines.Length - 1)
        {
            var groupCode = lines[index].Trim();
            index++;

            if (groupCode == "0") // Next entity
            {
                index--;
                break;
            }

            var value = lines[index].Trim();
            index++;

            switch (groupCode)
            {
                case "10": // Center X
                    if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out centerX))
                        hasCenter = true;
                    break;

                case "20": // Center Y
                    if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out centerY))
                        hasCenter = true;
                    break;

                case "40": // Radius
                    if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out radius))
                        hasRadius = true;
                    break;

                case "50": // Start angle (degrees)
                    if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out startAngle))
                        hasStartAngle = true;
                    break;

                case "51": // End angle (degrees)
                    if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out endAngle))
                        hasEndAngle = true;
                    break;
            }
        }

        if (hasCenter && hasRadius && hasStartAngle && hasEndAngle && radius > 0)
        {
            // Convert angles from degrees to radians
            double startRad = startAngle * Math.PI / 180.0;
            double endRad = endAngle * Math.PI / 180.0;

            // Handle angle wrapping
            if (endRad < startRad)
                endRad += 2 * Math.PI;

            double angleRange = endRad - startRad;

            // Approximate arc as a line string
            var points = new List<Point>();
            for (int i = 0; i <= segments; i++)
            {
                double t = (double)i / segments;
                double angle = startRad + angleRange * t;
                double x = centerX + radius * Math.Cos(angle);
                double y = centerY + radius * Math.Sin(angle);
                points.Add(new Point(x, y));
            }

            if (points.Count >= 2)
                return new Geometry<Point>(points, GeometryType.LineString, srid);
        }

        return null;
    }
}

