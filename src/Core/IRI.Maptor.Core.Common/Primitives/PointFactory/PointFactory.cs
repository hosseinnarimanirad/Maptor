using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.Common.Primitives;

public class PointFactory : IPointFactory<Point>
{
    public Point Create(double x, double y, double[] coords) => new Point(x, y);
}
