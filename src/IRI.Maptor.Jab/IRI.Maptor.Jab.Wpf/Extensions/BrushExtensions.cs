using IRI.Maptor.Jab.Wpf.Helpers;
using System.Windows.Media;

namespace IRI.Maptor.Extensions;

public static class BrushExtensions
{ 
    public static System.Windows.Media.Color? AsSolidColor(this Brush brush) => (brush as SolidColorBrush)?.Color;

    public static System.Drawing.Color? AsGdiSolidColor(this Brush brush, double? opacity = null) => brush.AsSolidColor()?.AsGdiColor(opacity);

    public static System.Drawing.Brush? AsGdiBrush(this Brush brush, double? opacity = null)
    {
        var solidColor = brush.AsSolidColor()?.AsGdiColor(opacity);

        return solidColor != null ? new System.Drawing.SolidBrush(solidColor.Value) : null;
    }
}
