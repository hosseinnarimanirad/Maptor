using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq; 

namespace IRI.Maptor.Jab.Wpf.Cartography;
 
public class DiscreteRangeColor
{
    public double MinValue { get; }

    public double MaxValue { get; }

    public List<double> MidValues { get; }

    public List<Color> Colors { get; }

    public DiscreteRangeColor(List<double> values, List<Color> colors)
    {
        if (values.Count - 1 != colors.Count)
        {
            throw new NotImplementedException();
        }

        MinValue = values.Min();

        MaxValue = values.Max();

        MidValues = values;

        Colors = colors;
    }

    public Color Interpolate(double value)
    {
        if (value < MinValue)
        {
            return Colors.First();
        }
        else if (value > MaxValue)
        {
            return Colors.Last();
        }

        var index = MidValues.IndexOf(MidValues.First(v => v >= value));

        return Colors[index == 0 ? 0 : index - 1];
    }

    public static DiscreteRangeColor GetRangeForRisk(double minValue, double maxValue)
    {
        List<double> stepValues = Enumerable.Range(0, 10).Select(e => minValue + e * (maxValue - minValue) / 10.0).ToList();

        stepValues.Add(maxValue);

        List<Color> stepColors = new List<Color>()
                        {
                          Color.FromArgb(40, 145, 198),
                          Color.FromArgb(103, 165, 178),
                          Color.FromArgb(149, 190, 160),
                          Color.FromArgb(191, 211, 139),
                          Color.FromArgb(231, 237, 115),
                          Color.FromArgb(252, 228, 91),
                          Color.FromArgb(252, 179, 68),
                          Color.FromArgb(249, 134, 51),
                          Color.FromArgb(242, 84, 36),
                          Color.FromArgb(232, 16, 21),
                        };

        return new DiscreteRangeColor(stepValues, stepColors);
    }

    public static DiscreteRangeColor GetRangeForRisk(List<double> values)
    {
        var minValue = values.Min();

        var maxValue = values.Max();

        return GetRangeForRisk(minValue, maxValue);
    }

}
