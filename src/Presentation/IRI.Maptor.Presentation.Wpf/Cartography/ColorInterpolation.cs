using System.Drawing;

namespace IRI.Maptor.Presentation.Wpf.Cartography;

public class ColorInterpolation
{
    private readonly Color _minColor;
    //private readonly Color _maxColor;

    int rangeR, rangeB, rangeG;

    public ColorInterpolation(Color minColor, Color maxColor)
    {
        _minColor = minColor;

        //_maxColor = maxColor;

        rangeR = maxColor.R - minColor.R;
        rangeG = maxColor.G - minColor.G;
        rangeB = maxColor.B - minColor.B;

    }

    public Color Interpolate(double value, double minValue, double maxValue)
    {
        var r = (int)(_minColor.R + rangeR / (maxValue - minValue) * (value - minValue));
        var g = (int)(_minColor.G + rangeG / (maxValue - minValue) * (value - minValue));
        var b = (int)(_minColor.B + rangeB / (maxValue - minValue) * (value - minValue));

        return Color.FromArgb(r, g, b);

    }

}
