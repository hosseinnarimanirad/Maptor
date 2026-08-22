using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace IRI.Maptor.Core.Common.Helpers;

/// <summary>
/// Turns the coordinate text people actually paste — from a GPS receiver, a web map URL,
/// a chat message or a survey sheet — into numbers. Tolerant by design: any mix of
/// decimal degrees, degrees-minutes(-seconds), hemisphere letters (prefix or suffix),
/// typographic degree/minute/second marks, Persian or Arabic-Indic digits and the Persian
/// decimal/thousands separators is accepted.
/// </summary>
/// <remarks>
/// Recognised pair layouts (latitude first unless hemisphere letters say otherwise):
/// <list type="bullet">
/// <item><c>35.6892, 51.3890</c> · <c>35.6892 51.3890</c> · <c>35.6892;51.3890</c></item>
/// <item><c>35°41'21.1"N 51°23'20.4"E</c> · <c>35 41 21.1 N, 51 23 20.4 E</c></item>
/// <item><c>N35 41.352 E51 23.340</c> · <c>35.6892N 51.3890E</c> · <c>51.3890E 35.6892N</c></item>
/// <item><c>-33.8688, 151.2093</c> · <c>۳۵٫۶۸۹۲، ۵۱٫۳۸۹۰</c></item>
/// <item>Google / OSM style URLs: <c>…/@35.6892,51.389,15z</c> · <c>…?q=35.6892,51.389</c> · <c>…#map=15/35.6892/51.3890</c></item>
/// </list>
/// A bare comma is always a separator between the two axes, never a decimal mark.
/// </remarks>
public static class CoordinateTextParser
{
    private static readonly Regex NumberRegex = new Regex(@"[-+]?(?:\d+\.?\d*|\.\d+)", RegexOptions.Compiled);

