using IRI.Maptor.Sta.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.Spatial.IO;

public static class TiffReader
{
    public static RasterGeoTiff ReadGeoTiff32bitDEM(string filePath)
    {
        bool isLittleEndian;
        var tags = ReadTiffTags(filePath, out isLittleEndian);

        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var reader = new BinaryReader(stream);

        // read raster
        var raster = ReadRaster(tags, reader, isLittleEndian);

        // read metadata
        var meta = ReadGeoMetadata(tags, reader, isLittleEndian);

        return new RasterGeoTiff(raster, meta.GetGeodeticWgs84BoundingBox());
    }


    public static Dictionary<ushort, TiffTag> ReadTiffTags(string filePath, out bool isLittleEndian)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var reader = new BinaryReader(stream);

        // --- HEADER ---
        byte[] endianBytes = reader.ReadBytes(2);

        isLittleEndian = endianBytes[0] == 'I' && endianBytes[1] == 'I';

        if (!(isLittleEndian || (endianBytes[0] == 'M' && endianBytes[1] == 'M')))
            throw new InvalidDataException("Invalid TIFF byte order");

        ushort magic = ReadUInt16(reader, isLittleEndian);

        if (magic != 42)
            throw new InvalidDataException("Invalid TIFF magic number");

        uint ifdOffset = ReadUInt32(reader, isLittleEndian);

        reader.BaseStream.Seek(ifdOffset, SeekOrigin.Begin);

        // --- IFD ENTRIES ---
        ushort entryCount = ReadUInt16(reader, isLittleEndian);

        var tags = new Dictionary<ushort, TiffTag>();

        for (int i = 0; i < entryCount; i++)
        {
            ushort tag = ReadUInt16(reader, isLittleEndian);

            ushort type = ReadUInt16(reader, isLittleEndian);

            uint count = ReadUInt32(reader, isLittleEndian);

            uint valueOrOffset = ReadUInt32(reader, isLittleEndian);

            tags[tag] = new TiffTag { Type = type, Count = count, ValueOrOffset = valueOrOffset };
        }

