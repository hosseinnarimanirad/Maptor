using System;

namespace IRI.Maptor.Core.Spatial.IO.PmTiles;

/// <summary>
/// Provides conversions between z/x/y tile coordinates and PMTiles Hilbert-based tile identifiers.
/// </summary>
public static class PmTilesHilbert
{
    private const int MaxOrder = 26; // 2*26 = 52 < 64 bits safety margin

    public static ulong ToTileId(int zoom, int x, int y)
    {
        ValidateTileCoordinate(zoom, x, y);

        if (zoom == 0)
        {
            return 0;
        }

        var baseId = LevelBase(zoom);
        var index = XYToHilbertIndex(zoom, x, y);
        return baseId + index;
    }

    public static (int zoom, int x, int y) FromTileId(ulong tileId)
    {
        if (tileId == 0)
        {
            return (0, 0, 0);
        }

        var zoom = 0;
        while (zoom < MaxOrder && tileId >= LevelBase(zoom + 1))
        {
            zoom++;
        }

        var baseId = LevelBase(zoom);
        var index = tileId - baseId;
        var (x, y) = HilbertIndexToXY(zoom, index);
        return (zoom, x, y);
    }

    private static ulong LevelBase(int zoom)
    {
        if (zoom <= 0)
        {
            return 0;
        }

        return ((1UL << (zoom * 2)) - 1UL) / 3UL;
    }

    private static void ValidateTileCoordinate(int zoom, int x, int y)
    {
        if (zoom < 0 || zoom > MaxOrder)
        {
            throw new ArgumentOutOfRangeException(nameof(zoom), zoom, $"Zoom must be between 0 and {MaxOrder}.");
        }

        var maxIndex = 1 << zoom;
        if (x < 0 || x >= maxIndex)
        {
            throw new ArgumentOutOfRangeException(nameof(x), x, $"X must be between 0 and {maxIndex - 1} at zoom {zoom}.");
        }

        if (y < 0 || y >= maxIndex)
        {
            throw new ArgumentOutOfRangeException(nameof(y), y, $"Y must be between 0 and {maxIndex - 1} at zoom {zoom}.");
        }
    }

    private static ulong XYToHilbertIndex(int order, int x, int y)
    {
        if (order == 0)
        {
            return 0;
        }

        var n = 1 << order;
        ulong index = 0;

        for (var s = n / 2; s > 0; s /= 2)
        {
            var rx = (x & s) > 0 ? 1 : 0;
            var ry = (y & s) > 0 ? 1 : 0;
            index += (ulong)(s * s) * (ulong)(((3 * rx) ^ ry));
            Rotate(s, ref x, ref y, rx, ry);
        }

        return index;
    }

    private static (int x, int y) HilbertIndexToXY(int order, ulong index)
    {
        if (order == 0)
        {
            return (0, 0);
        }

        var n = 1 << order;
        var x = 0;
        var y = 0;
        var t = index;

        for (var s = 1; s < n; s *= 2)
        {
            var rx = (int)((t / 2) & 1);
            var ry = (int)((t ^ (ulong)rx) & 1);
            Rotate(s, ref x, ref y, rx, ry);
            x += s * rx;
            y += s * ry;
            t /= 4;
        }

        return (x, y);
    }

    private static void Rotate(int regionSize, ref int x, ref int y, int rx, int ry)
    {
        if (ry == 0)
        {
            if (rx == 1)
            {
                x = regionSize - 1 - x;
                y = regionSize - 1 - y;
            }

            (x, y) = (y, x);
        }
    }
} 
