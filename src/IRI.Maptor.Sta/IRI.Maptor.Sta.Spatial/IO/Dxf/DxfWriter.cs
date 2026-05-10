using System.Globalization;
using System.Text;
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;

namespace IRI.Maptor.Sta.Spatial.IO.Dxf;

/// <summary>
/// DXF (Drawing Exchange Format) writer for Geometry types, fully compatible with ArcMap and AutoCAD.
/// </summary>
public class DxfWriter
{
    private const string DXF_VERSION = "AC1015"; // AutoCAD 2000 format

    // Reserved handles for block records and model space block
    private const string HANDLE_BLOCK_RECORD_MODEL_SPACE = "1F";
    private const string HANDLE_BLOCK_MODEL_SPACE = "20";
    private const string HANDLE_ENDBLK_MODEL_SPACE = "21";

    private static int _handleCounter = 0x100; // start after reserved handles

    public static void ResetHandleCounter()
    {
        _handleCounter = 0x100;
    }

    private static string GetNextHandle()
    {
        return (_handleCounter++).ToString("X");
    }

    // ----------------------------------------------------------------------
    // Public write methods
    // ----------------------------------------------------------------------

    public static string WriteToFile(Geometry<Point> geometry, string filePath)
    {
        return WriteToFile(geometry, filePath, null);
    }

    public static string WriteToFile(Geometry<Point> geometry, string filePath, DxfColorInfo? colorInfo)
    {
        var content = Write(geometry, colorInfo);

        File.WriteAllText(filePath, content);
         
        if (geometry is not null)
        {
            WritePrj(filePath, geometry.Srid);
        }

        return filePath;
    }

    public static string WriteToFile(IEnumerable<Geometry<Point>> geometries, string filePath, DxfColorInfo? colorInfo = null)
    {
        var content = Write(geometries, colorInfo);

        File.WriteAllText(filePath, content);

        var srid = geometries?.FirstOrDefault().Srid;

        if (srid is not null)
        {
            WritePrj(filePath, srid.Value);
        }

        return filePath;
    }

    public static string WriteToFile(IEnumerable<Geometry<Point>> geometries, string filePath, Func<Geometry<Point>, DxfColorInfo?> getColorInfo)
    {
        var content = Write(geometries, getColorInfo);

        File.WriteAllText(filePath, content);

        var srid = geometries?.FirstOrDefault().Srid;

        if (srid is not null)
        {
            WritePrj(filePath, srid.Value);
        }

        return filePath;
    }

    private static void WritePrj(string filePath, int srid)
    {
        string prjPath = Path.ChangeExtension(filePath, ".prj");

        File.WriteAllText(prjPath, SridHelper.AsSrsBase(srid)?.AsEsriCrsWkt());
    }

    public static string Write(Geometry<Point> geometry, DxfColorInfo? colorInfo)
    {
        return Write(new[] { geometry }, colorInfo);
    }

    public static string Write(IEnumerable<Geometry<Point>> geometries, DxfColorInfo? colorInfo = null)
    {
        if (geometries == null)
            throw new ArgumentNullException(nameof(geometries));

        ResetHandleCounter();
        var sb = new StringBuilder();

        var bbox = ComputeBoundingBox(geometries);
        WriteHeader(sb, bbox);
        WriteTables(sb, bbox);
        WriteBlocks(sb);
        WriteEntities(sb, geometries, colorInfo);
        WriteEndOfFile(sb);

        return sb.ToString();
    }

    public static string Write(IEnumerable<Geometry<Point>> geometries, Func<Geometry<Point>, DxfColorInfo?> getColorInfo)
    {
        if (geometries == null)
            throw new ArgumentNullException(nameof(geometries));
        if (getColorInfo == null)
            throw new ArgumentNullException(nameof(getColorInfo));

        ResetHandleCounter();
        var sb = new StringBuilder();

        var bbox = ComputeBoundingBox(geometries);
        WriteHeader(sb, bbox);
        WriteTables(sb, bbox);
        WriteBlocks(sb);
        WriteEntities(sb, geometries, getColorInfo);
        WriteEndOfFile(sb);

        return sb.ToString();
    }

