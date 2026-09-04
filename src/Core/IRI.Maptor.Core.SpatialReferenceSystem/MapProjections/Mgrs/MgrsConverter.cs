using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using IRI.Maptor.Core.Common.Abstractions;
using IRI.Maptor.Core.Common.Primitives;

using Ellipsoid = IRI.Maptor.Core.SpatialReferenceSystem.Ellipsoid<IRI.Maptor.Core.Common.Metrics.Meter, IRI.Maptor.Core.Common.Metrics.Degree>;

namespace IRI.Maptor.Core.SpatialReferenceSystem.MapProjections.Mgrs;

/// <summary>
/// Reads and writes MGRS (Military Grid Reference System) references such as
/// <c>39S WV 53516 39501</c>.
/// </summary>
/// <remarks>
/// <para>
/// MGRS is not a projection — it is a <em>text encoding</em> of UTM coordinates, which is why it
/// lives here rather than beside <see cref="MapProjectionBase"/>: there is no continuous x/y plane
/// to project onto, only a square named by letters and digits. A reference names a square, not a
/// point; the value returned by <see cref="ToGeodetic(string, bool)"/> is that square's south-west
/// corner unless the centre is asked for.
/// </para>
/// <para>
/// Coverage is the UTM band, 80°S to 84°N. The polar caps (UPS, bands A/B/Y/Z) need a polar
/// stereographic projection that this library does not have, so positions outside that range are
/// rejected.
/// </para>
/// <para>All conversions use WGS84 unless another ellipsoid is passed.</para>
/// <para>
/// Decoding is deliberately lenient about one thing: a row letter fixes the northing only
/// modulo 2 000 km and the band picks the repeat, but nothing here re-checks that the position
/// which comes out really falls inside that band. <c>39N SK</c> is 16 km north of band N and is
/// still resolved rather than rejected. Every reference a correct encoder produces round-trips;
/// only a hand-mistyped one can land slightly outside its own band.
/// </para>
/// </remarks>
public static class MgrsConverter
{
    private const double ScaleFactor = 0.9996;

    private const double FalseEasting = 500000.0;

