namespace IRI.Maptor.Core.FileGdb.Enums;

/// <summary>
/// Where the cells of a raster field live.
/// </summary>
public enum GdbRasterStorage : byte
{
    /// <summary>The row stores a path to an external raster file.</summary>
    External = 0,

    /// <summary>The row stores an int32 id pointing to auxiliary raster tables.</summary>
    Managed = 1,

    /// <summary>The row stores the raster definition inline as a blob.</summary>
    Inline = 2,
}
