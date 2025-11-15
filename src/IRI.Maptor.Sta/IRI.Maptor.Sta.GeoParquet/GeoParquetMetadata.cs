using System.Text.Json.Serialization;

namespace IRI.Maptor.Sta.GeoParquet;

/// <summary>
/// Represents GeoParquet metadata structure according to GeoParquet 1.1 specification
/// </summary>
public class GeoParquetMetadata
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.1.0";

    [JsonPropertyName("primary_column")]
    public string PrimaryColumn { get; set; } = "geometry";

    [JsonPropertyName("columns")]
    public Dictionary<string, GeometryColumnMetadata> Columns { get; set; } = new();

    public static GeoParquetMetadata Create(string primaryColumn, int srid, string[]? geometryTypes = null, double[]? bbox = null)
    {
        var metadata = new GeoParquetMetadata
        {
            PrimaryColumn = primaryColumn
        };

        var crs = srid > 0 ? $"EPSG:{srid}" : null;

        metadata.Columns[primaryColumn] = new GeometryColumnMetadata
        {
            Encoding = "WKB",
            GeometryTypes = geometryTypes ?? Array.Empty<string>(),
            Crs = crs,
            Bbox = bbox
        };

        return metadata;
    }
}

/// <summary>
/// Metadata for a geometry column in GeoParquet
/// </summary>
public class GeometryColumnMetadata
{
    [JsonPropertyName("encoding")]
    public string Encoding { get; set; } = "WKB";

    [JsonPropertyName("geometry_types")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? GeometryTypes { get; set; }

    [JsonPropertyName("crs")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Crs { get; set; }

    [JsonPropertyName("bbox")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double[]? Bbox { get; set; }

    [JsonPropertyName("edges")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Edges { get; set; }

    [JsonPropertyName("orientation")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Orientation { get; set; }
}

