using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using IRI.Maptor.Core.Astronomy.Solar;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Persistence.DataSources;
using IRI.Maptor.Core.Satellite;
using IRI.Maptor.Core.Satellite.Elements;
using IRI.Maptor.Core.Satellite.Frames;
using IRI.Maptor.Core.Satellite.Passes;
using IRI.Maptor.Core.Satellite.Propagation;
using IRI.Maptor.Core.Satellite.Tracks;
using IRI.Maptor.Presentation.Wpf.Cartography.Symbologies;
using IRI.Maptor.Presentation.Wpf.Layers;
using IRI.Maptor.Tests.Common;

namespace IRI.Maptor.Tests.Satellite;

/// <summary>Passes over a point or an area: the search against brute force, the geometry against independent formulas.</summary>
public class PassesTest
{
    private static Sgp4Propagator Iss => Sgp4Propagator.Create(
        TwoLineElement.Parse(OrbitalElementsReadingTest.IssLine1, OrbitalElementsReadingTest.IssLine2, "ISS"));

    private static readonly Point Tehran = new(51.389, 35.689);

    /// <summary>Roughly Iran: 44–63° E, 25–40° N.</summary>
    private static PassRegion Iran => PassRegion.FromPolygon(new[] { new Point(44, 25), new Point(63, 25), new Point(63, 40), new Point(44, 40) });

    /// <summary>Intervals where <paramref name="inside"/> holds, sampled every second.</summary>
    private static List<(DateTime Start, DateTime End)> BruteForce(DateTime start, DateTime end, Func<DateTime, bool> inside)
    {
        var intervals = new List<(DateTime, DateTime)>();

        DateTime? open = null;

        var last = start;

        for (var t = start; t <= end; t = t.AddSeconds(1))
        {
            var now = inside(t);

            if (now && open is null)
                open = t;
            else if (!now && open is DateTime s)
            {
                intervals.Add((s, last));
                open = null;
            }

            last = t;
        }

        if (open is DateTime o)
            intervals.Add((o, last));

        return intervals;
    }

    // ------------------------------------------------------------------ visible from a point

    [Fact]
    public void VisibleFromAPoint_MatchesASecondBySecondScan()
    {
        var iss = Iss;
        var start = iss.Elements.Epoch;
        var end = start.AddDays(2);

        var passes = PassFinder.Visible(iss, PassRegion.FromPoint(Tehran), start, end, minimumElevation: 10);

        var brute = BruteForce(start, end, t => LookAngles.From(Tehran, iss.GetPosition(t)!).Elevation >= 10);

        Assert.NotEmpty(passes);
        Assert.Equal(brute.Count, passes.Count);

        for (var i = 0; i < passes.Count; i++)
        {
            // the scan's first second inside is up to 1 s after the true start, its last up to 1 s before the end
            Assert.InRange((brute[i].Start - passes[i].Start).TotalSeconds, -0.1, 1.1);
            Assert.InRange((passes[i].End - brute[i].End).TotalSeconds, -0.1, 1.1);

            var bruteMax = Enumerable.Range(0, (int)(brute[i].End - brute[i].Start).TotalSeconds + 1)
                .Max(s => LookAngles.From(Tehran, iss.GetPosition(brute[i].Start.AddSeconds(s))!).Elevation);

            Assert.InRange(passes[i].PeakElevation - bruteMax, -0.01, 0.05);
            Assert.True(passes[i].Start < passes[i].Peak && passes[i].Peak < passes[i].End);
            Assert.False(passes[i].StartsBeforeWindow || passes[i].EndsAfterWindow);

            var northbound = iss.GetPosition(passes[i].Peak.AddSeconds(30))!.SubSatellitePoint.Y > iss.GetPosition(passes[i].Peak.AddSeconds(-30))!.SubSatellitePoint.Y;
            Assert.Equal(northbound, passes[i].IsNorthbound);
        }
    }

