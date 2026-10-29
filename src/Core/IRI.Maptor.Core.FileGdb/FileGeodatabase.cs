using IRI.Maptor.Core.FileGdb.Catalog;
using IRI.Maptor.Core.FileGdb.Catalog.Model;

namespace IRI.Maptor.Core.FileGdb;

/// <summary>
/// A read-only ESRI File Geodatabase (a <c>.gdb</c> folder): its feature classes and tables,
/// feature datasets, domains and relationship classes.
/// </summary>
/// <remarks>
/// <see cref="Open"/> reads the catalog once and keeps no files open; each
/// <see cref="FileGdbDataset.ReadFeatures"/> call opens its table for the duration of the
/// enumeration. Topology bookkeeping tables, attachment tables and raster datasets are not listed.
/// </remarks>
public sealed class FileGeodatabase
{
    private readonly GdbCatalog _catalog;

    private readonly Dictionary<string, FileGdbDataset> _datasetsByName;

    private FileGeodatabase(GdbCatalog catalog)
    {
        _catalog = catalog;

        Datasets = catalog.Datasets.Select(d => new FileGdbDataset(this, d)).ToList();
        Domains = catalog.Domains.ToList();

        _datasetsByName = new Dictionary<string, FileGdbDataset>(StringComparer.OrdinalIgnoreCase);
        foreach (var dataset in Datasets)
            _datasetsByName[dataset.Name] = dataset;
    }

    /// <summary>Full path of the <c>.gdb</c> folder.</summary>
    public string Path => _catalog.Path;

    /// <summary>Feature classes and standalone tables, in catalog order.</summary>
    public IReadOnlyList<FileGdbDataset> Datasets { get; }

    public IReadOnlyList<FileGdbFeatureDataset> FeatureDatasets => _catalog.FeatureDatasets;

    public IReadOnlyList<FileGdbDomain> Domains { get; }

    public IReadOnlyList<FileGdbRelationshipClass> RelationshipClasses => _catalog.RelationshipClasses;

    /// <summary>Item definitions that could not be read and were skipped.</summary>
    public IReadOnlyList<string> Warnings => _catalog.Warnings;

    /// <summary>
    /// Opens the geodatabase in <paramref name="gdbPath"/>.
    /// </summary>
    /// <exception cref="DirectoryNotFoundException">The folder does not exist.</exception>
    /// <exception cref="InvalidDataException">The folder is not a readable file geodatabase.</exception>
    /// <exception cref="NotSupportedException">The geodatabase's system tables are compressed or from ArcGIS 9.x.</exception>
    public static FileGeodatabase Open(string gdbPath)
    {
        if (string.IsNullOrWhiteSpace(gdbPath))
            throw new ArgumentException("A file geodatabase folder is required.", nameof(gdbPath));

        return new FileGeodatabase(GdbCatalog.Read(System.IO.Path.GetFullPath(gdbPath)));
    }

    /// <summary>The dataset with the given name (case-insensitive), or <c>null</c>.</summary>
    public FileGdbDataset? GetDataset(string name) => _datasetsByName.TryGetValue(name, out var dataset) ? dataset : null;

    /// <summary>The domain with the given name (case-insensitive), or <c>null</c>.</summary>
    public FileGdbDomain? GetDomain(string? name) => _catalog.GetDomain(name);

    public override string ToString() => Path;
}
