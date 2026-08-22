using IRI.Maptor.Core.Common.Abstractions;
using IRI.Maptor.Core.Common.Primitives;

namespace IRI.Maptor.Core.Spatial.Analysis;

public class GeomUtils<T> where T : IPoint, new()
{
    // Add necessary GeomUtils methods here
    private static bool Clockwise(T A, T B, T C)
    {
        // Determines if points A, B, & C are sequenced clockwise around a triangle
        return (C.Y - A.Y) * (B.X - A.X) > (B.Y - A.Y) * (C.X - A.X);
    }

    private static bool Intersect(T A, T B, T C, T D)
    {
        // Quickly determines if segment AB intersects segment CD
        return Clockwise(A, C, D) != Clockwise(B, C, D) && Clockwise(A, B, C) != Clockwise(A, B, D);

    }

    private static T Intersection(T A, T B, T C, T D, bool infinite = true)
    {
        double xp, yp;

        if (A.X == B.X)
        {
            if (C.X == D.X)
            {
                xp = yp = double.NaN; // lines are parallel
            }
            else // first line vertical
            {
                double b2 = (D.Y - C.Y) / (D.X - C.X);
                double a2 = C.Y - b2 * C.X;
                xp = A.X;
                yp = a2 + b2 * xp;
            }
        }
        else
        {
            if (C.X == D.X) // second line vertical
            {
                double b1 = (B.Y - A.Y) / (B.X - A.X);
                double a1 = A.Y - b1 * A.X;
                xp = C.X;
                yp = a1 + b1 * xp;
            }
            else // neither line vertical
            {
                double b1 = (B.Y - A.Y) / (B.X - A.X);
                double b2 = (D.Y - C.Y) / (D.X - C.X);
                double a1 = A.Y - b1 * A.X;
                double a2 = C.Y - b2 * C.X;
                if (b1 == b2)
                {
                    xp = yp = double.NaN; // lines are parallel
                }
                else
                {
                    xp = -(a1 - a2) / (b1 - b2);
                    yp = a1 + b1 * xp;
                }
            }
        }

        // test whether intersection point falls on either line
        if (!infinite && xp != null)
        {
            if ((A.X - xp) * (xp - B.X) < 0 || (C.X - xp) * (xp - D.X) < 0 || (A.Y - yp) * (yp - B.Y) < 0 || (C.Y - yp) * (yp - D.Y) < 0)
            {
                xp = yp = double.NaN;
            }
        }

        //if (xp != null) xp = (double)xp;
        //if (yp != null) yp = (double)yp;

        return new T() { X = xp, Y = yp };
    }

    private static double Area(List<T> pts, bool absolute)
    {
        // Ensure the last point is the same as the first point
        if (!pts[pts.Count - 1].Equals(pts[0]))
        {
            pts.Add(pts[0]);
        }

        // Compute the area
        var a = new List<double>();
        for (int i = 0; i < pts.Count - 1; i++)
        {
            double term = (pts[i + 1].X - pts[i].X) * (pts[i].Y + pts[i + 1].Y);
            a.Add(term);
        }
        double A = a.Sum() / 2;

        // Return the area
        return absolute ? Math.Abs(A) : A;
    }

    private static double DistancePtLine(T pt, T lineStart, T lineEnd)
    {
        double x0 = pt.X, y0 = pt.Y;
        double x1 = lineStart.X, y1 = lineStart.Y;
        double x2 = lineEnd.X, y2 = lineEnd.Y;
        double num = Math.Abs((y2 - y1) * x0 - (x2 - x1) * y0 + x2 * y1 - y2 * x1);
        double den = Math.Sqrt(Math.Pow(y2 - y1, 2) + Math.Pow(x2 - x1, 2));
        return num / den;
    }

    private static bool XRightOfAB(T A, T B, T X, double tolerance = 0)
    {

        // Determine area of ABC
        double ax = A.X, ay = A.Y;
        double bx = B.X, by = B.Y;
        double xx = X.X, xy = X.Y;
        double minx = Math.Min(Math.Min(ax, bx), xx);
        double miny = Math.Min(Math.Min(ay, by), xy);
        ax -= minx;
        bx -= minx;
        xx -= minx;
        ay -= miny;
        by -= miny;
        xy -= miny;
        double area = ax * xy + bx * ay + xx * by - ax * by - bx * xy - xx * ay;

        // Check precision - THIS MIGHT REALLY SLOW THINGS DOWN!
        if (tolerance > 0)
        {
            double d = DistancePtLine(X, A, B);
            if (d < tolerance)
            {
                ax = A.X;
                ay = A.Y;
                bx = B.X;
                by = B.Y;
                xx = X.X;
                xy = X.Y;
                minx = Math.Min(Math.Min(ax, bx), xx);
                miny = Math.Min(Math.Min(ay, by), xy);
                ax -= minx;
                bx -= minx;
                xx -= minx;
                ay -= miny;
                by -= miny;
                xy -= miny;
                area = ax * xy + bx * ay + xx * by - ax * by - bx * xy - xx * ay;
            }
        }

        // If area is positive, C is right of AB
        return area >= 0;
    }


