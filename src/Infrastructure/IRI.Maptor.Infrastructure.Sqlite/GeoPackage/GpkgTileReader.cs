using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IRI.Maptor.Core.Common.Primitives;
using Microsoft.Data.Sqlite;

namespace IRI.Maptor.Infrastructure.Sqlite.GeoPackage;

/// <summary>
/// Reader for tile/raster data in OGC GeoPackage format
/// Specification: https://www.geopackage.org/
/// </summary>
public class GpkgTileReader : IDisposable
{
    private readonly string _filePath;
    private SqliteConnection? _connection;
    private bool _disposed;

    public GpkgTileReader(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentNullException(nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"GeoPackage file not found: {filePath}");

        _filePath = filePath;
    }

    /// <summary>
    /// Opens the GeoPackage database
    /// </summary>
    public void Open()
    {
        if (_connection != null)
            return;

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _filePath,
            Mode = SqliteOpenMode.ReadOnly,
            Cache = SqliteCacheMode.Shared
        }.ToString();

        _connection = new SqliteConnection(connectionString);
        _connection.Open();
    }

    /// <summary>
    /// Opens the GeoPackage database asynchronously
    /// </summary>
    public async Task OpenAsync()
    {
        if (_connection != null)
            return;

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _filePath,
            Mode = SqliteOpenMode.ReadOnly,
            Cache = SqliteCacheMode.Shared
        }.ToString();

        _connection = new SqliteConnection(connectionString);
        await _connection.OpenAsync();
    }

    /// <summary>
    /// Gets all tile layers available in the GeoPackage
    /// </summary>
    public List<GpkgLayerMetadata> GetTileLayers()
    {
        EnsureConnectionOpen();

        var layers = new List<GpkgLayerMetadata>();

        using var command = _connection!.CreateCommand();
        command.CommandText = @"
            SELECT table_name, data_type, identifier, description, 
                   last_change, min_x, min_y, max_x, max_y, srs_id
            FROM gpkg_contents
            WHERE data_type = 'tiles'";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            layers.Add(new GpkgLayerMetadata
            {
                TableName = reader.GetString(0),
                DataType = reader.GetString(1),
                Identifier = reader.IsDBNull(2) ? null : reader.GetString(2),
                Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                LastChange = reader.IsDBNull(4) ? null : DateTime.Parse(reader.GetString(4)),
                MinX = reader.GetDouble(5),
                MinY = reader.GetDouble(6),
                MaxX = reader.GetDouble(7),
                MaxY = reader.GetDouble(8),
                Srs_Id = reader.GetInt32(9)
            });
        }

        return layers;
    }

    /// <summary>
    /// Gets tile matrix set information for a tile layer
    /// </summary>
    public GpkgTileMatrixSet? GetTileMatrixSet(string tableName)
    {
        EnsureConnectionOpen();

        using var command = _connection!.CreateCommand();
        command.CommandText = @"
            SELECT table_name, srs_id, min_x, min_y, max_x, max_y
            FROM gpkg_tile_matrix_set
            WHERE table_name = @tableName";
        command.Parameters.AddWithValue("@tableName", tableName);

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return new GpkgTileMatrixSet
            {
                TableName = reader.GetString(0),
                SrsId = reader.GetInt32(1),
                MinX = reader.GetDouble(2),
                MinY = reader.GetDouble(3),
                MaxX = reader.GetDouble(4),
                MaxY = reader.GetDouble(5)
            };
        }

        return null;
    }

    /// <summary>
    /// Gets all tile matrices (zoom levels) for a tile layer
    /// </summary>
    public List<GpkgTileMatrix> GetTileMatrices(string tableName)
    {
        EnsureConnectionOpen();

        var matrices = new List<GpkgTileMatrix>();

        using var command = _connection!.CreateCommand();
        command.CommandText = @"
            SELECT table_name, zoom_level, matrix_width, matrix_height, 
                   tile_width, tile_height, pixel_x_size, pixel_y_size
            FROM gpkg_tile_matrix
            WHERE table_name = @tableName
            ORDER BY zoom_level";
        command.Parameters.AddWithValue("@tableName", tableName);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            matrices.Add(new GpkgTileMatrix
            {
                TableName = reader.GetString(0),
                ZoomLevel = reader.GetInt32(1),
                MatrixWidth = reader.GetInt32(2),
                MatrixHeight = reader.GetInt32(3),
                TileWidth = reader.GetInt32(4),
                TileHeight = reader.GetInt32(5),
                PixelXSize = reader.GetDouble(6),
                PixelYSize = reader.GetDouble(7)
            });
        }

        return matrices;
    }

    /// <summary>
    /// Gets a tile from the specified tile table
    /// GeoPackage uses TMS (Tile Map Service) scheme like MBTiles
    /// </summary>
    /// <param name="tableName">Name of the tile table</param>
    /// <param name="zoomLevel">Zoom level</param>
    /// <param name="tileColumn">Tile column (X)</param>
    /// <param name="tileRow">Tile row (Y) in TMS scheme (origin top-left for GeoPackage)</param>
    /// <returns>Tile data as byte array, or null if not found</returns>
    public byte[]? GetTile(string tableName, int zoomLevel, int tileColumn, int tileRow)
    {
        EnsureConnectionOpen();

        using var command = _connection!.CreateCommand();
        command.CommandText = $@"
            SELECT tile_data 
            FROM {tableName}
            WHERE zoom_level = @zoom AND tile_column = @column AND tile_row = @row";
        command.Parameters.AddWithValue("@zoom", zoomLevel);
        command.Parameters.AddWithValue("@column", tileColumn);
        command.Parameters.AddWithValue("@row", tileRow);

        var result = command.ExecuteScalar();
        return result as byte[];
    }

    /// <summary>
    /// Gets a tile asynchronously
    /// </summary>
    public async Task<byte[]?> GetTileAsync(string tableName, int zoomLevel, int tileColumn, int tileRow)
    {
        EnsureConnectionOpen();

        using var command = _connection!.CreateCommand();
        command.CommandText = $@"
            SELECT tile_data 
            FROM {tableName}
            WHERE zoom_level = @zoom AND tile_column = @column AND tile_row = @row";
        command.Parameters.AddWithValue("@zoom", zoomLevel);
        command.Parameters.AddWithValue("@column", tileColumn);
        command.Parameters.AddWithValue("@row", tileRow);

        var result = await command.ExecuteScalarAsync();
        return result as byte[];
    }

    /// <summary>
    /// Gets all available zoom levels for a tile table
    /// </summary>
    public List<int> GetZoomLevels(string tableName)
    {
        EnsureConnectionOpen();

        var zoomLevels = new List<int>();

        using var command = _connection!.CreateCommand();
        command.CommandText = $"SELECT DISTINCT zoom_level FROM {tableName} ORDER BY zoom_level";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            zoomLevels.Add(reader.GetInt32(0));
        }

        return zoomLevels;
    }

    /// <summary>
    /// Gets the number of tiles in a tile table
    /// </summary>
    public long GetTileCount(string tableName, int? zoomLevel = null)
    {
        EnsureConnectionOpen();

        using var command = _connection!.CreateCommand();

        if (zoomLevel.HasValue)
        {
            command.CommandText = $"SELECT COUNT(*) FROM {tableName} WHERE zoom_level = @zoom";
            command.Parameters.AddWithValue("@zoom", zoomLevel.Value);
        }
        else
        {
            command.CommandText = $"SELECT COUNT(*) FROM {tableName}";
        }

        var result = command.ExecuteScalar();
        return result != null ? Convert.ToInt64(result) : 0;
    }

    /// <summary>
    /// Gets the bounding box for a tile layer
    /// </summary>
    public BoundingBox? GetBoundingBox(string tableName)
    {
        var tileMatrixSet = GetTileMatrixSet(tableName);
        if (tileMatrixSet == null)
            return null;

        return new BoundingBox(
            tileMatrixSet.MinX,
            tileMatrixSet.MinY,
            tileMatrixSet.MaxX,
            tileMatrixSet.MaxY);
    }

    /// <summary>
    /// Gets tiles in a specific geographic area at a specific zoom level
    /// </summary>
    public List<TileInfo> GetTilesInBounds(string tableName, int zoomLevel, BoundingBox bounds)
    {
        EnsureConnectionOpen();

        var tiles = new List<TileInfo>();

        using var command = _connection!.CreateCommand();
        command.CommandText = $@"
            SELECT zoom_level, tile_column, tile_row, tile_data
            FROM {tableName}
            WHERE zoom_level = @zoom";
        command.Parameters.AddWithValue("@zoom", zoomLevel);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var tile = new TileInfo
            {
                ZoomLevel = reader.GetInt32(0),
                Column = reader.GetInt32(1),
                Row = reader.GetInt32(2),
                Data = reader.IsDBNull(3) ? null : (byte[])reader.GetValue(3)
            };

            tiles.Add(tile);
        }

        return tiles;
    }

    /// <summary>
    /// Gets min and max zoom levels from tile matrices
    /// </summary>
    public (int minZoom, int maxZoom)? GetZoomRange(string tableName)
    {
        var matrices = GetTileMatrices(tableName);
        if (!matrices.Any())
            return null;

        return (matrices.Min(m => m.ZoomLevel), matrices.Max(m => m.ZoomLevel));
    }

    private void EnsureConnectionOpen()
    {
        if (_connection == null)
            throw new InvalidOperationException("Connection not opened. Call Open() or OpenAsync() first.");
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _connection?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~GpkgTileReader()
    {
        Dispose();
    }
}

/// <summary>
/// Represents tile information
/// </summary>
public class TileInfo
{
    public int ZoomLevel { get; set; }
    public int Column { get; set; }
    public int Row { get; set; }
    public byte[]? Data { get; set; }

    public override string ToString()
    {
        return $"Tile: Z={ZoomLevel}, X={Column}, Y={Row}, Size={Data?.Length ?? 0} bytes";
    }
}

