using System.Text.Json.Serialization;

namespace IRI.Maptor.Core.Spatial.GeoJsonFormat;

/// <summary>
/// Represents a GeoJSON Coordinate Reference System (CRS) object.
/// Note: CRS support is deprecated in RFC 7946. GeoJSON should always use WGS84.
/// </summary>
public class GeoJsonCrs
{
    /// <summary>
    /// Gets or sets the type of the CRS object.
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the properties of the CRS object.
    /// </summary>
    [JsonPropertyName("properties")]
    public GeoJsonProperties? Properties { get; set; }
}