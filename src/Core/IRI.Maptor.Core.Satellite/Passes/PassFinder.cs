using System.Threading;
using IRI.Maptor.Core.Astronomy.Solar;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Satellite.Propagation;
using IRI.Maptor.Core.Satellite.Tracks;

namespace IRI.Maptor.Core.Satellite.Passes;

/// <summary>
/// Finds when a satellite passes over a <see cref="PassRegion"/>: when it is visible from there, or
/// when its imager's swath reaches it.
/// </summary>
/// <remarks>
/// <para>
/// Both come down to one measure, g(t), that is negative during a pass:
/// </para>
/// <list type="bullet">
/// <item>visible from a point: the minimum elevation minus the satellite's elevation there;</item>
/// <item>visible from a polygon: the ground distance from the point beneath the satellite to the
/// region, minus the radius of the area that sees it above the minimum elevation (<see cref="Footprint"/>);</item>
/// <item>imaged: that ground distance minus half the swath width. A circle of half the width swept
/// along the track is exactly the swath strip.</item>
/// </list>
/// <para>
/// g is sampled every <see cref="ScanStepFor"/> (30 s for a low orbit, up to 5 min for a high one); each
/// sampled minimum is refined by golden-section search,
/// and when it is negative the start and end are found by bisection, to under a tenth of a second. A
/// low orbit's pass over a point is never shorter than the step can bracket, since the refinement
/// searches the whole interval around each sampled minimum.
/// </para>
/// </remarks>
public static class PassFinder
{
    /// <summary>The coarse sampling interval for a low orbit, and the shortest one used.</summary>
    public static readonly TimeSpan ScanStep = TimeSpan.FromSeconds(30);

    /// <summary>The longest sampling interval, for geostationary and other high orbits.</summary>
    public static readonly TimeSpan MaximumScanStep = TimeSpan.FromMinutes(5);

    /// <summary>
    /// The sampling interval for <paramref name="satellite"/>: one two-hundredth of its period, between
    /// <see cref="ScanStep"/> and <see cref="MaximumScanStep"/>. A high orbit moves slowly over the
    /// ground, so its passes last hours and a coarser scan finds them as surely, in far fewer steps.
    /// </summary>
    public static TimeSpan ScanStepFor(Sgp4Propagator satellite)
    {
        if (satellite is null)
            throw new ArgumentNullException(nameof(satellite));

        var step = TimeSpan.FromMinutes(satellite.PeriodMinutes / 200.0);

        return step < ScanStep ? ScanStep : step > MaximumScanStep ? MaximumScanStep : step;
    }

    private static readonly TimeSpan CrossingTolerance = TimeSpan.FromSeconds(0.05);

    private static readonly TimeSpan PeakTolerance = TimeSpan.FromSeconds(0.2);

    /// <summary>How often a pass is checked for the satellite being sunlit against a dark sky.</summary>
    private static readonly TimeSpan NakedEyeStep = TimeSpan.FromSeconds(10);

    /// <summary>How often the track is sampled to measure coverage.</summary>
    private static readonly TimeSpan CoverageStep = TimeSpan.FromSeconds(2);

    /// <summary>The Sun below this, at the region's centre, makes the sky dark enough to see a satellite.</summary>
    public const double DarkSkySunElevation = -6;

    /// <summary>Passes during which the satellite is at least <paramref name="minimumElevation"/> above the horizon from the region.</summary>
    public static IReadOnlyList<SatellitePass> Visible(Sgp4Propagator satellite, PassRegion region, DateTime start, DateTime end,
        double minimumElevation = Footprint.DefaultMinimumElevation, CancellationToken cancellationToken = default)
    {
        Check(satellite, region, ref start, ref end);

        if (!(minimumElevation >= 0 && minimumElevation < 90))
            throw new ArgumentOutOfRangeException(nameof(minimumElevation), "The minimum elevation must be in [0, 90).");

        Func<SatellitePosition, double> margin = region.IsPoint
            ? p => minimumElevation - LookAngles.From(region.Center, p).Elevation
            : p => region.DistanceKm(p.SubSatellitePoint) - Footprint.AngularRadiusDegrees(Math.Max(p.AltitudeKm, 1), minimumElevation) * Math.PI / 180.0 * GreatCircle.MeanEarthRadiusKm;

        var passes = Find(satellite, region, start, end, PassKind.Visible, margin, cancellationToken);

        foreach (var pass in passes)
            // a deep-space satellite (GPS, Galileo, geostationary) is far too faint to see without a telescope
            pass.IsNakedEyeVisible = !satellite.IsDeepSpace && IsNakedEyeVisible(satellite, region, pass);

        return passes;
    }

