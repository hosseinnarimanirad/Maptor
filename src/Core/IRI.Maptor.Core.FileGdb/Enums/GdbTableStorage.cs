namespace IRI.Maptor.Core.FileGdb.Enums;

/// <summary>
/// How a table listed in the system catalog is stored on disk.
/// </summary>
public enum GdbTableStorage
{
    /// <summary>A plain .gdbtable file that this library can read.</summary>
    Table,

    /// <summary>Compressed (CDF or SDC); not readable by this library.</summary>
    Compressed,

    /// <summary>Listed in the catalog but no .gdbtable file exists.</summary>
    Missing,
}
