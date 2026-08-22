using System.Text.Json.Serialization;

namespace IRI.Maptor.Core.Spatial.GeoJsonFormat;

/// <summary>
/// Represents properties for a GeoJSON CRS object.
/// </summary>
public class GeoJsonProperties
{
    /// <summary>
    /// Gets or sets the name of the CRS.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}