    [Fact]
    public void VisibleFromAPoint_GivesDirections_AndElevationsAtTheEdgesAreTheMinimum()
    {
        var iss = Iss;
        var start = iss.Elements.Epoch;

        foreach (var pass in PassFinder.Visible(iss, PassRegion.FromPoint(Tehran), start, start.AddDays(1), 10))
        {
            Assert.NotNull(pass.RiseAzimuth);
            Assert.NotNull(pass.SetAzimuth);
            Assert.NotNull(pass.PeakAzimuth);

            Assert.InRange(LookAngles.From(Tehran, iss.GetPosition(pass.Start)!).Elevation, 9.95, 10.05);
            Assert.InRange(LookAngles.From(Tehran, iss.GetPosition(pass.End)!).Elevation, 9.95, 10.05);

            Assert.True(pass.Duration > TimeSpan.FromMinutes(1) && pass.Duration < TimeSpan.FromMinutes(10), pass.ToString());
        }
    }

    [Fact]
    public void ATinySquare_GivesTheSamePassesAsItsCentrePoint()
    {
        var iss = Iss;
        var start = iss.Elements.Epoch;
        var end = start.AddDays(2);

        const double d = 0.001;
        var square = PassRegion.FromPolygon(new[]
        {
            new Point(Tehran.X - d, Tehran.Y - d), new Point(Tehran.X + d, Tehran.Y - d),
            new Point(Tehran.X + d, Tehran.Y + d), new Point(Tehran.X - d, Tehran.Y + d),
        });

        var fromPoint = PassFinder.Visible(iss, PassRegion.FromPoint(Tehran), start, end);
        var fromSquare = PassFinder.Visible(iss, square, start, end);

        Assert.Equal(fromPoint.Count, fromSquare.Count);

        for (var i = 0; i < fromPoint.Count; i++)
        {
            // the square uses a sphere, the point the ellipsoid: a few seconds apart at most
            Assert.InRange((fromSquare[i].Start - fromPoint[i].Start).TotalSeconds, -5, 5);
            Assert.InRange((fromSquare[i].End - fromPoint[i].End).TotalSeconds, -5, 5);
            Assert.InRange(fromSquare[i].PeakElevation - fromPoint[i].PeakElevation, -0.5, 0.5);
        }
    }

    // ------------------------------------------------------------------ visible from an area

    [Fact]
    public void VisibleFromAnArea_MatchesAScan_AndThePeakIsTheBestElevationInTheArea()
    {
        var iss = Iss;
        var start = iss.Elements.Epoch;
        var end = start.AddDays(1);
        var iran = Iran;

        var passes = PassFinder.Visible(iss, iran, start, end, 10);

        // the scan uses the same measure, so this checks the search; the elevation below checks the measure
        var brute = BruteForce(start, end, t =>
        {
            var p = iss.GetPosition(t)!;
            return iran.DistanceKm(p.SubSatellitePoint) <= Footprint.Of(p, 10).RadiusKm;
        });

        Assert.NotEmpty(passes);
        Assert.Equal(brute.Count, passes.Count);

        for (var i = 0; i < passes.Count; i++)
        {
            Assert.InRange((brute[i].Start - passes[i].Start).TotalSeconds, -0.1, 1.1);
            Assert.InRange((passes[i].End - brute[i].End).TotalSeconds, -0.1, 1.1);

            // directly: the highest elevation over a fine grid of the area (plus its corners) at the peak
            var at = iss.GetPosition(passes[i].Peak)!;
            var candidates = iran.SamplePoints(2500).Concat(Edges(iran, 0.02)).ToList();

            if (iran.Contains(at.SubSatellitePoint))
                candidates.Add(at.SubSatellitePoint);

            var best = candidates.Max(q => LookAngles.From(q, at).Elevation);

            Assert.InRange(passes[i].PeakElevation - best, -0.5, 0.5);
            Assert.Null(passes[i].RiseAzimuth);
        }
    }

