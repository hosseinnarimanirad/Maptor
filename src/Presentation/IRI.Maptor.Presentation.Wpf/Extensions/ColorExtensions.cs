using System.Windows.Media;

namespace IRI.Maptor.Extensions;

public static class ColorExtensions
{
    public static System.Drawing.Color AsGdiColor(this Color color, double? opacity = null)
    {
        int alpha = (int)(opacity.HasValue && opacity.Value <= 1 && opacity.Value >= 0 ? opacity.Value * color.A : color.A);

        return System.Drawing.Color.FromArgb(alpha, color.R, color.G, color.B);
    }

    public static System.Drawing.SolidBrush AsGdiBrush(this Color color, double? opacity = null)
    {
        return new System.Drawing.SolidBrush(color.AsGdiColor(opacity));
    }

    public static string ToHexString(this Color color)
    {
        return color.ToString();
    }


}
