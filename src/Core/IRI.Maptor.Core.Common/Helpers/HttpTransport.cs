using IRI.Maptor.Core.Common.Services;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace IRI.Maptor.Core.Common.Helpers;

public static class HttpTransport
{
    private const string ContentTypeJson = "application/json";
    private const string ContentTypeXml = "text/xml";
    private const string AcceptJson = "application/json";
    private const string AcceptXml = "text/xml";
    private const string AcceptAll = "*/*";
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(100);
    private static readonly JsonSerializerOptions ErrorJsonOptions = new() { PropertyNameCaseInsensitive = true };

    private sealed class ApiErrorEnvelope
    {
        public ApiErrorPayload? Error { get; set; }
    }

    private sealed class ApiErrorPayload
    {
        public string? ResourceKey { get; set; }
        public string? Code { get; set; }
    }

    public static Task<Response<T>> GetAsync<T>(
        string address,
        Encoding? encoding = null,
        WebProxy? proxy = null,
        string? bearer = null,
        string? contentType = ContentTypeJson,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default) where T : class
    {
        _ = encoding;
        _ = contentType;

        return SendAsync<T>(HttpMethod.Get, address, null, proxy, bearer, headers, DefaultTimeout, AcceptJson, cancellationToken);
    }

    public static Response<T> Get<T>(
        string address,
        Encoding? encoding = null,
        WebProxy? proxy = null,
        string? bearer = null,
        string? contentType = ContentTypeJson,
        Dictionary<string, string>? headers = null) where T : class
    {
        return GetAsync<T>(address, encoding, proxy, bearer, contentType, headers).GetAwaiter().GetResult();
    }

    public static async Task<Response<T>> PostAsync<T>(
        string address,
        object? data,
        Encoding? encoding = null,
        WebProxy? proxy = null,
        string? bearer = null,
        string? contentType = ContentTypeJson,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default) where T : class
    {
        var content = CreateJsonContent(data, encoding, contentType);
        return await SendAsync<T>(HttpMethod.Post, address, content, proxy, bearer, headers, DefaultTimeout, AcceptJson, cancellationToken);
    }

    public static async Task<Response<T>> PostAsync<T>(
        HttpClient client,
        string address,
        object? data,
        CancellationToken cancellationToken = default) where T : class
    {
        if (client == null)
        {
            return ResponseFactory.CreateError<T>("HttpClient cannot be null.");
        }

        using var content = CreateJsonContent(data, Encoding.UTF8, ContentTypeJson);
        return await SendWithClientAsync<T>(client, HttpMethod.Post, address, content, cancellationToken);
    }

    /// <summary>
    /// GET on a caller-provided (typically long-lived, shared) client, so connections are pooled and
    /// reused instead of paying a new TCP+TLS handshake per request as the address-based overload does.
    /// Bearer/headers, when given, are set on the request message only — the shared client's defaults
    /// are never mutated, and a null bearer leaves the client's default Authorization in effect.
    /// </summary>
    public static async Task<Response<T>> GetAsync<T>(
        HttpClient client,
        string address,
        string? bearer = null,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default) where T : class
    {
        if (client == null)
        {
            return ResponseFactory.CreateError<T>("HttpClient cannot be null.");
        }

        return await SendWithClientAsync<T>(client, HttpMethod.Get, address, null, cancellationToken, bearer, headers);
    }

    /// <summary>
    /// PUT on a caller-provided shared client. See the client-based <see cref="GetAsync{T}(HttpClient, string, string?, Dictionary{string, string}?, CancellationToken)"/> for semantics.
    /// </summary>
    public static async Task<Response<T>> PutAsync<T>(
        HttpClient client,
        string address,
        object? data,
        string? bearer = null,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default) where T : class
    {
        if (client == null)
        {
            return ResponseFactory.CreateError<T>("HttpClient cannot be null.");
        }

        using var content = CreateJsonContent(data, Encoding.UTF8, ContentTypeJson);
        return await SendWithClientAsync<T>(client, HttpMethod.Put, address, content, cancellationToken, bearer, headers);
    }

