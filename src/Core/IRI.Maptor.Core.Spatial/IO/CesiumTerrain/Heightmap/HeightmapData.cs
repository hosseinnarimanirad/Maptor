namespace IRI.Maptor.Core.Spatial.IO.CesiumTerrain;

/// <summary>
/// Represents heightmap-1.0 format terrain data (regular grid of elevation samples)
/// </summary>
public class HeightmapData
{
    /// <summary>
    /// Grid size (e.g., 65, 257, etc.)
    /// Common sizes: 65×65, 257×257
    /// </summary>
    public int GridSize { get; set; }

    /// <summary>
    /// Height values in row-major order (GridSize × GridSize)
    /// Values are typically in meters
    /// </summary>
    public float[,] Heights { get; set; }

    /// <summary>
    /// Minimum height value in the grid
    /// </summary>
    public float MinHeight { get; set; }

    /// <summary>
    /// Maximum height value in the grid
    /// </summary>
    public float MaxHeight { get; set; }

    /// <summary>
    /// Gets the height at a specific grid position
    /// </summary>
    public float GetHeight(int row, int col)
    {
        if (row < 0 || row >= GridSize || col < 0 || col >= GridSize)
            throw new ArgumentOutOfRangeException($"Position ({row},{col}) is outside grid bounds (0-{GridSize-1})");

        return Heights[row, col];
    }

    /// <summary>
    /// Gets interpolated height at normalized coordinates (0-1)
    /// </summary>
    public float GetInterpolatedHeight(double u, double v)
    {
        // Convert normalized coordinates to grid coordinates
        double x = u * (GridSize - 1);
        double y = v * (GridSize - 1);

        // Get integer and fractional parts
        int x0 = (int)Math.Floor(x);
        int y0 = (int)Math.Floor(y);
        int x1 = Math.Min(x0 + 1, GridSize - 1);
        int y1 = Math.Min(y0 + 1, GridSize - 1);

        double fx = x - x0;
        double fy = y - y0;

        // Bilinear interpolation
        float h00 = Heights[y0, x0];
        float h10 = Heights[y0, x1];
        float h01 = Heights[y1, x0];
        float h11 = Heights[y1, x1];

        float h0 = (float)(h00 * (1 - fx) + h10 * fx);
        float h1 = (float)(h01 * (1 - fx) + h11 * fx);

        return (float)(h0 * (1 - fy) + h1 * fy);
    }

    /// <summary>
    /// Validates the heightmap data
    /// </summary>
    public bool IsValid()
    {
        if (GridSize <= 0)
            return false;

        if (Heights == null)
            return false;

        if (Heights.GetLength(0) != GridSize || Heights.GetLength(1) != GridSize)
            return false;

        return true;
    }

    /// <summary>
    /// Gets total number of height samples
    /// </summary>
    public int TotalSamples => GridSize * GridSize;
}

