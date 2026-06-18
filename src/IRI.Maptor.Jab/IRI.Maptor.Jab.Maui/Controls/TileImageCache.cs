using System.Collections.Concurrent;
using System.Globalization;
using System.Text;

using IRI.Maptor.Sta.Spatial.Model;

using Microsoft.Maui.Graphics.Platform;
using Microsoft.Maui.Storage;

using IImage = Microsoft.Maui.Graphics.IImage;

namespace IRI.Maptor.Jab.Maui.Controls;

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

        if (_inFlight.TryAdd(key, 0))
        {
            _ = FetchAsync(key, tile, urlFunc, layerKey);
        }

        return null;
    }

    private async Task FetchAsync(string memoryKey, TileInfo tile, Func<TileInfo, string> urlFunc, string layerKey)
    {
        try
        {
            var diskPath = DiskPath(layerKey, tile);

            byte[]? bytes = await TryReadDiskAsync(diskPath).ConfigureAwait(false);

            if (bytes is null)
            {
                var url = urlFunc(tile);

                if (string.IsNullOrEmpty(url))
                {
                    return;
                }

                bytes = await _httpClient.GetByteArrayAsync(url).ConfigureAwait(false);

                await TryWriteDiskAsync(diskPath, bytes).ConfigureAwait(false);
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
            // Missing/failed tiles are simply not drawn this frame. A later pan/zoom
            // will trigger another attempt.
        }
        finally
        {
            _inFlight.TryRemove(memoryKey, out _);
        }
    }

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
