using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Mathematics;
using System;

namespace IRI.Maptor.Sta.Spatial.IO.CesiumTerrain;

/// <summary>
/// Provides conversion between heightmap-1.0 format and RasterGeoTiff (DEM)
/// </summary>
public static class HeightmapRasterConverter
{
    /// <summary>
    /// Converts a HeightmapData to RasterGeoTiff
    /// </summary>
    /// <param name="heightmapData">The heightmap data to convert</param>
    /// <param name="tileCoordinate">The tile coordinate for geographic bounds</param>
    /// <returns>A RasterGeoTiff with the heightmap elevation values</returns>
    public static RasterGeoTiff ToRasterGeoTiff(
        HeightmapData heightmapData,
        TerrainTileCoordinate tileCoordinate)
    {
        if (heightmapData == null || !heightmapData.IsValid())
            throw new ArgumentException("Invalid heightmap data", nameof(heightmapData));

        if (tileCoordinate == null)
            throw new ArgumentNullException(nameof(tileCoordinate));

        // Get geographic bounds
        var (west, south, east, north) = tileCoordinate.GetBoundingBox();
        var boundingBox = new BoundingBox(west, south, east, north);

        // Create output matrix (heightmap is already a regular grid)
        int gridSize = heightmapData.GridSize;
        var matrix = new Matrix(gridSize, gridSize);

        // Copy height values to matrix
        for (int row = 0; row < gridSize; row++)
        {
            for (int col = 0; col < gridSize; col++)
            {
                matrix[row, col] = heightmapData.Heights[row, col];
            }
        }

        return new RasterGeoTiff(matrix, boundingBox);
    }

    /// <summary>
    /// Converts a RasterGeoTiff to HeightmapData
    /// </summary>
    /// <param name="raster">The raster DEM to convert</param>
    /// <param name="targetGridSize">Target grid size (must be 2^n + 1, e.g., 65, 129, 257)</param>
    /// <returns>A HeightmapData with resampled elevation values</returns>
    public static HeightmapData FromRasterGeoTiff(
        RasterGeoTiff raster,
        int targetGridSize = 257)
    {
        if (raster?.Data == null)
            throw new ArgumentException("Invalid raster data", nameof(raster));

        // Validate grid size (should be 2^n + 1)
        if (!IsValidGridSize(targetGridSize))
            throw new ArgumentException($"Grid size must be 2^n + 1 (e.g., 65, 129, 257, 513). Got: {targetGridSize}", nameof(targetGridSize));

        int sourceWidth = raster.Data.NumberOfColumns;
        int sourceHeight = raster.Data.NumberOfRows;

        var heightmapData = new HeightmapData
        {
            GridSize = targetGridSize,
            Heights = new float[targetGridSize, targetGridSize]
        };

        float minHeight = float.MaxValue;
        float maxHeight = float.MinValue;

        // Resample the raster to the target grid size
        for (int row = 0; row < targetGridSize; row++)
        {
            for (int col = 0; col < targetGridSize; col++)
            {
                // Calculate source coordinates
                double srcRow = row * (sourceHeight - 1.0) / (targetGridSize - 1.0);
                double srcCol = col * (sourceWidth - 1.0) / (targetGridSize - 1.0);

                // Bilinear interpolation
                int r0 = (int)Math.Floor(srcRow);
                int c0 = (int)Math.Floor(srcCol);
                int r1 = Math.Min(r0 + 1, sourceHeight - 1);
                int c1 = Math.Min(c0 + 1, sourceWidth - 1);

                double fr = srcRow - r0;
                double fc = srcCol - c0;

                double h00 = raster.Data[r0, c0];
                double h10 = raster.Data[r0, c1];
                double h01 = raster.Data[r1, c0];
                double h11 = raster.Data[r1, c1];

                // Handle NaN values
                if (double.IsNaN(h00) || double.IsNaN(h10) || double.IsNaN(h01) || double.IsNaN(h11))
                {
                    heightmapData.Heights[row, col] = 0; // Use 0 for missing data
                }
                else
                {
                    // Bilinear interpolation
                    double h0 = h00 * (1 - fc) + h10 * fc;
                    double h1 = h01 * (1 - fc) + h11 * fc;
                    float height = (float)(h0 * (1 - fr) + h1 * fr);

                    heightmapData.Heights[row, col] = height;

                    minHeight = Math.Min(minHeight, height);
                    maxHeight = Math.Max(maxHeight, height);
                }
            }
        }

        heightmapData.MinHeight = minHeight;
        heightmapData.MaxHeight = maxHeight;

        return heightmapData;
    }

    /// <summary>
    /// Converts a heightmap to a different grid size
    /// </summary>
    /// <param name="heightmapData">Source heightmap</param>
    /// <param name="targetGridSize">Target grid size</param>
    /// <returns>Resampled heightmap</returns>
    public static HeightmapData Resample(HeightmapData heightmapData, int targetGridSize)
    {
        if (heightmapData == null || !heightmapData.IsValid())
            throw new ArgumentException("Invalid heightmap data", nameof(heightmapData));

        if (!IsValidGridSize(targetGridSize))
            throw new ArgumentException($"Grid size must be 2^n + 1. Got: {targetGridSize}", nameof(targetGridSize));

        if (heightmapData.GridSize == targetGridSize)
            return heightmapData; // No resampling needed

        var result = new HeightmapData
        {
            GridSize = targetGridSize,
            Heights = new float[targetGridSize, targetGridSize]
        };

        float minHeight = float.MaxValue;
        float maxHeight = float.MinValue;

        int sourceSize = heightmapData.GridSize;

        for (int row = 0; row < targetGridSize; row++)
        {
            for (int col = 0; col < targetGridSize; col++)
            {
                // Calculate normalized coordinates (0-1)
                double u = col / (double)(targetGridSize - 1);
                double v = row / (double)(targetGridSize - 1);

                // Get interpolated height from source
                float height = heightmapData.GetInterpolatedHeight(u, v);

                result.Heights[row, col] = height;

                minHeight = Math.Min(minHeight, height);
                maxHeight = Math.Max(maxHeight, height);
            }
        }

        result.MinHeight = minHeight;
        result.MaxHeight = maxHeight;

        return result;
    }

    /// <summary>
    /// Validates if a grid size is valid (must be 2^n + 1)
    /// </summary>
    private static bool IsValidGridSize(int size)
    {
        // Common valid sizes: 65 (2^6+1), 129 (2^7+1), 257 (2^8+1), 513 (2^9+1), 1025 (2^10+1)
        int[] validSizes = { 3, 5, 9, 17, 33, 65, 129, 257, 513, 1025, 2049, 4097 };
        return Array.IndexOf(validSizes, size) >= 0;
    }

    /// <summary>
    /// Gets the nearest valid grid size for a given dimension
    /// </summary>
    public static int GetNearestValidGridSize(int desiredSize)
    {
        int[] validSizes = { 65, 129, 257, 513, 1025 };

        int nearest = validSizes[0];
        int minDiff = Math.Abs(desiredSize - nearest);

        foreach (int size in validSizes)
        {
            int diff = Math.Abs(desiredSize - size);
            if (diff < minDiff)
            {
                minDiff = diff;
                nearest = size;
            }
        }

        return nearest;
    }
}