    [Fact]
    public void ARegionTheOrbitNeverReaches_HasNoPasses()
    {
        var iss = Iss;

        // 51.6° inclination plus a 10° footprint reaches about 65° N
        var passes = PassFinder.Visible(iss, PassRegion.FromPoint(new Point(20, 80)), iss.Elements.Epoch, iss.Elements.Epoch.AddDays(3));

        Assert.Empty(passes);
    }

    // ------------------------------------------------------------------ imaged

    [Fact]
    public void ImagedPoint_LiesInsideTheSwathDrawnForThePass()
    {
        var iss = Iss;
        var start = iss.Elements.Epoch;
        const double width = 290;

        var passes = PassFinder.Imaged(iss, width, PassRegion.FromPoint(Tehran), start, start.AddDays(5));

        Assert.NotEmpty(passes);

        foreach (var pass in passes)
        {
            Assert.Equal(PassKind.Imaged, pass.Kind);
            Assert.Equal(1.0, pass.CoveredFraction);

            // the strip drawn along the track a minute either side of the pass contains the point
            var track = GroundTrack.Compute(iss, pass.Start.AddMinutes(-1), pass.End.AddMinutes(1), TimeSpan.FromSeconds(5));

            Assert.Contains(Swath.Compute(track, width), ring => PassRegion.FromPolygon(ring).Contains(Tehran));

            // and a strip half as wide misses it whenever the pass is near its edge
            var nearest = Enumerable.Range(0, (int)pass.Duration.TotalSeconds + 1)
                .Min(s => GreatCircle.DistanceKm(Tehran, iss.GetPosition(pass.Start.AddSeconds(s))!.SubSatellitePoint));

            Assert.True(nearest <= width / 2 + 1, $"{nearest:F1} km");
        }
    }

    [Fact]
    public void ImagedArea_CoverageMatchesTheShareOfTheAreaInsideTheSwath()
    {
        var iss = Iss;
        var start = iss.Elements.Epoch;
        const double width = 290;
        var iran = Iran;

        var passes = PassFinder.Imaged(iss, width, iran, start, start.AddDays(2));

        Assert.NotEmpty(passes);

        var samples = iran.SamplePoints();

        foreach (var pass in passes)
        {
            Assert.InRange(pass.CoveredFraction!.Value, 0, 1);

            var track = GroundTrack.Compute(iss, pass.Start.AddMinutes(-2), pass.End.AddMinutes(2), TimeSpan.FromSeconds(5));
            var rings = Swath.Compute(track, width).Select(PassRegion.FromPolygon).ToList();

            var inside = (double)samples.Count(s => rings.Any(r => r.Contains(s))) / samples.Count;

            // the disc sweep and the drawn strip differ only at the strip's ends and its chunk seams
            Assert.InRange(pass.CoveredFraction.Value - inside, -0.03, 0.03);
        }

        // a 290 km strip never covers all of a 1,900 km wide area
        Assert.All(passes, p => Assert.True(p.CoveredFraction < 0.5));
    }

    [Fact]
    public void ImagedPasses_UseTheSensorSwath()
    {
        var iss = Iss;
        var start = iss.Elements.Epoch;
        var region = PassRegion.FromPoint(Tehran);

        var narrow = PassFinder.Imaged(iss, new SensorInfo("narrow", 100), region, start, start.AddDays(7));
        var wide = PassFinder.Imaged(iss, new SensorInfo("wide", 1000), region, start, start.AddDays(7));

        Assert.True(wide.Count > narrow.Count);
        Assert.All(narrow, p => Assert.Contains(wide, w => w.Start <= p.Start && p.End <= w.End));
    }

    // ------------------------------------------------------------------ sunlight

