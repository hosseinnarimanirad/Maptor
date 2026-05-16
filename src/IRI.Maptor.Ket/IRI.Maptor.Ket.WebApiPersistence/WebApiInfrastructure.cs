using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Common.Services;
using IRI.Maptor.Sta.Spatial.Dtos;

namespace IRI.Maptor.Ket.WebApiPersistence;

public static class WebApiInfrastructure
{
    /// <summary>
    /// Gets features from the API endpoint with optional query parameters.
    /// </summary>
    public static async Task<FeatureSetDto?> GetFeaturesAsync(
        //string baseUrl,
        string endpoint,
        ListFeaturesQueryParams? queryParams = null,
        string? bearerToken = null,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        var url = BuildUrl(/*baseUrl, */endpoint, queryParams);

        var response = await HttpClientHelper.HttpGetAsync<FeatureSetDto>(
            url,
            bearer: bearerToken,
            headers: headers,
            cancellationToken: cancellationToken);

        return response.HasNotNullResult() ? response.Result : null;
    }

    /// <summary>
    /// Sends a unit-of-work DTO (added, updated, deleted) to the update endpoint.
    /// </summary>
    public static async Task<Response<SyncResultDto>> SaveChangesAsync(
        string endpoint,
        FeatureSetChangesDto dto,
        string? bearerToken = null,
        Dictionary<string, string>? headers = null)
    {
        //var response = await HttpClientHelper.HttpPutAsync<SyncResultDto>(
        //    endpoint,
        //    dto,
        //    bearer: bearerToken,
        //    headers: headers);

        //return response.HasNotNullResult() ? response.Result : null;
        return await HttpClientHelper.HttpPutAsync<SyncResultDto>(
            endpoint,
            dto,
            bearer: bearerToken,
            headers: headers);
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
        //string baseUrl,
        string endpoint,
        FeatureDto featureDto,
        string? bearerToken = null,
        Dictionary<string, string>? headers = null)
    {
        var url = BuildUrl(/*baseUrl,*/ endpoint);

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
    /// Builds a URL with query string from ListFeaturesQueryParams.
    /// </summary>
    private static string BuildUrl(/*string baseUrl, */string endpoint, ListFeaturesQueryParams? queryParams = null)
    {
        //var url = $"{baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";

        if (queryParams == null)
            return endpoint;

        var parts = new List<string>();
        if (!string.IsNullOrEmpty(queryParams.GeometryWkbHex))
            parts.Add($"geometry={Uri.EscapeDataString(queryParams.GeometryWkbHex)}");
        if (!string.IsNullOrEmpty(queryParams.SearchText))
            parts.Add($"search={Uri.EscapeDataString(queryParams.SearchText)}");

        if (parts.Count > 0)
            endpoint += "?" + string.Join("&", parts);

        return endpoint;
    }
}