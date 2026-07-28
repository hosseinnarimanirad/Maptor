using IRI.Maptor.Sta.Common.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.Common.Primitives;

public interface IPointFactory<T> where T : IPoint
{
    // coords:
    //  x, y
    //  x, y, z
    //  x, y, m
    //  x, y, z, m
    T Create(double x, double y, double[] coords);
}
