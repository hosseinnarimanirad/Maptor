using System.Collections.Generic;
using System.Linq;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Persistence.DataSources;
using IRI.Maptor.Core.Satellite;
using IRI.Maptor.Core.Satellite.Elements;
using IRI.Maptor.Core.Satellite.Propagation;
using IRI.Maptor.Core.Satellite.Tracks;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Tests.Astronomy;

namespace IRI.Maptor.Tests.Satellite;

public class SatellitesDataSourceTest
{
    private static Sgp4Propagator Iss => Sgp4Propagator.Create(
        TwoLineElement.Parse(OrbitalElementsReadingTest.IssLine1, OrbitalElementsReadingTest.IssLine2, "ISS"));

    private static string Kind(Core.Spatial.Primitives.Feature<Point> f) => (string)f.Attributes[SatellitesDataSource.KindFieldName];

    [Fact]
    public void SelectedSatellite_GetsTrackFootprintAndPosition_InDrawingOrder()
    {
        var iss = Iss;

        var source = new SatellitesDataSource { Satellites = new[] { iss }, Selected = iss, Time = iss.Elements.Epoch };

        var kinds = source.GetFeatures().Select(Kind).ToList();

        // no swath: the ISS is not in the sensor catalog
        Assert.DoesNotContain(SatellitesDataSource.SwathKind, kinds);
        Assert.Contains(SatellitesDataSource.FootprintKind, kinds);
        Assert.Contains(SatellitesDataSource.PastTrackKind, kinds);
        Assert.Contains(SatellitesDataSource.FutureTrackKind, kinds);
        Assert.Equal(SatellitesDataSource.SelectedKind, kinds[^1]);
        Assert.Single(kinds, k => k == SatellitesDataSource.SelectedKind);

        // footprint under the tracks, tracks under the point
        Assert.True(kinds.LastIndexOf(SatellitesDataSource.FootprintKind) < kinds.IndexOf(SatellitesDataSource.PastTrackKind));
    }

    [Fact]
    public void Swath_AppearsForSatellitesWithAKnownSensor()
    {
        var iss = Iss;

        var sensors = SensorCatalog.CreateDefault();
        sensors.Set(25544, new SensorInfo("Test camera", 100));

        var source = new SatellitesDataSource { Satellites = new[] { iss }, Selected = iss, Time = iss.Elements.Epoch, Sensors = sensors };

        var features = source.GetFeatures();

        var swath = features.Where(f => Kind(f) == SatellitesDataSource.SwathKind).ToList();

        Assert.NotEmpty(swath);
        Assert.All(swath, f => Assert.Equal(GeometryType.Polygon, f.TheGeometry.Type));
        Assert.Equal(SatellitesDataSource.SwathKind, Kind(features[0]));

        source.ShowSwath = false;

        Assert.DoesNotContain(source.GetFeatures(), f => Kind(f) == SatellitesDataSource.SwathKind);
    }

    [Fact]
    public void Footprint_SurroundsThePointBeneathTheSatellite()
    {
        var iss = Iss;

        var source = new SatellitesDataSource { Satellites = new[] { iss }, Selected = iss, Time = iss.Elements.Epoch.AddMinutes(17) };

        var features = source.GetFeatures();

        var point = features.Single(f => Kind(f) == SatellitesDataSource.SelectedKind).TheGeometry.Points[0];

        Assert.Contains(features.Where(f => Kind(f) == SatellitesDataSource.FootprintKind),
            f => SphericalCapHelperTest.IsInside(f.TheGeometry.Geometries[0].Points, point.X, point.Y));
    }

    [Fact]
    public void Options_SwitchOverlaysOff_AndOnlyTheSelectedSatelliteGetsThem()
    {
        var iss = Iss;

        var other = Sgp4Propagator.Create(TwoLineElement.Parse(
            OrbitalElementsReadingTest.WithChecksum(OrbitalElementsReadingTest.IssLine1.Replace("1 25544U", "1 25545U")),
            OrbitalElementsReadingTest.WithChecksum(OrbitalElementsReadingTest.IssLine2.Replace("2 25544", "2 25545").Replace("325.0288", "145.0288")),
            "OTHER"));

        var source = new SatellitesDataSource
        {
            Satellites = new[] { iss, other },
            Selected = iss,
            Time = iss.Elements.Epoch,
            ShowTrack = false,
            ShowFootprint = false,
        };

        var kinds = source.GetFeatures().Select(Kind).ToList();

        Assert.Equal(new[] { SatellitesDataSource.OtherKind, SatellitesDataSource.SelectedKind }, kinds);

        source.Selected = null;
        source.ShowTrack = true;
        source.ShowFootprint = true;

        Assert.All(source.GetFeatures(), f => Assert.Equal(SatellitesDataSource.OtherKind, Kind(f)));
    }

    [Fact]
    public void TrackLength_FollowsTheOrbitOptions()
    {
        var iss = Iss;

        var source = new SatellitesDataSource { Satellites = new[] { iss }, Selected = iss, Time = iss.Elements.Epoch, PastOrbits = 0, FutureOrbits = 0.25 };

        var features = source.GetFeatures();

        Assert.DoesNotContain(features, f => Kind(f) == SatellitesDataSource.PastTrackKind);

        var future = features.Where(f => Kind(f) == SatellitesDataSource.FutureTrackKind).ToList();

        // a quarter orbit of the ISS is about 23 minutes, 10 000 km along the ground
        var length = future.Sum(f =>
        {
            var points = f.TheGeometry.Points.Select(p => MapProjects.WebMercatorToGeodeticWgs84(p)).ToList();

            return points.Zip(points.Skip(1), GreatCircle.DistanceKm).Sum();
        });

        Assert.InRange(length, 9000, 11000);
    }
}
