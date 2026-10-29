using System;
using System.Linq;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Persistence.DataSources;
using IRI.Maptor.Core.Satellite;
using IRI.Maptor.Core.Satellite.Elements;
using IRI.Maptor.Core.Satellite.Propagation;
using IRI.Maptor.Core.Satellite.Tracks;
using IRI.Maptor.Presentation.Wpf.Cartography.Symbologies;
using IRI.Maptor.Presentation.Wpf.Layers;
using IRI.Maptor.Tests.Common;

namespace IRI.Maptor.Tests.Satellite;

/// <summary>Choosing a satellite by clicking it, which satellites are named, and the ready-made layer.</summary>
public class SatelliteSelectionTest
{
    private static Sgp4Propagator Iss => Sgp4Propagator.Create(
        TwoLineElement.Parse(OrbitalElementsReadingTest.IssLine1, OrbitalElementsReadingTest.IssLine2, "ISS"));

    /// <summary>The same orbit half a revolution on: far from the ISS at any instant.</summary>
    private static Sgp4Propagator OtherSide => Sgp4Propagator.Create(TwoLineElement.Parse(
        OrbitalElementsReadingTest.WithChecksum(OrbitalElementsReadingTest.IssLine1.Replace("25544U", "25545U")),
        OrbitalElementsReadingTest.WithChecksum(OrbitalElementsReadingTest.IssLine2.Replace("2 25544", "2 25545").Replace("325.0288", "145.0288")),
        "OTHER SIDE"));

    /// <summary>A plain map: 10 pixels a degree, north up, no repeated worlds.</summary>
    private static Point ToScreen(Point geographic) => new(geographic.X * 10, -geographic.Y * 10);

    private static Point ScreenOf(Sgp4Propagator satellite, DateTime utc) => ToScreen(satellite.GetPosition(utc)!.SubSatellitePoint);

    [Fact]
    public void HitTest_FindsTheSatelliteUnderTheClick_WithinTheTolerance()
    {
        var iss = Iss;
        var time = iss.Elements.Epoch;
        var source = new SatellitesDataSource { Satellites = new[] { iss }, Time = time };

        var at = ScreenOf(iss, time);

        Assert.Same(iss, source.HitTest(new Point(at.X + 3, at.Y + 4), ToScreen));          // 5 px
        Assert.Same(iss, source.HitTest(new Point(at.X + 6, at.Y + 8), ToScreen));          // 10 px: the edge counts
        Assert.Null(source.HitTest(new Point(at.X + 6, at.Y + 8.1), ToScreen));             // just outside
        Assert.Same(iss, source.HitTest(new Point(at.X + 20, at.Y), ToScreen, tolerance: 25));
    }

    [Fact]
    public void HitTest_TheNearestSatelliteWins()
    {
        var iss = Iss;
        var other = OtherSide;
        var time = iss.Elements.Epoch;
        var source = new SatellitesDataSource { Satellites = new[] { iss, other }, Time = time };

        var issAt = ScreenOf(iss, time);
        var otherAt = ScreenOf(other, time);

        Assert.True(Math.Abs(issAt.X - otherAt.X) + Math.Abs(issAt.Y - otherAt.Y) > 500, "the two must be far apart");

        // a tolerance that covers the whole map: the nearer one is chosen, whichever is listed first
        Assert.Same(iss, source.HitTest(issAt, ToScreen, tolerance: 1e6));
        Assert.Same(other, source.HitTest(otherAt, ToScreen, tolerance: 1e6));
    }

    [Fact]
    public void HitTest_FindsASatelliteAcrossTheAntimeridian_OnAMapThatRepeatsTheWorld()
    {
        var iss = Iss;
        var time = iss.Elements.Epoch;
        var source = new SatellitesDataSource { Satellites = new[] { iss }, Time = time };

        var p = iss.GetPosition(time)!.SubSatellitePoint;

        // the copy one world to the east, and one to the west
        Assert.Same(iss, source.HitTest(ToScreen(new Point(p.X + 360, p.Y)), ToScreen));
        Assert.Same(iss, source.HitTest(ToScreen(new Point(p.X - 360, p.Y)), ToScreen));
        Assert.Null(source.HitTest(ToScreen(new Point(p.X + 180, p.Y)), ToScreen));
    }

    [Fact]
    public void HitTest_SkipsDecayedSatellites_AndAnEmptyList()
    {
        // MINOTAUR R/B from the verification set: lost within an hour of its epoch
        var minotaur = Sgp4Propagator.Create(TwoLineElement.Parse(
            "1 28872U 05037B   05333.02012661  .25992681  00000-0  24476-3 0  1534",
            "2 28872  96.4736 157.9986 0303955 244.0492 110.6523 16.46015938 10708"));

        // the first minute SGP4 reports it lost (a later instant may come out "valid" again, from below ground)
        var lost = Enumerable.Range(1, 180).Select(m => minotaur.Elements.Epoch.AddMinutes(m)).First(t => minotaur.GetPosition(t) is null);

        var source = new SatellitesDataSource { Satellites = new[] { minotaur }, Time = lost };

        Assert.Null(minotaur.GetPosition(source.Time));
        Assert.Null(source.HitTest(new Point(0, 0), ToScreen, tolerance: 1e9));

        Assert.Null(new SatellitesDataSource().HitTest(new Point(0, 0), ToScreen, tolerance: 1e9));
        Assert.Throws<ArgumentNullException>(() => source.HitTest(new Point(0, 0), null!));
    }

