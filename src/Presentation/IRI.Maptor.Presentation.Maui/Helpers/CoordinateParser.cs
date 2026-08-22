using System.Globalization;

namespace IRI.Maptor.Presentation.Maui.Helpers;

/// <summary>
/// Parses free-form "latitude, longitude" text such as <c>"35.6892, 51.3890"</c>,
/// <c>"35.6892 51.389"</c> or <c>"35.6892° , 51.389°"</c>.
/// </summary>
public static class CoordinateParser
{
    private static readonly char[] Separators = [',', ';', ' ', '\t'];

    public static bool TryParseLatLon(string? text, out double latitude, out double longitude)
    {
        latitude = 0;
        longitude = 0;

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var cleaned = text.Replace("°", " ").Trim();

        var parts = cleaned.Split(Separators, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 2)
        {
            return false;
        }

        if (!double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out latitude) ||
            !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out longitude))
        {
            return false;
        }

        return latitude is >= -90 and <= 90 && longitude is >= -180 and <= 180;
    }
}
