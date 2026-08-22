using System;
using System.IO;

namespace IRI.Maptor.Core.Spatial.IO.CesiumTerrain;

/// <summary>
/// Unified terrain reader that automatically detects and reads both heightmap-1.0 and quantized-mesh-1.0 formats
/// </summary>
public static class TerrainReader
{
    /// <summary>
    /// Reads a terrain file and automatically detects the format
    /// </summary>
    public static (TerrainFormat format, object data) ReadAuto(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Terrain file not found: {filePath}");

        var format = DetectFormat(filePath);

        switch (format)
        {
            case TerrainFormat.Heightmap:
                var heightmapData = HeightmapReader.Read(filePath);
                return (TerrainFormat.Heightmap, heightmapData);

            case TerrainFormat.QuantizedMesh:
                var meshData = QuantizedMeshReader.Read(filePath);
                return (TerrainFormat.QuantizedMesh, meshData);

            default:
                throw new InvalidDataException($"Unable to detect terrain format for file: {filePath}");
        }
    }

    /// <summary>
    /// Detects the terrain format of a file
    /// </summary>
    public static TerrainFormat DetectFormat(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        var fileInfo = new FileInfo(filePath);
        long fileSize = fileInfo.Length;

        // Check if it's heightmap format (fixed grid size)
        if (HeightmapReader.IsHeightmapFormat(filePath))
            return TerrainFormat.Heightmap;

        // Check if it starts with quantized-mesh header (88 bytes minimum)
        if (fileSize >= 88)
        {
            try
            {
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                using var reader = new BinaryReader(stream);

                // Try to read as quantized-mesh (has specific structure)
                // Read first few bytes to validate header
                double centerX = reader.ReadDouble();
                double centerY = reader.ReadDouble();
                double centerZ = reader.ReadDouble();

                // ECEF coordinates should be in reasonable range (Earth radius ~6,371,000 meters)
                double magnitude = Math.Sqrt(centerX * centerX + centerY * centerY + centerZ * centerZ);
                
                if (magnitude > 6_000_000 && magnitude < 7_000_000)
                {
                    return TerrainFormat.QuantizedMesh;
                }
            }
            catch
            {
                // If reading fails, not quantized-mesh
            }
        }

        return TerrainFormat.Unknown;
    }

    /// <summary>
    /// Gets the height at a specific Web Mercator tile coordinate and normalized position within the tile
    /// </summary>
    /// <param name="filePath">Path to terrain tile file</param>
    /// <param name="u">Normalized U coordinate within tile (0-1, west to east)</param>
    /// <param name="v">Normalized V coordinate within tile (0-1, north to south)</param>
    /// <returns>Height in meters</returns>
    public static float GetHeightAt(string filePath, double u, double v)
    {
        var (format, data) = ReadAuto(filePath);

        switch (format)
        {
            case TerrainFormat.Heightmap:
                var heightmapData = (HeightmapData)data;
                return heightmapData.GetInterpolatedHeight(u, v);

            case TerrainFormat.QuantizedMesh:
                var meshData = (QuantizedMeshData)data;
                return GetHeightFromMesh(meshData, u, v);

            default:
                throw new NotSupportedException($"Unsupported terrain format: {format}");
        }
    }

    /// <summary>
    /// Gets the height at a specific Web Mercator tile coordinate (z/x/y) and pixel position
    /// </summary>
    /// <param name="terrainBasePath">Base path to terrain tiles</param>
    /// <param name="zoom">Zoom level</param>
    /// <param name="tileX">Tile X coordinate</param>
    /// <param name="tileY">Tile Y coordinate</param>
    /// <param name="pixelX">Pixel X within tile (0-255 for standard 256×256 tile)</param>
    /// <param name="pixelY">Pixel Y within tile (0-255 for standard 256×256 tile)</param>
    /// <param name="tileSize">Tile size in pixels (default 256)</param>
    /// <returns>Height in meters</returns>
    public static float GetHeightAtPixel(
        string terrainBasePath,
        int zoom,
        int tileX,
        int tileY,
        int pixelX,
        int pixelY,
        int tileSize = 256)
    {
        // Build terrain file path
        string filePath = Path.Combine(terrainBasePath, $"{zoom}/{tileX}/{tileY}.terrain");

        // Convert pixel position to normalized coordinates
        double u = pixelX / (double)tileSize;
        double v = pixelY / (double)tileSize;

        return GetHeightAt(filePath, u, v);
    }

    /// <summary>
    /// Gets height from quantized mesh using barycentric interpolation
    /// </summary>
    private static float GetHeightFromMesh(QuantizedMeshData meshData, double u, double v)
    {
        // Build triangle list for interpolation
        for (int i = 0; i < meshData.Indices.Length; i += 3)
        {
            uint idx0 = meshData.Indices[i];
            uint idx1 = meshData.Indices[i + 1];
            uint idx2 = meshData.Indices[i + 2];

            double u0 = meshData.GetNormalizedU((int)idx0);
            double v0 = meshData.GetNormalizedV((int)idx0);
            double h0 = meshData.GetHeight((int)idx0);

            double u1 = meshData.GetNormalizedU((int)idx1);
            double v1 = meshData.GetNormalizedV((int)idx1);
            double h1 = meshData.GetHeight((int)idx1);

            double u2 = meshData.GetNormalizedU((int)idx2);
            double v2 = meshData.GetNormalizedV((int)idx2);
            double h2 = meshData.GetHeight((int)idx2);

            // Check if point is inside this triangle
            if (IsPointInTriangle(u, v, u0, v0, u1, v1, u2, v2))
            {
                // Barycentric interpolation
                return (float)BarycentricInterpolation(u, v, u0, v0, h0, u1, v1, h1, u2, v2, h2);
            }
        }

        // Point not found in any triangle, return minimum height as fallback
        return meshData.Header.MinimumHeight;
    }

    private static bool IsPointInTriangle(
        double px, double py,
        double x0, double y0, double x1, double y1, double x2, double y2)
    {
        double d0 = Sign(px, py, x0, y0, x1, y1);
        double d1 = Sign(px, py, x1, y1, x2, y2);
        double d2 = Sign(px, py, x2, y2, x0, y0);

        bool hasNeg = (d0 < 0) || (d1 < 0) || (d2 < 0);
        bool hasPos = (d0 > 0) || (d1 > 0) || (d2 > 0);

        return !(hasNeg && hasPos);
    }

    private static double Sign(double px, double py, double x0, double y0, double x1, double y1)
    {
        return (px - x1) * (y0 - y1) - (x0 - x1) * (py - y1);
    }

    private static double BarycentricInterpolation(
        double px, double py,
        double x0, double y0, double z0,
        double x1, double y1, double z1,
        double x2, double y2, double z2)
    {
        double denom = (y1 - y2) * (x0 - x2) + (x2 - x1) * (y0 - y2);

        if (Math.Abs(denom) < 1e-10)
            return z0;

        double w0 = ((y1 - y2) * (px - x2) + (x2 - x1) * (py - y2)) / denom;
        double w1 = ((y2 - y0) * (px - x2) + (x0 - x2) * (py - y2)) / denom;
        double w2 = 1.0 - w0 - w1;

        return w0 * z0 + w1 * z1 + w2 * z2;
    }
}

