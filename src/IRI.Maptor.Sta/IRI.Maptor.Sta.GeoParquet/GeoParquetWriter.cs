using System.Text.Json;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using Parquet;
using Parquet.Data;
using Parquet.Schema;
using Field = Parquet.Schema.Field;
using CompressionMethod = Parquet.CompressionMethod;

namespace IRI.Maptor.Sta.GeoParquet;

/// <summary>
/// Writer for GeoParquet files
/// </summary>
public static class GeoParquetWriter
{
    /// <summary>
    /// Writes features to a GeoParquet file
    /// </summary>
    public static async Task WriteFeaturesAsync(string filePath, IEnumerable<Feature<Point>> features, GeoParquetOptions? options = null)
    {
        options ??= new GeoParquetOptions();

        var featuresList = features.ToList();
        if (featuresList.Count == 0)
        {
            throw new ArgumentException("Cannot write empty feature collection", nameof(features));
        }

        // Determine geometry types and extent
        var geometryTypes = GetGeometryTypes(featuresList);
        var bbox = options.IncludeBbox ? GetBoundingBox(featuresList) : null;
        var srid = featuresList[0].TheGeometry.Srid;

        // Create GeoParquet metadata
        var metadata = GeoParquetMetadata.Create(
            options.GeometryColumnName,
            srid,
            options.IncludeGeometryTypes ? geometryTypes : null,
            bbox);

        // Build schema
        var schema = BuildSchema(featuresList, options.GeometryColumnName);

        // Create GeoParquet metadata JSON
        var metadataJson = JsonSerializer.Serialize(metadata);

        // Write file
        using var fileStream = File.Create(filePath);
        using var parquetWriter = await ParquetWriter.CreateAsync(schema, fileStream);

        // Set compression method
        parquetWriter.CompressionMethod = options.CompressionMethod;

        // Add GeoParquet metadata
        parquetWriter.CustomMetadata = new Dictionary<string, string>
        {
            ["geo"] = metadataJson
        };

        // Write data
        using var rowGroup = parquetWriter.CreateRowGroup();

        // Write geometry column
        var geometryField = schema.GetDataFields().First(f => f.Name == options.GeometryColumnName);
        var geometryData = featuresList.Select(f => f.TheGeometry.AsWkb() ?? Array.Empty<byte>()).ToArray();
        await rowGroup.WriteColumnAsync(new DataColumn(geometryField, geometryData));

        // Write attribute columns
        var attributeFields = schema.GetDataFields().Where(f => f.Name != options.GeometryColumnName).ToList();
        if (attributeFields.Count > 0)
        {
            var attributeData = GetAttributeData(featuresList, attributeFields);
            foreach (var field in attributeFields)
            {
                var data = attributeData[field.Name];
                await rowGroup.WriteColumnAsync(new DataColumn(field, data));
            }
        }
    }

    /// <summary>
    /// Writes a FeatureSet to a GeoParquet file
    /// </summary>
    public static async Task WriteFeatureSetAsync(string filePath, FeatureSet<Point> featureSet, GeoParquetOptions? options = null)
    {
        await WriteFeaturesAsync(filePath, featureSet.Features, options);
    }

