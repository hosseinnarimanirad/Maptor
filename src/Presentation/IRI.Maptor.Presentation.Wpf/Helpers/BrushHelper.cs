using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Media;
using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Common.Helpers;

namespace IRI.Maptor.Presentation.Wpf.Helpers;

public static class BrushHelper
{
    /// <summary>
    /// A colour for a new layer or symbol. Same as <see cref="PickGoodBrush"/>: it used to pick any
    /// named WPF brush, which included Transparent and near-white ones, so a new layer could open
    /// invisible.
    /// </summary>
    public static Brush PickBrush() => PickGoodBrush();

    /// <summary>The next colour of the curated palette, round-robin (<see cref="ColorHelper.GetNextWpfColor"/>).</summary>
    public static Brush PickGoodBrush() => new SolidColorBrush(ColorHelper.GetNextWpfColor());

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

    public static Brush? CreateBrush(string? hexColor) => string.IsNullOrEmpty(hexColor) ? null : new SolidColorBrush(ColorHelper.ToWpfColor(hexColor));

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
