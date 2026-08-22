using Microsoft.Maui.Graphics;

namespace IRI.Maptor.Presentation.Maui.Layers;

/// <summary>
/// Produces a deterministic, distinct color for a vector MBTiles sub-layer from its id, so the same
/// layer keeps the same color across runs. Ported from the WPF <c>MbTilesVectorSymbology</c> (which
/// is WPF-only) to <see cref="Microsoft.Maui.Graphics.Color"/>.
/// </summary>
internal static class MbTilesColor
{
    public static Color FromId(string? id)
    {
        // Stable FNV-1a hash (string.GetHashCode is randomized across runs).
        uint hash = 2166136261;

        foreach (char c in id ?? string.Empty)
        {
            hash ^= c;
            hash *= 16777619;
        }

        double hue = hash % 360;
        return FromHsl(hue, 0.6, 0.5);
    }

    private static Color FromHsl(double hue, double saturation, double lightness)
    {
        double c = (1 - Math.Abs(2 * lightness - 1)) * saturation;
        double x = c * (1 - Math.Abs((hue / 60.0) % 2 - 1));
        double m = lightness - c / 2;

        double r = 0, g = 0, b = 0;

        if (hue < 60) { r = c; g = x; }
        else if (hue < 120) { r = x; g = c; }
        else if (hue < 180) { g = c; b = x; }
        else if (hue < 240) { g = x; b = c; }
        else if (hue < 300) { r = x; b = c; }
        else { r = c; b = x; }

        return Color.FromRgb((float)(r + m), (float)(g + m), (float)(b + m));
    }
}