    // ----------------------------------------------------------------------
    // Header section with bounding box
    // ----------------------------------------------------------------------

    private static void WriteHeader(StringBuilder sb, BoundingBox? bbox)
    {
        sb.AppendLine("0");
        sb.AppendLine("SECTION");
        sb.AppendLine("2");
        sb.AppendLine("HEADER");
        sb.AppendLine("9");
        sb.AppendLine("$ACADVER");
        sb.AppendLine("1");
        sb.AppendLine(DXF_VERSION);

        // Required for ArcMap: drawing extents
        if (bbox.HasValue)
        {
            var inv = CultureInfo.InvariantCulture;
            sb.AppendLine("9");
            sb.AppendLine("$EXTMIN");
            sb.AppendLine("10");
            sb.AppendLine(bbox.Value.XMin.ToString("F6", inv));
            sb.AppendLine("20");
            sb.AppendLine(bbox.Value.YMin.ToString("F6", inv));
            sb.AppendLine("30");
            sb.AppendLine("0.0");

            sb.AppendLine("9");
            sb.AppendLine("$EXTMAX");
            sb.AppendLine("10");
            sb.AppendLine(bbox.Value.XMax.ToString("F6", inv));
            sb.AppendLine("20");
            sb.AppendLine(bbox.Value.YMax.ToString("F6", inv));
            sb.AppendLine("30");
            sb.AppendLine("0.0");

            // LIMITS (optional but recommended)
            sb.AppendLine("9");
            sb.AppendLine("$LIMMIN");
            sb.AppendLine("10");
            sb.AppendLine("0.0");
            sb.AppendLine("20");
            sb.AppendLine("0.0");

            sb.AppendLine("9");
            sb.AppendLine("$LIMMAX");
            sb.AppendLine("10");
            sb.AppendLine("12.0");
            sb.AppendLine("20");
            sb.AppendLine("9.0");
        }

        sb.AppendLine("0");
        sb.AppendLine("ENDSEC");
        //sb.AppendLine()
    }

    private static BoundingBox? ComputeBoundingBox(IEnumerable<Geometry<Point>> geometries)
    {
        double? minX = null, minY = null, maxX = null, maxY = null;

        foreach (var geom in geometries)
        {
            if (geom == null) continue;
            foreach (var point in geom.GetAllPoints())
            {
                if (point == null) continue;
                if (!minX.HasValue || point.X < minX) minX = point.X;
                if (!maxX.HasValue || point.X > maxX) maxX = point.X;
                if (!minY.HasValue || point.Y < minY) minY = point.Y;
                if (!maxY.HasValue || point.Y > maxY) maxY = point.Y;
            }
        }

        if (minX.HasValue && minY.HasValue && maxX.HasValue && maxY.HasValue)
            return new BoundingBox(minX.Value, minY.Value, maxX.Value, maxY.Value);
        return null;
    }

    // ----------------------------------------------------------------------
    // TABLES section (includes VPORT, LTYPE, LAYER, BLOCK_RECORD)
    // ----------------------------------------------------------------------

