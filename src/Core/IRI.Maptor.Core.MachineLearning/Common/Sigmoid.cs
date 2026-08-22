using IRI.Maptor.Core.Common.Mathematics;
using IRI.Maptor.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.MachineLearning;

public class Sigmoid
{
    public static double CalculateSigmoid(double zValue)
    {
        return 1.0 / (1.0 + Math.Exp(-zValue));
    }

}
