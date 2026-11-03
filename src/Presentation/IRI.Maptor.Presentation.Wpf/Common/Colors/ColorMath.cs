using System;
using System.Globalization;
using System.Windows.Media;

namespace IRI.Maptor.Presentation.Wpf;

/// <summary>
/// A colour in HSV (also called HSB): <see cref="Hue"/> in degrees [0, 360),
/// <see cref="Saturation"/> and <see cref="Value"/> in [0, 1].
/// </summary>
public readonly record struct HsvColor(double Hue, double Saturation, double Value)
{
    public static HsvColor FromColor(Color color) => ColorMath.ToHsv(color);

    public Color ToColor(byte alpha = 255) => ColorMath.FromHsv(Hue, Saturation, Value, alpha);
}

/// <summary>
/// Conversions used by <see cref="Controls.MaptorColorPicker"/>: RGB ⇄ HSV, hex text, and alpha ⇄ opacity percent.
/// </summary>
public static class ColorMath
{
    /// <summary>
    /// RGB to HSV. Hue is 0 for greys (no saturation), where it is undefined; a picker that wants to
    /// keep the user's hue while they drag through grey must remember it itself.
    /// </summary>
    public static HsvColor ToHsv(Color color)
    {
        double r = color.R / 255.0, g = color.G / 255.0, b = color.B / 255.0;

        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));
        double delta = max - min;

        double hue = 0;

        if (delta > 0)
        {
            if (max == r)
                hue = 60 * (((g - b) / delta) % 6);
            else if (max == g)
                hue = 60 * (((b - r) / delta) + 2);
            else
                hue = 60 * (((r - g) / delta) + 4);

            if (hue < 0)
                hue += 360;
        }

        double saturation = max == 0 ? 0 : delta / max;

        return new HsvColor(hue, saturation, max);
    }

    /// <summary>
    /// HSV to RGB. Hue wraps (360 = 0, -30 = 330); saturation and value are clamped to [0, 1].
    /// </summary>
    public static Color FromHsv(double hue, double saturation, double value, byte alpha = 255)
    {
        hue = ((hue % 360) + 360) % 360;
        saturation = Math.Clamp(saturation, 0, 1);
        value = Math.Clamp(value, 0, 1);

        double c = value * saturation;
        double x = c * (1 - Math.Abs((hue / 60) % 2 - 1));
        double m = value - c;

        (double r, double g, double b) = (int)(hue / 60) switch
        {
            0 => (c, x, 0d),
            1 => (x, c, 0d),
            2 => (0d, c, x),
            3 => (0d, x, c),
            4 => (x, 0d, c),
            _ => (c, 0d, x),
        };

        return Color.FromArgb(alpha, ToByte(r + m), ToByte(g + m), ToByte(b + m));
    }

    /// <summary><c>#RRGGBB</c>, upper case. Alpha is not included: the picker shows opacity separately.</summary>
    public static string ToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    /// <summary>
    /// Parses hex colour text as a user types it: surrounding spaces and the leading <c>#</c> are
    /// optional. Accepts <c>RGB</c> (each digit doubled), <c>RRGGBB</c> (opaque) and <c>AARRGGBB</c>
    /// (alpha first, as WPF writes colours). Anything else fails.
    /// </summary>
    public static bool TryParseHex(string? text, out Color color)
    {
        color = Colors.Black;

        if (string.IsNullOrWhiteSpace(text))
            return false;

        var hex = text.Trim();

        if (hex.StartsWith('#'))
            hex = hex.Substring(1);

        if (!uint.TryParse(hex, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var v))
            return false;

        switch (hex.Length)
        {
            case 3:
                color = Color.FromRgb(Double((v >> 8) & 0xF), Double((v >> 4) & 0xF), Double(v & 0xF));
                return true;
            case 6:
                color = Color.FromRgb((byte)(v >> 16), (byte)(v >> 8), (byte)v);
                return true;
            case 8:
                color = Color.FromArgb((byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v);
                return true;
            default:
                return false;
        }

        static byte Double(uint nibble) => (byte)(nibble * 17);
    }

    /// <summary>Alpha 0–255 as a whole opacity percentage 0–100.</summary>
    public static int AlphaToPercent(byte alpha) => (int)Math.Round(alpha * 100.0 / 255, MidpointRounding.AwayFromZero);

    /// <summary>Opacity percentage (clamped to 0–100) as alpha 0–255.</summary>
    public static byte PercentToAlpha(double percent) => ToByte(Math.Clamp(percent, 0, 100) / 100);

    private static byte ToByte(double unit) => (byte)Math.Round(Math.Clamp(unit, 0, 1) * 255, MidpointRounding.AwayFromZero);
}