    /// <summary>
    /// zone digits · band letter · two square letters · an even number of coordinate digits,
    /// with optional spaces anywhere between the groups. <c>I</c> and <c>O</c> are not letters
    /// MGRS uses.
    /// <para>
    /// The band and the square are optional so that a reference which stops early still parses:
    /// <c>39</c> is a whole zone and <c>39S</c> a grid zone cell. A square without a band is not a
    /// legal reference, but the regex alone cannot say so — backtracking will happily read
    /// <c>39WV</c> as a bandless square — so <see cref="TryParseParts"/> rejects that case.
    /// </para>
    /// </summary>
    private static readonly Regex MgrsRegex = new Regex(
        @"^\s*(?<zone>\d{1,2})\s*(?<band>[C-HJ-NP-X])?\s*(?:(?<column>[A-HJ-NP-Z])\s*(?<row>[A-HJ-NP-V])\s*(?<digits>[\d\s]*))?\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    #region Geodetic

    /// <summary>
    /// The MGRS reference for a geodetic position. <paramref name="geodeticPoint"/> is
    /// (longitude, latitude) in degrees, as everywhere else in this library.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The position is outside 80°S–84°N, or the longitude is not finite.</exception>
    public static string FromGeodetic(IPoint geodeticPoint, MgrsPrecision precision = MgrsPrecision.M1)
        => FromGeodetic(geodeticPoint.X, geodeticPoint.Y, precision);

    /// <inheritdoc cref="FromGeodetic(IPoint, MgrsPrecision)"/>
    public static string FromGeodetic(double longitude, double latitude, MgrsPrecision precision = MgrsPrecision.M1)
        => FromGeodetic(longitude, latitude, precision, Ellipsoids.WGS84);

    /// <inheritdoc cref="FromGeodetic(IPoint, MgrsPrecision)"/>
    public static string FromGeodetic(double longitude, double latitude, MgrsPrecision precision, Ellipsoid ellipsoid)
    {
        if (!TryFromGeodetic(longitude, latitude, precision, ellipsoid, out var result))
            throw new ArgumentOutOfRangeException(
                nameof(latitude),
                $"MGRS covers 80°S to 84°N only; ({longitude}, {latitude}) is outside it.");

        return result;
    }

    /// <summary>
    /// The MGRS reference for a geodetic position, or <c>false</c> when the position is outside
    /// the range MGRS covers without UPS.
    /// </summary>
    public static bool TryFromGeodetic(double longitude, double latitude, MgrsPrecision precision, out string mgrs)
        => TryFromGeodetic(longitude, latitude, precision, Ellipsoids.WGS84, out mgrs);

    /// <inheritdoc cref="TryFromGeodetic(double, double, MgrsPrecision, out string)"/>
    public static bool TryFromGeodetic(double longitude, double latitude, MgrsPrecision precision, Ellipsoid ellipsoid, out string mgrs)
    {
        mgrs = string.Empty;

        if (double.IsNaN(longitude) || double.IsInfinity(longitude))
            return false;

        var band = MgrsBands.GetBandLetter(latitude);

        if (band == '\0')
            return false;

        var zone = MgrsBands.GetZone(longitude, latitude);

        var utm = ToUtmCore(longitude, latitude, zone, ellipsoid);

        if (!TryBuild(zone, band, utm.easting, utm.northing, precision, out var coordinate))
            return false;

        mgrs = coordinate.ToString();

        return true;
    }

    /// <summary>
    /// The position a reference names: the square's south-west corner, or its centre when
    /// <paramref name="useSquareCentre"/> is set. Returns (longitude, latitude) in degrees.
    /// </summary>
    /// <exception cref="FormatException">The text is not a well-formed MGRS reference.</exception>
    public static Point ToGeodetic(string mgrs, bool useSquareCentre = false)
        => ToGeodetic(mgrs, useSquareCentre, Ellipsoids.WGS84);

    /// <inheritdoc cref="ToGeodetic(string, bool)"/>
    public static Point ToGeodetic(string mgrs, bool useSquareCentre, Ellipsoid ellipsoid)
    {
        if (!TryToGeodetic(mgrs, useSquareCentre, ellipsoid, out var result))
            throw new FormatException($"'{mgrs}' is not a valid MGRS reference.");

        return result;
    }

    /// <inheritdoc cref="ToGeodetic(string, bool)"/>
    public static bool TryToGeodetic(string? mgrs, out Point geodeticPoint)
        => TryToGeodetic(mgrs, useSquareCentre: false, Ellipsoids.WGS84, out geodeticPoint);

    /// <inheritdoc cref="ToGeodetic(string, bool)"/>
    public static bool TryToGeodetic(string? mgrs, bool useSquareCentre, out Point geodeticPoint)
        => TryToGeodetic(mgrs, useSquareCentre, Ellipsoids.WGS84, out geodeticPoint);

    /// <inheritdoc cref="ToGeodetic(string, bool)"/>
    public static bool TryToGeodetic(string? mgrs, bool useSquareCentre, Ellipsoid ellipsoid, out Point geodeticPoint)
    {
        geodeticPoint = new Point(double.NaN, double.NaN);

        if (!TryToUtm(mgrs, useSquareCentre, out var zone, out var isNorthHemisphere, out var easting, out var northing))
            return false;

        geodeticPoint = FromUtmCore(zone, isNorthHemisphere, easting, northing, ellipsoid);

        return true;
    }

    #endregion

    #region UTM

    /// <summary>
    /// The MGRS reference for a UTM coordinate. <paramref name="northing"/> is the value as UTM
    /// states it, so southern-hemisphere northings include the 10 000 km false northing.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The zone or the coordinate is out of range.</exception>
    public static string FromUtm(int zone, bool isNorthHemisphere, double easting, double northing, MgrsPrecision precision = MgrsPrecision.M1)
    {
        if (!TryFromUtm(zone, isNorthHemisphere, easting, northing, precision, out var result))
            throw new ArgumentOutOfRangeException(
                nameof(zone),
                $"Zone {zone} easting {easting} northing {northing} is not a position MGRS can name.");

        return result;
    }

    /// <inheritdoc cref="FromUtm(int, bool, double, double, MgrsPrecision)"/>
    public static bool TryFromUtm(int zone, bool isNorthHemisphere, double easting, double northing, MgrsPrecision precision, out string mgrs)
    {
        mgrs = string.Empty;

        if (zone < 1 || zone > 60)
            return false;

        // The band letter is a function of latitude, and only the projection knows it.
        var geodetic = FromUtmCore(zone, isNorthHemisphere, easting, northing, Ellipsoids.WGS84);

        var band = MgrsBands.GetBandLetter(geodetic.Y);

        if (band == '\0')
            return false;

        if (!TryBuild(zone, band, easting, northing, precision, out var coordinate))
            return false;

        mgrs = coordinate.ToString();

        return true;
    }

    /// <summary>
    /// The UTM coordinate a reference names — the square's south-west corner, or its centre when
    /// <paramref name="useSquareCentre"/> is set.
    /// </summary>
    /// <exception cref="FormatException">The text is not a well-formed MGRS reference.</exception>
    public static (int zone, bool isNorthHemisphere, double easting, double northing) ToUtm(string mgrs, bool useSquareCentre = false)
    {
        if (!TryToUtm(mgrs, useSquareCentre, out var zone, out var isNorthHemisphere, out var easting, out var northing))
            throw new FormatException($"'{mgrs}' is not a valid MGRS reference.");

        return (zone, isNorthHemisphere, easting, northing);
    }

    /// <inheritdoc cref="ToUtm(string, bool)"/>
    public static bool TryToUtm(string? mgrs, out int zone, out bool isNorthHemisphere, out double easting, out double northing)
        => TryToUtm(mgrs, useSquareCentre: false, out zone, out isNorthHemisphere, out easting, out northing);

    /// <inheritdoc cref="ToUtm(string, bool)"/>
    public static bool TryToUtm(string? mgrs, bool useSquareCentre, out int zone, out bool isNorthHemisphere, out double easting, out double northing)
    {
        zone = 0;
        isNorthHemisphere = true;
        easting = double.NaN;
        northing = double.NaN;

        if (!TryParse(mgrs, out var coordinate))
            return false;

        return TryToUtm(coordinate, useSquareCentre, out zone, out isNorthHemisphere, out easting, out northing);
    }

    /// <inheritdoc cref="ToUtm(string, bool)"/>
    public static bool TryToUtm(MgrsCoordinate coordinate, bool useSquareCentre, out int zone, out bool isNorthHemisphere, out double easting, out double northing)
    {
        zone = coordinate.Zone;
        isNorthHemisphere = MgrsBands.IsNorthernBand(coordinate.Band);
        easting = double.NaN;
        northing = double.NaN;

        var columnEasting = MgrsBands.GetColumnEasting(coordinate.Zone, coordinate.Column);

        if (double.IsNaN(columnEasting))
            return false;

        var rowNorthing = MgrsBands.GetRowNorthing(coordinate.Zone, coordinate.Row);

        if (double.IsNaN(rowNorthing))
            return false;

        var minimumNorthing = MgrsBands.GetBandMinimumNorthing(coordinate.Band);

        if (double.IsNaN(minimumNorthing))
            return false;

        var squareSize = GetSquareSize(coordinate.Precision);

        var offsetEasting = coordinate.Easting * squareSize;

        var offsetNorthing = coordinate.Northing * squareSize;

        easting = columnEasting + offsetEasting;

        // A row letter pins the northing only within a 2 000 km cycle. The band says which
        // repeat is meant: take the first one at or above the band's lowest possible northing.
        northing = rowNorthing + offsetNorthing;

        while (northing < minimumNorthing)
            northing += MgrsBands.NorthingCycle;

        if (useSquareCentre)
        {
            easting += squareSize / 2.0;
            northing += squareSize / 2.0;
        }

        return true;
    }

    #endregion

    #region The grid itself

    /// <summary>
    /// The latitude band letters from south to north: <c>C</c>–<c>X</c> without <c>I</c> and
    /// <c>O</c>. Each covers 8° from 80°S, except <c>X</c>, which covers 12°.
    /// </summary>
    public static IReadOnlyList<char> BandLetters { get; } = MgrsBands.BandLetters.ToCharArray();

    /// <summary>
    /// The latitude a band covers, as [min, max) in degrees, or NaN when the letter is not a
    /// band letter.
    /// </summary>
    public static (double min, double max) GetBandLatitudeRange(char band) => MgrsBands.GetBandLatitudeRange(band);

    /// <summary>
    /// The longitude one grid zone cell covers — one zone within one band — as [west, east) in
    /// degrees. Normally the zone's plain six degrees, but Norway and Svalbard redraw the cells
    /// there; NaN means the cell does not exist, which is the case for 32X, 34X and 36X.
    /// </summary>
    public static (double west, double east) GetGridZoneLongitudeRange(int zone, char band) => MgrsBands.GetGridZoneLongitudeRange(zone, band);

    /// <summary>
    /// The zone a position falls in, honouring the Norway and Svalbard exceptions. Away from
    /// those, this is <see cref="MapProjects.FindUtmZone(double)"/>.
    /// </summary>
    public static int GetZone(double longitude, double latitude) => MgrsBands.GetZone(longitude, latitude);

    #endregion

    #region Extent

    /// <summary>
    /// The region a reference names, as a geodetic (longitude, latitude) bounding box in degrees.
    /// Every level is accepted, and the coarser the reference the larger the region:
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <item><term><c>39</c></term><description>the whole zone — a six-degree strip from 80°S to 84°N</description></item>
    /// <item><term><c>39S</c></term><description>one grid zone cell — 6° × 8°</description></item>
    /// <item><term><c>39S WV</c></term><description>a 100 km square</description></item>
    /// <item><term><c>39S WV 53516 39501</c></term><description>a 1 m square</description></item>
    /// </list>
    /// <para>
    /// A UTM square is not a latitude/longitude rectangle — its edges curve — so for anything
    /// from the 100 km square down this is the box <em>around</em> the square, found by walking
    /// its edges rather than just its corners. Good enough to zoom to; not the square's outline.
    /// </para>
    /// <para>
    /// Longitudes are measured continuously either side of the zone's central meridian, so a
    /// square that spills over the antimeridian yields a box that runs past ±180 rather than one
    /// that appears to wrap around the world.
    /// </para>
    /// </remarks>
    /// <exception cref="FormatException">The text is not a well-formed MGRS reference.</exception>
    public static BoundingBox GetBoundingBox(string mgrs)
    {
        if (!TryGetBoundingBox(mgrs, out var result))
            throw new FormatException($"'{mgrs}' is not a valid MGRS reference.");

        return result;
    }

    /// <inheritdoc cref="GetBoundingBox(string)"/>
    public static bool TryGetBoundingBox(string? mgrs, out BoundingBox geodeticBoundingBox)
        => TryGetBoundingBox(mgrs, Ellipsoids.WGS84, out geodeticBoundingBox);

    /// <inheritdoc cref="GetBoundingBox(string)"/>
    public static bool TryGetBoundingBox(string? mgrs, Ellipsoid ellipsoid, out BoundingBox geodeticBoundingBox)
    {
        geodeticBoundingBox = BoundingBox.NaN;

        if (!TryParseParts(mgrs, out var parts))
            return false;

        // A bare zone number: the whole strip, widened wherever Norway or Svalbard pushes the
        // zone outside its nominal six degrees.
        if (!parts.HasBand)
        {
            var (zoneWest, zoneEast) = MgrsBands.GetWidestZoneLongitudeRange(parts.Zone);

            geodeticBoundingBox = new BoundingBox(zoneWest, MgrsBands.MinLatitude, zoneEast, MgrsBands.MaxLatitude);

            return true;
        }

        var (latitudeMin, latitudeMax) = MgrsBands.GetBandLatitudeRange(parts.Band);

        if (double.IsNaN(latitudeMin))
            return false;

        // A grid zone designator: one cell of the zone/band lattice.
        if (!parts.HasSquare)
        {
            var (cellWest, cellEast) = MgrsBands.GetGridZoneLongitudeRange(parts.Zone, parts.Band);

            // Over Svalbard three of the even zones have no cell at all.
            if (double.IsNaN(cellWest))
                return false;

            geodeticBoundingBox = new BoundingBox(cellWest, latitudeMin, cellEast, latitudeMax);

            return true;
        }

        if (!TryToUtm(new MgrsCoordinate(parts.Zone, parts.Band, parts.Column, parts.Row, parts.Easting, parts.Northing, parts.Precision),
                      useSquareCentre: false, out var zone, out var isNorthHemisphere, out var easting, out var northing))
            return false;

        geodeticBoundingBox = GetSquareBoundingBox(zone, isNorthHemisphere, easting, northing, GetSquareSize(parts.Precision), ellipsoid);

        return true;
    }

    /// <summary>
    /// The geodetic box around a UTM square. The edges are sampled rather than just the corners,
    /// because a straight line in UTM bows in latitude/longitude and the bulge is what decides
    /// the box for a 100 km square.
    /// </summary>
    private static BoundingBox GetSquareBoundingBox(int zone, bool isNorthHemisphere, double easting, double northing, double size, Ellipsoid ellipsoid)
    {
        const int samplesPerEdge = 8;

        var centralMeridian = MapProjects.CalculateCentralMeridian(zone);

        double west = double.MaxValue, east = double.MinValue, south = double.MaxValue, north = double.MinValue;

        void Include(double e, double n)
        {
            var geodetic = FromUtmCore(zone, isNorthHemisphere, e, n, ellipsoid);

            // Measure longitude continuously either side of the central meridian so a square at
            // the antimeridian does not read as one spanning the globe.
            var longitude = centralMeridian + MgrsBands.NormalizeLongitude(geodetic.X - centralMeridian);

            west = Math.Min(west, longitude);
            east = Math.Max(east, longitude);
            south = Math.Min(south, geodetic.Y);
            north = Math.Max(north, geodetic.Y);
        }

        for (var i = 0; i <= samplesPerEdge; i++)
        {
            var t = size * i / samplesPerEdge;

            Include(easting + t, northing);            // south edge
            Include(easting + t, northing + size);     // north edge
            Include(easting, northing + t);            // west edge
            Include(easting + size, northing + t);     // east edge
        }

        return new BoundingBox(west, south, east, north);
    }

    #endregion

    #region Parsing and formatting

    /// <summary>
    /// Parses a reference in any of the usual spacings — <c>39SWV5351639501</c>,
    /// <c>39S WV 53516 39501</c>, <c>39s wv 5351639501</c> — into its parts.
    /// </summary>
    /// <exception cref="FormatException">The text is not a well-formed MGRS reference.</exception>
    public static MgrsCoordinate Parse(string mgrs)
    {
        if (!TryParse(mgrs, out var result))
            throw new FormatException($"'{mgrs}' is not a valid MGRS reference.");

        return result;
    }

    /// <summary>
    /// Parses a complete reference — one that names at least a 100 km square. A reference that
    /// stops at the zone or the grid zone is a region rather than a coordinate; use
    /// <see cref="TryGetBoundingBox(string, out BoundingBox)"/> for those.
    /// </summary>
    /// <inheritdoc cref="Parse(string)"/>
    public static bool TryParse(string? mgrs, out MgrsCoordinate coordinate)
    {
        coordinate = default;

        if (!TryParseParts(mgrs, out var parts) || !parts.HasSquare)
            return false;

        coordinate = new MgrsCoordinate(parts.Zone, parts.Band, parts.Column, parts.Row, parts.Easting, parts.Northing, parts.Precision);

        return true;
    }

    /// <summary>
    /// The reference written the conventional way — upper case, spaces between the grid zone, the
    /// square and the two digit groups, leading zeros kept. Any level is accepted, so
    /// <c>39swv5351639501</c> comes back as <c>39S WV 53516 39501</c> and <c>39s</c> as <c>39S</c>.
    /// </summary>
    public static bool TryNormalize(string? mgrs, out string canonical)
    {
        canonical = string.Empty;

        if (!TryParseParts(mgrs, out var parts))
            return false;

        if (!parts.HasBand)
        {
            canonical = parts.Zone.ToString(CultureInfo.InvariantCulture);

            return true;
        }

        if (!parts.HasSquare)
        {
            canonical = parts.Zone.ToString(CultureInfo.InvariantCulture) + parts.Band;

            return true;
        }

        canonical = new MgrsCoordinate(parts.Zone, parts.Band, parts.Column, parts.Row, parts.Easting, parts.Northing, parts.Precision).ToString();

        return true;
    }

    /// <summary>The side of the square a reference at this precision names, in metres.</summary>
    public static double GetSquareSize(MgrsPrecision precision)
        => MgrsBands.OneHundredKm / Math.Pow(10, (int)precision);

    #endregion

    #region Internals

    /// <summary>How much of a reference was actually written.</summary>
    private readonly struct ReferenceParts
    {
        public readonly int Zone;
        public readonly char Band;      // ' ' when the reference stops at the zone
        public readonly char Column;    // ' ' when it stops at the grid zone
        public readonly char Row;
        public readonly int Easting;
        public readonly int Northing;
        public readonly MgrsPrecision Precision;

        public ReferenceParts(int zone, char band, char column, char row, int easting, int northing, MgrsPrecision precision)
        {
            Zone = zone;
            Band = band;
            Column = column;
            Row = row;
            Easting = easting;
            Northing = northing;
            Precision = precision;
        }

        public bool HasBand => Band != ' ';

        public bool HasSquare => Column != ' ';
    }

    /// <summary>
    /// Parses a reference at any level — zone, grid zone, or 100 km square and finer — without
    /// deciding what it means.
    /// </summary>
    private static bool TryParseParts(string? mgrs, out ReferenceParts parts)
    {
        parts = default;

        if (string.IsNullOrWhiteSpace(mgrs))
            return false;

        var match = MgrsRegex.Match(mgrs!);

        if (!match.Success)
            return false;

        var zone = int.Parse(match.Groups["zone"].Value, CultureInfo.InvariantCulture);

        if (zone < 1 || zone > 60)
            return false;

        var band = match.Groups["band"].Success ? char.ToUpperInvariant(match.Groups["band"].Value[0]) : ' ';

        var hasSquare = match.Groups["column"].Success;

        // A square with no band in front of it is not a legal reference; the regex reaches it only
        // by backtracking past the optional band, so reject it here rather than there.
        if (hasSquare && band == ' ')
            return false;

        if (!hasSquare)
        {
            parts = new ReferenceParts(zone, band, ' ', ' ', 0, 0, MgrsPrecision.Km100);

            return true;
        }

        var column = char.ToUpperInvariant(match.Groups["column"].Value[0]);

        var row = char.ToUpperInvariant(match.Groups["row"].Value[0]);

        // The column letter set rotates every three zones, so not every letter is legal here.
        if (double.IsNaN(MgrsBands.GetColumnEasting(zone, column)))
            return false;

        var digits = StripWhitespace(match.Groups["digits"].Value);

        if (digits.Length % 2 != 0 || digits.Length > 10)
            return false;

        var half = digits.Length / 2;

        var easting = half == 0 ? 0 : int.Parse(digits.Substring(0, half), CultureInfo.InvariantCulture);

        var northing = half == 0 ? 0 : int.Parse(digits.Substring(half), CultureInfo.InvariantCulture);

        parts = new ReferenceParts(zone, band, column, row, easting, northing, (MgrsPrecision)half);

        return true;
    }

    private static (double easting, double northing) ToUtmCore(double longitude, double latitude, int zone, Ellipsoid ellipsoid)
    {
        var deltaLongitude = MgrsBands.NormalizeLongitude(longitude - MapProjects.CalculateCentralMeridian(zone));

        var projected = MapProjects.GeodeticToTransverseMercator(new Point(deltaLongitude, latitude), ellipsoid);

        var easting = projected.X * ScaleFactor + FalseEasting;

        var northing = projected.Y * ScaleFactor + (latitude < 0 ? MgrsBands.SouthernFalseNorthing : 0.0);

        return (easting, northing);
    }

    private static Point FromUtmCore(int zone, bool isNorthHemisphere, double easting, double northing, Ellipsoid ellipsoid)
    {
        var x = (easting - FalseEasting) / ScaleFactor;

        var y = (isNorthHemisphere ? northing : northing - MgrsBands.SouthernFalseNorthing) / ScaleFactor;

        var geodetic = MapProjects.TransverseMercatorToGeodetic(new Point(x, y), ellipsoid);

        return new Point(MgrsBands.NormalizeLongitude(geodetic.X + MapProjects.CalculateCentralMeridian(zone)), geodetic.Y);
    }

    private static bool TryBuild(int zone, char band, double easting, double northing, MgrsPrecision precision, out MgrsCoordinate coordinate)
    {
        coordinate = default;

        if (precision < MgrsPrecision.Km100 || precision > MgrsPrecision.M1)
            return false;

        if (double.IsNaN(easting) || double.IsInfinity(easting) || double.IsNaN(northing) || double.IsInfinity(northing))
            return false;

        var column = MgrsBands.GetColumnLetter(zone, easting);

        if (column == '\0')
            return false;

        var row = MgrsBands.GetRowLetter(zone, northing);

        var squareSize = GetSquareSize(precision);

        // Truncate rather than round: a reference names the square the point falls in, and
        // rounding up would name the neighbouring one.
        var digitsEasting = (int)Math.Floor(easting % MgrsBands.OneHundredKm / squareSize);

        var digitsNorthing = (int)Math.Floor(northing % MgrsBands.OneHundredKm / squareSize);

        coordinate = new MgrsCoordinate(zone, band, column, row, digitsEasting, digitsNorthing, precision);

        return true;
    }

    private static string StripWhitespace(string text)
    {
        var builder = new StringBuilder(text.Length);

        foreach (var c in text)
        {
            if (!char.IsWhiteSpace(c))
                builder.Append(c);
        }

        return builder.ToString();
    }

    #endregion
}
