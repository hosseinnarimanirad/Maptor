using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Common.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IRI.Maptor.Core.Spatial.IO.CesiumTerrain;

/// <summary>
/// Provides conversion between Cesium Quantized-Mesh terrain (.terrain) and RasterGeoTiff (DEM)
/// </summary>
public static class QuantizedMeshRasterConverter
{
    /// <summary>
    /// Converts a QuantizedMeshData to a RasterGeoTiff by sampling the mesh into a regular grid
    /// </summary>
    /// <param name="terrainData">The quantized mesh data to convert</param>
    /// <param name="tileCoordinate">The tile coordinate for geographic bounds</param>
    /// <param name="outputWidth">Desired width of the output raster (default: 256)</param>
    /// <param name="outputHeight">Desired height of the output raster (default: 256)</param>
    /// <returns>A RasterGeoTiff with interpolated elevation values</returns>
    public static RasterGeoTiff ToRasterGeoTiff(
        QuantizedMeshData terrainData,
        TerrainTileCoordinate tileCoordinate,
        int outputWidth = 256,
        int outputHeight = 256)
    {
        if (terrainData == null || !terrainData.IsValid())
            throw new ArgumentException("Invalid terrain data", nameof(terrainData));

        if (tileCoordinate == null)
            throw new ArgumentNullException(nameof(tileCoordinate));

        if (outputWidth <= 0 || outputHeight <= 0)
            throw new ArgumentException("Output dimensions must be positive");

        // Get geographic bounds
        var (west, south, east, north) = tileCoordinate.GetBoundingBox();
        var boundingBox = new BoundingBox(west, south, east, north);

        // Create output matrix
        var matrix = new Matrix(outputHeight, outputWidth);

        // Build a simple triangulation structure for fast lookup
        var triangles = BuildTriangleList(terrainData, west, south, east, north);

        // Sample each pixel in the output raster
        for (int row = 0; row < outputHeight; row++)
        {
            for (int col = 0; col < outputWidth; col++)
            {
                // Calculate geographic coordinate for this pixel
                double u = col / (double)(outputWidth - 1);
                double v = row / (double)(outputHeight - 1);

                double lon = west + u * (east - west);
                double lat = north - v * (north - south); // North to south

                // Find the triangle containing this point and interpolate height
                double height = InterpolateHeight(triangles, lon, lat, terrainData.Header.MinimumHeight);

                matrix[row, col] = height;
            }
        }

        return new RasterGeoTiff(matrix, boundingBox);
    }

