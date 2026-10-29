using IRI.Maptor.Core.Common.Primitives;

namespace IRI.Maptor.Core.FileGdb;

/// <summary>
/// Options for <see cref="FileGdbDataset.ReadFeatures"/>.
/// </summary>
public sealed class FileGdbReadOptions
{
    /// <summary>
    /// When set, only features whose envelope intersects this box are returned, in the dataset's
    /// coordinates. Rows without a geometry are skipped. Every row is still read: there is no
    /// spatial index lookup.
    /// </summary>
    public BoundingBox? BoundingBox { get; set; }

    /// <summary>
    /// When set, a row or geometry that cannot be decoded is reported here and skipped. When not
    /// set, it throws <see cref="InvalidDataException"/> and ends the enumeration.
    /// </summary>
    public Action<FileGdbRowError>? OnRowError { get; set; }
}

/// <summary>
/// A row that <see cref="FileGdbDataset.ReadFeatures"/> skipped because it could not be decoded.
/// </summary>
public sealed class FileGdbRowError
{
    internal FileGdbRowError(string datasetName, long objectId, string message, Exception exception)
    {
        DatasetName = datasetName;
        ObjectId = objectId;
        Message = message;
        Exception = exception;
    }

    public string DatasetName { get; }

    public long ObjectId { get; }

    public string Message { get; }

    public Exception Exception { get; }

    public override string ToString() => Message;
}
