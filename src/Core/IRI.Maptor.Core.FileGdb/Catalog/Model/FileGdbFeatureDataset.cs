namespace IRI.Maptor.Core.FileGdb.Catalog.Model;

/// <summary>
/// A feature dataset: a folder of feature classes sharing one spatial reference.
/// </summary>
public sealed class FileGdbFeatureDataset
{
    public string Name { get; internal set; } = string.Empty;

    public string? CatalogPath { get; internal set; }

    public FileGdbSpatialReference? SpatialReference { get; internal set; }

    public override string ToString() => Name;
}
