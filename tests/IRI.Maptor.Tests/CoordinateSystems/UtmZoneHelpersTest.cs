using System;

using IRI.Maptor.Core.SpatialReferenceSystem;

using Xunit;

namespace IRI.Maptor.Tests.CoordinateSystems;

/// <summary>
/// <see cref="MapProjects.FindUtmZone"/> and <see cref="MapProjects.CalculateCentralMeridian"/>.
/// </summary>
/// <remarks>
/// Both were rewritten in 2026-08 while adding MGRS, which needs them to be exact.
/// <c>FindUtmZone</c> used to compute <c>30 + ceil(lon / 6)</c> and so returned the zone to the
/// west at every exact multiple of six; <c>CalculateCentralMeridian</c> used to report the western
/// zones as 183–357 rather than as negative degrees, which is equivalent modulo 360 but left the
/// transverse Mercator formulas with a 360-degree longitude difference and showed up as "183° E"
/// in the Go To dialog's zone hint.
/// </remarks>
public class UtmZoneHelpersTest
{
    #region FindUtmZone

    /// <summary>
    /// Zones are six degrees wide and half-open, <c>[6n - 186, 6n - 180)</c>, so a longitude
    /// sitting exactly on a boundary belongs to the zone to its east.
    /// </summary>
    [Theory]
    [InlineData(-180.0, 1)]
    [InlineData(-177.0, 1)]     // zone 1's central meridian
    [InlineData(-174.0, 2)]
    [InlineData(-6.0, 30)]
    [InlineData(-0.001, 30)]
    [InlineData(0.0, 31)]       // used to answer 30
    [InlineData(3.0, 31)]
    [InlineData(5.999, 31)]
    [InlineData(6.0, 32)]       // used to answer 31
    [InlineData(12.0, 33)]
    [InlineData(48.0, 39)]      // used to answer 38
    [InlineData(51.0, 39)]
    [InlineData(53.999, 39)]
    [InlineData(54.0, 40)]
    [InlineData(174.0, 60)]
    [InlineData(179.999, 60)]
    public void FindUtmZone_OnAndAroundTheBoundaries(double longitude, int expected)
    {
        Assert.Equal(expected, MapProjects.FindUtmZone(longitude));
    }

    /// <summary>
    /// Callers pass central meridians as well as positions, and
    /// <see cref="MapProjects.CalculateCentralMeridian"/> used to hand back 183–357 for the
    /// western zones — values that are still in the wild in saved projections. Any turn-shifted
    /// form has to resolve to the same zone.
    /// </summary>
    [Theory]
    [InlineData(183.0, 1)]      // -177 expressed the old way
    [InlineData(-177.0, 1)]
    [InlineData(357.0, 30)]     // -3
    [InlineData(-3.0, 30)]
    [InlineData(411.0, 39)]     // 51 plus a full turn
    [InlineData(-309.0, 39)]    // 51 minus a full turn
    public void FindUtmZone_TurnShiftedLongitudes_ResolveTheSame(double longitude, int expected)
    {
        Assert.Equal(expected, MapProjects.FindUtmZone(longitude));
    }

    /// <summary>
    /// The antimeridian is one meridian written two ways. 180 normalizes to -180, and by the same
    /// half-open rule as every other boundary that puts it in the zone to its east — zone 1, not
    /// zone 60.
    /// </summary>
    [Fact]
    public void FindUtmZone_AtTheAntimeridian_IsZoneOneFromEitherSide()
    {
        Assert.Equal(MapProjects.FindUtmZone(-180.0), MapProjects.FindUtmZone(180.0));
        Assert.Equal(1, MapProjects.FindUtmZone(180.0));
    }

    /// <summary>
    /// A longitude that is not a finite number is rejected. This used to be worse than a wrong
    /// answer: the old implementation subtracted 360 in a loop until the value came into range,
    /// which never terminates for an infinity.
    /// </summary>
    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void FindUtmZone_NonFiniteLongitude_ThrowsRatherThanHanging(double longitude)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MapProjects.FindUtmZone(longitude));
    }

    /// <summary>Every longitude in the world lands on a real zone.</summary>
    [Fact]
    public void FindUtmZone_SweptWorldwide_AlwaysReturnsOneToSixty()
    {
        for (var longitude = -180.0; longitude <= 180.0; longitude += 0.25)
        {
            var zone = MapProjects.FindUtmZone(longitude);

            Assert.InRange(zone, 1, 60);
        }
    }

    #endregion

    #region CalculateCentralMeridian

    [Theory]
    [InlineData(1, -177)]
    [InlineData(29, -9)]
    [InlineData(30, -3)]
    [InlineData(31, 3)]
    [InlineData(32, 9)]         // the Norway zone
    [InlineData(38, 45)]
    [InlineData(39, 51)]        // Tehran
    [InlineData(41, 63)]
    [InlineData(60, 177)]
    public void CalculateCentralMeridian_IsSignedDegrees(int zone, int expected)
    {
        Assert.Equal(expected, MapProjects.CalculateCentralMeridian(zone));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(61)]
    [InlineData(-1)]
    public void CalculateCentralMeridian_OutsideOneToSixty_Throws(int zone)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MapProjects.CalculateCentralMeridian(zone));
    }

    /// <summary>
    /// The two are inverses: a zone's central meridian is inside that zone, and every central
    /// meridian is a multiple of six offset by three.
    /// </summary>
    [Fact]
    public void CentralMeridianOfEveryZone_FindsItsOwnZoneBack()
    {
        for (var zone = 1; zone <= 60; zone++)
        {
            var centralMeridian = MapProjects.CalculateCentralMeridian(zone);

            Assert.InRange(centralMeridian, -177, 177);
            Assert.Equal(3, Math.Abs(centralMeridian) % 6);
            Assert.Equal(zone, MapProjects.FindUtmZone(centralMeridian));
        }
    }

    #endregion

    #region NormalizeLongitude

    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(51.0, 51.0)]
    [InlineData(-177.0, -177.0)]
    [InlineData(183.0, -177.0)]
    [InlineData(360.0, 0.0)]
    [InlineData(-360.0, 0.0)]
    [InlineData(180.0, -180.0)]
    [InlineData(-180.0, -180.0)]
    public void NormalizeLongitude_MapsOntoTheHalfOpenRange(double longitude, double expected)
    {
        Assert.Equal(expected, MapProjects.NormalizeLongitude(longitude), 9);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void NormalizeLongitude_NonFinite_IsNaN(double longitude)
    {
        Assert.True(double.IsNaN(MapProjects.NormalizeLongitude(longitude)));
    }

    #endregion
}
