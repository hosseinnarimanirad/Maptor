namespace IRI.Maptor.Core.Spatial.IO.CesiumTerrain;

/// <summary>
/// Represents the header of a Cesium Quantized-Mesh terrain tile (.terrain file)
/// Specification: https://github.com/CesiumGS/quantized-mesh
/// </summary>
public class QuantizedMeshHeader
{
    /// <summary>
    /// The center of the tile in Earth-centered, Earth-fixed coordinates (X component)
    /// </summary>
    public double CenterX { get; set; }

    /// <summary>
    /// The center of the tile in Earth-centered, Earth-fixed coordinates (Y component)
    /// </summary>
    public double CenterY { get; set; }

    /// <summary>
    /// The center of the tile in Earth-centered, Earth-fixed coordinates (Z component)
    /// </summary>
    public double CenterZ { get; set; }

    /// <summary>
    /// The minimum height value in the tile (in meters)
    /// </summary>
    public float MinimumHeight { get; set; }

    /// <summary>
    /// The maximum height value in the tile (in meters)
    /// </summary>
    public float MaximumHeight { get; set; }

    /// <summary>
    /// The bounding sphere center X coordinate (relative to tile center)
    /// </summary>
    public double BoundingSphereCenterX { get; set; }

    /// <summary>
    /// The bounding sphere center Y coordinate (relative to tile center)
    /// </summary>
    public double BoundingSphereCenterY { get; set; }

    /// <summary>
    /// The bounding sphere center Z coordinate (relative to tile center)
    /// </summary>
    public double BoundingSphereCenterZ { get; set; }

    /// <summary>
    /// The radius of the bounding sphere (in meters)
    /// </summary>
    public double BoundingSphereRadius { get; set; }

    /// <summary>
    /// The horizon occlusion point X coordinate (relative to tile center)
    /// </summary>
    public double HorizonOcclusionPointX { get; set; }

    /// <summary>
    /// The horizon occlusion point Y coordinate (relative to tile center)
    /// </summary>
    public double HorizonOcclusionPointY { get; set; }

    /// <summary>
    /// The horizon occlusion point Z coordinate (relative to tile center)
    /// </summary>
    public double HorizonOcclusionPointZ { get; set; }

    /// <summary>
    /// Total size of the header in bytes (always 88 bytes)
    /// </summary>
    public const int HeaderSize = 88;
}

