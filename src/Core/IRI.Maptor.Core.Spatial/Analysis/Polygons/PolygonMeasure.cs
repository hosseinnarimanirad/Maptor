using IRI.Maptor.Core.Common.Abstractions;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Extensions;

namespace IRI.Maptor.Core.Spatial.Analysis;

/// <summary>
/// Area and perimeter of a polygon or multipolygon in ground units: square metres and metres for geodetic
/// WGS84 (Karney's ellipsoidal area, ellipsoidal edge lengths), coordinate units otherwise. Holes are taken
/// away from the area and added to the perimeter. Rings follow the house convention: the first point is not
/// repeated at the end.
/// </summary>
public static class PolygonMeasure
{
    public static double Area<T>(Geometry<T>? geometry, bool isGeodetic) where T : IPoint, new()
    {
        if (geometry is null || geometry.IsNullOrEmpty())
            return 0;

        if (isGeodetic)
            return Math.Abs(SpatialUtility.GetKarneyArea(geometry));

        return PolygonsOf(geometry).Sum(polygon =>
        {
            var rings = RingsOf(polygon);

            if (rings.Count == 0)
                return 0;

            var outer = SpatialUtility.GetUnsignedEuclideanArea(rings[0]);
            var holes = rings.Skip(1).Sum(r => SpatialUtility.GetUnsignedEuclideanArea(r));

            return Math.Max(0, outer - holes);
        });
    }

    public static double Perimeter<T>(Geometry<T>? geometry, bool isGeodetic) where T : IPoint, new()
    {
        if (geometry is null || geometry.IsNullOrEmpty())
            return 0;

        return PolygonsOf(geometry).SelectMany(RingsOf).Sum(ring =>
        {
            var length = 0.0;

            for (int i = 0; i < ring.Count; i++)
            {
                var a = ring[i];
                var b = ring[(i + 1) % ring.Count];

                length += isGeodetic ? SpatialUtility.GetEllipsoidalLength(a, b) : SpatialUtility.GetEuclideanLength(a, b);
            }

            return length;
        });
    }

    /// <summary>The polygons of a polygon, a multipolygon or a collection; empty for anything else.</summary>
    public static IReadOnlyList<Geometry<T>> PolygonsOf<T>(Geometry<T> geometry) where T : IPoint, new()
    {
        switch (geometry.Type)
        {
            case GeometryType.Polygon:
                return new[] { geometry };

            case GeometryType.MultiPolygon:
            case GeometryType.GeometryCollection:
                return (geometry.Geometries ?? new List<Geometry<T>>())
                    .Where(g => g is not null)
                    .SelectMany(PolygonsOf)
                    .ToList();

            default:
                return Array.Empty<Geometry<T>>();
        }
    }

    /// <summary>The rings of one polygon as point lists, outer ring first; rings with fewer than 3 points are left out.</summary>
    public static IReadOnlyList<List<T>> RingsOf<T>(Geometry<T> polygon) where T : IPoint, new()
        => (polygon.Geometries ?? new List<Geometry<T>>())
            .Where(r => r?.Points is { Count: >= 3 })
            .Select(r => r.Points!)
            .ToList();
}
