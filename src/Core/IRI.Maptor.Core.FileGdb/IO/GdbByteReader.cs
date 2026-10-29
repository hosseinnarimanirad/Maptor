using System.Buffers.Binary;
using System.Text;

namespace IRI.Maptor.Core.FileGdb.IO;

/// <summary>
/// Little-endian cursor over a byte span. Every read is bounds-checked and throws
/// <see cref="InvalidDataException"/> instead of reading past the end, so a truncated or
/// corrupt file surfaces as a clear error rather than garbage values.
/// </summary>
internal ref struct GdbByteReader
{
    private readonly ReadOnlySpan<byte> _buffer;

    public GdbByteReader(ReadOnlySpan<byte> buffer)
    {
        _buffer = buffer;
        Position = 0;
    }

    public int Position { get; private set; }

    public int Remaining => _buffer.Length - Position;

    public ReadOnlySpan<byte> ReadSpan(int count)
    {
        if (count < 0 || count > Remaining)
            throw new InvalidDataException($"Unexpected end of data: needed {count} bytes at position {Position}, {Remaining} left.");

        var result = _buffer.Slice(Position, count);
        Position += count;
        return result;
    }

    public void Skip(int count) => ReadSpan(count);

    public byte ReadByte() => ReadSpan(1)[0];

    public short ReadInt16() => BinaryPrimitives.ReadInt16LittleEndian(ReadSpan(2));

    public ushort ReadUInt16() => BinaryPrimitives.ReadUInt16LittleEndian(ReadSpan(2));

    public int ReadInt32() => BinaryPrimitives.ReadInt32LittleEndian(ReadSpan(4));

    public uint ReadUInt32() => BinaryPrimitives.ReadUInt32LittleEndian(ReadSpan(4));

    public long ReadInt64() => BinaryPrimitives.ReadInt64LittleEndian(ReadSpan(8));

    public ulong ReadUInt64() => BinaryPrimitives.ReadUInt64LittleEndian(ReadSpan(8));

    public float ReadSingle() => BitConverter.Int32BitsToSingle(ReadInt32());

    public double ReadDouble() => BitConverter.Int64BitsToDouble(ReadInt64());

    /// <summary>
    /// Reads an unsigned LEB128-style integer: 7 bits per byte, least significant group first,
    /// high bit set on every byte except the last.
    /// </summary>
    public ulong ReadVarUInt64()
    {
        ulong result = 0;
        int shift = 0;

        while (true)
        {
            byte b = ReadByte();

            if (shift == 63 && (b & 0x7E) != 0)
                throw new InvalidDataException($"Varuint overflows 64 bits at position {Position}.");

            result |= (ulong)(b & 0x7F) << shift;

            if ((b & 0x80) == 0)
                return result;

            shift += 7;
            if (shift > 63)
                throw new InvalidDataException($"Varuint longer than 10 bytes at position {Position}.");
        }
    }

    /// <summary>
    /// Reads a signed varint as used for geometry coordinate deltas: the first byte holds 6 value
    /// bits, a sign bit (0x40) and the continuation bit; following bytes hold 7 value bits each.
    /// </summary>
    public long ReadVarInt64()
    {
        byte b = ReadByte();

        bool negative = (b & 0x40) != 0;
        ulong value = (ulong)(b & 0x3F);
        int shift = 6;

        while ((b & 0x80) != 0)
        {
            b = ReadByte();

            if (shift > 62 || (shift == 62 && (b & 0x7C) != 0))
                throw new InvalidDataException($"Varint overflows 64 bits at position {Position}.");

            value |= (ulong)(b & 0x7F) << shift;
            shift += 7;
        }

        if (value > long.MaxValue)
            throw new InvalidDataException($"Varint overflows 64 bits at position {Position}.");

        return negative ? -(long)value : (long)value;
    }

    /// <summary>
    /// Reads a varuint used as a byte length and checks it fits in what is left of the buffer.
    /// </summary>
    public int ReadVarLength()
    {
        ulong length = ReadVarUInt64();

        if (length > (ulong)Remaining)
            throw new InvalidDataException($"Length {length} at position {Position} exceeds the {Remaining} bytes left.");

        return (int)length;
    }

    /// <summary>
    /// Reads <paramref name="charCount"/> UTF-16LE code units.
    /// </summary>
    public string ReadUtf16(int charCount) => Encoding.Unicode.GetString(ReadSpan(charCount * 2));

    public string ReadUtf8(int byteCount) => Encoding.UTF8.GetString(ReadSpan(byteCount));
}
