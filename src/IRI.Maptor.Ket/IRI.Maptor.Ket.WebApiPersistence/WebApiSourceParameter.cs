using IRI.Maptor.Sta.SpatialReferenceSystem;

namespace IRI.Maptor.Ket.WebApiPersistence;

public class WebApiSourceParameter
{
    public string BaseUrl { get; set; }

    public string GetFeaturesEndpoint { get; set; }

    public string UpdateFeatureEndpoint { get; set; }

    public string? BearerToken { get; set; }

    public Dictionary<string, string>? Headers { get; set; }

    public int Srid { get; set; } = SridHelper.WebMercator;

    public string? IdColumnName { get; set; }

    public WebApiSourceParameter(
        string baseUrl,
        string getFeaturesEndpoint,
        string updateFeatureEndpoint,
        string? bearerToken = null,
        Dictionary<string, string>? headers = null,
        int srid = SridHelper.WebMercator,
        string? idColumnName = null)
    {
        BaseUrl = baseUrl;
        GetFeaturesEndpoint = getFeaturesEndpoint;
        UpdateFeatureEndpoint = updateFeatureEndpoint;
        BearerToken = bearerToken;
        Headers = headers;
        Srid = srid;
        IdColumnName = idColumnName;
    }
}
