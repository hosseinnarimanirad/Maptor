//// besmellahe rahmane rahim
//// Allahomma ajjel le-valiyek al-faraj

//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace IRI.Maptor.Core.Spatial.Primitives;

////Assumed to be CCW
//public class QuasiPolygon
//{
//    public List<int> Vertices { get; set; }

//    public List<int> neighbours;

//    public QuasiPolygon(List<int> vertices)
//    {
//        Vertices = vertices;

//        neighbours = new List<int>(vertices.Count);

//        for (int i = 0; i < neighbours.Count; i++)
//        {
//            neighbours[i] = -1;
//        }
//    }

//    public QuasiPolygon(List<int> vertices, List<int> neighbours)
//    {
//        if (vertices.Count != neighbours.Count)
//        {
//            throw new NotImplementedException();
//        }

//        Vertices = vertices;

//        this.neighbours = neighbours;
//    }

//    public int Count
//    {
//        get { return Vertices.Count; }
//    }

//    public override string ToString()
//    {
//        if (Count < 0)
//        {
//            return string.Empty;
//        }

//        StringBuilder result = new StringBuilder();

//        for (int i = 0; i < Vertices.Count - 2; i++)
//        {
//            result.Append(string.Format("{0}, ", Vertices[i]));
//        }
//        if (Vertices.Count > 0)
//        {
//            result.Append(Vertices[Vertices.Count - 1]);
//        }

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

//}
