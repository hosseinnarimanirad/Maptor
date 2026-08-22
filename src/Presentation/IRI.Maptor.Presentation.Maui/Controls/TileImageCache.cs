using System.Collections.Concurrent;
using System.Globalization;
using System.Text;

using IRI.Maptor.Core.Spatial.Model;

using Microsoft.Maui.Graphics.Platform;
using Microsoft.Maui.Storage;

using IImage = Microsoft.Maui.Graphics.IImage;

namespace IRI.Maptor.Presentation.Maui.Controls;

/// <summary>
/// Two-level cache of tile images keyed by basemap + tile. Decoded images are kept in
/// memory; raw bytes are persisted under <see cref="FileSystem.CacheDirectory"/> so they
/// survive app restarts and are not re-downloaded. Requests are de-duplicated while in
/// flight; when a tile becomes available the supplied callback is invoked to redraw.
/// </summary>
public sealed class TileImageCache
{
    private static readonly HttpClient _httpClient = CreateClient();

    private static readonly string _diskRoot = Path.Combine(FileSystem.CacheDirectory, "maptor-tiles");

    private readonly ConcurrentDictionary<string, IImage> _cache = new();
    private readonly ConcurrentDictionary<string, byte> _inFlight = new();

    // Tiles a local source reported as absent, so sparse MBTiles coverage isn't re-queried each frame.
    private readonly ConcurrentDictionary<string, byte> _absent = new();

    // Basemap (URL) tiles that recently failed to fetch/decode, mapped to the earliest
    // Environment.TickCount64 (ms) at which they may be retried. Without this, a failing or
    // rate-limited tile server would be re-requested on every redraw, which sustains the failure
    // (e.g. Google throttling the client). Unlike _absent this is time-bounded, since network
    // failures are transient whereas a missing MBTiles tile is permanent.
    private readonly ConcurrentDictionary<string, long> _retryAfter = new();

    private const long RetryBackoffMs = 60_000;

    private readonly Action _onTileLoaded;

