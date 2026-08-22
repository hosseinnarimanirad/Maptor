namespace IRI.Maptor.Core.Spatial.IO.PmTiles;

/// <summary>
/// Describes the payload type stored inside the PMTiles archive.
/// </summary>
public enum PmTilesTileType : byte
{
    Unknown = 0x00,
    VectorMvt = 0x01,
    RasterPng = 0x02,
    RasterJpeg = 0x03,
    RasterWebp = 0x04,
    RasterAvif = 0x05,
} 