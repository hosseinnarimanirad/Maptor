using IRI.Maptor.Core.Common.Abstractions;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Core.Spatial.Analysis;

/// <summary>How much two polygons overlap, measured in metres.</summary>
public sealed class OverlapMeasure<T> where T : IPoint, new()
{
    public OverlapMeasure(double area, double perimeter, T? centroid, double firstArea, double secondArea)
    {
        Area = area;
        Perimeter = perimeter;
        Centroid = centroid;
        FirstArea = firstArea;
        SecondArea = secondArea;
    }

    /// <summary>The shared area (square metres when geodetic).</summary>
    public double Area { get; }

    /// <summary>The length around the shared area.</summary>
    public double Perimeter { get; }

    /// <summary>The centre of the shared area, in the data's coordinates; null when there is no area.</summary>
    public T? Centroid { get; }

    /// <summary>A typical width of the shared area: 2 × area ÷ perimeter, which is the width of a long thin strip.</summary>
    public double Width => Perimeter <= 0 ? 0 : 2 * Area / Perimeter;

    public double FirstArea { get; }

    public double SecondArea { get; }
}

/// <summary>
/// The overlap of two polygons, measured without building the intersection geometry: the boundary of the shared
/// area is made of the pieces of each boundary that lie inside the other polygon, plus the stretches both share
/// running the same way; Green's theorem along those pieces gives the area, the perimeter and the centre. This
/// stays right where the boundaries share edges or vertices, which is the usual case for neighbouring parcels.
/// Both polygons are flattened into one <see cref="LocalMetricFrame"/> at the centre of their boxes.
/// </summary>
public static class PolygonOverlap
{
    /// <param name="first">A polygon or multipolygon.</param>
    /// <param name="second">A polygon or multipolygon.</param>
    /// <param name="isGeodetic">True for WGS84 longitude/latitude.</param>
    /// <param name="tolerance">
    /// Boundaries closer than this (metres) count as the same line. Keep it tiny (e.g. 1e-6): every piece of the
    /// shared area's boundary must be found for the area to be right, so a very thin overlap is measured as it is
    /// and judged afterwards by its <see cref="OverlapMeasure{T}.Width"/>.
    /// </param>
    public static OverlapMeasure<T> Measure<T>(Geometry<T> first, Geometry<T> second, bool isGeodetic, double tolerance) where T : IPoint, new()
    {
        if (first is null)
            throw new ArgumentNullException(nameof(first));

        if (second is null)
            throw new ArgumentNullException(nameof(second));

        var box = first.GetBoundingBox().Add(second.GetBoundingBox());
        var frame = LocalMetricFrame.At(box.Center, isGeodetic);

        var a = PlanarPolygon.From(first, frame);
        var b = PlanarPolygon.From(second, frame);

        var boundary = new List<RingPiece>();

        foreach (var piece in BoundaryPieces.Split(a, new[] { b }, tolerance))
        {
            var location = BoundaryPieces.Locate(piece, b, tolerance);

            if (location == PieceLocation.Inside || location == PieceLocation.OnSameDirection)
                boundary.Add(piece);
        }

        foreach (var piece in BoundaryPieces.Split(b, new[] { a }, tolerance))
        {
            if (BoundaryPieces.Locate(piece, a, tolerance) == PieceLocation.Inside)
                boundary.Add(piece);
        }

        double area = 0, perimeter = 0, cx = 0, cy = 0;

        foreach (var piece in boundary)
        {
            var (x1, y1) = piece.Start;
            var (x2, y2) = piece.End;

            var cross = x1 * y2 - x2 * y1;

            area += cross / 2;
            cx += (x1 + x2) * cross;
            cy += (y1 + y2) * cross;
            perimeter += piece.Length;
        }

        T? centroid = default;

        if (area > 1e-12)
        {
            var (x, y) = frame.FromMetres(cx / (6 * area), cy / (6 * area));
            centroid = new T { X = x, Y = y };
        }

        return new OverlapMeasure<T>(Math.Max(0, area), perimeter, centroid, a.Area, b.Area);
    }
}
