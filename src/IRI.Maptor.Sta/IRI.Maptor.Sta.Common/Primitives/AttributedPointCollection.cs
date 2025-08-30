// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj


using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

//This class has some security issues: Points can be changed
//without the change in the values or visa versa
namespace IRI.Maptor.Sta.Spatial.DigitalTerrainModeling;

[Serializable]
public class AttributedPointCollection : PointCollection, IEnumerable<AttributedPoint>
{
    //Fields

    /*protected PointCollection points;*/

    protected List<double> values;

    //Constructors
    public AttributedPointCollection()
        : base()
    {
        values = new List<double>();
    }

    public AttributedPointCollection(List<double> x, List<double> y, List<double> values)
    {
        if (x.Count != y.Count || x.Count != values.Count)
        {
            throw new NotImplementedException();
        }

        for (int i = 0; i < x.Count; i++)
        {
            Add(x[i], y[i], values[i]);
        }
    }

    //Indexers

    public AttributedPoint this[int index]
    {
        get { return new AttributedPoint(points[index].X, points[index].Y, values[index]); }
    }


    public override void Add(Point newPoint)
    {
        Add(new AttributedPoint(newPoint.X, newPoint.Y, 0));
    }

    private void Add(double x, double y, double value)
    {
        //int newCode = this.GetNewCode();

        Point temp = new Point(x, y);

        if (points.Contains(temp))
        {
            //this.queue.Enqueue(newCode);
            return;
            //throw new NotImplementedException();
        }
        else
        {
            //this.points.Add(temp);
            base.Add(temp);
            //this.codes.Add(newCode);

            values.Add(value);
        }
    }

    public void Add(AttributedPoint point)
    {
        //int newCode = this.GetNewCode();

        Point temp = new Point(point.X, point.Y);

        if (points.Contains(temp))
        {
            //this.queue.Enqueue(newCode);

            //throw new NotImplementedException();
            return;
        }
        else
        {
            //this.points.Add(temp);
            base.Add(temp);
            //this.codes.Add(newCode);

            values.Add(point.Value);
        }
    }

    public override void RemoveAt(int index)
    {
        if (index < 0 || index > Count - 1)
        {
            throw new NotImplementedException();
        }

        base.RemoveAt(index);

        values.RemoveAt(index);
    }

    public double GetValue(Point point)
    {
        int index = codes.IndexOf(point.GetHashCode());

        if (index > -1)
        {
            return values[index];
        }

        throw new NotImplementedException();
    }

    #region IEnumerable<AttributedPoint> Members

    public new IEnumerator<AttributedPoint> GetEnumerator()
    {
        for (int i = 0; i < Count; i++)
        {
            yield return new AttributedPoint(points[i].X, points[i].Y, values[i]);
        }
    }

    #endregion

    #region IEnumerable Members

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    #endregion
}
