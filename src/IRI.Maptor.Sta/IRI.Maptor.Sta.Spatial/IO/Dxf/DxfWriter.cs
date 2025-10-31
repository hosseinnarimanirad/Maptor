using System.Text;
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Sta.Spatial.IO.Dxf;

/// <summary>
/// DXF (Drawing Exchange Format) writer for Geometry types
/// </summary>
public class DxfWriter
{
    private const string DXF_VERSION = "AC1015"; // AutoCAD 2000 format
    
    public static string WriteToFile(Geometry<Point> geometry, string filePath)
    {
        var dxfContent = Write(geometry);
        File.WriteAllText(filePath, dxfContent);
        return filePath;
    }
    
    public static string Write(Geometry<Point> geometry)
    {
        var sb = new StringBuilder();
        
        // Write DXF sections
        WriteHeader(sb);
        WriteTables(sb);
        WriteEntities(sb, geometry);
        WriteEndOfFile(sb);
        
        return sb.ToString();
    }
    
    private static void WriteHeader(StringBuilder sb)
    {
        sb.AppendLine("0");
        sb.AppendLine("SECTION");
        sb.AppendLine("2");
        sb.AppendLine("HEADER");
        sb.AppendLine("9");
        sb.AppendLine("$ACADVER");
        sb.AppendLine("1");
        sb.AppendLine(DXF_VERSION);
        sb.AppendLine("0");
        sb.AppendLine("ENDSEC");
        sb.AppendLine();
    }
    
    private static void WriteTables(StringBuilder sb)
    {
        sb.AppendLine("0");
        sb.AppendLine("SECTION");
        sb.AppendLine("2");
        sb.AppendLine("TABLES");
        sb.AppendLine("0");
        sb.AppendLine("TABLE");
        sb.AppendLine("2");
        sb.AppendLine("LAYER");
        sb.AppendLine("5");
        sb.AppendLine("2");
        sb.AppendLine("100");
        sb.AppendLine("AcDbSymbolTable");
        sb.AppendLine("70");
        sb.AppendLine("1");
        sb.AppendLine("0");
        sb.AppendLine("LAYER");
        sb.AppendLine("5");
        sb.AppendLine("10");
        sb.AppendLine("100");
        sb.AppendLine("AcDbSymbolTableRecord");
        sb.AppendLine("100");
        sb.AppendLine("AcDbLayerTableRecord");
        sb.AppendLine("2");
        sb.AppendLine("0");
        sb.AppendLine("70");
        sb.AppendLine("0");
        sb.AppendLine("62");
        sb.AppendLine("7");
        sb.AppendLine("6");
        sb.AppendLine("CONTINUOUS");
        sb.AppendLine("0");
        sb.AppendLine("ENDTAB");
        sb.AppendLine("0");
        sb.AppendLine("ENDSEC");
        sb.AppendLine();
    }
    
    private static void WriteEntities(StringBuilder sb, Geometry<Point> geometry)
    {
        sb.AppendLine("0");
        sb.AppendLine("SECTION");
        sb.AppendLine("2");
        sb.AppendLine("ENTITIES");
        
        WriteGeometryEntities(sb, geometry);
        
        sb.AppendLine("0");
        sb.AppendLine("ENDSEC");
        sb.AppendLine();
    }
    
    private static void WriteGeometryEntities(StringBuilder sb, Geometry<Point> geometry)
    {
        if (geometry == null || !geometry.HasAnyPoint())
            return;
            
        switch (geometry.Type)
        {
            case GeometryType.Point:
                WritePointEntity(sb, geometry);
                break;
                
            case GeometryType.LineString:
                WriteLineStringEntity(sb, geometry);
                break;
                
            case GeometryType.MultiPoint:
                foreach (var pointGeo in geometry.Geometries)
                {
                    WritePointEntity(sb, pointGeo);
                }
                break;
                
            case GeometryType.MultiLineString:
                foreach (var lineGeo in geometry.Geometries)
                {
                    WriteLineStringEntity(sb, lineGeo);
                }
                break;
                
            case GeometryType.Polygon:
                WritePolygonEntity(sb, geometry);
                break;
                
            case GeometryType.MultiPolygon:
                foreach (var polygonGeo in geometry.Geometries)
                {
                    WritePolygonEntity(sb, polygonGeo);
                }
                break;
                
            case GeometryType.GeometryCollection:
                // Handle geometry collection by writing each geometry separately
                if (geometry.Geometries != null)
                {
                    foreach (var subGeometry in geometry.Geometries)
                    {
                        WriteGeometryEntities(sb, subGeometry);
                    }
                }
                break;
        }
    }
    
