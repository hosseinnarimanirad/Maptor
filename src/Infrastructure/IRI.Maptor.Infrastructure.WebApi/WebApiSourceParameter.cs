using IRI.Maptor.Core.SpatialReferenceSystem;

namespace IRI.Maptor.Infrastructure.WebApi;

public class WebApiSourceParameter
{
    /// <summary>
    /// Optional shared <see cref="System.Net.Http.HttpClient"/> for all requests of this source.
    /// When set, connections are pooled and reused across layers (one TLS handshake per pooled
    /// connection instead of one per request) and the client's handler policies (TLS versions,
    /// certificate validation, proxy, connection limits) apply. When null, each request builds a
    /// throwaway client (legacy behavior). With a shared client, a null <see cref="BearerToken"/>
    /// means the client's default Authorization header is used.
    /// </summary>
    public HttpClient? HttpClient { get; set; }

    public string? BearerToken { get; set; }

    public Dictionary<string, string>? Headers { get; set; }

    public int Srid { get; set; } = SridHelper.WebMercator;

    public string? IdColumnName { get; set; }

    /// <summary>
    /// When set, used instead of "LIST" as the list endpoint path. The full URL becomes BaseUrl + "/" + CustomListPath.TrimStart('/').
    /// Use for APIs where the list endpoint is a custom path (e.g. "/Substation/ListSubstat").
    /// </summary>
    public string ListUrl { get; set; }

    public string SyncUrl { get; set; }

    public WebApiSourceParameter(
        string listUrl,
        string syncUrl,
        string? bearerToken = null,
        Dictionary<string, string>? headers = null,
        int srid = SridHelper.WebMercator,
        string? idColumnName = null)
    {
        ListUrl = listUrl;
        SyncUrl = syncUrl;

        BearerToken = bearerToken;
        Headers = headers;
        Srid = srid;
        IdColumnName = idColumnName;
    }
}
