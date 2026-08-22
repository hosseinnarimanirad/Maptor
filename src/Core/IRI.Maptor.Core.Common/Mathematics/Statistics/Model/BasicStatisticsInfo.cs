using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.Common.Mathematics;

public class BasicStatisticsInfo
{
    public double Mean { get; set; }

    public double StandardDeviation { get; set; }

    public BasicStatisticsInfo()
    {

    }

    public BasicStatisticsInfo(double[] values)
    {
        this.Mean = IRI.Maptor.Core.Common.Mathematics.Statistics.CalculateMean(values);

        this.StandardDeviation = Statistics.CalculateStandardDeviation(values);
    }

    public override string ToString()
    {
        return $"Mean: {Mean}, Std: {StandardDeviation}";
    }
}
