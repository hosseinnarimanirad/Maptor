namespace IRI.Maptor.Sta.Spatial.IO.CesiumTerrain;

/// <summary>
/// Terrain file format types
/// </summary>
public enum TerrainFormat
{
    /// <summary>
    /// Unknown or undetected format
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Cesium Quantized-Mesh format (triangle mesh with adaptive detail)
    /// File has variable size, contains header, vertices, triangles, edges
    /// </summary>
    QuantizedMesh = 1,

    /// <summary>
    /// Heightmap format (regular grid of elevation samples)
    /// File has fixed size based on grid dimensions (e.g., 65×65, 257×257)
    /// </summary>
    Heightmap = 2
}

