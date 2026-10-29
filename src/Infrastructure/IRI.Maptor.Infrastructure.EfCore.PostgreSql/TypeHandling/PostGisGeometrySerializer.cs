using IRI.Maptor.Core.Common.Abstractions;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.IO.OgcSFA;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Infrastructure.EfCore.PostgreSql.TypeHandling;

/// <summary>
/// Converts between <see cref="Geometry{Point}"/> and the EWKB that PostGIS uses on the wire for
/// <c>geometry</c>/<c>geography</c> values.
/// </summary>
public static class PostGisGeometrySerializer
{
    /// <summary>
    /// Writes EWKB. The SRID is embedded when <see cref="Geometry{T}.Srid"/> is positive; with SRID 0 PostGIS
    /// stores an unknown SRID for <c>geometry</c> and assumes 4326 for <c>geography</c>.
    /// </summary>
    public static byte[] Serialize(Geometry<Point> geometry)
    {
        ArgumentNullException.ThrowIfNull(geometry);

        return geometry.AsEwkb()
            ?? throw new InvalidOperationException("The geometry could not be written as EWKB.");
    }

    /// <summary>
    /// Reads EWKB (or ISO WKB) into a 2D <see cref="Geometry{Point}"/>. Z and M values are dropped, since the
    /// mapped CLR type is 2D.
    /// </summary>
    public static Geometry<Point> Deserialize(byte[] ewkb)
    {
        ArgumentNullException.ThrowIfNull(ewkb);

        var geometry = WkbReader.Parse(ewkb, srid: 0)
            ?? throw new InvalidOperationException("The PostGIS value is empty and could not be read as a geometry.");

        return geometry switch
        {
            Geometry<Point> point => point,
            Geometry<PointZ> pointZ => DropZM(pointZ),
            Geometry<PointM> pointM => DropZM(pointM),
            Geometry<PointZM> pointZM => DropZM(pointZM),
            _ => throw new NotSupportedException($"Unexpected geometry type {geometry.GetType().Name} read from PostGIS.")
        };
    }

    private static Geometry<Point> DropZM<T>(Geometry<T> geometry) where T : IPoint, new()
    {
        if (geometry.Geometries is not null)
            return Geometry<Point>.Create(geometry.Geometries.Select(DropZM).ToList(), geometry.Type, geometry.Srid);

        // Create(null, ...) yields an empty geometry of the same type
        return Geometry<Point>.Create(geometry.Points?.Select(p => new Point(p.X, p.Y)).ToList()!, geometry.Type, geometry.Srid);
    }
}
