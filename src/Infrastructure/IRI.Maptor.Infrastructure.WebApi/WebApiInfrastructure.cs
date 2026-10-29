using IRI.Maptor.Core.Common.Helpers;
using IRI.Maptor.Core.Common.Services;
using IRI.Maptor.Core.Spatial.Dtos;
using System.Text.Json;

namespace IRI.Maptor.Infrastructure.WebApi;

public static class WebApiInfrastructure
{
    /// <summary>
    /// Gets features from the API endpoint with optional query parameters. The full response is
    /// returned so callers can distinguish a genuinely empty result from a failed request. When
    /// <paramref name="httpClient"/> is provided, its pooled connections are reused (see
    /// <see cref="WebApiSourceParameter.HttpClient"/>); otherwise a throwaway client is built per call.
    /// </summary>
    public static async Task<Response<FeatureSetDto>> GetFeaturesAsync(
        //string baseUrl,
        string endpoint,
        ListFeaturesQueryParams? queryParams = null,
        string? bearerToken = null,
        Dictionary<string, string>? headers = null,
        HttpClient? httpClient = null,
        CancellationToken cancellationToken = default,
        bool preferCompactFormat = false)
    {
        var url = BuildUrl(/*baseUrl, */endpoint, queryParams);

        if (httpClient != null && preferCompactFormat)
        {
            return await HttpTransport.GetStreamAsync(
                httpClient,
                url,
                CompactAccept,
                ReadFeatureSetAsync,
                bearer: bearerToken,
                headers: headers,
                cancellationToken: cancellationToken);
        }

        // Feature sets are the largest responses: stream them instead of reading one big string.
        if (httpClient != null)
        {
            return await HttpTransport.GetJsonStreamAsync<FeatureSetDto>(
                httpClient,
                url,
                bearer: bearerToken,
                headers: headers,
                cancellationToken: cancellationToken);
        }

        return await HttpTransport.GetAsync<FeatureSetDto>(
            url,
            bearer: bearerToken,
            headers: headers,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Sends a unit-of-work DTO (added, updated, deleted) to the update endpoint.
    /// </summary>
    public static async Task<Response<SyncResultDto>> SaveChangesAsync(
        string endpoint,
        FeatureSetChangesDto dto,
        string? bearerToken = null,
        Dictionary<string, string>? headers = null,
        HttpClient? httpClient = null)
    {
        if (httpClient != null)
        {
            return await HttpTransport.PutAsync<SyncResultDto>(
                httpClient,
                endpoint,
                dto,
                bearer: bearerToken,
                headers: headers);
        }

        return await HttpTransport.PutAsync<SyncResultDto>(
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

        var response = await HttpTransport.PutAsync<object>(
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

        var response = await HttpTransport.PostAsync<FeatureDto>(
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

        var response = await HttpTransport.DeleteAsync(
            url,
            bearer: bearerToken,
            headers: headers);

        return response.HasNotNullResult() && response.Result;
    }

    /// <summary>
    /// Builds a URL with query string from ListFeaturesQueryParams.
    /// </summary>
    /// <summary>
    /// Runs a layer query (<c>POST /LayerQuery/{layerId}</c>): a page of the attribute table, or every feature
    /// of a map extent. Needs a shared client; the answer is read from the response stream.
    /// </summary>
    public static Task<Response<FeaturePageDto>> QueryFeaturesAsync(
        HttpClient httpClient,
        string queryUrl,
        FeatureQueryDto query,
        string? bearerToken = null,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        return HttpTransport.PostJsonStreamAsync(
            httpClient,
            queryUrl,
            query,
            (_, stream, token) => JsonSerializer.DeserializeAsync<FeaturePageDto>(stream, JsonHelper.DefaultOptions, token).AsTask(),
            bearerToken,
            headers,
            cancellationToken);
    }

    /// <summary>The compact form first, plain JSON as the fallback a server that does not know it answers with.</summary>
    private const string CompactAccept = CompactFeatureSetDto.MediaType + ", application/json;q=0.9";

    /// <summary>Reads whichever form the server chose; both give the same <see cref="FeatureSetDto"/>.</summary>
    private static async Task<FeatureSetDto?> ReadFeatureSetAsync(string? mediaType, Stream stream, CancellationToken cancellationToken)
    {
        if (string.Equals(mediaType, CompactFeatureSetDto.MediaType, StringComparison.OrdinalIgnoreCase))
        {
            var compact = await JsonSerializer.DeserializeAsync<CompactFeatureSetDto>(stream, JsonHelper.DefaultOptions, cancellationToken);

            return compact?.ToFeatureSetDto();
        }

        return await JsonSerializer.DeserializeAsync<FeatureSetDto>(stream, JsonHelper.DefaultOptions, cancellationToken);
    }

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