    private static void WriteTables(StringBuilder sb, BoundingBox? bbox)
    {
        sb.AppendLine("0");
        sb.AppendLine("SECTION");
        sb.AppendLine("2");
        sb.AppendLine("TABLES");

        // ---------- VPORT table (critical for initial view) ----------
        sb.AppendLine("0");
        sb.AppendLine("TABLE");
        sb.AppendLine("2");
        sb.AppendLine("VPORT");
        sb.AppendLine("5");
        sb.AppendLine("8");
        sb.AppendLine("100");
        sb.AppendLine("AcDbSymbolTable");
        sb.AppendLine("70");
        sb.AppendLine("1");

        sb.AppendLine("0");
        sb.AppendLine("VPORT");
        sb.AppendLine("5");
        sb.AppendLine("29");
        sb.AppendLine("330");
        sb.AppendLine("8");
        sb.AppendLine("100");
        sb.AppendLine("AcDbSymbolTableRecord");
        sb.AppendLine("100");
        sb.AppendLine("AcDbViewportTableRecord");
        sb.AppendLine("2");
        sb.AppendLine("*Active");
        sb.AppendLine("70");
        sb.AppendLine("0");
        sb.AppendLine("10");
        sb.AppendLine("0.0");
        sb.AppendLine("20");
        sb.AppendLine("0.0");
        sb.AppendLine("11");
        sb.AppendLine("1.0");
        sb.AppendLine("21");
        sb.AppendLine("1.0");

        if (bbox.HasValue)
        {
            double centerX = (bbox.Value.XMin + bbox.Value.XMax) / 2.0;
            double centerY = (bbox.Value.YMin + bbox.Value.YMax) / 2.0;
            double height = bbox.Value.YMax - bbox.Value.YMin;
            double viewHeight = height * 1.2; // add 20% margin
            var inv = CultureInfo.InvariantCulture;

            sb.AppendLine("12");
            sb.AppendLine(centerX.ToString("F6", inv));
            sb.AppendLine("22");
            sb.AppendLine(centerY.ToString("F6", inv));
            sb.AppendLine("40");
            sb.AppendLine(viewHeight.ToString("F6", inv));
        }
        else
        {
            sb.AppendLine("12");
            sb.AppendLine("0.0");
            sb.AppendLine("22");
            sb.AppendLine("0.0");
            sb.AppendLine("40");
            sb.AppendLine("10.0");
        }

        sb.AppendLine("0");
        sb.AppendLine("ENDTAB");

        // ---------- LTYPE table (linetypes) ----------
        sb.AppendLine("0");
        sb.AppendLine("TABLE");
        sb.AppendLine("2");
        sb.AppendLine("LTYPE");
        sb.AppendLine("5");
        sb.AppendLine("5");
        sb.AppendLine("100");
        sb.AppendLine("AcDbSymbolTable");
        sb.AppendLine("70");
        sb.AppendLine("1");

        // ByBlock
        sb.AppendLine("0");
        sb.AppendLine("LTYPE");
        sb.AppendLine("5");
        sb.AppendLine("14");
        sb.AppendLine("100");
        sb.AppendLine("AcDbSymbolTableRecord");
        sb.AppendLine("100");
        sb.AppendLine("AcDbLinetypeTableRecord");
        sb.AppendLine("2");
        sb.AppendLine("ByBlock");
        sb.AppendLine("70");
        sb.AppendLine("0");
        sb.AppendLine("3");
        sb.AppendLine("");
        sb.AppendLine("72");
        sb.AppendLine("65");
        sb.AppendLine("73");
        sb.AppendLine("0");
        sb.AppendLine("40");
        sb.AppendLine("0.0");

        // ByLayer
        sb.AppendLine("0");
        sb.AppendLine("LTYPE");
        sb.AppendLine("5");
        sb.AppendLine("15");
        sb.AppendLine("100");
        sb.AppendLine("AcDbSymbolTableRecord");
        sb.AppendLine("100");
        sb.AppendLine("AcDbLinetypeTableRecord");
        sb.AppendLine("2");
        sb.AppendLine("ByLayer");
        sb.AppendLine("70");
        sb.AppendLine("0");
        sb.AppendLine("3");
        sb.AppendLine("");
        sb.AppendLine("72");
        sb.AppendLine("65");
        sb.AppendLine("73");
        sb.AppendLine("0");
        sb.AppendLine("40");
        sb.AppendLine("0.0");

        // Continuous
        sb.AppendLine("0");
        sb.AppendLine("LTYPE");
        sb.AppendLine("5");
        sb.AppendLine("16");
        sb.AppendLine("100");
        sb.AppendLine("AcDbSymbolTableRecord");
        sb.AppendLine("100");
        sb.AppendLine("AcDbLinetypeTableRecord");
        sb.AppendLine("2");
        sb.AppendLine("Continuous");
        sb.AppendLine("70");
        sb.AppendLine("0");
        sb.AppendLine("3");
        sb.AppendLine("Solid line");
        sb.AppendLine("72");
        sb.AppendLine("65");
        sb.AppendLine("73");
        sb.AppendLine("0");
        sb.AppendLine("40");
        sb.AppendLine("0.0");

        sb.AppendLine("0");
        sb.AppendLine("ENDTAB");

        // ---------- LAYER table ----------
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

        // ---------- BLOCK_RECORD table ----------
        sb.AppendLine("0");
        sb.AppendLine("TABLE");
        sb.AppendLine("2");
        sb.AppendLine("BLOCK_RECORD");
        sb.AppendLine("5");
        sb.AppendLine("1");
        sb.AppendLine("100");
        sb.AppendLine("AcDbSymbolTable");
        sb.AppendLine("70");
        sb.AppendLine("2");

        // *Model_Space record
        sb.AppendLine("0");
        sb.AppendLine("BLOCK_RECORD");
        sb.AppendLine("5");
        sb.AppendLine(HANDLE_BLOCK_RECORD_MODEL_SPACE);
        sb.AppendLine("100");
        sb.AppendLine("AcDbSymbolTableRecord");
        sb.AppendLine("100");
        sb.AppendLine("AcDbBlockTableRecord");
        sb.AppendLine("2");
        sb.AppendLine("*Model_Space");
        sb.AppendLine("340");
        sb.AppendLine(HANDLE_BLOCK_MODEL_SPACE);
        sb.AppendLine("70");
        sb.AppendLine("0");
        sb.AppendLine("280");
        sb.AppendLine("1");
        sb.AppendLine("281");
        sb.AppendLine("0");

        // *Paper_Space record (minimal)
        sb.AppendLine("0");
        sb.AppendLine("BLOCK_RECORD");
        sb.AppendLine("5");
        sb.AppendLine("1B");
        sb.AppendLine("100");
        sb.AppendLine("AcDbSymbolTableRecord");
        sb.AppendLine("100");
        sb.AppendLine("AcDbBlockTableRecord");
        sb.AppendLine("2");
        sb.AppendLine("*Paper_Space");
        sb.AppendLine("340");
        sb.AppendLine("1E");
        sb.AppendLine("70");
        sb.AppendLine("0");
        sb.AppendLine("280");
        sb.AppendLine("1");
        sb.AppendLine("281");
        sb.AppendLine("0");

        sb.AppendLine("0");
        sb.AppendLine("ENDTAB");

        sb.AppendLine("0");
        sb.AppendLine("ENDSEC");
        //sb.AppendLine()
    }

