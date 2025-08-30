// BESMELLAHE RAHMANE RAHIM
// ALLAHOMMA AJJEL LE-VALIYEK AL-FARAJ

using System;
using System.Collections.Generic;

using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Common.Enums;

namespace IRI.Maptor.Sta.Spatial.Primitives;

[Serializable]
public class PointCollection : IEnumerable<Point>
{

    public static PointComparisonPriority ComparisonPriority = PointComparisonPriority.YBased;

    #region Fields

    protected List<Point> points = new List<Point>();

    protected List<int> codes = new List<int>();

    protected Queue<int> queue = new Queue<int>();

    #endregion

    #region Constructors

    public PointCollection()
        : this(new List<Point>())
    {
        queue.Enqueue(0);
    }

    public PointCollection(List<Point> points)
    {
        foreach (Point item in points)
        {
            Add(item);
        }
    }

    public PointCollection(List<double> x, List<double> y)
    {
        if (x.Count != y.Count)
        {
            throw new NotImplementedException();
        }

        for (int i = 0; i < x.Count; i++)
        {
            Point tempValue = new Point(x[i], y[i]);

            if (!points.Contains(tempValue))
            {
                Add(tempValue);
            }
        }
    }

    #endregion

    #region Indexers & Properties

    public virtual Point this[int index]
    {
        get { return points[index]; }

        //set 
        //{
        //    points[index] = value; 
        //}
    }

    public int Count
    {
        get { return points.Count; }
    }

    public int LowerBoundIndex
    {
        get
        {
            int index = 0;

            double x = points[index].X;

            double y = points[index].Y;

            for (int i = 1; i < Count; i++)
            {
                if (points[i].Y < y)
                {
                    index = i;

                    x = points[i].X;

                    y = points[i].Y;
                }
                else if (points[i].Y == y)
                {
                    if (points[i].X < x)
                    {
                        index = i;

                        x = points[i].X;

                        y = points[i].Y;
                    }
                }
            }

            return index;
        }
    }

    public int UpperBoundIndex
    {
        get
        {
            int index = 0;

            double x = points[index].X;

            double y = points[index].Y;

            for (int i = 1; i < Count; i++)
            {
                if (points[i].Y > y)
                {
                    index = i;

                    x = points[i].X;

                    y = points[i].Y;
                }
                else if (points[i].Y == y)
                {
                    if (points[i].X > x)
                    {
                        index = i;

                        x = points[i].X;

                        y = points[i].Y;
                    }
                }
            }

            return index;
        }
    }

    public double MinY
    {
        get
        {
            double result = points[0].Y;

            foreach (Point item in points)
            {
                if (item.Y < result)
                {
                    result = item.Y;
                }
            }

            return result;
        }
    }

    public double MinX
    {
        get
        {
            double result = points[0].X;

            foreach (Point item in points)
            {
                if (item.X < result)
                {
                    result = item.X;
                }
            }
            return result;
        }
    }

    public double MaxY
    {
        get
        {
            double result = points[0].Y;

            foreach (Point item in points)
            {
                if (item.Y > result)
                {
                    result = item.Y;
                }
            }
            return result;
        }
    }

    public double MaxX
    {
        get
        {
            double result = points[0].X;

            foreach (Point item in points)
            {
                if (item.X > result)
                {
                    result = item.X;
                }
            }
            return result;
        }
    }

    #endregion

    #region Methods

    public virtual void Add(Point newPoint)
    {
        if (points.Contains(newPoint))
        {
            throw new NotImplementedException();
            //return;
        }

        int newCode = GetNewCode();

        Point temp = new Point(newPoint.X, newPoint.Y/*, newCode*/);

        points.Add(temp);

        if (codes.Contains(newCode))
        {
            throw new NotImplementedException();
        }

        codes.Add(newCode);
    }

    public void Remove(Point point)
    {
        int index = points.IndexOf(point);

        RemoveAt(index);
    }

    public virtual void RemoveAt(int index)
    {
        if (index < 0 || index >= points.Count)
        {
            throw new NotImplementedException();
        }

        queue.Enqueue(codes[index]);

        points.RemoveAt(index);

        codes.RemoveAt(index);

    }

    public void RemoveByCode(int pointCode)
    {
        int index = codes.IndexOf(pointCode);

        RemoveAt(index);
    }

    public bool Contains(Point point)
    {
        return points.Contains(point);
    }

    public int IndexOf(Point point)
    {
        return points.LastIndexOf(point);
    }

    public int IndexOf(int pointCode)
    {
        return codes.IndexOf(pointCode);
    }

    public Point GetPoint(int pointCode)
    {
        return points[IndexOf(pointCode)];
    }

    public int GetNewCode()
    {
        if (queue.Count < 1)
        {
            return Count;
        }
        else
        {
            return queue.Dequeue();
        }
    }

    #endregion

    /*Sort
    //public void Sort(IRI.Maptor.Sta.DataStructures.SortDirection direction, PointComparisonPriority priority)
    //{
    //    Point.ComparisonPriority = priority;

    //    IRI.Maptor.Sta.DataStructures.SortAlgorithm.Heapsort<Point>(ref this.points, direction);

    //    //RefreshCodes();
    //}
    */

    #region IEnumerable<Point> Members

    public IEnumerator<Point> GetEnumerator()
    {
        return points.GetEnumerator();
    }

    #endregion

    #region IEnumerable Members

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    #endregion
}