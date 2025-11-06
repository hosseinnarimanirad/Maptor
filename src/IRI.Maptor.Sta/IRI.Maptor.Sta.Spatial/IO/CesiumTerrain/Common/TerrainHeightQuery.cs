using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IRI.Maptor.Sta.Spatial.IO.CesiumTerrain;

/// <summary>
/// Provides high-level methods for querying terrain heights across multiple tiles
/// </summary>
public static class TerrainHeightQuery
{
    /// <summary>
    /// Gets a Matrix of heights for a bounding box at a specific zoom level using heightmap-1.0 format
    /// </summary>
    /// <param name="boundingBox">Geographic bounding box (WGS84)</param>
    /// <param name="zoomLevel">Zoom level</param>
    /// <param name="terrainBasePath">Base path to terrain tiles directory</param>
    /// <param name="outputResolution">Output resolution (samples per degree, higher = more detail). Default: 0 (native tile resolution)</param>
    /// <returns>Matrix containing elevation values in meters, with geographic extent matching the bounding box</returns>
    public static (Matrix heights, BoundingBox actualBounds) GetHeightsForBoundary(
        BoundingBox boundingBox,
        int zoomLevel,
        string terrainBasePath,
        int outputResolution = 0)
    {
        if (boundingBox == null)
            throw new ArgumentNullException(nameof(boundingBox));

        if (!Directory.Exists(terrainBasePath))
            throw new DirectoryNotFoundException($"Terrain path not found: {terrainBasePath}");

        // Get all tiles that intersect with the boundary
        var tileCoords = GetTileCoordinatesForBoundingBox(boundingBox, zoomLevel);

        if (tileCoords.Count == 0)
            throw new InvalidOperationException($"No tiles found for the specified bounding box at zoom level {zoomLevel}");

        Console.WriteLine($"Found {tileCoords.Count} tiles for zoom level {zoomLevel}");

        // Load all heightmap tiles
        var heightmapTiles = new List<(TerrainTileCoordinate coord, HeightmapData data)>();

        foreach (var coord in tileCoords)
        {
            string tilePath = Path.Combine(terrainBasePath, coord.GetFileName());

            if (!File.Exists(tilePath))
            {
                Console.WriteLine($"Warning: Tile not found: {tilePath}");
                continue;
            }

            try
            {
                // Try to read as heightmap
                if (HeightmapReader.IsHeightmapFormat(tilePath))
                {
                    var heightmapData = HeightmapReader.Read(tilePath);
                    heightmapTiles.Add((coord, heightmapData));
                    Console.WriteLine($"Loaded heightmap tile {coord} ({heightmapData.GridSize}×{heightmapData.GridSize})");
                }
                else
                {
                    // Try quantized-mesh and convert to raster
                    var meshData = QuantizedMeshReader.Read(tilePath);
                    
                    // Convert to raster then to heightmap
                    var raster = QuantizedMeshRasterConverter.ToRasterGeoTiff(meshData, coord, 257, 257);
                    var heightmap = HeightmapRasterConverter.FromRasterGeoTiff(raster, 257);
                    
                    heightmapTiles.Add((coord, heightmap));
                    Console.WriteLine($"Loaded and converted quantized-mesh tile {coord}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading tile {coord}: {ex.Message}");
            }
        }

        if (heightmapTiles.Count == 0)
            throw new InvalidOperationException("No valid tiles loaded");

        // Merge tiles into a single matrix
        var mergedResult = MergeHeightmapTiles(heightmapTiles, boundingBox, outputResolution);

        return mergedResult;
    }

    /// <summary>
    /// Merges multiple heightmap tiles into a single Matrix covering the bounding box
    /// </summary>
    private static (Matrix heights, BoundingBox actualBounds) MergeHeightmapTiles(
        List<(TerrainTileCoordinate coord, HeightmapData data)> tiles,
        BoundingBox requestedBounds,
        int outputResolution)
    {
        // Find the overall extent of loaded tiles
        var coords = tiles.Select(t => t.coord).ToList();
        int minX = coords.Min(c => c.X);
        int maxX = coords.Max(c => c.X);
        int minY = coords.Min(c => c.Y);
        int maxY = coords.Max(c => c.Y);
        int level = coords[0].Level;

        // Calculate actual geographic bounds
        var firstTile = new TerrainTileCoordinate(level, minX, minY);
        var lastTile = new TerrainTileCoordinate(level, maxX, maxY);
        
        var (west, _, _, north) = firstTile.GetBoundingBox();
        var (_, south, east, _) = lastTile.GetBoundingBox();
        
        var actualBounds = new BoundingBox(west, south, east, north);

        // Determine output dimensions
        int tilesWide = maxX - minX + 1;
        int tilesHigh = maxY - minY + 1;
        
        // Assume all tiles have the same grid size
        int tileGridSize = tiles[0].data.GridSize;
        
        int totalWidth = tilesWide * tileGridSize;
        int totalHeight = tilesHigh * tileGridSize;

        Console.WriteLine($"Merging {tiles.Count} tiles into {totalWidth}×{totalHeight} matrix");

        // Create output matrix
        var merged = new Matrix(totalHeight, totalWidth);

        // Initialize with NaN
        for (int row = 0; row < totalHeight; row++)
        {
            for (int col = 0; col < totalWidth; col++)
            {
                merged[row, col] = double.NaN;
            }
        }

        // Create a lookup dictionary for tiles
        var tileLookup = tiles.ToDictionary(t => (t.coord.X, t.coord.Y), t => t.data);

        // Copy each tile into the merged matrix
        for (int tileY = minY; tileY <= maxY; tileY++)
        {
            for (int tileX = minX; tileX <= maxX; tileX++)
            {
                if (!tileLookup.ContainsKey((tileX, tileY)))
                    continue;

                var heightmapData = tileLookup[(tileX, tileY)];
                int gridSize = heightmapData.GridSize;

                // Calculate position in merged matrix
                int startRow = (tileY - minY) * gridSize;
                int startCol = (tileX - minX) * gridSize;

                // Copy heights
                for (int row = 0; row < gridSize; row++)
                {
                    for (int col = 0; col < gridSize; col++)
                    {
                        int targetRow = startRow + row;
                        int targetCol = startCol + col;

                        if (targetRow < totalHeight && targetCol < totalWidth)
                        {
                            merged[targetRow, targetCol] = heightmapData.GetHeight(row, col);
                        }
                    }
                }
            }
        }

        // Optionally resample to requested resolution
        if (outputResolution > 0)
        {
            // Calculate desired output size based on resolution
            double degreesPerSample = 1.0 / outputResolution;
            double widthDegrees = east - west;
            double heightDegrees = north - south;
            
            int desiredWidth = (int)Math.Ceiling(widthDegrees / degreesPerSample);
            int desiredHeight = (int)Math.Ceiling(heightDegrees / degreesPerSample);

            merged = ResampleMatrix(merged, desiredWidth, desiredHeight);
            Console.WriteLine($"Resampled to {desiredWidth}×{desiredHeight}");
        }

        return (merged, actualBounds);
    }

    /// <summary>
    /// Resamples a matrix to a different size using bilinear interpolation
    /// </summary>
    private static Matrix ResampleMatrix(Matrix source, int targetWidth, int targetHeight)
    {
        int srcHeight = source.NumberOfRows;
        int srcWidth = source.NumberOfColumns;

        var result = new Matrix(targetHeight, targetWidth);

        for (int y = 0; y < targetHeight; y++)
        {
            for (int x = 0; x < targetWidth; x++)
            {
                // Calculate source coordinates
                double srcX = x * (srcWidth - 1.0) / (targetWidth - 1.0);
                double srcY = y * (srcHeight - 1.0) / (targetHeight - 1.0);

                int x0 = (int)Math.Floor(srcX);
                int y0 = (int)Math.Floor(srcY);
                int x1 = Math.Min(x0 + 1, srcWidth - 1);
                int y1 = Math.Min(y0 + 1, srcHeight - 1);

                double fx = srcX - x0;
                double fy = srcY - y0;

                double v00 = source[y0, x0];
                double v10 = source[y0, x1];
                double v01 = source[y1, x0];
                double v11 = source[y1, x1];

                // Check for NaN
                if (double.IsNaN(v00) || double.IsNaN(v10) || double.IsNaN(v01) || double.IsNaN(v11))
                {
                    result[y, x] = double.NaN;
                }
                else
                {
                    // Bilinear interpolation
                    double v0 = v00 * (1 - fx) + v10 * fx;
                    double v1 = v01 * (1 - fx) + v11 * fx;
                    result[y, x] = v0 * (1 - fy) + v1 * fy;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Gets tile coordinates that intersect with a bounding box at a given zoom level
    /// </summary>
    private static List<TerrainTileCoordinate> GetTileCoordinatesForBoundingBox(
        BoundingBox bbox,
        int zoomLevel)
    {
        var tiles = new List<TerrainTileCoordinate>();

        double west = bbox.XMin;
        double south = bbox.YMin;
        double east = bbox.XMax;
        double north = bbox.YMax;

        // Calculate tile indices
        int numTiles = 1 << zoomLevel; // 2^zoomLevel
        double tileWidth = 360.0 / numTiles;
        double tileHeight = 180.0 / numTiles;

        int minX = (int)Math.Floor((west + 180.0) / tileWidth);
        int maxX = (int)Math.Floor((east + 180.0) / tileWidth);
        int minY = (int)Math.Floor((90.0 - north) / tileHeight);
        int maxY = (int)Math.Floor((90.0 - south) / tileHeight);

        // Clamp to valid range
        minX = Math.Max(0, Math.Min(minX, numTiles - 1));
        maxX = Math.Max(0, Math.Min(maxX, numTiles - 1));
        minY = Math.Max(0, Math.Min(minY, numTiles - 1));
        maxY = Math.Max(0, Math.Min(maxY, numTiles - 1));

        // Generate all tile coordinates
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                tiles.Add(new TerrainTileCoordinate(zoomLevel, x, y));
            }
        }

        return tiles;
    }

    /// <summary>
    /// Gets heights for a bounding box with a specific output matrix size
    /// </summary>
    /// <param name="boundingBox">Geographic bounding box (WGS84)</param>
    /// <param name="zoomLevel">Zoom level</param>
    /// <param name="terrainBasePath">Base path to terrain tiles</param>
    /// <param name="outputWidth">Desired output width in pixels</param>
    /// <param name="outputHeight">Desired output height in pixels</param>
    /// <returns>Matrix of heights resampled to exact output dimensions</returns>
    public static (Matrix heights, BoundingBox bounds) GetHeightsForBoundaryWithSize(
        BoundingBox boundingBox,
        int zoomLevel,
        string terrainBasePath,
        int outputWidth,
        int outputHeight)
    {
        // First get the native resolution heights
        var (nativeHeights, actualBounds) = GetHeightsForBoundary(
            boundingBox,
            zoomLevel,
            terrainBasePath,
            outputResolution: 0  // Native resolution
        );

        // Resample to exact requested size
        var resampledHeights = ResampleMatrix(nativeHeights, outputWidth, outputHeight);

        return (resampledHeights, actualBounds);
    }

    /// <summary>
    /// Gets heights for display on a specific screen size
    /// Optimized for 3D terrain visualization
    /// </summary>
    /// <param name="boundingBox">Geographic bounding box to display</param>
    /// <param name="zoomLevel">Zoom level for detail</param>
    /// <param name="terrainBasePath">Path to terrain tiles</param>
    /// <param name="displayWidth">Screen/display width in pixels (e.g., 1024)</param>
    /// <param name="displayHeight">Screen/display height in pixels (e.g., 1024)</param>
    /// <returns>Matrix matching display dimensions with elevation data</returns>
    public static Matrix GetHeightsForDisplay(
        BoundingBox boundingBox,
        int zoomLevel,
        string terrainBasePath,
        int displayWidth = 1024,
        int displayHeight = 1024)
    {
        var (heights, bounds) = GetHeightsForBoundaryWithSize(
            boundingBox,
            zoomLevel,
            terrainBasePath,
            displayWidth,
            displayHeight
        );

        Console.WriteLine($"Generated {displayWidth}×{displayHeight} elevation grid");
        Console.WriteLine($"Bounds: [{bounds.XMin:F6}, {bounds.YMin:F6}, {bounds.XMax:F6}, {bounds.YMax:F6}]");

        // Calculate statistics
        double minHeight = double.MaxValue;
        double maxHeight = double.MinValue;
        int validPoints = 0;

        for (int row = 0; row < heights.NumberOfRows; row++)
        {
            for (int col = 0; col < heights.NumberOfColumns; col++)
            {
                double h = heights[row, col];
                if (!double.IsNaN(h))
                {
                    minHeight = Math.Min(minHeight, h);
                    maxHeight = Math.Max(maxHeight, h);
                    validPoints++;
                }
            }
        }

        Console.WriteLine($"Elevation range: {minHeight:F1}m to {maxHeight:F1}m ({validPoints} valid points)");

        return heights;
    }

    /// <summary>
    /// Gets heights along a geographic path/line
    /// </summary>
    /// <param name="waypoints">List of lon/lat waypoints</param>
    /// <param name="zoomLevel">Zoom level for terrain detail</param>
    /// <param name="terrainBasePath">Path to terrain tiles</param>
    /// <param name="samplesPerSegment">Number of samples between each waypoint</param>
    /// <returns>List of (longitude, latitude, elevation) tuples</returns>
    public static List<(double longitude, double latitude, float elevation)> GetElevationProfile(
        List<(double lon, double lat)> waypoints,
        int zoomLevel,
        string terrainBasePath,
        int samplesPerSegment = 10)
    {
        var profile = new List<(double, double, float)>();

        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            var (lon1, lat1) = waypoints[i];
            var (lon2, lat2) = waypoints[i + 1];

            // Interpolate between waypoints
            for (int s = 0; s <= samplesPerSegment; s++)
            {
                double t = s / (double)samplesPerSegment;
                double lon = lon1 + t * (lon2 - lon1);
                double lat = lat1 + t * (lat2 - lat1);

                // Get tile containing this point
                var coord = TerrainTileCoordinate.FromGeographic(lon, lat, zoomLevel);
                var (west, south, east, north) = coord.GetBoundingBox();

                // Calculate position within tile
                double u = (lon - west) / (east - west);
                double v = (north - lat) / (north - south);

                string tilePath = Path.Combine(terrainBasePath, coord.GetFileName());

                float elevation = 0;
                if (File.Exists(tilePath))
                {
                    try
                    {
                        elevation = TerrainReader.GetHeightAt(tilePath, u, v);
                    }
                    catch
                    {
                        elevation = float.NaN;
                    }
                }

                profile.Add((lon, lat, elevation));
            }
        }

        return profile;
    }
}

