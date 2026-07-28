using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.Mathematics;

public class StatisticsSummary
{
    public double Min { get; set; }

    public double FirstQuartile { get; set; }

    //actually this is the second quartile
    public double Median { get; set; }

    public double ThirdQuartile { get; set; }

    public double Max { get; set; }

    public double Mean { get; set; }

    public override string ToString()
    {
        return $"Min: {Min:N2}, Max: {Max:N2}, Mean: {Mean:N2}, FirstQuartile: {FirstQuartile:N2}, SecondQuartile (Median): {Median:N2}, ThirdQuartile: {ThirdQuartile:N2}";
    }

    public string AsCsvLine(string separator, string numberFormat)
    {
        var result = new List<string>()
        {
            Min.ToString(numberFormat),
            Max.ToString(numberFormat),
            Mean.ToString(numberFormat),
            FirstQuartile.ToString(numberFormat),
            Median.ToString(numberFormat),
            ThirdQuartile.ToString(numberFormat),            
        };

        return string.Join(separator, result);
    }
}
