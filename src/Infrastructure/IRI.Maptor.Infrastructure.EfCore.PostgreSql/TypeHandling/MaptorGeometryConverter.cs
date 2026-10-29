using Npgsql.Internal;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Infrastructure.EfCore.PostgreSql.TypeHandling;

/// <summary>
/// Npgsql converter for PostGIS <c>geometry</c>/<c>geography</c> values. The binary wire format of both types is
/// EWKB, decoded and encoded by <see cref="PostGisGeometrySerializer"/>.
/// </summary>
internal sealed class MaptorGeometryConverter : PgStreamingConverter<Geometry<Point>>
{
    public override Geometry<Point> Read(PgReader reader)
    {
        var ewkb = new byte[reader.CurrentRemaining];

        reader.ReadBytes(ewkb);

        return PostGisGeometrySerializer.Deserialize(ewkb);
    }

    public override async ValueTask<Geometry<Point>> ReadAsync(PgReader reader, CancellationToken cancellationToken = default)
    {
        var ewkb = new byte[reader.CurrentRemaining];

        await reader.ReadBytesAsync(ewkb, cancellationToken).ConfigureAwait(false);

        return PostGisGeometrySerializer.Deserialize(ewkb);
    }

    // The EWKB is produced once here, to know its size, and handed to Write through the write state.
    public override Size GetSize(SizeContext context, Geometry<Point> value, ref object? writeState)
    {
        var ewkb = PostGisGeometrySerializer.Serialize(value);

        writeState = ewkb;

        return ewkb.Length;
    }

    public override void Write(PgWriter writer, Geometry<Point> value)
        => writer.WriteBytes(GetEwkb(writer, value));

    public override ValueTask WriteAsync(PgWriter writer, Geometry<Point> value, CancellationToken cancellationToken = default)
        => writer.WriteBytesAsync(GetEwkb(writer, value), cancellationToken);

    private static byte[] GetEwkb(PgWriter writer, Geometry<Point> value)
        => writer.Current.WriteState as byte[] ?? PostGisGeometrySerializer.Serialize(value);
}
