using IRI.Maptor.Core.Common.Primitives;

namespace IRI.Maptor.Core.Satellite.Tracks;

/// <summary>
/// The strip of ground an imaging sensor covers as the satellite moves: half the swath width either
/// side of the ground track, at right angles to the direction of travel.
/// </summary>
/// <remarks>
/// <para>
/// The strip is built in chunks of a few samples. Each chunk is the left edge going forward and the
/// right edge coming back, with continuous longitudes, clipped into [−180, 180] by
/// <see cref="LongitudeWindows"/>. Short chunks keep every piece simple even where a polar orbit
/// turns near its highest latitude; neighbouring chunks share an edge and never overlap.
/// </para>
/// <para>
/// This is the nadir strip of a push-broom sensor. Off-nadir pointing and the Earth's curvature across
/// a very wide swath are not modelled: the edges are great-circle offsets of the track.
/// </para>
/// </remarks>
public static class Swath
{
    /// <summary>Samples per chunk; at the default track sampling this is an eighth of an orbit.</summary>
    public const int SamplesPerChunk = 19;

    /// <returns>Open rings (X = longitude, Y = latitude), each inside [−180, 180].</returns>
    public static List<List<Point>> Compute(GroundTrack track, double swathWidthKm)
    {
        if (track is null)
            throw new ArgumentNullException(nameof(track));

        if (!(swathWidthKm > 0))
            throw new ArgumentOutOfRangeException(nameof(swathWidthKm), "The swath width must be positive.");

        var samples = track.Samples;

        var result = new List<List<Point>>();

        if (samples.Count < 2)
            return result;

        var half = swathWidthKm / 2.0;

        var left = new Point[samples.Count];
        var right = new Point[samples.Count];

        for (int i = 0; i < samples.Count; i++)
        {
            // direction of travel: forward difference, backward at the last sample
            var heading = i < samples.Count - 1
                ? GreatCircle.Bearing(samples[i].Location, samples[i + 1].Location)
                : GreatCircle.Bearing(samples[i - 1].Location, samples[i].Location);

            left[i] = GreatCircle.Destination(samples[i].Location, heading - 90, half);
            right[i] = GreatCircle.Destination(samples[i].Location, heading + 90, half);
        }

        for (int start = 0; start < samples.Count - 1; start += SamplesPerChunk - 1)
        {
            var end = Math.Min(samples.Count - 1, start + SamplesPerChunk - 1);

            var ring = new List<Point>();

            for (int i = start; i <= end; i++)
                ring.Add(left[i]);

            for (int i = end; i >= start; i--)
                ring.Add(right[i]);

            result.AddRange(LongitudeWindows.Split(ring));
        }

        return result;
    }
}
