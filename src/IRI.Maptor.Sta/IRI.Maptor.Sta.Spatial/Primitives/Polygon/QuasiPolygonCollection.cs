//// besmellahe rahmane rahim
//// Allahomma ajjel le-valiyek al-faraj

//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace IRI.Maptor.Sta.Spatial.Primitives;

//public class QuasiPolygonCollection : IEnumerable<QuasiPolygon>
//{
//    List<QuasiPolygon> polygons;

//    List<int> codes;

//    public QuasiPolygonCollection()
//    {
//        polygons = new List<QuasiPolygon>();

//        codes = new List<int>();
//    }

//    public QuasiPolygonCollection(int length)
//    {
//        polygons = new List<QuasiPolygon>(length);

//        codes = new List<int>(length);
//    }

//    public QuasiPolygonCollection(List<QuasiPolygon> polygons)
//    {
//        this.polygons = polygons;

//        codes = new List<int>();

//        RefreshCodes(polygons);
//    }

//    public QuasiPolygon this[int index]
//    {
//        get { return polygons[index]; }
//    }

//    public int Count
//    {
//        get { return polygons.Count; }
//    }

//    private void RefreshCodes(List<QuasiPolygon> polygons)
//    {
//        codes.Clear();

//        foreach (QuasiPolygon item in polygons)
//        {
//            int tempCode = item.GetHashCode();

//            if (!codes.Contains(tempCode))
//            {
//                this.polygons.Add(item);

//                codes.Add(tempCode);
//            }
//        }
//    }

//    public bool ContainsCode(int polygonCode)
//    {
//        return codes.Contains(polygonCode);
//    }

//    public void Add(QuasiPolygon newPolygon)
//    {
//        int tempHashCode = newPolygon.GetHashCode();

//        if (codes.Contains(tempHashCode))
//        {
//            throw new NotImplementedException();
//            //return;
//        }

//        polygons.Add(newPolygon);

//        codes.Add(tempHashCode);
//    }

//    public void RemoveAt(int index)
//    {
//        if (index < 0 || index >= codes.Count)
//        {
//            throw new NotImplementedException();
//        }

//        codes.RemoveAt(index);

//        polygons.RemoveAt(index);
//    }

//    public void RemoveByCode(int polygonCode)
//    {
//        int index = codes.IndexOf(polygonCode);

//        if (index == -1)
//        {
//            throw new NotImplementedException();
//        }

//        RemoveAt(index);
//    }

//    public QuasiPolygon GetPolygon(int polygonCode)
//    {
//        int index = codes.IndexOf(polygonCode);

//        return index == -1 ? null : polygons[codes.IndexOf(polygonCode)];
//    }

//    #region IEnumerable<QuasiTriangle> Members

//    public IEnumerator<QuasiPolygon> GetEnumerator()
//    {
//        return polygons.GetEnumerator();
//    }

//    #endregion

//    #region IEnumerable Members

//    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
//    {
//        return GetEnumerator();
//    }

//    #endregion
//}