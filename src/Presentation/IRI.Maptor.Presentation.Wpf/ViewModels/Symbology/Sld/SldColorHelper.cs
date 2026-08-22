using System.Globalization;
using System.Windows.Media;

namespace IRI.Maptor.Presentation.Wpf.ViewModels.Symbology;

/// <summary>
/// Shared conversions between WPF <see cref="Color"/> values and the
/// <c>#RRGGBB</c> hex strings used by the SLD CSS parameters / color-map entries.
/// </summary>
internal static class SldColorHelper
{
    public static string ToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    public static bool TryParseHexColor(string hex, out Color color)
    {
        color = Colors.Black;

        if (string.IsNullOrWhiteSpace(hex))
            return false;

        hex = hex.TrimStart('#');

        if (hex.Length == 6 &&
            byte.TryParse(hex.Substring(0, 2), NumberStyles.HexNumber, null, out var r) &&
            byte.TryParse(hex.Substring(2, 2), NumberStyles.HexNumber, null, out var g) &&
            byte.TryParse(hex.Substring(4, 2), NumberStyles.HexNumber, null, out var b))
        {
            color = Color.FromRgb(r, g, b);
            return true;
        }

        return false;
    }
}
