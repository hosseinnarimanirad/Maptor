//// besmellahe rahmane rahim
//// Allahomma ajjel le-valiyek al-faraj

//using IRI.Maptor.Core.Common.Primitives;
//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace IRI.Maptor.Core.Spatial.Primitives;

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

//    public VoronoiCell(Point primaryPoint, PointCollection vertices)
//        : base(vertices)
//    {
//        PrimaryPoint = primaryPoint;
//    }

//    public VoronoiCell(Point primaryPoint, PointCollection vertices, List<int> neighbours)
//        : base(vertices, neighbours)
//    {
//        PrimaryPoint = primaryPoint;
//    }

//    public override string ToString()
//    {
//        return string.Format("Primary Point:{0}, Vertices:{1}", PrimaryPoint.ToString(), base.ToString());
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
//            temp1.Add(Vertices[i]);
//        }

//        temp1.Add(intersections[1]);

//        temp2.Add(intersections[1]);

//        for (int i = edgeIndexes[1] + 1; i < edgeIndexes[0] + Vertices.Count - 1 - edgeIndexes[1]; i++)
//        {
//            int j = (i + 1) % Vertices.Count;

//            temp2.Add(Vertices[j]);
//        }

//        temp2.Add(intersections[0]);
//    }
//}
