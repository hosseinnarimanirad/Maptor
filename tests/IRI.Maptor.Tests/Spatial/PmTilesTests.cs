using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IRI.Maptor.Core.Spatial.IO.PmTiles;
using Xunit;

namespace IRI.Maptor.Tests.Spatial;

public class PmTilesTests
{
    [Fact]
    public async Task PmTilesWriterAndReader_VectorTiles_RoundTrip()
    {
        var writer = new PmTilesWriter();

        writer.AddTile(0, 0, 0, Encoding.UTF8.GetBytes("tile-0"));
        writer.AddTile(1, 1, 1, Encoding.UTF8.GetBytes("tile-1"));

        await using var memoryStream = new MemoryStream();
        await writer.WriteAsync(
            memoryStream,
            new PmTilesWriterOptions
            {
                TileType = PmTilesTileType.VectorMvt,
                TileCompression = PmTilesCompression.Gzip,
                InternalCompression = PmTilesCompression.Gzip,
                MetadataJson = "{\"name\":\"unit-test\"}"
            });

        var archiveBytes = memoryStream.ToArray();
        await using var reader = new PmTilesReader(new InMemoryPmTilesStreamSource(archiveBytes));
        await reader.InitializeAsync();

        Assert.Equal(PmTilesTileType.VectorMvt, reader.Header.TileType);
        Assert.Equal(PmTilesCompression.Gzip, reader.Header.TileCompression);
        Assert.Contains("\"name\":\"unit-test\"", reader.MetadataJson);

        var tile0 = await reader.GetTileAsync(0, 0, 0);
        Assert.NotNull(tile0);
        Assert.Equal("tile-0", Encoding.UTF8.GetString(tile0!.Content.Span));

        var tile1 = await reader.GetTileAsync(1, 1, 1);
        Assert.NotNull(tile1);
        Assert.Equal("tile-1", Encoding.UTF8.GetString(tile1!.Content.Span));
    }

    [Fact]
    public async Task PmTilesWriterAndReader_RasterTiles_NoCompression()
    {
        var writer = new PmTilesWriter();
        writer.AddTile(2, 2, 1, new byte[] { 0x01, 0x02, 0x03, 0x04 });

        await using var memoryStream = new MemoryStream();
        await writer.WriteAsync(
            memoryStream,
            new PmTilesWriterOptions
            {
                TileType = PmTilesTileType.RasterPng,
                TileCompression = PmTilesCompression.None,
                InternalCompression = PmTilesCompression.None
            });

        var archiveBytes = memoryStream.ToArray();
        await using var reader = new PmTilesReader(new InMemoryPmTilesStreamSource(archiveBytes));
        await reader.InitializeAsync();

        var tile = await reader.GetTileAsync(2, 2, 1);
        Assert.NotNull(tile);
        Assert.Equal(new byte[] { 0x01, 0x02, 0x03, 0x04 }, tile!.Content.ToArray());
        Assert.Equal(PmTilesCompression.None, reader.Header.TileCompression);
    }

    private sealed class InMemoryPmTilesStreamSource : IPmTilesStreamSource
    {
        private readonly byte[] buffer;

        public InMemoryPmTilesStreamSource(byte[] buffer)
        {
            this.buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
        }

        public ValueTask<ReadOnlyMemory<byte>> ReadAsync(long offset, int length, CancellationToken cancellationToken = default)
        {
            if (offset < 0 || length < 0 || offset + length > buffer.LongLength)
            {
                throw new ArgumentOutOfRangeException();
            }

            return new ValueTask<ReadOnlyMemory<byte>>(new ReadOnlyMemory<byte>(buffer, (int)offset, length));
        }

        public ValueTask<Stream> OpenStreamAsync(CancellationToken cancellationToken = default)
        {
            Stream stream = new MemoryStream(buffer, writable: false);
            return new ValueTask<Stream>(stream);
        }

        public ValueTask DisposeAsync()
        {
            return default;
        }
    }
}

