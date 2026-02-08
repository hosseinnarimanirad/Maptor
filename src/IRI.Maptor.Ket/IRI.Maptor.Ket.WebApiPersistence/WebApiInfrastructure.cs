using IRI.Maptor.Ket.WebApiPersistence.DTOs;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Common.Services;

namespace IRI.Maptor.Ket.WebApiPersistence;

public static class WebApiInfrastructure
{
    /// <summary>
    /// Gets features from the API endpoint with optional query parameters
    /// </summary>
    public static async Task<FeatureSetDto?> GetFeaturesAsync(
        string baseUrl,
        string endpoint,
        Dictionary<string, string>? queryParameters = null,
        string? bearerToken = null,
        Dictionary<string, string>? headers = null)
    {
        var url = BuildUrl(baseUrl, endpoint, queryParameters);

        var response = await HttpClientHelper.HttpGetAsync<FeatureSetDto>(
            url,
            bearer: bearerToken,
            headers: headers);

        return response.HasNotNullResult() ? response.Result : null;
    }

    /// <summary>
    /// Updates a feature via PUT request
    /// </summary>
    public static async Task<bool> UpdateFeatureAsync(
        string baseUrl,
        string endpoint,
        int featureId,
        FeatureDto featureDto,
        string? bearerToken = null,
        Dictionary<string, string>? headers = null)
    {
        var url = $"{baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}/{featureId}";

        var response = await HttpClientHelper.HttpPutAsync<object>(
            url,
            featureDto,
            bearer: bearerToken,
            headers: headers);

        return response.HasNotNullResult();
    }

    /// <summary>
    /// Adds a new feature via POST request
    /// </summary>
    public static async Task<FeatureDto?> AddFeatureAsync(
        string baseUrl,
        string endpoint,
        FeatureDto featureDto,
        string? bearerToken = null,
        Dictionary<string, string>? headers = null)
    {
        var url = BuildUrl(baseUrl, endpoint);

        var response = await HttpClientHelper.HttpPostAsync<FeatureDto>(
            url,
            featureDto,
            bearer: bearerToken,
            headers: headers);

        return response.HasNotNullResult() ? response.Result : null;
    }

    /// <summary>
    /// Deletes a feature via DELETE request
    /// </summary>
    public static async Task<bool> DeleteFeatureAsync(
        string baseUrl,
        string endpoint,
        int featureId,
        string? bearerToken = null,
        Dictionary<string, string>? headers = null)
    {
        var url = $"{baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}/{featureId}";

        var response = await HttpClientHelper.HttpDeleteAsync(
            url,
            bearer: bearerToken,
            headers: headers);

        return response.HasNotNullResult() && response.Result;
    }

    /// <summary>
    /// Builds a URL with query parameters
    /// </summary>
    private static string BuildUrl(string baseUrl, string endpoint, Dictionary<string, string>? queryParameters = null)
    {
        var url = $"{baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";

        if (queryParameters != null && queryParameters.Count > 0)
        {
            var queryString = string.Join("&", queryParameters.Select(kvp =>
                $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value ?? string.Empty)}"));
            url += $"?{queryString}";
        }

        return url;
    }
}