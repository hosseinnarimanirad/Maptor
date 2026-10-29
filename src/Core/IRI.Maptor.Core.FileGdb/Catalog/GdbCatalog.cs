using System.Xml;
using System.Xml.Linq;

using IRI.Maptor.Core.FileGdb.Catalog.Model;
using IRI.Maptor.Core.FileGdb.Catalog.Xml;
using IRI.Maptor.Core.FileGdb.Enums;
using IRI.Maptor.Core.Spatial.IO.Prj;

namespace IRI.Maptor.Core.FileGdb.Catalog;

/// <summary>
/// The user-facing content of a file geodatabase: datasets, feature datasets, domains (with
/// subtypes on the datasets) and relationship classes, read from the item definitions in
/// GDB_Items and joined with the physical tables in GDB_SystemCatalog.
/// </summary>
/// <remarks>
/// Left out entirely (user decision 2026-10-13): topology bookkeeping tables
/// (<c>T_{id}_DirtyAreas</c>, <c>_PointErrors</c>, <c>_LineErrors</c>, <c>_PolyErrors</c>), attachment
/// tables and their relationship classes, and raster datasets with their <c>fras_*</c> helper
/// tables. Tables that exist with no item definition are listed with
/// <see cref="GdbDatasetInfo.HasDefinition"/> false.
/// </remarks>
internal sealed class GdbCatalog
{
    private const string ItemsTableName = "GDB_Items";

    private static readonly string[] TopologyTableSuffixes = ["DirtyAreas", "PointErrors", "LineErrors", "PolyErrors"];

    private static readonly string[] RasterHelperPrefixes = ["fras_ras_", "fras_aux_", "fras_bnd_", "fras_blk_"];

    private readonly Dictionary<string, FileGdbDomain> _domains;

    private readonly Dictionary<string, GdbDatasetInfo> _datasetsByName;

