using IRI.Maptor.Core.FileGdb.Enums;
using IRI.Maptor.Core.FileGdb.Table;

namespace IRI.Maptor.Core.FileGdb.Catalog;

/// <summary>
/// Reads GDB_SystemCatalog (always a00000001.gdbtable): the list of every table in the
/// geodatabase. The OBJECTID of a catalog row is the table number, so table N is stored in
/// the files <c>a{N:x8}.*</c>.
/// </summary>
internal static class GdbSystemCatalog
{
    public const string CatalogFileName = "a00000001.gdbtable";

    private const string NameField = "Name";

    private const string FileFormatField = "FileFormat";

    public static IReadOnlyList<GdbCatalogEntry> Read(string gdbPath)
    {
        if (!Directory.Exists(gdbPath))
            throw new DirectoryNotFoundException($"File geodatabase folder not found: {gdbPath}");

        var catalogPath = Path.Combine(gdbPath, CatalogFileName);

        if (!File.Exists(catalogPath))
            throw new InvalidDataException($"{gdbPath} is not a file geodatabase: {CatalogFileName} is missing.");

        using var table = GdbTableReader.Open(catalogPath, "GDB_SystemCatalog");

        int nameIndex = table.GetFieldIndex(NameField);
        int formatIndex = table.GetFieldIndex(FileFormatField);

        if (nameIndex < 0 || table.Fields[nameIndex].FieldType != GdbFieldType.String ||
            formatIndex < 0 || table.Fields[formatIndex].FieldType is not (GdbFieldType.Int16 or GdbFieldType.Int32))
            throw new InvalidDataException($"{catalogPath}: unexpected GDB_SystemCatalog schema.");

        var result = new List<GdbCatalogEntry>();

        foreach (var row in table.ReadRows())
        {
            if (row[nameIndex] is not string name || name.Length == 0)
                continue;

            var baseName = GetBaseFileName(row.ObjectId);
            var tablePath = Path.Combine(gdbPath, baseName + ".gdbtable");

            result.Add(new GdbCatalogEntry(
                name,
                row.ObjectId,
                Convert.ToInt32(row[formatIndex] ?? 0),
                tablePath,
                GetStorage(tablePath)));
        }

        return result;
    }

    public static string GetBaseFileName(long tableNumber) => $"a{tableNumber:x8}";

    private static GdbTableStorage GetStorage(string tablePath)
    {
        if (File.Exists(tablePath + ".cdf") || File.Exists(tablePath + ".sdc"))
            return GdbTableStorage.Compressed;

        return File.Exists(tablePath) ? GdbTableStorage.Table : GdbTableStorage.Missing;
    }
}

/// <summary>
/// One table listed in GDB_SystemCatalog.
/// </summary>
internal sealed class GdbCatalogEntry
{
    public GdbCatalogEntry(string name, long tableNumber, int fileFormat, string tablePath, GdbTableStorage storage)
    {
        Name = name;
        TableNumber = tableNumber;
        FileFormat = fileFormat;
        TablePath = tablePath;
        Storage = storage;
    }

    public string Name { get; }

    /// <summary>OBJECTID in the catalog; also the number in the table's file name.</summary>
    public long TableNumber { get; }

    /// <summary>Raw FileFormat column; its meaning is not documented.</summary>
    public int FileFormat { get; }

    /// <summary>Full path of the table's .gdbtable file (which may not exist, see <see cref="Storage"/>).</summary>
    public string TablePath { get; }

    public GdbTableStorage Storage { get; }

    /// <summary>
    /// Geodatabase bookkeeping tables (GDB_Items, GDB_SpatialRefs, ...), recognised by name prefix.
    /// A name-based guess only: the authoritative list of user datasets comes from GDB_Items.
    /// </summary>
    public bool IsSystemTable => Name.StartsWith("GDB_", StringComparison.OrdinalIgnoreCase);

    public GdbTableReader OpenTable() => GdbTableReader.Open(TablePath, Name);

    public override string ToString() => $"{Name} ({Path.GetFileName(TablePath)}, {Storage})";
}
