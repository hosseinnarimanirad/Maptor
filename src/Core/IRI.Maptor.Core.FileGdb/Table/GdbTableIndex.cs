using IRI.Maptor.Core.FileGdb.IO;

namespace IRI.Maptor.Core.FileGdb.Table;

/// <summary>
/// Reads a .gdbtablx file: the offset of every row in the matching .gdbtable, indexed by OBJECTID.
/// </summary>
/// <remarks>
/// Offsets are stored in blocks of 1024 rows. Blocks with no live rows may be left out of the
/// file; a bitmap in the trailer then says which blocks are present. Offset 0 means the row is
/// deleted (or never existed). Layout reference: https://github.com/rouault/dump_gdbtable/wiki/FGDB-Spec
/// </remarks>
internal sealed class GdbTableIndex : IDisposable
{
    internal const int BlockSize = 1024;

    private const int HeaderSize = 16;

    // Version 4 (64-bit OBJECTID) bitmap section; only this one layout is understood.
    private const int V4BitmapLeadingBytes = 22;
    private const int V4BitmapBytes = 32768;
    private const int V4BitmapSectionSize = V4BitmapLeadingBytes + V4BitmapBytes + 52;

    private readonly FileStream _stream;

    private readonly string _path;

    // For each block (0-based): its position among the blocks present in the file, or -1 when absent.
    // Null when every block up to BlocksPresent is present (no bitmap).
    private readonly long[]? _presentOrdinal;

    private GdbTableIndex(FileStream stream, string path, int offsetSize, long blocksPresent, long totalRowCount, long[]? presentOrdinal, bool isObjectIdReliable)
    {
        _stream = stream;
        _path = path;
        OffsetSize = offsetSize;
        BlocksPresent = blocksPresent;
        TotalRowCount = totalRowCount;
        _presentOrdinal = presentOrdinal;
        IsObjectIdReliable = isObjectIdReliable;
    }

    /// <summary>Bytes per stored offset: 4, 5 or 6.</summary>
    public int OffsetSize { get; }

    public long BlocksPresent { get; }

    /// <summary>Highest OBJECTID slot, deleted rows included.</summary>
    public long TotalRowCount { get; }

    /// <summary>
    /// False for a version 4 index whose sparse-block layout is not understood; rows can still be
    /// read, but their OBJECTIDs may be wrong (same limitation as GDAL).
    /// </summary>
    public bool IsObjectIdReliable { get; }

