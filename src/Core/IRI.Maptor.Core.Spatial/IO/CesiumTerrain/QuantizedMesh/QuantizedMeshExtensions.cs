namespace IRI.Maptor.Core.Spatial.IO.CesiumTerrain;

/// <summary>
/// Optional extensions that may be present in a quantized-mesh terrain tile
/// </summary>
public class QuantizedMeshExtensions
{
    /// <summary>
    /// Water mask indicating which parts of the terrain are water (0 = land, 255 = water)
    /// </summary>
    public byte[] WaterMask { get; set; }

    /// <summary>
    /// Vertex normals for lighting calculations (oct-encoded)
    /// </summary>
    public byte[] VertexNormals { get; set; }

    /// <summary>
    /// Metadata as JSON string
    /// </summary>
    public string Metadata { get; set; }

    /// <summary>
    /// Indicates if water mask extension is present
    /// </summary>
    public bool HasWaterMask => WaterMask != null && WaterMask.Length > 0;

    /// <summary>
    /// Indicates if vertex normals extension is present
    /// </summary>
    public bool HasVertexNormals => VertexNormals != null && VertexNormals.Length > 0;

    /// <summary>
    /// Indicates if metadata extension is present
    /// </summary>
    public bool HasMetadata => !string.IsNullOrEmpty(Metadata);
}

/// <summary>
/// Extension identifiers used in quantized-mesh terrain files
/// </summary>
public enum QuantizedMeshExtensionId : byte
{
    /// <summary>
    /// Oct-Encoded Per-Vertex Normals (1 byte identifier)
    /// </summary>
    VertexNormals = 1,

    /// <summary>
    /// Water Mask (2 bytes identifier)
    /// </summary>
    WaterMask = 2,

    /// <summary>
    /// Metadata (4 bytes identifier)
    /// </summary>
    Metadata = 4
}

