using System;
using System.IO;

namespace IRI.Maptor.Sta.Spatial.IO.CesiumTerrain;

/// <summary>
/// Reader for Cesium Quantized-Mesh terrain tiles (.terrain files)
/// Specification: https://github.com/CesiumGS/quantized-mesh
/// </summary>
public static class QuantizedMeshReader
{
    /// <summary>
    /// Reads a .terrain file and returns the quantized mesh data
    /// </summary>
    /// <param name="filePath">Path to the .terrain file</param>
    /// <returns>Parsed quantized mesh data</returns>
    public static QuantizedMeshData Read(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Terrain file not found: {filePath}");

        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var reader = new BinaryReader(stream);

        return Read(reader);
    }

    /// <summary>
    /// Reads quantized mesh data from a stream
    /// </summary>
    /// <param name="stream">Stream containing terrain data</param>
    /// <returns>Parsed quantized mesh data</returns>
    public static QuantizedMeshData Read(Stream stream)
    {
        using var reader = new BinaryReader(stream);
        return Read(reader);
    }

    /// <summary>
    /// Reads quantized mesh data from a binary reader
    /// </summary>
    /// <param name="reader">Binary reader positioned at the start of terrain data</param>
    /// <returns>Parsed quantized mesh data</returns>
    public static QuantizedMeshData Read(BinaryReader reader)
    {
        var data = new QuantizedMeshData
        {
            Header = ReadHeader(reader)
        };

        // Read vertex data
        data.VertexCount = reader.ReadUInt32();

        // Read U coordinates (zigzag decoded deltas)
        data.U = ReadQuantizedVertexData(reader, data.VertexCount);

        // Read V coordinates (zigzag decoded deltas)
        data.V = ReadQuantizedVertexData(reader, data.VertexCount);

        // Read height values (zigzag decoded deltas)
        data.Height = ReadQuantizedVertexData(reader, data.VertexCount);

        // Read triangle indices (high water mark encoded)
        uint triangleCount = reader.ReadUInt32();
        data.Indices = ReadIndices(reader, triangleCount * 3);

        // Read edge indices
        data.WestIndices = ReadEdgeIndices(reader);
        data.SouthIndices = ReadEdgeIndices(reader);
        data.EastIndices = ReadEdgeIndices(reader);
        data.NorthIndices = ReadEdgeIndices(reader);

        // Read optional extensions if any data remains
        if (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            data.Extensions = ReadExtensions(reader);
        }

        return data;
    }

    /// <summary>
    /// Reads the terrain tile header (88 bytes)
    /// </summary>
    private static TerrainTileHeader ReadHeader(BinaryReader reader)
    {
        return new TerrainTileHeader
        {
            // Center coordinates (3 doubles = 24 bytes)
            CenterX = reader.ReadDouble(),
            CenterY = reader.ReadDouble(),
            CenterZ = reader.ReadDouble(),

            // Min/Max height (2 floats = 8 bytes)
            MinimumHeight = reader.ReadSingle(),
            MaximumHeight = reader.ReadSingle(),

            // Bounding sphere (4 doubles = 32 bytes)
            BoundingSphereCenterX = reader.ReadDouble(),
            BoundingSphereCenterY = reader.ReadDouble(),
            BoundingSphereCenterZ = reader.ReadDouble(),
            BoundingSphereRadius = reader.ReadDouble(),

            // Horizon occlusion point (3 doubles = 24 bytes)
            HorizonOcclusionPointX = reader.ReadDouble(),
            HorizonOcclusionPointY = reader.ReadDouble(),
            HorizonOcclusionPointZ = reader.ReadDouble()
        };
        // Total: 24 + 8 + 32 + 24 = 88 bytes
    }