    public static GdbTableIndex Open(string path, int expectedVersion)
    {
        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        try
        {
            return expectedVersion == 3 ? ReadV3(stream, path) : ReadV4(stream, path);
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }

    private static GdbTableIndex ReadV3(FileStream stream, string path)
    {
        var header = new GdbByteReader(stream.ReadExactly(0, HeaderSize, path));

        CheckVersion(header.ReadInt32(), 3, path);
        long blocksPresent = header.ReadUInt32();
        long totalRowCount = header.ReadInt32();
        int offsetSize = CheckOffsetSize(header.ReadInt32(), path);

        if (blocksPresent == 0)
        {
            if (totalRowCount != 0)
                throw new InvalidDataException($"{path}: no offset blocks but {totalRowCount} rows declared.");

            return new GdbTableIndex(stream, path, offsetSize, 0, 0, null, true);
        }

        if (totalRowCount < 0)
            throw new InvalidDataException($"{path}: negative row count {totalRowCount}.");

        long trailerOffset = HeaderSize + (long)offsetSize * BlockSize * blocksPresent;
        var trailer = new GdbByteReader(stream.ReadExactly(trailerOffset, 16, path));

        uint bitmapInt32Words = trailer.ReadUInt32();
        long bitsForBlockMap = trailer.ReadUInt32();
        long blocksPresentAgain = trailer.ReadUInt32();

        if (blocksPresentAgain != blocksPresent)
            throw new InvalidDataException($"{path}: trailer declares {blocksPresentAgain} blocks, header {blocksPresent}.");

        if (bitmapInt32Words == 0)
        {
            if (bitsForBlockMap != blocksPresent)
                throw new InvalidDataException($"{path}: {bitsForBlockMap} blocks in total but {blocksPresent} present and no block bitmap.");

            CheckRowsFitBlocks(totalRowCount, blocksPresent, path);

            return new GdbTableIndex(stream, path, offsetSize, blocksPresent, totalRowCount, null, true);
        }

        if (totalRowCount > bitsForBlockMap * BlockSize)
            throw new InvalidDataException($"{path}: {totalRowCount} rows do not fit the {bitsForBlockMap} blocks of the bitmap.");

        var bitmap = stream.ReadExactly(trailerOffset + 16, (int)((bitsForBlockMap + 7) / 8), path);
        var presentOrdinal = BuildPresentOrdinals(bitmap, bitsForBlockMap);

        if (CountPresent(presentOrdinal) != blocksPresent)
            throw new InvalidDataException($"{path}: block bitmap does not match the {blocksPresent} blocks present.");

        return new GdbTableIndex(stream, path, offsetSize, blocksPresent, totalRowCount, presentOrdinal, true);
    }

    private static GdbTableIndex ReadV4(FileStream stream, string path)
    {
        var header = new GdbByteReader(stream.ReadExactly(0, HeaderSize, path));

        CheckVersion(header.ReadInt32(), 4, path);

        // int64 in version 4 (shares bytes 8-11 with what is the row count in version 3)
        ulong blocksPresentRaw = header.ReadUInt64();
        int offsetSize = CheckOffsetSize(header.ReadInt32(), path);

        if (blocksPresentRaw > (ulong)((long.MaxValue - HeaderSize) / (offsetSize * BlockSize)))
            throw new InvalidDataException($"{path}: implausible block count {blocksPresentRaw}.");

        long blocksPresent = (long)blocksPresentRaw;

        if (blocksPresent == 0)
            return new GdbTableIndex(stream, path, offsetSize, 0, 0, null, true);

        long trailerOffset = HeaderSize + (long)offsetSize * BlockSize * blocksPresent;
        var trailer = new GdbByteReader(stream.ReadExactly(trailerOffset, 12, path));

        long totalRowCount = trailer.ReadInt64();
        uint bitmapSectionSize = trailer.ReadUInt32();

        if (totalRowCount < 0)
            throw new InvalidDataException($"{path}: negative row count {totalRowCount}.");

        if (bitmapSectionSize == 0)
        {
            CheckRowsFitBlocks(totalRowCount, blocksPresent, path);

            return new GdbTableIndex(stream, path, offsetSize, blocksPresent, totalRowCount, null, true);
        }

        if (bitmapSectionSize == V4BitmapSectionSize && totalRowCount <= (long)V4BitmapBytes * 8 * BlockSize)
        {
            var section = stream.ReadExactly(trailerOffset + 12, V4BitmapSectionSize, path);

            if (IsUnderstoodV4BitmapSection(section))
            {
                long totalBlocks = (totalRowCount + BlockSize - 1) / BlockSize;
                var presentOrdinal = BuildPresentOrdinals(section.AsSpan(V4BitmapLeadingBytes, V4BitmapBytes), totalBlocks);

                if (CountPresent(presentOrdinal) == blocksPresent)
                    return new GdbTableIndex(stream, path, offsetSize, blocksPresent, totalRowCount, presentOrdinal, true);
            }
        }

        // Sparse layout not understood: read the present blocks back to back. Every row is still
        // found, but OBJECTIDs after a gap are shifted.
        return new GdbTableIndex(stream, path, offsetSize, blocksPresent, blocksPresent * BlockSize, null, false);
    }

    /// <summary>
    /// Yields (OBJECTID, offset in .gdbtable) for every slot whose offset is not 0, in OBJECTID order.
    /// </summary>
    public IEnumerable<GdbRowLocation> EnumerateRowLocations()
    {
        long totalBlocks = (TotalRowCount + BlockSize - 1) / BlockSize;
        var buffer = new byte[OffsetSize * BlockSize];

        for (long block = 0; block < totalBlocks; block++)
        {
            long ordinal = GetPresentOrdinal(block);

            if (ordinal < 0)
                continue;

            _stream.ReadExactly(HeaderSize + ordinal * buffer.Length, buffer, buffer.Length, _path);

            long firstObjectId = block * BlockSize + 1;
            int rowsInBlock = (int)Math.Min(BlockSize, TotalRowCount - block * BlockSize);

            for (int i = 0; i < rowsInBlock; i++)
            {
                long offset = DecodeOffset(buffer, i * OffsetSize);

                if (offset != 0)
                    yield return new GdbRowLocation(firstObjectId + i, offset);
            }
        }
    }

    /// <summary>
    /// Offset of the row with the given OBJECTID, or 0 when it is deleted or out of range.
    /// </summary>
    public long GetRowOffset(long objectId)
    {
        if (objectId < 1 || objectId > TotalRowCount)
            return 0;

        long row = objectId - 1;
        long ordinal = GetPresentOrdinal(row / BlockSize);

        if (ordinal < 0)
            return 0;

        var buffer = new byte[OffsetSize];
        _stream.ReadExactly(HeaderSize + (ordinal * BlockSize + row % BlockSize) * OffsetSize, buffer, buffer.Length, _path);

        return DecodeOffset(buffer, 0);
    }

    public void Dispose() => _stream.Dispose();

    private long GetPresentOrdinal(long block)
    {
        if (_presentOrdinal is null)
            return block < BlocksPresent ? block : -1;

        return block < _presentOrdinal.Length ? _presentOrdinal[block] : -1;
    }

    private long DecodeOffset(byte[] buffer, int start)
    {
        long value = 0;

        for (int i = OffsetSize - 1; i >= 0; i--)
            value = (value << 8) | buffer[start + i];

        return value;
    }

    private static long[] BuildPresentOrdinals(ReadOnlySpan<byte> bitmap, long blockCount)
    {
        var result = new long[blockCount];
        long ordinal = 0;

        for (long block = 0; block < blockCount; block++)
        {
            bool present = (bitmap[(int)(block / 8)] & (1 << (int)(block % 8))) != 0;
            result[block] = present ? ordinal++ : -1;
        }

        return result;
    }

    private static long CountPresent(long[] presentOrdinal) => presentOrdinal.LongCount(o => o >= 0);

    private static bool IsUnderstoodV4BitmapSection(byte[] section)
    {
        ReadOnlySpan<byte> leading = [0x01, 0x00, 0x01, 0x00, 0x00, 0x00];
        ReadOnlySpan<byte> trailing = [0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];

        return section.AsSpan(0, leading.Length).SequenceEqual(leading)
            && section.AsSpan(V4BitmapLeadingBytes + V4BitmapBytes, trailing.Length).SequenceEqual(trailing);
    }

    private static void CheckVersion(int version, int expected, string path)
    {
        if (version != expected)
            throw new InvalidDataException($"{path}: .gdbtablx version {version} does not match .gdbtable version {expected}.");
    }

    private static int CheckOffsetSize(int offsetSize, string path)
    {
        if (offsetSize < 4 || offsetSize > 6)
            throw new InvalidDataException($"{path}: unsupported offset size {offsetSize} (expected 4, 5 or 6).");

        return offsetSize;
    }

    private static void CheckRowsFitBlocks(long totalRowCount, long blocksPresent, string path)
    {
        if (totalRowCount > blocksPresent * BlockSize)
            throw new InvalidDataException($"{path}: {totalRowCount} rows do not fit the {blocksPresent} offset blocks.");
    }
}

/// <summary>
/// OBJECTID of a row slot and the byte offset of its record in the .gdbtable file.
/// </summary>
internal readonly struct GdbRowLocation
{
    public GdbRowLocation(long objectId, long offset)
    {
        ObjectId = objectId;
        Offset = offset;
    }

    public long ObjectId { get; }

    public long Offset { get; }
}
