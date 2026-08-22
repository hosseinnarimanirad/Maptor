using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Media;
using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Common.Helpers;

namespace IRI.Maptor.Presentation.Wpf.Helpers;

public static class PenHelper
{
    public static System.Drawing.Pen AsGdiPen(string hexColor, float thickness)
    {
        var color = ColorHelper.ToGdiColor(hexColor);

        return new System.Drawing.Pen(color, thickness);
    }

    public static System.Drawing.Pen AsGdiPen(string hexColor, float thickness, double opacity)
    {
        var color = ColorHelper.ToGdiColor(hexColor);

        var alpha = opacity > 1 ? 255 : opacity < 0 ? 0 : opacity * 255;

        return new System.Drawing.Pen(System.Drawing.Color.FromArgb((int)alpha, color.R, color.G, color.B), thickness);
    }

}