    public TileImageCache(Action onTileLoaded)
    {
        _onTileLoaded = onTileLoaded ?? throw new ArgumentNullException(nameof(onTileLoaded));
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30),
        };

        // Several tile servers (e.g. OpenStreetMap) reject requests without a User-Agent.
        client.DefaultRequestHeaders.UserAgent.ParseAdd("IRI.Maptor.Maui/1.0 (+https://github.com/hosseinnarimanirad/Maptor)");

        return client;
    }

    /// <summary>
    /// Returns the cached image for the tile of the given basemap layer, or <c>null</c> if
    /// not yet available (kicking off a background disk/network fetch in that case).
    /// </summary>
    public IImage? GetOrRequest(TileInfo tile, Func<TileInfo, string> urlFunc, string layerKey)
    {
        var key = MemoryKey(layerKey, tile);

        if (_cache.TryGetValue(key, out var image))
        {
            return image;
        }

        // A recently-failed tile is left alone until its backoff expires, so a throttling/failing
        // server isn't re-hammered on every redraw.
        if (_retryAfter.TryGetValue(key, out var retryAt) && Environment.TickCount64 < retryAt)
        {
            return null;
        }

        if (_inFlight.TryAdd(key, 0))
        {
            _ = FetchAsync(key, tile, urlFunc, layerKey);
        }

        return null;
    }

    /// <summary>
    /// Returns the cached image for a tile whose bytes come from a local source (e.g. an MBTiles
    /// file) instead of a URL, or <c>null</c> if not yet available. Absent tiles are remembered so
    /// they are not re-requested on every frame.
    /// </summary>
    public IImage? GetOrRequest(TileInfo tile, Func<TileInfo, byte[]?> byteSource, string layerKey)
    {
        var key = MemoryKey(layerKey, tile);

        if (_cache.TryGetValue(key, out var image))
        {
            return image;
        }

        if (_absent.ContainsKey(key))
        {
            return null;
        }

        if (_inFlight.TryAdd(key, 0))
        {
            _ = FetchBytesAsync(key, tile, byteSource);
        }

        return null;
    }

    private async Task FetchBytesAsync(string memoryKey, TileInfo tile, Func<TileInfo, byte[]?> byteSource)
    {
        try
        {
            // The read (SQLite) runs off the UI thread.
            var bytes = await Task.Run(() => byteSource(tile)).ConfigureAwait(false);

            if (bytes is null || bytes.Length == 0)
            {
                _absent[memoryKey] = 0;
                return;
            }

            using var stream = new MemoryStream(bytes);
            var image = PlatformImage.FromStream(stream);

            if (image != null)
            {
                _cache[memoryKey] = image;
                _onTileLoaded();
            }
        }
        catch
        {
            // Corrupt/undecodable tile — skip it this frame.
        }
        finally
        {
            _inFlight.TryRemove(memoryKey, out _);
        }
    }

    private async Task FetchAsync(string memoryKey, TileInfo tile, Func<TileInfo, string> urlFunc, string layerKey)
    {
        try
        {
            var diskPath = DiskPath(layerKey, tile);

            byte[]? bytes = await TryReadDiskAsync(diskPath).ConfigureAwait(false);
            bool fromDisk = bytes != null;

            if (bytes is null)
            {
                var url = urlFunc(tile);

                if (string.IsNullOrEmpty(url))
                {
                    // No basemap selected — not a failure, so no backoff.
                    return;
                }

                bytes = await _httpClient.GetByteArrayAsync(url).ConfigureAwait(false);
            }

            using var stream = new MemoryStream(bytes);
            var image = PlatformImage.FromStream(stream);

            if (image != null)
            {
                _cache[memoryKey] = image;
                _retryAfter.TryRemove(memoryKey, out _);

                // Persist only decodable tiles (a poisoned/HTML error body must never be cached).
                if (!fromDisk)
                {
                    await TryWriteDiskAsync(diskPath, bytes).ConfigureAwait(false);
                }

                _onTileLoaded();
            }
            else
            {
                // Undecodable response: back off, and drop the poisoned on-disk copy if any.
                MarkFailed(memoryKey);

                if (fromDisk)
                {
                    TryDeleteDisk(diskPath);
                }
            }
        }
        catch
        {
            // HTTP error / throttle / timeout: back off so we don't re-hammer the server every frame.
            MarkFailed(memoryKey);
        }
        finally
        {
            _inFlight.TryRemove(memoryKey, out _);
        }
    }

    private void MarkFailed(string memoryKey)
        => _retryAfter[memoryKey] = Environment.TickCount64 + RetryBackoffMs;

    private static async Task<byte[]?> TryReadDiskAsync(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                return await File.ReadAllBytesAsync(path).ConfigureAwait(false);
            }
        }
        catch
        {
            // Corrupt/locked cache entry — fall back to re-download.
        }

        return null;
    }

    private static void TryDeleteDisk(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best-effort eviction of a poisoned cache entry; ignore failures.
        }
    }

    private static async Task TryWriteDiskAsync(string path, byte[] bytes)
    {
        try
        {
            var directory = Path.GetDirectoryName(path);

            if (directory != null)
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllBytesAsync(path, bytes).ConfigureAwait(false);
        }
        catch
        {
            // Best-effort cache; ignore write failures (e.g. low disk).
        }
    }

    private static string MemoryKey(string layerKey, TileInfo tile)
        => $"{layerKey}/{tile.ZoomLevel}/{tile.ColumnNumber}/{tile.RowNumber}";

    private static string DiskPath(string layerKey, TileInfo tile)
    {
        return Path.Combine(
            _diskRoot,
            MakeSafe(layerKey),
            tile.ZoomLevel.ToString(CultureInfo.InvariantCulture),
            tile.ColumnNumber.ToString(CultureInfo.InvariantCulture),
            $"{tile.RowNumber.ToString(CultureInfo.InvariantCulture)}.tile");
    }

    private static string MakeSafe(string value)
    {
        var builder = new StringBuilder(value.Length);

        foreach (var c in value)
        {
            builder.Append(char.IsLetterOrDigit(c) ? c : '_');
        }

        return builder.Length == 0 ? "default" : builder.ToString();
    }
}
