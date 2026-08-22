using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRI.Maptor.Core.Ogc.SFA;

public class OgcGeometryCollection<T> : List<T>, IOgcGeometryCollection where T : IOgcGeometry
{
    public OgcGeometryCollection(int capacity)
        : base(capacity)
    {

    }

    public OgcGeometryCollection(List<T> values)
    {
        this.AddRange(values);
    }

    public new IOgcGeometry this[int index]
    {
        get
        {
            return base[index];
        }
        set
        {
            base[index] = (T)value;
        }
    }

    public new IEnumerator<IOgcGeometry> GetEnumerator()
    {
        for (int i = 0; i < this.Count; i++)
        {
            yield return base[i];
        }
    }
}
