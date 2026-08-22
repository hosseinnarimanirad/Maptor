namespace IRI.Maptor.Core.Spatial.IO.CesiumTerrain;

/// <summary>
/// Represents the coordinate of a terrain tile in the tile pyramid
/// </summary>
public class TerrainTileCoordinate
{
    /// <summary>
    /// Zoom level (0 = global, higher = more detailed)
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// X coordinate (column) in the tile grid
    /// </summary>
    public int X { get; set; }

    /// <summary>
    /// Y coordinate (row) in the tile grid
    /// </summary>
    public int Y { get; set; }

    public TerrainTileCoordinate()
    {
    }

    public TerrainTileCoordinate(int level, int x, int y)
    {
        Level = level;
        X = x;
        Y = y;
    }

    /// <summary>
    /// Gets the standard filename for this tile (e.g., "15/12345/67890.terrain")
    /// </summary>
    public string GetFileName() => $"{Level}/{X}/{Y}.terrain";

    /// <summary>
    /// Calculates the tile coordinate that contains a geographic position
    /// </summary>
    /// <param name="longitude">Longitude in degrees (-180 to 180)</param>
    /// <param name="latitude">Latitude in degrees (-90 to 90)</param>
    /// <param name="zoom">Zoom level</param>
    /// <returns>Tile coordinate containing the geographic position</returns>
    public static TerrainTileCoordinate FromGeographic(double longitude, double latitude, int zoom)
    {
        int numTiles = 1 << zoom; // 2^zoom
        
        // Calculate tile indices
        int x = (int)Math.Floor((longitude + 180.0) / 360.0 * numTiles);
        int y = (int)Math.Floor((90.0 - latitude) / 180.0 * numTiles);
        
        // Clamp to valid range
        x = Math.Max(0, Math.Min(x, numTiles - 1));
        y = Math.Max(0, Math.Min(y, numTiles - 1));
        
        return new TerrainTileCoordinate(zoom, x, y);
    }

    /// <summary>
    /// Parses a terrain tile coordinate from a file path
    /// </summary>
    /// <param name="path">Path in format "level/x/y.terrain"</param>
    /// <returns>Parsed tile coordinate or null if invalid</returns>
    public static TerrainTileCoordinate FromPath(string path)
    {
        try
        {
            // Remove .terrain extension if present
            path = path.Replace(".terrain", "");

            // Split by / or \
            var parts = path.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 3)
                return null;

            // Take last 3 parts (level/x/y)
            int level = int.Parse(parts[^3]);
            int x = int.Parse(parts[^2]);
            int y = int.Parse(parts[^1]);

            return new TerrainTileCoordinate(level, x, y);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the geographic bounding box for this tile (WGS84)
    /// Assumes Web Mercator tiling scheme
    /// </summary>
    public (double west, double south, double east, double north) GetBoundingBox()
    {
        int n = 1 << Level; // 2^level
        double tileSize = 360.0 / n;

        double west = -180.0 + X * tileSize;
        double east = west + tileSize;

        // For geographic (not web mercator) tiling
        double north = 90.0 - Y * tileSize;
        double south = north - tileSize;

        return (west, south, east, north);
    }

    /// <summary>
    /// Gets the parent tile coordinate at the previous zoom level
    /// </summary>
    public TerrainTileCoordinate GetParent()
    {
        if (Level == 0)
            return null;

        return new TerrainTileCoordinate(Level - 1, X / 2, Y / 2);
    }

    /// <summary>
    /// Gets the four child tiles at the next zoom level
    /// </summary>
    public TerrainTileCoordinate[] GetChildren()
    {
        return new[]
        {
            new TerrainTileCoordinate(Level + 1, X * 2, Y * 2),         // Top-left
            new TerrainTileCoordinate(Level + 1, X * 2 + 1, Y * 2),     // Top-right
            new TerrainTileCoordinate(Level + 1, X * 2, Y * 2 + 1),     // Bottom-left
            new TerrainTileCoordinate(Level + 1, X * 2 + 1, Y * 2 + 1)  // Bottom-right
        };
    }

    public override string ToString() => $"Level {Level}: ({X}, {Y})";

    public override bool Equals(object obj)
    {
        if (obj is TerrainTileCoordinate other)
        {
            return Level == other.Level && X == other.X && Y == other.Y;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Level, X, Y);
    }
}

