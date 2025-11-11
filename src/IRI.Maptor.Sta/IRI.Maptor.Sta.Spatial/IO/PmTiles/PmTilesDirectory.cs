using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IRI.Maptor.Sta.Spatial.IO.PmTiles;

/// <summary>
/// Represents a PMTiles directory (root or leaf) and provides encode/decode helpers.
/// </summary>
public sealed class PmTilesDirectory
{
    private readonly PmTilesDirectoryEntry[] entries;

    public PmTilesDirectory(IEnumerable<PmTilesDirectoryEntry> entries)
    {
        this.entries = entries?.ToArray() ?? throw new ArgumentNullException(nameof(entries));

        if (this.entries.Length == 0)
        {
            throw new ArgumentException("A directory must contain at least one entry.", nameof(entries));
        }

        ValidateOrdering(this.entries);
    }

    public IReadOnlyList<PmTilesDirectoryEntry> Entries => entries;

    public bool ContainsLeafPointers => entries.Any(e => e.IsLeafDirectory);

    public ulong TotalRunLength
    {
        get
        {
            ulong total = 0;
            foreach (var entry in entries)
            {
                total += entry.RunLength;
            }

            return total;
        }
    }

    public static PmTilesDirectory Decode(ReadOnlySpan<byte> data)
    {
        var offset = 0;
        var entryCount = (int)PmTilesVarint.Read(data, ref offset);

        if (entryCount <= 0)
        {
            throw new InvalidDataException("A directory must contain at least one entry.");
        }

        var tileIds = new ulong[entryCount];
        var runLengths = new ulong[entryCount];
        var lengths = new ulong[entryCount];
        var offsets = new ulong[entryCount];

        ulong lastTileId = 0;
        for (var i = 0; i < entryCount; i++)
        {
            var delta = PmTilesVarint.Read(data, ref offset);
            lastTileId += delta;
            tileIds[i] = lastTileId;
        }

        for (var i = 0; i < entryCount; i++)
        {
            runLengths[i] = PmTilesVarint.Read(data, ref offset);
        }

        for (var i = 0; i < entryCount; i++)
        {
            var length = PmTilesVarint.Read(data, ref offset);
            if (length == 0)
            {
                throw new InvalidDataException("Directory entry length must be greater than zero.");
            }

            lengths[i] = length;
        }

        for (var i = 0; i < entryCount; i++)
        {
            var encodedOffset = PmTilesVarint.Read(data, ref offset);

            if (encodedOffset == 0)
            {
                if (i == 0)
                {
                    throw new InvalidDataException("The first directory entry cannot have a zero offset marker.");
                }

                offsets[i] = offsets[i - 1] + lengths[i - 1];
            }
            else
            {
                offsets[i] = encodedOffset - 1;
            }
        }

        var entries = new PmTilesDirectoryEntry[entryCount];
        for (var i = 0; i < entryCount; i++)
        {
            entries[i] = new PmTilesDirectoryEntry(tileIds[i], offsets[i], lengths[i], runLengths[i]);
        }

        return new PmTilesDirectory(entries);
    }

    public byte[] Encode()
    {
        var entryCount = entries.Length;

        if (entryCount == 0)
        {
            throw new InvalidOperationException("Cannot encode a directory with zero entries.");
        }

        var size = PmTilesVarint.GetEncodedSize((ulong)entryCount);

        ulong lastTileId = 0;
        for (var i = 0; i < entryCount; i++)
        {
            var delta = entries[i].TileId - lastTileId;
            size += PmTilesVarint.GetEncodedSize(delta);
            lastTileId = entries[i].TileId;
        }

        for (var i = 0; i < entryCount; i++)
        {
            size += PmTilesVarint.GetEncodedSize(entries[i].RunLength);
        }

        for (var i = 0; i < entryCount; i++)
        {
            if (entries[i].Length == 0)
            {
                throw new InvalidOperationException("Directory entry length must be greater than zero.");
            }

            size += PmTilesVarint.GetEncodedSize(entries[i].Length);
        }

        for (var i = 0; i < entryCount; i++)
        {
            var previousContiguous = i > 0 && entries[i].Offset == entries[i - 1].Offset + entries[i - 1].Length;
            var encodedOffset = previousContiguous ? 0UL : entries[i].Offset + 1;
            size += PmTilesVarint.GetEncodedSize(encodedOffset);
        }

        var result = new byte[size];
        var offset = 0;

        PmTilesVarint.Write(result, ref offset, (ulong)entryCount);

        lastTileId = 0;
        for (var i = 0; i < entryCount; i++)
        {
            var delta = entries[i].TileId - lastTileId;
            PmTilesVarint.Write(result, ref offset, delta);
            lastTileId = entries[i].TileId;
        }

        for (var i = 0; i < entryCount; i++)
        {
            PmTilesVarint.Write(result, ref offset, entries[i].RunLength);
        }

        for (var i = 0; i < entryCount; i++)
        {
            PmTilesVarint.Write(result, ref offset, entries[i].Length);
        }

        for (var i = 0; i < entryCount; i++)
        {
            var previousContiguous = i > 0 && entries[i].Offset == entries[i - 1].Offset + entries[i - 1].Length;
            var encodedOffset = previousContiguous ? 0UL : entries[i].Offset + 1;
            PmTilesVarint.Write(result, ref offset, encodedOffset);
        }

        if (offset != size)
        {
            throw new InvalidOperationException("Directory encoding produced an unexpected byte count.");
        }

        return result;
    }

    private static void ValidateOrdering(IReadOnlyList<PmTilesDirectoryEntry> items)
    {
        ulong previousTileId = 0;

        for (var i = 0; i < items.Count; i++)
        {
            var entry = items[i];
            if (i > 0 && entry.TileId < previousTileId)
            {
                throw new ArgumentException("Directory entries must be sorted by TileId in ascending order.", nameof(items));
            }

            previousTileId = entry.TileId;
        }
    }
}
