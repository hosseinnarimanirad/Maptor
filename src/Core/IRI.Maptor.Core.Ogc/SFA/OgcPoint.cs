using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace IRI.Maptor.Core.Ogc.SFA;

[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct OgcPoint : IOgcPoint
{
    double x;

    double y;

    public double X { get { return this.x; } }

    public double Y { get { return this.y; } }

    public OgcPoint(double x, double y)
    {
        this.x = x;

        this.y = y;
    }
}
