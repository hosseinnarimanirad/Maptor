// BESMELLAHE RAHMANE RAHIM
// ALLAHOMMA AJJEL LE-VALIYEK AL-FARAJ

using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Topology;
using IRI.Maptor.Sta.Spatial.Helpers;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Sta.Spatial.Analysis;

public class VoronoiDiagram
{
    QuasiVoronoiCellCollection polygons;

    PointCollection vertexes;

    Point upperRight, lowerLeft;

    public VoronoiDiagram(PointCollection points, Point upperRight, Point lowerLeft)
    {

        vertexes = new PointCollection();

        if (points.Equals(null))
        {
            throw new NotImplementedException();
        }

        if (points.MaxX > upperRight.X || points.MaxY > upperRight.Y ||
            points.MinX < lowerLeft.X || points.MinY < lowerLeft.Y)
        {
            throw new NotImplementedException();
        }

        polygons = new QuasiVoronoiCellCollection();

        Initialize(points[0], upperRight, lowerLeft);

        for (int i = 1; i < points.Count; i++)
        {
            OccupyRegion(points[i]);
        }
    }

    private void Initialize(Point firstPrimaryPoint, Point upperRight, Point lowerLeft)
    {
        this.upperRight = upperRight;

        this.lowerLeft = lowerLeft;

        Point lowerRight = new Point(upperRight.X, lowerLeft.Y);

        Point upperLeft = new Point(lowerLeft.X, upperRight.Y);

        vertexes.Add(lowerLeft); vertexes.Add(lowerRight);

        vertexes.Add(upperRight); vertexes.Add(upperLeft);

        QuasiVoronoiCell tempCell = new QuasiVoronoiCell(
                                        firstPrimaryPoint,
                                        new List<int>(
                                            new int[] {
                                                lowerLeft.GetHashCode(),
                                                lowerRight.GetHashCode(),
                                                upperRight.GetHashCode(),
                                                upperLeft.GetHashCode()}));

        tempCell.neighbours.AddRange(new int[] { -1, -1, -1, -1 });

        polygons.Add(tempCell);

    }

    private VoronoiCell FindCircumferencePolygon(Point point, out PointPolygonRelation relation)
    {
        for (int i = 0; i < polygons.Count; i++)
        {
            VoronoiCell result = GetPolygon(polygons[i].GetHashCode());

            PointPolygonRelation tempValue = result.GetRelationTo(point);

            if (tempValue == PointPolygonRelation.Out)
            {
                continue;
            }
            else
            {
                relation = tempValue;

                return result;
            }
        }

        throw new NotImplementedException();
    }

    public VoronoiCell GetPolygon(int polygonCode)
    {
        PointCollection collection = new PointCollection();

        QuasiVoronoiCell temp = polygons.GetCell(polygonCode);

        for (int i = 0; i < temp.Vertexes.Count; i++)
        {
            collection.Add(vertexes.GetPoint(temp.Vertexes[i]));
        }

        return new VoronoiCell(temp.PrimaryPoint, collection, temp.neighbours);
    }

    public void OccupyRegion(Point point)
    {
        PointPolygonRelation relation;

        VoronoiCell polygon = FindCircumferencePolygon(point, out relation);

        if (relation == PointPolygonRelation.In)
        {
            DivideTheRegionWithPointInTheRegion(polygon, point);
        }
        else if (relation == PointPolygonRelation.On)
        {
            if (polygon.HasThePoint(point))
            {
                DivideTheRegionWithPointOnTheVertex(polygon, point);
            }
            else
            {
                DivideTheRegionWithPointOnTheEdge(polygon, point);
            }
        }
    }

    private void DivideTheRegionWithPointOnTheEdge(VoronoiCell polygon, Point point)
    {
        throw new NotImplementedException();
    }

    private void DivideTheRegionWithPointOnTheVertex(VoronoiCell polygon, Point point)
    {
        throw new NotImplementedException();
    }

