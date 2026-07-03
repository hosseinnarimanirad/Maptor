using System.Windows.Media;

using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Ket.SqlitePersistence.MbTiles;
using IRI.Maptor.Jab.Common.Cartography.Symbologies;

namespace IRI.Maptor.Jab.Common.Cartography;

/// <summary>
/// Produces default per-layer symbology for vector MBTiles layers. Each layer gets a deterministic
/// colour derived from its id, and geometry-type-aware defaults (filled polygons, coloured lines,
/// small point markers). This is a sensible default — not a full Mapbox GL style.
/// </summary>
public static class MbTilesVectorSymbology
{
    public static VisualParameters For(MvtVectorLayerInfo info)
    {
        var color = ColorFromId(info.Id);

        switch (info.GeometryType)
        {
            case GeometryType.Polygon:
            case GeometryType.MultiPolygon:
                return VisualParameters.Get(WithAlpha(color, 90), color, 0.6);

            case GeometryType.LineString:
            case GeometryType.MultiLineString:
                return VisualParameters.GetStroke(color, 1.2);

            case GeometryType.Point:
            case GeometryType.MultiPoint:
                var point = VisualParameters.Get(WithAlpha(color, 220), Darken(color), 1);
                point.PointSymbol = new SimplePointSymbolizer(3);
                return point;

            default:
                // Unknown geometry type: a universal style that renders fills, lines and points.
                var universal = VisualParameters.Get(WithAlpha(color, 70), color, 0.8);
                universal.PointSymbol = new SimplePointSymbolizer(3);
                return universal;
        }
    }

    private static Color ColorFromId(string? id)
    {
        // Stable FNV-1a hash so colours are reproducible across runs (string.GetHashCode is randomized).
        uint hash = 2166136261;

        foreach (char c in id ?? string.Empty)
        {
            hash ^= c;
            hash *= 16777619;
        }

        double hue = hash % 360;
        return FromHsl(hue, 0.6, 0.5);
    }

    private static Color WithAlpha(Color color, byte alpha) => Color.FromArgb(alpha, color.R, color.G, color.B);

    private static Color Darken(Color color) =>
        Color.FromArgb(color.A, (byte)(color.R * 0.7), (byte)(color.G * 0.7), (byte)(color.B * 0.7));

    private static Color FromHsl(double hue, double saturation, double lightness)
    {
        double c = (1 - System.Math.Abs(2 * lightness - 1)) * saturation;
        double x = c * (1 - System.Math.Abs((hue / 60.0) % 2 - 1));
        double m = lightness - c / 2;

        double r = 0, g = 0, b = 0;

        if (hue < 60) { r = c; g = x; }
        else if (hue < 120) { r = x; g = c; }
        else if (hue < 180) { g = c; b = x; }
        else if (hue < 240) { g = x; b = c; }
        else if (hue < 300) { r = x; b = c; }
        else { r = c; b = x; }

        return Color.FromRgb(
            (byte)System.Math.Round((r + m) * 255),
            (byte)System.Math.Round((g + m) * 255),
            (byte)System.Math.Round((b + m) * 255));
    }
}
