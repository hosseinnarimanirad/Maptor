//// besmellahe rahmane rahim
//// Allahomma ajjel le-valiyek al-faraj

//using IRI.Maptor.Sta.Common.Primitives;
//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace IRI.Maptor.Sta.Spatial.Primitives;

//public class VoronoiCell : ConvexPolygon
//{
//    public Point PrimaryPoint { get; set; }

//    public VoronoiCell()
//        : base(new PointCollection())
//    {
//        PrimaryPoint = new Point();
//    }

//    public VoronoiCell(Point primaryPoint)
//        : base(new PointCollection())
//    {
//        PrimaryPoint = primaryPoint;
//    }

//    public VoronoiCell(Point primaryPoint, PointCollection vertexes)
//        : base(vertexes)
//    {
//        PrimaryPoint = primaryPoint;
//    }

//    public VoronoiCell(Point primaryPoint, PointCollection vertexes, List<int> neighbours)
//        : base(vertexes, neighbours)
//    {
//        PrimaryPoint = primaryPoint;
//    }

//    public override string ToString()
//    {
//        return string.Format("Primary Point:{0}, Vertexes:{1}", PrimaryPoint.ToString(), base.ToString());
//    }

//    public override int GetHashCode()
//    {
//        return PrimaryPoint.GetHashCode();
//    }

//    public override bool Equals(object obj)
//    {
//        return obj.GetType() == typeof(VoronoiCell) && obj.ToString().Equals(ToString());
//    }

//    public void Clip(Point firstPointLine, Point secondPointLine)
//    {
//        List<int> edgeIndexes;

//        List<Point> intersections = Intersects(firstPointLine, secondPointLine, out edgeIndexes);

//        if (intersections.Count < 2)
//        {
//            return;
//        }

//        List<Point> temp1 = new List<Point>(); List<Point> temp2 = new List<Point>();

//        temp1.Add(intersections[0]);

//        for (int i = edgeIndexes[0] + 1; i < edgeIndexes[1]; i++)
//        {
//            temp1.Add(Vertexes[i]);
//        }

//        temp1.Add(intersections[1]);

//        temp2.Add(intersections[1]);

//        for (int i = edgeIndexes[1] + 1; i < edgeIndexes[0] + Vertexes.Count - 1 - edgeIndexes[1]; i++)
//        {
//            int j = (i + 1) % Vertexes.Count;

//            temp2.Add(Vertexes[j]);
//        }

//        temp2.Add(intersections[0]);
//    }
//}
