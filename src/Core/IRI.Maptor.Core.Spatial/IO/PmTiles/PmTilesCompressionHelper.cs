using System;
using System.IO;
using System.IO.Compression;

namespace IRI.Maptor.Core.Spatial.IO.PmTiles;

internal static class PmTilesCompressionHelper
{
    public static byte[] Decompress(ReadOnlySpan<byte> data, PmTilesCompression compression)
    {
        return compression switch
        {
            PmTilesCompression.None => data.ToArray(),
            PmTilesCompression.Gzip => Inflate(data, streamFactory: s => new GZipStream(s, CompressionMode.Decompress, leaveOpen: false)),
            PmTilesCompression.Brotli => Inflate(data, streamFactory: s => new BrotliStream(s, CompressionMode.Decompress, leaveOpen: false)),
            PmTilesCompression.Zstandard => throw new NotSupportedException("Zstandard compression is not supported by the current runtime."),
            PmTilesCompression.Unknown => throw new NotSupportedException("Unknown compression is not supported."),
            _ => throw new NotSupportedException($"Unsupported compression value: {compression}.")
        };
    }

    public static byte[] Compress(ReadOnlySpan<byte> data, PmTilesCompression compression)
    {
        return compression switch
        {
            PmTilesCompression.None => data.ToArray(),
            PmTilesCompression.Gzip => Deflate(data, streamFactory: s => new GZipStream(s, CompressionLevel.Optimal, leaveOpen: true)),
            PmTilesCompression.Brotli => Deflate(data, streamFactory: s => new BrotliStream(s, CompressionLevel.Optimal, leaveOpen: true)),
            PmTilesCompression.Zstandard => throw new NotSupportedException("Zstandard compression is not supported by the current runtime."),
            PmTilesCompression.Unknown => throw new NotSupportedException("Unknown compression is not supported."),
            _ => throw new NotSupportedException($"Unsupported compression value: {compression}.")
        };
    }

    private static byte[] Inflate(ReadOnlySpan<byte> data, Func<Stream, Stream> streamFactory)
    {
        using var source = new MemoryStream(data.ToArray());
        using var decompressor = streamFactory(source);
        using var destination = new MemoryStream();
        decompressor.CopyTo(destination);
        return destination.ToArray();
    }

    private static byte[] Deflate(ReadOnlySpan<byte> data, Func<Stream, Stream> streamFactory)
    {
        using var destination = new MemoryStream();
        using (var compressor = streamFactory(destination))
        {
            compressor.Write(data);
        }

        return destination.ToArray();
    }
}
 
