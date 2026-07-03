using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace IRI.Maptor.Sta.Spatial.IO.VectorTiles;

/// <summary>
/// Minimal Protocol Buffers wire-format reader, sufficient to decode Mapbox Vector Tiles
/// (https://github.com/mapbox/vector-tile-spec). Reads varints, length-delimited fields
/// (strings / sub-messages / packed repeated) and the fixed 32/64-bit values used by the
/// MVT <c>Value</c> message. Kept dependency-free so the core <c>Sta.Spatial</c> package does
/// not take on a protobuf NuGet reference.
/// </summary>
internal sealed class MvtProtoReader
{
    // Protobuf wire types.
    public const int WireVarint = 0;
    public const int WireFixed64 = 1;
    public const int WireLengthDelimited = 2;
    public const int WireFixed32 = 5;

    private readonly byte[] _buffer;
    private int _position;
    private readonly int _end;

    public MvtProtoReader(byte[] buffer) : this(buffer, 0, buffer?.Length ?? 0) { }

    private MvtProtoReader(byte[] buffer, int start, int end)
    {
        _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
        _position = start;
        _end = end;
    }

    /// <summary>Reads the next field tag. Returns false when the (sub-)message is exhausted.</summary>
    public bool ReadTag(out int fieldNumber, out int wireType)
    {
        if (_position >= _end)
        {
            fieldNumber = 0;
            wireType = 0;
            return false;
        }

        var tag = ReadVarint();
        fieldNumber = (int)(tag >> 3);
        wireType = (int)(tag & 0x7);
        return true;
    }

    public ulong ReadVarint()
    {
        ulong result = 0;
        int shift = 0;

        while (_position < _end)
        {
            byte b = _buffer[_position++];
            result |= (ulong)(b & 0x7F) << shift;

            if ((b & 0x80) == 0)
                return result;

            shift += 7;

            if (shift > 63)
                throw new InvalidDataException("MVT varint is too long.");
        }

        throw new InvalidDataException("MVT varint is truncated.");
    }

    public uint ReadUInt32() => (uint)ReadVarint();

    public long ReadInt64() => (long)ReadVarint();

    public bool ReadBool() => ReadVarint() != 0;

    public string ReadString()
    {
        int length = (int)ReadVarint();
        EnsureAvailable(length);
        var value = Encoding.UTF8.GetString(_buffer, _position, length);
        _position += length;
        return value;
    }

    /// <summary>Returns a reader scoped to the next length-delimited sub-message.</summary>
    public MvtProtoReader ReadMessage()
    {
        int length = (int)ReadVarint();
        EnsureAvailable(length);
        var sub = new MvtProtoReader(_buffer, _position, _position + length);
        _position += length;
        return sub;
    }

    public float ReadFloat()
    {
        EnsureAvailable(4);
        // Protobuf is little-endian; .NET on supported (little-endian) platforms matches.
        float value = BitConverter.ToSingle(_buffer, _position);
        _position += 4;
        return value;
    }

    public double ReadDouble()
    {
        EnsureAvailable(8);
        double value = BitConverter.ToDouble(_buffer, _position);
        _position += 8;
        return value;
    }

    /// <summary>Reads a packed repeated uint32 field into <paramref name="output"/>.</summary>
    public void ReadPackedUInt32(List<uint> output)
    {
        int length = (int)ReadVarint();
        EnsureAvailable(length);
        int subEnd = _position + length;

        while (_position < subEnd)
            output.Add((uint)ReadVarint());
    }

    public void SkipField(int wireType)
    {
        switch (wireType)
        {
            case WireVarint:
                ReadVarint();
                break;

            case WireFixed64:
                EnsureAvailable(8);
                _position += 8;
                break;

            case WireLengthDelimited:
                int length = (int)ReadVarint();
                EnsureAvailable(length);
                _position += length;
                break;

            case WireFixed32:
                EnsureAvailable(4);
                _position += 4;
                break;

            default:
                throw new InvalidDataException($"MVT unsupported wire type {wireType}.");
        }
    }

    private void EnsureAvailable(int count)
    {
        if (count < 0 || _position + count > _end)
            throw new InvalidDataException("MVT message is truncated.");
    }
}
