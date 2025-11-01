using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using IRI.Maptor.Sta.Common.Primitives;

namespace IRI.Maptor.Ket.SqlitePersistence.MbTiles;

/// <summary>
/// Reader for MBTiles format - a specification for storing tiled map data in SQLite databases
/// Specification: https://github.com/mapbox/mbtiles-spec
/// </summary>
public class MbTilesReader : IDisposable
{
    private readonly string _filePath;
    private SqliteConnection? _connection;
    private bool _disposed;

    public MbTilesMetadata? Metadata { get; private set; }

    public MbTilesReader(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentNullException(nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"MBTiles file not found: {filePath}");

        _filePath = filePath;
    }

    /// <summary>
    /// Opens the MBTiles database and reads metadata
    /// </summary>
    public void Open()
    {
        if (_connection != null)
            return; // Already opened

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _filePath,
            Mode = SqliteOpenMode.ReadOnly,
            Cache = SqliteCacheMode.Shared
        }.ToString();

        _connection = new SqliteConnection(connectionString);
        _connection.Open();

        // Read metadata
        Metadata = ReadMetadata();
    }

    /// <summary>
    /// Opens the MBTiles database asynchronously
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

        Metadata = await ReadMetadataAsync();
    }

    /// <summary>
    /// Reads metadata from the metadata table
    /// </summary>
    private MbTilesMetadata ReadMetadata()
    {
        EnsureConnectionOpen();

        var metadata = new MbTilesMetadata();

        using var command = _connection!.CreateCommand();
        command.CommandText = "SELECT name, value FROM metadata";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var name = reader.GetString(0);
            var value = reader.IsDBNull(1) ? null : reader.GetString(1);

            switch (name.ToLowerInvariant())
            {
                case "name":
                    metadata.Name = value;
                    break;
                case "format":
                    metadata.Format = value;
                    break;
                case "bounds":
                    metadata.Bounds = value;
                    break;
                case "center":
                    metadata.Center = value;
                    break;
                case "minzoom":
                    if (int.TryParse(value, out int minZoom))
                        metadata.MinZoom = minZoom;
                    break;
                case "maxzoom":
                    if (int.TryParse(value, out int maxZoom))
                        metadata.MaxZoom = maxZoom;
                    break;
                case "description":
                    metadata.Description = value;
                    break;
                case "attribution":
                    metadata.Attribution = value;
                    break;
                case "type":
                    metadata.Type = value;
                    break;
                case "version":
                    metadata.Version = value;
                    break;
                default:
                    if (value != null)
                        metadata.AdditionalMetadata[name] = value;
                    break;
            }
        }

        return metadata;
    }

    /// <summary>
    /// Reads metadata asynchronously
    /// </summary>
    private async Task<MbTilesMetadata> ReadMetadataAsync()
    {
        EnsureConnectionOpen();

        var metadata = new MbTilesMetadata();

        using var command = _connection!.CreateCommand();
        command.CommandText = "SELECT name, value FROM metadata";

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var name = reader.GetString(0);
            var value = reader.IsDBNull(1) ? null : reader.GetString(1);

            switch (name.ToLowerInvariant())
            {
                case "name":
                    metadata.Name = value;
                    break;
                case "format":
                    metadata.Format = value;
                    break;
                case "bounds":
                    metadata.Bounds = value;
                    break;
                case "center":
                    metadata.Center = value;
                    break;
                case "minzoom":
                    if (int.TryParse(value, out int minZoom))
                        metadata.MinZoom = minZoom;
                    break;
                case "maxzoom":
                    if (int.TryParse(value, out int maxZoom))
                        metadata.MaxZoom = maxZoom;
                    break;
                case "description":
                    metadata.Description = value;
                    break;
                case "attribution":
                    metadata.Attribution = value;
                    break;
                case "type":
                    metadata.Type = value;
                    break;
                case "version":
                    metadata.Version = value;
                    break;
                default:
                    if (value != null)
                        metadata.AdditionalMetadata[name] = value;
                    break;
            }
        }

        return metadata;
    }

    /// <summary>
    /// Gets a tile at the specified zoom level and tile coordinates
    /// </summary>
    /// <param name="zoom">Zoom level</param>
    /// <param name="column">Tile column (X)</param>
    /// <param name="row">Tile row (Y) - Note: MBTiles uses TMS scheme (origin bottom-left)</param>
    /// <returns>Tile data as byte array, or null if not found</returns>
    public byte[]? GetTile(int zoom, int column, int row)
    {
        EnsureConnectionOpen();

        using var command = _connection!.CreateCommand();
        command.CommandText = "SELECT tile_data FROM tiles WHERE zoom_level = @zoom AND tile_column = @column AND tile_row = @row";
        command.Parameters.AddWithValue("@zoom", zoom);
        command.Parameters.AddWithValue("@column", column);
        command.Parameters.AddWithValue("@row", row);

        var result = command.ExecuteScalar();
        return result as byte[];
    }

    /// <summary>
    /// Gets a tile asynchronously
    /// </summary>
    public async Task<byte[]?> GetTileAsync(int zoom, int column, int row)
    {
        EnsureConnectionOpen();

        using var command = _connection!.CreateCommand();
        command.CommandText = "SELECT tile_data FROM tiles WHERE zoom_level = @zoom AND tile_column = @column AND tile_row = @row";
        command.Parameters.AddWithValue("@zoom", zoom);
        command.Parameters.AddWithValue("@column", column);
        command.Parameters.AddWithValue("@row", row);

        var result = await command.ExecuteScalarAsync();
        return result as byte[];
    }

    /// <summary>
    /// Gets all available zoom levels
    /// </summary>
    public List<int> GetZoomLevels()
    {
        EnsureConnectionOpen();

        var zoomLevels = new List<int>();

        using var command = _connection!.CreateCommand();
        command.CommandText = "SELECT DISTINCT zoom_level FROM tiles ORDER BY zoom_level";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            zoomLevels.Add(reader.GetInt32(0));
        }

        return zoomLevels;
    }

    /// <summary>
    /// Gets the number of tiles at a specific zoom level
    /// </summary>
    public long GetTileCount(int? zoomLevel = null)
    {
        EnsureConnectionOpen();

        using var command = _connection!.CreateCommand();
        
        if (zoomLevel.HasValue)
        {
            command.CommandText = "SELECT COUNT(*) FROM tiles WHERE zoom_level = @zoom";
            command.Parameters.AddWithValue("@zoom", zoomLevel.Value);
        }
        else
        {
            command.CommandText = "SELECT COUNT(*) FROM tiles";
        }

        var result = command.ExecuteScalar();
        return result != null ? Convert.ToInt64(result) : 0;
    }

    /// <summary>
    /// Gets the bounding box from metadata (WGS84)
    /// </summary>
    public BoundingBox? GetBoundingBox()
    {
        if (Metadata?.Bounds == null)
            return null;

        try
        {
            var parts = Metadata.Bounds.Split(',');
            if (parts.Length != 4)
                return null;

            var west = double.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture);
            var south = double.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
            var east = double.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture);
            var north = double.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture);

            return new BoundingBox(west, south, east, north);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Checks if the database has the required MBTiles schema
    /// </summary>
    public bool ValidateSchema()
    {
        EnsureConnectionOpen();

        try
        {
            using var command = _connection!.CreateCommand();
            command.CommandText = @"
                SELECT COUNT(*) FROM sqlite_master 
                WHERE type='table' AND (name='tiles' OR name='metadata')";

            var result = command.ExecuteScalar();
            return result != null && Convert.ToInt32(result) == 2;
        }
        catch
        {
            return false;
        }
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

    ~MbTilesReader()
    {
        Dispose();
    }
}

