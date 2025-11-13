using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace IRI.Maptor.Sta.Spatial.IO;

public static class TiffWriter
{
    private const bool IsLittleEndian = true; // Always use little-endian for consistency

    /// <summary>
    /// Writes a GeoTIFF file with image data and metadata.
    /// </summary>
    public static void WriteGeoTiff(string filePath, int width, int height, int bitsPerSample, int samplesPerPixel,
        GeoTiffMetadata metadata, Func<int, int, byte[]> getRowData)
    {
        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        using var writer = new BinaryWriter(stream);

        // Write TIFF header
        WriteHeader(writer);

        // Write image data as strips
        int bytesPerPixel = bitsPerSample / 8;
        int bytesPerRow = width * bytesPerPixel;
        int stripHeight = 256;
        int numStrips = (int)Math.Ceiling((double)height / stripHeight);

        List<uint> stripOffsets = new List<uint>();
        List<uint> stripByteCounts = new List<uint>();

        for (int strip = 0; strip < numStrips; strip++)
        {
            uint stripOffset = (uint)stream.Position;
            stripOffsets.Add(stripOffset);

            int startRow = strip * stripHeight;
            int endRow = Math.Min(startRow + stripHeight, height);
            int actualStripHeight = endRow - startRow;

            for (int row = startRow; row < endRow; row++)
            {
                byte[] rowData = getRowData(row, width);
                writer.Write(rowData);
            }

            stripByteCounts.Add((uint)(actualStripHeight * bytesPerRow));
        }

        // Write IFD
        uint ifdOffset = (uint)stream.Position;
        WriteIFD(writer, width, height, bitsPerSample, samplesPerPixel, stripHeight, numStrips,
            stripOffsets.ToArray(), stripByteCounts.ToArray(), metadata, ref ifdOffset);

        // Update header with IFD offset
        stream.Position = 4;
        writer.Write(ifdOffset);
    }

    private static void WriteHeader(BinaryWriter writer)
    {
        writer.Write((byte)'I');
        writer.Write((byte)'I');
        writer.Write((ushort)42); // Magic number
        writer.Write((uint)0); // Placeholder for IFD offset (will be updated later)
    }

    private static void WriteIFD(BinaryWriter writer, int width, int height, int bitsPerSample, int samplesPerPixel,
        int rowsPerStrip, int numStrips, uint[] stripOffsets, uint[] stripByteCounts,
        GeoTiffMetadata metadata, ref uint currentDataOffset)
    {
        // Calculate number of IFD entries
        ushort entryCount = 15; // Base tags + GeoTIFF tags
        if (metadata.GeoKeyDirectory != null) entryCount++;
        if (metadata.GeoAsciiParams != null) entryCount++;

        writer.Write(entryCount);

        uint ifdEndPosition = (uint)(writer.BaseStream.Position + (entryCount * 12) + 4);
        currentDataOffset = ifdEndPosition;

        // Write standard TIFF tags
        WriteTag(writer, 256, 4, 1, (uint)width); // ImageWidth
        WriteTag(writer, 257, 4, 1, (uint)height); // ImageLength
        WriteTag(writer, 258, 3, (ushort)samplesPerPixel, GetBitsPerSampleArray(bitsPerSample, samplesPerPixel), ref currentDataOffset); // BitsPerSample
        WriteTag(writer, 259, 3, 1, (ushort)1); // Compression (1 = uncompressed)
        WriteTag(writer, 262, 3, 1, (ushort)2); // PhotometricInterpretation (2 = RGB)
        WriteTag(writer, 273, 4, (uint)numStrips, stripOffsets, ref currentDataOffset); // StripOffsets
        WriteTag(writer, 278, 4, 1, (uint)rowsPerStrip); // RowsPerStrip
        WriteTag(writer, 279, 4, (uint)numStrips, stripByteCounts, ref currentDataOffset); // StripByteCounts
        WriteTag(writer, 282, 5, 1, 1.0, ref currentDataOffset); // XResolution (dummy)
        WriteTag(writer, 283, 5, 1, 1.0, ref currentDataOffset); // YResolution (dummy)
        WriteTag(writer, 296, 3, 1, (ushort)2); // ResolutionUnit (2 = inches)

        // Write GeoTIFF tags
        if (metadata.PixelScale != null && metadata.PixelScale.Length >= 3)
        {
            WriteTag(writer, 33550, 12, (uint)metadata.PixelScale.Length, metadata.PixelScale, ref currentDataOffset); // ModelPixelScaleTag
        }

        if (metadata.TiePoints != null && metadata.TiePoints.Length >= 6)
        {
            WriteTag(writer, 33922, 12, (uint)metadata.TiePoints.Length, metadata.TiePoints, ref currentDataOffset); // ModelTiepointTag
        }

        if (metadata.GeoKeyDirectory != null && metadata.GeoKeyDirectory.Length > 0)
        {
            WriteTag(writer, 34735, 3, (ushort)metadata.GeoKeyDirectory.Length, metadata.GeoKeyDirectory, ref currentDataOffset); // GeoKeyDirectoryTag
        }

        if (!string.IsNullOrEmpty(metadata.GeoAsciiParams))
        {
            byte[] asciiBytes = Encoding.ASCII.GetBytes(metadata.GeoAsciiParams);
            WriteTag(writer, 34737, 2, (ushort)asciiBytes.Length, asciiBytes, ref currentDataOffset); // GeoAsciiParamsTag
        }

        // Next IFD offset (0 = no more IFDs)
        writer.Write((uint)0);
    }

