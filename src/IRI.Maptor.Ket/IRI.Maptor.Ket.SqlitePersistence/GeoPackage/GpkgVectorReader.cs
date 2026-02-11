using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Ket.SqlitePersistence.GeoPackage;

/// <summary>
/// Reader for vector features in OGC GeoPackage format
/// Specification: https://www.geopackage.org/
/// </summary>
public class GpkgVectorReader : IDisposable
{
    private readonly string _filePath;
    private SqliteConnection? _connection;
    private bool _disposed;

    public GpkgVectorReader(string filePath)
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
    /// Gets all feature layers (tables) available in the GeoPackage
    /// </summary>
    public List<GpkgLayerMetadata> GetFeatureLayers()
    {
        EnsureConnectionOpen();

        var layers = new List<GpkgLayerMetadata>();

        using var command = _connection!.CreateCommand();
        command.CommandText = @"
            SELECT table_name, data_type, identifier, description, 
                   last_change, min_x, min_y, max_x, max_y, srs_id
            FROM gpkg_contents
            WHERE data_type = 'features'";

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
    /// Gets geometry column information for a feature layer
    /// </summary>
    public GpkgGeometryColumn? GetGeometryColumnInfo(string tableName)
    {
        EnsureConnectionOpen();

        using var command = _connection!.CreateCommand();
        command.CommandText = @"
            SELECT table_name, column_name, geometry_type_name, srs_id, z, m
            FROM gpkg_geometry_columns
            WHERE table_name = @tableName";
        command.Parameters.AddWithValue("@tableName", tableName);

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return new GpkgGeometryColumn
            {
                TableName = reader.GetString(0),
                ColumnName = reader.GetString(1),
                GeometryTypeName = reader.GetString(2),
                SrsId = reader.GetInt32(3),
                HasZ = reader.GetInt32(4) > 0,
                HasM = reader.GetInt32(5) > 0
            };
        }

        return null;
    }

    /// <summary>
    /// Reads all features from a layer
    /// </summary>
    public List<Feature<Point>> ReadFeatures(string tableName)
    {
        EnsureConnectionOpen();

        var geometryColumn = GetGeometryColumnInfo(tableName);
        if (geometryColumn == null)
            throw new InvalidOperationException($"No geometry column found for table: {tableName}");

        var features = new List<Feature<Point>>();

        // Get all column names
        var columns = GetColumnNames(tableName);

        using var command = _connection!.CreateCommand();
        command.CommandText = $"SELECT * FROM {tableName}";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var feature = new Feature<Point>();

            // Read attributes
            var attributes = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var columnName = reader.GetName(i);

                if (columnName.Equals(geometryColumn.ColumnName, StringComparison.OrdinalIgnoreCase))
                {
                    // Read geometry as WKB (Well-Known Binary)
                    if (!reader.IsDBNull(i))
                    {
                        var wkb = GetGeoPackageGeometry(reader, i);
                        if (wkb != null && wkb.Length > 0)
                        {
                            try
                            {
                                feature.TheGeometry = Geometry<Point>.FromWkb(wkb, geometryColumn.SrsId);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Trace.WriteLine($"Error parsing geometry: {ex.Message}");
                                feature.TheGeometry = Geometry<Point>.Empty;
                            }
                        }
                    }
                }
                else
                {
                    // Store attribute
                    attributes[columnName] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }
            }

