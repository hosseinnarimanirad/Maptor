namespace IRI.Maptor.Presentation.Blazor.Toc;

/// <summary>
/// Converts the Maptor stack's stored colour strings into CSS colours.
///
/// The whole WPF/Maptor lineage persists colours as <c>#AARRGGBB</c> — alpha FIRST (that is what
/// <c>System.Windows.Media.Color</c> round-trips, and what the Saba LayerSetting seeds contain,
/// e.g. <c>#662E8BC7</c>). CSS hex is <c>#RRGGBBAA</c> — alpha LAST. Handing a stored value
/// straight to CSS therefore does not fail loudly; it silently paints the wrong colour, using the
/// alpha byte as red. This helper exists so no web component ever does that by accident.
/// </summary>
public static class HexColor
{
    /// <summary>
    /// Returns a CSS colour for a stored value, or <paramref name="fallback"/> when the input is
    /// absent or not a hex literal we recognise. Six-digit (<c>#RRGGBB</c>) and three-digit
    /// (<c>#RGB</c>) values are already CSS-shaped and pass through untouched.
    /// </summary>
    public static string ToCss(string? storedColor, string fallback = "transparent")
    {
        if (string.IsNullOrWhiteSpace(storedColor))
            return fallback;

        var value = storedColor.Trim();

        if (value[0] != '#')
            return fallback;

        var digits = value.AsSpan(1);

        if (!IsHex(digits))
            return fallback;

        return digits.Length switch
        {
            // #AARRGGBB -> #RRGGBBAA
            8 => $"#{digits[2]}{digits[3]}{digits[4]}{digits[5]}{digits[6]}{digits[7]}{digits[0]}{digits[1]}",

            // #ARGB -> #RGBA (the shorthand form of the same convention)
            4 => $"#{digits[1]}{digits[2]}{digits[3]}{digits[0]}",

            // Already CSS-shaped.
            6 or 3 => value,

            _ => fallback,
        };
    }

    private static bool IsHex(ReadOnlySpan<char> value)
    {
        foreach (var c in value)
        {
            var isHexDigit = c is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F';

            if (!isHexDigit)
                return false;
        }

        return value.Length > 0;
    }
}