    private static void WritePointEntity(StringBuilder sb, Geometry<Point> geometry)
    {
        if (geometry.Points == null || geometry.Points.Count == 0)
            return;
            
        var point = geometry.Points[0];
        
        sb.AppendLine("0");
        sb.AppendLine("POINT");
        sb.AppendLine("5");
        sb.AppendLine(GetNextHandle());
        sb.AppendLine("100");
        sb.AppendLine("AcDbEntity");
        sb.AppendLine("8");
        sb.AppendLine("0");
        sb.AppendLine("100");
        sb.AppendLine("AcDbPoint");
        sb.AppendLine("10");
        sb.AppendLine(point.X.ToString("F6", System.Globalization.CultureInfo.InvariantCulture));
        sb.AppendLine("20");
        sb.AppendLine(point.Y.ToString("F6", System.Globalization.CultureInfo.InvariantCulture));
        sb.AppendLine("30");
        sb.AppendLine("0.0");
    }
    
    private static void WriteLineStringEntity(StringBuilder sb, Geometry<Point> geometry)
    {
        if (geometry.Points == null || geometry.Points.Count < 2)
            return;
            
        // Use LWPOLYLINE for line strings
        sb.AppendLine("0");
        sb.AppendLine("LWPOLYLINE");
        sb.AppendLine("5");
        sb.AppendLine(GetNextHandle());
        sb.AppendLine("100");
        sb.AppendLine("AcDbEntity");
        sb.AppendLine("8");
        sb.AppendLine("0");
        sb.AppendLine("100");
        sb.AppendLine("AcDbPolyline");
        sb.AppendLine("90");
        sb.AppendLine(geometry.Points.Count.ToString());
        sb.AppendLine("70");
        sb.AppendLine("0"); // Open polyline (not closed)
        
        foreach (var point in geometry.Points)
        {
            sb.AppendLine("10");
            sb.AppendLine(point.X.ToString("F6", System.Globalization.CultureInfo.InvariantCulture));
            sb.AppendLine("20");
            sb.AppendLine(point.Y.ToString("F6", System.Globalization.CultureInfo.InvariantCulture));
        }
    }
    
    private static void WritePolygonEntity(StringBuilder sb, Geometry<Point> geometry)
    {
        if (geometry.Geometries == null || geometry.Geometries.Count == 0)
            return;
            
        // Write exterior ring first
        if (geometry.Geometries[0].Points != null && geometry.Geometries[0].Points.Count > 0)
        {
            WritePolylineEntity(sb, geometry.Geometries[0].Points, closed: true);
        }
        
        // Write interior rings (holes) if any
        if (geometry.Geometries.Count > 1)
        {
            for (int i = 1; i < geometry.Geometries.Count; i++)
            {
                if (geometry.Geometries[i].Points != null && geometry.Geometries[i].Points.Count > 0)
                {
                    WritePolylineEntity(sb, geometry.Geometries[i].Points, closed: true);
                }
            }
        }
    }
    
    private static void WritePolylineEntity(StringBuilder sb, List<Point> points, bool closed)
    {
        sb.AppendLine("0");
        sb.AppendLine("LWPOLYLINE");
        sb.AppendLine("5");
        sb.AppendLine(GetNextHandle());
        sb.AppendLine("100");
        sb.AppendLine("AcDbEntity");
        sb.AppendLine("8");
        sb.AppendLine("0");
        sb.AppendLine("100");
        sb.AppendLine("AcDbPolyline");
        sb.AppendLine("90");
        sb.AppendLine(points.Count.ToString());
        sb.AppendLine("70");
        sb.AppendLine(closed ? "1" : "0"); // Closed polyline flag
        
        foreach (var point in points)
        {
            sb.AppendLine("10");
            sb.AppendLine(point.X.ToString("F6", System.Globalization.CultureInfo.InvariantCulture));
            sb.AppendLine("20");
            sb.AppendLine(point.Y.ToString("F6", System.Globalization.CultureInfo.InvariantCulture));
        }
    }
    
    private static void WriteEndOfFile(StringBuilder sb)
    {
        sb.AppendLine("0");
        sb.AppendLine("EOF");
    }
    
    // Simple handle generator - in production you might want a better approach
    private static int _handleCounter = 1;
    private static string GetNextHandle()
    {
        var handle = _handleCounter.ToString("X");
        _handleCounter++;
        return handle;
    }
    
    public static void ResetHandleCounter()
    {
        _handleCounter = 1;
    }
}