            feature.Attributes = attributes;
            features.Add(feature);
        }

        return features;
    }

    /// <summary>
    /// Reads features that intersect with the given bounding box
    /// </summary>
    public List<Feature<Point>> ReadFeatures(string tableName, BoundingBox boundingBox)
    {
        EnsureConnectionOpen();

        var geometryColumn = GetGeometryColumnInfo(tableName);
        if (geometryColumn == null)
            throw new InvalidOperationException($"No geometry column found for table: {tableName}");

        var features = new List<Feature<Point>>();

        using var command = _connection!.CreateCommand();
        
        // Use spatial index if available (rtree)
        command.CommandText = $@"
            SELECT f.*
            FROM {tableName} f
            WHERE f.rowid IN (
                SELECT id FROM rtree_{tableName}_{geometryColumn.ColumnName}
                WHERE minx <= @maxX AND maxx >= @minX 
                  AND miny <= @maxY AND maxy >= @minY
            )";
        
        command.Parameters.AddWithValue("@minX", boundingBox.XMin);
        command.Parameters.AddWithValue("@minY", boundingBox.YMin);
        command.Parameters.AddWithValue("@maxX", boundingBox.XMax);
        command.Parameters.AddWithValue("@maxY", boundingBox.YMax);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var feature = new Feature<Point>();
            var attributes = new Dictionary<string, object?>();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                var columnName = reader.GetName(i);

                if (columnName.Equals(geometryColumn.ColumnName, StringComparison.OrdinalIgnoreCase))
                {
                    if (!reader.IsDBNull(i))
                    {
                        var wkb = GetGeoPackageGeometry(reader, i);
                        if (wkb != null && wkb.Length > 0)
                        {
                            try
                            {
                                feature.TheGeometry = Geometry<Point>.FromWkb(wkb, geometryColumn.SrsId);
                            }
                            catch
                            {
                                feature.TheGeometry = Geometry<Point>.Empty/*Geometry<Point>.CreateEmpty(geometryColumn.SrsId)*/;
                            }
                        }
                    }
                }
                else
                {
                    attributes[columnName] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }
            }

            feature.Attributes = attributes;
            features.Add(feature);
        }

        return features;
    }

    /// <summary>
    /// Reads all features from a layer asynchronously
    /// </summary>
    public async Task<List<Feature<Point>>> ReadFeaturesAsync(string tableName)
    {
        await EnsureConnectionOpenAsync();

        var geometryColumn = GetGeometryColumnInfo(tableName);
        if (geometryColumn == null)
            throw new InvalidOperationException($"No geometry column found for table: {tableName}");

        var features = new List<Feature<Point>>();

        await using var command = _connection!.CreateCommand();
        command.CommandText = $"SELECT * FROM {tableName}";

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var feature = new Feature<Point>();

            var attributes = new Dictionary<string, object?>();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var columnName = reader.GetName(i);

                if (columnName.Equals(geometryColumn.ColumnName, StringComparison.OrdinalIgnoreCase))
                {
                    if (!reader.IsDBNull(i))
                    {
                        var wkb = GetGeoPackageGeometry(reader, i);
                        if (wkb != null && wkb.Length > 0)
                        {
                            try
                            {
                                feature.TheGeometry = Geometry<Point>.FromWkb(wkb, geometryColumn.SrsId);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Trace.WriteLine($"Error parsing geometry: {ex.Message}");
                                feature.TheGeometry = Geometry<Point>.Empty;
                            }
                        }
                    }
                }
                else
                {
                    attributes[columnName] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }
            }

            feature.Attributes = attributes;
            features.Add(feature);
        }

        return features;
    }

    /// <summary>
    /// Reads features that intersect with the given bounding box asynchronously
    /// </summary>
    public async Task<List<Feature<Point>>> ReadFeaturesAsync(string tableName, BoundingBox boundingBox)
    {
        await EnsureConnectionOpenAsync();

        var geometryColumn = GetGeometryColumnInfo(tableName);
        if (geometryColumn == null)
            throw new InvalidOperationException($"No geometry column found for table: {tableName}");

        var features = new List<Feature<Point>>();

        await using var command = _connection!.CreateCommand();

        command.CommandText = $@"
            SELECT f.*
            FROM {tableName} f
            WHERE f.rowid IN (
                SELECT id FROM rtree_{tableName}_{geometryColumn.ColumnName}
                WHERE minx <= @maxX AND maxx >= @minX 
                  AND miny <= @maxY AND maxy >= @minY
            )";

        command.Parameters.AddWithValue("@minX", boundingBox.XMin);
        command.Parameters.AddWithValue("@minY", boundingBox.YMin);
        command.Parameters.AddWithValue("@maxX", boundingBox.XMax);
        command.Parameters.AddWithValue("@maxY", boundingBox.YMax);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var feature = new Feature<Point>();
            var attributes = new Dictionary<string, object?>();

            for (var i = 0; i < reader.FieldCount; i++)
            {
                var columnName = reader.GetName(i);

                if (columnName.Equals(geometryColumn.ColumnName, StringComparison.OrdinalIgnoreCase))
                {
                    if (!reader.IsDBNull(i))
                    {
                        var wkb = GetGeoPackageGeometry(reader, i);
                        if (wkb != null && wkb.Length > 0)
                        {
                            try
                            {
                                feature.TheGeometry = Geometry<Point>.FromWkb(wkb, geometryColumn.SrsId);
                            }
                            catch
                            {
                                feature.TheGeometry = Geometry<Point>.Empty;
                            }
                        }
                    }
                }
                else
                {
                    attributes[columnName] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }
            }

            feature.Attributes = attributes;
            features.Add(feature);
        }

        return features;
    }

    private Task EnsureConnectionOpenAsync()
    {
        if (_connection == null)
            throw new InvalidOperationException("Connection not opened. Call Open() or OpenAsync() first.");
        if (_connection.State == System.Data.ConnectionState.Open)
            return Task.CompletedTask;
        return _connection.OpenAsync();
    }

    /// <summary>
    /// Gets the number of features in a layer
    /// </summary>
    public long GetFeatureCount(string tableName)
    {
        EnsureConnectionOpen();

        using var command = _connection!.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {tableName}";

        var result = command.ExecuteScalar();
        return result != null ? Convert.ToInt64(result) : 0;
    }

    /// <summary>
    /// Gets all Spatial Reference Systems defined in the GeoPackage
    /// </summary>
    public List<GpkgSpatialReferenceSystem> GetSpatialReferenceSystems()
    {
        EnsureConnectionOpen();

        var srsList = new List<GpkgSpatialReferenceSystem>();

        using var command = _connection!.CreateCommand();
        command.CommandText = @"
            SELECT srs_id, organization, organization_coordsys_id, definition, description
            FROM gpkg_spatial_ref_sys";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            srsList.Add(new GpkgSpatialReferenceSystem
            {
                SrsId = reader.GetInt32(0),
                Organization = reader.IsDBNull(1) ? null : reader.GetString(1),
                OrganizationCoordsysId = reader.GetInt32(2),
                Definition = reader.IsDBNull(3) ? null : reader.GetString(3),
                Description = reader.IsDBNull(4) ? null : reader.GetString(4)
            });
        }

        return srsList;
    }

    /// <summary>
    /// Gets column names for a table
    /// </summary>
    private List<string> GetColumnNames(string tableName)
    {
        EnsureConnectionOpen();

        var columns = new List<string>();

        using var command = _connection!.CreateCommand();
        command.CommandText = $"PRAGMA table_info({tableName})";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            columns.Add(reader.GetString(1)); // Column name is at index 1
        }

        return columns;
    }

    /// <summary>
    /// Extracts WKB from GeoPackage binary geometry format
    /// GeoPackage stores geometries with a header, we need to skip it to get standard WKB
    /// </summary>
    private byte[]? GetGeoPackageGeometry(SqliteDataReader reader, int columnIndex)
    {
        if (reader.IsDBNull(columnIndex))
            return null;

        // GeoPackage geometry format has a header before the WKB
        // Header structure: 'GP' (2 bytes) + version (1 byte) + flags (1 byte) + SRID (4 bytes) + envelope + WKB
        var gpkgGeometry = (byte[])reader.GetValue(columnIndex);

        if (gpkgGeometry.Length < 8)
            return null;

        // Check for GeoPackage magic number 'GP' (0x47, 0x50)
        if (gpkgGeometry[0] != 0x47 || gpkgGeometry[1] != 0x50)
        {
            // Not a GeoPackage geometry, might be standard WKB
            return gpkgGeometry;
        }

        // Parse flags to determine envelope size
        byte flags = gpkgGeometry[3];
        int envelopeType = (flags >> 1) & 0x07; // Bits 1-3

        int headerSize = 8; // GP + version + flags + SRID

        // Add envelope size based on type
        switch (envelopeType)
        {
            case 0: // No envelope
                headerSize += 0;
                break;
            case 1: // XY envelope
                headerSize += 32; // 4 doubles
                break;
            case 2: // XYZ envelope
                headerSize += 48; // 6 doubles
                break;
            case 3: // XYM envelope
                headerSize += 48; // 6 doubles
                break;
            case 4: // XYZM envelope
                headerSize += 64; // 8 doubles
                break;
            default:
                headerSize += 0;
                break;
        }

        // Extract WKB (skip header)
        if (gpkgGeometry.Length <= headerSize)
            return null;

        var wkb = new byte[gpkgGeometry.Length - headerSize];
        Array.Copy(gpkgGeometry, headerSize, wkb, 0, wkb.Length);

        return wkb;
    }

    /// <summary>
    /// Validates that the database is a valid GeoPackage
    /// </summary>
    public bool ValidateSchema()
    {
        EnsureConnectionOpen();

        try
        {
            using var command = _connection!.CreateCommand();
            command.CommandText = @"
                SELECT COUNT(*) FROM sqlite_master 
                WHERE type='table' AND name IN ('gpkg_contents', 'gpkg_geometry_columns', 'gpkg_spatial_ref_sys')";

            var result = command.ExecuteScalar();
            return result != null && Convert.ToInt32(result) >= 3;
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

    ~GpkgVectorReader()
    {
        Dispose();
    }
}

