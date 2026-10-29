using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IRI.Maptor.Core.Satellite;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Satellite.Elements;
using IRI.Maptor.Core.Satellite.Passes;
using IRI.Maptor.Core.Satellite.Propagation;
using IRI.Maptor.Core.Satellite.Tracks;

namespace IRI.Maptor.Tests.Satellite;

/// <summary>Deep-space orbits as a map sees them: GPS, geostationary, and the propagator's own guarantees.</summary>
public class DeepSpaceTest
{
    private static readonly string AssetFolder = Path.Combine(AppContext.BaseDirectory, "Assets", "Sgp4");

    private static OrbitalElements FromVerificationSet(int catalogNumber)
        => TleReader.ReadFile(Path.Combine(AssetFolder, "SGP4-VER.TLE"), _ => { }).First(e => e.CatalogNumberValue == catalogNumber);

    /// <summary>NAVSTAR (GPS IIR), half a sidereal day per orbit.</summary>
    private static Sgp4Propagator Gps => Sgp4Propagator.Create(FromVerificationSet(28129));

    /// <summary>A geosynchronous satellite at 0.002° inclination.</summary>
    private static Sgp4Propagator Geostationary => Sgp4Propagator.Create(FromVerificationSet(28626));

    [Fact]
    public void Classification_NearEarthAndDeepSpace()
    {
        var iss = Sgp4Propagator.Create(TwoLineElement.Parse(OrbitalElementsReadingTest.IssLine1, OrbitalElementsReadingTest.IssLine2));

        Assert.False(iss.IsDeepSpace);
        Assert.False(Sgp4Propagator.IsDeepSpaceOrbit(iss.Elements));

        Assert.True(Gps.IsDeepSpace);
        Assert.True(Sgp4Propagator.IsDeepSpaceOrbit(Gps.Elements));
        Assert.True(Sgp4Propagator.IsSupported(Gps.Elements));

        Assert.Equal(Sgp4OperationMode.Improved, Gps.OperationMode);
        Assert.Equal(Sgp4OperationMode.Afspc, Sgp4Propagator.Create(Gps.Elements, operationMode: Sgp4OperationMode.Afspc).OperationMode);
    }

    [Fact]
    public void Geostationary_StaysOverOnePlace_ForAWeek()
    {
        var geo = Geostationary;
        var epoch = geo.Elements.Epoch;

        var points = Enumerable.Range(0, 7 * 24).Select(h => geo.GetPosition(epoch.AddHours(h))!).ToList();

        var longitudes = points.Select(p => p.SubSatellitePoint.X).ToList();

        // a station-kept satellite drifts by a small fraction of a degree in a week
        Assert.InRange(longitudes.Max() - longitudes.Min(), 0, 0.5);
        Assert.All(points, p => Assert.InRange(p.SubSatellitePoint.Y, -0.2, 0.2));
        Assert.All(points, p => Assert.InRange(p.AltitudeKm, 35_700, 35_870));

        // it keeps pace with the Earth: almost no speed over the ground, about 3.07 km/s in space
        Assert.All(points, p => Assert.InRange(p.GroundSpeedKmS, 0, 0.05));
        Assert.All(points, p => Assert.InRange(p.SpeedKmS, 3.0, 3.15));

        // and sees about 70° around it above 10° elevation
        Assert.InRange(Footprint.Of(points[0], 10).AngularRadius, 69, 73);
    }

    [Fact]
    public void Gps_TrackRepeatsEverySiderealDay()
    {
        var gps = Gps;
        var epoch = gps.Elements.Epoch;

        // two orbits take one sidereal day, 1436.07 minutes, so the point beneath it comes back
        var siderealDay = TimeSpan.FromMinutes(1436.0682);

        foreach (var hours in new[] { 0, 3, 7, 11 })
        {
            var a = gps.GetPosition(epoch.AddHours(hours))!.SubSatellitePoint;
            var b = gps.GetPosition(epoch.AddHours(hours) + siderealDay)!.SubSatellitePoint;

            Assert.True(GreatCircle.DistanceKm(a, b) < 50, $"{GreatCircle.DistanceKm(a, b):F1} km apart after a sidereal day");
        }

        // at about 20,200 km, inclined 55°: the track reaches about 55° north and south
        var day = Enumerable.Range(0, 24 * 60 / 5).Select(m => gps.GetPosition(epoch.AddMinutes(m * 5))!).ToList();

        Assert.All(day, p => Assert.InRange(p.AltitudeKm, 19_900, 20_600));
        Assert.InRange(day.Max(p => p.SubSatellitePoint.Y), 54, 56);
        Assert.InRange(day.Min(p => p.SubSatellitePoint.Y), -56, -54);
    }

