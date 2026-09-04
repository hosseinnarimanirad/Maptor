using System;

namespace IRI.Maptor.Core.SpatialReferenceSystem.MapProjections.Mgrs;

/// <summary>
/// The lookup tables behind MGRS: latitude bands, the 100 km square lettering, and the
/// zone-number rules — including the two places where the UTM grid stops being regular
/// (Norway at band V, Svalbard at band X).
/// </summary>
/// <remarks>
/// MGRS omits the letters <c>I</c> and <c>O</c> everywhere so they cannot be misread as 1 and 0,
/// which is why every alphabet here is a table rather than arithmetic on <c>'A'</c>.
/// </remarks>
internal static class MgrsBands
{
    /// <summary>Size of a 100 km square, in metres.</summary>
    internal const double OneHundredKm = 100000.0;

    /// <summary>The northing cycle of the row lettering: 20 rows of 100 km.</summary>
    internal const double NorthingCycle = 2000000.0;

    /// <summary>Northing added to southern-hemisphere UTM coordinates so they stay positive.</summary>
    internal const double SouthernFalseNorthing = 10000000.0;

    /// <summary>Lowest latitude MGRS covers without UPS.</summary>
    internal const double MinLatitude = -80.0;

    /// <summary>Highest latitude MGRS covers without UPS.</summary>
    internal const double MaxLatitude = 84.0;

    /// <summary>
    /// The latitude bands from 80°S to 84°N, 8° each except <c>X</c>, which is 12° (72°–84°)
    /// because the polar bands <c>Y</c>/<c>Z</c> start only at 84°.
    /// </summary>
    internal const string BandLetters = "CDEFGHJKLMNPQRSTUVWX";

    /// <summary>
    /// Row letters for the 100 km squares: <c>A</c>–<c>V</c> without <c>I</c> and <c>O</c>.
    /// The sequence repeats every 2 000 km of northing.
    /// </summary>
    internal const string RowLetters = "ABCDEFGHJKLMNPQRSTUV";

    /// <summary>
    /// Column letters for the 100 km squares, one set per <c>(zone - 1) % 3</c>. Each set covers
    /// eastings 100 000–900 000 in eight steps.
    /// </summary>
    private static readonly string[] ColumnLetterSets =
    {
        "ABCDEFGH",
        "JKLMNPQR",
        "STUVWXYZ",
    };

    /// <summary>
    /// The lowest UTM northing that can occur inside each latitude band, rounded down to a
    /// 100 km boundary. Decoding needs it because a row letter fixes the northing only modulo
    /// 2 000 km — this table says which of the repeats is meant. Southern values include the
    /// 10 000 km false northing.
    /// </summary>
    private static readonly double[] BandMinimumNorthings =
    {
        1100000.0, // C  -80..-72
        2000000.0, // D  -72..-64
        2800000.0, // E  -64..-56
        3700000.0, // F  -56..-48
        4600000.0, // G  -48..-40
        5500000.0, // H  -40..-32
        6400000.0, // J  -32..-24
        7300000.0, // K  -24..-16
        8200000.0, // L  -16..-8
        9100000.0, // M   -8..0
              0.0, // N    0..8
         800000.0, // P    8..16
        1700000.0, // Q   16..24
        2600000.0, // R   24..32
        3500000.0, // S   32..40
        4400000.0, // T   40..48
        5300000.0, // U   48..56
        6200000.0, // V   56..64
        7000000.0, // W   64..72
        7900000.0, // X   72..84
    };

    #region Latitude bands

    /// <summary>
    /// The band letter for a latitude, or the null character outside the 80°S–84°N range MGRS
    /// covers without UPS.
    /// </summary>
    internal static char GetBandLetter(double latitude)
    {
        if (double.IsNaN(latitude) || latitude < MinLatitude || latitude > MaxLatitude)
            return '\0';

        // X absorbs the last 12 degrees, so clamping the index also handles latitude == 84 exactly.
        var index = (int)Math.Floor((latitude - MinLatitude) / 8.0);

        if (index > BandLetters.Length - 1)
            index = BandLetters.Length - 1;

        return BandLetters[index];
    }

    internal static int IndexOfBand(char bandLetter) => BandLetters.IndexOf(char.ToUpperInvariant(bandLetter));

    /// <summary>Inclusive lower / exclusive upper latitude of a band, for validation.</summary>
    internal static (double min, double max) GetBandLatitudeRange(char bandLetter)
    {
        var index = IndexOfBand(bandLetter);

        if (index < 0)
            return (double.NaN, double.NaN);

        var min = MinLatitude + index * 8.0;

        return (min, index == BandLetters.Length - 1 ? MaxLatitude : min + 8.0);
    }

    internal static bool IsNorthernBand(char bandLetter) => IndexOfBand(bandLetter) >= IndexOfBand('N');

    internal static double GetBandMinimumNorthing(char bandLetter)
    {
        var index = IndexOfBand(bandLetter);

        return index < 0 ? double.NaN : BandMinimumNorthings[index];
    }

    #endregion

    #region 100 km square letters

    private static string GetColumnLetterSet(int zone) => ColumnLetterSets[(zone - 1) % 3];

