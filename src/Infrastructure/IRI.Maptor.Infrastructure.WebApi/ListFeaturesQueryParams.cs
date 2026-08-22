namespace IRI.Maptor.Infrastructure.WebApi;

/// <summary>
/// Explicit query parameters for the list/get-features endpoint. Used instead of a dictionary.
/// </summary>
public class ListFeaturesQueryParams
{
    /// <summary>
    /// Optional WKB geometry as hex string for spatial filter.
    /// </summary>
    public string? GeometryWkbHex { get; set; }

    /// <summary>
    /// Optional search text for text filter.
    /// </summary>
    public string? SearchText { get; set; }
}
