// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Topology;
using IRI.Maptor.Sta.Spatial.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.Spatial.Primitives;

public class ConvexPolygon
{
    //Points are in ccw form
    public PointCollection Vertexes { get; set; }

    public List<int> Neighbours { get; set; }

    public ConvexPolygon(PointCollection vertexes)
    {
        Vertexes = vertexes;

        Neighbours = new List<int>(vertexes.Count);

        for (int i = 0; i < Neighbours.Count; i++)
        {
            Neighbours[i] = -1;
        }
    }

    public ConvexPolygon(PointCollection vertexes, List<int> neighbours)
    {
        if (vertexes.Count != neighbours.Count)
        {
            throw new NotImplementedException();
        }

        Vertexes = vertexes;

        Neighbours = neighbours;
    }

    public int Count
    {
        get { return Vertexes.Count; }
    }

    //private void VertexAdded()
    //{
    //    this.Neighbours.a
    //}

    public override string ToString()
    {
        if (Count < 0)
        {
            return string.Empty;
        }

        StringBuilder result = new StringBuilder();

        for (int i = 0; i < Vertexes.Count - 1; i++)
        {
            result.Append(string.Format("{0}, ", Vertexes[i].ToString()));
        }

        result.Append(Vertexes[Vertexes.Count - 1].ToString());

        return result.ToString();
    }

    public override int GetHashCode()
    {
        return ToString().GetHashCode();
    }

    public override bool Equals(object obj)
    {
        if (obj.GetType() == typeof(QuasiPolygon))
        {
            return obj.GetHashCode() == GetHashCode();
        }

        return false;
    }

    public double Perimeter
    {
        get { return CalculatePerimeter(); }
    }

    private double CalculatePerimeter()
    {
        double tempValue = 0;

        int count = Count;

        for (int i = 0; i < count; i++)
        {
            int j = (i + 1) % count;

            //tempValue += ComputationalGeometry.CalculateDistance(Vertexes[i], Vertexes[j]);
            tempValue += Vertexes[i].DistanceTo(Vertexes[j]);
        }

        return tempValue;
    }

    public PointPolygonRelation GetRelationTo(Point point)
    {
        int count = Count;

        int tempValue = 0;

        PointVectorRelation[] relation = new PointVectorRelation[count];

        for (int i = 0; i < count; i++)
        {
            int j = (i + 1) % count;

            relation[i] = TopologyUtility.GetPointVectorRelation(point, Vertexes[i], Vertexes[j]);

            tempValue += (int)relation[i];

            if (i > 0 && (int)relation[i] * (int)relation[i - 1] < 1)
            {
                return PointPolygonRelation.Out;
            }

        }

        if (tempValue == count || tempValue == -1 * count)
        {
            return PointPolygonRelation.In;
        }
        else
        {
            return PointPolygonRelation.On;
        }
    }

    public bool HasThePoint(Point point)
    {
        foreach (Point item in Vertexes)
        {
            if (item.Equals(point))
            {
                return true;
            }
        }

        return false;
    }

    public double CalculateArea()
    {
        double result = 0;

        int count = Vertexes.Count;

        for (int i = 0; i < count; i++)
        {
            int j = (i + 1) % count;

            result += Vertexes[i].X * Vertexes[j].Y - Vertexes[j].X * Vertexes[i].Y;
        }

        //Polygon is counterClockWise
        if (result < 0)
        {
            throw new NotImplementedException();
        }

        return result / 2;
    }

    public List<Point> Intersects(Point firstPointLine, Point secondPointLine, out List<int> edgeIndexes)
    {
        List<Point> result = new List<Point>();

        edgeIndexes = new List<int>();

        int count = Vertexes.Count;

        for (int i = 0; i < count; i++)
        {
            int j = (i + 1) % count;

            Point intersection;

            LineLineSegmentRelation relation = TopologyUtility.LineSegmentsIntersects(firstPointLine, secondPointLine, Vertexes[i], Vertexes[j], out intersection);

            if (relation == LineLineSegmentRelation.Intersect)
            {
                //check if intersection is not the vertex!
                if (!result.Contains(intersection) && !intersection.AreTheSame(Vertexes[i], 10))
                {
                    result.Add(intersection);

                    edgeIndexes.Add(i);
                }
            }
            else if (relation == LineLineSegmentRelation.Coinciding)
            {
                if (!result.Contains(Vertexes[j]))
                {
                    result.Add(Vertexes[j]);

                    edgeIndexes.Add(i);
                }
            }
        }

        return result;
    }
}
