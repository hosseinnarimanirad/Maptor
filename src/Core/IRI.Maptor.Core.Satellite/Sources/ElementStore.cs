using System.Globalization;
using System.Text.Json;
using IRI.Maptor.Core.Satellite.Elements;

namespace IRI.Maptor.Core.Satellite.Sources;

/// <summary>Where a set of elements came from.</summary>
public enum ElementSource
{
    /// <summary>Freshly downloaded from CelesTrak.</summary>
    Downloaded,

    /// <summary>The local copy, still fresh, or too recent to download again.</summary>
    Cache,

    /// <summary>The download failed (offline, server error), so the last local copy is used.</summary>
    CacheAfterFailedDownload,

    /// <summary>Read from a file the user imported, e.g. on a network without internet access.</summary>
    Import,

    /// <summary>Nothing could be obtained.</summary>
    None,
}

/// <summary>The outcome of asking an <see cref="ElementStore"/> for elements.</summary>
public sealed class ElementSetResult
{
    internal ElementSetResult(ElementSource source, IReadOnlyList<OrbitalElements> elements, IReadOnlyList<string> errors, DateTime? retrievedUtc, string message)
    {
        Source = source;
        Elements = elements;
        Errors = errors;
        RetrievedUtc = retrievedUtc;
        Message = message;
    }

    public ElementSource Source { get; }

    public IReadOnlyList<OrbitalElements> Elements { get; }

    /// <summary>Element sets that could not be read, and why the download failed when it did.</summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>When the data was downloaded or imported.</summary>
    public DateTime? RetrievedUtc { get; }

    /// <summary>A sentence for the user, e.g. "Cached copy from 2026-10-28 09:12 UTC (offline: …)".</summary>
    public string Message { get; }
}

/// <summary>
/// Orbital elements from CelesTrak, kept in a local folder so they are available offline and
/// downloaded no more often than CelesTrak allows.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>A copy younger than <see cref="MinimumDownloadInterval"/> (2 hours, CelesTrak's rule) is never
/// downloaded again, even when a refresh is forced.</item>
/// <item>A copy younger than <see cref="FreshFor"/> (12 hours) is used without asking.</item>
/// <item>When a download fails, the last copy is used and the result says so, with its age.</item>
/// <item><see cref="Import"/> reads a TLE or OMM file into the store, for networks without internet
/// access; imports are kept under their own name and never expire.</item>
/// </list>
/// Each copy is one JSON or TLE file plus a small "&lt;name&gt;.meta.json" with the time it was retrieved.
/// </remarks>
public sealed class ElementStore
{
    private readonly string _folder;

    private readonly CelesTrakClient _client;

    private readonly Func<DateTime> _utcNow;

    public ElementStore(string folder, CelesTrakClient? client = null, Func<DateTime>? utcNow = null)
    {
        if (string.IsNullOrWhiteSpace(folder))
            throw new ArgumentException("A cache folder is required.", nameof(folder));

        _folder = folder;
        _client = client ?? new CelesTrakClient();
        _utcNow = utcNow ?? (() => DateTime.UtcNow);
    }

    public TimeSpan MinimumDownloadInterval { get; set; } = TimeSpan.FromHours(2);

    public TimeSpan FreshFor { get; set; } = TimeSpan.FromHours(12);

    public string Folder => _folder;

