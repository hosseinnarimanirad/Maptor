using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Common.Mathematics;
using IRI.Maptor.Core.Spatial.Topology;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Common.Abstractions;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Common.Enums;

namespace IRI.Maptor.Core.Spatial.Helpers;

public class TopologyUtility
{
    #region Point-Line, Point-Circle

    public static Point CalculateCircumcenterCenterPoint<T>(T firstPoint, T secondPoint, T thirdPoint) where T : IPoint, new()
    {
        double x1 = firstPoint.X; double y1 = firstPoint.Y;

        double x2 = secondPoint.X; double y2 = secondPoint.Y;

        double x3 = thirdPoint.X; double y3 = thirdPoint.Y;

        double temp = 2 * (x1 * (y2 - y3) + x2 * (y3 - y1) + x3 * (y1 - y2));

        double tempX = ((y1 * y1 + x1 * x1) * (y2 - y3) + (y2 * y2 + x2 * x2) * (y3 - y1) + (y3 * y3 + x3 * x3) * (y1 - y2)) / temp;

        double tempY = ((y1 * y1 + x1 * x1) * (x3 - x2) + (y2 * y2 + x2 * x2) * (x1 - x3) + (y3 * y3 + x3 * x3) * (x2 - x1)) / temp;

        return new Point(tempX, tempY);
    }

    public static PointCircleRelation GetPointCircleRelation<T>(T sightlyPoint, T point1, T point2, T point3) where T : IPoint, new()
    {
        // check if point1, point2, point3 are in ccw form
        double tempValue = (point1.X - point3.X) * (point2.Y - point3.Y) - (point2.X - point3.X) * (point1.Y - point3.Y);

        bool isCounterClockWise = tempValue > 0;

        double x0, x1, x2, x3, y0, y1, y2, y3;

        if (Math.Abs(tempValue) < double.Epsilon)
        {
            throw new NotImplementedException();
        }
        else if (isCounterClockWise)
        {
            x0 = sightlyPoint.X; y0 = sightlyPoint.Y;

            x1 = point1.X; y1 = point1.Y;

            x2 = point2.X; y2 = point2.Y;

            x3 = point3.X; y3 = point3.Y;
        }
        else
        {
            x0 = sightlyPoint.X; y0 = sightlyPoint.Y;

            x1 = point1.X; y1 = point1.Y;

            x2 = point3.X; y2 = point3.Y;

            x3 = point2.X; y3 = point2.Y;
        }

        Matrix tempMatrix = new Matrix(
                               [
                                    [x1 - x0, x2 - x0, x3 - x0],
                                    [y1 - y0, y2 - y0, y3 - y0],
                                    [ x1 * x1 - x0 * x0 + y1 * y1 - y0 * y0,
                                        x2 * x2 - x0 * x0 + y2 * y2 - y0 * y0,
                                        x3 * x3 - x0 * x0 + y3 * y3 - y0 * y0 ]
                                ]);

        double result = tempMatrix.Determinant;

        return result > 0 ? PointCircleRelation.In : result == 0 ? PointCircleRelation.On : PointCircleRelation.Out;
    }

    /// <summary>
    /// Check whether the sightly point is in the direction of the vector or not
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="sightlyPoint"></param>
    /// <param name="startVertex"></param>
    /// <param name="endVertex"></param>
    /// <param name="tolerance">Maximum absolute cross-product value still classified as LiesOnTheLine; 0 (default) means exact.</param>
    /// <returns></returns>
    public static PointVectorRelation GetPointVectorRelation<T>(T sightlyPoint, T startVertex, T endVertex, double tolerance = 0) where T : IPoint, new()
    {
        double tempValue = (startVertex.X - sightlyPoint.X) * (endVertex.Y - sightlyPoint.Y) -
                            (endVertex.X - sightlyPoint.X) * (startVertex.Y - sightlyPoint.Y);

        if (Math.Abs(tempValue) <= tolerance)
        {
            return PointVectorRelation.LiesOnTheLine;
        }
        else if (tempValue > 0)
        {
            return PointVectorRelation.LiesLeft;
        }
        else if (tempValue < 0)
        {
            return PointVectorRelation.LiesRight;
        }

        return PointVectorRelation.LiesOnTheLine;
    }