    // ----------------------------------------------------------------------
    // BLOCKS section (defines *Model_Space)
    // ----------------------------------------------------------------------

    private static void WriteBlocks(StringBuilder sb)
    {
        sb.AppendLine("0");
        sb.AppendLine("SECTION");
        sb.AppendLine("2");
        sb.AppendLine("BLOCKS");

        // *Model_Space block definition
        sb.AppendLine("0");
        sb.AppendLine("BLOCK");
        sb.AppendLine("5");
        sb.AppendLine(HANDLE_BLOCK_MODEL_SPACE);
        sb.AppendLine("330");
        sb.AppendLine(HANDLE_BLOCK_RECORD_MODEL_SPACE);
        sb.AppendLine("100");
        sb.AppendLine("AcDbEntity");
        sb.AppendLine("8");
        sb.AppendLine("0");
        sb.AppendLine("100");
        sb.AppendLine("AcDbBlockBegin");
        sb.AppendLine("2");
        sb.AppendLine("*Model_Space");
        sb.AppendLine("70");
        sb.AppendLine("0");
        sb.AppendLine("10");
        sb.AppendLine("0.0");
        sb.AppendLine("20");
        sb.AppendLine("0.0");
        sb.AppendLine("30");
        sb.AppendLine("0.0");
        sb.AppendLine("3");
        sb.AppendLine("*Model_Space");
        sb.AppendLine("1");
        sb.AppendLine("");

        sb.AppendLine("0");
        sb.AppendLine("ENDBLK");
        sb.AppendLine("5");
        sb.AppendLine(HANDLE_ENDBLK_MODEL_SPACE);
        sb.AppendLine("330");
        sb.AppendLine(HANDLE_BLOCK_MODEL_SPACE);
        sb.AppendLine("100");
        sb.AppendLine("AcDbEntity");
        sb.AppendLine("8");
        sb.AppendLine("0");
        sb.AppendLine("100");
        sb.AppendLine("AcDbBlockEnd");

        sb.AppendLine("0");
        sb.AppendLine("ENDSEC");
        //sb.AppendLine()
    }

