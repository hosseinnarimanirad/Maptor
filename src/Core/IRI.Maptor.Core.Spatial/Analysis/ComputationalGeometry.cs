// BESMELLAHE RAHMANE RAHIM
// ALLAHOMMA AJJEL LE-VALIYEK AL-FARAJ

using IRI.Maptor.Core.Spatial.Topology;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Helpers;

namespace IRI.Maptor.Core.Spatial.Analysis;

public static class ComputationalGeometry
{
    /// <summary>
    /// Computes the convex hull of the given points using a Graham scan.
    /// Returns the hull vertices in counter-clockwise order, starting at the
    /// lowest point (ties broken by lowest X). Duplicate input points are
    /// ignored; collinear points interior to hull edges are excluded.
    /// Degenerate inputs (0, 1 or 2 distinct points) are returned as-is.
    /// </summary>
    public static List<Point> CreateConvexHull(List<Point> points)
    {
        if (points is null || points.Count == 0)
            return new List<Point>();

        // dedup by coordinate value (Point overrides Equals/GetHashCode on X,Y)
        var seen = new HashSet<Point>();
        var distinct = new List<Point>(points.Count);
        foreach (var p in points)
            if (seen.Add(p))
                distinct.Add(p);

        if (distinct.Count <= 2)
            return distinct.Select(p => new Point(p.X, p.Y)).ToList();

        // pivot: min Y, ties by min X
        int pivotIndex = 0;
        for (int i = 1; i < distinct.Count; i++)
            if (distinct[i].Y < distinct[pivotIndex].Y ||
               (distinct[i].Y == distinct[pivotIndex].Y && distinct[i].X < distinct[pivotIndex].X))
                pivotIndex = i;

        Point pivot = distinct[pivotIndex];

        // candidate indices (everything except the pivot)
        int[] candidates = new int[distinct.Count - 1];
        int counter = 0;
        for (int i = 0; i < distinct.Count; i++)
            if (i != pivotIndex)
                candidates[counter++] = i;

        // ascending polar angle around the pivot; collinear ties nearest-first.
        Array.Sort(candidates, (i, j) =>
        {
            var pi = distinct[i];
            var pj = distinct[j];

            double cross = (pi.X - pivot.X) * (pj.Y - pivot.Y) - (pj.X - pivot.X) * (pi.Y - pivot.Y);

            if (cross > 0) return -1;   // pi has the smaller angle
            if (cross < 0) return 1;

            double dxi = pi.X - pivot.X, dyi = pi.Y - pivot.Y;
            double dxj = pj.X - pivot.X, dyj = pj.Y - pivot.Y;

            double di = dxi * dxi + dyi * dyi;
            double dj = dxj * dxj + dyj * dyj;

            return di < dj ? -1 : (di > dj ? 1 : 0); // 0 unreachable after dedup
        });

        var result = new List<Point> { pivot };

        counter = 0;

        while (counter < candidates.Length)
        {
            Point tempPoint = distinct[candidates[counter]];

            if (result.Count < 2)
            {
                result.Add(tempPoint);

                counter++;

                continue;
            }

            PointVectorRelation pointSituation = TopologyUtility.GetPointVectorRelation(tempPoint, result[result.Count - 2], result[result.Count - 1]);

            if (pointSituation == PointVectorRelation.LiesLeft)
            {
                result.Add(tempPoint);

                counter++;
            }
            else // LiesRight or LiesOnTheLine: top of stack is not a strict hull vertex
            {
                result.RemoveAt(result.Count - 1);
            }
        }

        // fresh copies: Point is mutable; never alias caller-owned instances
        return result.Select(p => new Point(p.X, p.Y)).ToList();
    }
}
