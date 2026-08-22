using System.Collections;

using Microsoft.EntityFrameworkCore.ChangeTracking;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Infrastructure.EfCore.ValueConversion;

/// <summary>
/// EF Core change-tracking comparer for <see cref="Geometry{Point}"/>. Two geometries are considered equal when
/// they share the same SRID and produce byte-identical WKB; snapshots are taken via <see cref="Geometry{T}.Clone"/>
/// so the change tracker holds an independent copy.
/// </summary>
/// <remarks>
/// Comparison serializes to WKB per call, which is acceptable for typical feature entities (queries are mostly
/// <c>AsNoTracking</c>). It is exposed publicly so applications can reuse it with a plain
/// <c>HasConversion(..., new MaptorGeometryValueComparer())</c> when not going through the SQL Server type-mapping plugin.
/// </remarks>
public sealed class MaptorGeometryValueComparer : ValueComparer<Geometry<Point>>
{
    public MaptorGeometryValueComparer()
        : base(
            (left, right) => AreEqual(left, right),
            geometry => ComputeHashCode(geometry),
            geometry => geometry.Clone())
    {
    }

    private static bool AreEqual(Geometry<Point>? left, Geometry<Point>? right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left is null || right is null)
            return false;

        if (left.Srid != right.Srid)
            return false;

        return StructuralComparisons.StructuralEqualityComparer.Equals(left.AsWkb(), right.AsWkb());
    }

    private static int ComputeHashCode(Geometry<Point> geometry)
    {
        var wkb = geometry.AsWkb();
        var wkbHash = wkb is null ? 0 : StructuralComparisons.StructuralEqualityComparer.GetHashCode(wkb);

        return HashCode.Combine(geometry.Srid, wkbHash);
    }
}
