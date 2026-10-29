using System.Collections.Generic;
using System.Linq;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Satellite;
using IRI.Maptor.Core.Satellite.Elements;
using IRI.Maptor.Core.Satellite.Propagation;
using IRI.Maptor.Core.Satellite.Tracks;
using IRI.Maptor.Tests.Astronomy;

namespace IRI.Maptor.Tests.Satellite;

public class TracksTest
{
    private static Sgp4Propagator Iss => Sgp4Propagator.Create(
        TwoLineElement.Parse(OrbitalElementsReadingTest.IssLine1, OrbitalElementsReadingTest.IssLine2, "ISS"));

    private static GroundTrack TwoIssOrbits()
    {
        var iss = Iss;

        return GroundTrack.Compute(iss, iss.Elements.Epoch, iss.Elements.Epoch.AddMinutes(2 * iss.PeriodMinutes));
    }

    // ----------------------------------------------------------------- ground track

    [Fact]
    public void Track_SamplesAreSgp4PositionsAndCoverTheWholeWindow()
    {
        var iss = Iss;
        var start = iss.Elements.Epoch;
        var end = start.AddMinutes(100);

        var track = GroundTrack.Compute(iss, start, end, TimeSpan.FromMinutes(7));

        Assert.Equal(start, track.Samples[0].Utc);
        Assert.Equal(end, track.Samples[^1].Utc);
        Assert.False(track.EndsInDecay);

        foreach (var sample in track.Samples)
        {
            var position = iss.GetPosition(sample.Utc)!;

            Assert.Equal(position.SubSatellitePoint.X, sample.Location.X, 9);
            Assert.Equal(position.SubSatellitePoint.Y, sample.Location.Y, 9);
        }
    }

    [Fact]
    public void Track_IsCutCleanlyAtThe180Meridian()
    {
        var track = TwoIssOrbits();

        Assert.True(track.Segments.Count >= 2, "two ISS orbits must cross the 180° meridian");

        foreach (var segment in track.Segments)
        {
            Assert.All(segment, p => Assert.InRange(p.Location.X, -180, 180));

            for (int i = 1; i < segment.Count; i++)
                Assert.True(Math.Abs(segment[i].Location.X - segment[i - 1].Location.X) < 180, "no line may streak across the map");
        }

        for (int i = 1; i < track.Segments.Count; i++)
        {
            var end = track.Segments[i - 1][^1];
            var start = track.Segments[i][0];

            Assert.Equal(180, Math.Abs(end.Location.X), 9);
            Assert.Equal(-end.Location.X, start.Location.X, 9);
            Assert.Equal(end.Location.Y, start.Location.Y, 9);
            Assert.Equal(end.Utc, start.Utc);
        }
    }

    [Fact]
    public void SplitAtAntimeridian_InterpolatesTheCrossing()
    {
        var t = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var segments = GroundTrack.SplitAtAntimeridian(new[]
        {
            new TrackPoint(t, new Point(170, 0), 400),
            new TrackPoint(t.AddMinutes(2), new Point(-170, 10), 410),
        });

        Assert.Equal(2, segments.Count);
        Assert.Equal(new Point(180, 5).ToString(), segments[0][^1].Location.ToString());
        Assert.Equal(new Point(-180, 5).ToString(), segments[1][0].Location.ToString());
        Assert.Equal(t.AddMinutes(1), segments[1][0].Utc);
        Assert.Equal(405, segments[1][0].AltitudeKm, 9);
    }

    [Fact]
    public void Track_OfTheIss_ReachesItsInclination()
    {
        var highest = TwoIssOrbits().Samples.Max(p => Math.Abs(p.Location.Y));

        Assert.InRange(highest, 51.5, 52.0);
    }

    [Fact]
    public void Track_StopsWhereTheSatelliteDecays()
    {
        // MINOTAUR R/B from the verification set: sub-orbital, lost within 50 minutes
        const string line1 = "1 28872U 05037B   05333.02012661  .25992681  00000-0  24476-3 0  1534";
        const string line2 = "2 28872  96.4736 157.9986 0303955 244.0492 110.6523 16.46015938 10708";

        var minotaur = Sgp4Propagator.Create(TwoLineElement.Parse(line1, line2));

        var track = GroundTrack.Compute(minotaur, minotaur.Elements.Epoch, minotaur.Elements.Epoch.AddHours(3));

        Assert.True(track.EndsInDecay);
        Assert.True(track.Samples[^1].Utc < minotaur.Elements.Epoch.AddHours(1));
    }

    [Fact]
    public void Track_RejectsABackwardWindow()
    {
        var iss = Iss;

        Assert.Throws<ArgumentException>(() => GroundTrack.Compute(iss, iss.Elements.Epoch, iss.Elements.Epoch.AddMinutes(-1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => GroundTrack.Compute(iss, iss.Elements.Epoch, iss.Elements.Epoch.AddMinutes(1), TimeSpan.Zero));
    }

    // ----------------------------------------------------------------- footprint

    [Fact]
    public void Footprint_AtZeroElevation_IsTheHorizonCircle()
    {
        var expected = Math.Acos(GreatCircle.MeanEarthRadiusKm / (GreatCircle.MeanEarthRadiusKm + 420)) * 180 / Math.PI;

        Assert.Equal(expected, Footprint.AngularRadiusDegrees(420, 0), 12);
    }

    [Fact]
    public void Footprint_OfAGeostationarySatellite_IsAbout81Degrees()
    {
        // the classic figure: a geostationary satellite sees out to 81.3° from its sub-point
        Assert.InRange(Footprint.AngularRadiusDegrees(35786, 0), 81.2, 81.4);
    }

    [Fact]
    public void Footprint_OfTheIssAt10Degrees_IsAbout1200Km()
    {
        var position = Iss.GetPosition(Iss.Elements.Epoch)!;

        var footprint = Footprint.Of(position);

        Assert.Equal(10, footprint.MinimumElevation);
        Assert.Equal(position.SubSatellitePoint.ToString(), footprint.Center.ToString());
        // the 2008 ISS flew at about 350 km, so its 10° circle is about 1 230 km across the ground
        Assert.InRange(position.AltitudeKm, 330, 370);
        Assert.InRange(footprint.RadiusKm, 1150, 1300);

        // a stricter elevation sees less
        Assert.True(Footprint.Of(position, 30).AngularRadius < footprint.AngularRadius);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(400, 90)]
    [InlineData(400, -1)]
    public void Footprint_RejectsInvalidInput(double altitude, double elevation)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Footprint.AngularRadiusDegrees(altitude, elevation));
    }