    public static async Task<Response<T>> PutAsync<T>(
        string address,
        object? data,
        Encoding? encoding = null,
        WebProxy? proxy = null,
        string? bearer = null,
        string? contentType = ContentTypeJson,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default) where T : class
    {
        var content = CreateJsonContent(data, encoding, contentType);
        return await SendAsync<T>(HttpMethod.Put, address, content, proxy, bearer, headers, DefaultTimeout, AcceptJson, cancellationToken);
    }

    public static async Task<Response<string>> PostXmlAsync(
        string address,
        string xmlData,
        Encoding? encoding = null,
        WebProxy? proxy = null,
        string? bearer = null,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(xmlData))
        {
            return ResponseFactory.CreateError<string>("XML data cannot be null or empty.");
        }

        using var content = new StringContent(xmlData, encoding ?? Encoding.UTF8, ContentTypeXml);
        return await SendAsync<string>(HttpMethod.Post, address, content, proxy, bearer, headers, DefaultTimeout, AcceptXml, cancellationToken);
    }

    public static Task<Response<bool>> DeleteAsync(
        string address,
        WebProxy? proxy = null,
        string? bearer = null,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<bool>(HttpMethod.Delete, address, null, proxy, bearer, headers, DefaultTimeout, AcceptAll, cancellationToken);
    }

    public static async Task<Response<string>> GetStringAsync(
        string address,
        WebProxy? proxy = null,
        string? bearer = null,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        return await SendAsync<string>(HttpMethod.Get, address, null, proxy, bearer, headers, DefaultTimeout, AcceptAll, cancellationToken);
    }

    private static async Task<Response<T>> SendAsync<T>(
        HttpMethod method,
        string address,
        HttpContent? content,
        WebProxy? proxy,
        string? bearer,
        Dictionary<string, string>? headers,
        TimeSpan timeout,
        string acceptHeader,
        CancellationToken cancellationToken)
    {
        if (!TryValidateAbsoluteUri(address, out var uri, out var error))
        {
            return ResponseFactory.CreateError<T>(error!);
        }

        using var client = CreateHttpClient(proxy, bearer, headers, timeout, acceptHeader);
        using var request = new HttpRequestMessage(method, uri) { Content = content };

        return await SendCoreAsync<T>(client, request, cancellationToken);
    }

    private static async Task<Response<T>> SendWithClientAsync<T>(
        HttpClient client,
        HttpMethod method,
        string address,
        HttpContent? content,
        CancellationToken cancellationToken,
        string? bearer = null,
        Dictionary<string, string>? headers = null)
    {
        if (!TryBuildUri(client, address, out var uri, out var error))
        {
            return ResponseFactory.CreateError<T>(error!);
        }

        using var request = CreateClientRequest(method, uri!, content, bearer, headers);

        return await SendCoreAsync<T>(client, request, cancellationToken);
    }

    /// <summary>
    /// GET on a caller-provided shared client for large JSON responses: the body is deserialized
    /// straight from the response stream instead of first being read into one string, so a
    /// 100 MB feature set is never held twice in memory. Results and errors are the same as the
    /// client-based <see cref="GetAsync{T}(HttpClient, string, string?, Dictionary{string, string}?, CancellationToken)"/>.
    /// Only the time to the response headers counts towards HttpClient.Timeout; reading the body
    /// is bounded by <paramref name="cancellationToken"/>.
    /// </summary>
    public static Task<Response<T>> GetJsonStreamAsync<T>(
        HttpClient client,
        string address,
        string? bearer = null,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default) where T : class
    {
        return GetStreamAsync(
            client,
            address,
            accept: null,
            (_, stream, token) => JsonSerializer.DeserializeAsync<T>(stream, JsonHelper.DefaultOptions, token).AsTask(),
            bearer,
            headers,
            cancellationToken);
    }

    /// <summary>
    /// As <see cref="GetJsonStreamAsync{T}"/>, but sends <paramref name="accept"/> (when given) and lets
    /// <paramref name="read"/> choose how to parse the body from the response's media type, so a client
    /// can ask for an optional format and still read a server that answers with plain JSON.
    /// </summary>
    public static Task<Response<T>> GetStreamAsync<T>(
        HttpClient client,
        string address,
        string? accept,
        Func<string?, Stream, CancellationToken, Task<T?>> read,
        string? bearer = null,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default) where T : class
    {
        return SendStreamAsync(client, HttpMethod.Get, address, null, accept, read, bearer, headers, cancellationToken);
    }

    /// <summary>
    /// POST of <paramref name="body"/> as JSON on a caller-provided shared client, with the answer read from the
    /// response stream as in <see cref="GetStreamAsync{T}"/>. For queries whose parameters do not fit in a url
    /// (filters, geometries), such as a layer query.
    /// </summary>
    public static Task<Response<T>> PostJsonStreamAsync<T>(
        HttpClient client,
        string address,
        object body,
        Func<string?, Stream, CancellationToken, Task<T?>> read,
        string? bearer = null,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default) where T : class
    {
        var content = CreateJsonContent(body, Encoding.UTF8, ContentTypeJson);

        return SendStreamAsync(client, HttpMethod.Post, address, content, AcceptJson, read, bearer, headers, cancellationToken);
    }

    private static async Task<Response<T>> SendStreamAsync<T>(
        HttpClient client,
        HttpMethod method,
        string address,
        HttpContent? content,
        string? accept,
        Func<string?, Stream, CancellationToken, Task<T?>> read,
        string? bearer,
        Dictionary<string, string>? headers,
        CancellationToken cancellationToken) where T : class
    {
        if (client == null)
        {
            return ResponseFactory.CreateError<T>("HttpClient cannot be null.");
        }

        if (!TryBuildUri(client, address, out var uri, out var error))
        {
            return ResponseFactory.CreateError<T>(error!);
        }

        using var request = CreateClientRequest(method, uri!, content, bearer, headers);

        if (!string.IsNullOrWhiteSpace(accept))
        {
            request.Headers.TryAddWithoutValidation("Accept", accept);
        }

        try
        {
            var stopwatch = Stopwatch.StartNew();

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            var headersElapsed = stopwatch.ElapsedMilliseconds;

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                return BuildErrorResponse<T>(response.StatusCode, response.ReasonPhrase, errorBody);
            }

            var result = Response<T>.Create(isSuccess: true, statusCode: (int)response.StatusCode);

            // An empty body is a null result, as in BuildSuccessResponse.
            if (response.Content.Headers.ContentLength == 0)
            {
                result.Result = default!;
                return result;
            }

            var mediaType = response.Content.Headers.ContentType?.MediaType;

            await using var stream = await response.Content.ReadAsStreamAsync();

            result.Result = (await read(mediaType, stream, cancellationToken))!;

            Trace.TraceInformation(
                $"{method} {uri}: headers after {headersElapsed} ms, body downloaded and parsed after {stopwatch.ElapsedMilliseconds} ms " +
                $"(type {mediaType}, Content-Length {response.Content.Headers.ContentLength?.ToString() ?? "unknown"}, encoding {string.Join(",", response.Content.Headers.ContentEncoding)})");

            return result;
        }
        catch (TaskCanceledException ex)
        {
            var message = ex.CancellationToken.IsCancellationRequested
                ? "Request was cancelled."
                : $"Request timed out: {ex.Message}";
            return ResponseFactory.CreateError<T>(message);
        }
        catch (HttpRequestException ex)
        {
            return ResponseFactory.CreateError<T>($"HTTP request failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ResponseFactory.CreateError<T>(ex.Message);
        }
    }

    /// <summary>
    /// Per-request headers only — a shared client's defaults must never be mutated. A request-level
    /// Authorization overrides the client default; a null bearer leaves the client default in effect.
    /// </summary>
    private static HttpRequestMessage CreateClientRequest(
        HttpMethod method,
        Uri uri,
        HttpContent? content,
        string? bearer,
        Dictionary<string, string>? headers)
    {
        var request = new HttpRequestMessage(method, uri) { Content = content };

        if (!string.IsNullOrWhiteSpace(bearer))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer.Trim());
        }

        if (headers != null)
        {
            foreach (var header in headers)
            {
                if (string.IsNullOrWhiteSpace(header.Key))
                {
                    continue;
                }

                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return request;
    }

    private static async Task<Response<T>> SendCoreAsync<T>(HttpClient client, HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return BuildErrorResponse<T>(response.StatusCode, response.ReasonPhrase, responseBody);
            }

            return BuildSuccessResponse<T>(response.StatusCode, responseBody);
        }
        catch (TaskCanceledException ex)
        {
            var message = ex.CancellationToken.IsCancellationRequested
                ? "Request was cancelled."
                : $"Request timed out: {ex.Message}";
            return ResponseFactory.CreateError<T>(message);
        }
        catch (HttpRequestException ex)
        {
            return ResponseFactory.CreateError<T>($"HTTP request failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ResponseFactory.CreateError<T>(ex.Message);
        }
    }

    private static Response<T> BuildSuccessResponse<T>(HttpStatusCode statusCode, string responseBody)
    {
        var result = Response<T>.Create(isSuccess: true, statusCode: (int)statusCode);

        if (typeof(T) == typeof(string))
        {
            result.Result = (T)(object)responseBody;
            return result;
        }

        if (typeof(T) == typeof(bool))
        {
            result.Result = (T)(object)true;
            return result;
        }

        if (string.IsNullOrWhiteSpace(responseBody))
        {
            result.Result = default!;
            return result;
        }

        result.Result = JsonHelper.Deserialize<T>(responseBody);
        return result;
    }

    private static Response<T> BuildErrorResponse<T>(HttpStatusCode statusCode, string? reasonPhrase, string? responseBody)
    {
        var result = Response<T>.Create(isSuccess: false, statusCode: (int)statusCode);
        result.Error = BuildProblemDetails(statusCode, reasonPhrase, responseBody);
        return result;
    }

    private static ProblemDetails BuildProblemDetails(HttpStatusCode statusCode, string? reasonPhrase, string? responseBody)
    {
        var parsed = TryParseProblemDetails(responseBody);
        if (parsed != null)
        {
            parsed.Status ??= (int)statusCode;
            parsed.Title ??= GetDefaultTitle(statusCode, reasonPhrase);
            parsed.Detail ??= GetDefaultDetail(statusCode);
            parsed.Type ??= $"https://httpstatuses.com/{(int)statusCode}";
            return parsed;
        }

        var detail = GetDefaultDetail(statusCode);
        if (!string.IsNullOrWhiteSpace(responseBody))
        {
            var truncatedBody = responseBody.Length > 500 ? responseBody[..500] + "..." : responseBody;
            detail = $"{detail} Response: {truncatedBody}";
        }

        return new ProblemDetails
        {
            Type = $"https://httpstatuses.com/{(int)statusCode}",
            Title = GetDefaultTitle(statusCode, reasonPhrase),
            Detail = detail,
            Status = (int)statusCode
        };
    }

    private static ProblemDetails? TryParseProblemDetails(string? responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return null;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<ProblemDetails>(responseBody, ErrorJsonOptions);
            if (parsed != null &&
                (!string.IsNullOrWhiteSpace(parsed.Title) ||
                 !string.IsNullOrWhiteSpace(parsed.Detail) ||
                 parsed.Status.HasValue))
            {
                return parsed;
            }
        }
        catch
        {
        }

        try
        {
            var apiError = JsonSerializer.Deserialize<ApiErrorEnvelope>(responseBody, ErrorJsonOptions);
            if (!string.IsNullOrWhiteSpace(apiError?.Error?.ResourceKey) || !string.IsNullOrWhiteSpace(apiError?.Error?.Code))
            {
                return new ProblemDetails
                {
                    Title = apiError?.Error?.Code,
                    Detail = apiError?.Error?.ResourceKey
                };
            }
        }
        catch
        {
        }

        return null;
    }

    private static string GetDefaultTitle(HttpStatusCode statusCode, string? reasonPhrase)
    {
        return statusCode switch
        {
            HttpStatusCode.Unauthorized => "Unauthorized",
            HttpStatusCode.Forbidden => "Forbidden",
            _ => string.IsNullOrWhiteSpace(reasonPhrase) ? "Request failed" : reasonPhrase
        };
    }

    private static string GetDefaultDetail(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.Unauthorized => "Authentication is required to access this resource.",
            HttpStatusCode.Forbidden => "You do not have permission to perform this action.",
            _ => $"HTTP {(int)statusCode} request failed."
        };
    }

    private static HttpClient CreateHttpClient(
        WebProxy? proxy,
        string? bearer,
        Dictionary<string, string>? headers,
        TimeSpan timeout,
        string acceptHeader)
    {
        HttpClientHandler? handler = null;
        if (proxy?.Address != null)
        {
            handler = new HttpClientHandler { Proxy = proxy, UseProxy = true };
        }

        var client = handler != null ? new HttpClient(handler) : new HttpClient();
        client.Timeout = timeout;
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("application", "1.0"));

        if (!string.IsNullOrWhiteSpace(acceptHeader))
        {
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(acceptHeader));
        }

        if (!string.IsNullOrWhiteSpace(bearer))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer.Trim());
        }

        if (headers != null)
        {
            foreach (var header in headers)
            {
                if (string.IsNullOrWhiteSpace(header.Key))
                {
                    continue;
                }

                if (string.Equals(header.Key, "Authorization", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.Equals(header.Key, "Accept", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!client.DefaultRequestHeaders.Contains(header.Key))
                {
                    client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
                }
            }
        }

        return client;
    }

    private static StringContent CreateJsonContent(object? data, Encoding? encoding, string? contentType)
    {
        var json = JsonHelper.SerializeWithIgnoreNullOption(data ?? new { });
        return new StringContent(json, encoding ?? Encoding.UTF8, string.IsNullOrWhiteSpace(contentType) ? ContentTypeJson : contentType);
    }

    private static bool TryValidateAbsoluteUri(string address, out Uri? uri, out string? error)
    {
        error = null;
        uri = null;

        if (string.IsNullOrWhiteSpace(address))
        {
            error = "Address cannot be null or empty.";
            return false;
        }

        if (!Uri.TryCreate(address, UriKind.Absolute, out var parsed))
        {
            error = $"Address '{address}' is not a valid absolute URI.";
            return false;
        }

        if (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps)
        {
            error = $"URI scheme must be http or https, but was '{parsed.Scheme}'.";
            return false;
        }

        uri = parsed;
        return true;
    }

    private static bool TryBuildUri(HttpClient client, string address, out Uri? uri, out string? error)
    {
        error = null;
        uri = null;

        if (string.IsNullOrWhiteSpace(address))
        {
            error = "Address cannot be null or empty.";
            return false;
        }

        if (Uri.TryCreate(address, UriKind.Absolute, out var absolute))
        {
            uri = absolute;
            return true;
        }

        if (client.BaseAddress == null)
        {
            error = $"Relative address '{address}' cannot be used without HttpClient.BaseAddress.";
            return false;
        }

        uri = new Uri(client.BaseAddress, address);
        return true;
    }
}
