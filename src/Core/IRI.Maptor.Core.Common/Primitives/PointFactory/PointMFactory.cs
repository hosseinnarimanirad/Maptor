using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.Common.Primitives;

public class PointMFactory : IPointFactory<PointM>
{
    public PointM Create(double x, double y, double[] coords)
        => new PointM { X = x, Y = y, M = coords[2] };
}