    // ----------------------------------------------------------------- swath

    [Fact]
    public void Swath_EdgesAreHalfTheWidthFromTheTrack()
    {
        // one orbit: with two, the next orbit's track crosses this swath near the turning latitudes,
        // and a vertex can then be nearer to a point of the other pass than to its own
        var iss = Iss;
        var track = GroundTrack.Compute(iss, iss.Elements.Epoch, iss.Elements.Epoch.AddMinutes(iss.PeriodMinutes));

        var rings = Swath.Compute(track, 290);

        Assert.NotEmpty(rings);

        foreach (var vertex in rings.SelectMany(r => r))
        {
            // points added by the clip at ±180 lie between two edge points, not on the offset line
            if (Math.Abs(Math.Abs(vertex.X) - 180) < 1e-9)
                continue;

            var nearest = track.Samples.Min(s => GreatCircle.DistanceKm(s.Location, vertex));

            Assert.InRange(nearest, 144.9, 145.1);
        }
    }

    [Fact]
    public void Swath_ContainsTheGroundTrack_AndStaysOnTheMap()
    {
        var track = TwoIssOrbits();

        var rings = Swath.Compute(track, 290);

        Assert.All(rings.SelectMany(r => r), p => Assert.InRange(p.X, -180, 180));

        // both sides of the map are used where the track crosses 180°
        Assert.Contains(rings, r => r.Any(p => p.X == 180));
        Assert.Contains(rings, r => r.Any(p => p.X == -180));

        // a sample where two chunks meet lies on their shared edge, where point-in-polygon is undefined
        var inside = track.Samples
            .Where((s, i) => i % (Swath.SamplesPerChunk - 1) != 0 && Math.Abs(Math.Abs(s.Location.X) - 180) > 2);

        foreach (var sample in inside)
            Assert.True(rings.Any(r => SphericalCapHelperTest.IsInside(r, sample.Location.X, sample.Location.Y)),
                $"track point {sample.Location} is outside the swath");
    }

    [Theory]
    // eastward over +180 first, and westward over −180 first
    [InlineData(false)]
    [InlineData(true)]
    public void LongitudeWindows_SplitsAShapeAcross180IntoTwoHalves(bool startWest)
    {
        var square = startWest
            ? new[] { new Point(-170, -5), new Point(170, -5), new Point(170, 5), new Point(-170, 5) }
            : new[] { new Point(170, -5), new Point(-170, -5), new Point(-170, 5), new Point(170, 5) };

        var pieces = LongitudeWindows.Split(square);

        Assert.Equal(2, pieces.Count);

        var east = pieces.Single(p => p.Max(q => q.X) == 180);
        var west = pieces.Single(p => p.Min(q => q.X) == -180);

        Assert.Equal(170, east.Min(q => q.X), 9);
        Assert.Equal(-170, west.Max(q => q.X), 9);
        Assert.All(pieces.SelectMany(p => p), q => Assert.InRange(q.Y, -5, 5));
    }

    [Fact]
    public void LongitudeWindows_LeavesAnOrdinaryShapeAlone()
    {
        var square = new[] { new Point(10, 10), new Point(20, 10), new Point(20, 20), new Point(10, 20) };

        var piece = Assert.Single(LongitudeWindows.Split(square));

        Assert.Equal(square.Select(p => p.ToString()), piece.Select(p => p.ToString()));
    }

    [Fact]
    public void GreatCircle_DestinationAndDistanceAgree()
    {
        var tehran = new Point(51.389, 35.689);

        foreach (var bearing in new[] { 0.0, 45, 90, 180, 270, 333 })
        {
            var there = GreatCircle.Destination(tehran, bearing, 500);

            Assert.Equal(500, GreatCircle.DistanceKm(tehran, there), 6);
            Assert.Equal(bearing % 360, GreatCircle.Bearing(tehran, there), 3);
        }
    }

    // ----------------------------------------------------------------- sensors

    [Fact]
    public void SensorCatalog_KnowsTheBuiltInSatellites_AndCanBeExtended()
    {
        var catalog = SensorCatalog.CreateDefault();

        Assert.True(catalog.TryGet(49260, out var landsat9));
        Assert.Equal(185, landsat9.SwathWidthKm);
        Assert.True(catalog.TryGet(42063, out var sentinel2B));
        Assert.Equal(290, sentinel2B.SwathWidthKm);
        Assert.False(catalog.TryGet(25544, out _));

        catalog.Set(25544, new SensorInfo("Camera", 50));
        Assert.True(catalog.TryGet(25544, out var custom));
        Assert.Equal("Camera (50 km)", custom.ToString());

        Assert.True(catalog.Remove(25544));
        Assert.False(catalog.TryGet(25544, out _));

        // each call starts from the built-in list again
        Assert.False(SensorCatalog.CreateDefault().TryGet(25544, out _));
    }
}