        return tags;
    }

    // --- Metadata helper ---
    public static GeoTiffMetadata ReadGeoMetadata(Dictionary<ushort, TiffTag> tags, BinaryReader reader, bool isLittleEndian)
    {
        var meta = new GeoTiffMetadata();

        if (tags.ContainsKey(256)) // ImageWidth
        {
            //var widthTag = tags[256];
            meta.ImageWidth = (int)ReadTagScalarUInt(tags[256], reader, isLittleEndian);
        }

        if (tags.ContainsKey(257)) // ImageLength (Height)
        {
            //var heightTag = tags[257];
            meta.ImageHeight = (int)ReadTagScalarUInt(tags[257], reader, isLittleEndian);
        }

        if (tags.ContainsKey(33550)) // PixelScale
            meta.PixelScale = ReadTagDoubleArray(tags[33550], reader, isLittleEndian);

        if (tags.ContainsKey(33922)) // Tiepoints
            meta.TiePoints = ReadTagDoubleArray(tags[33922], reader, isLittleEndian);

        if (tags.ContainsKey(34264)) // Transformation
            meta.Transformation = ReadTagDoubleArray(tags[34264], reader, isLittleEndian);

        if (tags.ContainsKey(34735)) // GeoKeyDirectory
            meta.GeoKeyDirectory = ReadTagUShortArray(tags[34735], reader, isLittleEndian);

        if (tags.ContainsKey(34736)) // GeoDoubleParams
            meta.GeoDoubleParams = ReadTagDoubleArray(tags[34736], reader, isLittleEndian);

        if (tags.ContainsKey(34737)) // GeoAsciiParams
            meta.GeoAsciiParams = ReadTagAscii(tags[34737], reader);

        return meta;
    }

    private static Matrix ReadRaster(Dictionary<ushort, TiffTag> tags, BinaryReader reader, bool isLittleEndian)
    {
        uint width = ReadTagScalarUInt(tags[256], reader, isLittleEndian);

        uint height = ReadTagScalarUInt(tags[257], reader, isLittleEndian);

        ushort bitsPerSample = (ushort)ReadTagScalarUInt(tags[258], reader, isLittleEndian);

        ushort compression = (ushort)ReadTagScalarUInt(tags[259], reader, isLittleEndian);

        ushort sampleFormat = (ushort)ReadTagScalarUInt(tags[339], reader, isLittleEndian);

        if (bitsPerSample != 32 || compression != 1 || sampleFormat != 3)
            throw new InvalidDataException("Unsupported TIFF format. Must be 32-bit float, uncompressed grayscale");

        var result = new Matrix((int)height, (int)width);

        // --- STRIPS ---
        if (tags.ContainsKey(273))
        {
            uint[] stripOffsets = ReadTagUIntArray(tags[273], reader, isLittleEndian);

            uint[] stripByteCounts = tags.ContainsKey(279)
                ? ReadTagUIntArray(tags[279], reader, isLittleEndian)
                : [width * height * 4];

            int rowSize = (int)(width * 4);

            int currentRow = 0;

            byte[] buffer = new byte[4];

            for (int s = 0; s < stripOffsets.Length; s++)
            {
                reader.BaseStream.Seek(stripOffsets[s], SeekOrigin.Begin);

                int rowsInStrip = (int)(stripByteCounts[s] / rowSize);

                for (int y = 0; y < rowsInStrip && currentRow < height; y++, currentRow++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        reader.Read(buffer, 0, 4);

                        if (isLittleEndian != BitConverter.IsLittleEndian)
                            Array.Reverse(buffer);

                        result[currentRow, x] = BitConverter.ToSingle(buffer, 0);
                    }
                }
            }
        }
        // --- TILES ---
        else if (tags.ContainsKey(324) && tags.ContainsKey(325))
        {
            uint[] tileOffsets = ReadTagUIntArray(tags[324], reader, isLittleEndian);

            uint[] tileByteCounts = ReadTagUIntArray(tags[325], reader, isLittleEndian);

            uint tileWidth = tags.ContainsKey(322) ? ReadTagScalarUInt(tags[322], reader, isLittleEndian) : width;

            uint tileLength = tags.ContainsKey(323) ? ReadTagScalarUInt(tags[323], reader, isLittleEndian) : height;

            int tilesAcross = (int)Math.Ceiling(width / (double)tileWidth);

            int tilesDown = (int)Math.Ceiling(height / (double)tileLength);

            byte[] buffer = new byte[4];

            for (int t = 0; t < tileOffsets.Length; t++)
            {
                int tileX = t % tilesAcross;

                int tileY = t / tilesAcross;

                reader.BaseStream.Seek(tileOffsets[t], SeekOrigin.Begin);

                for (int y = 0; y < tileLength; y++)
                {
                    int imgY = tileY * (int)tileLength + y;

                    if (imgY >= height) break;

                    for (int x = 0; x < tileWidth; x++)
                    {
                        int imgX = tileX * (int)tileWidth + x;

                        if (imgX >= width) break;

                        reader.Read(buffer, 0, 4);

                        if (isLittleEndian != BitConverter.IsLittleEndian)
                            Array.Reverse(buffer);

                        result[imgY, imgX] = BitConverter.ToSingle(buffer, 0);
                    }
                }
            }
        }
        else
        {
            throw new InvalidDataException("TIFF has neither StripOffsets (273) nor TileOffsets (324). Unsupported layout.");
        }

        return result;
    }


    // --- Utilities ---
    // === LOW-LEVEL HELPERS ===
    private static ushort ReadUInt16(BinaryReader reader, bool little)
    {
        var b = reader.ReadBytes(2);
        if (little != BitConverter.IsLittleEndian) Array.Reverse(b);
        return BitConverter.ToUInt16(b, 0);
    }

    private static uint ReadUInt32(BinaryReader reader, bool little)
    {
        var b = reader.ReadBytes(4);
        if (little != BitConverter.IsLittleEndian) Array.Reverse(b);

        return BitConverter.ToUInt32(b, 0);
    }

    private static uint ReadTagUInt32(TiffTag tag, BinaryReader reader, bool isLittleEndian)
    {
        // Move to offset if necessary
        reader.BaseStream.Seek(tag.ValueOrOffset, SeekOrigin.Begin);
        byte[] bytes = reader.ReadBytes(4);
        if (isLittleEndian != BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        return BitConverter.ToUInt32(bytes, 0);
    }

    private static uint ReadTagScalarUInt(TiffTag tag, BinaryReader reader, bool little)
    {
        if (tag.Count != 1) throw new InvalidDataException("Expected scalar tag");
        if (tag.Type == 3) return (ushort)tag.ValueOrOffset; // SHORT
        if (tag.Type == 4) return tag.ValueOrOffset;         // LONG
        throw new InvalidDataException("Unsupported tag type");
    }

    private static uint[] ReadTagUIntArray(TiffTag tag, BinaryReader reader, bool little)
    {
        if (tag.Count == 1)
            return [ReadTagScalarUInt(tag, reader, little)];

        long saved = reader.BaseStream.Position;

        reader.BaseStream.Seek(tag.ValueOrOffset, SeekOrigin.Begin);

        var result = new uint[tag.Count];

        for (int i = 0; i < tag.Count; i++)
        {
            if (tag.Type == 3)
                result[i] = ReadUInt16(reader, little);

            else if (tag.Type == 4)
                result[i] = ReadUInt32(reader, little);

            else
                throw new InvalidDataException("Unsupported array tag type");
        }

        reader.BaseStream.Seek(saved, SeekOrigin.Begin);

        return result;
    }

    private static ushort[] ReadTagUShortArray(TiffTag tag, BinaryReader reader, bool little)
    {
        long saved = reader.BaseStream.Position;

        reader.BaseStream.Seek(tag.ValueOrOffset, SeekOrigin.Begin);

        ushort[] values = new ushort[tag.Count];

        for (int i = 0; i < tag.Count; i++)
            values[i] = ReadUInt16(reader, little);

        reader.BaseStream.Seek(saved, SeekOrigin.Begin);

        return values;
    }

    private static double[] ReadTagDoubleArray(TiffTag tag, BinaryReader reader, bool little)
    {
        long saved = reader.BaseStream.Position;

        reader.BaseStream.Seek(tag.ValueOrOffset, SeekOrigin.Begin);

        double[] values = new double[tag.Count];

        for (int i = 0; i < tag.Count; i++)
        {
            var b = reader.ReadBytes(8);

            if (little != BitConverter.IsLittleEndian) Array.Reverse(b);

            values[i] = BitConverter.ToDouble(b, 0);
        }

        reader.BaseStream.Seek(saved, SeekOrigin.Begin);

        return values;
    }

    private static string ReadTagAscii(TiffTag tag, BinaryReader reader)
    {
        long saved = reader.BaseStream.Position;

        reader.BaseStream.Seek(tag.ValueOrOffset, SeekOrigin.Begin);

        var bytes = reader.ReadBytes((int)tag.Count);

        reader.BaseStream.Seek(saved, SeekOrigin.Begin);

        return System.Text.Encoding.ASCII.GetString(bytes).TrimEnd('\0');
    }
}
