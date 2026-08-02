using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq; 

namespace IRI.Maptor.Jab.Common.Cartography;
 
public class ContinuousRangeColor
{
    public double MinValue { get; }

    public double MaxValue { get; }

    public List<double> MidValues { get; }

    //public List<Color> Colors { get; }

    public List<ColorInterpolation> Interpolations { get; private set; }

    public ContinuousRangeColor(List<double> values, List<Color> colors)
    {
        if (values.Count - 1 != colors.Count)
        {
            throw new NotImplementedException();
        }

        MinValue = values.First();

        MaxValue = values.Last();

        MidValues = values;

        Interpolations = new List<ColorInterpolation>();

        for (int i = 0; i < colors.Count - 1; i++)
        {
            Interpolations.Add(new ColorInterpolation(colors[i], colors[i + 1]));
        }
    }

    public Color Interpolate(double value)
    {
        var index = MidValues.IndexOf(MidValues.First(v => v >= value));

        index = index == Interpolations.Count ? Interpolations.Count - 1 : index;

        return Interpolations[index].Interpolate(value, MidValues[index - 1], MidValues[index]);
    }
}