    //public static PoinTriangleRelation GetPointTriangleRelation(Point sightlyPoint, Point firstVertex, Point secondVertex, Point thirdVertex)
    //{
    //    int firstRelation = (int)TopologyUtility.GetPointVectorRelation(sightlyPoint, firstVertex, secondVertex);

    //    int secondRelation = (int)TopologyUtility.GetPointVectorRelation(sightlyPoint, secondVertex, thirdVertex);

    //    int thirdRelation = (int)TopologyUtility.GetPointVectorRelation(sightlyPoint, thirdVertex, firstVertex);

    //    return (PoinTriangleRelation)(firstRelation * QuasiTriangle.firstEdgeWeight +
    //                                    secondRelation * QuasiTriangle.secondEdgeWeight +
    //                                    thirdRelation * QuasiTriangle.thirdEdgeWeight);
    //}

    public static bool PointIntersectsLineSegment<T>(T sightlyPoint, T startVertex, T endVertex) where T : IPoint, new()
    {
        if (GetPointVectorRelation(sightlyPoint, startVertex, endVertex) == PointVectorRelation.LiesOnTheLine)
        {
            if (sightlyPoint.X > startVertex.X && sightlyPoint.X > endVertex.X ||
               sightlyPoint.Y > startVertex.Y && sightlyPoint.Y > endVertex.Y ||
               sightlyPoint.X < startVertex.X && sightlyPoint.X < endVertex.X ||
               sightlyPoint.Y < startVertex.Y && sightlyPoint.Y < endVertex.Y)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        return false;
    }

    #endregion

    public static bool CalculateIntersection<T>(T firstLinePoint, double firstLineSlope, T secondLinePoint, double secondLineSlope) where T : IPoint, new()
    {
        throw new NotImplementedException();
    }

    // line - line
    public static LineLineRelation CalculateIntersection<T>(
        T firstLineFirstPoint,
        T firstLineSecondPoint,
        T secondLineFirstPoint,
        T secondLineSecondPoint,
        out T intersection) where T : IPoint, new()
    {
        if (firstLineFirstPoint.Equals(firstLineSecondPoint) || secondLineFirstPoint.Equals(secondLineSecondPoint))
            throw new NotImplementedException();
        // precision issues!
        double firstSlope = SpatialUtility.GetSlope(firstLineFirstPoint, firstLineSecondPoint);

        double secondSlope = SpatialUtility.GetSlope(secondLineFirstPoint, secondLineSecondPoint);

        if (firstSlope - secondSlope == 0 || double.IsInfinity(firstSlope) && double.IsInfinity(secondSlope))
        {
            if (GetPointVectorRelation(firstLineFirstPoint, secondLineFirstPoint, secondLineSecondPoint) == PointVectorRelation.LiesOnTheLine)
            {
                intersection = new T() { X = double.NaN, Y = double.NaN };

                return LineLineRelation.Coinciding;
            }
            else
            {
                intersection = new T() { X = double.PositiveInfinity, Y = double.PositiveInfinity };

                return LineLineRelation.Parallel;
            }
        }

        double tempX, tempY;

        if (double.IsInfinity(firstSlope))
        {
            tempX = firstLineFirstPoint.X;

            tempY = secondLineFirstPoint.Y + secondSlope * (tempX - secondLineFirstPoint.X);
        }
        else if (double.IsInfinity(secondSlope))
        {
            tempX = secondLineFirstPoint.X;

            tempY = firstLineFirstPoint.Y + firstSlope * (tempX - firstLineFirstPoint.X);
        }
        else
        {
            tempX = (firstLineFirstPoint.Y -
                        secondLineFirstPoint.Y +
                        secondSlope * secondLineFirstPoint.X -
                        firstSlope * firstLineFirstPoint.X) / (secondSlope - firstSlope);

            tempY = secondLineFirstPoint.Y + secondSlope * (tempX - secondLineFirstPoint.X);
        }

        intersection = new T() { X = tempX, Y = tempY };

        return LineLineRelation.Intersect;
    }

    // segment - segment
    public static LineLineSegmentRelation LineSegmentsIntersects<T>(
        T firstSegmentFirstPoint,
        T firstSegmentSecondPoint,
        T secondSegmentFirstPoint,
        T secondSegmentSecondPoint,
        out T intersection) where T : IPoint, new()
    {
        LineLineRelation relation =
            CalculateIntersection(firstSegmentFirstPoint, firstSegmentSecondPoint, secondSegmentFirstPoint, secondSegmentSecondPoint, out intersection);

        if (relation == LineLineRelation.Intersect)
        {
            if (BoundingBox.Create(secondSegmentFirstPoint, secondSegmentSecondPoint).CoversApproximately(intersection) &&
                BoundingBox.Create(firstSegmentFirstPoint, firstSegmentSecondPoint).CoversApproximately(intersection))
                return LineLineSegmentRelation.Intersect;

            else
                return LineLineSegmentRelation.Nothing;

            //if ((intersection.X > startOfLineSegment.X && intersection.X > endOfLineSegment.X) ||
            //    (intersection.Y > startOfLineSegment.Y && intersection.Y > endOfLineSegment.Y) ||
            //    (intersection.X < startOfLineSegment.X && intersection.X < endOfLineSegment.X) ||
            //    (intersection.Y < startOfLineSegment.Y && intersection.Y < endOfLineSegment.Y))
            //    return LineLineSegmentRelation.Nothing;
            //else
            //    return LineLineSegmentRelation.Intersect;
        }
        else if (relation == LineLineRelation.Coinciding)
        {
            return LineLineSegmentRelation.Coinciding;
        }
        else if (relation == LineLineRelation.Parallel)
        {
            return LineLineSegmentRelation.Parallel;
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    ////todo: halate montabeg shodan dar nazar gerefte nashode!
    //public static bool DoesIntersectDirectionTheLineSegment<T>(
    //    T startOfDirection,
    //    T endOfDirection,
    //    T startOfLine,
    //    T endOfLine,
    //    out T intersection) where T : IPoint, new()
    //{
    //    if (IntersectLineWithLineSegment(startOfDirection, endOfDirection, startOfLine, endOfLine, out intersection) == LineLineSegmentRelation.Intersect)
    //    {
    //        if (endOfDirection.X != startOfDirection.X)
    //        {
    //            return (intersection.X - startOfDirection.X) / (endOfDirection.X - startOfDirection.X) > 1;
    //        }
    //        else if (endOfDirection.Y != startOfDirection.Y)
    //        {
    //            return (intersection.Y - startOfDirection.Y) / (endOfDirection.Y - startOfDirection.Y) > 1;
    //        }
    //        else
    //        {
    //            throw new NotImplementedException();
    //        }
    //    }
    //    else
    //    {
    //        return false;
    //    }
    //}



    /// <summary>
    /// Point-in-ring test using ray casting (no trig). Points on the ring boundary are <strong>not</strong> considered inside (strict interior).
    /// Optionally pass a precomputed envelope to avoid rebuilding it.
    /// </summary>
    public static bool IsPointInRing<T>(Geometry<T> ring, T point, BoundingBox? ringBoundingBox = null) where T : IPoint, new()
    {
        if (ring.IsNullOrEmpty() || point is null)
            return false;

        var numberOfPoints = ring.Points.Count;

        if (ring.Type != GeometryType.LineString || numberOfPoints < 3)
            throw new NotImplementedException("SpatialUtility.cs > IsPointInPolygon");

        var boundingBox = ringBoundingBox ?? ring.GetBoundingBox();

        if (!boundingBox.Covers(point))
            return false;

        return IsPointInPolygonRayCasting(ring.Points, point);
    }

    /// <summary>
    /// Even–odd ray casting (+X); boundary points are not inside. Half-open y-interval on edges (<c>ymin &lt; py &lt;= ymax</c>)
    /// avoids double-counting when the scanline passes through a vertex. Assumes a valid simple ring (no repeated closing vertex, no self-intersection).
    /// </summary>
    private static bool IsPointInPolygonRayCasting<T>(List<T> points, T point) where T : IPoint, new()
    {
        int n = points.Count;
        double px = point.X;
        double py = point.Y;

        for (int i = 0; i < n; i++)
        {
            var a = points[i];
            var b = points[(i + 1) % n];
            if (PointIntersectsLineSegment(point, a, b))
                return false;
        }

        bool inside = false;

        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            double xi = points[i].X;
            double yi = points[i].Y;
            double xj = points[j].X;
            double yj = points[j].Y;

            if (yi == yj)
                continue;

            double ymin = yi < yj ? yi : yj;
            double ymax = yi < yj ? yj : yi;

            if (ymin < py && py <= ymax)
            {
                double xIntersect = xi + (py - yi) / (yj - yi) * (xj - xi);
                if (px < xIntersect)
                    inside = !inside;
            }
        }

        return inside;
    }

    /// <summary>
    /// Original point-in-ring test: sum of signed inner angles at the query point (uses trig per edge).
    /// Uses the legacy heuristic <c>|totalAngle| &gt; π/2</c>. Prefer <see cref="IsPointInRing{T}(Geometry{T}, T, BoundingBox?)"/> for performance.
    /// </summary>
    public static bool IsPointInRingUsingSignedAngles<T>(Geometry<T> ring, T point, BoundingBox? ringBoundingBox = null) where T : IPoint, new()
    {
        if (ring.IsNullOrEmpty() || point is null)
            return false;

        var numberOfPoints = ring.Points.Count;

        if (ring.Type != GeometryType.LineString || numberOfPoints < 3)
            throw new NotImplementedException("SpatialUtility.cs > IsPointInPolygon");

        var boundingBox = ringBoundingBox ?? ring.GetBoundingBox();

        if (!boundingBox.Covers(point))
            return false;

        double totalAngle = 0.0;

        for (int i = 0; i < numberOfPoints - 1; i++)
        {
            totalAngle += SpatialUtility.GetSignedInnerAngle(ring.Points[i], point, ring.Points[i + 1]);
        }

        totalAngle += SpatialUtility.GetSignedInnerAngle(ring.Points[numberOfPoints - 1], point, ring.Points[0]);

        if (Math.Abs(totalAngle) > Math.PI / 2.0)
            return true;

        return false;
    }

    public static bool IsPointInPolygon<T>(Geometry<T> polygonOrMultiPolygon, T point) where T : IPoint, new()
    {
        if (polygonOrMultiPolygon.Type == GeometryType.MultiPolygon)
            return polygonOrMultiPolygon.Geometries.Any(g => IsPointInPolygon(g, point));

        else if (polygonOrMultiPolygon.Type != GeometryType.Polygon)
            throw new NotImplementedException("TopologyUtility > IsPointInPolygon");

        var inOuterRing = IsPointInRing(polygonOrMultiPolygon.Geometries[0], point);

        if (inOuterRing && polygonOrMultiPolygon.Geometries.Count > 1)
        {
            var inInnerRings = polygonOrMultiPolygon.Geometries.Skip(1).Any(g => IsPointInRing(g, point));

            return !inInnerRings;
        }

        return inOuterRing;
    }

    public static bool IsPointOnLineString<T>(Geometry<T> lineString, T point) where T : IPoint, new()
    {
        if (lineString.IsNullOrEmpty() || point is null)
            return false;

        var numberOfPoints = lineString.Points.Count;

        if (lineString.Type != GeometryType.LineString || numberOfPoints < 2)
            throw new NotImplementedException("SpatialUtility.cs > IsPointOnLineString");

        var boundingBox = lineString.GetBoundingBox();

        var doesEncompass = boundingBox.Covers(point);

        if (!doesEncompass)
            return false;

        for (int i = 0; i < numberOfPoints - 1; i++)
        {
            if (PointIntersectsLineSegment(point, lineString.Points[i], lineString.Points[i + 1]))
                return true;
        }

        return false;
    }

    public static bool LineSegmentIntersectsLineStringOrRing<T>(Geometry<T> lineStringOrRing, T lineSegmentStart, T lineSegmentEnd, bool isRing) where T : IPoint, new()
    {
        if (lineStringOrRing.IsNullOrEmpty() || lineSegmentStart is null || lineSegmentEnd is null)
            return false;

        var numberOfPoints = lineStringOrRing.Points.Count;

        if (lineStringOrRing.Type != GeometryType.LineString || numberOfPoints < 2)
            throw new NotImplementedException("TopologyUtility > IsPointOnLineString");

        var boundingBox = lineStringOrRing.GetBoundingBox();

        if (!boundingBox.Intersects(BoundingBox.Create(lineSegmentStart, lineSegmentEnd)))
            return false;

        for (int i = 0; i < numberOfPoints - 1; i++)
        {
            var relation = LineSegmentsIntersects(lineStringOrRing.Points[i], lineStringOrRing.Points[i + 1], lineSegmentStart, lineSegmentEnd, out _);

            if (relation == LineLineSegmentRelation.Intersect || relation == LineLineSegmentRelation.Coinciding)
                return true;
        }

        // consider ring scenario
        if (isRing)
        {
            var relation = LineSegmentsIntersects(lineStringOrRing.Points[0], lineStringOrRing.Points[numberOfPoints - 1], lineSegmentStart, lineSegmentEnd, out _);

            if (relation == LineLineSegmentRelation.Intersect || relation == LineLineSegmentRelation.Coinciding)
                return true;
        }

        return false;
    }

    #region Point - Ring/Polygon Boundary

    /// <summary>
    /// Distance from a point to a line segment (perpendicular distance clamped to the segment).
    /// </summary>
    public static double GetPointToLineSegmentDistance<T>(T point, T segmentStart, T segmentEnd) where T : IPoint, new()
    {
        double dx = segmentEnd.X - segmentStart.X;
        double dy = segmentEnd.Y - segmentStart.Y;

        double squaredLength = dx * dx + dy * dy;

        if (squaredLength == 0)
            return SpatialUtility.GetEuclideanLength(point, segmentStart);

        double t = ((point.X - segmentStart.X) * dx + (point.Y - segmentStart.Y) * dy) / squaredLength;

        t = Math.Max(0, Math.Min(1, t));

        double projectedX = segmentStart.X + t * dx;
        double projectedY = segmentStart.Y + t * dy;

        return Math.Sqrt((point.X - projectedX) * (point.X - projectedX) + (point.Y - projectedY) * (point.Y - projectedY));
    }

    /// <summary>
    /// Distance-tolerant point-on-ring test. The ring is expected to be stored without the
    /// repeated closing vertex; the closing segment is checked implicitly.
    /// </summary>
    public static bool IsPointOnRing<T>(Geometry<T> ring, T point, double tolerance = SpatialUtility.EpsilonDistance) where T : IPoint, new()
    {
        if (ring.IsNullOrEmpty() || point is null)
            return false;

        var points = ring.Points;
        var numberOfPoints = points.Count;

        if (ring.Type != GeometryType.LineString || numberOfPoints < 3)
            throw new NotImplementedException("TopologyUtility > IsPointOnRing");

        for (int i = 0; i < numberOfPoints; i++)
        {
            if (GetPointToLineSegmentDistance(point, points[i], points[(i + 1) % numberOfPoints]) < tolerance)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Distance-tolerant test for a point lying on the boundary (any ring) of a polygon or multipolygon.
    /// </summary>
    public static bool IsPointOnPolygonBoundary<T>(Geometry<T> polygonOrMultiPolygon, T point, double tolerance = SpatialUtility.EpsilonDistance) where T : IPoint, new()
    {
        if (polygonOrMultiPolygon.IsNullOrEmpty() || point is null)
            return false;

        if (polygonOrMultiPolygon.Type == GeometryType.MultiPolygon)
            return polygonOrMultiPolygon.Geometries.Any(g => IsPointOnPolygonBoundary(g, point, tolerance));

        if (polygonOrMultiPolygon.Type != GeometryType.Polygon)
            throw new NotImplementedException("TopologyUtility > IsPointOnPolygonBoundary");

        return polygonOrMultiPolygon.Geometries.Any(ring => IsPointOnRing(ring, point, tolerance));
    }

    /// <summary>
    /// OGC-style point-polygon intersection test: true when the point is in the interior
    /// or on the boundary of the polygon (<see cref="IsPointInPolygon{T}"/> alone is strict-interior).
    /// </summary>
    public static bool IsPointInPolygonOrOnBoundary<T>(Geometry<T> polygonOrMultiPolygon, T point) where T : IPoint, new()
    {
        return IsPointInPolygon(polygonOrMultiPolygon, point) || IsPointOnPolygonBoundary(polygonOrMultiPolygon, point);
    }

    #endregion
}