    private void DivideTheRegionWithPointInTheRegion(VoronoiCell polygon, Point point)
    {
        Point midPoint = SpatialUtility.CalculateMidPoint(point, polygon.PrimaryPoint);

        Point temPoint = new Point(midPoint.X + 10, midPoint.Y - 10 / SpatialUtility.CalculateSlope(point, polygon.PrimaryPoint));

        List<int> edgeIndexes;

        List<Point> newVertexes = polygon.Intersects(midPoint, temPoint, out edgeIndexes);

        //20/12/2009
        if (newVertexes.Count != 2)
        {
            throw new NotImplementedException();
        }

        List<int> affectedPolygons = GetAffectedPolygons(polygon, newVertexes, edgeIndexes);

        List<int> ii = new List<int>();

        for (int i = 0; i < affectedPolygons.Count; i++)
        {
            if (affectedPolygons[i] == -1)
                continue;

            List<Point> p2 = MakeUpPolygon(affectedPolygons[i], point);

            foreach (Point item in p2)
            {
                if (!vertexes.Contains(item))
                {
                    vertexes.Add(item);
                }
                if (!ii.Contains(item.GetHashCode()))
                {
                    ii.Add(item.GetHashCode());
                }
            }
        }

        polygons.Add(new QuasiVoronoiCell(point, ii));
        //20/12/2009

        //check if neighbour exists!
        //newVertexes is for check!
        //int nextPolygon = MakeUpPolygon(polygon.Neighbours[edgeIndexes[0]], newVertexes[0], point);
    }

    private List<int> GetAffectedPolygons(VoronoiCell polygon, List<Point> newVertexes, List<int> edgeIndexes)
    {
        PointVectorRelation relation = TopologyUtility.GetPointVectorRelation(polygon.PrimaryPoint, newVertexes[0], newVertexes[1]);

        if (relation == PointVectorRelation.LiesOnTheLine)
        {
            throw new NotImplementedException();
        }

        List<int> result = new List<int>();

        bool condition = false;

        for (int i = 0; i < polygon.Vertexes.Count; i++)
        {
            PointVectorRelation tempRelation = TopologyUtility.GetPointVectorRelation(polygon.Vertexes[i], newVertexes[0], newVertexes[1]);

            if (tempRelation != relation)
            {
                if (tempRelation == PointVectorRelation.LiesOnTheLine)
                {
                    if (condition)
                    {
                        int temp = i - 1 >= 0 ? i - 1 : polygon.Vertexes.Count - 1;

                        result.Add(polygon.Neighbours[temp]);
                    }
                }
                else
                {
                    condition = true;

                    int temp = i - 1 >= 0 ? i - 1 : polygon.Vertexes.Count - 1;

                    result.Add(polygon.Neighbours[temp]);
                }
            }
        }

        return result;
    }

    private List<Point> MakeUpPolygon(int polygonCode, Point point)
    {
        VoronoiCell polygon = GetPolygon(polygonCode);

        Point midPoint = SpatialUtility.CalculateMidPoint(point, polygon.PrimaryPoint);

        Point temPoint = new Point(midPoint.X + 10, midPoint.Y - 10 / SpatialUtility.CalculateSlope(point, polygon.PrimaryPoint));

        List<int> edgeIndexes;

        List<Point> newVertexes = polygon.Intersects(midPoint, temPoint, out edgeIndexes);

        if (newVertexes.Count == 1)
        {
            return newVertexes;
        }

        if (newVertexes.Count != 2)
        {
            throw new NotImplementedException();
        }

        VoronoiCell first = new VoronoiCell(polygon.PrimaryPoint);

        first.Vertexes.Add(newVertexes[1]);

        first.Vertexes.Add(newVertexes[0]);

        for (int i = edgeIndexes[0] + 1; i < edgeIndexes[1]; i++)
        {
            first.Vertexes.Add(polygon.Vertexes[i]);

            first.Neighbours.Add(polygon.Neighbours[i - 1]);
        }

        VoronoiCell second = new VoronoiCell(polygon.PrimaryPoint);

        for (int i = edgeIndexes[1] + 1; i < polygon.Vertexes.Count; i++)
        {
            second.Vertexes.Add(polygon.Vertexes[i]);

            first.Neighbours.Add(polygon.Neighbours[i]);
        }

        for (int i = 0; i < edgeIndexes[0]; i++)
        {
            second.Vertexes.Add(polygon.Vertexes[i]);

            first.Neighbours.Add(polygon.Neighbours[i]);
        }

        second.Vertexes.Add(newVertexes[0]);

        second.Vertexes.Add(newVertexes[1]);

        if (first.GetRelationTo(polygon.PrimaryPoint) == PointPolygonRelation.In)
        {
            polygon = first;
        }
        else
        {
            polygon = second;
        }

        return newVertexes;
    }

    private List<int> GetNeighbours(ConvexPolygon currentPolygon, Point commonPoint)
    {
        List<int> result = new List<int>();

        foreach (int item in currentPolygon.Neighbours)
        {
            ConvexPolygon temp = GetPolygon(item);

            if (temp.HasThePoint(commonPoint))
            {
                result.Add(item);
            }
        }

        return result;
    }


}