    [Fact]
    public void Sunlight_FollowsTheShadowCylinder()
    {
        var utc = new DateTime(2026, 3, 20, 12, 0, 0, DateTimeKind.Utc);

        var sun = Sunlight.SunDirection(utc);
        Assert.InRange(sun.Length, 0.999999, 1.000001);

        // the Sun is overhead at the sub-solar point
        var subSolar = Sun.GetSubSolarPoint(utc);
        Assert.InRange(Sun.GetHorizontal(subSolar, utc).Elevation, 89.5, 90.0);

        Assert.True(Sunlight.IsSunlit(7000 * sun, utc));
        Assert.False(Sunlight.IsSunlit(-7000 * sun, utc));

        // behind the Earth but well off to the side of its shadow
        var side = Normalize(Cross(sun, new Vector3(0, 0, 1)));
        Assert.True(Sunlight.IsSunlit(-3000 * sun + 7000 * side, utc));
        Assert.False(Sunlight.IsSunlit(-3000 * sun + 6000 * side, utc));
    }

    [Fact]
    public void NakedEye_OnlyAgainstADarkSky_AndTheIssHasSomeOverTwoWeeks()
    {
        var iss = Iss;
        var start = iss.Elements.Epoch;

        var passes = PassFinder.Visible(iss, PassRegion.FromPoint(Tehran), start, start.AddDays(14), 10);

        // a pass with the Sun well up at its peak never has a dark sky (it lasts minutes, the Sun moves ~2°)
        Assert.All(passes.Where(p => p.SunElevation > 5), p => Assert.False(p.IsNakedEyeVisible));
        Assert.All(passes.Where(p => p.IsNakedEyeVisible), p => Assert.True(p.SunElevation < 0));

        Assert.Contains(passes, p => p.IsNakedEyeVisible);
        Assert.Contains(passes, p => p.IsDaylight);
    }

    // ------------------------------------------------------------------ edges of the window

    [Fact]
    public void APassUnderWayAtTheStart_IsCutAtTheWindow()
    {
        var iss = Iss;
        var start = iss.Elements.Epoch;
        var region = PassRegion.FromPoint(Tehran);

        var full = PassFinder.Visible(iss, region, start, start.AddDays(1))[0];

        var middle = full.Start + TimeSpan.FromTicks(full.Duration.Ticks / 2);

        var cut = PassFinder.Visible(iss, region, middle, middle.AddHours(1));

        Assert.True(cut[0].StartsBeforeWindow);
        Assert.Equal(middle, cut[0].Start);
        Assert.InRange((cut[0].End - full.End).TotalSeconds, -0.2, 0.2);
        Assert.Null(cut[0].RiseAzimuth);

        var ends = PassFinder.Visible(iss, region, start, middle);

        Assert.True(ends[^1].EndsAfterWindow);
        Assert.Equal(middle, ends[^1].End);
    }

    [Fact]
    public void ASatelliteThatDecays_IsSearchedUpToItsDecay()
    {
        // MINOTAUR R/B from the verification set: lost within an hour of its epoch
        var minotaur = Sgp4Propagator.Create(TwoLineElement.Parse(
            "1 28872U 05037B   05333.02012661  .25992681  00000-0  24476-3 0  1534",
            "2 28872  96.4736 157.9986 0303955 244.0492 110.6523 16.46015938 10708"));

        var start = minotaur.Elements.Epoch;

        var lost = Enumerable.Range(1, 180).Select(m => start.AddMinutes(m)).First(t => minotaur.GetPosition(t) is null);

        var passes = PassFinder.Imaged(minotaur, 3000, PassRegion.FromPoint(new Point(0, 0)), start, start.AddDays(1));

        Assert.All(passes, p => Assert.True(p.End < lost));
    }