    // ------------------------------------------------------------------ labels

    private static string[] Labeled(SatellitesDataSource source) => source.GetFeatures()
        .Where(f => f.LabelAttribute == SatellitesDataSource.NameFieldName)
        .Select(f => f.Label)
        .OrderBy(n => n)
        .ToArray();

    [Fact]
    public void Labels_AutoNamesAll_UpToTheLimit_ThenOnlyTheSelectedOne()
    {
        var iss = Iss;
        var other = OtherSide;
        var source = new SatellitesDataSource { Satellites = new[] { iss, other }, Selected = iss, Time = iss.Elements.Epoch };

        Assert.Equal(SatelliteLabelMode.Auto, source.LabelMode);
        Assert.Equal(new[] { "ISS", "OTHER SIDE" }, Labeled(source));

        source.AutoLabelLimit = 2;
        Assert.Equal(new[] { "ISS", "OTHER SIDE" }, Labeled(source));

        source.AutoLabelLimit = 1;
        Assert.Equal(new[] { "ISS" }, Labeled(source));

        source.Selected = null;
        Assert.Empty(Labeled(source));
    }

    [Fact]
    public void Labels_AllAndSelectedOnly_IgnoreTheLimit()
    {
        var iss = Iss;
        var other = OtherSide;
        var source = new SatellitesDataSource { Satellites = new[] { iss, other }, Selected = other, Time = iss.Elements.Epoch, AutoLabelLimit = 0 };

        source.LabelMode = SatelliteLabelMode.All;
        Assert.Equal(new[] { "ISS", "OTHER SIDE" }, Labeled(source));

        source.LabelMode = SatelliteLabelMode.SelectedOnly;
        source.AutoLabelLimit = 100;
        Assert.Equal(new[] { "OTHER SIDE" }, Labeled(source));

        // tracks and areas are never named
        Assert.All(source.GetFeatures().Where(f => f.LabelAttribute == SatellitesDataSource.NameFieldName),
            f => Assert.Contains((string)f.Attributes[SatellitesDataSource.KindFieldName], new[] { SatellitesDataSource.SelectedKind, SatellitesDataSource.OtherKind }));
    }

    // ------------------------------------------------------------------ the layer

    [Fact]
    public void Layer_DrawsEveryFeatureWithExactlyOneShapeSymbol_AndNamesOnlyLabeledOnes()
    {
        WpfTestHost.Run(() =>
        {
            var iss = Iss;
            var other = OtherSide;

            var sensors = SensorCatalog.CreateDefault();
            sensors.Set(25544, new SensorInfo("Test camera", 100));

            var source = new SatellitesDataSource
            {
                Satellites = new[] { iss, other },
                Selected = iss,
                Time = iss.Elements.Epoch,
                Sensors = sensors,
                LabelMode = SatelliteLabelMode.SelectedOnly,
            };

            var layer = SatelliteLayers.Create(source);

            var shapes = layer.Symbolizers.Where(s => s is not LabelSymbolizer).ToList();
            var labels = layer.Symbolizers.OfType<LabelSymbolizer>().Single();

            var features = source.GetFeatures();

            // all six kinds are present, so each shape symbol is exercised
            Assert.Equal(6, features.Select(f => f.Attributes[SatellitesDataSource.KindFieldName]).Distinct().Count());

            Assert.All(features, f => Assert.Single(shapes, s => s.IsFilterPassed(f)));

            Assert.Equal(new[] { "ISS" }, features.Where(labels.IsFilterPassed).Select(f => f.Label).ToArray());

            // the order the symbols are drawn in is the order the source gives the kinds
            var order = new[]
            {
                SatellitesDataSource.SwathKind, SatellitesDataSource.FootprintKind, SatellitesDataSource.RegionKind,
                SatellitesDataSource.PastTrackKind, SatellitesDataSource.FutureTrackKind, SatellitesDataSource.PassTrackKind,
                SatellitesDataSource.OtherKind, SatellitesDataSource.SelectedKind,
            };

            Assert.Equal(order.Length, shapes.Count);

            // no region or pass here: those two are checked in PassesTest
            for (var i = 0; i < order.Length; i++)
                if (features.FirstOrDefault(f => (string)f.Attributes[SatellitesDataSource.KindFieldName] == order[i]) is { } feature)
                    Assert.True(shapes[i].IsFilterPassed(feature), order[i]);
        });
    }

    [Fact]
    public void Layer_RejectsNoSource()
    {
        Assert.Throws<ArgumentNullException>(() => SatelliteLayers.Create(null!));
    }
}
