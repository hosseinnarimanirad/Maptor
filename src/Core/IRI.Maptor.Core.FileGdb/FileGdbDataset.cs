using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.FileGdb.Catalog;
using IRI.Maptor.Core.FileGdb.Catalog.Model;
using IRI.Maptor.Core.FileGdb.Enums;
using IRI.Maptor.Core.FileGdb.Shapes;
using IRI.Maptor.Core.FileGdb.Table;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Core.FileGdb;

/// <summary>
/// A feature class or standalone table of a <see cref="FileGeodatabase"/>.
/// </summary>
/// <remarks>
/// The catalog properties (name, alias, subtypes, ...) are always available. The physical schema
/// (<see cref="Fields"/>, <see cref="RowCount"/>, <see cref="GeometryType"/>, ...) is read from the
/// table on first use and needs <see cref="Storage"/> to be <see cref="GdbTableStorage.Table"/>;
/// otherwise those properties throw, as <see cref="ReadFeatures"/> does.
/// </remarks>
public sealed class FileGdbDataset
{
    private readonly GdbDatasetInfo _info;

    private readonly Lazy<Schema> _schema;

    internal FileGdbDataset(FileGeodatabase geodatabase, GdbDatasetInfo info)
    {
        Geodatabase = geodatabase;
        _info = info;
        _schema = new Lazy<Schema>(LoadSchema);

        RelationshipClasses = geodatabase.RelationshipClasses
            .Where(r => info.RelationshipClassNames.Contains(r.Name, StringComparer.OrdinalIgnoreCase))
            .ToList();
    }

    public FileGeodatabase Geodatabase { get; }

    public string Name => _info.Name;

    public string? AliasName => _info.AliasName;

    public GdbDatasetKind Kind => _info.Kind;

    /// <summary>Path inside the geodatabase, e.g. <c>\FD_Default\Roads</c>; <c>null</c> without a definition.</summary>
    public string? CatalogPath => _info.CatalogPath;

    /// <summary>The feature dataset holding this dataset, or <c>null</c> at the geodatabase root.</summary>
    public string? FeatureDatasetName => _info.FeatureDatasetName;

    /// <summary>
    /// False for a table with no item definition in GDB_Items: it has no aliases from the
    /// definition, no domains and no subtypes.
    /// </summary>
    public bool HasDefinition => _info.HasDefinition;

    /// <summary>Whether the table can be read; compressed and missing tables are listed but not readable.</summary>
    public GdbTableStorage Storage => _info.Table?.Storage ?? GdbTableStorage.Missing;

    public string? SubtypeFieldName => _info.SubtypeFieldName;

    public int? DefaultSubtypeCode => _info.DefaultSubtypeCode;

    public IReadOnlyList<FileGdbSubtype> Subtypes => _info.Subtypes;

    /// <summary>Relationship classes this dataset is the origin or destination of.</summary>
    public IReadOnlyList<FileGdbRelationshipClass> RelationshipClasses { get; }

    #region Physical schema

    /// <summary>Fields in storage order; the index of a field is its index in <see cref="FileGdbFeature.Values"/>.</summary>
    public IReadOnlyList<FileGdbField> Fields => _schema.Value.Fields;

    /// <summary>Number of live (non-deleted) rows.</summary>
    public long RowCount => _schema.Value.RowCount;

    /// <summary><see cref="GdbTableGeometryType.None"/> for a standalone table.</summary>
    public GdbTableGeometryType GeometryType => _schema.Value.GeometryType;

    /// <summary>The stored geometries have Z values. They are not read: geometries are 2D.</summary>
    public bool HasZ => _schema.Value.HasZ;

    /// <summary>The stored geometries have M values. They are not read: geometries are 2D.</summary>
    public bool HasM => _schema.Value.HasM;

    public string? ObjectIdFieldName => _schema.Value.ObjectIdIndex < 0 ? null : Fields[_schema.Value.ObjectIdIndex].Name;

    public string? GeometryFieldName => _schema.Value.GeometryIndex < 0 ? null : Fields[_schema.Value.GeometryIndex].Name;

    /// <summary>
    /// The definition's spatial reference, or for a table without a definition the one stored in
    /// its geometry field; <c>null</c> for a standalone table.
    /// </summary>
    public FileGdbSpatialReference? SpatialReference => Kind == GdbDatasetKind.Table ? null : _info.SpatialReference ?? _schema.Value.SpatialReference;

    /// <summary>EPSG code of <see cref="SpatialReference"/>, or 0 when unknown.</summary>
    public int Srid => SpatialReference?.Srid ?? 0;

    /// <summary>Layer extent as stored in the table; <c>null</c> for an empty layer or a standalone table.</summary>
    public BoundingBox? Extent => _schema.Value.Extent;

    /// <summary>
    /// False when OBJECTIDs may be shifted: an ArcGIS Pro 3.2+ sparse table layout this library
    /// does not fully understand. Rows and values are still read correctly.
    /// </summary>
    public bool IsObjectIdReliable => _schema.Value.IsObjectIdReliable;

    #endregion

    /// <summary>A field by name (case-insensitive), or <c>null</c>.</summary>
    public FileGdbField? GetField(string fieldName) => _schema.Value.FieldsByName.TryGetValue(fieldName, out var field) ? field : null;

    public FileGdbSubtype? GetSubtype(int code) => _info.GetSubtype(code);

    /// <summary>
    /// The domain of a field for a row of the given subtype: the subtype's own domain when it sets
    /// one, else the field's domain. Pass <c>null</c> when the row has no subtype.
    /// </summary>
    public FileGdbDomain? GetDomain(string fieldName, int? subtypeCode = null)
        => Geodatabase.GetDomain(_info.GetDomainName(fieldName, subtypeCode));

