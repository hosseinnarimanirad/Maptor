using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.Common.Primitives;

public class PointZMFactory : IPointFactory<PointZM>
{
    public PointZM Create(double x, double y, double[] coords)
        => new PointZM { X = x, Y = y, Z = coords[2], M = coords[3] };
}
