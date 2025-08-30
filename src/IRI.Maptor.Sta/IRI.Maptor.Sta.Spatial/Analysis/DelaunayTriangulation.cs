// BESMELLAHE RAHMANE RAHIM
// ALLAHOMMA AJJEL LE-VALIYEK AL-FARAJ

using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Topology;
using IRI.Maptor.Sta.Spatial.Helpers;
using IRI.Maptor.Sta.Spatial.Primitives;


namespace IRI.Maptor.Sta.Spatial.Analysis;

public class DelaunayTriangulation
{
    PointCollection points;

    int lowerLeftIndex, upperLeftIndex, upperRightIndex, lowerRightIndex;

    public QuasiTriangleCollection triangles;


    public DelaunayTriangulation(PointCollection points)
    {
        if (points.Count < 3)
            throw new NotImplementedException();

        InitializeMembers(points);

        CreateBigTriangles();

        for (int i = 0; i < points.Count - 4; i++)
        {
            List<int> queue;

            List<QuasiTriangle> temp = TraceTriangle(points[i], out queue);

            if (temp != null)
            {
                if (temp.Count == 1)
                {
                    TrisectTriangle(temp[0], points[i]);
                }
                else if (temp.Count == 2)
                {
                    DivideTriangles(temp, points[i]);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        RemoveBigTriangles();
    }

    private void InitializeMembers(PointCollection points)
    {
        this.points = points;

        triangles = new QuasiTriangleCollection();
    }

    //Modified
    private void CreateBigTriangles()
    {
        double height = points.MaxY - points.MinY;

        double width = points.MaxX - points.MinX;

        int scale = 1000;

        Point lowerLeft = new Point(points.MinX - scale * width, points.MinY - scale * height);

        Point upperLeft = new Point(points.MinX - scale * width, points.MaxY + scale * height);

        Point upperRight = new Point(points.MaxX + scale * width, points.MaxY + scale * height);

        Point lowerRight = new Point(points.MaxX + scale * width, points.MinY - scale * height);

        points.Add(lowerLeft); points.Add(upperLeft);

        points.Add(upperRight); points.Add(lowerRight);

        lowerLeftIndex = points[points.Count - 4].GetHashCode();

        upperLeftIndex = points[points.Count - 3].GetHashCode();

        upperRightIndex = points[points.Count - 2].GetHashCode();

        lowerRightIndex = points[points.Count - 1].GetHashCode();

        QuasiTriangle firstBigTriangle = new QuasiTriangle(lowerRightIndex, upperRightIndex, lowerLeftIndex, triangles.GetNewCode());

        QuasiTriangle secondBigTriangle = new QuasiTriangle(upperRightIndex, upperLeftIndex, lowerLeftIndex, triangles.GetNewCode() + 1);

        firstBigTriangle.SecondThirdNeighbour = secondBigTriangle.GetHashCode();

        secondBigTriangle.ThirdFirstNeighbour = firstBigTriangle.GetHashCode();

        triangles.Add(firstBigTriangle);

        triangles.Add(secondBigTriangle);
    }

    private void TrisectTriangle(QuasiTriangle temp, Point point)
    {
        QuasiTriangle firsTriangle = MakeCCWTriangle(temp.First, temp.Second, point.GetHashCode(), triangles.GetNewCode());

        triangles.Add(firsTriangle);

        QuasiTriangle secondTriangle = MakeCCWTriangle(temp.Second, temp.Third, point.GetHashCode(), triangles.GetNewCode());

        triangles.Add(secondTriangle);

        QuasiTriangle thirdTriangle = MakeCCWTriangle(temp.Third, temp.First, point.GetHashCode(), triangles.GetNewCode());

        triangles.Add(thirdTriangle);

        UpdateRelation(firsTriangle.GetHashCode(), secondTriangle.GetHashCode());

        UpdateRelation(firsTriangle.GetHashCode(), thirdTriangle.GetHashCode());

        UpdateRelation(secondTriangle.GetHashCode(), thirdTriangle.GetHashCode());

        if (temp.FirstSecondNeighbour != -1)
        {
            UpdateRelation(temp.FirstSecondNeighbour, firsTriangle.GetHashCode());

            Stack<KeyValuePair<QuasiEdge, QuasiTriangle>> stack = new Stack<KeyValuePair<QuasiEdge, QuasiTriangle>>();

            QuasiEdge edge = new QuasiEdge(temp.First, temp.Second);

            stack.Push(new KeyValuePair<QuasiEdge, QuasiTriangle>(edge, firsTriangle));

            CheckForSwapEdge(ref stack);
        }
        if (temp.SecondThirdNeighbour != -1)
        {
            UpdateRelation(temp.SecondThirdNeighbour, secondTriangle.GetHashCode());

            Stack<KeyValuePair<QuasiEdge, QuasiTriangle>> stack = new Stack<KeyValuePair<QuasiEdge, QuasiTriangle>>();

            QuasiEdge edge = new QuasiEdge(temp.Second, temp.Third);

            stack.Push(new KeyValuePair<QuasiEdge, QuasiTriangle>(edge, secondTriangle));

            CheckForSwapEdge(ref stack);
        }
        if (temp.ThirdFirstNeighbour != -1)
        {
            UpdateRelation(temp.ThirdFirstNeighbour, thirdTriangle.GetHashCode());

            Stack<KeyValuePair<QuasiEdge, QuasiTriangle>> stack = new Stack<KeyValuePair<QuasiEdge, QuasiTriangle>>();

            QuasiEdge edge = new QuasiEdge(temp.Third, temp.First);

            stack.Push(new KeyValuePair<QuasiEdge, QuasiTriangle>(edge, thirdTriangle));

            CheckForSwapEdge(ref stack);
        }

        triangles.RemoveByCode(temp.GetHashCode());

    }

    private void DivideTriangles(List<QuasiTriangle> trianglePair, Point point)
    {
        if (trianglePair[0] == null)
        {
            throw new NotImplementedException();
        }
        else if (trianglePair[1] == null)
        {
            DichotomizeTriangle(trianglePair[0], point);
        }
        else
        {
            DichotomizeTriangles(trianglePair[0], trianglePair[1], point);
        }

    }

    private void DichotomizeTriangle(QuasiTriangle triangle, Point point)
    {
        PoinTriangleRelation relation = GetPointTriangleRelation(triangle, point);

        QuasiEdge edge;

        if (relation == PoinTriangleRelation.OnFirstEdge)
        {
            edge = new QuasiEdge(triangle.First, triangle.Second);
        }
        else if (relation == PoinTriangleRelation.OnSecondEdge)
        {
            edge = new QuasiEdge(triangle.Second, triangle.Third);
        }
        else if (relation == PoinTriangleRelation.OnThirdEdge)
        {
            edge = new QuasiEdge(triangle.Third, triangle.First);
        }
        else
        {
            throw new NotImplementedException();
        }

        DichotomizeTriangle(triangle, edge, point);
    }

    private void DichotomizeTriangles(QuasiTriangle firstTrianglePair, QuasiTriangle secondTrianglePair, Point point)
    {
        PoinTriangleRelation relation = GetPointTriangleRelation(firstTrianglePair, point);

        QuasiEdge edge;

        if (relation == PoinTriangleRelation.OnFirstEdge)
        {
            edge = new QuasiEdge(firstTrianglePair.First, firstTrianglePair.Second);
        }
        else if (relation == PoinTriangleRelation.OnSecondEdge)
        {
            edge = new QuasiEdge(firstTrianglePair.Second, firstTrianglePair.Third);
        }
        else if (relation == PoinTriangleRelation.OnThirdEdge)
        {
            edge = new QuasiEdge(firstTrianglePair.Third, firstTrianglePair.First);
        }
        else
        {
            throw new NotImplementedException();
        }

        DichotomizeTriangles(firstTrianglePair, secondTrianglePair, edge, point);

    }

    private void DichotomizeTriangle(QuasiTriangle triangle, QuasiEdge edge, Point point)
    {
        int thirdPointCode = triangle.GetThirdPoint(edge);

        QuasiTriangle firstPart = MakeCCWTriangle(thirdPointCode, point.GetHashCode(), edge.First, triangles.GetNewCode());

        QuasiEdge firstEdge = new QuasiEdge(thirdPointCode, edge.First);

        int firstNeigbour = triangle.GetNeighbour(firstEdge);

        triangles.Add(firstPart);

        QuasiTriangle secondPart = MakeCCWTriangle(thirdPointCode, point.GetHashCode(), edge.Second, triangles.GetNewCode());

        QuasiEdge secondEdge = new QuasiEdge(thirdPointCode, edge.Second);

        int secondNeigbour = triangle.GetNeighbour(secondEdge);

        triangles.Add(secondPart);

        UpdateRelation(firstPart.GetHashCode(), secondPart.GetHashCode());

        if (firstNeigbour != -1)
        {
            UpdateRelation(firstNeigbour, firstPart.GetHashCode());

            Stack<KeyValuePair<QuasiEdge, QuasiTriangle>> stack = new Stack<KeyValuePair<QuasiEdge, QuasiTriangle>>();

            stack.Push(new KeyValuePair<QuasiEdge, QuasiTriangle>(firstEdge, firstPart));

            CheckForSwapEdge(ref stack);
        }

        if (secondNeigbour != -1)
        {
            UpdateRelation(secondNeigbour, secondPart.GetHashCode());

            Stack<KeyValuePair<QuasiEdge, QuasiTriangle>> stack = new Stack<KeyValuePair<QuasiEdge, QuasiTriangle>>();

            stack.Push(new KeyValuePair<QuasiEdge, QuasiTriangle>(secondEdge, secondPart));

            CheckForSwapEdge(ref stack);

        }

        triangles.RemoveByCode(triangle.GetHashCode());

    }

    private void DichotomizeTriangles(QuasiTriangle firstTrianglePair, QuasiTriangle secondTrianglePair, QuasiEdge edge, Point point)
    {
        int firstThirdPointCode = firstTrianglePair.GetThirdPoint(edge); int secondThirdPointCode = secondTrianglePair.GetThirdPoint(edge);

        QuasiTriangle firstPart = MakeCCWTriangle(firstThirdPointCode, point.GetHashCode(), edge.First, triangles.GetNewCode());

        triangles.Add(firstPart);

        QuasiTriangle secondPart = MakeCCWTriangle(firstThirdPointCode, point.GetHashCode(), edge.Second, triangles.GetNewCode());

        triangles.Add(secondPart);

        QuasiTriangle thirdPart = MakeCCWTriangle(secondThirdPointCode, point.GetHashCode(), edge.First, triangles.GetNewCode());

        triangles.Add(thirdPart);

        QuasiTriangle fourthPart = MakeCCWTriangle(secondThirdPointCode, point.GetHashCode(), edge.Second, triangles.GetNewCode());

        triangles.Add(fourthPart);

        QuasiEdge firstEdge = new QuasiEdge(firstThirdPointCode, edge.First); QuasiEdge secondEdge = new QuasiEdge(firstThirdPointCode, edge.Second);

        QuasiEdge thirdEdge = new QuasiEdge(secondThirdPointCode, edge.First); QuasiEdge fourthEdge = new QuasiEdge(secondThirdPointCode, edge.Second);

        int firstNeigbour = firstTrianglePair.GetNeighbour(firstEdge); int secondNeigbour = firstTrianglePair.GetNeighbour(secondEdge);

        int thirdNeigbour = secondTrianglePair.GetNeighbour(thirdEdge); int fourthNeigbour = secondTrianglePair.GetNeighbour(fourthEdge);

        //triangles.Add(firstPart); triangles.Add(secondPart);

        //triangles.Add(thirdPart); triangles.Add(fourthPart);

        UpdateRelation(firstPart.GetHashCode(), secondPart.GetHashCode()); UpdateRelation(firstPart.GetHashCode(), thirdPart.GetHashCode());

        UpdateRelation(secondPart.GetHashCode(), fourthPart.GetHashCode()); UpdateRelation(fourthPart.GetHashCode(), thirdPart.GetHashCode());

        if (firstNeigbour != -1)
        {
            UpdateRelation(firstNeigbour, firstPart.GetHashCode());

            Stack<KeyValuePair<QuasiEdge, QuasiTriangle>> stack = new Stack<KeyValuePair<QuasiEdge, QuasiTriangle>>();

            stack.Push(new KeyValuePair<QuasiEdge, QuasiTriangle>(firstEdge, firstPart));

            CheckForSwapEdge(ref stack);
        }
        if (secondNeigbour != -1)
        {
            UpdateRelation(secondNeigbour, secondPart.GetHashCode());

            Stack<KeyValuePair<QuasiEdge, QuasiTriangle>> stack = new Stack<KeyValuePair<QuasiEdge, QuasiTriangle>>();

            stack.Push(new KeyValuePair<QuasiEdge, QuasiTriangle>(secondEdge, secondPart));

            CheckForSwapEdge(ref stack);
        }
        if (thirdNeigbour != -1)
        {
            UpdateRelation(thirdNeigbour, thirdPart.GetHashCode());

            Stack<KeyValuePair<QuasiEdge, QuasiTriangle>> stack = new Stack<KeyValuePair<QuasiEdge, QuasiTriangle>>();

            stack.Push(new KeyValuePair<QuasiEdge, QuasiTriangle>(thirdEdge, thirdPart));

            CheckForSwapEdge(ref stack);
        }
        if (fourthNeigbour != -1)
        {
            UpdateRelation(fourthNeigbour, fourthPart.GetHashCode());

            Stack<KeyValuePair<QuasiEdge, QuasiTriangle>> stack = new Stack<KeyValuePair<QuasiEdge, QuasiTriangle>>();

            stack.Push(new KeyValuePair<QuasiEdge, QuasiTriangle>(fourthEdge, fourthPart));

            CheckForSwapEdge(ref stack);
        }

        triangles.RemoveByCode(firstTrianglePair.GetHashCode());

        triangles.RemoveByCode(secondTrianglePair.GetHashCode());
    }

    private void CheckForSwapEdge(ref Stack<KeyValuePair<QuasiEdge, QuasiTriangle>> stack)
    {
        if (stack.Count == 0)
            return;

        KeyValuePair<QuasiEdge, QuasiTriangle> currentValue = stack.Pop();

        QuasiTriangle currenTriangle = currentValue.Value;

        QuasiEdge currentEdge = currentValue.Key;

        QuasiTriangle neigbour = triangles.GetTriangle(GetNeighbour(currenTriangle, currentEdge));

        //if (neigbour != null)
        //{

        Point neighbourFarPoint = points.GetPoint(GetNeighbourFarPoint(currenTriangle, neigbour));

        if (TopologyUtility.GetPointCircleRelation(neighbourFarPoint,
                                                    points.GetPoint(currenTriangle.First),
                                                    points.GetPoint(currenTriangle.Second),
                                                    points.GetPoint(currenTriangle.Third)) == PointCircleRelation.In)
        {
            int thirdPoint = currenTriangle.GetThirdPoint(currentEdge);

            QuasiTriangle firstPart = MakeCCWTriangle(currentEdge.First, thirdPoint, neighbourFarPoint.GetHashCode(), triangles.GetNewCode());

            triangles.Add(firstPart);

            QuasiTriangle secondPart = MakeCCWTriangle(currentEdge.Second, thirdPoint, neighbourFarPoint.GetHashCode(), triangles.GetNewCode());

            triangles.Add(secondPart);

            UpdateRelation(firstPart.GetHashCode(), secondPart.GetHashCode());

            UpdateRelation(firstPart.GetHashCode(), currenTriangle.GetNeighbour(new QuasiEdge(currentEdge.First, thirdPoint)));

            UpdateRelation(secondPart.GetHashCode(), currenTriangle.GetNeighbour(new QuasiEdge(currentEdge.Second, thirdPoint)));

            int neighbour1, neighbour2;

            GetOtherNeighbours(currenTriangle.GetHashCode(), neigbour.GetHashCode(), out neighbour1, out neighbour2);

            if (neighbour1 != -1)
            {
                if (AreNeighbour(firstPart, triangles.GetTriangle(neighbour1)) && AreNeighbour(secondPart, triangles.GetTriangle(neighbour1)))
                {
                    throw new NotImplementedException();
                }
                if (AreNeighbour(firstPart, triangles.GetTriangle(neighbour1)))
                {
                    UpdateRelation(firstPart.GetHashCode(), neighbour1);

                    stack.Push(new KeyValuePair<QuasiEdge, QuasiTriangle>(GetCommonEdge(firstPart, triangles.GetTriangle(neighbour1)), firstPart));
                }
                else if (AreNeighbour(secondPart, triangles.GetTriangle(neighbour1)))
                {
                    UpdateRelation(secondPart.GetHashCode(), neighbour1);

                    stack.Push(new KeyValuePair<QuasiEdge, QuasiTriangle>(GetCommonEdge(secondPart, triangles.GetTriangle(neighbour1)), secondPart));
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            if (neighbour2 != -1)
            {
                if (AreNeighbour(firstPart, triangles.GetTriangle(neighbour2)) && AreNeighbour(secondPart, triangles.GetTriangle(neighbour2)))
                {
                    throw new NotImplementedException();
                }
                if (AreNeighbour(firstPart, triangles.GetTriangle(neighbour2)))
                {
                    UpdateRelation(firstPart.GetHashCode(), neighbour2);

                    stack.Push(new KeyValuePair<QuasiEdge, QuasiTriangle>(GetCommonEdge(firstPart, triangles.GetTriangle(neighbour2)), firstPart));
                }
                else if (AreNeighbour(secondPart, triangles.GetTriangle(neighbour2)))
                {
                    UpdateRelation(secondPart.GetHashCode(), neighbour2);

                    stack.Push(new KeyValuePair<QuasiEdge, QuasiTriangle>(GetCommonEdge(secondPart, triangles.GetTriangle(neighbour2)), secondPart));
                }

                else
                {
                    throw new NotImplementedException();
                }
            }

            triangles.RemoveByCode(neigbour.GetHashCode());

            triangles.RemoveByCode(currenTriangle.GetHashCode());
        }
        else if (TopologyUtility.GetPointCircleRelation(neighbourFarPoint,
                                                    points.GetPoint(currenTriangle.First),
                                                    points.GetPoint(currenTriangle.Second),
                                                    points.GetPoint(currenTriangle.Third)) == PointCircleRelation.On)
        {
            int thirdPointCode = currenTriangle.GetThirdPoint(currentEdge);

            Point thirdPoint = points.GetPoint(thirdPointCode);

            Point secondPoint = points.GetPoint(currentEdge.Second);

            Point firstPoint = points.GetPoint(currentEdge.First);

            Triangle triangle1 = new Triangle(firstPoint, secondPoint, thirdPoint);

            Triangle triangle2 = new Triangle(firstPoint, secondPoint, neighbourFarPoint);

            Triangle triangle3 = new Triangle(thirdPoint, firstPoint, neighbourFarPoint);

            Triangle triangle4 = new Triangle(thirdPoint, secondPoint, neighbourFarPoint);

            double[] firstSetAngle = new double[] { triangle1.FirstAngle, triangle1.SecondAngle, triangle1.ThirdAngle,
                                                        triangle2.FirstAngle, triangle2.SecondAngle, triangle2.ThirdAngle};

            double[] secondSetAngle = new double[] { triangle3.FirstAngle, triangle3.SecondAngle, triangle3.ThirdAngle,
                                                        triangle4.FirstAngle, triangle4.SecondAngle, triangle4.ThirdAngle};

            if (IRI.Maptor.Sta.Mathematics.Statistics.GetMax(firstSetAngle) > IRI.Maptor.Sta.Mathematics.Statistics.GetMax(secondSetAngle) &&
                IRI.Maptor.Sta.Mathematics.Statistics.GetMin(firstSetAngle) < IRI.Maptor.Sta.Mathematics.Statistics.GetMin(secondSetAngle))
            {

                bool condition1 = TopologyUtility.GetPointCircleRelation(secondPoint, thirdPoint, firstPoint, neighbourFarPoint) == PointCircleRelation.In;

                bool condition2 = TopologyUtility.GetPointCircleRelation(firstPoint, thirdPoint, secondPoint, neighbourFarPoint) == PointCircleRelation.In;

                if (!condition1 && !condition2)
                {
                    QuasiTriangle firstPart = MakeCCWTriangle(currentEdge.First, thirdPointCode, neighbourFarPoint.GetHashCode(), triangles.GetNewCode());

                    triangles.Add(firstPart);

                    QuasiTriangle secondPart = MakeCCWTriangle(currentEdge.Second, thirdPointCode, neighbourFarPoint.GetHashCode(), triangles.GetNewCode());

                    triangles.Add(secondPart);

                    UpdateRelation(firstPart.GetHashCode(), secondPart.GetHashCode());

                    UpdateRelation(firstPart.GetHashCode(), currenTriangle.GetNeighbour(new QuasiEdge(currentEdge.First, thirdPointCode)));

                    UpdateRelation(secondPart.GetHashCode(), currenTriangle.GetNeighbour(new QuasiEdge(currentEdge.Second, thirdPointCode)));

                    int neighbour1, neighbour2;

                    GetOtherNeighbours(currenTriangle.GetHashCode(), neigbour.GetHashCode(), out neighbour1, out neighbour2);

                    if (neighbour1 != -1)
                    {
                        if (AreNeighbour(firstPart, triangles.GetTriangle(neighbour1)) && AreNeighbour(secondPart, triangles.GetTriangle(neighbour1)))
                        {
                            throw new NotImplementedException();
                        }
                        if (AreNeighbour(firstPart, triangles.GetTriangle(neighbour1)))
                        {
                            UpdateRelation(firstPart.GetHashCode(), neighbour1);

                            stack.Push(new KeyValuePair<QuasiEdge, QuasiTriangle>(GetCommonEdge(firstPart, triangles.GetTriangle(neighbour1)), firstPart));
                        }
                        else if (AreNeighbour(secondPart, triangles.GetTriangle(neighbour1)))
                        {
                            UpdateRelation(secondPart.GetHashCode(), neighbour1);

                            stack.Push(new KeyValuePair<QuasiEdge, QuasiTriangle>(GetCommonEdge(secondPart, triangles.GetTriangle(neighbour1)), secondPart));
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                    }
                    if (neighbour2 != -1)
                    {
                        if (AreNeighbour(firstPart, triangles.GetTriangle(neighbour2)) && AreNeighbour(secondPart, triangles.GetTriangle(neighbour2)))
                        {
                            throw new NotImplementedException();
                        }
                        if (AreNeighbour(firstPart, triangles.GetTriangle(neighbour2)))
                        {
                            UpdateRelation(firstPart.GetHashCode(), neighbour2);

                            stack.Push(new KeyValuePair<QuasiEdge, QuasiTriangle>(GetCommonEdge(firstPart, triangles.GetTriangle(neighbour2)), firstPart));
                        }
                        else if (AreNeighbour(secondPart, triangles.GetTriangle(neighbour2)))
                        {
                            UpdateRelation(secondPart.GetHashCode(), neighbour2);

                            stack.Push(new KeyValuePair<QuasiEdge, QuasiTriangle>(GetCommonEdge(secondPart, triangles.GetTriangle(neighbour2)), secondPart));
                        }

                        else
                        {
                            throw new NotImplementedException();
                        }
                    }

                    triangles.RemoveByCode(neigbour.GetHashCode());

                    triangles.RemoveByCode(currenTriangle.GetHashCode());
                }
            }
        }

        CheckForSwapEdge(ref stack);
    }

    private void RemoveBigTriangles()
    {
        for (int i = triangles.Count - 1; i >= 0; i--)
        {
            if (triangles[i].HasThePoint(lowerLeftIndex) ||
                    triangles[i].HasThePoint(upperLeftIndex) ||
                    triangles[i].HasThePoint(upperRightIndex) ||
                    triangles[i].HasThePoint(lowerRightIndex))
            {

                if (triangles[i].FirstSecondNeighbour != -1)
                {
                    UpdateNeighbour(triangles[i].FirstSecondNeighbour, triangles[i].GetHashCode(), -1);
                }
                if (triangles[i].SecondThirdNeighbour != -1)
                {
                    UpdateNeighbour(triangles[i].SecondThirdNeighbour, triangles[i].GetHashCode(), -1);
                }
                if (triangles[i].ThirdFirstNeighbour != -1)
                {
                    UpdateNeighbour(triangles[i].ThirdFirstNeighbour, triangles[i].GetHashCode(), -1);
                }

                triangles.RemoveAt(i);
            }

        }

        points.RemoveByCode(lowerLeftIndex); points.RemoveByCode(upperLeftIndex);

        points.RemoveByCode(upperRightIndex); points.RemoveByCode(lowerRightIndex);
    }

    private void UpdateRelation(int firstTriangleCode, int secondTriangleCode)
    {
        if (firstTriangleCode == -1 || secondTriangleCode == -1)
            return;

        QuasiTriangle triangle1 = triangles.GetTriangle(firstTriangleCode);

        QuasiTriangle triangle2 = triangles.GetTriangle(secondTriangleCode);

        TriangleRelation relation = triangle1.GetRelationTo(triangle2);

        switch (relation)
        {
            case TriangleRelation.FirstSecondNeighbour:

                triangle1.FirstSecondNeighbour = secondTriangleCode;

                break;

            case TriangleRelation.SecondThirdNeighbour:

                triangle1.SecondThirdNeighbour = secondTriangleCode;

                break;

            case TriangleRelation.ThirdFirstNeighbour:

                triangle1.ThirdFirstNeighbour = secondTriangleCode;

                break;

            default:
                throw new NotImplementedException();
        }

        relation = triangle2.GetRelationTo(triangle1);

        switch (relation)
        {
            case TriangleRelation.FirstSecondNeighbour:

                triangle2.FirstSecondNeighbour = firstTriangleCode;

                break;

            case TriangleRelation.SecondThirdNeighbour:

                triangle2.SecondThirdNeighbour = firstTriangleCode;

                break;

            case TriangleRelation.ThirdFirstNeighbour:

                triangle2.ThirdFirstNeighbour = firstTriangleCode;

                break;

            default:
                throw new NotImplementedException();
        }
    }


    public Triangle GeTriangle(int triangleCode)
    {
        QuasiTriangle temp = triangles.GetTriangle(triangleCode);

        return new Triangle(points.GetPoint(temp.First),
                                points.GetPoint(temp.Second),
                                points.GetPoint(temp.Third));
    }

    public Triangle GetTriangle(Point point, ref QuasiTriangle starTriangle)
    {
        QuasiTriangle triangle = GetContainingTriangle(point, starTriangle);

        starTriangle = triangle;

        if (triangle == null)
        {
            return null;
        }
        else
        {
            return new Triangle(points.GetPoint(triangle.First),
                                points.GetPoint(triangle.Second),
                                points.GetPoint(triangle.Third));
        }
    }

    //public Triangle SearchTriangle(Point point, ref QuasiTriangle starTriangle)
    //{
    //    PoinTriangleRelation relation = GetPointTriangleRelation(starTriangle, point);

    //    if (relation != PoinTriangleRelation.In &&
    //            relation != PoinTriangleRelation.OnFirstEdge &&
    //            relation != PoinTriangleRelation.OnSecondEdge &&
    //            relation != PoinTriangleRelation.OnThirdEdge)
    //    {
    //        starTriangle = SearchTriangle(point);
    //    }

    //    if (starTriangle == null)
    //    {
    //        return null;
    //    }
    //    else
    //    {
    //        return new Triangle(points.GetPoint(starTriangle.First),
    //                                      points.GetPoint(starTriangle.Second),
    //                                      points.GetPoint(starTriangle.Third));
    //    }
    //}

    private List<QuasiTriangle> TraceTriangle(Point point, out List<int> triangleQueue)
    {
        triangleQueue = new List<int>();

        QuasiTriangle temp = triangles[0];

        List<QuasiTriangle> result = new List<QuasiTriangle>();

        int counter = 0;

        while (true)
        {
            if (counter > triangles.Count)
            {
                return null;
            }
            triangleQueue.Add(temp.GetHashCode());

            if (temp.HasThePoint(point.GetHashCode()))
            {
                return null;
            }

            PoinTriangleRelation relation = GetPointTriangleRelation(temp, point);

            if (relation == PoinTriangleRelation.OnFirstEdge)
            {
                result.Add(temp);

                result.Add(triangles.GetTriangle(temp.FirstSecondNeighbour));

                return result;
            }
            if (relation == PoinTriangleRelation.OnSecondEdge)
            {
                result.Add(temp);

                result.Add(triangles.GetTriangle(temp.SecondThirdNeighbour));

                return result;
            }
            if (relation == PoinTriangleRelation.OnThirdEdge)
            {
                result.Add(temp);

                result.Add(triangles.GetTriangle(temp.ThirdFirstNeighbour));

                return result;
            }
            else if (relation == PoinTriangleRelation.In)
            {
                result.Add(temp);

                return result;
            }
            else if (relation == PoinTriangleRelation.AlongFirstEdgeNegative ||
                        relation == PoinTriangleRelation.BehindFirstVertex)
            {
                temp = WalkToNeighbour(temp, 2, 0, 1);
            }
            else if (relation == PoinTriangleRelation.AlongFirstEdgePositive ||
                        relation == PoinTriangleRelation.BehindSecondVertex)
            {
                temp = WalkToNeighbour(temp, 1, 0, 2);
            }
            else if (relation == PoinTriangleRelation.AlongSecondEdgeNegative)
            {
                temp = WalkToNeighbour(temp, 0, 1, 2);
            }
            else if (relation == PoinTriangleRelation.AlongSecondEdgePositive ||
                        relation == PoinTriangleRelation.BehindThirdVertex)
            {
                temp = WalkToNeighbour(temp, 2, 1, 0);
            }
            else if (relation == PoinTriangleRelation.AlongThirdEdgeNegative)
            {
                temp = WalkToNeighbour(temp, 1, 2, 0);
            }
            else if (relation == PoinTriangleRelation.AlongThirdEdgePositive)
            {
                temp = WalkToNeighbour(temp, 0, 2, 1);
            }
            else if (relation == PoinTriangleRelation.RightOfFirstEdge)
            {
                temp = WalkToNeighbour(temp, 0, 1, 2);
            }
            else if (relation == PoinTriangleRelation.RightOfSecondEdge)
            {
                temp = WalkToNeighbour(temp, 1, 2, 0);
            }
            else if (relation == PoinTriangleRelation.RightOfThirdEdge)
            {
                temp = WalkToNeighbour(temp, 2, 0, 1);
            }
            else
            {
                throw new NotImplementedException();
            }

            counter++;
        }
    }

    /* On the vertex
    //public List<QuasiTriangle> GetTrianglesWith(Point vertex)
    //{
    //    List<QuasiTriangle> result = new List<QuasiTriangle>();

    //    int pointCode = vertex.GetHashCode();

    //    foreach (QuasiTriangle item in triangles)
    //    {
    //        if (item.HasThePoint(pointCode))
    //        {
    //            result.Add(item);
    //        }
    //    }

    //    return result;
    //}
    */

    //Linear Search
    private QuasiTriangle SearchTriangle(Point point)
    {
        foreach (QuasiTriangle item in triangles)
        {
            PoinTriangleRelation relation = GetPointTriangleRelation(item, point);

            if (relation == PoinTriangleRelation.In ||
                relation == PoinTriangleRelation.OnFirstEdge ||
                relation == PoinTriangleRelation.OnSecondEdge ||
                relation == PoinTriangleRelation.OnThirdEdge)
            {
                return item;
            }
        }

        return null;
    }

    //Containing: In, On the edge, On the vertex
    private QuasiTriangle GetContainingTriangle(Point point, QuasiTriangle starTriangle)
    {
        QuasiTriangle temp = starTriangle;

        int counter = 0;

        while (true)
        {
            if (counter > triangles.Count)
            {
                return null;
            }

            if (temp.HasThePoint(point.GetHashCode()))
            {
                return temp;
            }

            PoinTriangleRelation relation = GetPointTriangleRelation(temp, point);

            if (relation == PoinTriangleRelation.OnFirstEdge)
            {
                return temp;
            }
            if (relation == PoinTriangleRelation.OnSecondEdge)
            {
                return temp;
            }
            if (relation == PoinTriangleRelation.OnThirdEdge)
            {
                return temp;
            }
            else if (relation == PoinTriangleRelation.In)
            {
                return temp;
            }
            else if (relation == PoinTriangleRelation.AlongFirstEdgeNegative ||
                        relation == PoinTriangleRelation.BehindFirstVertex)
            {
                temp = WalkToNeighbour(temp, 2, 0, 1);
            }
            else if (relation == PoinTriangleRelation.AlongFirstEdgePositive ||
                        relation == PoinTriangleRelation.BehindSecondVertex)
            {
                temp = WalkToNeighbour(temp, 1, 0, 2);
            }
            else if (relation == PoinTriangleRelation.AlongSecondEdgeNegative)
            {
                temp = WalkToNeighbour(temp, 0, 1, 2);
            }
            else if (relation == PoinTriangleRelation.AlongSecondEdgePositive ||
                        relation == PoinTriangleRelation.BehindThirdVertex)
            {
                temp = WalkToNeighbour(temp, 2, 1, 0);
            }
            else if (relation == PoinTriangleRelation.AlongThirdEdgeNegative)
            {
                temp = WalkToNeighbour(temp, 1, 2, 0);
            }
            else if (relation == PoinTriangleRelation.AlongThirdEdgePositive)
            {
                temp = WalkToNeighbour(temp, 0, 2, 1);
            }
            else if (relation == PoinTriangleRelation.RightOfFirstEdge)
            {
                temp = WalkToNeighbour(temp, 0, 1, 2);
            }
            else if (relation == PoinTriangleRelation.RightOfSecondEdge)
            {
                temp = WalkToNeighbour(temp, 1, 2, 0);
            }
            else if (relation == PoinTriangleRelation.RightOfThirdEdge)
            {
                temp = WalkToNeighbour(temp, 2, 0, 1);
            }
            else
            {
                throw new NotImplementedException();
            }

            counter++;
        }
    }

    private QuasiTriangle WalkToNeighbour(QuasiTriangle current, byte firstPriority, byte secondPriority, byte thirdPriority)
    {
        int[] neighbours = current.OrderedNeighbours;

        if (neighbours[firstPriority] != -1)
        {
            return triangles.GetTriangle(neighbours[firstPriority]);
        }
        else if (neighbours[secondPriority] != -1)
        {
            return triangles.GetTriangle(neighbours[secondPriority]);
        }
        else if (neighbours[thirdPriority] != -1)
        {
            return triangles.GetTriangle(neighbours[thirdPriority]);
        }
        else
        {
            throw new NotImplementedException();
        }
    }


    private int GetNeighbourFarPoint(QuasiTriangle current, QuasiTriangle neighbour)
    {
        bool hasFirstPoint = current.HasThePoint(neighbour.First);

        bool hasSecondPoint = current.HasThePoint(neighbour.Second);

        bool hasThirdPoint = current.HasThePoint(neighbour.Third);

        if (hasFirstPoint && hasSecondPoint && !hasThirdPoint)
        {
            return neighbour.Third;
        }
        else if (hasFirstPoint && !hasSecondPoint && hasThirdPoint)
        {
            return neighbour.Second;
        }
        else if (!hasFirstPoint && hasSecondPoint && hasThirdPoint)
        {
            return neighbour.First;
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    private QuasiEdge GetCommonEdge(QuasiTriangle current, QuasiTriangle neighbour)
    {
        bool hasFirstPoint = neighbour.HasThePoint(current.First);

        bool hasSecondPoint = neighbour.HasThePoint(current.Second);

        bool hasThirdPoint = neighbour.HasThePoint(current.Third);

        if (hasFirstPoint && hasSecondPoint && !hasThirdPoint)
        {
            return new QuasiEdge(current.First, current.Second);
        }
        else if (hasFirstPoint && !hasSecondPoint && hasThirdPoint)
        {
            return new QuasiEdge(current.First, current.Third);
        }
        else if (!hasFirstPoint && hasSecondPoint && hasThirdPoint)
        {
            return new QuasiEdge(current.Second, current.Third);
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    private int GetNeighbour(QuasiTriangle current, QuasiEdge commonEdge)
    {
        if (current.FirstSecondNeighbour != -1)
        {
            if (GetCommonEdge(current, triangles.GetTriangle(current.FirstSecondNeighbour)).Equals(commonEdge))
            {
                return current.FirstSecondNeighbour;
            }
        }
        if (current.SecondThirdNeighbour != -1)
        {
            if (GetCommonEdge(current, triangles.GetTriangle(current.SecondThirdNeighbour)).Equals(commonEdge))
            {
                return current.SecondThirdNeighbour;
            }
        }
        if (current.ThirdFirstNeighbour != -1)
        {
            if (GetCommonEdge(current, triangles.GetTriangle(current.ThirdFirstNeighbour)).Equals(commonEdge))
            {
                return current.ThirdFirstNeighbour;
            }
        }

        return -1;

    }

    private void GetOtherNeighbours(int currentTriangle, int neighbour, out int neighbour1, out int neighbour2)
    {
        QuasiTriangle tempTriangle = triangles.GetTriangle(neighbour);

        neighbour1 = tempTriangle.FirstSecondNeighbour != currentTriangle ? tempTriangle.FirstSecondNeighbour : tempTriangle.ThirdFirstNeighbour;

        neighbour2 = tempTriangle.SecondThirdNeighbour != currentTriangle ? tempTriangle.SecondThirdNeighbour : tempTriangle.ThirdFirstNeighbour;
    }

    private bool AreNeighbour(QuasiTriangle firsTriangle, QuasiTriangle secondTriangle)
    {
        TriangleRelation relation = firsTriangle.GetRelationTo(secondTriangle);

        return relation == TriangleRelation.FirstSecondNeighbour ||
                relation == TriangleRelation.SecondThirdNeighbour ||
                relation == TriangleRelation.ThirdFirstNeighbour;
    }

    private void UpdateNeighbour(int triangleCode, int oldNeighbour, int newNeigbour)
    {
        QuasiTriangle tempValue = triangles.GetTriangle(triangleCode);

        if (tempValue.FirstSecondNeighbour == oldNeighbour)
        {
            tempValue.FirstSecondNeighbour = newNeigbour;
        }
        else if (tempValue.SecondThirdNeighbour == oldNeighbour)
        {
            tempValue.SecondThirdNeighbour = newNeigbour;
        }
        else if (tempValue.ThirdFirstNeighbour == oldNeighbour)
        {
            tempValue.ThirdFirstNeighbour = newNeigbour;
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    private PoinTriangleRelation GetPointTriangleRelation(QuasiTriangle triangle, Point point)
    {
        Point firstVertex = points.GetPoint(triangle.First);

        Point secondVertex = points.GetPoint(triangle.Second);

        Point thirdVertex = points.GetPoint(triangle.Third);

        int relation1 = (int)TopologyUtility.GetPointVectorRelation(point, firstVertex, secondVertex);

        int relation2 = (int)TopologyUtility.GetPointVectorRelation(point, secondVertex, thirdVertex);

        int relation3 = (int)TopologyUtility.GetPointVectorRelation(point, thirdVertex, firstVertex);

        return (PoinTriangleRelation)(relation1 * QuasiTriangle.firstEdgeWeight +
                                        relation2 * QuasiTriangle.secondEdgeWeight +
                                        relation3 * QuasiTriangle.thirdEdgeWeight);
    }

    private QuasiTriangle MakeCCWTriangle(int firstPointCode, int secondPointCode, int thirdPointCode, int code)
    {
        PointVectorRelation relation = TopologyUtility.GetPointVectorRelation(points.GetPoint(thirdPointCode),
                                                           points.GetPoint(firstPointCode),
                                                           points.GetPoint(secondPointCode));

        if (relation == PointVectorRelation.LiesOnTheLine)
        {
            return null;
        }
        else if (relation == PointVectorRelation.LiesLeft)
        {
            return new QuasiTriangle(firstPointCode, secondPointCode, thirdPointCode, code);
        }
        else if (relation == PointVectorRelation.LiesRight)
        {
            return new QuasiTriangle(firstPointCode, thirdPointCode, secondPointCode, code);
        }
        else
        {
            throw new NotImplementedException();
        }
    }


    #region Voronoi

    public VoronoiPointCollection GetVoronoiDiagram(double minX, double minY, double maX, double maxY)
    {
        VoronoiPointCollection result = new VoronoiPointCollection();

        Dictionary<int, int> temp = new Dictionary<int, int>();

        foreach (QuasiTriangle item in triangles)
        {
            var center = TopologyUtility.CalculateCircumcenterCenterPoint(points.GetPoint(item.First),
                                                                                    points.GetPoint(item.Second),
                                                                                    points.GetPoint(item.Third));

            VoronoiPoint point = new VoronoiPoint(item.GetHashCode(), new Point() { X = center.X, Y = center.Y });

            result.Add(point);

            temp.Add(item.GetHashCode(), point.GetHashCode());
        }

        for (int i = 0; i < triangles.Count; i++)
        {
            if (triangles[i].FirstSecondNeighbour != -1)
            {
                result[i].NeigboursCode.Add(temp[triangles[i].FirstSecondNeighbour]);
            }
            if (triangles[i].SecondThirdNeighbour != -1)
            {
                result[i].NeigboursCode.Add(temp[triangles[i].SecondThirdNeighbour]);
            }
            if (triangles[i].ThirdFirstNeighbour != -1)
            {
                result[i].NeigboursCode.Add(temp[triangles[i].ThirdFirstNeighbour]);
            }
        }

        //result = RefineBoundary(result, minX, minY, maX, maxY);

        return result;
    }

    #endregion
}