    [Fact]
    public void ResonanceIntegration_GivesTheSameAnswer_InAnyOrder_AndFromManyThreads()
    {
        var gps = Gps;
        var molniya = Sgp4Propagator.Create(FromVerificationSet(8195));

        var times = new[] { 0.0, 2880, -1440, 10_000, 719.5, 721, -20_000, 5_000.25 };

        foreach (var propagator in new[] { gps, molniya, Geostationary })
        {
            // each from a fresh propagator: what the reference's restart would give
            var fresh = times.ToDictionary(t => t, t => Sgp4Propagator.Create(propagator.Elements).PropagateMinutes(t).Position);

            // the same propagator, in a shuffled order and from several threads at once
            var shuffled = times.Reverse().Concat(times).ToArray();
            var results = new (double T, IRI.Maptor.Core.Satellite.Frames.Vector3 R)[shuffled.Length];

            Parallel.For(0, shuffled.Length, i => results[i] = (shuffled[i], propagator.PropagateMinutes(shuffled[i]).Position));

            foreach (var (t, r) in results)
            {
                Assert.Equal(fresh[t].X, r.X);
                Assert.Equal(fresh[t].Y, r.Y);
                Assert.Equal(fresh[t].Z, r.Z);
            }
        }
    }

    [Fact]
    public void GroundTrackAndFootprint_WorkForDeepSpace()
    {
        var gps = Gps;
        var epoch = gps.Elements.Epoch;

        var track = GroundTrack.Compute(gps, epoch, epoch.AddMinutes(gps.PeriodMinutes));

        Assert.False(track.EndsInDecay);
        Assert.InRange(track.Samples.Count, 140, 160);

        // the same 150 points per orbit as a low orbit, so each step covers about 5 minutes
        Assert.InRange((track.Samples[1].Utc - track.Samples[0].Utc).TotalMinutes, 4.5, 5.5);
    }

    // ------------------------------------------------------------------ passes

    private static readonly Point Tehran = new(51.389, 35.689);

    [Fact]
    public void ScanStep_GrowsWithThePeriod()
    {
        var iss = Sgp4Propagator.Create(TwoLineElement.Parse(OrbitalElementsReadingTest.IssLine1, OrbitalElementsReadingTest.IssLine2));

        Assert.Equal(PassFinder.ScanStep, PassFinder.ScanStepFor(iss));
        Assert.InRange(PassFinder.ScanStepFor(Gps).TotalMinutes, 3.5, 3.7);
        Assert.Equal(PassFinder.MaximumScanStep, PassFinder.ScanStepFor(Geostationary));
    }

    [Fact]
    public void GpsPasses_MatchASecondBySecondScan_AndAreNeverNakedEye()
    {
        var gps = Gps;
        var start = gps.Elements.Epoch;
        var end = start.AddDays(1);

        var passes = PassFinder.Visible(gps, PassRegion.FromPoint(Tehran), start, end, 10);

        // the scan, every second
        var brute = new List<(DateTime Start, DateTime End)>();
        DateTime? open = null;
        var last = start;
        for (var t = start; t <= end; t = t.AddSeconds(1))
        {
            var up = LookAngles.From(Tehran, gps.GetPosition(t)!).Elevation >= 10;
            if (up && open is null) open = t;
            else if (!up && open is DateTime o) { brute.Add((o, last)); open = null; }
            last = t;
        }
        if (open is DateTime still) brute.Add((still, last));

        Assert.NotEmpty(passes);
        Assert.Equal(brute.Count, passes.Count);

        for (var i = 0; i < passes.Count; i++)
        {
            if (!passes[i].StartsBeforeWindow)
                Assert.InRange((brute[i].Start - passes[i].Start).TotalSeconds, -0.1, 1.1);
            if (!passes[i].EndsAfterWindow)
                Assert.InRange((passes[i].End - brute[i].End).TotalSeconds, -0.1, 1.1);

            // a GPS satellite is in view for hours, not minutes
            Assert.True(passes[i].Duration > TimeSpan.FromHours(1) || passes[i].StartsBeforeWindow || passes[i].EndsAfterWindow, passes[i].ToString());
        }

        // in the dark or not, a satellite at 20,000 km is too faint to see
        Assert.All(passes, p => Assert.False(p.IsNakedEyeVisible));
    }