    /// <summary>Passes during which a swath of <paramref name="swathWidthKm"/> along the track reaches the region.</summary>
    public static IReadOnlyList<SatellitePass> Imaged(Sgp4Propagator satellite, double swathWidthKm, PassRegion region, DateTime start, DateTime end,
        CancellationToken cancellationToken = default)
    {
        Check(satellite, region, ref start, ref end);

        if (!(swathWidthKm > 0))
            throw new ArgumentOutOfRangeException(nameof(swathWidthKm), "The swath width must be positive.");

        var half = swathWidthKm / 2;

        var passes = Find(satellite, region, start, end, PassKind.Imaged, p => region.DistanceKm(p.SubSatellitePoint) - half, cancellationToken);

        foreach (var pass in passes)
            pass.CoveredFraction = CoveredFraction(satellite, region, pass, half);

        return passes;
    }

    /// <summary>Passes during which the swath of <paramref name="sensor"/> reaches the region.</summary>
    public static IReadOnlyList<SatellitePass> Imaged(Sgp4Propagator satellite, SensorInfo sensor, PassRegion region, DateTime start, DateTime end,
        CancellationToken cancellationToken = default)
    {
        if (sensor is null)
            throw new ArgumentNullException(nameof(sensor));

        return Imaged(satellite, sensor.SwathWidthKm, region, start, end, cancellationToken);
    }

    /// <summary>
    /// The elevation of a satellite at <paramref name="altitudeKm"/> seen from a place
    /// <paramref name="groundDistanceKm"/> from the point beneath it, on a sphere, degrees.
    /// </summary>
    public static double ElevationAt(double groundDistanceKm, double altitudeKm)
    {
        const double R = GreatCircle.MeanEarthRadiusKm;

        var psi = groundDistanceKm / R;

        return Math.Atan2(Math.Cos(psi) - R / (R + altitudeKm), Math.Sin(psi)) * 180.0 / Math.PI;
    }

    private static void Check(Sgp4Propagator satellite, PassRegion region, ref DateTime start, ref DateTime end)
    {
        if (satellite is null)
            throw new ArgumentNullException(nameof(satellite));

        if (region is null)
            throw new ArgumentNullException(nameof(region));

        start = ToUtc(start);
        end = ToUtc(end);

        if (end <= start)
            throw new ArgumentException("The window must end after it starts.", nameof(end));
    }

    private static DateTime ToUtc(DateTime t) => t.Kind == DateTimeKind.Local ? t.ToUniversalTime() : DateTime.SpecifyKind(t, DateTimeKind.Utc);

    private static List<SatellitePass> Find(Sgp4Propagator satellite, PassRegion region, DateTime start, DateTime end, PassKind kind,
        Func<SatellitePosition, double> margin, CancellationToken cancellationToken)
    {
        double? G(DateTime t) => satellite.GetPosition(t) is SatellitePosition p ? margin(p) : null;

        // coarse samples, up to the end of the window or the satellite's decay
        var times = new List<DateTime>();
        var values = new List<double>();

        var scanStep = ScanStepFor(satellite);

        for (var t = start; ; t += scanStep)
        {
            if (t > end)
                t = end;

            if ((times.Count & 63) == 0)
                cancellationToken.ThrowIfCancellationRequested();

            if (G(t) is not double g)
                break;

            times.Add(t);
            values.Add(g);

            if (t == end)
                break;
        }

        var passes = new List<SatellitePass>();

        var n = times.Count;

        if (n == 0)
            return passes;

        var lastEnd = DateTime.MinValue;

        for (var k = 0; k < n; k++)
        {
            var left = k > 0 ? values[k - 1] : double.PositiveInfinity;
            var right = k < n - 1 ? values[k + 1] : double.PositiveInfinity;

            if (!(values[k] <= left && values[k] <= right) || times[k] <= lastEnd)
                continue;

            cancellationToken.ThrowIfCancellationRequested();

            // the true minimum lies between the neighbouring samples
            var (peak, peakValue) = Minimize(G, times[Math.Max(0, k - 1)], times[Math.Min(n - 1, k + 1)]);

            if (values[k] < peakValue)
                (peak, peakValue) = (times[k], values[k]);

            if (peakValue > 0 || peak <= lastEnd)
                continue;

            // walk out to the first sample outside the pass, then bisect
            var startsBefore = false;
            DateTime passStart;

            var j = k - 1;
            while (j >= 0 && values[j] <= 0)
                j--;

            if (j < 0)
            {
                startsBefore = true;
                passStart = times[0];
            }
            else
            {
                passStart = Crossing(G, times[j], j + 1 < k ? times[j + 1] : peak);
            }

            var endsAfter = false;
            DateTime passEnd;

            var m = k + 1;
            while (m < n && values[m] <= 0)
                m++;

            if (m >= n)
            {
                // the window's end, or the decay, comes first
                endsAfter = true;
                passEnd = times[n - 1];
            }
            else
            {
                passEnd = Crossing(G, times[m], m - 1 > k ? times[m - 1] : peak);
            }

            // the deepest point of the whole pass: a long one (over an area, or a satellite that stays in
            // view) can dip more than once, and the first dip found need not be the deepest
            var best = k;

            for (var q = Math.Max(0, j + 1); q < Math.Min(n, m); q++)
                if (values[q] < values[best])
                    best = q;

            if (best != k)
            {
                var (deeper, deeperValue) = Minimize(G, times[Math.Max(0, best - 1)], times[Math.Min(n - 1, best + 1)]);

                if (values[best] < deeperValue)
                    (deeper, deeperValue) = (times[best], values[best]);

                if (deeperValue < peakValue)
                    (peak, peakValue) = (deeper, deeperValue);
            }

            lastEnd = passEnd;

            passes.Add(Describe(satellite, region, kind, passStart, peak, passEnd, startsBefore, endsAfter));
        }

        return passes;
    }