    private static readonly Regex UtmRegex = new Regex(
        @"^\s*(?:UTM\s*)?(?<zone>\d{1,2})\s*(?<hemi>[NS])(?:\s*[,;]\s*|\s+)(?<x>[-+]?\d+\.?\d*)\s*(?:M|E)?(?:\s*[,;]\s*|\s+)(?<y>[-+]?\d+\.?\d*)\s*(?:M|N)?\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    #region Single numbers

    /// <summary>
    /// Parses a plain number, accepting Persian/Arabic-Indic digits and the Persian decimal
    /// separator, and ignoring thousands separators (<c>,</c> <c>٬</c> and spaces).
    /// Always invariant: the UI culture is never consulted.
    /// </summary>
    public static bool TryParseNumber(string? text, out double value)
    {
        value = double.NaN;

        if (string.IsNullOrWhiteSpace(text))
            return false;

        var normalized = NormalizeDigits(text!)
            .Replace("٬", string.Empty)   // Arabic thousands separator
            .Replace(",", string.Empty)
            .Replace(" ", string.Empty)
            .Replace(" ", string.Empty)
            .Trim();

        return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
            && !double.IsInfinity(value);
    }

    /// <summary>
    /// Parses one angle: decimal degrees, or degrees / minutes / seconds in any common
    /// notation, with an optional hemisphere letter before or after. Returns signed decimal
    /// degrees (south and west are negative).
    /// </summary>
    public static bool TryParseAngle(string? text, out double degrees)
    {
        degrees = double.NaN;

        if (string.IsNullOrWhiteSpace(text))
            return false;

        var tokens = Tokenize(StripAxisWords(Normalize(text!)));

        return TryBuildAngle(tokens, out degrees, out _);
    }

    #endregion

    #region Pairs

    /// <summary>
    /// Parses a latitude / longitude pair in any of the layouts listed on the class.
    /// Values are signed decimal degrees and are range-checked (±90 / ±180).
    /// </summary>
    public static bool TryParseLatLong(string? text, out double latitude, out double longitude)
    {
        latitude = double.NaN;
        longitude = double.NaN;

        if (string.IsNullOrWhiteSpace(text))
            return false;

        var normalized = StripAxisWords(ExtractFromUrl(Normalize(text!)));

        if (!TrySplitAxes(normalized, out var first, out var second))
            return false;

        if (!TryBuildAngle(first, out var a, out var aAxis) || !TryBuildAngle(second, out var b, out var bAxis))
            return false;

        // Which one is the latitude? Hemisphere letters decide; otherwise assume "lat, lon"
        // unless that reading is impossible and the swapped one is not.
        bool firstIsLatitude;

        if (aAxis == Axis.Latitude || bAxis == Axis.Longitude)
            firstIsLatitude = true;
        else if (aAxis == Axis.Longitude || bAxis == Axis.Latitude)
            firstIsLatitude = false;
        else
            firstIsLatitude = !(Math.Abs(a) > 90 && Math.Abs(b) <= 90);

        if (aAxis != Axis.Unknown && bAxis != Axis.Unknown && aAxis == bAxis)
            return false; // "35N 51N" — two latitudes

        latitude = firstIsLatitude ? a : b;
        longitude = firstIsLatitude ? b : a;

        return IsValidLatitude(latitude) && IsValidLongitude(longitude);
    }

    /// <summary>
    /// Parses a UTM reading of the form <c>39N 534123 3950123</c> (optional leading "UTM",
    /// optional E/N or m unit letters, comma or semicolon separators).
    /// </summary>
    public static bool TryParseUtm(string? text, out int zone, out bool isNorth, out double easting, out double northing)
    {
        zone = 0;
        isNorth = true;
        easting = double.NaN;
        northing = double.NaN;

        if (string.IsNullOrWhiteSpace(text))
            return false;

        var match = UtmRegex.Match(Normalize(text!));

        if (!match.Success)
            return false;

        zone = int.Parse(match.Groups["zone"].Value, CultureInfo.InvariantCulture);

        if (zone < 1 || zone > 60)
            return false;

        isNorth = char.ToUpperInvariant(match.Groups["hemi"].Value[0]) == 'N';

        return double.TryParse(match.Groups["x"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out easting)
            && double.TryParse(match.Groups["y"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out northing);
    }

    #endregion

    #region Validation

    public static bool IsValidLatitude(double value) => !double.IsNaN(value) && value >= -90 && value <= 90;

    public static bool IsValidLongitude(double value) => !double.IsNaN(value) && value >= -180 && value <= 180;

    #endregion

    #region Normalisation

    /// <summary>
    /// Maps Persian (U+06F0–U+06F9) and Arabic-Indic (U+0660–U+0669) digits to ASCII and the
    /// Persian decimal separator (U+066B) to a full stop.
    /// </summary>
    public static string NormalizeDigits(string text)
    {
        var sb = new StringBuilder(text.Length);

        foreach (var c in text)
        {
            if (c >= '۰' && c <= '۹')
                sb.Append((char)('0' + (c - '۰')));
            else if (c >= '٠' && c <= '٩')
                sb.Append((char)('0' + (c - '٠')));
            else if (c == '٫')
                sb.Append('.');
            else
                sb.Append(c);
        }

        return sb.ToString();
    }

    private static string Normalize(string text)
    {
        var sb = new StringBuilder(NormalizeDigits(text).Length);

        foreach (var c in NormalizeDigits(text))
        {
            switch (c)
            {
                // degree marks
                case '°': case 'º': case '˚': case '⁰': case 'ᵒ':
                    sb.Append('°');
                    break;

                // minute marks
                case '\'': case '′': case '’': case 'ʹ': case '´': case '`':
                    sb.Append('\'');
                    break;

                // second marks
                case '"': case '″': case '”': case 'ʺ': case '“':
                    sb.Append('"');
                    break;

                // hard separators between the two axes
                case '،': case ';': case '\t': case '|':
                    sb.Append(',');
                    break;

                // no-break space
                case '\u00A0':
                    sb.Append(' ');
                    break;

                // unicode minus
                case '−': case '–':
                    sb.Append('-');
                    break;

                default:
                    sb.Append(c);
                    break;
            }
        }

        return sb.ToString().Trim().ToUpperInvariant();
    }

    /// <summary>
    /// "lat: 35.6, lon: 51.3" — the words are noise; the order they imply is the default anyway.
    /// Runs after URL extraction so that query keys such as <c>mlat=</c> are still intact there.
    /// </summary>
    private static string StripAxisWords(string text)
    {
        foreach (var word in new[] { "LATITUDE", "LONGITUDE", "LAT", "LONG", "LON", "LNG", ":" })
            text = text.Replace(word, " ");

        return text.Trim();
    }

    /// <summary>
    /// Pulls the coordinate part out of a pasted map URL; returns the input untouched when it
    /// does not look like one.
    /// </summary>
    private static string ExtractFromUrl(string text)
    {
        if (text.IndexOf("HTTP", StringComparison.Ordinal) < 0 && text.IndexOf('/') < 0 && text.IndexOf('=') < 0)
            return text;

        // Google Maps: .../@35.6892,51.389,15z
        var at = text.LastIndexOf('@');
        if (at >= 0)
            return CutCoordinateRun(text.Substring(at + 1));

        // query-string keys carrying "lat,lon"
        foreach (var key in new[] { "Q=", "LL=", "QUERY=", "CENTER=", "SLL=", "DESTINATION=", "MLAT=" })
        {
            var i = text.IndexOf(key, StringComparison.Ordinal);
            if (i >= 0)
            {
                var run = CutCoordinateRun(text.Substring(i + key.Length));

                // ?mlat=35.1&mlon=51.2 — two separate keys
                if (key == "MLAT=")
                {
                    var j = text.IndexOf("MLON=", StringComparison.Ordinal);
                    if (j >= 0)
                        return run + "," + CutCoordinateRun(text.Substring(j + 5));
                }

                return run;
            }
        }

        // OpenStreetMap: #map=15/35.6892/51.3890
        var map = text.IndexOf("MAP=", StringComparison.Ordinal);
        if (map >= 0)
        {
            var parts = text.Substring(map + 4).Split(new[] { '/', '&' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3)
                return parts[1] + "," + parts[2];
        }

        return text;
    }

    private static string CutCoordinateRun(string text)
    {
        int end = 0;

        while (end < text.Length && (char.IsDigit(text[end]) || text[end] == '.' || text[end] == ',' || text[end] == '-' || text[end] == '+' || text[end] == ' '))
            end++;

        var run = text.Substring(0, end);

        // drop a trailing zoom level: "35.68,51.38,15" → "35.68,51.38"
        var fields = run.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        if (fields.Length > 2)
            run = fields[0] + "," + fields[1];

        return run;
    }

    #endregion

    #region Tokens

    private enum Axis { Unknown, Latitude, Longitude }

    private readonly struct Token
    {
        public readonly double Number;
        public readonly char Hemisphere; // 'N' 'S' 'E' 'W' or '\0'

        public Token(double number) { Number = number; Hemisphere = '\0'; }
        public Token(char hemisphere) { Number = double.NaN; Hemisphere = hemisphere; }

        public bool IsHemisphere => Hemisphere != '\0';
    }

    private static List<Token> Tokenize(string text)
    {
        var tokens = new List<Token>();

        int i = 0;
        while (i < text.Length)
        {
            var c = text[i];

            if (c == 'N' || c == 'S' || c == 'E' || c == 'W')
            {
                // a hemisphere letter must stand alone, not start a word ("EAST" is fine too:
                // the following letters are skipped as noise)
                tokens.Add(new Token(c));
                i++;
                while (i < text.Length && char.IsLetter(text[i]))
                    i++;
                continue;
            }

            if (char.IsDigit(c) || c == '.' || ((c == '-' || c == '+') && i + 1 < text.Length && (char.IsDigit(text[i + 1]) || text[i + 1] == '.')))
            {
                var m = NumberRegex.Match(text, i);
                if (m.Success && m.Index == i)
                {
                    if (!double.TryParse(m.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                        return new List<Token>();

                    tokens.Add(new Token(number));
                    i += m.Length;
                    continue;
                }
            }

            if (char.IsLetter(c))
                return new List<Token>(); // any other word means this is not a coordinate

            i++; // ° ' " , spaces and other punctuation carry no meaning beyond separating numbers
        }

        return tokens;
    }

    /// <summary>
    /// Splits the text into the two axes. A comma is authoritative; without one the split
    /// follows the hemisphere letters, and failing that the numbers are halved.
    /// </summary>
    private static bool TrySplitAxes(string text, out List<Token> first, out List<Token> second)
    {
        first = new List<Token>();
        second = new List<Token>();

        var commaParts = text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

        if (commaParts.Length == 2)
        {
            first = Tokenize(commaParts[0]);
            second = Tokenize(commaParts[1]);
            return first.Count > 0 && second.Count > 0;
        }

        if (commaParts.Length > 2)
            return false;

        var tokens = Tokenize(text);

        if (tokens.Count < 2)
            return false;

        int hemisphereCount = 0;
        foreach (var t in tokens)
            if (t.IsHemisphere)
                hemisphereCount++;

        if (hemisphereCount == 2)
        {
            int split;

            if (tokens[0].IsHemisphere)
            {
                // prefix style: N35 41.3 E51 23.3 → split before the second letter
                split = 1;
                while (split < tokens.Count && !tokens[split].IsHemisphere)
                    split++;
            }
            else
            {
                // suffix style: 35 41.3 N 51 23.3 E → split after the first letter
                split = 0;
                while (split < tokens.Count && !tokens[split].IsHemisphere)
                    split++;
                split++;
            }

            first = tokens.GetRange(0, split);
            second = tokens.GetRange(split, tokens.Count - split);
            return first.Count > 0 && second.Count > 0;
        }

        if (hemisphereCount != 0)
            return false; // one letter for two axes is ambiguous

        if (tokens.Count % 2 != 0 || tokens.Count > 6)
            return false;

        int half = tokens.Count / 2;
        first = tokens.GetRange(0, half);
        second = tokens.GetRange(half, half);
        return true;
    }

    private static bool TryBuildAngle(List<Token> tokens, out double degrees, out Axis axis)
    {
        degrees = double.NaN;
        axis = Axis.Unknown;

        if (tokens.Count == 0)
            return false;

        char hemisphere = '\0';
        var numbers = new List<double>(3);

        for (int i = 0; i < tokens.Count; i++)
        {
            var t = tokens[i];

            if (t.IsHemisphere)
            {
                // one letter, and only at the start or the end
                if (hemisphere != '\0' || (i != 0 && i != tokens.Count - 1))
                    return false;

                hemisphere = t.Hemisphere;
            }
            else
            {
                numbers.Add(t.Number);
            }
        }

        if (numbers.Count == 0 || numbers.Count > 3)
            return false;

        // only the degrees may carry a sign; minutes and seconds must be 0 ≤ v < 60
        for (int i = 1; i < numbers.Count; i++)
        {
            if (numbers[i] < 0 || numbers[i] >= 60)
                return false;
        }

        // a fractional degree or minute cannot be followed by a finer unit (35.5° 20′ is nonsense)
        for (int i = 0; i < numbers.Count - 1; i++)
        {
            if (Math.Abs(numbers[i]) % 1 != 0)
                return false;
        }

        bool negative = numbers[0] < 0 || (numbers[0] == 0 && 1 / numbers[0] < 0);

        double magnitude = Math.Abs(numbers[0]);

        if (numbers.Count > 1)
            magnitude += numbers[1] / 60.0;

        if (numbers.Count > 2)
            magnitude += numbers[2] / 3600.0;

        switch (hemisphere)
        {
            case 'N':
                axis = Axis.Latitude;
                break;
            case 'S':
                axis = Axis.Latitude;
                negative = true;
                break;
            case 'E':
                axis = Axis.Longitude;
                break;
            case 'W':
                axis = Axis.Longitude;
                negative = true;
                break;
        }

        degrees = negative ? -magnitude : magnitude;

        return true;
    }

    #endregion
}
