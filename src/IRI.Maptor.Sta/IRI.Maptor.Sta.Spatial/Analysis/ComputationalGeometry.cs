// BESMELLAHE RAHMANE RAHIM
// ALLAHOMMA AJJEL LE-VALIYEK AL-FARAJ

using IRI.Maptor.Sta.DataStructures;
using IRI.Maptor.Sta.Spatial.Topology;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.DataStructures.CustomStructures;
using IRI.Maptor.Sta.Spatial.Helpers;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Sta.Spatial.Analysis;

public static class ComputationalGeometry
{
    public static PoinTriangleRelation GetPointTriangleRelation(Point sightlyPoint, Point firstVertex, Point secondVertex, Point thirdVertex)
    {
        int firstRelation = (int)TopologyUtility.GetPointVectorRelation(sightlyPoint, firstVertex, secondVertex);

        int secondRelation = (int)TopologyUtility.GetPointVectorRelation(sightlyPoint, secondVertex, thirdVertex);

        int thirdRelation = (int)TopologyUtility.GetPointVectorRelation(sightlyPoint, thirdVertex, firstVertex);

        return (PoinTriangleRelation)(firstRelation * QuasiTriangle.firstEdgeWeight +
                                        secondRelation * QuasiTriangle.secondEdgeWeight +
                                        thirdRelation * QuasiTriangle.thirdEdgeWeight);
    }

    public static PointCollection CreateConvexHull(PointCollection points)
    {
        int leftBoundIndex = points.LowerBoundIndex;

        Point initialPoint = points[leftBoundIndex];

        int length = points.Count;

        IndexValue<double>[] unsortedPoints = new IndexValue<double>[length - 1];

        int counter = 0;

        for (int i = 0; i < length; i++)
        {
            if (i == leftBoundIndex)
                continue;

            unsortedPoints[counter] = new IndexValue<double>(i,
                                                            Math.Atan2(points[i].Y - initialPoint.Y,
                                                            points[i].X - initialPoint.X));
            counter++;
        }

        IndexValue<double>[] sortedPoints = SortAlgorithm.Heapsort(unsortedPoints, SortDirection.Descending);

        PointCollection result = new PointCollection();

        result.Add(points[leftBoundIndex]);

        counter = 0;

        while (counter < sortedPoints.Length)
        {
            Point tempPoint = points[sortedPoints[counter].Index];

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
            else if (pointSituation == PointVectorRelation.LiesRight)
            {
                result.RemoveAt(result.Count - 1);
            }
            else
            {
                if (sortedPoints[counter].Value == sortedPoints[0].Value)
                {
                    //if (CalculateDistance(initialPoint, tempPoint) > CalculateDistance(initialPoint, result[result.Count - 1]))
                    if (initialPoint.DistanceTo(tempPoint) > initialPoint.DistanceTo(result[result.Count - 1]))
                    {
                        result.Add(tempPoint);
                    }

                    counter++;
                }
                else if (sortedPoints[counter].Value == sortedPoints[length - 2].Value)
                {
                    //if (CalculateDistance(initialPoint, tempPoint) < CalculateDistance(initialPoint, result[result.Count - 1]))
                    if (initialPoint.DistanceTo(tempPoint) < initialPoint.DistanceTo(result[result.Count - 1]))
                    {
                        result.Add(tempPoint);
                    }

                    counter++;
                }
                else
                {
                    result.RemoveAt(result.Count - 1);
                }
            }
        }

        return result;
    }

    public static List<int> GetConvexHullVertexes(PointCollection points)
    {
        int leftBoundIndex = points.LowerBoundIndex;

        Point initialPoint = points[leftBoundIndex];

        int length = points.Count;

        IndexValue<double>[] list = new IndexValue<double>[length - 1];

        int counter = 0;

        for (int i = 0; i < length; i++)
        {
            if (i == leftBoundIndex)
                continue;

            list[counter] = new IndexValue<double>(i,
                                                            Math.Atan2(points[i].Y - initialPoint.Y,
                                                            points[i].X - initialPoint.X));
            counter++;
        }

        list = SortAlgorithm.Heapsort(list, SortDirection.Descending);

        List<int> result = new List<int>();

        result.Add(leftBoundIndex);

        counter = 0;

        while (counter < list.Length)
        {
            Point tempPoint = points[list[counter].Index];

            if (result.Count < 2)
            {
                result.Add(list[counter].Index);

                counter++;

                continue;
            }

            PointVectorRelation pointSituation = TopologyUtility.GetPointVectorRelation(tempPoint,
                                                                                    points[result[result.Count - 2]],
                                                                                    points[result[result.Count - 1]]);

            if (pointSituation == PointVectorRelation.LiesLeft)
            {
                result.Add(list[counter].Index);

                counter++;
            }
            else if (pointSituation == PointVectorRelation.LiesRight)
            {
                result.RemoveAt(result.Count - 1);
            }
            else
            {
                if (list[counter].Value == list[0].Value &&
                    //CalculateDistance(initialPoint, tempPoint) > CalculateDistance(initialPoint, points[result[result.Count - 1]]))
                    initialPoint.DistanceTo(tempPoint) > initialPoint.DistanceTo(points[result[result.Count - 1]]))
                {
                    result.Add(list[counter].Index);

                    counter++;
                }
                else if (list[counter].Value == list[length - 2].Value &&
                    //CalculateDistance(initialPoint, tempPoint) < CalculateDistance(initialPoint, points[result[result.Count - 1]]))
                    initialPoint.DistanceTo(tempPoint) < initialPoint.DistanceTo(points[result[result.Count - 1]]))
                {
                    result.Add(list[counter].Index);

                    counter++;
                }
                else
                {
                    result.RemoveAt(result.Count - 1);
                }
            }
        }

        return result;
    }
}