    /// <summary>
    /// Converts a RasterGeoTiff (DEM) to QuantizedMeshData
    /// </summary>
    /// <param name="raster">The raster DEM to convert</param>
    /// <param name="tileCoordinate">The tile coordinate for the output terrain tile</param>
    /// <param name="simplificationTolerance">Tolerance for mesh simplification (0 = no simplification)</param>
    /// <returns>A QuantizedMeshData representing the raster as a mesh</returns>
    public static QuantizedMeshData FromRasterGeoTiff(
        RasterGeoTiff raster,
        TerrainTileCoordinate tileCoordinate,
        double simplificationTolerance = 0.0)
    {
        if (raster?.Data == null)
            throw new ArgumentException("Invalid raster data", nameof(raster));

        if (tileCoordinate == null)
            throw new ArgumentNullException(nameof(tileCoordinate));

        int width = raster.Data.NumberOfColumns;
        int height = raster.Data.NumberOfRows;

        // Get bounds
        var bbox = raster.GeodeticWgs84BoundingBox;
        double west = bbox.XMin;
        double south = bbox.YMin;
        double east = bbox.XMax;
        double north = bbox.YMax;

        // Find min/max heights
        float minHeight = float.MaxValue;
        float maxHeight = float.MinValue;

        for (int row = 0; row < height; row++)
        {
            for (int col = 0; col < width; col++)
            {
                double value = raster.Data[row, col];
                if (!double.IsNaN(value))
                {
                    minHeight = Math.Min(minHeight, (float)value);
                    maxHeight = Math.Max(maxHeight, (float)value);
                }
            }
        }

        // Create vertices from raster grid
        var vertices = new List<(double lon, double lat, double height)>();
        var vertexMap = new Dictionary<(int row, int col), int>();

        for (int row = 0; row < height; row++)
        {
            for (int col = 0; col < width; col++)
            {
                double value = raster.Data[row, col];
                if (!double.IsNaN(value))
                {
                    double uNorm = col / (double)(width - 1);
                    double vNorm = row / (double)(height - 1);

                    double lon = west + uNorm * (east - west);
                    double lat = north - vNorm * (north - south);

                    vertexMap[(row, col)] = vertices.Count;
                    vertices.Add((lon, lat, value));
                }
            }
        }

        // Create triangles from grid (two triangles per grid cell)
        var indices = new List<uint>();

        for (int row = 0; row < height - 1; row++)
        {
            for (int col = 0; col < width - 1; col++)
            {
                // Check if all four corners exist
                bool hasTopLeft = vertexMap.ContainsKey((row, col));
                bool hasTopRight = vertexMap.ContainsKey((row, col + 1));
                bool hasBottomLeft = vertexMap.ContainsKey((row + 1, col));
                bool hasBottomRight = vertexMap.ContainsKey((row + 1, col + 1));

                if (hasTopLeft && hasTopRight && hasBottomLeft && hasBottomRight)
                {
                    uint tl = (uint)vertexMap[(row, col)];
                    uint tr = (uint)vertexMap[(row, col + 1)];
                    uint bl = (uint)vertexMap[(row + 1, col)];
                    uint br = (uint)vertexMap[(row + 1, col + 1)];

                    // Triangle 1: top-left, bottom-left, top-right
                    indices.Add(tl);
                    indices.Add(bl);
                    indices.Add(tr);

                    // Triangle 2: top-right, bottom-left, bottom-right
                    indices.Add(tr);
                    indices.Add(bl);
                    indices.Add(br);
                }
            }
        }

        // Quantize vertices
        uint vertexCount = (uint)vertices.Count;
        var u = new ushort[vertexCount];
        var v = new ushort[vertexCount];
        var heightValues = new ushort[vertexCount];

        for (int i = 0; i < vertexCount; i++)
        {
            var (lon, lat, heightValue) = vertices[i];

            // Normalize to [0, 1]
            double normalizedU = (lon - west) / (east - west);
            double normalizedV = (lat - south) / (north - south);
            double normalizedHeight = (heightValue - minHeight) / (maxHeight - minHeight);

            // Quantize to 16-bit
            u[i] = (ushort)(normalizedU * 32767.0);
            v[i] = (ushort)(normalizedV * 32767.0);
            heightValues[i] = (ushort)(normalizedHeight * 32767.0);
        }

        // Calculate center in ECEF (simplified - using tile center)
        var (tileWest, tileSouth, tileEast, tileNorth) = tileCoordinate.GetBoundingBox();
        double centerLon = (tileWest + tileEast) / 2.0;
        double centerLat = (tileSouth + tileNorth) / 2.0;
        var (ecefX, ecefY, ecefZ) = GeographicToECEF(centerLon, centerLat, (minHeight + maxHeight) / 2.0);

        // Create header
        var header = new QuantizedMeshHeader
        {
            CenterX = ecefX,
            CenterY = ecefY,
            CenterZ = ecefZ,
            MinimumHeight = minHeight,
            MaximumHeight = maxHeight,
            BoundingSphereRadius = CalculateBoundingSphereRadius(tileWest, tileSouth, tileEast, tileNorth, maxHeight),
            // Other header fields would need more sophisticated calculation
        };

        // Extract edge indices
        var (westIndices, southIndices, eastIndices, northIndices) = 
            ExtractEdgeIndices(vertexMap, width, height);

        return new QuantizedMeshData
        {
            Header = header,
            VertexCount = vertexCount,
            U = u,
            V = v,
            Height = heightValues,
            Indices = indices.ToArray(),
            WestIndices = westIndices,
            SouthIndices = southIndices,
            EastIndices = eastIndices,
            NorthIndices = northIndices
        };
    }

    #region Helper Methods

    private class Triangle
    {
        public double Lon1, Lat1, Height1;
        public double Lon2, Lat2, Height2;
        public double Lon3, Lat3, Height3;
    }

    private static List<Triangle> BuildTriangleList(
        QuantizedMeshData terrainData,
        double west, double south, double east, double north)
    {
        var triangles = new List<Triangle>();

        for (int i = 0; i < terrainData.Indices.Length; i += 3)
        {
            uint idx1 = terrainData.Indices[i];
            uint idx2 = terrainData.Indices[i + 1];
            uint idx3 = terrainData.Indices[i + 2];

            // Convert vertices to geographic coordinates
            var v1 = GetGeographicVertex(terrainData, idx1, west, south, east, north);
            var v2 = GetGeographicVertex(terrainData, idx2, west, south, east, north);
            var v3 = GetGeographicVertex(terrainData, idx3, west, south, east, north);

            triangles.Add(new Triangle
            {
                Lon1 = v1.lon, Lat1 = v1.lat, Height1 = v1.height,
                Lon2 = v2.lon, Lat2 = v2.lat, Height2 = v2.height,
                Lon3 = v3.lon, Lat3 = v3.lat, Height3 = v3.height
            });
        }

        return triangles;
    }

    private static (double lon, double lat, double height) GetGeographicVertex(
        QuantizedMeshData terrainData, uint index,
        double west, double south, double east, double north)
    {
        double u = terrainData.GetNormalizedU((int)index);
        double v = terrainData.GetNormalizedV((int)index);
        double height = terrainData.GetHeight((int)index);

        double lon = west + u * (east - west);
        double lat = south + v * (north - south);

        return (lon, lat, height);
    }