    private static Tuple<T, T> EqualAreaLine(T A, T B, T C, T D)
    {
        double a = D.Y - A.Y;
        double b = A.X - D.X;
        double c = -A.X * B.Y + (A.Y - C.Y) * B.X + (B.Y - D.Y) * C.X + C.Y * D.X;

        if (a == 0)
        {
            if (b == 0)
            {
                return null;
            }
            else
            {
                double liney = -c / b;
                return Tuple.Create(new T() { X = A.X, Y = liney }, new T() { X = D.X, Y = liney });
            }
        }
        else if (b == 0)
        {
            double linex = -c / a;
            return Tuple.Create(new T() { X = linex, Y = A.Y }, new T() { X = linex, Y = D.Y });
        }
        else
        {
            if (Math.Abs(a) > Math.Abs(b))
            {
                return Tuple.Create(new T() { X = (-c - b * A.Y) / a, Y = A.Y }, new T() { X = (-c - b * D.Y) / a, Y = D.Y });
            }
            else
            {
                return Tuple.Create(new T() { X = A.X, Y = (-c - a * A.X) / b }, new T() { X = D.X, Y = (-c - a * D.X) / b });
            }
        }
    }

    // calculate areal displacement (SideshiftArea)
    public static double CalculateArealDisplacement(T pA, T pB, T pC, T pD, T pE, int overlapEndpt)
    {
        if (Intersect(pA, pB, pC, pD))
        {
            var x = Intersection(pA, pB, pC, pD, true);
            return Area(new List<T> { x, pC, pB }, true) + Area(new List<T> { x, pA, pE, pD }, true);
        }
        else if (overlapEndpt == 0)
        {
            if (Intersect(pB, pC, pE, pD))
            {
                var x = Intersection(pB, pC, pE, pD, true);
                return Area(new List<T> { pB, pE, x }, true) + Area(new List<T> { x, pD, pC }, true);
            }
            else
            {
                return Area(new List<T> { pD, pC, pB, pE }, true);
            }
        }
        else if (overlapEndpt == 1)
        {
            if (Intersect(pC, pB, pE, pA))
            {
                var x = Intersection(pC, pB, pE, pA, true);
                return Area(new List<T> { pC, pE, x }, true) + Area(new List<T> { x, pA, pB }, true);
            }
            else
            {
                return Area(new List<T> { pA, pB, pC, pE }, true);
            }
        }
        else
        {
            if (Intersect(pA, pE, pB, pC))
            {
                var x1 = Intersection(pA, pE, pB, pC, true);
                if (Intersect(pD, pE, pB, pC))
                {
                    var x2 = Intersection(pD, pE, pB, pC, true);
                    return Area(new List<T> { pA, x1, pB }, true) + Area(new List<T> { x1, pE, x2 }, true) + Area(new List<T> { x2, pD, pC }, true);
                }
                else
                {
                    return Area(new List<T> { pA, x1, pB }, true) + Area(new List<T> { x1, pE, pC, pD }, true);
                }
            }
            else if (Intersect(pA, pE, pC, pD))
            {
                var x = Intersection(pA, pE, pC, pD, true);
                return Area(new List<T> { pA, x, pC, pB }, true) + Area(new List<T> { x, pE, pD }, true);
            }
            else if (Intersect(pD, pE, pA, pB))
            {
                var x = Intersection(pD, pE, pA, pB, true);
                return Area(new List<T> { pA, pE, x }, true) + Area(new List<T> { x, pD, pC, pB }, true);
            }
            else if (Intersect(pD, pE, pB, pC))
            {
                var x = Intersection(pD, pE, pB, pC, true);
                return Area(new List<T> { pA, pE, x, pB }, true) + Area(new List<T> { x, pD, pC }, true);
            }
            else
            {
                return Area(new List<T> { pA, pE, pD, pC, pB }, true);
            }
        }
    }

    // calculate steiner point (E) PlacementAP_EAmin
    public static Tuple<T, double, int> CalculateNewPoint(T pA, T pB, T pC, T pD)
    {
        if (pA.Equals(pD))
        {
            return Tuple.Create(new T() { X = double.NaN, Y = double.NaN }, -1.0, -1);
        }

        // Get symmetry line
        var S = EqualAreaLine(pA, pB, pC, pD);

        // Determine situation
        if (S == null)
        {
            return Tuple.Create(new T() { X = double.NaN, Y = double.NaN }, -1.0, -1);
        }
        else if (DistancePtLine(pA, S.Item1, S.Item2) == 0)
        {
            return Tuple.Create(new T() { X = double.NaN, Y = double.NaN }, -1.0, -1);
        }
        else
        {
            // Default: intersect S with CD
            var C2 = pC;
            var D2 = pD;
            int overlapEndpt = 1;

            if (XRightOfAB(pA, pD, pB) == XRightOfAB(pA, pD, pC))
            {
                double BFurtherFromAD = DistancePtLine(pB, pA, pD) - DistancePtLine(pC, pA, pD);
                if (BFurtherFromAD > 0) // Intersect S with AB
                {
                    D2 = pA;
                    C2 = pB;
                    overlapEndpt = 0;
                }
            }
            else
            {
                if (XRightOfAB(pA, pD, pB) == XRightOfAB(pA, pD, S.Item1)) // Intersect S with AB
                {
                    D2 = pA;
                    C2 = pB;
                    overlapEndpt = 0;
                }
            }

            // Calculate intersection
            var pE = Intersection(D2, C2, S.Item1, S.Item2);

            // Handle case of collinear points
            if (pE.X == null)
            {
                pE = pC;
            }

            // Calculate displacement area
            double area;
            if (overlapEndpt == 1) // Intersect S with DC
            {
                area = CalculateArealDisplacement(pA, pB, pC, pD, pE, overlapEndpt);
            }
            else // Intersect S with AB
            {
                area = CalculateArealDisplacement(pD, pC, pB, pA, pE, overlapEndpt);
            }

            return Tuple.Create(pE, area, overlapEndpt);
        }
    }
}
