using IRI.Maptor.Sta.SpatialReferenceSystem;

namespace IRI.Maptor.Ket.WebApiPersistence;

public class WebApiSourceParameter
{
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
