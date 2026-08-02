//// besmellahe rahmane rahim
//// Allahomma ajjel le-valiyek al-faraj

//using IRI.Maptor.Sta.Common.Primitives;
//using IRI.Maptor.Sta.Spatial.Topology;
//using IRI.Maptor.Sta.Spatial.Helpers;
//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace IRI.Maptor.Sta.Spatial.Primitives;

//public class ConvexPolygon
//{
//    //Points are in ccw form
//    public PointCollection Vertices { get; set; }

//    public List<int> Neighbours { get; set; }

//    public ConvexPolygon(PointCollection vertices)
//    {
//        Vertices = vertices;

//        Neighbours = new List<int>(vertices.Count);

//        for (int i = 0; i < Neighbours.Count; i++)
//        {
//            Neighbours[i] = -1;
//        }
//    }

//    public ConvexPolygon(PointCollection vertices, List<int> neighbours)
//    {
//        if (vertices.Count != neighbours.Count)
//        {
//            throw new NotImplementedException();
//        }

//        Vertices = vertices;

//        Neighbours = neighbours;
//    }

//    public int Count
//    {
//        get { return Vertices.Count; }
//    }

//    //private void VertexAdded()
//    //{
//    //    this.Neighbours.a
//    //}

//    public override string ToString()
//    {
//        if (Count < 0)
//        {
//            return string.Empty;
//        }

//        StringBuilder result = new StringBuilder();

//        for (int i = 0; i < Vertices.Count - 1; i++)
//        {
//            result.Append(string.Format("{0}, ", Vertices[i].ToString()));
//        }

//        result.Append(Vertices[Vertices.Count - 1].ToString());

//        return result.ToString();
//    }

//    public override int GetHashCode()
//    {
//        return ToString().GetHashCode();
//    }

//    public override bool Equals(object obj)
//    {
//        if (obj.GetType() == typeof(QuasiPolygon))
//        {
//            return obj.GetHashCode() == GetHashCode();
//        }

//        return false;
//    }

//    public double Perimeter
//    {
//        get { return CalculatePerimeter(); }
//    }

//    private double CalculatePerimeter()
//    {
//        double tempValue = 0;

//        int count = Count;

//        for (int i = 0; i < count; i++)
//        {
//            int j = (i + 1) % count;

//            //tempValue += ComputationalGeometry.CalculateDistance(Vertices[i], Vertices[j]);
//            tempValue += Vertices[i].DistanceTo(Vertices[j]);
//        }

//        return tempValue;
//    }

//    public PointPolygonRelation GetRelationTo(Point point)
//    {
//        int count = Count;

//        int tempValue = 0;

//        PointVectorRelation[] relation = new PointVectorRelation[count];

//        for (int i = 0; i < count; i++)
//        {
//            int j = (i + 1) % count;

//            relation[i] = TopologyUtility.GetPointVectorRelation(point, Vertices[i], Vertices[j]);

//            tempValue += (int)relation[i];

//            if (i > 0 && (int)relation[i] * (int)relation[i - 1] < 1)
//            {
//                return PointPolygonRelation.Out;
//            }

//        }

//        if (tempValue == count || tempValue == -1 * count)
//        {
//            return PointPolygonRelation.In;
//        }
//        else
//        {
//            return PointPolygonRelation.On;
//        }
//    }

//    public bool HasThePoint(Point point)
//    {
//        foreach (Point item in Vertices)
//        {
//            if (item.Equals(point))
//            {
//                return true;
//            }
//        }

//        return false;
//    }

//    public double CalculateArea()
//    {
//        double result = 0;

//        int count = Vertices.Count;

//        for (int i = 0; i < count; i++)
//        {
//            int j = (i + 1) % count;

//            result += Vertices[i].X * Vertices[j].Y - Vertices[j].X * Vertices[i].Y;
//        }

//        //Polygon is counterClockWise
//        if (result < 0)
//        {
//            throw new NotImplementedException();
//        }

//        return result / 2;
//    }

//    public List<Point> Intersects(Point firstPointLine, Point secondPointLine, out List<int> edgeIndexes)
//    {
//        List<Point> result = new List<Point>();

//        edgeIndexes = new List<int>();

//        int count = Vertices.Count;

//        for (int i = 0; i < count; i++)
//        {
//            int j = (i + 1) % count;

//            Point intersection;

//            LineLineSegmentRelation relation = TopologyUtility.LineSegmentsIntersects(firstPointLine, secondPointLine, Vertices[i], Vertices[j], out intersection);

//            if (relation == LineLineSegmentRelation.Intersect)
//            {
//                //check if intersection is not the vertex!
//                if (!result.Contains(intersection) && !intersection.AreTheSame(Vertices[i], 10))
//                {
//                    result.Add(intersection);

//                    edgeIndexes.Add(i);
//                }
//            }
//            else if (relation == LineLineSegmentRelation.Coinciding)
//            {
//                if (!result.Contains(Vertices[j]))
//                {
//                    result.Add(Vertices[j]);

//                    edgeIndexes.Add(i);
//                }
//            }
//        }

//        return result;
//    }
//}
