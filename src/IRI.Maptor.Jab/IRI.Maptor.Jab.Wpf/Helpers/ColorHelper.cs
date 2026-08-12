using System;
using System.Windows.Media;
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Helpers;

namespace IRI.Maptor.Jab.Wpf.Helpers;

public static class ColorHelper
{
    public static Color ToWpfColor(string? hexColor, double opacity = 1.0)
    {
        if (string.IsNullOrEmpty(hexColor))
            return Colors.Transparent;

        if (!hexColor.StartsWith("#"))
            hexColor = $"#{hexColor}";

        var result = (Color)ColorConverter.ConvertFromString(hexColor);
         
        // Clamp opacity between 0 and 1
        opacity = Math.Clamp(opacity, 0.0, 1.0);

        // Apply opacity to the alpha channel
        result.A = (byte)Math.Round(result.A * opacity);

        return result;
    }

    public static System.Drawing.Color ToGdiColor(string hexColor, double opacity = 1.0)
    {
        if (string.IsNullOrEmpty(hexColor))
            return System.Drawing.Color.Transparent;

        if (!hexColor.StartsWith("#"))
            hexColor = $"#{hexColor}";

        var color = System.Drawing.ColorTranslator.FromHtml(hexColor);

        // Clamp opacity between 0.0 and 1.0
        opacity = Math.Clamp(opacity, 0.0, 1.0);

        // Scale existing alpha by opacity
        byte alpha = (byte)Math.Round(color.A * opacity);

        return System.Drawing.Color.FromArgb(alpha, color.R, color.G, color.B);
    }

    public static string ToHexString(System.Drawing.Color color)
    {
        //1395.10.20: returns color name for system defined colors!
        //return System.Drawing.ColorTranslator.ToHtml(color); 

        return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    public static Color GetRandomWpfColor()
    {
        int randomIndex = RandomHelper.Get(goodColors.Length);

        return ToWpfColor(goodColors[randomIndex]);
    }

    private readonly static string[] goodColors =
    {
        "#FFE51400", //red
        "#FF61A917", //green
        "#FF1CA1E2", //blue
        "#FF6900FF", //purple
        "#FFF1A30B", //orange
        "#FFA4C401", //lime    
        "#FFF572D0", //pink 
        "#FFE4C802", //yellow
        "#FF835A2C", //brown 
    };
      
}
