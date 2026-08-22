using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.Common.Primitives;

public class PointZFactory : IPointFactory<PointZ>
{
    public PointZ Create(double x, double y, double[] coords)
        => new PointZ { X = x, Y = y, Z = coords[2] };
}
