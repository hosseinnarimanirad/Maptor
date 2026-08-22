using System.Globalization;
using System.Text;
using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Spatial.IO.Prj;
using IRI.Maptor.Core.SpatialReferenceSystem;

namespace IRI.Maptor.Core.Spatial.IO.Dxf;

/// <summary>
/// Result of DXF preview extraction: detected SRID from file and sample of coordinate points.
/// </summary>
public class DxfPreviewResult
{
    public int DetectedSrid { get; }

    public IReadOnlyList<Point> SamplePoints { get; }

    public DxfPreviewResult(int detectedSrid, IReadOnlyList<Point> samplePoints)
    {
        DetectedSrid = detectedSrid;
        SamplePoints = samplePoints ?? Array.Empty<Point>();
    }
}

/// <summary>
/// A geometry extracted from a DXF file together with the CAD context it came from:
/// the source DXF layer, the entity type, the resolved color and whether the entity
/// is drawing annotation (text, dimensions, leaders, arrowheads, ...) rather than a
/// real-world feature.
/// </summary>
public class DxfFeature
{
    public Geometry<Point> Geometry { get; set; } = Geometry<Point>.Empty;

    /// <summary>
    /// Name of the DXF layer the source entity was drawn on (group code 8).
    /// </summary>
    public string DxfLayerName { get; set; } = "0";

    /// <summary>
    /// DXF entity type of the source entity (LINE, LWPOLYLINE, INSERT, TEXT, ...).
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Resolved color as #RRGGBB: the entity's true color (420) or ACI color (62),
    /// falling back to the color of its DXF layer. Null when no color is defined anywhere.
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Text content for TEXT/MTEXT/ATTRIB entities; null otherwise.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// True for CAD annotation/decoration — text, dimensions, leaders, SOLID/TRACE arrowheads,
    /// anonymous (*) block references and anything on the DEFPOINTS layer — as opposed to
    /// geometry representing real-world features.
    /// </summary>
    public bool IsAnnotation { get; set; }
}

/// <summary>
/// DXF (Drawing Exchange Format) reader for converting DXF files to Geometry types
/// </summary>
public class DxfReader
{
    /// <summary>
    /// Extracts preview data from a DXF file: detected SRID (if any) and up to maxSamplePoints coordinate pairs.
    /// </summary>
    public static async Task<DxfPreviewResult> GetPreviewAsync(string filePath, int maxSamplePoints = 50)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("DXF file not found", filePath);

        var content = await File.ReadAllTextAsync(filePath);
        var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        var detectedSrid = ExtractSridFromDxf(lines);
        var samplePoints = ExtractSamplePoints(lines, maxSamplePoints);

