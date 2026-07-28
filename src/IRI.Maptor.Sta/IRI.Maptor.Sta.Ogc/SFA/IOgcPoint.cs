using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using IRI.Maptor.Sta.Common.Abstractions;

namespace IRI.Maptor.Sta.Ogc.SFA;

public interface IOgcPoint  
{
    double X { get; }

    double Y { get; }


}