    /// <summary>
    /// Writes features to a GeoParquet file (synchronous wrapper)
    /// </summary>
    public static void WriteFeatures(string filePath, IEnumerable<Feature<Point>> features, GeoParquetOptions? options = null)
    {
        WriteFeaturesAsync(filePath, features, options).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Writes a FeatureSet to a GeoParquet file (synchronous wrapper)
    /// </summary>
    public static void WriteFeatureSet(string filePath, FeatureSet<Point> featureSet, GeoParquetOptions? options = null)
    {
        WriteFeatureSetAsync(filePath, featureSet, options).GetAwaiter().GetResult();
    }

    private static ParquetSchema BuildSchema(List<Feature<Point>> features, string geometryColumnName)
    {
        var fields = new List<Field>();

        // Add geometry column (binary/WKB)
        fields.Add(new DataField<byte[]>(geometryColumnName));

        // Add attribute columns
        if (features.Count > 0 && features[0].Attributes != null)
        {
            var allAttributeKeys = features
                .SelectMany(f => f.Attributes?.Keys ?? Enumerable.Empty<string>())
                .Distinct()
                .ToList();

            foreach (var key in allAttributeKeys)
            {
                var sampleValue = features
                    .Select(f => f.Attributes?.GetValueOrDefault(key))
                    .FirstOrDefault(v => v != null);

                if (sampleValue != null)
                {
                    var field = CreateFieldFromValue(key, sampleValue);
                    if (field != null)
                    {
                        fields.Add(field);
                    }
                }
            }
        }

        return new ParquetSchema(fields);
    }

    private static Field? CreateFieldFromValue(string name, object value)
    {
        return value switch
        {
            int => new DataField<int>(name),
            long => new DataField<long>(name),
            float => new DataField<float>(name),
            double => new DataField<double>(name),
            bool => new DataField<bool>(name),
            string => new DataField<string>(name),
            DateTime => new DataField<DateTime>(name),
            _ => new DataField<string>(name) // Default to string for unknown types
        };
    }

    private static Dictionary<string, Array> GetAttributeData(List<Feature<Point>> features, List<DataField> fields)
    {
        var result = new Dictionary<string, Array>();

        foreach (var field in fields)
        {
            Array data;
            
            if (field.ClrType == typeof(int))
            {
                data = features.Select(f =>
                {
                    var value = f.Attributes?.GetValueOrDefault(field.Name);
                    return value is int i ? i : (value != null && int.TryParse(value.ToString(), out var parsed) ? parsed : default(int));
                }).ToArray();
            }
            else if (field.ClrType == typeof(long))
            {
                data = features.Select(f =>
                {
                    var value = f.Attributes?.GetValueOrDefault(field.Name);
                    return value is long l ? l : (value != null && long.TryParse(value.ToString(), out var parsed) ? parsed : default(long));
                }).ToArray();
            }
            else if (field.ClrType == typeof(float))
            {
                data = features.Select(f =>
                {
                    var value = f.Attributes?.GetValueOrDefault(field.Name);
                    return value is float fl ? fl : (value != null && float.TryParse(value.ToString(), out var parsed) ? parsed : default(float));
                }).ToArray();
            }
            else if (field.ClrType == typeof(double))
            {
                data = features.Select(f =>
                {
                    var value = f.Attributes?.GetValueOrDefault(field.Name);
                    return value is double d ? d : (value != null && double.TryParse(value.ToString(), out var parsed) ? parsed : default(double));
                }).ToArray();
            }
            else if (field.ClrType == typeof(bool))
            {
                data = features.Select(f =>
                {
                    var value = f.Attributes?.GetValueOrDefault(field.Name);
                    return value is bool b ? b : (value != null && bool.TryParse(value.ToString(), out var parsed) ? parsed : default(bool));
                }).ToArray();
            }
            else if (field.ClrType == typeof(DateTime))
            {
                data = features.Select(f =>
                {
                    var value = f.Attributes?.GetValueOrDefault(field.Name);
                    return value is DateTime dt ? dt : default(DateTime);
                }).ToArray();
            }
            else // string or other
            {
                data = features.Select(f =>
                {
                    var value = f.Attributes?.GetValueOrDefault(field.Name);
                    return value?.ToString() ?? string.Empty;
                }).ToArray();
            }

            result[field.Name] = data;
        }

        return result;
    }

    private static object? GetDefaultValue(DataField field)
    {
        return field.ClrType switch
        {
            Type t when t == typeof(int) => default(int),
            Type t when t == typeof(long) => default(long),
            Type t when t == typeof(float) => default(float),
            Type t when t == typeof(double) => default(double),
            Type t when t == typeof(bool) => default(bool),
            Type t when t == typeof(DateTime) => default(DateTime),
            _ => null
        };
    }

    private static string[] GetGeometryTypes(List<Feature<Point>> features)
    {
        return features
            .Select(f => f.GeometryType/*TheGeometry.Type*/.ToString())
            .Distinct()
            .ToArray();
    }

    private static double[]? GetBoundingBox(List<Feature<Point>> features)
    {
        if (features.Count == 0)
            return null;

        var bbox = features[0].TheGeometry.GetBoundingBox();
        if (bbox.IsNaN())
            return null;

        foreach (var feature in features.Skip(1))
        {
            var featureBbox = feature.TheGeometry.GetBoundingBox();
            if (!featureBbox.IsNaN())
            {
                bbox = BoundingBox.GetMergedBoundingBox(new[] { bbox, featureBbox });
            }
        }

        return new[] { bbox.XMin, bbox.YMin, bbox.XMax, bbox.YMax };
    }
}

