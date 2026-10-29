using System.Globalization;

namespace IRI.Maptor.Core.Satellite.Elements;

/// <summary>
/// Reads one two-line element set (TLE) into <see cref="OrbitalElements"/>.
/// </summary>
/// <remarks>
/// Fields are read from their fixed columns (CelesTrak's "TLE format" page), so anything after
/// column 69 — such as the start/stop/step columns of Vallado's verification file — is ignored.
/// Both lines' checksums are verified: a single wrong digit in a TLE moves the satellite, and the
/// checksum is the only protection against a mistyped or truncated line.
/// </remarks>
public static class TwoLineElement
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    /// <param name="line1">Line 1, starting "1 ".</param>
    /// <param name="line2">Line 2, starting "2 ".</param>
    /// <param name="name">The optional name line of a 3-line set (a leading "0 " is removed).</param>
    /// <exception cref="FormatException">The lines are not a valid TLE.</exception>
    public static OrbitalElements Parse(string line1, string line2, string? name = null)
    {
        if (!TryParse(line1, line2, name, out var elements, out var error))
            throw new FormatException(error);

        return elements!;
    }

    public static bool TryParse(string line1, string line2, string? name, out OrbitalElements? elements, out string? error)
    {
        elements = null;

        error = Validate(line1, nameof(line1), '1') ?? Validate(line2, nameof(line2), '2');

        if (error is not null)
            return false;

        var catalog1 = line1.Substring(2, 5).Trim();
        var catalog2 = line2.Substring(2, 5).Trim();

        if (catalog1 != catalog2)
        {
            error = $"Line 1 is for catalog number {catalog1} but line 2 for {catalog2}.";
            return false;
        }

        try
        {
            var epochYear = int.Parse(line1.Substring(18, 2), Invariant);
            var epochDay = Double(line1, 20, 12);

            elements = new OrbitalElements(
                catalogNumber: catalog1,
                epoch: EpochToUtc(epochYear, epochDay),
                inclination: Double(line2, 8, 8),
                rightAscension: Double(line2, 17, 8),
                eccentricity: Double("0." + line2.Substring(26, 7).Trim(), 0, null),
                argumentOfPerigee: Double(line2, 34, 8),
                meanAnomaly: Double(line2, 43, 8),
                meanMotion: Double(line2, 52, 11),
                bStar: AssumedDecimal(line1.Substring(53, 8)),
                meanMotionDot: Double(line1, 33, 10),
                meanMotionDdot: AssumedDecimal(line1.Substring(44, 8)),
                name: CleanName(name),
                internationalDesignator: line1.Substring(9, 8),
                classification: line1[7] == ' ' ? 'U' : line1[7],
                elementSetNumber: IntOrZero(line1.Substring(64, 4)),
                revolutionNumber: IntOrZero(line2.Substring(63, 5)));

            return true;
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or ArgumentException)
        {
            elements = null;
            error = $"Catalog number {catalog1}: {ex.Message}";
            return false;
        }
    }

    /// <summary>
    /// The TLE checksum of a line: the sum of its first 68 characters' digits, with each minus sign
    /// counting as 1, modulo 10.
    /// </summary>
    public static int Checksum(string line)
    {
        if (line is null)
            throw new ArgumentNullException(nameof(line));

        int sum = 0;

        for (int i = 0; i < Math.Min(68, line.Length); i++)
        {
            var c = line[i];

            if (c >= '0' && c <= '9')
                sum += c - '0';
            else if (c == '-')
                sum += 1;
        }

        return sum % 10;
    }

    /// <summary>A two-digit year: 57–99 is 1957–1999 (Sputnik onwards), 00–56 is 2000–2056; day 1.0 is 1 January 00:00.</summary>
    public static DateTime EpochToUtc(int twoDigitYear, double dayOfYear)
    {
        var year = twoDigitYear < 57 ? 2000 + twoDigitYear : 1900 + twoDigitYear;

        return new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks((long)Math.Round((dayOfYear - 1.0) * TimeSpan.TicksPerDay));
    }

    private static string? Validate(string line, string lineName, char number)
    {
        if (line is null)
            return $"{lineName} is missing.";

        if (line.Length < 69)
            return $"{lineName} has {line.Length} characters; a TLE line has 69.";

        if (line[0] != number || line[1] != ' ')
            return $"{lineName} must start with \"{number} \".";

        if (!char.IsDigit(line[68]))
            return $"{lineName} has no checksum digit in column 69.";

        var expected = Checksum(line);

        if (line[68] - '0' != expected)
            return $"{lineName} fails its checksum (column 69 says {line[68]}, the line sums to {expected}).";

        return null;
    }

    private static double Double(string line, int start, int? length)
    {
        var text = length is int n ? line.Substring(start, n) : line;

        return double.Parse(text.Trim(), NumberStyles.Float, Invariant);
    }

    /// <summary>The TLE "assumed decimal point" exponent form: " 28098-4" is 0.28098e-4.</summary>
    private static double AssumedDecimal(string field)
    {
        var text = field.Trim();

        if (text.Length == 0)
            return 0;

        var sign = 1.0;

        if (text[0] is '-' or '+')
        {
            sign = text[0] == '-' ? -1.0 : 1.0;
            text = text.Substring(1);
        }

        // the exponent is the trailing signed digit, e.g. "28098-4" → mantissa 28098, exponent -4
        var exponentAt = Math.Max(text.LastIndexOf('-'), text.LastIndexOf('+'));

        if (exponentAt <= 0)
            return sign * double.Parse("0." + text, NumberStyles.Float, Invariant);

        var mantissa = double.Parse("0." + text.Substring(0, exponentAt), NumberStyles.Float, Invariant);

        var exponent = int.Parse(text.Substring(exponentAt), NumberStyles.AllowLeadingSign, Invariant);

        return sign * mantissa * Math.Pow(10, exponent);
    }

    private static int IntOrZero(string field) => int.TryParse(field.Trim(), NumberStyles.Integer, Invariant, out var value) ? value : 0;

    private static string? CleanName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var text = name!.Trim();

        // the 3LE convention puts "0 " before the name
        return text.StartsWith("0 ", StringComparison.Ordinal) ? text.Substring(2).Trim() : text;
    }
}
