using IRI.Maptor.Sta.Common.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace IRI.Maptor.Sta.Common.Helpers;

/// <summary>
/// Helper class for making HTTP requests using HttpClient with support for proxy, bearer tokens, and custom headers.
/// </summary>
public static class HttpClientHelper
{
    private const string ContentTypeJson = "application/json";
    private const string ContentTypeXml = "text/xml";
    private const string AcceptJson = "application/json";
    private const string AcceptXml = "text/xml";
    private const string AcceptAll = "*/*";
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(100);

    private sealed class ApiErrorEnvelope
    {
        public ApiErrorPayload? Error { get; set; }
    }

    private sealed class ApiErrorPayload
    {
        public string? ResourceKey { get; set; }
        public object[]? Parameters { get; set; }
        public string? Code { get; set; }
    }

    /// <summary>
    /// Creates an HttpClient instance with the specified configuration.
    /// </summary>
    private static HttpClient CreateHttpClient(
        WebProxy? proxy = null,
        string? bearer = null,
        Dictionary<string, string>? headers = null,
        TimeSpan? timeout = null,
        string? acceptHeader = null)
    {
        HttpClientHandler? handler = null;

        if (proxy?.Address != null)
        {
            handler = new HttpClientHandler
            {
                Proxy = proxy,
                UseProxy = true
            };
        }

        var client = handler != null ? new HttpClient(handler) : new HttpClient();

        if (timeout.HasValue)
        {
            client.Timeout = timeout.Value;
        }
        else
        {
            client.Timeout = DefaultTimeout;
        }

        // Set User-Agent header
        if (!client.DefaultRequestHeaders.Contains("User-Agent"))
        {
            client.DefaultRequestHeaders.Add("User-Agent", "application!");
        }

        // Set Accept header if specified
        if (!string.IsNullOrWhiteSpace(acceptHeader) && !client.DefaultRequestHeaders.Contains("Accept"))
        {
            client.DefaultRequestHeaders.Add("Accept", acceptHeader);
        }

        // Set Authorization header if bearer token is provided
        if (!string.IsNullOrWhiteSpace(bearer))
        {
            var trimmedBearer = bearer.Trim();
            if (string.IsNullOrWhiteSpace(trimmedBearer))
            {
                throw new ArgumentException("Bearer token cannot be empty or whitespace.", nameof(bearer));
            }
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", trimmedBearer);
        }

        // Add custom headers
        if (headers != null && headers.Any())
        {
            ValidateHeaders(headers);
            foreach (var header in headers)
            {
                var headerName = header.Key?.Trim();
                if (string.IsNullOrWhiteSpace(headerName))
                {
                    throw new ArgumentException("Header name cannot be null or empty.", nameof(headers));
                }

                // Skip headers that are already set in DefaultRequestHeaders
                if (headerName.Equals("User-Agent", StringComparison.OrdinalIgnoreCase) ||
                    headerName.Equals("Accept", StringComparison.OrdinalIgnoreCase) ||
                    headerName.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!client.DefaultRequestHeaders.Contains(headerName))
                {
                    client.DefaultRequestHeaders.Add(headerName, header.Value);
                }
            }
        }

        return client;
    }

    /// <summary>
    /// Validates that header names and values are valid.
    /// </summary>
    private static void ValidateHeaders(Dictionary<string, string> headers)
    {
        foreach (var header in headers)
        {
            if (string.IsNullOrWhiteSpace(header.Key))
            {
                throw new ArgumentException("Header name cannot be null or empty.", nameof(headers));
            }

            if (header.Value == null)
            {
                throw new ArgumentException($"Header value for '{header.Key}' cannot be null.", nameof(headers));
            }
        }
    }

