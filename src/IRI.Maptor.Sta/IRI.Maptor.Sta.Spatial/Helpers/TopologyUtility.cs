using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Mathematics;
using IRI.Maptor.Sta.Spatial.Topology;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.Spatial.Analysis;

namespace IRI.Maptor.Sta.Spatial.Helpers;

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
                               new double[][] {
                                    new double[] { x1 - x0, x2 - x0, x3 - x0 },
                                    new double[] { y1 - y0, y2 - y0, y3 - y0 },
                                    new double[] { x1 * x1 - x0 * x0 + y1 * y1 - y0 * y0,
                                                    x2 * x2 - x0 * x0 + y2 * y2 - y0 * y0,
                                                    x3 * x3 - x0 * x0 + y3 * y3 - y0 * y0 } });
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
    /// <returns></returns>
    public static PointVectorRelation GetPointVectorRelation<T>(T sightlyPoint, T startVertex, T endVertex) where T : IPoint, new()
    {
        double tempValue = (startVertex.X - sightlyPoint.X) * (endVertex.Y - sightlyPoint.Y) -
                            (endVertex.X - sightlyPoint.X) * (startVertex.Y - sightlyPoint.Y);

        if (Math.Abs(tempValue) < double.Epsilon)
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
        double firstSlope = SpatialUtility.CalculateSlope(firstLineFirstPoint, firstLineSecondPoint);

        double secondSlope = SpatialUtility.CalculateSlope(secondLineFirstPoint, secondLineSecondPoint);

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



    public static bool IsPointInRing<T>(Geometry<T> ring, T point) where T : IPoint, new()
    {
        if (ring.IsNullOrEmpty() || point is null)
            return false;

        var numberOfPoints = ring.Points.Count;

        if (ring.Type != GeometryType.LineString || numberOfPoints < 3)
            throw new NotImplementedException("SpatialUtility.cs > IsPointInPolygon");

        var boundingBox = ring.GetBoundingBox();

        var doesEncomapss = boundingBox.Covers(point);

        if (!doesEncomapss)
            return false;

        double totalAngle = 0.0;

        for (int i = 0; i < numberOfPoints - 1; i++)
        {
            var angle = SpatialUtility.GetSignedInnerAngle(ring.Points[i], point, ring.Points[i + 1]);

            totalAngle += angle;
        }

        totalAngle += SpatialUtility.GetSignedInnerAngle(ring.Points[numberOfPoints - 1], point, ring.Points[0]);

        //if (Math.Abs(Math.Abs(totalAngle) - 2 * Math.PI) < 0.1)
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

        var inOutterRing = IsPointInRing(polygonOrMultiPolygon.Geometries[0], point);

        if (inOutterRing && polygonOrMultiPolygon.Geometries.Count > 1)
        {
            var inInnerRings = polygonOrMultiPolygon.Geometries.Skip(1).Any(g => IsPointInRing(g, point));

            return !inInnerRings;
        }

        return inOutterRing;
    }

    public static bool IsPointOnLineString<T>(Geometry<T> lineString, T point) where T : IPoint, new()
    {
        if (lineString.IsNullOrEmpty() || point is null)
            return false;

        var numberOfPoints = lineString.Points.Count;

        if (lineString.Type != GeometryType.LineString || numberOfPoints < 2)
            throw new NotImplementedException("SpatialUtility.cs > IsPointOnLineString");

        var boundingBox = lineString.GetBoundingBox();

        var doesEncomapss = boundingBox.Covers(point);

        if (!doesEncomapss)
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
}