    private static double InterpolateHeight(
        List<Triangle> triangles,
        double lon, double lat,
        double defaultHeight)
    {
        // Find triangle containing the point
        foreach (var tri in triangles)
        {
            if (IsPointInTriangle(lon, lat, tri.Lon1, tri.Lat1, tri.Lon2, tri.Lat2, tri.Lon3, tri.Lat3))
            {
                // Barycentric interpolation
                return BarycentricInterpolation(
                    lon, lat,
                    tri.Lon1, tri.Lat1, tri.Height1,
                    tri.Lon2, tri.Lat2, tri.Height2,
                    tri.Lon3, tri.Lat3, tri.Height3);
            }
        }

        // Point not in any triangle - return default (or could use nearest neighbor)
        return defaultHeight;
    }

    private static bool IsPointInTriangle(
        double px, double py,
        double x1, double y1, double x2, double y2, double x3, double y3)
    {
        double d1 = Sign(px, py, x1, y1, x2, y2);
        double d2 = Sign(px, py, x2, y2, x3, y3);
        double d3 = Sign(px, py, x3, y3, x1, y1);

        bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);

        return !(hasNeg && hasPos);
    }

    private static double Sign(double px, double py, double x1, double y1, double x2, double y2)
    {
        return (px - x2) * (y1 - y2) - (x1 - x2) * (py - y2);
    }

    private static double BarycentricInterpolation(
        double px, double py,
        double x1, double y1, double z1,
        double x2, double y2, double z2,
        double x3, double y3, double z3)
    {
        double denom = (y2 - y3) * (x1 - x3) + (x3 - x2) * (y1 - y3);

        if (Math.Abs(denom) < 1e-10)
            return z1; // Degenerate triangle

        double w1 = ((y2 - y3) * (px - x3) + (x3 - x2) * (py - y3)) / denom;
        double w2 = ((y3 - y1) * (px - x3) + (x1 - x3) * (py - y3)) / denom;
        double w3 = 1.0 - w1 - w2;

        return w1 * z1 + w2 * z2 + w3 * z3;
    }

    private static (uint[] west, uint[] south, uint[] east, uint[] north) ExtractEdgeIndices(
        Dictionary<(int row, int col), int> vertexMap,
        int width, int height)
    {
        var west = new List<uint>();
        var south = new List<uint>();
        var east = new List<uint>();
        var north = new List<uint>();

        // West edge (col = 0)
        for (int row = 0; row < height; row++)
        {
            if (vertexMap.ContainsKey((row, 0)))
                west.Add((uint)vertexMap[(row, 0)]);
        }

        // South edge (row = height - 1)
        for (int col = 0; col < width; col++)
        {
            if (vertexMap.ContainsKey((height - 1, col)))
                south.Add((uint)vertexMap[(height - 1, col)]);
        }

        // East edge (col = width - 1)
        for (int row = 0; row < height; row++)
        {
            if (vertexMap.ContainsKey((row, width - 1)))
                east.Add((uint)vertexMap[(row, width - 1)]);
        }

        // North edge (row = 0)
        for (int col = 0; col < width; col++)
        {
            if (vertexMap.ContainsKey((0, col)))
                north.Add((uint)vertexMap[(0, col)]);
        }

        return (west.ToArray(), south.ToArray(), east.ToArray(), north.ToArray());
    }

    /// <summary>
    /// Converts WGS84 geographic coordinates to ECEF (Earth-Centered, Earth-Fixed)
    /// </summary>
    private static (double x, double y, double z) GeographicToECEF(double longitude, double latitude, double height)
    {
        const double a = 6378137.0; // WGS84 semi-major axis
        const double f = 1.0 / 298.257223563; // WGS84 flattening
        const double e2 = 2 * f - f * f; // First eccentricity squared

        double latRad = latitude * Math.PI / 180.0;
        double lonRad = longitude * Math.PI / 180.0;

        double sinLat = Math.Sin(latRad);
        double cosLat = Math.Cos(latRad);
        double sinLon = Math.Sin(lonRad);
        double cosLon = Math.Cos(lonRad);

        double N = a / Math.Sqrt(1 - e2 * sinLat * sinLat);

        double x = (N + height) * cosLat * cosLon;
        double y = (N + height) * cosLat * sinLon;
        double z = (N * (1 - e2) + height) * sinLat;

        return (x, y, z);
    }

    private static double CalculateBoundingSphereRadius(
        double west, double south, double east, double north, double maxHeight)
    {
        // Calculate diagonal distance of tile at max height
        var (x1, y1, z1) = GeographicToECEF(west, south, maxHeight);
        var (x2, y2, z2) = GeographicToECEF(east, north, maxHeight);

        double distance = Math.Sqrt(
            (x2 - x1) * (x2 - x1) +
            (y2 - y1) * (y2 - y1) +
            (z2 - z1) * (z2 - z1));

        return distance / 2.0;
    }

    #endregion
}