    /// <param name="query">The group or catalog number.</param>
    /// <param name="cancellationToken">Cancels the download; a cancelled call throws instead of falling back to the copy.</param>
    /// <param name="forceRefresh">Download even if the copy is still fresh, unless it is younger than <see cref="MinimumDownloadInterval"/>.</param>
    public async Task<ElementSetResult> GetAsync(CelesTrakQuery query, bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        if (query is null)
            throw new ArgumentNullException(nameof(query));

        var now = _utcNow();

        var cached = ReadCopy(query.Key);

        if (cached is not null)
        {
            var age = now - cached.Value.RetrievedUtc;

            if (age < MinimumDownloadInterval)
                return FromText(ElementSource.Cache, cached.Value.Text, cached.Value.RetrievedUtc,
                    $"Cached copy from {Format(cached.Value.RetrievedUtc)} — CelesTrak asks for at most one download every {MinimumDownloadInterval.TotalHours:0} hours.");

            if (age < FreshFor && !forceRefresh)
                return FromText(ElementSource.Cache, cached.Value.Text, cached.Value.RetrievedUtc, $"Cached copy from {Format(cached.Value.RetrievedUtc)}.");
        }

        string text;

        try
        {
            text = await _client.DownloadAsync(query, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException)
        {
            if (cancellationToken.IsCancellationRequested)
                throw;

            if (cached is not null)
                return FromText(ElementSource.CacheAfterFailedDownload, cached.Value.Text, cached.Value.RetrievedUtc,
                    $"Could not reach CelesTrak ({ex.Message}); using the copy from {Format(cached.Value.RetrievedUtc)}.", ex.Message);

            return new ElementSetResult(ElementSource.None, Array.Empty<OrbitalElements>(), new[] { ex.Message }, null,
                $"Could not reach CelesTrak ({ex.Message}) and there is no local copy of {query}.");
        }

        var result = FromText(ElementSource.Downloaded, text, now, $"Downloaded from CelesTrak at {Format(now)}.");

        // keep a good answer only: a "No GP data found" reply must not replace a working copy
        if (result.Elements.Count > 0)
            WriteCopy(query.Key, text, now, "json");
        else if (cached is not null)
            return FromText(ElementSource.CacheAfterFailedDownload, cached.Value.Text, cached.Value.RetrievedUtc,
                $"CelesTrak returned no elements for {query}; using the copy from {Format(cached.Value.RetrievedUtc)}.", result.Errors.ToArray());

        return result;
    }

    /// <summary>Reads a TLE or OMM JSON file and keeps it in the store under <paramref name="name"/> (default: the file name).</summary>
    public ElementSetResult Import(string path, string? name = null)
    {
        var text = File.ReadAllText(path);

        var now = _utcNow();

        var key = "import-" + Sanitize(name ?? Path.GetFileNameWithoutExtension(path));

        var result = FromText(ElementSource.Import, text, now, $"Imported {Path.GetFileName(path)} at {Format(now)}.");

        if (result.Elements.Count > 0)
            WriteCopy(key, text, now, LooksLikeJson(text) ? "json" : "tle");

        return result;
    }

    /// <summary>A stored copy by key ("group-stations", "catnr-25544", "import-…"), without any download.</summary>
    public ElementSetResult? ReadCached(string key)
    {
        var cached = ReadCopy(key);

        return cached is null ? null
            : FromText(key.StartsWith("import-", StringComparison.Ordinal) ? ElementSource.Import : ElementSource.Cache,
                cached.Value.Text, cached.Value.RetrievedUtc, $"Local copy from {Format(cached.Value.RetrievedUtc)}.");
    }

    /// <summary>The keys of every stored copy.</summary>
    public IReadOnlyList<string> Keys()
        => Directory.Exists(_folder)
            ? Directory.GetFiles(_folder, "*.meta.json").Select(f => Path.GetFileName(f).Replace(".meta.json", string.Empty)).OrderBy(k => k).ToList()
            : (IReadOnlyList<string>)Array.Empty<string>();

    /// <summary>Elements from TLE or OMM JSON text, with the sets that could not be read.</summary>
    public static (List<OrbitalElements> Elements, List<string> Errors) Parse(string text)
    {
        var errors = new List<string>();

        try
        {
            var elements = LooksLikeJson(text)
                ? OmmJsonReader.Read(text, errors.Add)
                : TleReader.Read(text, e => errors.Add(e.ToString()));

            if (elements.Count == 0 && errors.Count == 0)
                errors.Add(text.Length > 120 ? "No element sets were found." : $"No element sets were found: \"{text.Trim()}\"");

            return (elements, errors);
        }
        catch (Exception ex) when (ex is JsonException or FormatException)
        {
            errors.Add(ex.Message);

            return (new List<OrbitalElements>(), errors);
        }
    }

    private static ElementSetResult FromText(ElementSource source, string text, DateTime retrievedUtc, string message, params string[] extraErrors)
    {
        var (elements, errors) = Parse(text);

        errors.AddRange(extraErrors);

        return new ElementSetResult(source, elements, errors, retrievedUtc, message);
    }

    private static bool LooksLikeJson(string text)
    {
        var start = text.TrimStart();

        return start.StartsWith("[", StringComparison.Ordinal) || start.StartsWith("{", StringComparison.Ordinal);
    }

    private static string Format(DateTime utc) => utc.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture);

    private static string Sanitize(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();

        var clean = new string(name.Select(c => invalid.Contains(c) || char.IsWhiteSpace(c) ? '_' : c).ToArray());

        return clean.Length == 0 ? "file" : clean;
    }

    private (string Text, DateTime RetrievedUtc)? ReadCopy(string key)
    {
        var meta = Path.Combine(_folder, key + ".meta.json");

        if (!File.Exists(meta))
            return null;

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(meta));

            var root = document.RootElement;

            var retrieved = DateTime.Parse(root.GetProperty("retrievedUtc").GetString()!, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);

            var dataFile = Path.Combine(_folder, root.GetProperty("file").GetString()!);

            return File.Exists(dataFile) ? (File.ReadAllText(dataFile), retrieved) : null;
        }
        catch (Exception ex) when (ex is JsonException or FormatException or KeyNotFoundException or IOException or InvalidOperationException)
        {
            // a damaged copy is as good as none
            return null;
        }
    }

    private void WriteCopy(string key, string text, DateTime retrievedUtc, string extension)
    {
        Directory.CreateDirectory(_folder);

        var file = key + "." + extension;

        File.WriteAllText(Path.Combine(_folder, file), text);

        var meta = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["file"] = file,
            ["retrievedUtc"] = retrievedUtc.ToString("o", CultureInfo.InvariantCulture),
        });

        File.WriteAllText(Path.Combine(_folder, key + ".meta.json"), meta);
    }
}
