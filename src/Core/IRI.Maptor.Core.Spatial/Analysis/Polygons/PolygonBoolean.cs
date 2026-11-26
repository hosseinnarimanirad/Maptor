using IRI.Maptor.Core.Common.Abstractions;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Core.Spatial.Analysis;

/// <summary>
/// Difference and union of two polygons, built from their boundary pieces the way <see cref="PolygonOverlap"/>
/// measures them: each boundary is cut where it meets the other, the pieces that bound the result are kept and
/// chained into rings. This stays right where the boundaries share edges or vertices (neighbouring parcels), where
/// <c>Geometry.Intersection</c> does not work. Rings follow the house way: never closed, outer counterclockwise,
/// holes clockwise. Both polygons are flattened into one <see cref="LocalMetricFrame"/> at the centre of their boxes.
/// </summary>
public static class PolygonBoolean
{
    /// <summary>
    /// <paramref name="first"/> without what it shares with <paramref name="second"/>; null when nothing is left.
    /// </summary>
    /// <param name="first">A polygon or multipolygon.</param>
    /// <param name="second">A polygon or multipolygon.</param>
    /// <param name="isGeodetic">True for WGS84 longitude/latitude.</param>
    /// <param name="tolerance">Boundaries closer than this (metres) are one line; keep it tiny (e.g. 1e-6).</param>
    public static Geometry<T>? Difference<T>(Geometry<T> first, Geometry<T> second, bool isGeodetic, double tolerance) where T : IPoint, new()
    {
        var (frame, a, b) = Planar(first, second, isGeodetic);

        var pieces = new List<RingPiece>();

        foreach (var piece in BoundaryPieces.Split(a, new[] { b }, tolerance))
        {
            var location = BoundaryPieces.Locate(piece, b, tolerance);

            // Outside the other, or a shared edge with the other on the far side: still a boundary of the result.
            if (location == PieceLocation.Outside || location == PieceLocation.OnOppositeDirection)
                pieces.Add(piece);
        }

        foreach (var piece in BoundaryPieces.Split(b, new[] { a }, tolerance))
        {
            // The other's boundary inside this one bounds the result, the other way round.
            if (BoundaryPieces.Locate(piece, a, tolerance) == PieceLocation.Inside)
                pieces.Add(new RingPiece(piece.End, piece.Start, piece.RingIndex));
        }

        return Build<T>(pieces, frame, first.Srid, tolerance);
    }

    /// <summary>The area covered by either polygon; null when both are empty.</summary>
    /// <param name="first">A polygon or multipolygon.</param>
    /// <param name="second">A polygon or multipolygon.</param>
    /// <param name="isGeodetic">True for WGS84 longitude/latitude.</param>
    /// <param name="tolerance">Boundaries closer than this (metres) are one line; keep it tiny (e.g. 1e-6).</param>
    public static Geometry<T>? Union<T>(Geometry<T> first, Geometry<T> second, bool isGeodetic, double tolerance) where T : IPoint, new()
    {
        var (frame, a, b) = Planar(first, second, isGeodetic);

        var pieces = new List<RingPiece>();

        foreach (var piece in BoundaryPieces.Split(a, new[] { b }, tolerance))
        {
            var location = BoundaryPieces.Locate(piece, b, tolerance);

            // A stretch both run the same way is kept once (from the first); one between them disappears.
            if (location == PieceLocation.Outside || location == PieceLocation.OnSameDirection)
                pieces.Add(piece);
        }

        foreach (var piece in BoundaryPieces.Split(b, new[] { a }, tolerance))
        {
            if (BoundaryPieces.Locate(piece, a, tolerance) == PieceLocation.Outside)
                pieces.Add(piece);
        }

        return Build<T>(pieces, frame, first.Srid, tolerance);
    }

    private static (LocalMetricFrame Frame, PlanarPolygon First, PlanarPolygon Second) Planar<T>(Geometry<T> first, Geometry<T> second, bool isGeodetic) where T : IPoint, new()
    {
        if (first is null)
            throw new ArgumentNullException(nameof(first));

        if (second is null)
            throw new ArgumentNullException(nameof(second));

        var box = first.GetBoundingBox().Add(second.GetBoundingBox());
        var frame = LocalMetricFrame.At(box.Center, isGeodetic);

        return (frame, PlanarPolygon.From(first, frame), PlanarPolygon.From(second, frame));
    }

    /// <summary>Chains the pieces into rings, puts each hole in the smallest outer ring around it, and makes the geometry.</summary>
    private static Geometry<T>? Build<T>(List<RingPiece> pieces, LocalMetricFrame frame, int srid, double tolerance) where T : IPoint, new()
    {
        var rings = new List<(double X, double Y)[]>();

        foreach (var loop in PieceLoops.Chain(pieces, p => p, Math.Max(tolerance, 1e-9)))
        {
            var ring = new List<(double X, double Y)>();

            foreach (var piece in loop)
            {
                if (ring.Count == 0 || Distance(ring[ring.Count - 1], piece.Start) > tolerance)
                    ring.Add(piece.Start);
            }

            if (ring.Count > 1 && Distance(ring[ring.Count - 1], ring[0]) <= tolerance)
                ring.RemoveAt(ring.Count - 1);

            if (ring.Count >= 3 && Math.Abs(PlanarPolygon.SignedArea(ring.ToArray())) > 1e-9)
                rings.Add(ring.ToArray());
        }

        var outers = rings.Where(r => PlanarPolygon.SignedArea(r) > 0).OrderBy(r => PlanarPolygon.SignedArea(r)).ToList();
        var holes = rings.Where(r => PlanarPolygon.SignedArea(r) < 0).ToList();

        if (outers.Count == 0)
            return null;

        var holesOf = outers.ToDictionary(o => o, _ => new List<(double X, double Y)[]>());

        foreach (var hole in holes)
        {
            // Smallest outer first, so a hole goes to the ring right around it.
            var owner = outers.FirstOrDefault(o => Contains(o, hole[0].X, hole[0].Y) || Contains(o, Middle(hole[0], hole[1]).X, Middle(hole[0], hole[1]).Y));

            if (owner is not null)
                holesOf[owner].Add(hole);
        }

        Geometry<T> Ring((double X, double Y)[] ring)
            => Geometry<T>.CreatePolygonRing(ring.Select(p => frame.FromMetres(p.X, p.Y)).Select(c => new T { X = c.X, Y = c.Y }).ToList(), srid);

        var polygons = outers
            .OrderByDescending(o => PlanarPolygon.SignedArea(o))
            .Select(o => Geometry<T>.Create(new[] { Ring(o) }.Concat(holesOf[o].Select(Ring)).ToList(), GeometryType.Polygon, srid))
            .ToList();

        return polygons.Count == 1 ? polygons[0] : Geometry<T>.Create(polygons, GeometryType.MultiPolygon, srid);
    }

    private static double Distance((double X, double Y) p, (double X, double Y) q) => Math.Sqrt((p.X - q.X) * (p.X - q.X) + (p.Y - q.Y) * (p.Y - q.Y));

    private static (double X, double Y) Middle((double X, double Y) p, (double X, double Y) q) => ((p.X + q.X) / 2, (p.Y + q.Y) / 2);

    private static bool Contains((double X, double Y)[] ring, double x, double y)
    {
        var inside = false;

        for (int i = 0, j = ring.Length - 1; i < ring.Length; j = i++)
        {
            var (xi, yi) = ring[i];
            var (xj, yj) = ring[j];

            if ((yi > y) != (yj > y) && x < (xj - xi) * (y - yi) / (yj - yi) + xi)
                inside = !inside;
        }

        return inside;
    }
}