    /// <summary>Golden-section search for the minimum of <paramref name="g"/> in [a, b].</summary>
    private static (DateTime Time, double Value) Minimize(Func<DateTime, double?> g, DateTime a, DateTime b)
    {
        const double ratio = 0.6180339887498949;

        double Eval(DateTime t) => g(t) ?? double.PositiveInfinity;

        var c = b - TimeSpan.FromTicks((long)((b - a).Ticks * ratio));
        var d = a + TimeSpan.FromTicks((long)((b - a).Ticks * ratio));
        var fc = Eval(c);
        var fd = Eval(d);

        while (b - a > PeakTolerance)
        {
            if (fc < fd)
            {
                b = d; d = c; fd = fc;
                c = b - TimeSpan.FromTicks((long)((b - a).Ticks * ratio));
                fc = Eval(c);
            }
            else
            {
                a = c; c = d; fc = fd;
                d = a + TimeSpan.FromTicks((long)((b - a).Ticks * ratio));
                fd = Eval(d);
            }
        }

        var mid = a + TimeSpan.FromTicks((b - a).Ticks / 2);

        return (mid, Eval(mid));
    }

    /// <summary>Bisection between <paramref name="outside"/> (g &gt; 0) and <paramref name="inside"/> (g ≤ 0).</summary>
    private static DateTime Crossing(Func<DateTime, double?> g, DateTime outside, DateTime inside)
    {
        while ((outside > inside ? outside - inside : inside - outside) > CrossingTolerance)
        {
            var mid = outside + TimeSpan.FromTicks((inside - outside).Ticks / 2);

            if ((g(mid) ?? double.PositiveInfinity) > 0)
                outside = mid;
            else
                inside = mid;
        }

        return inside;
    }

    private static SatellitePass Describe(Sgp4Propagator satellite, PassRegion region, PassKind kind,
        DateTime start, DateTime peak, DateTime end, bool startsBefore, bool endsAfter)
    {
        var pass = new SatellitePass(satellite, kind, start, peak, end)
        {
            StartsBeforeWindow = startsBefore,
            EndsAfterWindow = endsAfter,
            SunElevation = Sun.GetHorizontal(region.Center, peak).Elevation,
        };

        var atPeak = satellite.GetPosition(peak)!;

        if (region.IsPoint)
        {
            var look = LookAngles.From(region.Center, atPeak);

            pass.PeakElevation = look.Elevation;
            pass.PeakAzimuth = look.Azimuth;

            if (!startsBefore && satellite.GetPosition(start) is SatellitePosition rise)
                pass.RiseAzimuth = LookAngles.From(region.Center, rise).Azimuth;

            if (!endsAfter && satellite.GetPosition(end) is SatellitePosition set)
                pass.SetAzimuth = LookAngles.From(region.Center, set).Azimuth;
        }
        else
        {
            pass.PeakElevation = ElevationAt(region.DistanceKm(atPeak.SubSatellitePoint), atPeak.AltitudeKm);
        }

        var before = satellite.GetPosition(peak.AddSeconds(-1));
        var after = satellite.GetPosition(peak.AddSeconds(1));

        pass.IsNorthbound = before is not null && after is not null && after.SubSatellitePoint.Y > before.SubSatellitePoint.Y;

        return pass;
    }

    private static bool IsNakedEyeVisible(Sgp4Propagator satellite, PassRegion region, SatellitePass pass)
    {
        for (var t = pass.Start; ; t += NakedEyeStep)
        {
            if (t > pass.End)
                t = pass.End;

            if (Sun.GetHorizontal(region.Center, t).Elevation < DarkSkySunElevation
                && satellite.GetPosition(t) is SatellitePosition p
                && Sunlight.IsSunlit(p))
                return true;

            if (t == pass.End)
                return false;
        }
    }

    /// <summary>The share of the region's sample points that come within <paramref name="halfWidthKm"/> of the track.</summary>
    private static double CoveredFraction(Sgp4Propagator satellite, PassRegion region, SatellitePass pass, double halfWidthKm)
    {
        if (region.IsPoint)
            return 1;

        var nadir = new List<Point>();

        for (var t = pass.Start; ; t += CoverageStep)
        {
            if (t > pass.End)
                t = pass.End;

            if (satellite.GetPosition(t) is SatellitePosition p)
                nadir.Add(p.SubSatellitePoint);

            if (t == pass.End)
                break;
        }

        var samples = region.SamplePoints();

        var covered = samples.Count(s => nadir.Any(q => GreatCircle.DistanceKm(s, q) <= halfWidthKm));

        return (double)covered / samples.Count;
    }
}
