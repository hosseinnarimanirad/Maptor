using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using IRI.Maptor.Sta.Common.Abstractions;
using IRI.Maptor.Sta.Common.Primitives;

namespace IRI.Maptor.Sta.Ogc.SFA;

[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct OgcPointM : IOgcPoint, IHasM
{
    double x;

    double y;

    double m;

    public double X { get { return this.x; } }

    public double Y { get { return this.y; } }

    public double M { get { return this.m; } }

    //public PointType EsriType => PointType.Normal;

    public OgcPointM(double x, double y, double m)
    {
        this.x = x;

        this.y = y;

        this.m = m;
    }
}
