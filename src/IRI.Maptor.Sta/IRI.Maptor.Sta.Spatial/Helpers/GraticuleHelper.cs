using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;

namespace IRI.Maptor.Sta.Spatial.Helpers;

/// <summary>
/// Generates a geographic graticule (meridians and parallels) for a web-mercator extent,
/// with a round interval chosen from the extent span. No antimeridian wrapping.
/// </summary>
public static class GraticuleHelper
{
    // Web Mercator is only defined up to ~±85.05°; beyond that y goes to infinity.
    private const double MaxLatitude = 85.05;

    private static readonly double[] _intervalLadderDegrees = new[]
    {
        30, 20, 10, 5, 2, 1,
        30 / 60.0, 20 / 60.0, 15 / 60.0, 10 / 60.0, 5 / 60.0, 2 / 60.0, 1 / 60.0,
        30 / 3600.0, 15 / 3600.0, 10 / 3600.0, 5 / 3600.0, 2 / 3600.0, 1 / 3600.0,
    };

    public static IReadOnlyList<double> IntervalLadderDegrees => _intervalLadderDegrees;

    /// <summary>
    /// Picks the largest ladder interval that yields at least <paramref name="minLines"/>
    /// lines across the larger extent span (falls back to the finest interval).
    /// </summary>
    public static double ChooseInterval(double lonSpanDegrees, double latSpanDegrees, int minLines = 4, int maxLines = 8)
    {
        var span = Math.Max(Math.Abs(lonSpanDegrees), Math.Abs(latSpanDegrees));

        for (var i = 0; i < _intervalLadderDegrees.Length; i++)
        {
            var interval = _intervalLadderDegrees[i];
            var count = span / interval;

            if (count < minLines)
                continue;

            // Ladder steps are coarse (up to 2.5x), so the first interval reaching minLines
            // may overshoot maxLines; the previous (coarser) interval is then the closer fit.
            if (count > maxLines && i > 0)
                return _intervalLadderDegrees[i - 1];

            return interval;
        }

        return _intervalLadderDegrees[^1];
    }

    /// <summary>
    /// Builds graticule lines covering the given web-mercator extent.
    /// </summary>
    /// <param name="webMercatorExtent">Map extent in web-mercator (EPSG:3857)</param>
    /// <param name="intervalDegrees">Line spacing in degrees; null to choose automatically</param>
    /// <param name="samplesPerLine">Vertices per line (2 suffices: graticule lines are straight in web mercator)</param>
    public static Graticule Create(BoundingBox webMercatorExtent, double? intervalDegrees = null, int samplesPerLine = 2)
    {
        var min = MapProjects.WebMercatorToGeodeticWgs84(new Point(webMercatorExtent.XMin, webMercatorExtent.YMin));
        var max = MapProjects.WebMercatorToGeodeticWgs84(new Point(webMercatorExtent.XMax, webMercatorExtent.YMax));

        var lonMin = Math.Max(min.X, -180);
        var lonMax = Math.Min(max.X, 180);
        var latMin = Math.Max(min.Y, -MaxLatitude);
        var latMax = Math.Min(max.Y, MaxLatitude);

        var interval = intervalDegrees ?? ChooseInterval(lonMax - lonMin, latMax - latMin);

        var result = new Graticule { IntervalDegrees = interval };

        if (interval <= 0 || lonMax <= lonMin || latMax <= latMin)
            return result;

        samplesPerLine = Math.Max(2, samplesPerLine);

        for (var lon = Math.Ceiling(lonMin / interval) * interval; lon <= lonMax; lon += interval)
        {
            result.Meridians.Add(CreateLine(lon, isMeridian: true, latMin, latMax, samplesPerLine));
        }

        for (var lat = Math.Ceiling(latMin / interval) * interval; lat <= latMax; lat += interval)
        {
            result.Parallels.Add(CreateLine(lat, isMeridian: false, lonMin, lonMax, samplesPerLine));
        }

        return result;
    }

    private static GraticuleLine CreateLine(double valueDegrees, bool isMeridian, double from, double to, int samplesPerLine)
    {
        var line = new GraticuleLine
        {
            ValueDegrees = valueDegrees,
            IsMeridian = isMeridian,
            Label = FormatDegreeLabel(valueDegrees, isLatitude: !isMeridian),
        };

        for (var i = 0; i < samplesPerLine; i++)
        {
            var t = from + (to - from) * i / (samplesPerLine - 1);

            var geodetic = isMeridian ? new Point(valueDegrees, t) : new Point(t, valueDegrees);

            line.WebMercatorPoints.Add(MapProjects.GeodeticWgs84ToWebMercator(geodetic));
        }

        return line;
    }

    /// <summary>
    /// Formats a graticule value as a compact DMS label, e.g. "51°30’E"; zero minute/second parts are omitted.
    /// </summary>
    public static string FormatDegreeLabel(double degrees, bool isLatitude)
    {
        var hemisphere = isLatitude
            ? (degrees < 0 ? "S" : "N")
            : (degrees < 0 ? "W" : "E");

        if (degrees == 0)
            hemisphere = string.Empty;

        // Round to whole arc-seconds: ladder intervals are exact multiples of 1", and this
        // avoids the 59.999..." artifacts of truncation-based DMS splits.
        var totalSeconds = (long)Math.Round(Math.Abs(degrees) * 3600);

        var d = totalSeconds / 3600;
        var m = (totalSeconds % 3600) / 60;
        var s = totalSeconds % 60;

        var label = FormattableString.Invariant($"{d}°");

        if (m > 0 || s > 0)
            label += FormattableString.Invariant($"{m:00}{DegreeHelper.minuteSign}");

        if (s > 0)
            label += FormattableString.Invariant($"{s:00}{DegreeHelper.secondSign}");

        return label + hemisphere;
    }
}

public class Graticule
{
    public double IntervalDegrees { get; set; }

    public List<GraticuleLine> Meridians { get; set; } = new();

    public List<GraticuleLine> Parallels { get; set; } = new();
}

public class GraticuleLine
{
    /// <summary>
    /// The constant longitude (meridian) or latitude (parallel) of this line, in degrees
    /// </summary>
    public double ValueDegrees { get; set; }

    public bool IsMeridian { get; set; }

    public List<Point> WebMercatorPoints { get; set; } = new();

    public string Label { get; set; } = string.Empty;
}