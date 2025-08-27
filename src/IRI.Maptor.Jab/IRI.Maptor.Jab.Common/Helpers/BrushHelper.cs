using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Media;
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Helpers;

namespace IRI.Maptor.Jab.Common.Helpers;

public static class BrushHelper
{
    public static Brush PickBrush()
    {
        Type brushesType = typeof(Brushes);

        PropertyInfo[] properties = brushesType.GetProperties();

        int randomIndex = RandomHelper.Get(properties.Length);

        var result = properties[randomIndex].GetValue(null, null) as SolidColorBrush ?? Brushes.Transparent;

        return new SolidColorBrush(result.Color);
    }

    public static Brush PickGoodBrush() => new SolidColorBrush(ColorHelper.GetRandomWpfColor());

    public static List<SolidColorBrush> GetAllKnownColors()
    {
        Type ColorType = typeof(System.Windows.Media.Colors);

        PropertyInfo[] properties = ColorType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        List<SolidColorBrush> result = new List<SolidColorBrush>(properties.Length);

        foreach (PropertyInfo property in properties)
        {
            if (property.GetValue(null) is Color color)
            {
                result.Add(new SolidColorBrush(color));
            }
        }

        return result;
    }

    public static Brush CreateFromNameOrHex(string colorNameOrHex)
    {
        var converter = new BrushConverter();

        return converter.ConvertFromString(colorNameOrHex) as SolidColorBrush ?? Brushes.Transparent;
    }

    public static Brush CreateBrush(string hexColor) => new SolidColorBrush(ColorHelper.ToWpfColor(hexColor));

    public static Brush CreateBrush(string hexColor, double opacity) => new SolidColorBrush(ColorHelper.ToWpfColor(hexColor, opacity));

    public static Brush CreateBrush(Color color, double opacity)
    {
        // Clamp opacity between 0.0 and 1.0
        opacity = Math.Clamp(opacity, 0.0, 1.0);

        // Scale existing alpha
        byte alpha = (byte)Math.Round(color.A * opacity);
 
        return new SolidColorBrush(Color.FromArgb((byte)alpha, r: color.R, g: color.G, b: color.B));
    }

    public static System.Drawing.Brush CreateGdiBrush(string hexColor)
    {
        return new System.Drawing.SolidBrush(ColorHelper.ToGdiColor(hexColor));
    }

    public static System.Drawing.Brush CreateGdiBrush(string hexColor, double opacity)
    {
        var color = ColorHelper.ToGdiColor(hexColor, opacity);
         
        return new System.Drawing.SolidBrush(color);
    }

    public static System.Drawing.Brush CreateGdiBrush(System.Drawing.Color color, double opacity)
    {
        // Clamp opacity between 0.0 and 1.0
        opacity = Math.Clamp(opacity, 0.0, 1.0);

        // Scale existing alpha and round
        byte alpha = (byte)Math.Round(color.A * opacity);


        return new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(alpha: alpha, red: color.R, green: color.G, blue: color.B));
    }

    public static System.Drawing.Brush? ToGdiBrush(Brush brush, double? opacity = null)
    {
        var solidColor = brush.AsSolidColor()?.AsGdiColor(opacity);

        return solidColor != null ? new System.Drawing.SolidBrush(solidColor.Value) : null;
    }


}