    /// <summary>
    /// Reads quantized vertex data with zigzag decoding
    /// </summary>
    private static ushort[] ReadQuantizedVertexData(BinaryReader reader, uint count)
    {
        var result = new ushort[count];
        int value = 0;

        for (int i = 0; i < count; i++)
        {
            // Decode zigzag-encoded delta
            int delta = DecodeZigZag(reader);
            value += delta;
            result[i] = (ushort)value;
        }

        return result;
    }

    /// <summary>
    /// Reads triangle indices with high water mark encoding
    /// </summary>
    private static uint[] ReadIndices(BinaryReader reader, uint count)
    {
        var result = new uint[count];
        uint highest = 0;

        for (int i = 0; i < count; i++)
        {
            uint code = DecodeVariableLengthUInt(reader);

            // Decode high water mark
            result[i] = highest - code;

            if (code == 0)
            {
                highest++;
            }
        }

        return result;
    }

    /// <summary>
    /// Reads edge indices for tile stitching
    /// </summary>
    private static uint[] ReadEdgeIndices(BinaryReader reader)
    {
        uint count = reader.ReadUInt32();
        var indices = new uint[count];

        for (int i = 0; i < count; i++)
        {
            indices[i] = reader.ReadUInt32();
        }

        return indices;
    }

    /// <summary>
    /// Reads optional extensions
    /// </summary>
    private static TerrainTileExtensions ReadExtensions(BinaryReader reader)
    {
        var extensions = new TerrainTileExtensions();

        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            byte extensionId = reader.ReadByte();
            uint extensionLength = reader.ReadUInt32();

            switch ((TerrainExtensionId)extensionId)
            {
                case TerrainExtensionId.VertexNormals:
                    extensions.VertexNormals = reader.ReadBytes((int)extensionLength);
                    break;

                case TerrainExtensionId.WaterMask:
                    extensions.WaterMask = reader.ReadBytes((int)extensionLength);
                    break;

                case TerrainExtensionId.Metadata:
                    byte[] metadataBytes = reader.ReadBytes((int)extensionLength);
                    extensions.Metadata = System.Text.Encoding.UTF8.GetString(metadataBytes);
                    break;

                default:
                    // Unknown extension - skip it
                    reader.BaseStream.Seek(extensionLength, SeekOrigin.Current);
                    break;
            }
        }

        return extensions;
    }

    /// <summary>
    /// Decodes a zigzag-encoded signed integer
    /// </summary>
    private static int DecodeZigZag(BinaryReader reader)
    {
        uint encoded = DecodeVariableLengthUInt(reader);
        return (int)((encoded >> 1) ^ (-(int)(encoded & 1)));
    }

    /// <summary>
    /// Decodes a variable-length encoded unsigned integer
    /// </summary>
    private static uint DecodeVariableLengthUInt(BinaryReader reader)
    {
        uint result = 0;
        byte shift = 0;
        byte b;

        do
        {
            b = reader.ReadByte();
            result |= (uint)(b & 0x7F) << shift;
            shift += 7;
        } while ((b & 0x80) != 0);

        return result;
    }

    /// <summary>
    /// Decodes oct-encoded normals to 3D vectors
    /// </summary>
    /// <param name="octX">X component (0-255)</param>
    /// <param name="octY">Y component (0-255)</param>
    /// <returns>Normalized 3D normal vector (x, y, z)</returns>
    public static (double x, double y, double z) DecodeOctNormal(byte octX, byte octY)
    {
        // Convert from [0, 255] to [-1, 1]
        double x = (octX / 255.0) * 2.0 - 1.0;
        double y = (octY / 255.0) * 2.0 - 1.0;

        double z = 1.0 - Math.Abs(x) - Math.Abs(y);

        if (z < 0.0)
        {
            double oldX = x;
            x = (1.0 - Math.Abs(y)) * (x >= 0.0 ? 1.0 : -1.0);
            y = (1.0 - Math.Abs(oldX)) * (y >= 0.0 ? 1.0 : -1.0);
        }

        // Normalize
        double length = Math.Sqrt(x * x + y * y + z * z);
        return (x / length, y / length, z / length);
    }
}