    private GdbCatalog(
        string path,
        IReadOnlyList<GdbCatalogEntry> tables,
        IReadOnlyList<GdbDatasetInfo> datasets,
        IReadOnlyList<FileGdbFeatureDataset> featureDatasets,
        IReadOnlyList<FileGdbRelationshipClass> relationshipClasses,
        Dictionary<string, FileGdbDomain> domains,
        IReadOnlyList<string> warnings)
    {
        Path = path;
        Tables = tables;
        Datasets = datasets;
        FeatureDatasets = featureDatasets;
        RelationshipClasses = relationshipClasses;
        _domains = domains;
        Warnings = warnings;

        _datasetsByName = new Dictionary<string, GdbDatasetInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var dataset in datasets)
            _datasetsByName[dataset.Name] = dataset;
    }

    public string Path { get; }

    /// <summary>Every table in GDB_SystemCatalog, system tables included.</summary>
    public IReadOnlyList<GdbCatalogEntry> Tables { get; }

    /// <summary>Feature classes and standalone tables, in GDB_Items order, then tables without a definition.</summary>
    public IReadOnlyList<GdbDatasetInfo> Datasets { get; }

    public IReadOnlyList<FileGdbFeatureDataset> FeatureDatasets { get; }

    public IReadOnlyList<FileGdbRelationshipClass> RelationshipClasses { get; }

    public IReadOnlyCollection<FileGdbDomain> Domains => _domains.Values;

    /// <summary>Item definitions that could not be read and were skipped.</summary>
    public IReadOnlyList<string> Warnings { get; }

    public GdbDatasetInfo? GetDataset(string name) => _datasetsByName.TryGetValue(name, out var dataset) ? dataset : null;

    public FileGdbDomain? GetDomain(string? name) => name is not null && _domains.TryGetValue(name, out var domain) ? domain : null;

    public static GdbCatalog Read(string gdbPath)
    {
        var tables = GdbSystemCatalog.Read(gdbPath);

        var itemsEntry = tables.FirstOrDefault(t => string.Equals(t.Name, ItemsTableName, StringComparison.OrdinalIgnoreCase));

        if (itemsEntry is null || itemsEntry.Storage != GdbTableStorage.Table)
            throw new InvalidDataException($"{gdbPath}: {ItemsTableName} is missing; the geodatabase definitions cannot be read.");

        var items = ReadItems(itemsEntry);
        var warnings = new List<string>();

        var domains = new Dictionary<string, FileGdbDomain>(StringComparer.OrdinalIgnoreCase);
        var featureDatasets = new List<FileGdbFeatureDataset>();
        var relationshipClasses = new List<FileGdbRelationshipClass>();
        var datasetItems = new List<(GdbItem Item, XElement Definition)>();

        // names (catalog or physical) of tables that are deliberately not listed
        var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // every physical table some item accounts for, listed or not
        var claimed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            claimed.Add(item.Name);

            if (item.PhysicalName is not null)
                claimed.Add(item.PhysicalName);

            if (item.Definition is null)
                continue;

            XElement definition;
            try
            {
                definition = GdbXml.Parse(item.Definition);
            }
            catch (XmlException ex)
            {
                warnings.Add($"Item '{item.Name}': invalid definition XML ({ex.Message}).");
                continue;
            }

            if (item.Type == GdbItemTypes.FeatureClass || item.Type == GdbItemTypes.Table)
            {
                datasetItems.Add((item, definition));
            }
            else if (item.Type == GdbItemTypes.CodedValueDomain || item.Type == GdbItemTypes.RangeDomain)
            {
                var domain = ParseDomain(definition, item.Name);
                domains[domain.Name] = domain;
            }
            else if (item.Type == GdbItemTypes.FeatureDataset)
            {
                featureDatasets.Add(new FileGdbFeatureDataset
                {
                    Name = item.Name,
                    CatalogPath = item.Path,
                    SpatialReference = ParseSpatialReference(definition.Child("SpatialReference")),
                });
            }
            else if (item.Type == GdbItemTypes.RelationshipClass)
            {
                var relationship = ParseRelationshipClass(definition, item, warnings);

                if (relationship is null)
                    continue;

                if (definition.Bool("IsAttachmentRelationship"))
                {
                    excluded.Add(relationship.DestinationClassName);
                    continue;
                }

                relationshipClasses.Add(relationship);
            }
            else if (item.Type == GdbItemTypes.Topology)
            {
                if (definition.Int("TopologyID") is int topologyId)
                {
                    foreach (var suffix in TopologyTableSuffixes)
                        excluded.Add($"T_{topologyId}_{suffix}");
                }
            }
            else if (item.Type == GdbItemTypes.RasterDataset)
            {
                excluded.Add(item.Name);

                foreach (var prefix in RasterHelperPrefixes)
                    excluded.Add(prefix + item.Name);
            }
        }

        var tablesByName = new Dictionary<string, GdbCatalogEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var table in tables)
            tablesByName[table.Name] = table;

        var datasets = new List<GdbDatasetInfo>();

        foreach (var (item, definition) in datasetItems)
        {
            if (excluded.Contains(item.Name) || (item.PhysicalName is not null && excluded.Contains(item.PhysicalName)))
                continue;

            var dataset = ParseDataset(definition, item, tablesByName);

            // The definition's RelationshipClassNames element is left empty by ArcGIS (participation
            // is kept in GDB_ItemRelationships), so derive it from the relationship classes.
            dataset.RelationshipClassNames = relationshipClasses
                .Where(r => string.Equals(r.OriginClassName, dataset.Name, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(r.DestinationClassName, dataset.Name, StringComparison.OrdinalIgnoreCase))
                .Select(r => r.Name)
                .ToList();

            datasets.Add(dataset);
        }

        datasets.AddRange(GetTablesWithoutDefinition(tables, claimed, excluded));

        return new GdbCatalog(gdbPath, tables, datasets, featureDatasets, relationshipClasses, domains, warnings);
    }

    #region Items

    private sealed class GdbItem
    {
        public Guid Type { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? PhysicalName { get; set; }

        public string? Path { get; set; }

        public string? Definition { get; set; }
    }

    private static List<GdbItem> ReadItems(GdbCatalogEntry itemsEntry)
    {
        using var table = itemsEntry.OpenTable();

        int type = RequireField(table, "Type");
        int name = RequireField(table, "Name");
        int physicalName = RequireField(table, "PhysicalName");
        int path = RequireField(table, "Path");
        int definition = RequireField(table, "Definition");

        var result = new List<GdbItem>();

        foreach (var row in table.ReadRows())
        {
            if (row[type] is not Guid typeId || row[name] is not string itemName || itemName.Length == 0)
                continue;

            result.Add(new GdbItem
            {
                Type = typeId,
                Name = itemName,
                PhysicalName = row[physicalName] as string,
                Path = row[path] as string,
                Definition = row[definition] as string,
            });
        }

        return result;
    }

    private static int RequireField(Table.GdbTableReader table, string fieldName)
    {
        int index = table.GetFieldIndex(fieldName);

        if (index < 0)
            throw new InvalidDataException($"{table.Path}: {ItemsTableName} has no '{fieldName}' field.");

        return index;
    }

    /// <summary>
    /// Physical tables that no item accounts for (GDAL reports such tables too), minus system and
    /// excluded tables.
    /// </summary>
    private static IEnumerable<GdbDatasetInfo> GetTablesWithoutDefinition(IReadOnlyList<GdbCatalogEntry> tables, HashSet<string> claimed, HashSet<string> excluded)
    {
        foreach (var table in tables)
        {
            if (table.IsSystemTable || claimed.Contains(table.Name) || excluded.Contains(table.Name) ||
                table.Storage == GdbTableStorage.Missing ||
                RasterHelperPrefixes.Any(p => table.Name.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                continue;

            yield return new GdbDatasetInfo
            {
                Name = table.Name,
                Kind = HasGeometry(table) ? GdbDatasetKind.FeatureClass : GdbDatasetKind.Table,
                HasDefinition = false,
                Table = table,
            };
        }
    }

    private static bool HasGeometry(GdbCatalogEntry table)
    {
        if (table.Storage != GdbTableStorage.Table)
            return false;

        try
        {
            using var reader = table.OpenTable();
            return reader.GeometryFieldIndex >= 0;
        }
        catch (Exception ex) when (ex is InvalidDataException or NotSupportedException or IOException)
        {
            return false;
        }
    }

    #endregion

    #region Definitions

    private static GdbDatasetInfo ParseDataset(XElement definition, GdbItem item, Dictionary<string, GdbCatalogEntry> tablesByName)
    {
        var table = (item.PhysicalName is not null && tablesByName.TryGetValue(item.PhysicalName, out var byPhysical) ? byPhysical : null)
                    ?? (tablesByName.TryGetValue(item.Name, out var byName) ? byName : null);

        return ParseDataset(definition, item.Name, item.Path, item.Type == GdbItemTypes.FeatureClass, table);
    }

    internal static GdbDatasetInfo ParseDataset(XElement definition, string name, string? path, bool isFeatureClass, GdbCatalogEntry? table)
    {
        return new GdbDatasetInfo
        {
            Name = name,
            Kind = isFeatureClass ? GdbDatasetKind.FeatureClass : GdbDatasetKind.Table,
            CatalogPath = path,
            FeatureDatasetName = GetFeatureDatasetName(path),
            AliasName = definition.Text("AliasName"),
            HasDefinition = true,
            Table = table,
            ShapeType = isFeatureClass ? definition.Text("ShapeType") : null,
            ShapeFieldName = isFeatureClass ? definition.Text("ShapeFieldName") : null,
            ObjectIdFieldName = definition.Text("OIDFieldName"),
            SpatialReference = isFeatureClass ? ParseSpatialReference(definition.Child("SpatialReference")) : null,
            Fields = definition.Child("GPFieldInfoExs").Children("GPFieldInfoEx").Select(ParseField).ToList(),
            SubtypeFieldName = definition.Text("SubtypeFieldName"),
            DefaultSubtypeCode = definition.Int("DefaultSubtypeCode"),
            Subtypes = definition.Child("Subtypes").Children("Subtype").Select(ParseSubtype).ToList(),
        };
    }

    /// <summary><c>\FD_Default\Roads</c> → <c>FD_Default</c>; <c>\Roads</c> → <c>null</c>.</summary>
    internal static string? GetFeatureDatasetName(string? catalogPath)
    {
        var parts = catalogPath?.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);

        return parts is { Length: > 1 } ? parts[parts.Length - 2] : null;
    }

    private static GdbItemFieldInfo ParseField(XElement field) => new()
    {
        Name = field.Text("Name") ?? string.Empty,
        AliasName = field.Text("AliasName"),
        DomainName = field.Text("DomainName"),
        FieldType = field.Text("FieldType"),
        IsNullable = field.Bool("IsNullable"),
    };

    private static FileGdbSubtype ParseSubtype(XElement subtype) => new()
    {
        Code = subtype.Int("SubtypeCode") ?? 0,
        Name = subtype.Text("SubtypeName") ?? string.Empty,
        Fields = subtype.Child("FieldInfos").Children("SubtypeFieldInfo").Select(f => new FileGdbSubtypeField
        {
            FieldName = f.Text("FieldName") ?? string.Empty,
            DomainName = f.Text("DomainName"),
            DefaultValue = f.Child("DefaultValue").TypedValue(),
        }).ToList(),
    };

    internal static FileGdbSpatialReference? ParseSpatialReference(XElement? spatialReference)
    {
        if (spatialReference is null)
            return null;

        var wkt = spatialReference.Text("WKT");
        var wkid = spatialReference.Int("WKID");
        var latestWkid = spatialReference.Int("LatestWKID");

        // a stored WKID of 0 means "none" (custom systems), so the WKT decides
        int srid = AsWkid(latestWkid) ?? AsWkid(wkid) ?? GetSridFromWkt(wkt);

        return new FileGdbSpatialReference(wkt, wkid, latestWkid, srid);
    }

    private static int? AsWkid(int? value) => value > 0 ? value : null;

    internal static int GetSridFromWkt(string? wkt)
    {
        if (string.IsNullOrWhiteSpace(wkt))
            return 0;

        try
        {
            return EsriPrjFile.Parse(wkt!).Srid;
        }
        catch (Exception)
        {
            // an unparsable WKT only means the SRID is unknown
            return 0;
        }
    }

    internal static FileGdbDomain ParseDomain(XElement definition, string itemName)
    {
        var name = definition.Text("DomainName") ?? itemName;
        var description = definition.Text("Description");
        var fieldType = definition.Text("FieldType");

        if (definition.Name.LocalName.IndexOf("RangeDomain", StringComparison.Ordinal) >= 0)
        {
            return new FileGdbRangeDomain(name, description, fieldType,
                definition.Child("MinValue").TypedValue(),
                definition.Child("MaxValue").TypedValue());
        }

        var codedValues = definition.Child("CodedValues").Children("CodedValue")
            .Select(c => (Code: c.Child("Code").TypedValue(), Name: c.Text("Name") ?? string.Empty))
            .Where(c => c.Code is not null)
            .Select(c => new FileGdbCodedValue(c.Code!, c.Name))
            .ToList();

        return new FileGdbCodedValueDomain(name, description, fieldType, codedValues);
    }

    internal static FileGdbRelationshipClass? ParseRelationshipClass(XElement definition, string name, string? path, List<string> warnings)
    {
        GdbRelationshipCardinality cardinality;

        switch (definition.Text("Cardinality"))
        {
            case "esriRelCardinalityOneToOne": cardinality = GdbRelationshipCardinality.OneToOne; break;
            case "esriRelCardinalityOneToMany": cardinality = GdbRelationshipCardinality.OneToMany; break;
            case "esriRelCardinalityManyToMany": cardinality = GdbRelationshipCardinality.ManyToMany; break;
            default:
                warnings.Add($"Relationship class '{name}': unknown cardinality '{definition.Text("Cardinality")}'.");
                return null;
        }

        var origin = definition.Child("OriginClassNames").Children("Name").FirstOrDefault()?.Value;
        var destination = definition.Child("DestinationClassNames").Children("Name").FirstOrDefault()?.Value;

        if (string.IsNullOrEmpty(origin) || string.IsNullOrEmpty(destination))
        {
            warnings.Add($"Relationship class '{name}': origin or destination class is missing.");
            return null;
        }

        var keys = (definition.Child("OriginClassKeys")?.Elements() ?? Enumerable.Empty<XElement>())
            .Concat(definition.Child("DestinationClassKeys")?.Elements() ?? Enumerable.Empty<XElement>())
            .Select(k => (Role: k.Text("KeyRole"), Field: k.Text("ObjectKeyName")))
            .ToList();

        string? Key(string role) => keys.FirstOrDefault(k => k.Role == role).Field;

        return new FileGdbRelationshipClass
        {
            Name = name,
            FeatureDatasetName = GetFeatureDatasetName(path),
            Cardinality = cardinality,
            OriginClassName = origin!,
            DestinationClassName = destination!,
            OriginPrimaryKey = Key("esriRelKeyRoleOriginPrimary"),
            OriginForeignKey = Key("esriRelKeyRoleOriginForeign"),
            DestinationPrimaryKey = Key("esriRelKeyRoleDestinationPrimary"),
            DestinationForeignKey = Key("esriRelKeyRoleDestinationForeign"),
            IsComposite = definition.Bool("IsComposite"),
            IsAttributed = definition.Bool("IsAttributed"),
            ForwardPathLabel = definition.Text("ForwardPathLabel"),
            BackwardPathLabel = definition.Text("BackwardPathLabel"),
            Notification = definition.Text("Notification"),
        };
    }

    private static FileGdbRelationshipClass? ParseRelationshipClass(XElement definition, GdbItem item, List<string> warnings)
        => ParseRelationshipClass(definition, item.Name, item.Path, warnings);

    #endregion
}