    [Fact]
    public void Geostationary_IsOnePassCoveringTheWindow_FromBelow_AndNoneFromTheFarSide()
    {
        var geo = Geostationary;
        var start = geo.Elements.Epoch;
        var end = start.AddDays(3);

        var below = geo.GetPosition(start)!.SubSatellitePoint;
        var observer = new Point(below.X, 30);

        var passes = PassFinder.Visible(geo, PassRegion.FromPoint(observer), start, end, 10);

        var pass = Assert.Single(passes);
        Assert.True(pass.CoversWholeWindow);
        Assert.Equal(start, pass.Start);
        Assert.Equal(end, pass.End);
        Assert.False(pass.IsNakedEyeVisible);
        Assert.InRange(pass.PeakElevation, 50, 56);
        Assert.InRange(pass.PeakAzimuth!.Value, 175, 185);   // due south, from 30° N

        var farSide = new Point(GreatCircle.NormalizeLongitude(below.X + 180), 0);
        Assert.Empty(PassFinder.Visible(geo, PassRegion.FromPoint(farSide), start, end, 10));
    }

    [Fact]
    public void AWholeWindowPass_PeaksAtItsHighestElevation_NotItsFirstDip()
    {
        // inclined 3.9°: seen from the equator it rises and falls a little every day
        var inclined = Sgp4Propagator.Create(FromVerificationSet(24208));
        var start = inclined.Elements.Epoch;
        var end = start.AddDays(3);

        var observer = new Point(inclined.GetPosition(start)!.SubSatellitePoint.X + 20, 0);

        var pass = Assert.Single(PassFinder.Visible(inclined, PassRegion.FromPoint(observer), start, end, 10));
        Assert.True(pass.CoversWholeWindow);

        var best = Enumerable.Range(0, 3 * 24 * 60).Max(m => LookAngles.From(observer, inclined.GetPosition(start.AddMinutes(m))!).Elevation);

        Assert.InRange(pass.PeakElevation - best, -0.01, 0.01);
    }

    // ------------------------------------------------------------------ orbit classes

    [Fact]
    public void OrbitClasses_TellTheKindsApart()
    {
        var iss = TwoLineElement.Parse(OrbitalElementsReadingTest.IssLine1, OrbitalElementsReadingTest.IssLine2);

        Assert.Equal(OrbitClass.LowEarth, OrbitClasses.Of(iss));
        Assert.Equal(OrbitClass.MediumEarth, OrbitClasses.Of(Gps.Elements));
        Assert.Equal(OrbitClass.Geostationary, OrbitClasses.Of(Geostationary.Elements));
        Assert.Equal(OrbitClass.Geosynchronous, OrbitClasses.Of(FromVerificationSet(24208)));
        Assert.Equal(OrbitClass.HighlyElliptical, OrbitClasses.Of(FromVerificationSet(8195)));   // Molniya

        // Tundra (Sirius radio): a 24-hour orbit, but elliptical, so not geosynchronous in this sense
        var tundra = new OrbitalElements("99001", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            inclination: 63.4, rightAscension: 0, eccentricity: 0.27, argumentOfPerigee: 270, meanAnomaly: 0, meanMotion: 1.0027, bStar: 0);
        Assert.Equal(OrbitClass.HighlyElliptical, OrbitClasses.Of(tundra));

        var (perigee, apogee) = OrbitClasses.HeightsKm(iss);
        Assert.InRange(perigee, 330, 370);
        Assert.InRange(apogee, 340, 380);

        var (gpsPerigee, gpsApogee) = OrbitClasses.HeightsKm(Gps.Elements);
        Assert.InRange(gpsPerigee, 19_900, 20_300);
        Assert.InRange(gpsApogee, 20_100, 20_500);
    }
}
