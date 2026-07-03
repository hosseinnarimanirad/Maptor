using System.Collections.Generic;

namespace IRI.Maptor.Sta.Spatial.IO.VectorTiles;

/// <summary>
/// Decodes a (decompressed) Mapbox Vector Tile protobuf into <see cref="MvtTile"/> DTOs.
/// Field numbers follow the vector-tile spec:
/// Tile.layers=3; Layer.name=1, features=2, keys=3, values=4, extent=5, version=15;
/// Feature.id=1, tags=2, type=3, geometry=4; Value.string=1, float=2, double=3, int=4, uint=5, sint=6, bool=7.
/// </summary>
public static class MvtTileReader
{
    public static MvtTile Decode(byte[] decompressed)
    {
        var tile = new MvtTile();

        if (decompressed == null || decompressed.Length == 0)
            return tile;

        var reader = new MvtProtoReader(decompressed);

        while (reader.ReadTag(out int field, out int wireType))
        {
            if (field == 3 && wireType == MvtProtoReader.WireLengthDelimited)
                tile.Layers.Add(DecodeLayer(reader.ReadMessage()));
            else
                reader.SkipField(wireType);
        }

        return tile;
    }

    private static MvtLayer DecodeLayer(MvtProtoReader reader)
    {
        var layer = new MvtLayer();
        var keys = new List<string>();
        var values = new List<object?>();

        // Features are buffered raw because keys/values may be declared after them.
        var rawFeatures = new List<(ulong Id, MvtGeometryKind Kind, List<uint> Tags, List<uint> Geometry)>();

        while (reader.ReadTag(out int field, out int wireType))
        {
            switch (field)
            {
                case 1:
                    layer.Name = reader.ReadString();
                    break;

                case 2:
                    rawFeatures.Add(DecodeFeature(reader.ReadMessage()));
                    break;

                case 3:
                    keys.Add(reader.ReadString());
                    break;

                case 4:
                    values.Add(DecodeValue(reader.ReadMessage()));
                    break;

                case 5:
                    layer.Extent = reader.ReadUInt32();
                    break;

                case 15:
                    layer.Version = reader.ReadUInt32();
                    break;

                default:
                    reader.SkipField(wireType);
                    break;
            }
        }

        foreach (var raw in rawFeatures)
        {
            var feature = new MvtFeature
            {
                Id = raw.Id,
                GeometryKind = raw.Kind,
                Geometry = raw.Geometry,
            };

            // Tags are (keyIndex, valueIndex) pairs.
            for (int i = 0; i + 1 < raw.Tags.Count; i += 2)
            {
                int keyIndex = (int)raw.Tags[i];
                int valueIndex = (int)raw.Tags[i + 1];

                if (keyIndex >= 0 && keyIndex < keys.Count && valueIndex >= 0 && valueIndex < values.Count)
                    feature.Attributes[keys[keyIndex]] = values[valueIndex];
            }

            layer.Features.Add(feature);
        }

        return layer;
    }

    private static (ulong, MvtGeometryKind, List<uint>, List<uint>) DecodeFeature(MvtProtoReader reader)
    {
        ulong id = 0;
        var kind = MvtGeometryKind.Unknown;
        var tags = new List<uint>();
        var geometry = new List<uint>();

        while (reader.ReadTag(out int field, out int wireType))
        {
            switch (field)
            {
                case 1:
                    id = reader.ReadVarint();
                    break;

                case 2:
                    if (wireType == MvtProtoReader.WireLengthDelimited)
                        reader.ReadPackedUInt32(tags);
                    else
                        tags.Add((uint)reader.ReadVarint());
                    break;

                case 3:
                    kind = (MvtGeometryKind)reader.ReadVarint();
                    break;

                case 4:
                    if (wireType == MvtProtoReader.WireLengthDelimited)
                        reader.ReadPackedUInt32(geometry);
                    else
                        geometry.Add((uint)reader.ReadVarint());
                    break;

                default:
                    reader.SkipField(wireType);
                    break;
            }
        }

        return (id, kind, tags, geometry);
    }

    private static object? DecodeValue(MvtProtoReader reader)
    {
        object? value = null;

        while (reader.ReadTag(out int field, out int wireType))
        {
            switch (field)
            {
                case 1:
                    value = reader.ReadString();
                    break;

                case 2:
                    value = reader.ReadFloat();
                    break;

                case 3:
                    value = reader.ReadDouble();
                    break;

                case 4:
                    value = reader.ReadInt64();
                    break;

                case 5:
                    value = (long)reader.ReadVarint();
                    break;

                case 6:
                    value = ZigZagDecode(reader.ReadVarint());
                    break;

                case 7:
                    value = reader.ReadBool();
                    break;

                default:
                    reader.SkipField(wireType);
                    break;
            }
        }

        return value;
    }

    private static long ZigZagDecode(ulong value) => (long)(value >> 1) ^ -(long)(value & 1);
}