    /// <summary>
    /// The column letter for a UTM easting. Eastings stay inside 100 000–900 000 m in every
    /// legal zone, so the index is always 0–7.
    /// </summary>
    internal static char GetColumnLetter(int zone, double easting)
    {
        var index = (int)Math.Floor(easting / OneHundredKm) - 1;

        if (index < 0 || index > 7)
            return '\0';

        return GetColumnLetterSet(zone)[index];
    }

    /// <summary>
    /// Easting of the west edge of the named 100 km column, or NaN when the letter is not in
    /// the zone's set.
    /// </summary>
    internal static double GetColumnEasting(int zone, char columnLetter)
    {
        var index = GetColumnLetterSet(zone).IndexOf(char.ToUpperInvariant(columnLetter));

        return index < 0 ? double.NaN : (index + 1) * OneHundredKm;
    }

    /// <summary>
    /// The row letter for a UTM northing. Odd zones start the sequence at <c>A</c> on the equator,
    /// even zones at <c>F</c> — a five-letter offset, which is why the full column+row pattern
    /// repeats every six zones rather than every three.
    /// </summary>
    internal static char GetRowLetter(int zone, double northing)
    {
        var row = (int)Math.Floor(northing / OneHundredKm) + GetRowOffset(zone);

        return RowLetters[Modulo(row, RowLetters.Length)];
    }

    /// <summary>
    /// Northing of the south edge of the named 100 km row, within the first 2 000 km cycle.
    /// Returns NaN when the letter is not an MGRS row letter.
    /// </summary>
    internal static double GetRowNorthing(int zone, char rowLetter)
    {
        var index = RowLetters.IndexOf(char.ToUpperInvariant(rowLetter));

        if (index < 0)
            return double.NaN;

        return Modulo(index - GetRowOffset(zone), RowLetters.Length) * OneHundredKm;
    }

    private static int GetRowOffset(int zone) => zone % 2 == 0 ? 5 : 0;

    private static int Modulo(int value, int divisor)
    {
        var result = value % divisor;

        return result < 0 ? result + divisor : result;
    }

    #endregion

    #region Zones

    /// <summary>
    /// The UTM zone for a position, honouring the two irregular areas of the grid.
    /// <see cref="MapProjects.FindUtmZone(double)"/> supplies the regular zone; only the Norway
    /// and Svalbard exceptions are MGRS's own.
    /// </summary>
    internal static int GetZone(double longitude, double latitude)
    {
        var normalized = NormalizeLongitude(longitude);

        var zone = (int)MapProjects.FindUtmZone(normalized);

        // Norway: zone 32 is widened westward over the band V so Bergen stays out of zone 31.
        if (latitude >= 56.0 && latitude < 64.0 && normalized >= 3.0 && normalized < 12.0)
            return 32;

        // Svalbard: over the band X, zones 31/33/35/37 are widened and 32/34/36 do not exist.
        if (latitude >= 72.0 && latitude < 84.0 && normalized >= 0.0 && normalized < 42.0)
        {
            if (normalized < 9.0)
                return 31;

            if (normalized < 21.0)
                return 33;

            if (normalized < 33.0)
                return 35;

            return 37;
        }

        return zone;
    }

    /// <summary>Maps any longitude onto [-180, 180).</summary>
    internal static double NormalizeLongitude(double longitude) => MapProjects.NormalizeLongitude(longitude);

    /// <summary>
    /// The longitude span of a grid zone cell — one zone within one latitude band. Normally the
    /// zone's plain six degrees, but the Norway and Svalbard exceptions redraw the cells there,
    /// and over band X three of the even zones do not exist at all (NaN is returned for those).
    /// </summary>
    internal static (double west, double east) GetGridZoneLongitudeRange(int zone, char bandLetter)
    {
        var band = char.ToUpperInvariant(bandLetter);

        // Norway: zone 32 is widened west to 3 E over band V, at zone 31's expense.
        if (band == 'V')
        {
            if (zone == 31)
                return (0.0, 3.0);

            if (zone == 32)
                return (3.0, 12.0);
        }

        // Svalbard: over band X the odd zones are widened and 32, 34 and 36 do not exist.
        if (band == 'X')
        {
            switch (zone)
            {
                case 31: return (0.0, 9.0);
                case 33: return (9.0, 21.0);
                case 35: return (21.0, 33.0);
                case 37: return (33.0, 42.0);
                case 32:
                case 34:
                case 36: return (double.NaN, double.NaN);
            }
        }

        return GetZoneLongitudeRange(zone);
    }

    /// <summary>The nominal six-degree longitude strip of a zone, ignoring the irregular cells.</summary>
    internal static (double west, double east) GetZoneLongitudeRange(int zone)
        => (6.0 * zone - 186.0, 6.0 * zone - 180.0);

    /// <summary>
    /// The longitude span of a whole zone across every band it appears in — the nominal strip
    /// widened wherever Norway or Svalbard pushes the zone outside it.
    /// </summary>
    internal static (double west, double east) GetWidestZoneLongitudeRange(int zone)
    {
        var (west, east) = GetZoneLongitudeRange(zone);

        foreach (var band in new[] { 'V', 'X' })
        {
            var (bandWest, bandEast) = GetGridZoneLongitudeRange(zone, band);

            if (double.IsNaN(bandWest))
                continue;

            west = Math.Min(west, bandWest);
            east = Math.Max(east, bandEast);
        }

        return (west, east);
    }

    #endregion
}