    /// <summary>
    /// Streams the rows in OBJECTID order, one at a time. The table is opened when enumeration
    /// starts and closed when it ends.
    /// </summary>
    /// <exception cref="NotSupportedException">The table is compressed, or holds multipatch geometries (thrown here, before enumeration).</exception>
    /// <exception cref="FileNotFoundException">The table's file is missing (thrown here, before enumeration).</exception>
    /// <exception cref="InvalidDataException">A row or geometry cannot be decoded and <see cref="FileGdbReadOptions.OnRowError"/> is not set.</exception>
    public IEnumerable<FileGdbFeature> ReadFeatures(FileGdbReadOptions? options = null)
    {
        EnsureReadable();

        if (GeometryType == GdbTableGeometryType.Multipatch)
            throw new NotSupportedException($"{Name}: multipatch feature classes are not supported.");

        return ReadFeaturesCore(options ?? new FileGdbReadOptions());
    }

    private IEnumerable<FileGdbFeature> ReadFeaturesCore(FileGdbReadOptions options)
    {
        var schema = _schema.Value;

        using var table = _info.Table!.OpenTable();

        if (table.Fields.Count != schema.Fields.Count)
            throw new InvalidDataException($"{Name}: the table's fields changed after the geodatabase was opened.");

        var geometryInfo = schema.GeometryIndex < 0 ? null : table.Fields[schema.GeometryIndex].Geometry!;
        int srid = Srid;

        foreach (var location in table.EnumerateRowLocations())
        {
            GdbRow? row;

            try
            {
                row = table.ReadRow(location);
            }
            catch (InvalidDataException ex) when (options.OnRowError is not null)
            {
                options.OnRowError(new FileGdbRowError(Name, location.ObjectId, ex.Message, ex));
                continue;
            }

            if (row is null)
                continue;

            var values = row.Values;
            GdbDecodedGeometry? decoded = null;

            if (geometryInfo is not null && values[schema.GeometryIndex] is byte[] blob)
            {
                try
                {
                    decoded = GdbGeometryDecoder.Decode(blob, geometryInfo, srid);
                }
                catch (Exception ex) when (ex is InvalidDataException or NotSupportedException)
                {
                    var message = $"{Name}: cannot read the geometry of OBJECTID {row.ObjectId}: {ex.Message}";

                    if (options.OnRowError is null)
                        throw new InvalidDataException(message, ex);

                    options.OnRowError(new FileGdbRowError(Name, row.ObjectId, message, ex));
                    continue;
                }

                // the feature exposes the decoded geometry, not the stored bytes
                values[schema.GeometryIndex] = decoded?.Geometry;
            }

            if (options.BoundingBox is BoundingBox box && !Intersects(decoded?.Geometry, box))
                continue;

            yield return new FileGdbFeature(this, row.ObjectId, values, decoded?.Geometry, decoded?.HasCurves ?? false);
        }
    }

    private static bool Intersects(Geometry<Point>? geometry, BoundingBox box)
        => geometry is not null && !geometry.IsEmpty() && box.Intersects(geometry.GetBoundingBox());

    private void EnsureReadable()
    {
        switch (Storage)
        {
            case GdbTableStorage.Compressed:
                throw new NotSupportedException($"{Name}: compressed tables (CDF/SDC) are not supported. Uncompress the geodatabase in ArcGIS first.");

            case GdbTableStorage.Missing:
                throw new FileNotFoundException($"{Name}: the table's file is missing from the geodatabase.", _info.Table?.TablePath);
        }
    }

    #region Schema

    private sealed class Schema
    {
        public IReadOnlyList<FileGdbField> Fields { get; set; } = Array.Empty<FileGdbField>();

        public Dictionary<string, FileGdbField> FieldsByName { get; set; } = new();

        public long RowCount { get; set; }

        public GdbTableGeometryType GeometryType { get; set; }

        public bool HasZ { get; set; }

        public bool HasM { get; set; }

        public int ObjectIdIndex { get; set; }

        public int GeometryIndex { get; set; }

        public FileGdbSpatialReference? SpatialReference { get; set; }

        public BoundingBox? Extent { get; set; }

        public bool IsObjectIdReliable { get; set; }
    }

    private Schema LoadSchema()
    {
        EnsureReadable();

        using var table = _info.Table!.OpenTable();

        var fields = new List<FileGdbField>(table.Fields.Count);

        for (int i = 0; i < table.Fields.Count; i++)
        {
            var physical = table.Fields[i];
            var definition = _info.GetField(physical.Name);

            fields.Add(new FileGdbField(physical, definition, Geodatabase.GetDomain(definition?.DomainName), i));
        }

        var schema = new Schema
        {
            Fields = fields,
            FieldsByName = fields.GroupBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                                 .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase),
            RowCount = table.ValidRowCount,
            GeometryType = table.GeometryType,
            HasZ = table.HasZ,
            HasM = table.HasM,
            ObjectIdIndex = table.ObjectIdFieldIndex,
            GeometryIndex = table.GeometryFieldIndex,
            IsObjectIdReliable = table.IsObjectIdReliable,
        };

        if (table.GeometryFieldIndex >= 0)
        {
            var geometry = table.Fields[table.GeometryFieldIndex].Geometry!;

            schema.SpatialReference = new FileGdbSpatialReference(geometry.Wkt, null, null, GdbCatalog.GetSridFromWkt(geometry.Wkt));

            if (!double.IsNaN(geometry.XMin) && !double.IsNaN(geometry.YMin) && !double.IsNaN(geometry.XMax) && !double.IsNaN(geometry.YMax))
                schema.Extent = new BoundingBox(geometry.XMin, geometry.YMin, geometry.XMax, geometry.YMax);
        }

        return schema;
    }

    #endregion

    public override string ToString() => $"{Name} ({Kind})";
}
