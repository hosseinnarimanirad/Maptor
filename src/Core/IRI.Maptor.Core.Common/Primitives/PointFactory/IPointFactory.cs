using IRI.Maptor.Core.Common.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.Common.Primitives;

public interface IPointFactory<T> where T : IPoint
{
    // coords:
    //  x, y
    //  x, y, z
    //  x, y, m
    //  x, y, z, m
    T Create(double x, double y, double[] coords);
}
