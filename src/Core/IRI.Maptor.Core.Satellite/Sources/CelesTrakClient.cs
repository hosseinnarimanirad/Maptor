using System.Net.Http;

namespace IRI.Maptor.Core.Satellite.Sources;

/// <summary>What to ask CelesTrak for: a named group, or one satellite by catalog number.</summary>
public sealed class CelesTrakQuery
{
    private CelesTrakQuery(string parameter, string value)
    {
        Parameter = parameter;
        Value = value;
    }

    /// <summary>A CelesTrak group, e.g. "stations", "resource", "weather", "gps-ops", "starlink".</summary>
    public static CelesTrakQuery Group(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Any(c => !(char.IsLetterOrDigit(c) || c == '-' || c == '_')))
            throw new ArgumentException("A group name is letters, digits, '-' and '_'.", nameof(name));

        return new CelesTrakQuery("GROUP", name.Trim().ToLowerInvariant());
    }

    public static CelesTrakQuery CatalogNumber(int number)
    {
        if (number <= 0)
            throw new ArgumentOutOfRangeException(nameof(number), "A catalog number is positive.");

        return new CelesTrakQuery("CATNR", number.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    public string Parameter { get; }

    public string Value { get; }

    /// <summary>A name that is safe as a file name and unique per query, e.g. "group-stations", "catnr-25544".</summary>
    public string Key => (Parameter == "GROUP" ? "group-" : "catnr-") + Value;

    public override string ToString() => Key;
}

/// <summary>
/// Downloads general-perturbations (GP) element sets from CelesTrak (https://celestrak.org) as OMM JSON.
/// </summary>
/// <remarks>
/// CelesTrak asks users not to download the same data more than once every two hours: its data only
/// changes that often, and repeated requests can get an address blocked. <see cref="ElementStore"/>
/// enforces that rule and should normally be used instead of this class directly.
/// </remarks>
public sealed class CelesTrakClient
{
    public const string DefaultBaseAddress = "https://celestrak.org";

    private readonly HttpClient _http;

    private readonly string _baseAddress;

    /// <param name="http">The client to use; a new one when null. Pass one with a handler to test without a network.</param>
    /// <param name="baseAddress">CelesTrak's address; a mirror can be given instead.</param>
    public CelesTrakClient(HttpClient? http = null, string baseAddress = DefaultBaseAddress)
    {
        _http = http ?? new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

        _baseAddress = baseAddress.TrimEnd('/');
    }

    public Uri GetAddress(CelesTrakQuery query)
        => new Uri($"{_baseAddress}/NORAD/elements/gp.php?{query.Parameter}={Uri.EscapeDataString(query.Value)}&FORMAT=JSON");

    /// <summary>The raw OMM JSON text. CelesTrak answers an unknown group or number with a short plain-text message, which the reader then reports.</summary>
    public async Task<string> DownloadAsync(CelesTrakQuery query, CancellationToken cancellationToken = default)
    {
        if (query is null)
            throw new ArgumentNullException(nameof(query));

        using var response = await _http.GetAsync(GetAddress(query), cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
    }
}
