using System;

namespace IRI.Maptor.Core.Spatial.IO.PmTiles;

/// <summary>
/// Represents the compression applied to metadata, directories, or tile payloads in a PMTiles archive.
/// The numeric values follow the official PMTiles v3 specification.
/// </summary>
public enum PmTilesCompression : byte
{
    Unknown = 0,
    None = 1,
    Gzip = 2,
    Brotli = 3,
    Zstandard = 4,
} 