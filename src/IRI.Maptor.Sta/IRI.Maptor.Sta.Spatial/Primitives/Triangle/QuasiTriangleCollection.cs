//// besmellahe rahmane rahim
//// Allahomma ajjel le-valiyek al-faraj

//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace IRI.Maptor.Sta.Spatial.Primitives;

//public class QuasiTriangleCollection : IEnumerable<QuasiTriangle>
//{
//    List<QuasiTriangle> triangles;

//    List<int> codes;

//    Queue<int> queue;

//    public QuasiTriangleCollection()
//    {
//        triangles = new List<QuasiTriangle>();

//        codes = new List<int>();

//        queue = new Queue<int>();

//        queue.Enqueue(0);
//    }

//    public QuasiTriangleCollection(int length)
//    {
//        triangles = new List<QuasiTriangle>(length);

//        codes = new List<int>(length);

//        queue.Enqueue(0);
//    }

//    public QuasiTriangleCollection(List<QuasiTriangle> triangles)
//    {
//        this.triangles = new List<QuasiTriangle>();

//        codes = new List<int>();

//        RefreshCodes(triangles);
//    }

//    public QuasiTriangle this[int index]
//    {
//        get { return triangles[index]; }
//    }

//    public int Count
//    {
//        get { return triangles.Count; }
//    }

//    private void RefreshCodes(List<QuasiTriangle> triangles)
//    {
//        codes.Clear();

//        foreach (QuasiTriangle item in triangles)
//        {
//            int tempCode = item.GetHashCode();

//            if (!codes.Contains(tempCode))
//            {
//                this.triangles.Add(item);

//                codes.Add(tempCode);
//            }
//        }
//    }

//    public bool ContainsCode(int triangleCode)
//    {
//        return codes.Contains(triangleCode);
//    }

//    public bool Contains(QuasiTriangle triangle)
//    {
//        return triangles.Contains(triangle);
//    }

//    public void Add(QuasiTriangle newTriangle)
//    {
//        int tempHashCode = newTriangle.GetHashCode();

//        if (triangles.Contains(newTriangle))
//        {
//            throw new NotImplementedException();
//        }

//        if (codes.Contains(tempHashCode))
//        {
//            throw new NotImplementedException();
//            //return;
//        }

//        triangles.Add(newTriangle);

//        codes.Add(tempHashCode);
//    }

//    public void RemoveAt(int index)
//    {
//        if (index < 0 || index >= codes.Count)
//        {
//            throw new NotImplementedException();
//        }

//        queue.Enqueue(codes[index]);

//        codes.RemoveAt(index);

//        triangles.RemoveAt(index);
//    }

//    public void RemoveByCode(int triangleCode)
//    {
//        int index = codes.IndexOf(triangleCode);

//        RemoveAt(index);
//    }

//    public QuasiTriangle GetTriangle(int triangleCode)
//    {
//        int index = codes.IndexOf(triangleCode);

//        return index == -1 ? null : triangles[index];
//    }

//    public int GetNewCode()
//    {
//        //this.queue.Enqueue(
//        if (queue.Count < 1)
//        {
//            return Count;
//        }
//        else
//        {
//            return queue.Dequeue();
//        }
//    }

//    #region IEnumerable<QuasiTriangle> Members

//    public IEnumerator<QuasiTriangle> GetEnumerator()
//    {
//        return triangles.GetEnumerator();
//    }

//    #endregion

//    #region IEnumerable Members

//    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
//    {
//        return GetEnumerator();
//    }

//    #endregion
//}