    // ----------------------------------------------------------------------
    // ENTITIES section – main geometry output with proper handles and 330 pointers
    // ----------------------------------------------------------------------

    private static void WriteEntities(StringBuilder sb, IEnumerable<Geometry<Point>> geometries, DxfColorInfo? globalColorInfo)
    {
        sb.AppendLine("0");
        sb.AppendLine("SECTION");
        sb.AppendLine("2");
        sb.AppendLine("ENTITIES");

        foreach (var geom in geometries)
        {
            if (geom != null)
                WriteGeometryWithPointer(sb, geom, globalColorInfo);
        }

        sb.AppendLine("0");
        sb.AppendLine("ENDSEC");
        //sb.AppendLine()
    }

    private static void WriteEntities(StringBuilder sb, IEnumerable<Geometry<Point>> geometries, Func<Geometry<Point>, DxfColorInfo?> getColorInfo)
    {
        sb.AppendLine("0");
        sb.AppendLine("SECTION");
        sb.AppendLine("2");
        sb.AppendLine("ENTITIES");

        foreach (var geom in geometries)
        {
            if (geom != null)
            {
                var colorInfo = getColorInfo(geom);
                WriteGeometryWithPointer(sb, geom, colorInfo);
            }
        }

        sb.AppendLine("0");
        sb.AppendLine("ENDSEC");
        //sb.AppendLine()
    }

    /// <summary>
    /// Writes a geometry entity with the required 330 pointer to *Model_Space.
    /// </summary>
    private static void WriteGeometryWithPointer(StringBuilder sb, Geometry<Point> geometry, DxfColorInfo? colorInfo)
    {
        if (geometry == null || !geometry.HasAnyPoint())
            return;

        // For Multi-geometries, recurse
        if (geometry.Type == GeometryType.MultiPoint ||
            geometry.Type == GeometryType.MultiLineString ||
            geometry.Type == GeometryType.MultiPolygon ||
            geometry.Type == GeometryType.GeometryCollection)
        {
            foreach (var subGeom in geometry.Geometries)
                WriteGeometryWithPointer(sb, subGeom, colorInfo);
            return;
        }

        string handle = GetNextHandle();

        // Common entity header for all types
        sb.AppendLine("0");
        sb.AppendLine(GetEntityTypeName(geometry));
        sb.AppendLine("5");
        sb.AppendLine(handle);
        sb.AppendLine("330");
        sb.AppendLine(HANDLE_BLOCK_RECORD_MODEL_SPACE); // points to *Model_Space block record
        sb.AppendLine("100");
        sb.AppendLine("AcDbEntity");
        sb.AppendLine("8");
        sb.AppendLine("0"); // layer name

        // Write color/transparency if provided
        if (colorInfo?.StrokeColor != null)
            WriteColorCodes(sb, colorInfo.StrokeColor.Value, colorInfo.Opacity);

        // Write geometry-specific data
        switch (geometry.Type)
        {
            case GeometryType.Point:
                WritePointBody(sb, geometry, colorInfo);
                break;
            case GeometryType.LineString:
                WriteLineStringBody(sb, geometry, colorInfo);
                break;
            case GeometryType.Polygon:
                WritePolygonBody(sb, geometry, colorInfo);
                break;
            default:
                // Fallback: try as line string
                WriteLineStringBody(sb, geometry, colorInfo);
                break;
        }
    }