    [Fact]
    public void Search_IsCancellable_AndChecksItsArguments()
    {
        var iss = Iss;
        var start = iss.Elements.Epoch;
        var region = PassRegion.FromPoint(Tehran);

        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        Assert.ThrowsAny<OperationCanceledException>(() => PassFinder.Visible(iss, region, start, start.AddDays(7), 10, cancelled.Token));
        Assert.ThrowsAny<OperationCanceledException>(() => PassFinder.Imaged(iss, 290, region, start, start.AddDays(7), cancelled.Token));

        Assert.Throws<ArgumentException>(() => PassFinder.Visible(iss, region, start, start));
        Assert.Throws<ArgumentOutOfRangeException>(() => PassFinder.Visible(iss, region, start, start.AddDays(1), 90));
        Assert.Throws<ArgumentOutOfRangeException>(() => PassFinder.Imaged(iss, 0, region, start, start.AddDays(1)));
        Assert.Throws<ArgumentNullException>(() => PassFinder.Visible(null!, region, start, start.AddDays(1)));
        Assert.Throws<ArgumentNullException>(() => PassFinder.Visible(iss, null!, start, start.AddDays(1)));
    }

    // ------------------------------------------------------------------ region and look angles

    [Fact]
    public void Region_DistancesAndInside()
    {
        var strip = PassRegion.FromPolygon(new[] { new Point(0, 0), new Point(10, 0), new Point(10, 5), new Point(0, 5) });

        Assert.Equal(0, strip.DistanceKm(new Point(5, 2)));
        Assert.True(strip.Contains(new Point(5, 2)));
        Assert.False(strip.Contains(new Point(5, -1)));

        // 1° south of the bottom edge, and beyond its corner
        Assert.InRange(strip.DistanceKm(new Point(5, -1)), 111.0, 111.4);
        Assert.InRange(strip.DistanceKm(new Point(-1, -1)), GreatCircle.DistanceKm(new Point(-1, -1), new Point(0, 0)) - 0.01, GreatCircle.DistanceKm(new Point(-1, -1), new Point(0, 0)) + 0.01);

        Assert.Equal(new Point(5, 2.5), strip.Center);
        Assert.All(strip.SamplePoints(), p => Assert.True(strip.Contains(p)));
        Assert.InRange(strip.SamplePoints(400).Count, 350, 450);

        var point = PassRegion.FromPoint(new Point(190, 10));
        Assert.True(point.IsPoint);
        Assert.Equal(-170, point.Center.X, 9);

        Assert.Throws<ArgumentException>(() => PassRegion.FromPolygon(new[] { new Point(0, 0), new Point(1, 1) }));
        Assert.Throws<ArgumentException>(() => PassRegion.FromPolygon(new[] { new Point(170, 0), new Point(-170, 0), new Point(-170, 5) }));
        Assert.Throws<ArgumentOutOfRangeException>(() => PassRegion.FromPoint(new Point(0, 91)));
    }

    [Fact]
    public void LookAngles_OverheadNorthAndEast()
    {
        var site = new Point(30, 40);

        var overhead = LookAngles.From(site, 0, Wgs84Geodetic.ToEcef(site, 500));
        Assert.InRange(overhead.Elevation, 89.999, 90.0);
        Assert.InRange(overhead.RangeKm, 499.999, 500.001);

        // from the point beneath a real satellite: exactly overhead, never NaN
        var iss = Iss;
        var position = iss.GetPosition(iss.Elements.Epoch)!;
        Assert.InRange(LookAngles.From(position.SubSatellitePoint, position).Elevation, 89.99, 90.0);

        var north = LookAngles.From(site, 0, Wgs84Geodetic.ToEcef(new Point(30, 45), 500));
        Assert.True(north.Azimuth < 1 || north.Azimuth > 359, north.ToString());

        var east = LookAngles.From(site, 0, Wgs84Geodetic.ToEcef(new Point(35, 40), 500));
        Assert.InRange(east.Azimuth, 80, 95);

        // the sphere formula agrees with the ellipsoid to a few tenths of a degree
        var distance = GreatCircle.DistanceKm(site, new Point(35, 40));
        Assert.InRange(PassFinder.ElevationAt(distance, 500) - east.Elevation, -0.5, 0.5);
        Assert.Equal(90, PassFinder.ElevationAt(0, 500), 6);
    }

