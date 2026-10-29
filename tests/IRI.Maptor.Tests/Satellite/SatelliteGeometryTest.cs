using System.Linq;
using IRI.Maptor.Core.Astronomy.Time;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Satellite;
using IRI.Maptor.Core.Satellite.Elements;
using IRI.Maptor.Core.Satellite.Frames;
using IRI.Maptor.Core.Satellite.Propagation;

namespace IRI.Maptor.Tests.Satellite;

public class SatelliteGeometryTest
{
    private static OrbitalElements Iss => TwoLineElement.Parse(OrbitalElementsReadingTest.IssLine1, OrbitalElementsReadingTest.IssLine2, "ISS");

    [Fact]
    public void Wgs84_KnownPoints()
    {
        var equator = Wgs84Geodetic.ToEcef(new Point(0, 0), 0);
        Assert.Equal(6378.137, equator.X, 9);
        Assert.Equal(0, equator.Y, 9);
        Assert.Equal(0, equator.Z, 9);

        var pole = Wgs84Geodetic.ToEcef(new Point(0, 90), 0);
        Assert.Equal(0, pole.X, 9);
        Assert.Equal(6356.752314245, pole.Z, 6);

        var lon90 = Wgs84Geodetic.ToEcef(new Point(90, 0), 100);
        Assert.Equal(6478.137, lon90.Y, 9);
    }

    [Theory]
    [InlineData(51.389, 35.689, 1.2)]
    [InlineData(-179.9, -33.87, 420)]
    [InlineData(0, 89.9999, 800)]
    [InlineData(10, -90, 35786)]
    [InlineData(-70, 0, -0.5)]
    public void Wgs84_RoundTrips(double longitude, double latitude, double heightKm)
    {
        var ecef = Wgs84Geodetic.ToEcef(new Point(longitude, latitude), heightKm);

        var back = Wgs84Geodetic.FromEcef(ecef, out var height);

        Assert.Equal(latitude, back.Y, 9);
        Assert.Equal(heightKm, height, 7);

        if (Math.Abs(latitude) < 90)
            Assert.Equal(longitude, back.X, 9);
    }

    [Fact]
    public void TemeToEcef_PutsTheDirectionOfGreenwichSiderealTimeOnTheGreenwichMeridian()
    {
        var utc = new DateTime(2026, 10, 28, 9, 30, 0, DateTimeKind.Utc);

        var theta = SiderealTime.GreenwichMean(utc) * Math.PI / 180;

        var teme = new Vector3(7000 * Math.Cos(theta), 7000 * Math.Sin(theta), 1000);

        var ecef = EarthFrames.TemeToEcef(teme, utc);

        Assert.Equal(7000, ecef.X, 6);
        Assert.Equal(0, ecef.Y, 6);
        Assert.Equal(1000, ecef.Z, 9);

        var back = EarthFrames.EcefToTeme(ecef, utc);

        Assert.Equal(teme.X, back.X, 6);
        Assert.Equal(teme.Y, back.Y, 6);
    }

    [Fact]
    public void GroundVelocity_OfAPointFixedToTheEarth_IsZero()
    {
        var utc = new DateTime(2026, 10, 28, 9, 30, 0, DateTimeKind.Utc);

        var ecef = Wgs84Geodetic.ToEcef(new Point(51.4, 35.7), 0);

        var teme = EarthFrames.EcefToTeme(ecef, utc);

        // a place on the ground moves with the Earth: ω × r in the inertial frame
        var inertialVelocity = new Vector3(-EarthFrames.EarthRotationRate * teme.Y, EarthFrames.EarthRotationRate * teme.X, 0);

        var ground = EarthFrames.TemeVelocityToEcef(teme, inertialVelocity, utc);

        Assert.True(ground.Length < 1e-9, $"ground velocity {ground.Length} km/s");
    }

    [Fact]
    public void Iss_FliesWhereTheIssFlies()
    {
        var propagator = Sgp4Propagator.Create(Iss);

        Assert.InRange(propagator.PeriodMinutes, 91.0, 92.0);

        var positions = Enumerable.Range(0, 2 * 92 * 2)
            .Select(i => propagator.GetPosition(Iss.Epoch.AddSeconds(30 * i)))
            .ToList();

        Assert.All(positions, p =>
        {
            Assert.NotNull(p);
            Assert.InRange(p!.AltitudeKm, 320, 380);
            Assert.InRange(p.SpeedKmS, 7.6, 7.8);
            Assert.InRange(p.SubSatellitePoint.X, -180, 180);
        });

        // the ground track reaches the inclination (geodetic latitude runs a little above the geocentric 51.64°)
        var highest = positions.Max(p => Math.Abs(p!.SubSatellitePoint.Y));

        Assert.InRange(highest, 51.5, 52.0);
    }

    [Fact]
    public void Propagate_AtTheEpoch_EqualsPropagateMinutesZero()
    {
        var propagator = Sgp4Propagator.Create(Iss);

        var byTime = propagator.Propagate(Iss.Epoch);

        var byMinutes = propagator.PropagateMinutes(0);

        Assert.Equal(byMinutes.Position.X, byTime.Position.X, 9);
        Assert.Equal(0, byTime.MinutesSinceEpoch, 9);
        Assert.Equal(Sgp4Status.Ok, byTime.Status);
    }
}