    private static string GetEntityTypeName(Geometry<Point> geom)
    {
        return geom.Type switch
        {
            GeometryType.Point => "POINT",
            GeometryType.LineString => "LWPOLYLINE",
            GeometryType.Polygon => "LWPOLYLINE",
            _ => "LWPOLYLINE"
        };
    }

    // ----------------------------------------------------------------------
    // Geometry body writers (no 0/330/100, just the entity-specific codes)
    // ----------------------------------------------------------------------

    private static void WritePointBody(StringBuilder sb, Geometry<Point> geometry, DxfColorInfo? colorInfo)
    {
        if (geometry.Points == null || geometry.Points.Count == 0) return;
        var p = geometry.Points[0];
        var inv = CultureInfo.InvariantCulture;

        sb.AppendLine("100");
        sb.AppendLine("AcDbPoint");
        sb.AppendLine("10");
        sb.AppendLine(p.X.ToString("F6", inv));
        sb.AppendLine("20");
        sb.AppendLine(p.Y.ToString("F6", inv));
        sb.AppendLine("30");
        sb.AppendLine("0.0");
    }

    private static void WriteLineStringBody(StringBuilder sb, Geometry<Point> geometry, DxfColorInfo? colorInfo)
    {
        if (geometry.Points == null || geometry.Points.Count < 2) return;
        var inv = CultureInfo.InvariantCulture;

        sb.AppendLine("100");
        sb.AppendLine("AcDbPolyline");
        sb.AppendLine("90");
        sb.AppendLine(geometry.Points.Count.ToString());
        sb.AppendLine("70");
        sb.AppendLine("0"); // open polyline

        // Optional constant width
        if (colorInfo?.StrokeThickness > 0)
        {
            sb.AppendLine("43");
            sb.AppendLine(colorInfo.StrokeThickness.ToString("F6", inv));
        }

        foreach (var point in geometry.Points)
        {
            sb.AppendLine("10");
            sb.AppendLine(point.X.ToString("F6", inv));
            sb.AppendLine("20");
            sb.AppendLine(point.Y.ToString("F6", inv));
        }
    }

    private static void WritePolygonBody(StringBuilder sb, Geometry<Point> geometry, DxfColorInfo? colorInfo)
    {
        if (geometry.Geometries == null || geometry.Geometries.Count == 0) return;
        var exterior = geometry.Geometries[0];
        if (exterior.Points == null || exterior.Points.Count < 3) return;

        var inv = CultureInfo.InvariantCulture;

        sb.AppendLine("100");
        sb.AppendLine("AcDbPolyline");
        sb.AppendLine("90");
        sb.AppendLine(exterior.Points.Count.ToString());
        sb.AppendLine("70");
        sb.AppendLine("1"); // closed polyline

        if (colorInfo?.StrokeThickness > 0)
        {
            sb.AppendLine("43");
            sb.AppendLine(colorInfo.StrokeThickness.ToString("F6", inv));
        }

        foreach (var point in exterior.Points)
        {
            sb.AppendLine("10");
            sb.AppendLine(point.X.ToString("F6", inv));
            sb.AppendLine("20");
            sb.AppendLine(point.Y.ToString("F6", inv));
        }

        // Add HATCH for fill if fill color provided
        if (colorInfo?.FillColor != null)
            WriteHatchEntity(sb, geometry, colorInfo.FillColor.Value, colorInfo.Opacity);
    }

