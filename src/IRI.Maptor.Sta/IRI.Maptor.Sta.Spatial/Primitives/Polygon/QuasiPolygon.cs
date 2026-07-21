//// besmellahe rahmane rahim
//// Allahomma ajjel le-valiyek al-faraj

//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace IRI.Maptor.Sta.Spatial.Primitives;

////Assumed to be CCW
//public class QuasiPolygon
//{
//    public List<int> Vertexes { get; set; }

//    public List<int> neighbours;

//    public QuasiPolygon(List<int> vertexes)
//    {
//        Vertexes = vertexes;

//        neighbours = new List<int>(vertexes.Count);

//        for (int i = 0; i < neighbours.Count; i++)
//        {
//            neighbours[i] = -1;
//        }
//    }

//    public QuasiPolygon(List<int> vertexes, List<int> neighbours)
//    {
//        if (vertexes.Count != neighbours.Count)
//        {
//            throw new NotImplementedException();
//        }

//        Vertexes = vertexes;

//        this.neighbours = neighbours;
//    }

//    public int Count
//    {
//        get { return Vertexes.Count; }
//    }

//    public override string ToString()
//    {
//        if (Count < 0)
//        {
//            return string.Empty;
//        }

//        StringBuilder result = new StringBuilder();

//        for (int i = 0; i < Vertexes.Count - 2; i++)
//        {
//            result.Append(string.Format("{0}, ", Vertexes[i]));
//        }
//        if (Vertexes.Count > 0)
//        {
//            result.Append(Vertexes[Vertexes.Count - 1]);
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