    /// <summary>
    /// Validates that the address is a valid URI.
    /// </summary>
    private static bool IsValidUri(string address, out string? errorMessage)
    {
        errorMessage = null;
        if (string.IsNullOrWhiteSpace(address))
        {
            errorMessage = "Address cannot be null or empty";
            return false;
        }

        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri))
        {
            errorMessage = $"Address '{address}' is not a valid URI";
            return false;
        }

        if (uri.Scheme != "http" && uri.Scheme != "https")
        {
            errorMessage = $"URI scheme must be http or https, but was '{uri.Scheme}'";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Handles HTTP response errors and creates an error Response with detailed information.
    /// </summary>
    private static async Task<Response<T>> HandleHttpErrorAsync<T>(HttpResponseMessage response, string? context = null)
    {
        var statusCode = (int)response.StatusCode;
        var statusReason = response.ReasonPhrase ?? "Unknown";
        string? responseBody = null;

        try
        {
            responseBody = await response.Content.ReadAsStringAsync();
        }
        catch
        {
            // Ignore errors reading response body
        }

        var errorMessage = $"HTTP {statusCode} {statusReason}";
        if (!string.IsNullOrWhiteSpace(context))
        {
            errorMessage = $"{context}: {errorMessage}";
        }

        if (!string.IsNullOrWhiteSpace(responseBody))
        {
            var truncatedBody = responseBody.Length > 500 ? responseBody.Substring(0, 500) + "..." : responseBody;
            errorMessage = $"{errorMessage}. Response: {truncatedBody}";
        }

        return ResponseFactory.CreateError<T>(errorMessage);
    }

    /// <summary>
    /// Handles exceptions and creates an error Response with detailed information.
    /// </summary>
    private static Response<T> HandleException<T>(Exception ex, HttpResponseMessage? response = null)
    {
        string errorMessage;

        if (ex is HttpRequestException httpEx)
        {
            errorMessage = $"HTTP request failed: {httpEx.Message}";
            if (httpEx.InnerException != null)
            {
                errorMessage += $" Inner exception: {httpEx.InnerException.Message}";
            }
        }
        else if (ex is TaskCanceledException canceledEx)
        {
            errorMessage = canceledEx.CancellationToken.IsCancellationRequested
                ? "Request was cancelled"
                : $"Request timed out: {canceledEx.Message}";
        }
        else if (ex is ArgumentException argEx)
        {
            errorMessage = $"Invalid argument: {argEx.Message}";
        }
        else
        {
            errorMessage = ex.Message;
        }

        if (response != null)
        {
            var statusCode = (int)response.StatusCode;
            errorMessage = $"HTTP {statusCode}: {errorMessage}";
        }

        return ResponseFactory.CreateError<T>(errorMessage);
    }

    #region GET Methods

    /// <summary>
    /// Performs an HTTP GET request and deserializes the JSON response to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the response to.</typeparam>
    /// <param name="address">The URI to request.</param>
    /// <param name="proxy">Optional proxy configuration.</param>
    /// <param name="bearer">Optional bearer token for authentication.</param>
    /// <param name="headers">Optional custom headers to include in the request.</param>
    /// <param name="timeout">Optional timeout for the request. Defaults to 100 seconds.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the request.</param>
    /// <returns>A Response containing the deserialized object or error information.</returns>
    public static async Task<Response<T>> HttpGetAsync<T>(
        string address,
        WebProxy? proxy = null,
        string? bearer = null,
        Dictionary<string, string>? headers = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default) where T : class
    {
        if (!IsValidUri(address, out var uriError))
        {
            return ResponseFactory.CreateError<T>(uriError!);
        }

        using var client = CreateHttpClient(proxy, bearer, headers, timeout, AcceptJson);
        try
        {
            using var response = await client.GetAsync(address, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                return await HandleHttpErrorAsync<T>(response, "GET request failed");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonHelper.Deserialize<T>(content);

            return ResponseFactory.Create(result);
        }
        catch (HttpRequestException ex)
        {
            return HandleException<T>(ex);
        }
        catch (TaskCanceledException ex)
        {
            return HandleException<T>(ex);
        }
        catch (Exception ex)
        {
            return HandleException<T>(ex);
        }
    }

    /// <summary>
    /// Performs an HTTP GET request and returns the response as a string.
    /// </summary>
    /// <param name="address">The URI to request.</param>
    /// <param name="proxy">Optional proxy configuration.</param>
    /// <param name="bearer">Optional bearer token for authentication.</param>
    /// <param name="headers">Optional custom headers to include in the request.</param>
    /// <param name="timeout">Optional timeout for the request. Defaults to 100 seconds.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the request.</param>
    /// <returns>A Response containing the response string or error information.</returns>
    public static async Task<Response<string>> HttpGetStringAsync(
        string address,
        WebProxy? proxy = null,
        string? bearer = null,
        Dictionary<string, string>? headers = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidUri(address, out var uriError))
        {
            return ResponseFactory.CreateError<string>(uriError!);
        }

        using var client = CreateHttpClient(proxy, bearer, headers, timeout, AcceptAll);
        try
        {
            using var response = await client.GetAsync(address, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                return await HandleHttpErrorAsync<string>(response, "GET request failed");
            }

            var content = await response.Content.ReadAsStringAsync();
            return ResponseFactory.Create(content);
        }
        catch (HttpRequestException ex)
        {
            return HandleException<string>(ex);
        }
        catch (TaskCanceledException ex)
        {
            return HandleException<string>(ex);
        }
        catch (Exception ex)
        {
            return HandleException<string>(ex);
        }
    }

    /// <summary>
    /// Performs an HTTP GET request and returns the response as a byte array.
    /// </summary>
    /// <param name="address">The URI to request.</param>
    /// <param name="proxy">Optional proxy configuration.</param>
    /// <param name="bearer">Optional bearer token for authentication.</param>
    /// <param name="headers">Optional custom headers to include in the request.</param>
    /// <param name="timeout">Optional timeout for the request. Defaults to 100 seconds.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the request.</param>
    /// <returns>A Response containing the response bytes or error information.</returns>
    public static async Task<Response<byte[]>> HttpGetBytesAsync(
        string address,
        WebProxy? proxy = null,
        string? bearer = null,
        Dictionary<string, string>? headers = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidUri(address, out var uriError))
        {
            return ResponseFactory.CreateError<byte[]>(uriError!);
        }

        using var client = CreateHttpClient(proxy, bearer, headers, timeout, AcceptAll);
        try
        {
            using var response = await client.GetAsync(address, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                return await HandleHttpErrorAsync<byte[]>(response, "GET request failed");
            }

            var content = await response.Content.ReadAsByteArrayAsync();
            return ResponseFactory.Create(content);
        }
        catch (HttpRequestException ex)
        {
            return HandleException<byte[]>(ex);
        }
        catch (TaskCanceledException ex)
        {
            return HandleException<byte[]>(ex);
        }
        catch (Exception ex)
        {
            return HandleException<byte[]>(ex);
        }
    }

    #endregion

    #region POST Methods

    /// <summary>
    /// Performs an HTTP POST request with JSON data and deserializes the JSON response to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the response to.</typeparam>
    /// <param name="address">The URI to request.</param>
    /// <param name="data">The data object to serialize and send.</param>
    /// <param name="proxy">Optional proxy configuration.</param>
    /// <param name="bearer">Optional bearer token for authentication.</param>
    /// <param name="encoding">Optional encoding for the request content. Defaults to UTF-8.</param>
    /// <param name="contentType">Optional content type. Defaults to application/json.</param>
    /// <param name="headers">Optional custom headers to include in the request.</param>
    /// <param name="timeout">Optional timeout for the request. Defaults to 100 seconds.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the request.</param>
    /// <returns>A Response containing the deserialized object or error information.</returns>
    public static async Task<Response<T>> HttpPostAsync<T>(
        string address,
        object data,
        WebProxy? proxy = null,
        string? bearer = null,
        Encoding? encoding = null,
        string? contentType = null,
        Dictionary<string, string>? headers = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default) where T : class
    {
        if (!IsValidUri(address, out var uriError))
        {
            return ResponseFactory.CreateError<T>(uriError!);
        }

        if (data == null)
        {
            return ResponseFactory.CreateError<T>("Data cannot be null");
        }

        using var client = CreateHttpClient(proxy, bearer, headers, timeout, AcceptJson);
        try
        {
            encoding ??= Encoding.UTF8;
            contentType ??= ContentTypeJson;

            var json = JsonHelper.SerializeWithIgnoreNullOption(data);
            using var content = new StringContent(json, encoding, contentType);

            using var response = await client.PostAsync(address, content, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                return await HandleHttpErrorAsync<T>(response, "POST request failed");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonHelper.Deserialize<T>(responseContent);

            return ResponseFactory.Create(result);
        }
        catch (HttpRequestException ex)
        {
            return HandleException<T>(ex);
        }
        catch (TaskCanceledException ex)
        {
            return HandleException<T>(ex);
        }
        catch (Exception ex)
        {
            return HandleException<T>(ex);
        }
    }

    /// <summary>
    /// Performs an HTTP POST request with JSON data and returns the response as a string.
    /// </summary>
    /// <param name="address">The URI to request.</param>
    /// <param name="data">The data object to serialize and send.</param>
    /// <param name="proxy">Optional proxy configuration.</param>
    /// <param name="bearer">Optional bearer token for authentication.</param>
    /// <param name="encoding">Optional encoding for the request content. Defaults to UTF-8.</param>
    /// <param name="contentType">Optional content type. Defaults to application/json.</param>
    /// <param name="headers">Optional custom headers to include in the request.</param>
    /// <param name="timeout">Optional timeout for the request. Defaults to 100 seconds.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the request.</param>
    /// <returns>A Response containing the response string or error information.</returns>
    public static async Task<Response<string>> HttpPostStringAsync(
        string address,
        object data,
        WebProxy? proxy = null,
        string? bearer = null,
        Encoding? encoding = null,
        string? contentType = null,
        Dictionary<string, string>? headers = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidUri(address, out var uriError))
        {
            return ResponseFactory.CreateError<string>(uriError!);
        }

        if (data == null)
        {
            return ResponseFactory.CreateError<string>("Data cannot be null");
        }

        using var client = CreateHttpClient(proxy, bearer, headers, timeout, AcceptAll);
        try
        {
            encoding ??= Encoding.UTF8;
            contentType ??= ContentTypeJson;

            var json = JsonHelper.SerializeWithIgnoreNullOption(data);
            using var content = new StringContent(json, encoding, contentType);

            using var response = await client.PostAsync(address, content, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                return await HandleHttpErrorAsync<string>(response, "POST request failed");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            return ResponseFactory.Create(responseContent);
        }
        catch (HttpRequestException ex)
        {
            return HandleException<string>(ex);
        }
        catch (TaskCanceledException ex)
        {
            return HandleException<string>(ex);
        }
        catch (Exception ex)
        {
            return HandleException<string>(ex);
        }
    }

    /// <summary>
    /// Performs an HTTP POST request with XML data and returns the response as a string.
    /// </summary>
    /// <param name="address">The URI to request.</param>
    /// <param name="xmlData">The XML string to send.</param>
    /// <param name="proxy">Optional proxy configuration.</param>
    /// <param name="bearer">Optional bearer token for authentication.</param>
    /// <param name="encoding">Optional encoding for the request content. Defaults to UTF-8.</param>
    /// <param name="headers">Optional custom headers to include in the request.</param>
    /// <param name="timeout">Optional timeout for the request. Defaults to 100 seconds.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the request.</param>
    /// <returns>A Response containing the response string or error information.</returns>
    public static async Task<Response<string>> HttpPostXmlAsync(
        string address,
        string xmlData,
        WebProxy? proxy = null,
        string? bearer = null,
        Encoding? encoding = null,
        Dictionary<string, string>? headers = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidUri(address, out var uriError))
        {
            return ResponseFactory.CreateError<string>(uriError!);
        }

        if (string.IsNullOrWhiteSpace(xmlData))
        {
            return ResponseFactory.CreateError<string>("XML data cannot be null or empty");
        }

        using var client = CreateHttpClient(proxy, bearer, headers, timeout, AcceptXml);
        try
        {
            encoding ??= Encoding.UTF8;
            using var content = new StringContent(xmlData, encoding, ContentTypeXml);

            using var response = await client.PostAsync(address, content, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                return await HandleHttpErrorAsync<string>(response, "POST XML request failed");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            return ResponseFactory.Create(responseContent);
        }
        catch (HttpRequestException ex)
        {
            return HandleException<string>(ex);
        }
        catch (TaskCanceledException ex)
        {
            return HandleException<string>(ex);
        }
        catch (Exception ex)
        {
            return HandleException<string>(ex);
        }
    }

    #endregion

    #region PUT Methods

    /// <summary>
    /// Performs an HTTP PUT request with JSON data and deserializes the JSON response to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the response to.</typeparam>
    /// <param name="address">The URI to request.</param>
    /// <param name="data">The data object to serialize and send.</param>
    /// <param name="proxy">Optional proxy configuration.</param>
    /// <param name="bearer">Optional bearer token for authentication.</param>
    /// <param name="encoding">Optional encoding for the request content. Defaults to UTF-8.</param>
    /// <param name="contentType">Optional content type. Defaults to application/json.</param>
    /// <param name="headers">Optional custom headers to include in the request.</param>
    /// <param name="timeout">Optional timeout for the request. Defaults to 100 seconds.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the request.</param>
    /// <returns>A Response containing the deserialized object or error information.</returns>
    public static async Task<Response<T>> HttpPutAsync<T>(
        string address,
        object data,
        WebProxy? proxy = null,
        string? bearer = null,
        Encoding? encoding = null,
        string? contentType = null,
        Dictionary<string, string>? headers = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default) where T : class
    {
        if (!IsValidUri(address, out var uriError))
        {
            return ResponseFactory.CreateError<T>(uriError!);
        }

        if (data == null)
        {
            return ResponseFactory.CreateError<T>("Data cannot be null");
        }

        using var client = CreateHttpClient(proxy, bearer, headers, timeout, AcceptJson);

        try
        {
            encoding ??= Encoding.UTF8;
            contentType ??= ContentTypeJson;

            var json = JsonHelper.SerializeWithIgnoreNullOption(data);
            using var content = new StringContent(json, encoding, contentType);

            using var response = await client.PutAsync(address, content, cancellationToken);

            var result = Response<T>.Create(isSuccess: response.IsSuccessStatusCode, statusCode: (int)response.StatusCode);
            //new Response<T>
            //{
            //    StatusCode = (int)response.StatusCode,
            //    IsSuccess = response.IsSuccessStatusCode
            //};

            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                result.Result = JsonHelper.Deserialize<T>(responseContent);
            }
            else
            {
                try
                {
                    result.Error = JsonSerializer.Deserialize<ProblemDetails>(responseContent);
                    if (result.Error != null && string.IsNullOrWhiteSpace(result.Error.Detail))
                    {
                        var apiError = JsonSerializer.Deserialize<ApiErrorEnvelope>(
                            responseContent,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (!string.IsNullOrWhiteSpace(apiError?.Error?.ResourceKey))
                        {
                            result.Error = new ProblemDetails
                            {
                                Title = apiError.Error.Code ?? "Request failed",
                                Detail = apiError.Error.ResourceKey,
                                Status = (int)response.StatusCode
                            };
                        }
                    }
                }
                catch
                {
                    // Fallback: create a simple ProblemDetails
                    result.Error = new ProblemDetails
                    {
                        Title = "Request failed",
                        Detail = responseContent,
                        Status = (int)response.StatusCode
                    };
                }
            }

            //return await HandleHttpErrorAsync<T>(response, "PUT request failed");

            //return ResponseFactory.Create(result);
            return result;
        }
        catch (HttpRequestException ex)
        {
            return HandleException<T>(ex);
        }
        catch (TaskCanceledException ex)
        {
            return HandleException<T>(ex);
        }
        catch (Exception ex)
        {
            return HandleException<T>(ex);
        }
    }

    /// <summary>
    /// Performs an HTTP PUT request with JSON data and returns the response as a string.
    /// </summary>
    /// <param name="address">The URI to request.</param>
    /// <param name="data">The data object to serialize and send.</param>
    /// <param name="proxy">Optional proxy configuration.</param>
    /// <param name="bearer">Optional bearer token for authentication.</param>
    /// <param name="encoding">Optional encoding for the request content. Defaults to UTF-8.</param>
    /// <param name="contentType">Optional content type. Defaults to application/json.</param>
    /// <param name="headers">Optional custom headers to include in the request.</param>
    /// <param name="timeout">Optional timeout for the request. Defaults to 100 seconds.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the request.</param>
    /// <returns>A Response containing the response string or error information.</returns>
    public static async Task<Response<string>> HttpPutStringAsync(
        string address,
        object data,
        WebProxy? proxy = null,
        string? bearer = null,
        Encoding? encoding = null,
        string? contentType = null,
        Dictionary<string, string>? headers = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidUri(address, out var uriError))
        {
            return ResponseFactory.CreateError<string>(uriError!);
        }

        if (data == null)
        {
            return ResponseFactory.CreateError<string>("Data cannot be null");
        }

        using var client = CreateHttpClient(proxy, bearer, headers, timeout, AcceptAll);
        try
        {
            encoding ??= Encoding.UTF8;
            contentType ??= ContentTypeJson;

            var json = JsonHelper.SerializeWithIgnoreNullOption(data);
            using var content = new StringContent(json, encoding, contentType);

            using var response = await client.PutAsync(address, content, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                return await HandleHttpErrorAsync<string>(response, "PUT request failed");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            return ResponseFactory.Create(responseContent);
        }
        catch (HttpRequestException ex)
        {
            return HandleException<string>(ex);
        }
        catch (TaskCanceledException ex)
        {
            return HandleException<string>(ex);
        }
        catch (Exception ex)
        {
            return HandleException<string>(ex);
        }
    }

    #endregion

    #region DELETE Method

    /// <summary>
    /// Performs an HTTP DELETE request.
    /// </summary>
    /// <param name="address">The URI to request.</param>
    /// <param name="proxy">Optional proxy configuration.</param>
    /// <param name="bearer">Optional bearer token for authentication.</param>
    /// <param name="headers">Optional custom headers to include in the request.</param>
    /// <param name="timeout">Optional timeout for the request. Defaults to 100 seconds.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the request.</param>
    /// <returns>A Response containing true if successful or error information.</returns>
    public static async Task<Response<bool>> HttpDeleteAsync(
        string address,
        WebProxy? proxy = null,
        string? bearer = null,
        Dictionary<string, string>? headers = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidUri(address, out var uriError))
        {
            return ResponseFactory.CreateError<bool>(uriError!);
        }

        using var client = CreateHttpClient(proxy, bearer, headers, timeout);
        try
        {
            using var response = await client.DeleteAsync(address, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                return await HandleHttpErrorAsync<bool>(response, "DELETE request failed");
            }

            return ResponseFactory.Create(true);
        }
        catch (HttpRequestException ex)
        {
            return HandleException<bool>(ex);
        }
        catch (TaskCanceledException ex)
        {
            return HandleException<bool>(ex);
        }
        catch (Exception ex)
        {
            return HandleException<bool>(ex);
        }
    }

    #endregion
}