    // ------------------------------------------------------------------ on the map

    [Fact]
    public void DataSource_DrawsTheRegionAndTheHighlightedPass_InOrder()
    {
        var iss = Iss;
        var pass = PassFinder.Visible(iss, Iran, iss.Elements.Epoch, iss.Elements.Epoch.AddDays(1))[0];

        var source = new SatellitesDataSource
        {
            Satellites = new[] { iss },
            Selected = iss,
            Time = pass.Peak,
            Region = Iran,
            HighlightedPass = pass,
        };

        var kinds = source.GetFeatures().Select(f => (string)f.Attributes[SatellitesDataSource.KindFieldName]).ToList();

        Assert.Single(kinds, k => k == SatellitesDataSource.RegionKind);
        Assert.Contains(SatellitesDataSource.PassTrackKind, kinds);

        // region over the footprint, under the tracks; the pass over the tracks, under the satellites
        Assert.True(kinds.LastIndexOf(SatellitesDataSource.FootprintKind) < kinds.IndexOf(SatellitesDataSource.RegionKind));
        Assert.True(kinds.IndexOf(SatellitesDataSource.RegionKind) < kinds.IndexOf(SatellitesDataSource.PastTrackKind));
        Assert.True(kinds.LastIndexOf(SatellitesDataSource.FutureTrackKind) < kinds.IndexOf(SatellitesDataSource.PassTrackKind));
        Assert.True(kinds.LastIndexOf(SatellitesDataSource.PassTrackKind) < kinds.IndexOf(SatellitesDataSource.SelectedKind));

        source.Region = PassRegion.FromPoint(Tehran);
        source.HighlightedPass = null;

        var features = source.GetFeatures();
        var region = features.Single(f => (string)f.Attributes[SatellitesDataSource.KindFieldName] == SatellitesDataSource.RegionKind);

        Assert.Equal(Core.Common.Enums.GeometryType.Point, region.TheGeometry.Type);
        Assert.DoesNotContain(features, f => (string)f.Attributes[SatellitesDataSource.KindFieldName] == SatellitesDataSource.PassTrackKind);
    }

    [Fact]
    public void Layer_HasOneShapeSymbolForTheRegionAndThePass()
    {
        WpfTestHost.Run(() =>
        {
            var iss = Iss;
            var pass = PassFinder.Visible(iss, Iran, iss.Elements.Epoch, iss.Elements.Epoch.AddDays(1))[0];

            var source = new SatellitesDataSource { Satellites = new[] { iss }, Selected = iss, Time = pass.Peak, Region = Iran, HighlightedPass = pass };

            var layer = SatelliteLayers.Create(source);
            var shapes = layer.Symbolizers.Where(s => s is not LabelSymbolizer).ToList();

            Assert.All(source.GetFeatures(), f => Assert.Single(shapes, s => s.IsFilterPassed(f)));
        });
    }

    /// <summary>Points every <paramref name="step"/> degrees along the polygon's edges.</summary>
    private static IEnumerable<Point> Edges(PassRegion region, double step)
    {
        var v = region.Vertices;

        for (var i = 0; i < v.Count; i++)
        {
            var a = v[i];
            var b = v[(i + 1) % v.Count];
            var n = (int)Math.Ceiling(Math.Max(Math.Abs(b.X - a.X), Math.Abs(b.Y - a.Y)) / step);

            for (var k = 0; k < n; k++)
                yield return new Point(a.X + (b.X - a.X) * k / n, a.Y + (b.Y - a.Y) * k / n);
        }
    }

    private static Vector3 Cross(Vector3 a, Vector3 b) => new(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);

    private static Vector3 Normalize(Vector3 v) => (1 / v.Length) * v;
}
