using System;
using System.Buffers.Binary;
using System.Text;

namespace IRI.Maptor.Core.Spatial.IO.PmTiles;

/// <summary>
/// Represents the 127-byte PMTiles v3 header.
/// </summary>
public sealed class PmTilesHeader
{
    public PmTilesHeader()
    {
    }

    public ulong RootDirectoryOffset { get; set; }

    public ulong RootDirectoryLength { get; set; }

    public ulong MetadataOffset { get; set; }

    public ulong MetadataLength { get; set; }

    public ulong LeafDirectoriesOffset { get; set; }

    public ulong LeafDirectoriesLength { get; set; }

    public ulong TileDataOffset { get; set; }

    public ulong TileDataLength { get; set; }

    public ulong NumberOfAddressedTiles { get; set; }

    public ulong NumberOfTileEntries { get; set; }

    public ulong NumberOfTileContents { get; set; }

    public bool IsClustered { get; set; }

    public PmTilesCompression InternalCompression { get; set; }

    public PmTilesCompression TileCompression { get; set; }

    public PmTilesTileType TileType { get; set; }

    public byte MinZoom { get; set; }

    public byte MaxZoom { get; set; }

    public PmTilesBounds Bounds { get; set; }

    public byte CenterZoom { get; set; }

    public PmTilesPosition CenterPosition { get; set; }

    public static PmTilesHeader Read(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < PmTilesConstants.HeaderLength)
        {
            throw new ArgumentException("The buffer is smaller than the PMTiles header size.", nameof(buffer));
        }

        var magic = Encoding.ASCII.GetString(buffer[..PmTilesConstants.MagicLength]);
        if (!string.Equals(magic, PmTilesConstants.Magic, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"The PMTiles magic string is invalid. Expected '{PmTilesConstants.Magic}', got '{magic}'.");
        }

        var version = buffer[PmTilesConstants.MagicLength];
        if (version != PmTilesConstants.CurrentVersion)
        {
            throw new InvalidOperationException($"Unsupported PMTiles version {version}. Only version {PmTilesConstants.CurrentVersion} is supported.");
        }

        var offset = PmTilesConstants.MagicLength + 1;

        var header = new PmTilesHeader
        {
            RootDirectoryOffset = ReadUInt64(buffer, ref offset),
            RootDirectoryLength = ReadUInt64(buffer, ref offset),
            MetadataOffset = ReadUInt64(buffer, ref offset),
            MetadataLength = ReadUInt64(buffer, ref offset),
            LeafDirectoriesOffset = ReadUInt64(buffer, ref offset),
            LeafDirectoriesLength = ReadUInt64(buffer, ref offset),
            TileDataOffset = ReadUInt64(buffer, ref offset),
            TileDataLength = ReadUInt64(buffer, ref offset),
            NumberOfAddressedTiles = ReadUInt64(buffer, ref offset),
            NumberOfTileEntries = ReadUInt64(buffer, ref offset),
            NumberOfTileContents = ReadUInt64(buffer, ref offset),
            IsClustered = ReadByte(buffer, ref offset) == 1,
            InternalCompression = (PmTilesCompression)ReadByte(buffer, ref offset),
            TileCompression = (PmTilesCompression)ReadByte(buffer, ref offset),
            TileType = (PmTilesTileType)ReadByte(buffer, ref offset),
            MinZoom = ReadByte(buffer, ref offset),
            MaxZoom = ReadByte(buffer, ref offset),
            Bounds = new PmTilesBounds(
                ReadPosition(buffer, ref offset),
                ReadPosition(buffer, ref offset)),
            CenterZoom = ReadByte(buffer, ref offset),
            CenterPosition = ReadPosition(buffer, ref offset)
        };

        ValidateZooms(header.MinZoom, header.MaxZoom);

        return header;
    }

    public byte[] ToBytes()
    {
        Span<byte> buffer = stackalloc byte[PmTilesConstants.HeaderLength];
        Write(buffer);
        return buffer.ToArray();
    }

    public void Write(Span<byte> buffer)
    {
        if (buffer.Length < PmTilesConstants.HeaderLength)
        {
            throw new ArgumentException("The buffer is smaller than the PMTiles header size.", nameof(buffer));
        }

        var magicBytes = Encoding.ASCII.GetBytes(PmTilesConstants.Magic);
        magicBytes.CopyTo(buffer);
        buffer[PmTilesConstants.MagicLength] = PmTilesConstants.CurrentVersion;

        var offset = PmTilesConstants.MagicLength + 1;

        WriteUInt64(buffer, ref offset, RootDirectoryOffset);
        WriteUInt64(buffer, ref offset, RootDirectoryLength);
        WriteUInt64(buffer, ref offset, MetadataOffset);
        WriteUInt64(buffer, ref offset, MetadataLength);
        WriteUInt64(buffer, ref offset, LeafDirectoriesOffset);
        WriteUInt64(buffer, ref offset, LeafDirectoriesLength);
        WriteUInt64(buffer, ref offset, TileDataOffset);
        WriteUInt64(buffer, ref offset, TileDataLength);
        WriteUInt64(buffer, ref offset, NumberOfAddressedTiles);
        WriteUInt64(buffer, ref offset, NumberOfTileEntries);
        WriteUInt64(buffer, ref offset, NumberOfTileContents);
        WriteByte(buffer, ref offset, IsClustered ? (byte)1 : (byte)0);
        WriteByte(buffer, ref offset, (byte)InternalCompression);
        WriteByte(buffer, ref offset, (byte)TileCompression);
        WriteByte(buffer, ref offset, (byte)TileType);
        WriteByte(buffer, ref offset, MinZoom);
        WriteByte(buffer, ref offset, MaxZoom);

        PmTilesPosition.WriteBytes(buffer.Slice(offset, 8), Bounds.Min);
        offset += 8;
        PmTilesPosition.WriteBytes(buffer.Slice(offset, 8), Bounds.Max);
        offset += 8;

        WriteByte(buffer, ref offset, CenterZoom);
        PmTilesPosition.WriteBytes(buffer.Slice(offset, 8), CenterPosition);
        offset += 8;

        if (offset != PmTilesConstants.HeaderLength)
        {
            throw new InvalidOperationException("Internal header serialization error: unexpected byte count.");
        }
    }

    private static ulong ReadUInt64(ReadOnlySpan<byte> buffer, ref int offset)
    {
        var value = BinaryPrimitives.ReadUInt64LittleEndian(buffer.Slice(offset, sizeof(ulong)));
        offset += sizeof(ulong);
        return value;
    }

    private static byte ReadByte(ReadOnlySpan<byte> buffer, ref int offset)
    {
        var value = buffer[offset];
        offset += 1;

        return value;
    }

    private static PmTilesPosition ReadPosition(ReadOnlySpan<byte> buffer, ref int offset)
    {
        var position = PmTilesPosition.FromBytes(buffer.Slice(offset, 8));
        offset += 8;
        return position;
    }

    private static void WriteUInt64(Span<byte> buffer, ref int offset, ulong value)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(buffer.Slice(offset, sizeof(ulong)), value);
        offset += sizeof(ulong);
    }

    private static void WriteByte(Span<byte> buffer, ref int offset, byte value)
    {
        buffer[offset] = value;
        offset += 1;
    }

    private static void ValidateZooms(byte minZoom, byte maxZoom)
    {
        if (minZoom > maxZoom)
        {
            throw new InvalidOperationException($"Invalid zoom range: min {minZoom} exceeds max {maxZoom}.");
        }
    }
} 
