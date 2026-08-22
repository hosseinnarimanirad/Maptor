using System;
using System.IO;

namespace IRI.Maptor.Core.Spatial.IO.PmTiles;

internal static class PmTilesVarint
{
    public static ulong Read(ReadOnlySpan<byte> buffer, ref int offset)
    {
        ulong result = 0;
        int shift = 0;

        while (offset < buffer.Length)
        {
            var b = buffer[offset++];
            result |= ((ulong)(b & 0x7F)) << shift;

            if ((b & 0x80) == 0)
            {
                return result;
            }

            shift += 7;

            if (shift > 63)
            {
                throw new InvalidDataException("Variable-length integer is too large.");
            }
        }

        throw new InvalidDataException("Unexpected end of buffer while reading variable-length integer.");
    }

    public static void Write(Span<byte> buffer, ref int offset, ulong value)
    {
        while (true)
        {
            var b = (byte)(value & 0x7F);
            value >>= 7;

            if (value != 0)
            {
                b |= 0x80;
            }

            buffer[offset++] = b;

            if (value == 0)
            {
                break;
            }
        }
    }

    public static int GetEncodedSize(ulong value)
    {
        int size = 1;
        while ((value >>= 7) != 0)
        {
            size++;
        }

        return size;
    }
} 