    // ----------------------------------------------------------------------
    // HATCH entity (solid fill)
    // ----------------------------------------------------------------------

    private static void WriteHatchEntity(StringBuilder sb, Geometry<Point> geometry, RgbColor fillColor, double opacity)
    {
        if (geometry.Geometries == null || geometry.Geometries.Count == 0) return;
        var exterior = geometry.Geometries[0];
        if (exterior.Points == null || exterior.Points.Count < 3) return;

        sb.AppendLine("0");
        sb.AppendLine("HATCH");
        sb.AppendLine("5");
        sb.AppendLine(GetNextHandle());
        sb.AppendLine("330");
        sb.AppendLine(HANDLE_BLOCK_RECORD_MODEL_SPACE);
        sb.AppendLine("100");
        sb.AppendLine("AcDbEntity");
        sb.AppendLine("8");
        sb.AppendLine("0");

        WriteColorCodes(sb, fillColor, opacity);

        sb.AppendLine("100");
        sb.AppendLine("AcDbHatch");
        sb.AppendLine("10");
        sb.AppendLine("0.0");
        sb.AppendLine("20");
        sb.AppendLine("0.0");
        sb.AppendLine("30");
        sb.AppendLine("0.0");
        sb.AppendLine("210");
        sb.AppendLine("0.0");
        sb.AppendLine("220");
        sb.AppendLine("0.0");
        sb.AppendLine("230");
        sb.AppendLine("1.0");
        sb.AppendLine("2");
        sb.AppendLine("SOLID");
        sb.AppendLine("70");
        sb.AppendLine("1");
        sb.AppendLine("71");
        sb.AppendLine("0");
        sb.AppendLine("91");
        sb.AppendLine(geometry.Geometries.Count.ToString());

        // Exterior boundary
        WriteHatchBoundary(sb, exterior.Points, isOuter: true);

        // Holes
        for (int i = 1; i < geometry.Geometries.Count; i++)
        {
            var hole = geometry.Geometries[i];
            if (hole.Points != null && hole.Points.Count > 0)
                WriteHatchBoundary(sb, hole.Points, isOuter: false);
        }

        sb.AppendLine("75");
        sb.AppendLine("1");
        sb.AppendLine("76");
        sb.AppendLine("1");
        sb.AppendLine("47");
        sb.AppendLine("0.0");
        sb.AppendLine("98");
        sb.AppendLine("0");
    }

    private static void WriteHatchBoundary(StringBuilder sb, List<Point> points, bool isOuter)
    {
        var inv = CultureInfo.InvariantCulture;
        sb.AppendLine("92");
        sb.AppendLine(isOuter ? "1" : "16");
        sb.AppendLine("93");
        sb.AppendLine("1");
        sb.AppendLine("72");
        sb.AppendLine("1");
        sb.AppendLine("97");
        sb.AppendLine(points.Count.ToString());

        foreach (var pt in points)
        {
            sb.AppendLine("10");
            sb.AppendLine(pt.X.ToString("F6", inv));
            sb.AppendLine("20");
            sb.AppendLine(pt.Y.ToString("F6", inv));
        }
        sb.AppendLine("97");
        sb.AppendLine("0");
    }

    // ----------------------------------------------------------------------
    // Color helpers
    // ----------------------------------------------------------------------

    private static void WriteColorCodes(StringBuilder sb, RgbColor color, double opacity)
    {
        sb.AppendLine("420");
        sb.AppendLine(color.ToDxfTrueColor().ToString());

        byte alpha = color.GetAlpha(opacity);
        if (alpha < 255)
        {
            sb.AppendLine("440");
            sb.AppendLine(alpha.ToString());
        }
    }

    // ----------------------------------------------------------------------
    // EOF
    // ----------------------------------------------------------------------

    private static void WriteEndOfFile(StringBuilder sb)
    {
        sb.AppendLine("0");
        sb.AppendLine("EOF");
    }
}