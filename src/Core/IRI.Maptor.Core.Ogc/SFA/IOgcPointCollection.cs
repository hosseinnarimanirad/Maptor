using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRI.Maptor.Core.Ogc.SFA;

public interface IOgcPointCollection : IEnumerable<IOgcPoint>
{
    IOgcPoint this[int index] { get; set; }

    int Count { get; }
}
