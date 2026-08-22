namespace IRI.Maptor.Core.Spatial.IO.CesiumTerrain;

/// <summary>
/// Represents the complete data structure of a Cesium Quantized-Mesh terrain tile
/// </summary>
public class QuantizedMeshData
{
    /// <summary>
    /// The header information containing tile metadata
    /// </summary>
    public QuantizedMeshHeader Header { get; set; }

    /// <summary>
    /// Number of vertices in the mesh
    /// </summary>
    public uint VertexCount { get; set; }

    /// <summary>
    /// Quantized U coordinates (horizontal, 0-32767)
    /// </summary>
    public ushort[] U { get; set; }

    /// <summary>
    /// Quantized V coordinates (vertical, 0-32767)
    /// </summary>
    public ushort[] V { get; set; }

    /// <summary>
    /// Quantized height values (0-32767)
    /// </summary>
    public ushort[] Height { get; set; }

    /// <summary>
    /// Triangle indices (groups of 3 form triangles)
    /// </summary>
    public uint[] Indices { get; set; }

    /// <summary>
    /// Number of triangles in the mesh
    /// </summary>
    public uint TriangleCount => (uint)(Indices?.Length ?? 0) / 3;

    /// <summary>
    /// Edge indices for the western edge
    /// </summary>
    public uint[] WestIndices { get; set; }

    /// <summary>
    /// Edge indices for the southern edge
    /// </summary>
    public uint[] SouthIndices { get; set; }

    /// <summary>
    /// Edge indices for the eastern edge
    /// </summary>
    public uint[] EastIndices { get; set; }

    /// <summary>
    /// Edge indices for the northern edge
    /// </summary>
    public uint[] NorthIndices { get; set; }

    /// <summary>
    /// Optional extensions (water mask, vertex normals, metadata, etc.)
    /// </summary>
    public QuantizedMeshExtensions Extensions { get; set; }

    /// <summary>
    /// Converts quantized U coordinate to normalized value [0, 1]
    /// </summary>
    public double GetNormalizedU(int index) => U[index] / 32767.0;

    /// <summary>
    /// Converts quantized V coordinate to normalized value [0, 1]
    /// </summary>
    public double GetNormalizedV(int index) => V[index] / 32767.0;

    /// <summary>
    /// Converts quantized height to actual height in meters
    /// </summary>
    public double GetHeight(int index)
    {
        double normalizedHeight = Height[index] / 32767.0;
        return Header.MinimumHeight + normalizedHeight * (Header.MaximumHeight - Header.MinimumHeight);
    }

    /// <summary>
    /// Validates the mesh data integrity
    /// </summary>
    public bool IsValid()
    {
        if (Header == null || VertexCount == 0)
            return false;

        if (U == null || V == null || Height == null)
            return false;

        if (U.Length != VertexCount || V.Length != VertexCount || Height.Length != VertexCount)
            return false;

        if (Indices == null || Indices.Length % 3 != 0)
            return false;

        return true;
    }
}