    private static ushort[] GetBitsPerSampleArray(int bitsPerSample, int samplesPerPixel)
    {
        int bitsPerChannel = bitsPerSample / samplesPerPixel;
        var result = new ushort[samplesPerPixel];
        for (int i = 0; i < samplesPerPixel; i++)
        {
            result[i] = (ushort)bitsPerChannel;
        }
        return result;
    }

    // Tag writing helpers
    private static void WriteTag(BinaryWriter writer, ushort tag, ushort type, uint count, uint value)
    {
        writer.Write(tag);
        writer.Write(type);
        writer.Write(count);
        writer.Write(value);
    }

    private static void WriteTag(BinaryWriter writer, ushort tag, ushort type, ushort count, ushort value)
    {
        writer.Write(tag);
        writer.Write(type);
        writer.Write((uint)count);
        writer.Write((uint)value);
    }

    private static void WriteTag(BinaryWriter writer, ushort tag, ushort type, ushort count, ushort[] values, ref uint currentDataOffset)
    {
        writer.Write(tag);
        writer.Write(type);
        writer.Write((uint)count);

        if (count * 2 <= 4)
        {
            // Fit in value field
            uint value = 0;
            for (int i = 0; i < count && i < 2; i++)
            {
                value |= (uint)(values[i] << (i * 16));
            }
            writer.Write(value);
        }
        else
        {
            // Store offset
            writer.Write(currentDataOffset);
            long savedPos = writer.BaseStream.Position;
            writer.BaseStream.Position = currentDataOffset;
            foreach (var val in values)
            {
                writer.Write(val);
            }
            currentDataOffset = (uint)writer.BaseStream.Position;
            writer.BaseStream.Position = savedPos;
        }
    }

    private static void WriteTag(BinaryWriter writer, ushort tag, ushort type, uint count, uint[] values, ref uint currentDataOffset)
    {
        writer.Write(tag);
        writer.Write(type);
        writer.Write(count);

        if (count * 4 <= 4)
        {
            writer.Write(count > 0 ? values[0] : 0);
        }
        else
        {
            writer.Write(currentDataOffset);
            long savedPos = writer.BaseStream.Position;
            writer.BaseStream.Position = currentDataOffset;
            foreach (var val in values)
            {
                writer.Write(val);
            }
            currentDataOffset = (uint)writer.BaseStream.Position;
            writer.BaseStream.Position = savedPos;
        }
    }

    private static void WriteTag(BinaryWriter writer, ushort tag, ushort type, uint count, double value, ref uint currentDataOffset)
    {
        writer.Write(tag);
        writer.Write(type);
        writer.Write(count);
        writer.Write(currentDataOffset);
        long savedPos = writer.BaseStream.Position;
        writer.BaseStream.Position = currentDataOffset;
        writer.Write(value);
        currentDataOffset = (uint)writer.BaseStream.Position;
        writer.BaseStream.Position = savedPos;
    }

    private static void WriteTag(BinaryWriter writer, ushort tag, ushort type, uint count, double[] values, ref uint currentDataOffset)
    {
        writer.Write(tag);
        writer.Write(type);
        writer.Write(count);
        writer.Write(currentDataOffset);
        long savedPos = writer.BaseStream.Position;
        writer.BaseStream.Position = currentDataOffset;
        foreach (var val in values)
        {
            writer.Write(val);
        }
        currentDataOffset = (uint)writer.BaseStream.Position;
        writer.BaseStream.Position = savedPos;
    }

    private static void WriteTag(BinaryWriter writer, ushort tag, ushort type, ushort count, byte[] values, ref uint currentDataOffset)
    {
        writer.Write(tag);
        writer.Write(type);
        writer.Write((uint)count);
        writer.Write(currentDataOffset);
        long savedPos = writer.BaseStream.Position;
        writer.BaseStream.Position = currentDataOffset;
        writer.Write(values);
        // Pad to even boundary
        if (values.Length % 2 == 1)
        {
            writer.Write((byte)0);
            currentDataOffset = (uint)(writer.BaseStream.Position);
        }
        else
        {
            currentDataOffset = (uint)writer.BaseStream.Position;
        }
        writer.BaseStream.Position = savedPos;
    }
}

