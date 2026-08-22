using System;
using System.IO;

namespace IRI.Maptor.Core.Spatial.IO.CesiumTerrain;

/// <summary>
/// Reader for heightmap-1.0 format terrain tiles
/// Regular grid of elevation samples (raster heightmap)
/// </summary>
public static class HeightmapReader
{
    /// <summary>
    /// Reads a heightmap-1.0 format terrain file
    /// </summary>
    public static HeightmapData Read(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Heightmap file not found: {filePath}");

        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var reader = new BinaryReader(stream);

        return Read(reader);
    }

    /// <summary>
    /// Reads heightmap data from a stream
    /// </summary>
    public static HeightmapData Read(Stream stream)
    {
        using var reader = new BinaryReader(stream);
        return Read(reader);
    }

    /// <summary>
    /// Reads heightmap data from a binary reader
    /// </summary>
    public static HeightmapData Read(BinaryReader reader)
    {
        // Heightmap format structure:
        // - All values in little-endian
        // - Grid of 16-bit signed integers (height values)
        // - Common grid sizes: 65×65 (4,225 values), 257×257 (66,049 values)

        // Detect grid size from file length
        long fileLength = reader.BaseStream.Length;
        int gridSize = DetectGridSize(fileLength);

        if (gridSize == 0)
            throw new InvalidDataException($"Invalid heightmap file size: {fileLength} bytes. Expected size for common grid dimensions.");

        var data = new HeightmapData
        {
            GridSize = gridSize,
            Heights = new float[gridSize, gridSize]
        };

        float minHeight = float.MaxValue;
        float maxHeight = float.MinValue;

        // Read height values (16-bit signed integers in meters)
        for (int row = 0; row < gridSize; row++)
        {
            for (int col = 0; col < gridSize; col++)
            {
                // Read as signed 16-bit integer (little-endian)
                short heightValue = reader.ReadInt16();
                float height = heightValue;

                data.Heights[row, col] = height;

                minHeight = Math.Min(minHeight, height);
                maxHeight = Math.Max(maxHeight, height);
            }
        }

        data.MinHeight = minHeight;
        data.MaxHeight = maxHeight;

        return data;
    }

    /// <summary>
    /// Detects grid size from file length
    /// Common sizes: 65×65 = 8,450 bytes, 257×257 = 132,098 bytes
    /// </summary>
    private static int DetectGridSize(long fileLength)
    {
        // Each height value is 2 bytes (16-bit signed integer)
        long numValues = fileLength / 2;

        // Check common grid sizes
        int[] commonSizes = { 65, 129, 257, 513, 1025 };

        foreach (int size in commonSizes)
        {
            if (size * size == numValues)
                return size;
        }

        // Try to find perfect square
        double sqrtValues = Math.Sqrt(numValues);
        if (Math.Abs(sqrtValues - Math.Round(sqrtValues)) < 0.001)
        {
            return (int)Math.Round(sqrtValues);
        }

        return 0; // Unknown size
    }

    /// <summary>
    /// Checks if a file is likely a heightmap format based on size
    /// </summary>
    public static bool IsHeightmapFormat(string filePath)
    {
        if (!File.Exists(filePath))
            return false;

        var fileInfo = new FileInfo(filePath);
        long fileLength = fileInfo.Length;

        return DetectGridSize(fileLength) > 0;
    }

    /// <summary>
    /// Checks if a stream contains heightmap format data
    /// </summary>
    public static bool IsHeightmapFormat(Stream stream)
    {
        return DetectGridSize(stream.Length) > 0;
    }
}

