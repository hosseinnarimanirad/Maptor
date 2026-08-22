using System;
using System.IO;
using System.IO.Compression;

namespace IRI.Maptor.Core.Spatial.IO.VectorTiles;

/// <summary>
/// Decompresses MVT tile blobs. Vector MBTiles usually store gzip-compressed protobuf, but the
/// spec also allows uncompressed payloads, so the gzip magic is sniffed first.
/// </summary>
public static class MvtDecompressionHelper
{
    public static byte[] Decompress(byte[] data)
    {
        if (data == null || data.Length == 0)
            return data ?? Array.Empty<byte>();

        if (!IsGzip(data))
            return data;

        using var input = new MemoryStream(data);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();

        gzip.CopyTo(output);

        return output.ToArray();
    }

    public static bool IsGzip(byte[] data) =>
        data != null && data.Length >= 2 && data[0] == 0x1f && data[1] == 0x8b;
}
