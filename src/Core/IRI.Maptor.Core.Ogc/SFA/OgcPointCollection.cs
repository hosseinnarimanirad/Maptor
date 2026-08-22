using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRI.Maptor.Core.Ogc.SFA;

public class OgcPointCollection<T> : List<T>, IOgcPointCollection where T : IOgcPoint
{

    public new IOgcPoint this[int index]
    {
        get { return base[index]; }

        set { base[index] = (T)value; }
    }
    
    public OgcPointCollection(int capacity)
        : base(capacity)
    {

    }

    public new IEnumerator<IOgcPoint> GetEnumerator()
    {
        for (int i = 0; i < this.Count; i++)
        {
            yield return base[i];
        }
    }
}
