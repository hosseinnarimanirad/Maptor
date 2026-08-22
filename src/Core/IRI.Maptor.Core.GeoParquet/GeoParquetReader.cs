using System.Text.Json;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.IO.OgcSFA;
using IRI.Maptor.Core.Spatial.Primitives;
using Parquet;
using Parquet.Data;
using Parquet.Schema;
using System.Runtime.CompilerServices;

namespace IRI.Maptor.Core.GeoParquet;

/// <summary>
/// Reader for GeoParquet files
/// </summary>
public static class GeoParquetReader
{ 
    /// <summary>
    /// Reads features from a GeoParquet file
    /// </summary>
    public static async IAsyncEnumerable<Feature<Point>> ReadFeaturesAsync(string filePath, GeoParquetOptions? options = null)
    {
        options ??= new GeoParquetOptions();

        using var fileStream = File.OpenRead(filePath);
        using var parquetReader = await ParquetReader.CreateAsync(fileStream);

        // Read GeoParquet metadata
        var metadata = ReadMetadata(parquetReader);
        var geometryColumnName = metadata.PrimaryColumn ?? options.GeometryColumnName;

        // Get schema
        var schema = parquetReader.Schema;

        // Read data
        var dataFields = schema.GetDataFields();
        var geometryField = dataFields.FirstOrDefault(f => f.Name == geometryColumnName);
        
        if (geometryField == null)
        {
            throw new InvalidOperationException($"Geometry column '{geometryColumnName}' not found in Parquet file");
        }

        // Determine SRID from metadata
        var srid = GetSridFromMetadata(metadata, geometryColumnName, options.DefaultSrid);

        // Read all row groups
        for (int i = 0; i < parquetReader.RowGroupCount; i++)
        {
            using var rowGroupReader = parquetReader.OpenRowGroupReader(i);
            
            // Read geometry column
            var geometryData = await rowGroupReader.ReadColumnAsync(geometryField);
            var geometryValues = geometryData.Data as byte[][];

            // Read attribute columns
            var attributeColumns = new Dictionary<string, DataColumn>();
            foreach (var field in dataFields)
            {
                if (field.Name != geometryColumnName)
                {
                    attributeColumns[field.Name] = await rowGroupReader.ReadColumnAsync(field);
                }
            }

            // Convert to features
            var rowCount = geometryValues?.Length ?? 0;
            for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                var geometryBytes = geometryValues?[rowIndex];
                if (geometryBytes == null || geometryBytes.Length == 0)
                    continue;

                var geometry = WkbReader.Parse(geometryBytes, srid) as Geometry<Point>;
                if (geometry == null)
                    continue;

                // Build attributes dictionary
                var attributes = new Dictionary<string, object>();
                foreach (var kvp in attributeColumns)
                {
                    var column = kvp.Value;
                    var value = GetValueFromColumn(column, rowIndex);
                    if (value != null)
                    {
                        attributes[kvp.Key] = value;
                    }
                }

                yield return new Feature<Point>(geometry, attributes);
            }
        }
    }

    /// <summary>
    /// Reads features from a GeoParquet file (synchronous wrapper)
    /// </summary>
    public static IEnumerable<Feature<Point>> ReadFeatures(string filePath, GeoParquetOptions? options = null)
    {
        return ReadFeaturesAsync(filePath, options).ToBlockingEnumerable();
    }

    private static IEnumerable<T> ToBlockingEnumerable<T>(this IAsyncEnumerable<T> source)
    {
        var enumerator = source.GetAsyncEnumerator();
        try
        {
            while (enumerator.MoveNextAsync().AsTask().Result)
            {
                yield return enumerator.Current;
            }
        }
        finally
        {
            enumerator.DisposeAsync().AsTask().Wait();
        }
    }

    /// <summary>
    /// Reads a FeatureSet from a GeoParquet file
    /// </summary>
    public static FeatureSet<Point> ReadFeatureSet(string filePath, GeoParquetOptions? options = null)
    {
        var features = ReadFeatures(filePath, options).ToList();
        
        if (features.Count == 0)
        {
            return FeatureSet<Point>.Empty;
        }

        var srid = features[0].TheGeometry.Srid;
        var featureSet = FeatureSet<Point>.Create(string.Empty, features);
        featureSet.Srid = srid;

        return featureSet;
    }

    private static GeoParquetMetadata ReadMetadata(ParquetReader reader)
    {
        var customMetadata = reader.CustomMetadata;
        
        if (!customMetadata.TryGetValue("geo", out var geoMetadataJson))
        {
            throw new InvalidOperationException("GeoParquet metadata ('geo' key) not found in Parquet file");
        }

        var metadata = JsonSerializer.Deserialize<GeoParquetMetadata>(geoMetadataJson);
        if (metadata == null)
        {
            throw new InvalidOperationException("Failed to parse GeoParquet metadata");
        }

        return metadata;
    }

    private static int GetSridFromMetadata(GeoParquetMetadata metadata, string geometryColumnName, int defaultSrid)
    {
        if (metadata.Columns.TryGetValue(geometryColumnName, out var columnMetadata))
        {
            if (!string.IsNullOrEmpty(columnMetadata.Crs))
            {
                // Parse EPSG:4326 format
                if (columnMetadata.Crs.StartsWith("EPSG:", StringComparison.OrdinalIgnoreCase))
                {
                    var epsgCode = columnMetadata.Crs.Substring(5);
                    if (int.TryParse(epsgCode, out var srid))
                    {
                        return srid;
                    }
                }
            }
        }

        return defaultSrid;
    }

    private static object? GetValueFromColumn(DataColumn column, int rowIndex)
    {
        if (rowIndex >= column.Data.Length)
            return null;

        var value = column.Data.GetValue(rowIndex);
        
        // Handle DBNull
        if (value == DBNull.Value)
            return null;

        return value;
    }
}