        return new DxfPreviewResult(detectedSrid, samplePoints);
    }

    public static async Task<List<Geometry<Point>>> ReadFromFile(string filePath, int? defaultSrid)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("DXF file not found", filePath);

        var content = await File.ReadAllTextAsync(filePath);
        return Read(content, defaultSrid);
    }

    public static List<Geometry<Point>> Read(string dxfContent, int? defaultSrid)
    {
        var features = ReadFeatures(dxfContent, defaultSrid);

        if (features.Count == 0)
            return [Geometry<Point>.Empty];

        return features.Select(f => f.Geometry).ToList();
    }

    public static async Task<List<DxfFeature>> ReadFeaturesFromFile(string filePath, int? defaultSrid)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("DXF file not found", filePath);

        var content = await File.ReadAllTextAsync(filePath);
        return ReadFeatures(content, defaultSrid);
    }

    /// <summary>
    /// Reads DXF content into geometries carrying their CAD context (DXF layer, entity type,
    /// resolved color, annotation flag) so callers can separate real-world features from
    /// drawing annotation and expose the CAD context as feature attributes.
    /// </summary>
    public static List<DxfFeature> ReadFeatures(string dxfContent, int? defaultSrid)
    {
        if (string.IsNullOrWhiteSpace(dxfContent))
            return [];

        var lines = dxfContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        // Use detected SRID from DXF only when user did not provide one
        if (!defaultSrid.HasValue || defaultSrid == 0)
        {
            var detectedSrid = ExtractSridFromDxf(lines);
            if (detectedSrid > 0)
                defaultSrid = detectedSrid;
        }

        defaultSrid = defaultSrid ?? SridHelper.GeodeticWGS84;

        var layerColors = ParseLayerColors(lines);

        var blocks = ParseBlocks(lines, defaultSrid.Value);

        var entities = ParseEntities(lines, defaultSrid.Value, blocks, layerColors);

        // ************************************************************************************
        // process polygons with holes
        // in the case of a polygon with holes they should not be returned as separated polygons
        // but they should be returned as a single polygon with holes. but in the case of multi-polygons
        // they should be returned as separated polygons
        var result = entities.Where(e => e.Geometry.Type != GeometryType.Polygon).ToList();

        // Real features and annotation are pooled separately so an arrowhead or hatch ring can
        // never be swallowed as the hole of a real parcel (and vice versa).
        foreach (var pool in entities.Where(e => e.Geometry.Type == GeometryType.Polygon).GroupBy(e => e.IsAnnotation))
        {
            // Every polygon entity contributes single rings; remember which entity owns each ring
            // (Geometry has reference equality) to re-attach the CAD context after reassembly.
            var ringOwners = new Dictionary<Geometry<Point>, DxfFeature>();

            var rings = new List<Geometry<Point>>();

            foreach (var entity in pool)
            {
                foreach (var ring in entity.Geometry.Geometries ?? [])
                {
                    rings.Add(ring);
                    ringOwners[ring] = entity;
                }
            }

            if (rings.IsNullOrEmpty())
                continue;

            var polygonOrMultiPolygon = Geometry<Point>.CreatePolygonOrMultiPolygon(rings, defaultSrid.Value);

            List<Geometry<Point>> polygons = polygonOrMultiPolygon.Type == GeometryType.MultiPolygon
                ? polygonOrMultiPolygon.Geometries!
                : [polygonOrMultiPolygon];

            foreach (var polygon in polygons)
            {
                // the exterior ring instance survives reassembly, so it identifies the source entity
                var owner = polygon.Geometries?.Count > 0 && ringOwners.TryGetValue(polygon.Geometries[0], out var o)
                    ? o
                    : pool.First();

                result.Add(new DxfFeature
                {
                    Geometry = polygon,
                    DxfLayerName = owner.DxfLayerName,
                    EntityType = owner.EntityType,
                    Color = owner.Color,
                    IsAnnotation = owner.IsAnnotation,
                });
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

    /// <summary>
    /// Extracts raw (x,y) coordinate pairs from ENTITIES section up to maxSamplePoints.
    /// </summary>
    private static List<Point> ExtractSamplePoints(string[] lines, int maxSamplePoints)
    {
        var points = new List<Point>();
        if (lines == null || lines.Length == 0)
            return points;

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

        if (entitiesStart < 0)
            return points;

        double? pendingX = null;
        for (int i = entitiesStart; i < lines.Length - 1 && points.Count < maxSamplePoints; i++)
        {
            var groupCode = lines[i].Trim();
            var value = lines[i + 1].Trim();

            if (groupCode == "0" && (value == "ENDSEC" || value == "EOF"))
                break;

            if (groupCode == "10" && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double x))
            {
                pendingX = x;
                i++;
            }
            else if ((groupCode == "20" || groupCode == "21") && pendingX.HasValue &&
                     double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double y))
            {
                points.Add(new Point(pendingX.Value, y));
                pendingX = null;
                i++;
            }
        }

        return points;
    }

    private static List<DxfFeature> ParseEntities(string[] lines, int srid, Dictionary<string, DxfBlock> blocks,
        Dictionary<string, string> layerColors)
    {
        var features = new List<DxfFeature>();

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
            return features;

        // Resolved block geometries are cached per block name — heavily reused
        // blocks (symbols placed hundreds of times) are expanded only once.
        var blockCache = new Dictionary<string, List<Geometry<Point>>>(StringComparer.OrdinalIgnoreCase);

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

                var meta = ScanEntityMetadata(lines, i_entity);

                if (entityType == "INSERT")
                {
                    var insert = ParseInsert(lines, ref i_entity);

                    if (insert != null)
                    {
                        // Anonymous blocks (*D dimensions, *X hatches, *U ad-hoc groups) are
                        // drawing machinery, not placed real-world symbols.
                        bool isAnnotation = insert.BlockName.StartsWith("*") || IsAnnotationLayer(meta.Layer);

                        // The insertion point is emitted as a Point feature (this matches
                        // ArcMap's CAD point feature class), followed by the referenced
                        // block's geometry transformed to the insertion site.
                        features.Add(CreateFeature(
                            Geometry<Point>.Create(insert.Ocs.ToWorldX(insert.X), insert.Y, srid),
                            entityType, meta, layerColors, isAnnotation));

                        var expanded = new List<Geometry<Point>>();
                        AppendInsertGeometries(expanded, ExpandInsert(insert, blocks, blockCache, srid, depth: 0), srid);

                        foreach (var geometry in expanded)
                            features.Add(CreateFeature(geometry, entityType, meta, layerColors, isAnnotation));
                    }
                }
                else if (entityType == "TEXT" || entityType == "MTEXT" || entityType == "ATTRIB")
                {
                    // TEXT/ATTRIB insertion points are OCS, MTEXT's is WCS
                    var (geometry, text) = ParseTextEntity(lines, ref i_entity, srid, applyOcs: entityType != "MTEXT");

                    if (geometry != null)
                    {
                        var feature = CreateFeature(geometry, entityType, meta, layerColors, isAnnotation: true);
                        feature.Text = text;
                        features.Add(feature);
                    }
                }
                else if (entityType == "LEADER")
                {
                    var geometry = ParseLeader(lines, ref i_entity, srid);

                    if (geometry != null)
                        features.Add(CreateFeature(geometry, entityType, meta, layerColors, isAnnotation: true));
                }
                else if (entityType == "HATCH")
                {
                    var geometry = ParseHatch(lines, ref i_entity, srid);

                    if (geometry != null)
                        features.Add(CreateFeature(geometry, entityType, meta, layerColors, isAnnotation: true));
                }
                else if (entityType == "WIPEOUT")
                {
                    var geometry = ParseWipeout(lines, ref i_entity, srid);

                    if (geometry != null)
                        features.Add(CreateFeature(geometry, entityType, meta, layerColors, isAnnotation: true));
                }
                else if (entityType == "DIMENSION")
                {
                    // A DIMENSION renders through an anonymous block (group 2) whose content is
                    // already drawn at the final position (base point 0,0) — use it untransformed.
                    var blockName = ParseDimensionBlockName(lines, ref i_entity);

                    if (blockName != null)
                    {
                        var expanded = new List<Geometry<Point>>();

                        // clone: the resolved geometries are shared via the block cache
                        AppendInsertGeometries(expanded,
                            ResolveBlockGeometries(blockName, blocks, blockCache, srid, depth: 0).Select(g => g.Clone()).ToList(),
                            srid);

                        foreach (var geometry in expanded)
                            features.Add(CreateFeature(geometry, entityType, meta, layerColors, isAnnotation: true));
                    }
                }
                else if (TryParseDrawableEntity(entityType, lines, ref i_entity, srid, out var geometry))
                {
                    if (geometry != null)
                        features.Add(CreateFeature(geometry, entityType, meta, layerColors,
                            IsAnnotationEntityType(entityType) || IsAnnotationLayer(meta.Layer)));
                }
                else
                {
                    // Skip unknown entity
                    i_entity++;
                }
            }
            else
            {
                i_entity++;
            }
        }

        return features;
    }

    /// <summary>
    /// SOLID/TRACE are filled 2D shapes used almost exclusively for arrowheads and hatching
    /// decoration; 3DFACE is a 3D visualization facet (TIN/model surface), not a mapped feature.
    /// </summary>
    private static bool IsAnnotationEntityType(string entityType) => entityType is "SOLID" or "TRACE" or "3DFACE";

    /// <summary>
    /// DEFPOINTS is AutoCAD's non-plotting layer holding dimension definition points.
    /// </summary>
    private static bool IsAnnotationLayer(string layerName) =>
        layerName.Equals("DEFPOINTS", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Common group codes shared by every DXF entity: 8 (layer), 62 (ACI color), 420 (true color).
    /// </summary>
    private struct DxfEntityMeta
    {
        public string Layer { get; set; }

        public int? Aci { get; set; }

        public int? TrueColor { get; set; }
    }

    /// <summary>
    /// Reads the common codes of the entity starting at <paramref name="index"/> (positioned at
    /// the entity type value) without consuming it. Stops at the next group 0, so for POLYLINE
    /// only the header — where these codes live — is inspected.
    /// </summary>
    private static DxfEntityMeta ScanEntityMetadata(string[] lines, int index)
    {
        string layer = "0";
        int? aci = null, trueColor = null;

        for (int i = index + 1; i < lines.Length - 1; i += 2)
        {
            var groupCode = lines[i].Trim();

            if (groupCode == "0")
                break;

            var value = lines[i + 1].Trim();

            switch (groupCode)
            {
                case "8":
                    layer = value;
                    break;

                case "62":
                    if (int.TryParse(value, out var colorIndex))
                        aci = colorIndex;
                    break;

                case "420":
                    if (int.TryParse(value, out var rgb))
                        trueColor = rgb;
                    break;
            }
        }

        return new DxfEntityMeta { Layer = layer, Aci = aci, TrueColor = trueColor };
    }

    private static DxfFeature CreateFeature(Geometry<Point> geometry, string entityType, in DxfEntityMeta meta,
        Dictionary<string, string> layerColors, bool isAnnotation)
    {
        return new DxfFeature
        {
            Geometry = geometry,
            EntityType = entityType,
            DxfLayerName = meta.Layer,
            Color = ResolveEntityColor(meta, layerColors),
            IsAnnotation = isAnnotation,
        };
    }

    /// <summary>
    /// Entity true color (420) wins over an explicit ACI color (62); 0 (ByBlock), 256 (ByLayer)
    /// and negative values fall back to the color of the entity's DXF layer.
    /// </summary>
    private static string? ResolveEntityColor(in DxfEntityMeta meta, Dictionary<string, string> layerColors)
    {
        if (meta.TrueColor.HasValue)
            return $"#{meta.TrueColor.Value & 0xFFFFFF:X6}";

        if (meta.Aci is > 0 and < 256)
            return DxfAciColor.ToHex(meta.Aci.Value);

        return layerColors.TryGetValue(meta.Layer, out var layerColor) ? layerColor : null;
    }

    /// <summary>
    /// Parses the LAYER table (TABLES section) into a layer name → #RRGGBB lookup. A negative
    /// color (layer switched off) keeps its color with the sign stripped. Returns an empty
    /// dictionary when the section is absent.
    /// </summary>
    private static Dictionary<string, string> ParseLayerColors(string[] lines)
    {
        var colors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        int tablesStart = -1;
        for (int i = 0; i < lines.Length - 3; i++)
        {
            if (lines[i].Trim() == "0" && lines[i + 1].Trim() == "SECTION" &&
                lines[i + 2].Trim() == "2" && lines[i + 3].Trim() == "TABLES")
            {
                tablesStart = i + 4;
                break;
            }
        }

        if (tablesStart == -1)
            return colors;

        string? layerName = null;
        int? aci = null, trueColor = null;
        bool inLayerRecord = false;

        // a DXF section is a strict sequence of (group code, value) pairs
        for (int i = tablesStart; i < lines.Length - 1; i += 2)
        {
            var groupCode = lines[i].Trim();
            var value = lines[i + 1].Trim();

            if (groupCode == "0")
            {
                CommitLayerColor(colors, layerName, aci, trueColor);
                layerName = null;
                aci = null;
                trueColor = null;

                if (value == "ENDSEC" || value == "EOF")
                    return colors;

                inLayerRecord = value == "LAYER";
            }
            else if (inLayerRecord)
            {
                switch (groupCode)
                {
                    case "2":
                        layerName = value;
                        break;

                    case "62":
                        if (int.TryParse(value, out var colorIndex))
                            aci = colorIndex;
                        break;

                    case "420":
                        if (int.TryParse(value, out var rgb))
                            trueColor = rgb;
                        break;
                }
            }
        }

        CommitLayerColor(colors, layerName, aci, trueColor);

        return colors;
    }

    private static void CommitLayerColor(Dictionary<string, string> colors, string? layerName, int? aci, int? trueColor)
    {
        if (string.IsNullOrEmpty(layerName))
            return;

        if (trueColor.HasValue)
        {
            colors[layerName] = $"#{trueColor.Value & 0xFFFFFF:X6}";
        }
        else if (aci.HasValue && Math.Abs(aci.Value) is >= 1 and <= 255)
        {
            colors[layerName] = DxfAciColor.ToHex(Math.Abs(aci.Value));
        }
    }

    /// <summary>
    /// Parses TEXT/MTEXT/ATTRIB into a Point at the insertion point plus the text content
    /// (group 1, preceded by 250-char group 3 chunks for long MTEXT).
    /// </summary>
    private static (Geometry<Point>? Geometry, string? Text) ParseTextEntity(string[] lines, ref int index, int srid, bool applyOcs)
    {
        double x = 0, y = 0;
        bool hasX = false, hasY = false;
        var text = new StringBuilder();
        var ocs = new Ocs();

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
                case "10": // Insertion X
                    if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out x))
                        hasX = true;
                    break;

                case "20": // Insertion Y
                    if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out y))
                        hasY = true;
                    break;

                case "3": // MTEXT: additional text chunks (precede group 1)
                case "1": // text value (the final chunk for MTEXT)
                    text.Append(value);
                    break;

                default:
                    if (applyOcs)
                        ocs.Read(groupCode, value);
                    break;
            }
        }

        if (!hasX || !hasY)
            return (null, null);

        return (Geometry<Point>.Create(ocs.ToWorldX(x), y, srid), text.Length == 0 ? null : text.ToString());
    }

    /// <summary>
    /// Parses a LEADER (annotation arrow/callout line) from its WCS vertices (repeated 10/20).
    /// </summary>
    private static Geometry<Point>? ParseLeader(string[] lines, ref int index, int srid)
    {
        var points = new List<Point>();
        double pendingX = 0;
        bool hasPendingX = false;

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
                case "10": // Vertex X
                    hasPendingX = double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out pendingX);
                    break;

                case "20": // Vertex Y
                    if (hasPendingX && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double vy))
                    {
                        points.Add(new Point(pendingX, vy));
                        hasPendingX = false;
                    }
                    break;
            }
        }

        if (points.Count == 0)
            return null;

        if (points.Count == 1)
            return Geometry<Point>.Create(points[0].X, points[0].Y, srid);

        return Geometry<Point>.Create(points, GeometryType.LineString, srid);
    }

    /// <summary>
    /// Consumes a DIMENSION entity and returns the name of the anonymous block (group 2)
    /// holding its rendered geometry, or null when absent.
    /// </summary>
    private static string? ParseDimensionBlockName(string[] lines, ref int index)
    {
        string? blockName = null;

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

            if (groupCode == "2")
                blockName = value;
        }

        return blockName;
    }

    /// <summary>
    /// Parses a HATCH boundary into a (multi-ring) polygon. Both boundary path flavors are
    /// supported: polyline paths (92 bit 1 set; vertices as 10/20, bulges flattened) and edge
    /// paths (line edges contribute their start point — consecutive edges share endpoints — and
    /// arc edges are sampled). The elevation point (10/20 before 91) and the seed points
    /// (after 98) are not boundary data and are ignored. Pattern data is skipped.
    /// </summary>
    private static Geometry<Point>? ParseHatch(string[] lines, ref int index, int srid)
    {
        var rings = new List<List<Point>>();
        List<Point>? currentRing = null;

        int pathsRemaining = 0;
        bool inPolylinePath = false;
        int edgeType = 0;
        double pendingX = 0;
        bool hasPendingX = false;

        // arc edge data (72 = 2): 10/20 center, 40 radius, 50/51 angles, 73 ccw flag (terminal)
        double arcCenterX = 0, arcCenterY = 0, arcRadius = 0, arcStartDeg = 0, arcEndDeg = 0;

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
                case "91": // Number of boundary paths
                    int.TryParse(value, out pathsRemaining);
                    break;

                case "92": // Boundary path type flag — starts a new path
                    if (pathsRemaining > 0 && int.TryParse(value, out int pathFlag))
                    {
                        currentRing = new List<Point>();
                        rings.Add(currentRing);
                        inPolylinePath = (pathFlag & 2) != 0;
                        edgeType = 0;
                        pathsRemaining--;
                    }
                    break;

                case "72": // Polyline path: has-bulge flag; edge path: edge type (1 line, 2 arc, 3 ellipse, 4 spline)
                    if (currentRing != null && !inPolylinePath)
                        int.TryParse(value, out edgeType);
                    break;

                case "10": // Vertex / line-edge start / arc-edge center X
                    if (currentRing != null)
                        hasPendingX = double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out pendingX);
                    break;

                case "20": // Vertex / line-edge start / arc-edge center Y
                    if (currentRing != null && hasPendingX &&
                        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double y))
                    {
                        if (!inPolylinePath && edgeType == 2)
                        {
                            arcCenterX = pendingX;
                            arcCenterY = y;
                        }
                        else if (inPolylinePath || edgeType == 1)
                        {
                            currentRing.Add(new Point(pendingX, y));
                        }
                        // ellipse/spline edge coordinates (center/control points) are not ring vertices

                        hasPendingX = false;
                    }
                    break;

                case "40": // Arc edge radius
                    if (currentRing != null && edgeType == 2)
                        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out arcRadius);
                    break;

                case "50": // Arc edge start angle (degrees)
                    if (currentRing != null && edgeType == 2)
                        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out arcStartDeg);
                    break;

                case "51": // Arc edge end angle (degrees)
                    if (currentRing != null && edgeType == 2)
                        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out arcEndDeg);
                    break;

                case "73": // Arc edge ccw flag — the arc edge's terminal code: sample it now
                    if (currentRing != null && !inPolylinePath && edgeType == 2 && arcRadius > 0)
                    {
                        AppendSampledArc(currentRing, arcCenterX, arcCenterY, arcRadius, arcStartDeg, arcEndDeg,
                            counterClockwise: value != "0");
                        arcRadius = 0;
                    }
                    break;

                case "98": // Seed point count — boundary data is over
                    currentRing = null;
                    pathsRemaining = 0;
                    break;
            }
        }

        var ringGeometries = new List<Geometry<Point>>();

        foreach (var ring in rings)
        {
            RemoveRepeatedClosingPoint(ring);

            if (ring.Count >= 3)
                ringGeometries.Add(Geometry<Point>.Create(ring, GeometryType.LineString, srid));
        }

        if (ringGeometries.Count == 0)
            return null;

        // outer/hole nesting is settled later by the file-wide polygon reassembly
        return Geometry<Point>.Create(ringGeometries, GeometryType.Polygon, srid);
    }

    private static void AppendSampledArc(List<Point> ring, double centerX, double centerY, double radius,
        double startDeg, double endDeg, bool counterClockwise, int segments = 8)
    {
        double start = startDeg * Math.PI / 180.0;
        double end = endDeg * Math.PI / 180.0;

        if (end <= start)
            end += 2 * Math.PI;

        var sampled = new List<Point>(segments + 1);

        for (int i = 0; i <= segments; i++)
        {
            double angle = start + (end - start) * i / segments;
            sampled.Add(new Point(centerX + radius * Math.Cos(angle), centerY + radius * Math.Sin(angle)));
        }

        if (!counterClockwise)
            sampled.Reverse();

        ring.AddRange(sampled);
    }

    /// <summary>
    /// Parses a WIPEOUT (masking frame) into a polygon. Clip boundary vertices (14/24) live in a
    /// unit square centered on the image — (-0.5,-0.5)..(0.5,0.5) with +Y pointing down — so
    /// world = insertion (10/20) + U (11/21)·(x + 0.5) + V (12/22)·(0.5 − y). A rectangular clip
    /// (2 vertices) is expanded to its 4 corners.
    /// </summary>
    private static Geometry<Point>? ParseWipeout(string[] lines, ref int index, int srid)
    {
        double insertX = 0, insertY = 0, uX = 0, uY = 0, vX = 0, vY = 0;
        var boundary = new List<Point>(); // image space
        double pendingX = 0;
        bool hasPendingX = false;

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
                case "10":
                    double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out insertX);
                    break;

                case "20":
                    double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out insertY);
                    break;

                case "11":
                    double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out uX);
                    break;

                case "21":
                    double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out uY);
                    break;

                case "12":
                    double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out vX);
                    break;

                case "22":
                    double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out vY);
                    break;

                case "14": // Clip boundary vertex X (image space)
                    hasPendingX = double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out pendingX);
                    break;

                case "24": // Clip boundary vertex Y (image space)
                    if (hasPendingX && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double by))
                    {
                        boundary.Add(new Point(pendingX, by));
                        hasPendingX = false;
                    }
                    break;
            }
        }

        if (boundary.Count == 2) // rectangular clip: two opposite corners
        {
            boundary = new List<Point>
            {
                boundary[0],
                new Point(boundary[1].X, boundary[0].Y),
                boundary[1],
                new Point(boundary[0].X, boundary[1].Y),
            };
        }

        if (boundary.Count < 3)
            return null;

        var points = boundary
            .Select(p => new Point(
                insertX + uX * (p.X + 0.5) + vX * (0.5 - p.Y),
                insertY + uY * (p.X + 0.5) + vY * (0.5 - p.Y)))
            .ToList();

        RemoveRepeatedClosingPoint(points);

        if (points.Count < 3)
            return null;

        var ring = Geometry<Point>.Create(points, GeometryType.LineString, srid);
        return Geometry<Point>.Create(new List<Geometry<Point>> { ring }, GeometryType.Polygon, srid);
    }

    /// <summary>
    /// Dispatches a drawable entity to its parser. Returns false when the entity type is not
    /// supported (index untouched, caller skips it); true when it was parsed — geometry may still
    /// be null for malformed/degenerate entities.
    /// </summary>
    private static bool TryParseDrawableEntity(string entityType, string[] lines, ref int index, int srid, out Geometry<Point>? geometry)
    {
        switch (entityType)
        {
            case "POINT":
                geometry = ParsePoint(lines, ref index, srid);
                return true;

            case "LINE":
                geometry = ParseLine(lines, ref index, srid);
                return true;

            case "LWPOLYLINE":
                geometry = ParseLwPolyline(lines, ref index, srid);
                return true;

            case "POLYLINE":
                geometry = ParsePolyline(lines, ref index, srid);
                return true;

            case "CIRCLE":
                // Circles are approximated as polygons
                geometry = ParseCircle(lines, ref index, srid);
                return true;

            case "ARC":
                // Arcs are approximated as line strings
                geometry = ParseArc(lines, ref index, srid);
                return true;

            case "ELLIPSE":
                // Full ellipses become polygons, elliptical arcs become line strings
                geometry = ParseEllipse(lines, ref index, srid);
                return true;

            case "SPLINE":
                // Approximated as a line string (or polygon when closed)
                geometry = ParseSpline(lines, ref index, srid);
                return true;

            case "SOLID":
            case "TRACE": // TRACE has the same structure as SOLID
                geometry = ParseFourCornerShape(lines, ref index, srid, zigzagOrder: true, applyOcs: true);
                return true;

            case "3DFACE":
                // True-3D entity: corners are WCS and already in ring order
                geometry = ParseFourCornerShape(lines, ref index, srid, zigzagOrder: false, applyOcs: false);
                return true;

            default:
                geometry = null;
                return false;
        }
    }

    /// <summary>
    /// Tracks the extrusion direction (group codes 210/220/230) of an entity.
    /// Planar entities (LWPOLYLINE, 2D POLYLINE, CIRCLE, ARC, SOLID, INSERT) store their
    /// coordinates in the Object Coordinate System (OCS) defined by this direction; when the
    /// extrusion is (0,0,-1) the OCS is the WCS mirrored about the Y axis, so every X must be
    /// negated to get world coordinates. True-3D entities (POINT, LINE, ELLIPSE, SPLINE, 3DFACE)
    /// store WCS coordinates and must NOT be mirrored. Entities with an arbitrary (tilted)
    /// extrusion axis are left untouched — they are rare and need the full Arbitrary Axis Algorithm.
    /// </summary>
    private struct Ocs
    {
        private double _x, _y, _z;

        private bool _hasZ;

        public void Read(string groupCode, string value)
        {
            switch (groupCode)
            {
                case "210":
                    double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _x);
                    break;

                case "220":
                    double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _y);
                    break;

                case "230":
                    if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _z))
                        _hasZ = true;
                    break;
            }
        }

        /// <summary>
        /// True when the entity is drawn on a plane whose normal is (0,0,-1); its X values are mirrored.
        /// </summary>
        public bool IsMirrored => _hasZ && _z < 0 && Math.Abs(_x) < 1e-10 && Math.Abs(_y) < 1e-10;

        public double ToWorldX(double x) => IsMirrored ? -x : x;

        public void ToWorld(List<Point> points)
        {
            if (!IsMirrored)
                return;

            for (int i = 0; i < points.Count; i++)
                points[i] = new Point(-points[i].X, points[i].Y);
        }
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
            return Geometry<Point>.Create(points, GeometryType.LineString, srid);
        }

        return null;
    }

    private static Geometry<Point>? ParseLwPolyline(string[] lines, ref int index, int srid)
    {
        var points = new List<Point>();
        bool isClosed = false;
        int numVertices = 0;
        var ocs = new Ocs();

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

                case "70": // Polyline flag; bit 0 (value 1) = closed, other bits (e.g. 128 = plinegen) may also be set
                    if (int.TryParse(value, out int flag))
                        isClosed = (flag & 1) != 0;
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

                default:
                    ocs.Read(groupCode, value);
                    break;
            }
        }

        if (points.Count == 0)
            return null;

        ocs.ToWorld(points);

        if (points.Count == 1)
            return Geometry<Point>.Create(points[0].X, points[0].Y, srid);

        if (isClosed)
        {
            RemoveRepeatedClosingPoint(points);

            if (points.Count >= 3)
            {
                // Create a polygon
                var ring = Geometry<Point>.Create(points, GeometryType.LineString, srid);
                return Geometry<Point>.Create(new List<Geometry<Point>> { ring }, GeometryType.Polygon, srid);
            }
        }

        // Create a line string
        return Geometry<Point>.Create(points, GeometryType.LineString, srid);
    }

    /// <summary>
    /// A DXF closed polyline may or may not repeat its first vertex as the last one, but the ring
    /// consumers (<see cref="Analysis.SpatialUtility.GetSignedEuclideanArea{T}"/> and therefore
    /// <see cref="Geometry{T}.CreatePolygonOrMultiPolygon"/>) require rings whose closing point is
    /// not repeated. Drop it so both spellings produce the same polygon.
    /// </summary>
    private static void RemoveRepeatedClosingPoint(List<Point> points)
    {
        while (points.Count > 1 &&
               points[0].X == points[points.Count - 1].X &&
               points[0].Y == points[points.Count - 1].Y)
        {
            points.RemoveAt(points.Count - 1);
        }
    }

    private static Geometry<Point>? ParsePolyline(string[] lines, ref int index, int srid)
    {
        var points = new List<Point>();
        bool isClosed = false;
        var ocs = new Ocs();

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
            else
            {
                ocs.Read(groupCode, value);
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

        ocs.ToWorld(points);

        if (points.Count == 1)
            return Geometry<Point>.Create(points[0].X, points[0].Y, srid);

        if (isClosed)
        {
            RemoveRepeatedClosingPoint(points);

            if (points.Count >= 3)
            {
                var ring = Geometry<Point>.Create(points, GeometryType.LineString, srid);
                return Geometry<Point>.Create(new List<Geometry<Point>> { ring }, GeometryType.Polygon, srid);
            }
        }

        return Geometry<Point>.Create(points, GeometryType.LineString, srid);
    }

    private static Geometry<Point>? ParseCircle(string[] lines, ref int index, int srid, int segments = 32)
    {
        double centerX = 0, centerY = 0, radius = 0;
        bool hasCenter = false, hasRadius = false;
        var ocs = new Ocs();

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

                default:
                    ocs.Read(groupCode, value);
                    break;
            }
        }

        if (hasCenter && hasRadius && radius > 0)
        {
            centerX = ocs.ToWorldX(centerX);

            // Approximate circle as a polygon
            var points = new List<Point>();
            for (int i = 0; i < segments; i++)
            {
                double angle = 2 * Math.PI * i / segments;
                double x = centerX + radius * Math.Cos(angle);
                double y = centerY + radius * Math.Sin(angle);
                points.Add(new Point(x, y));
            }

            var ring = Geometry<Point>.Create(points, GeometryType.LineString, srid);
            return Geometry<Point>.Create(new List<Geometry<Point>> { ring }, GeometryType.Polygon, srid);
        }

        return null;
    }

    private static Geometry<Point>? ParseArc(string[] lines, ref int index, int srid, int segments = 32)
    {
        double centerX = 0, centerY = 0, radius = 0;
        double startAngle = 0, endAngle = 0;
        bool hasCenter = false, hasRadius = false;
        bool hasStartAngle = false, hasEndAngle = false;
        var ocs = new Ocs();

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

                default:
                    ocs.Read(groupCode, value);
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

            // Approximate arc as a line string. The centre and the angles are expressed in the
            // entity's OCS, so the arc is sampled there first and mirrored into the WCS afterwards.
            var points = new List<Point>();
            for (int i = 0; i <= segments; i++)
            {
                double t = (double)i / segments;
                double angle = startRad + angleRange * t;
                double x = centerX + radius * Math.Cos(angle);
                double y = centerY + radius * Math.Sin(angle);
                points.Add(new Point(x, y));
            }

            ocs.ToWorld(points);

            if (points.Count >= 2)
                return Geometry<Point>.Create(points, GeometryType.LineString, srid);
        }

        return null;
    }

    #region Blocks & INSERT

    /// <summary>
    /// Guard against self-referencing block definitions; real drawings rarely nest deeper than 2-3 levels.
    /// </summary>
    private const int MaxBlockNestingDepth = 8;

    /// <summary>
    /// A block definition from the BLOCKS section: geometry in block-local coordinates
    /// plus any nested block references, expanded lazily on INSERT.
    /// </summary>
    private class DxfBlock
    {
        public string Name = string.Empty;

        public double BaseX, BaseY; // group 10/20 of the BLOCK header

        public List<Geometry<Point>> LocalGeometries = new();

        public List<DxfInsert> NestedInserts = new();
    }

    /// <summary>
    /// An INSERT (block reference) entity: which block to place, where, and how it is
    /// scaled/rotated. Columns/rows describe MINSERT arrays (a grid of copies).
    /// </summary>
    private class DxfInsert
    {
        public string BlockName = string.Empty; // group 2

        public double X, Y;                     // 10/20 insertion point (in the insert's OCS)

        public double ScaleX = 1, ScaleY = 1;   // 41/42 (negative = mirrored)

        public double RotationDeg;              // 50

        public Ocs Ocs;                         // 210/220/230

        public int Columns = 1, Rows = 1;       // 70/71 (MINSERT)

        public double ColumnSpacing, RowSpacing; // 44/45 (MINSERT)
    }

    /// <summary>
    /// Parses the BLOCKS section into a name → definition lookup. Block names are
    /// case-insensitive in DXF. Returns an empty dictionary when the section is absent.
    /// </summary>
    private static Dictionary<string, DxfBlock> ParseBlocks(string[] lines, int srid)
    {
        var blocks = new Dictionary<string, DxfBlock>(StringComparer.OrdinalIgnoreCase);

        // Find BLOCKS section
        int blocksStart = -1;
        for (int i = 0; i < lines.Length - 3; i++)
        {
            if (lines[i].Trim() == "0" && lines[i + 1].Trim() == "SECTION" &&
                lines[i + 2].Trim() == "2" && lines[i + 3].Trim() == "BLOCKS")
            {
                blocksStart = i + 4;
                break;
            }
        }

        if (blocksStart == -1)
            return blocks;

        int index = blocksStart;
        while (index < lines.Length)
        {
            if (lines[index].Trim() == "0")
            {
                index++;
                if (index >= lines.Length)
                    break;

                var marker = lines[index].Trim();

                if (marker == "ENDSEC" || marker == "EOF")
                    break;

                if (marker == "BLOCK")
                {
                    var block = ParseBlock(lines, ref index, srid);

                    if (block != null && block.Name.Length > 0 && !blocks.ContainsKey(block.Name))
                        blocks.Add(block.Name, block);
                }
                else
                {
                    index++;
                }
            }
            else
            {
                index++;
            }
        }

        return blocks;
    }

    private static DxfBlock? ParseBlock(string[] lines, ref int index, int srid)
    {
        var block = new DxfBlock();

        index++; // Move past "BLOCK"

        // Parse BLOCK header (name and base point) until the first contained entity
        while (index < lines.Length - 1)
        {
            var groupCode = lines[index].Trim();
            index++;

            if (groupCode == "0") // First contained entity (or ENDBLK)
            {
                index--; // Back up
                break;
            }

            var value = lines[index].Trim();
            index++;

            switch (groupCode)
            {
                case "2": // Block name (group 3 repeats it; keep the first)
                    if (block.Name.Length == 0)
                        block.Name = value;
                    break;

                case "10": // Base point X
                    double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out block.BaseX);
                    break;

                case "20": // Base point Y
                    double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out block.BaseY);
                    break;
            }
        }

        // Parse contained entities until ENDBLK
        while (index < lines.Length)
        {
            if (lines[index].Trim() == "0")
            {
                index++;
                if (index >= lines.Length)
                    break;

                var entityType = lines[index].Trim();

                if (entityType == "ENDBLK" || entityType == "ENDSEC" || entityType == "EOF")
                    break;

                if (entityType == "INSERT")
                {
                    var insert = ParseInsert(lines, ref index);

                    if (insert != null)
                        block.NestedInserts.Add(insert);
                }
                else if (TryParseDrawableEntity(entityType, lines, ref index, srid, out var geometry))
                {
                    if (geometry != null)
                        block.LocalGeometries.Add(geometry);
                }
                else
                {
                    index++; // Skip unknown entity
                }
            }
            else
            {
                index++;
            }
        }

        return block;
    }

    private static DxfInsert? ParseInsert(string[] lines, ref int index)
    {
        var insert = new DxfInsert();

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
                case "2": // Block name
                    insert.BlockName = value;
                    break;

                case "10": // Insertion X
                    double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out insert.X);
                    break;

                case "20": // Insertion Y
                    double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out insert.Y);
                    break;

                case "41": // X scale
                    if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double sx) && sx != 0)
                        insert.ScaleX = sx;
                    break;

                case "42": // Y scale
                    if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double sy) && sy != 0)
                        insert.ScaleY = sy;
                    break;

                case "50": // Rotation angle (degrees)
                    double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out insert.RotationDeg);
                    break;

                case "70": // Column count (MINSERT)
                    if (int.TryParse(value, out int cols) && cols > 1)
                        insert.Columns = cols;
                    break;

                case "71": // Row count (MINSERT)
                    if (int.TryParse(value, out int rows) && rows > 1)
                        insert.Rows = rows;
                    break;

                case "44": // Column spacing (MINSERT)
                    double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out insert.ColumnSpacing);
                    break;

                case "45": // Row spacing (MINSERT)
                    double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out insert.RowSpacing);
                    break;

                default:
                    insert.Ocs.Read(groupCode, value);
                    break;
            }
        }

        return insert.BlockName.Length > 0 ? insert : null;
    }

    /// <summary>
    /// Returns a block's complete geometry in block-local coordinates: its own entities plus the
    /// recursively expanded geometry of any nested block references. Results are memoized per
    /// block name so repeatedly inserted symbols are resolved only once.
    /// </summary>
    private static List<Geometry<Point>> ResolveBlockGeometries(string blockName, Dictionary<string, DxfBlock> blocks,
        Dictionary<string, List<Geometry<Point>>> cache, int srid, int depth)
    {
        if (depth > MaxBlockNestingDepth || !blocks.TryGetValue(blockName, out var block))
            return new List<Geometry<Point>>();

        if (cache.TryGetValue(blockName, out var cached))
            return cached;

        var result = new List<Geometry<Point>>(block.LocalGeometries);

        foreach (var nested in block.NestedInserts)
        {
            result.AddRange(ExpandInsert(nested, blocks, cache, srid, depth + 1));
        }

        cache[blockName] = result;

        return result;
    }

    /// <summary>
    /// Expands a block reference into world (parent) coordinates:
    /// translate by −base point → scale → rotate → translate to the insertion point,
    /// all inside the insert's OCS, then mirror the whole result when the OCS is (0,0,-1).
    /// MINSERT arrays repeat the expansion on a column/row grid.
    /// </summary>
    private static List<Geometry<Point>> ExpandInsert(DxfInsert insert, Dictionary<string, DxfBlock> blocks,
        Dictionary<string, List<Geometry<Point>>> cache, int srid, int depth)
    {
        var result = new List<Geometry<Point>>();

        if (depth > MaxBlockNestingDepth || !blocks.TryGetValue(insert.BlockName, out var block))
            return result;

        var localGeometries = ResolveBlockGeometries(insert.BlockName, blocks, cache, srid, depth);

        if (localGeometries.Count == 0)
            return result;

        double rotation = insert.RotationDeg * Math.PI / 180.0;
        double cos = Math.Cos(rotation), sin = Math.Sin(rotation);
        bool mirrored = insert.Ocs.IsMirrored;

        for (int col = 0; col < insert.Columns; col++)
        {
            for (int row = 0; row < insert.Rows; row++)
            {
                double columnOffset = col * insert.ColumnSpacing;
                double rowOffset = row * insert.RowSpacing;

                foreach (var geometry in localGeometries)
                {
                    result.Add(geometry.Transform(p =>
                    {
                        double x = (p.X - block.BaseX) * insert.ScaleX + columnOffset;
                        double y = (p.Y - block.BaseY) * insert.ScaleY + rowOffset;

                        double worldX = x * cos - y * sin + insert.X;
                        double worldY = x * sin + y * cos + insert.Y;

                        if (mirrored)
                            worldX = -worldX;

                        return new Point(worldX, worldY);
                    }, srid));
                }
            }
        }

        return result;
    }

    /// <summary>
    /// A block reference is one symbol, so its expanded geometry is merged into (at most) one
    /// multi-part feature per geometry class — mirroring how ArcMap surfaces block references —
    /// instead of flooding the result with one feature per entity inside the block.
    /// </summary>
    private static void AppendInsertGeometries(List<Geometry<Point>> target, List<Geometry<Point>> expanded, int srid)
    {
        if (expanded.Count == 0)
            return;

        var points = new List<Geometry<Point>>();
        var lineStrings = new List<Geometry<Point>>();
        var polygons = new List<Geometry<Point>>();

        foreach (var geometry in expanded)
        {
            switch (geometry.Type)
            {
                case GeometryType.Point:
                    points.Add(geometry);
                    break;

                case GeometryType.MultiPoint:
                    points.AddRange(geometry.Geometries);
                    break;

                case GeometryType.LineString:
                    lineStrings.Add(geometry);
                    break;

                case GeometryType.MultiLineString:
                    lineStrings.AddRange(geometry.Geometries);
                    break;

                case GeometryType.Polygon:
                    polygons.Add(geometry);
                    break;

                case GeometryType.MultiPolygon:
                    polygons.AddRange(geometry.Geometries);
                    break;
            }
        }

        if (points.Count == 1)
            target.Add(points[0]);
        else if (points.Count > 1)
            target.Add(Geometry<Point>.Create(points, GeometryType.MultiPoint, srid));

        if (lineStrings.Count == 1)
            target.Add(lineStrings[0]);
        else if (lineStrings.Count > 1)
            target.Add(Geometry<Point>.Create(lineStrings, GeometryType.MultiLineString, srid));

        // Polygons stay multi-part even when there is only one, so a symbol's rings do not get
        // pulled into the file-wide polygon/hole reassembly in Read() — concentric circles of a
        // symbol are sibling parts of one feature, not holes of some enclosing parcel.
        if (polygons.Count > 0)
            target.Add(Geometry<Point>.Create(polygons, GeometryType.MultiPolygon, srid));
    }

    #endregion

    private static Geometry<Point>? ParseEllipse(string[] lines, ref int index, int srid, int segments = 32)
    {
        // ELLIPSE coordinates are WCS (no OCS): center 10/20, major-axis endpoint 11/21 as a
        // vector RELATIVE to the center, 40 = minor/major ratio, 41/42 = start/end parameters
        // in radians (0 and 2π for a full ellipse).
        double centerX = 0, centerY = 0, majorX = 0, majorY = 0;
        double ratio = 1, startParam = 0, endParam = 2 * Math.PI;
        bool hasCenter = false, hasMajor = false;

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

                case "11": // Major axis endpoint X (relative to center)
                    if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out majorX))
                        hasMajor = true;
                    break;

                case "21": // Major axis endpoint Y (relative to center)
                    if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out majorY))
                        hasMajor = true;
                    break;

                case "40": // Ratio of minor axis to major axis
                    double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out ratio);
                    break;

                case "41": // Start parameter (radians)
                    double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out startParam);
                    break;

                case "42": // End parameter (radians)
                    double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out endParam);
                    break;
            }
        }

        if (!hasCenter || !hasMajor)
            return null;

        if (endParam <= startParam)
            endParam += 2 * Math.PI;

        double range = endParam - startParam;
        bool isFullEllipse = range >= 2 * Math.PI - 1e-9;

        // Minor axis = ratio × (major axis rotated 90° CCW)
        double minorX = -majorY * ratio;
        double minorY = majorX * ratio;

        var points = new List<Point>();
        int pointCount = isFullEllipse ? segments : segments + 1; // rings do not repeat the closing point

        for (int i = 0; i < pointCount; i++)
        {
            double t = startParam + range * i / segments;
            double x = centerX + Math.Cos(t) * majorX + Math.Sin(t) * minorX;
            double y = centerY + Math.Cos(t) * majorY + Math.Sin(t) * minorY;
            points.Add(new Point(x, y));
        }

        if (isFullEllipse && points.Count >= 3)
        {
            var ring = Geometry<Point>.Create(points, GeometryType.LineString, srid);
            return Geometry<Point>.Create(new List<Geometry<Point>> { ring }, GeometryType.Polygon, srid);
        }

        return Geometry<Point>.Create(points, GeometryType.LineString, srid);
    }

    private static Geometry<Point>? ParseSpline(string[] lines, ref int index, int srid)
    {
        // SPLINE coordinates are WCS: 70 flags (bit 0 = closed), 71 degree, repeated 40 = knots,
        // repeated 41 = weights, repeated 10/20 = control points, repeated 11/21 = fit points.
        var controlPoints = new List<Point>();
        var fitPoints = new List<Point>();
        var knots = new List<double>();
        var weights = new List<double>();
        int degree = 3;
        bool isClosed = false;
        double pendingControlX = 0, pendingFitX = 0;
        bool hasPendingControlX = false, hasPendingFitX = false;

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
                case "70": // Spline flag
                    if (int.TryParse(value, out int flag))
                        isClosed = (flag & 1) != 0;
                    break;

                case "71": // Degree
                    int.TryParse(value, out degree);
                    break;

                case "40": // Knot value
                    if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double knot))
                        knots.Add(knot);
                    break;

                case "41": // Weight
                    if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double weight))
                        weights.Add(weight);
                    break;

                case "10": // Control point X
                    hasPendingControlX = double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out pendingControlX);
                    break;

                case "20": // Control point Y
                    if (hasPendingControlX && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double cy))
                    {
                        controlPoints.Add(new Point(pendingControlX, cy));
                        hasPendingControlX = false;
                    }
                    break;

                case "11": // Fit point X
                    hasPendingFitX = double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out pendingFitX);
                    break;

                case "21": // Fit point Y
                    if (hasPendingFitX && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double fy))
                    {
                        fitPoints.Add(new Point(pendingFitX, fy));
                        hasPendingFitX = false;
                    }
                    break;
            }
        }

        // The curve passes through fit points, so they are already a faithful approximation;
        // otherwise evaluate the B-spline, falling back to the control polygon on degenerate data.
        List<Point> points;
        if (fitPoints.Count >= 2)
            points = fitPoints;
        else if (controlPoints.Count >= 2)
            points = EvaluateBSpline(controlPoints, knots, weights, degree) ?? controlPoints;
        else
            return null;

        if (isClosed)
        {
            points = new List<Point>(points);
            RemoveRepeatedClosingPoint(points);

            if (points.Count >= 3)
            {
                var ring = Geometry<Point>.Create(points, GeometryType.LineString, srid);
                return Geometry<Point>.Create(new List<Geometry<Point>> { ring }, GeometryType.Polygon, srid);
            }
        }

        return Geometry<Point>.Create(points, GeometryType.LineString, srid);
    }

    /// <summary>
    /// Samples a (rational) B-spline with the de Boor algorithm. Returns null when the knot
    /// vector does not match the control points/degree so the caller can fall back.
    /// </summary>
    private static List<Point>? EvaluateBSpline(List<Point> controlPoints, List<double> knots, List<double> weights, int degree)
    {
        int n = controlPoints.Count;

        if (degree < 1 || degree >= n || knots.Count != n + degree + 1)
            return null;

        bool isRational = weights.Count == n;

        double tStart = knots[degree];
        double tEnd = knots[n];

        if (!(tEnd > tStart))
            return null;

        int samples = Math.Max(32, 4 * n);
        var result = new List<Point>(samples + 1);

        for (int s = 0; s <= samples; s++)
        {
            double t = tStart + (tEnd - tStart) * s / samples;

            // Find the knot span k with knots[k] <= t < knots[k+1] (clamped to the last span)
            int k = degree;
            for (int i = degree; i < n; i++)
            {
                k = i;
                if (t < knots[i + 1])
                    break;
            }

            // de Boor recursion on homogeneous coordinates (x·w, y·w, w)
            var dx = new double[degree + 1];
            var dy = new double[degree + 1];
            var dw = new double[degree + 1];

            for (int j = 0; j <= degree; j++)
            {
                int idx = j + k - degree;
                double w = isRational ? weights[idx] : 1.0;
                dx[j] = controlPoints[idx].X * w;
                dy[j] = controlPoints[idx].Y * w;
                dw[j] = w;
            }

            for (int r = 1; r <= degree; r++)
            {
                for (int j = degree; j >= r; j--)
                {
                    int i = j + k - degree;
                    double denominator = knots[i + degree - r + 1] - knots[i];
                    double alpha = denominator <= 0 ? 0 : (t - knots[i]) / denominator;

                    dx[j] = (1 - alpha) * dx[j - 1] + alpha * dx[j];
                    dy[j] = (1 - alpha) * dy[j - 1] + alpha * dy[j];
                    dw[j] = (1 - alpha) * dw[j - 1] + alpha * dw[j];
                }
            }

            if (dw[degree] == 0)
                return null;

            result.Add(new Point(dx[degree] / dw[degree], dy[degree] / dw[degree]));
        }

        return result;
    }

    /// <summary>
    /// Parses SOLID/TRACE (zigzag corner order 1,2,4,3; OCS coordinates) and 3DFACE
    /// (ring order 1,2,3,4; WCS coordinates) into a polygon. A duplicated or missing
    /// fourth corner yields a triangle.
    /// </summary>
    private static Geometry<Point>? ParseFourCornerShape(string[] lines, ref int index, int srid, bool zigzagOrder, bool applyOcs)
    {
        var corners = new double[4, 2];
        var hasCorner = new bool[4];
        var ocs = new Ocs();

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
                case "10": case "11": case "12": case "13":
                    {
                        int corner = groupCode[1] - '0';
                        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out corners[corner, 0]))
                            hasCorner[corner] = true;
                    }
                    break;

                case "20": case "21": case "22": case "23":
                    double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out corners[groupCode[1] - '0', 1]);
                    break;

                default:
                    if (applyOcs)
                        ocs.Read(groupCode, value);
                    break;
            }
        }

        if (!hasCorner[0] || !hasCorner[1] || !hasCorner[2])
            return null;

        // SOLID stores its corners in zigzag order (1,2,4,3 traces the outline);
        // 3DFACE stores them already in ring order.
        var ringOrder = zigzagOrder ? new[] { 0, 1, 3, 2 } : new[] { 0, 1, 2, 3 };

        var points = new List<Point>();
        foreach (var corner in ringOrder)
        {
            if (!hasCorner[corner])
                continue;

            var candidate = new Point(corners[corner, 0], corners[corner, 1]);

            // Skip consecutive duplicates (triangles repeat a corner)
            if (points.Count == 0 || points[points.Count - 1].X != candidate.X || points[points.Count - 1].Y != candidate.Y)
                points.Add(candidate);
        }

        ocs.ToWorld(points);
        RemoveRepeatedClosingPoint(points);

        if (points.Count >= 3)
        {
            var ring = Geometry<Point>.Create(points, GeometryType.LineString, srid);
            return Geometry<Point>.Create(new List<Geometry<Point>> { ring }, GeometryType.Polygon, srid);
        }

        if (points.Count == 2)
            return Geometry<Point>.Create(points, GeometryType.LineString, srid);

        return null;
    }
}

