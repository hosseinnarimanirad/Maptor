using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using IRI.Maptor.Sta.Common.Abstrations;

namespace IRI.Maptor.Sta.Ogc.SFA;

[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct OgcPointZM : IOgcPoint, IHasZ, IHasM
{
    double x;

    double y;

    double z;

    double m;

    public double X { get { return this.x; } }

    public double Y { get { return this.y; } }

    public double Z { get { return this.z; } }

    public double M { get { return this.m; } }

    public OgcPointZM(double x, double y, double z, double m)
    {
        this.x = x;

        this.y = y;

        this.z = z;

        this.m = m;
    }
}
