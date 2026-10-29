using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Satellite.Propagation;

namespace IRI.Maptor.Core.Satellite.Tracks;

/// <summary>One sample of a ground track: the point beneath the satellite at an instant.</summary>
public sealed class TrackPoint
{
    public TrackPoint(DateTime utc, Point location, double altitudeKm)
    {
        Utc = utc;
        Location = location;
        AltitudeKm = altitudeKm;
    }

    public DateTime Utc { get; }

    /// <summary>X = longitude, Y = latitude, degrees.</summary>
    public Point Location { get; }

    public double AltitudeKm { get; }
}

/// <summary>
/// The path of the point beneath a satellite over a time window, as lines a flat map can draw.
/// </summary>
/// <remarks>
/// The path is cut wherever it crosses the 180° meridian. The crossing point is interpolated and
/// added to both pieces, at +180° on one and −180° on the other with the same latitude and time, so
/// the pieces meet exactly at the map's edge instead of a line streaking across the whole map.
/// </remarks>
public sealed class GroundTrack
{
    private GroundTrack(IReadOnlyList<IReadOnlyList<TrackPoint>> segments, IReadOnlyList<TrackPoint> samples, DateTime start, DateTime end, bool endsInDecay)
    {
        Segments = segments;
        Samples = samples;
        Start = start;
        End = end;
        EndsInDecay = endsInDecay;
    }

    /// <summary>The lines to draw, each within [−180, 180] longitude.</summary>
    public IReadOnlyList<IReadOnlyList<TrackPoint>> Segments { get; }

    /// <summary>The propagated samples in time order, without the added crossing points.</summary>
    public IReadOnlyList<TrackPoint> Samples { get; }

    public DateTime Start { get; }

    public DateTime End { get; }

    /// <summary>The satellite decayed (or SGP4 failed) inside the window; the track stops there.</summary>
    public bool EndsInDecay { get; }

    /// <summary>The default sampling: 150 points per orbit, about 36 s for the ISS.</summary>
    public static TimeSpan DefaultStep(Sgp4Propagator propagator) => TimeSpan.FromMinutes(propagator.PeriodMinutes / 150.0);

    public static GroundTrack Compute(Sgp4Propagator propagator, DateTime startUtc, DateTime endUtc, TimeSpan? step = null)
    {
        if (propagator is null)
            throw new ArgumentNullException(nameof(propagator));

        if (endUtc < startUtc)
            throw new ArgumentException("The end of the window must not be before its start.", nameof(endUtc));

        var interval = step ?? DefaultStep(propagator);

        if (interval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(step), "The step must be positive.");

        var samples = new List<TrackPoint>();

        var decayed = false;

        for (var time = startUtc; ; time += interval)
        {
            // always end exactly on the window's end
            if (time > endUtc)
                time = endUtc;

            var position = propagator.GetPosition(time);

            if (position is null)
            {
                decayed = true;
                break;
            }

            samples.Add(new TrackPoint(time, position.SubSatellitePoint, position.AltitudeKm));

            if (time == endUtc)
                break;
        }

        return new GroundTrack(SplitAtAntimeridian(samples), samples, startUtc, endUtc, decayed);
    }

    /// <summary>Cuts a sequence of points wherever consecutive longitudes jump by more than 180°.</summary>
    public static List<IReadOnlyList<TrackPoint>> SplitAtAntimeridian(IReadOnlyList<TrackPoint> samples)
    {
        var segments = new List<IReadOnlyList<TrackPoint>>();

        if (samples.Count == 0)
            return segments;

        var current = new List<TrackPoint> { samples[0] };

        for (int i = 1; i < samples.Count; i++)
        {
            var a = samples[i - 1];
            var b = samples[i];

            var dLon = b.Location.X - a.Location.X;

            if (Math.Abs(dLon) > 180)
            {
                // eastward over +180 when the jump is negative, westward over −180 when positive
                var edge = dLon < 0 ? 180.0 : -180.0;

                var unwrappedB = b.Location.X + (dLon < 0 ? 360 : -360);

                var fraction = (edge - a.Location.X) / (unwrappedB - a.Location.X);

                var latitude = a.Location.Y + fraction * (b.Location.Y - a.Location.Y);

                var time = a.Utc + TimeSpan.FromTicks((long)((b.Utc - a.Utc).Ticks * fraction));

                var altitude = a.AltitudeKm + fraction * (b.AltitudeKm - a.AltitudeKm);

                current.Add(new TrackPoint(time, new Point(edge, latitude), altitude));

                segments.Add(current);

                current = new List<TrackPoint> { new TrackPoint(time, new Point(-edge, latitude), altitude) };
            }

            current.Add(b);
        }

        segments.Add(current);

        return segments;
    }
}
