using System;
using System.IO;
using System.IO.Compression;

namespace IRI.Maptor.Core.Spatial.IO;

public static class PngWriter
{
    // PNG signature
    private static readonly byte[] PngSignature = { 137, 80, 78, 71, 13, 10, 26, 10 };

    /// <summary>
    /// Writes a PNG file with image data, processing row-by-row to handle large images.
    /// </summary>
    public static void WritePng(string filePath, int width, int height, int bitsPerPixel, int samplesPerPixel,
        Func<int, byte[]> getRowData)
    {
        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        using var writer = new BinaryWriter(stream);

        // Write PNG signature
        writer.Write(PngSignature);

        // Determine color type and bit depth
        byte colorType = samplesPerPixel == 4 ? (byte)6 : (byte)2; // 2 = RGB, 6 = RGBA
        byte bitDepth = (byte)(bitsPerPixel / samplesPerPixel); // Usually 8

        // Write IHDR chunk
        WriteIhdrChunk(writer, width, height, bitDepth, colorType);

        // Write image data as IDAT chunks
        WriteIdatChunks(writer, width, height, samplesPerPixel, getRowData);

        // Write IEND chunk
        WriteIendChunk(writer);
    }

    private static void WriteIhdrChunk(BinaryWriter writer, int width, int height, byte bitDepth, byte colorType)
    {
        // IHDR chunk: width(4), height(4), bitDepth(1), colorType(1), compression(1), filter(1), interlace(1)
        byte[] ihdrData = new byte[13];
        WriteUInt32BigEndian(ihdrData, 0, (uint)width);
        WriteUInt32BigEndian(ihdrData, 4, (uint)height);
        ihdrData[8] = bitDepth;
        ihdrData[9] = colorType;
        ihdrData[10] = 0; // Compression method (0 = deflate/inflate)
        ihdrData[11] = 0; // Filter method (0 = adaptive filtering)
        ihdrData[12] = 0; // Interlace method (0 = no interlace)

        WriteChunk(writer, "IHDR", ihdrData);
    }

    private static void WriteIdatChunks(BinaryWriter writer, int width, int height, int samplesPerPixel,
        Func<int, byte[]> getRowData)
    {
        int bytesPerPixel = samplesPerPixel;
        int bytesPerRow = width * bytesPerPixel;
        
        // Buffer for compressed data (zlib format)
        using var compressedStream = new MemoryStream();
        
        // Write zlib header (CMF + FLG)
        // CMF: compression method (8 = deflate) and window size (7 = 32K)
        // FLG: FCHECK, FDICT, FLEVEL
        compressedStream.WriteByte(0x78); // CMF
        compressedStream.WriteByte(0x9C); // FLG (no dictionary, default compression)
        
        // Process each row and compress
        byte[] previousRow = new byte[bytesPerRow];
        byte[] currentRow = new byte[bytesPerRow + 1]; // +1 for filter byte

        uint adler32 = 1; // Adler-32 checksum starts at 1

        using (var deflateStream = new DeflateStream(compressedStream, CompressionLevel.Optimal, leaveOpen: true))
        {
            for (int row = 0; row < height; row++)
            {
                byte[] rowData = getRowData(row);
                
                // Apply PNG filter (use Sub filter for better compression)
                currentRow[0] = 1; // Filter type: Sub
                for (int i = 0; i < bytesPerRow; i++)
                {
                    byte left = (i >= bytesPerPixel) ? rowData[i - bytesPerPixel] : (byte)0;
                    currentRow[i + 1] = (byte)((rowData[i] - left + 256) % 256);
                }

                // Update Adler-32 checksum
                foreach (byte b in currentRow)
                {
                    adler32 = UpdateAdler32(adler32, b);
                }

                // Write filtered row to deflate stream
                deflateStream.Write(currentRow, 0, currentRow.Length);
                
                // Keep previous row for Up filter (not used here, but good practice)
                Array.Copy(rowData, previousRow, bytesPerRow);
            }
            
            deflateStream.Flush();
        }

        // Write Adler-32 checksum (big-endian)
        byte[] adlerBytes = BitConverter.GetBytes(adler32);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(adlerBytes);
        compressedStream.Write(adlerBytes, 0, 4);

        // Write IDAT chunk with compressed data (including zlib header and checksum)
        byte[] compressedData = compressedStream.ToArray();
        WriteChunk(writer, "IDAT", compressedData);
    }

    private static uint UpdateAdler32(uint adler, byte data)
    {
        uint s1 = adler & 0xFFFF;
        uint s2 = (adler >> 16) & 0xFFFF;
        
        s1 = (s1 + data) % 65521;
        s2 = (s2 + s1) % 65521;
        
        return (s2 << 16) | s1;
    }

    private static void WriteIendChunk(BinaryWriter writer)
    {
        WriteChunk(writer, "IEND", Array.Empty<byte>());
    }

    private static void WriteChunk(BinaryWriter writer, string chunkType, byte[] data)
    {
        // Write chunk length (4 bytes, big-endian)
        WriteUInt32BigEndian(writer, (uint)data.Length);

        // Write chunk type (4 bytes)
        byte[] typeBytes = System.Text.Encoding.ASCII.GetBytes(chunkType);
        writer.Write(typeBytes);

        // Write chunk data
        writer.Write(data);

        // Calculate and write CRC32
        uint crc = CalculateCrc32(typeBytes, data);
        WriteUInt32BigEndian(writer, crc);
    }

    private static void WriteUInt32BigEndian(BinaryWriter writer, uint value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        writer.Write(bytes);
    }

    private static void WriteUInt32BigEndian(byte[] buffer, int offset, uint value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        Array.Copy(bytes, 0, buffer, offset, 4);
    }

    private static uint CalculateCrc32(byte[] typeBytes, byte[] data)
    {
        // CRC32 calculation for PNG
        uint[] crcTable = GenerateCrcTable();
        uint crc = 0xFFFFFFFF;

        foreach (byte b in typeBytes)
        {
            crc = crcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
        }

        foreach (byte b in data)
        {
            crc = crcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
        }

        return crc ^ 0xFFFFFFFF;
    }

    private static uint[] GenerateCrcTable()
    {
        uint[] table = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            uint c = i;
            for (int j = 0; j < 8; j++)
            {
                if ((c & 1) != 0)
                    c = 0xEDB88320 ^ (c >> 1);
                else
                    c >>= 1;
            }
            table[i] = c;
        }
        return table;
    }
}

