using IRI.Maptor.Core.Astronomy.Solar;
using IRI.Maptor.Core.Astronomy.Time;
using IRI.Maptor.Core.Common.Primitives;

namespace IRI.Maptor.Tests.Astronomy;

/// <summary>
/// Sun position against Meeus's worked examples, and daily events against the U.S. Naval Observatory
/// (aa.usno.navy.mil, rise/set/transit API), which publishes times rounded to the minute.
/// </summary>
public class SunTest
{
    private static readonly Point Tehran = new Point(51.3890, 35.6892);

    private static readonly Point Sydney = new Point(151.2093, -33.8688);

    private static readonly Point Tromso = new Point(18.9553, 69.6492);

    private static readonly TimeSpan IranStandardTime = new TimeSpan(3, 30, 0);

    private static void AssertNearMinute(DateTime date, TimeSpan utcOffset, int hour, int minute, DateTimeOffset? actual)
    {
        Assert.NotNull(actual);

        var expected = new DateTimeOffset(date.Year, date.Month, date.Day, hour, minute, 0, utcOffset);

        // A minute-rounded reference is ±30 s; allow the same again for the two algorithms' differences
        Assert.InRange(Math.Abs((actual!.Value - expected).TotalSeconds), 0, 60);

        Assert.Equal(utcOffset, actual.Value.Offset);
    }

    private static double SignedLongitude(double degrees) => degrees >= 180 ? degrees - 360 : degrees;

    [Fact]
    public void Position_MatchesMeeusExample25a()
    {
        // 1992 October 13.0 TD; the API takes UTC, so step back by ΔT
        var utc = new DateTime(1992, 10, 13, 0, 0, 0, DateTimeKind.Utc).AddSeconds(-AstroTime.DeltaT(new DateTime(1992, 10, 13)));

        var position = Sun.GetPosition(utc);

        // Meeus prints five decimals, so allow a unit in the last place
        Assert.InRange(position.JulianEphemerisDay - 2448908.5, -1e-6, 1e-6);
        Assert.InRange(position.ApparentLongitude - 199.90895, -1e-5, 1e-5);
        Assert.InRange(position.RightAscension - 198.38083, -1e-5, 1e-5);
        Assert.InRange(position.Declination - -7.78507, -1e-5, 1e-5);
        Assert.InRange(position.DistanceAu - 0.99766, -1e-5, 1e-5);
    }

    [Fact]
    public void EquationOfTime_MatchesMeeusExample28b()
    {
        // 1992 October 13.0 TD → +13m42.7s
        var utc = new DateTime(1992, 10, 13, 0, 0, 0, DateTimeKind.Utc).AddSeconds(-AstroTime.DeltaT(new DateTime(1992, 10, 13)));

        Assert.InRange(Sun.GetPosition(utc).EquationOfTime.TotalMinutes, 13.70, 13.72);
    }

    [Theory]
    // USNO equinoxes and solstices 2026 (UTC, rounded to the minute; the Sun moves 0.0007° a minute)
    [InlineData(2026, 3, 20, 14, 46, 0)]
    [InlineData(2026, 6, 21, 8, 24, 90)]
    [InlineData(2026, 9, 23, 0, 5, 180)]
    [InlineData(2026, 12, 21, 20, 50, 270)]
    public void ApparentLongitude_AtEquinoxesAndSolstices_IsAMultipleOf90(int year, int month, int day, int hour, int minute, double expected)
    {
        var position = Sun.GetPosition(new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Utc));

        Assert.InRange(SignedLongitude(position.ApparentLongitude - expected + 360), -0.01, 0.01);
    }

    [Fact]
    public void Declination_AtTheMarchEquinox_IsZero()
    {
        Assert.InRange(Sun.GetPosition(new DateTime(2026, 3, 20, 14, 46, 0, DateTimeKind.Utc)).Declination, -0.005, 0.005);
    }

    [Theory]
    [InlineData(2026, 1, 5, 3, 0)]
    [InlineData(2026, 3, 20, 14, 46)]
    [InlineData(2026, 7, 15, 22, 30)]
    [InlineData(2026, 10, 28, 9, 15)]
    public void SubSolarPoint_HasTheSunAtTheZenith(int year, int month, int day, int hour, int minute)
    {
        var utc = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Utc);

        var subSolar = Sun.GetSubSolarPoint(utc);

        var antipode = new Point(SignedLongitude(subSolar.X + 180 + 360) , -subSolar.Y);

        Assert.InRange(subSolar.X, -180, 180);
        Assert.True(Sun.GetHorizontal(subSolar, utc).Elevation > 89.99);
        Assert.True(Sun.GetHorizontal(antipode, utc).Elevation < -89.99);
    }

    [Fact]
    public void SolarDay_Tehran_JuneSolstice_MatchesUsno()
    {
        var date = new DateTime(2026, 6, 21);

        var day = Sun.GetSolarDay(Tehran, date, IranStandardTime);

        AssertNearMinute(date, IranStandardTime, 4, 19, day.CivilTwilight.Rising);
        AssertNearMinute(date, IranStandardTime, 4, 49, day.Sunrise);
        AssertNearMinute(date, IranStandardTime, 12, 6, day.SolarNoon);
        AssertNearMinute(date, IranStandardTime, 19, 24, day.Sunset);
        AssertNearMinute(date, IranStandardTime, 19, 54, day.CivilTwilight.Setting);

        Assert.False(day.IsPolarDay);
        Assert.False(day.IsPolarNight);
        Assert.Equal(day.Sunset!.Value - day.Sunrise!.Value, day.DayLength);
    }

    [Fact]
    public void SolarDay_Tehran_JuneSolstice_HasTheExpectedGeometry()
    {
        var day = Sun.GetSolarDay(Tehran, new DateTime(2026, 6, 21), IranStandardTime);

        // 90° − latitude + declination at the solstice
        Assert.Equal(90 - 35.6892 + 23.436, day.SolarNoonElevation, 1);
        Assert.InRange(Sun.GetHorizontal(Tehran, day.SolarNoon.UtcDateTime).Azimuth, 179.9, 180.1);

        // North-east sunrise and north-west sunset, mirrored about the meridian
        Assert.InRange(day.SunriseAzimuth!.Value, 59.5, 61.5);
        Assert.Equal(360 - day.SunriseAzimuth.Value, day.SunsetAzimuth!.Value, 0);

        // The twilights nest inside one another
        Assert.True(day.AstronomicalTwilight.Rising < day.NauticalTwilight.Rising);
        Assert.True(day.NauticalTwilight.Rising < day.CivilTwilight.Rising);
        Assert.True(day.CivilTwilight.Rising < day.Sunrise);
        Assert.True(day.Sunset < day.CivilTwilight.Setting);
        Assert.True(day.CivilTwilight.Setting < day.NauticalTwilight.Setting);
        Assert.True(day.NauticalTwilight.Setting < day.AstronomicalTwilight.Setting);
    }

    [Fact]
    public void SolarDay_Sydney_DecemberSolstice_MatchesUsno()
    {
        var date = new DateTime(2026, 12, 21);

        var offset = TimeSpan.FromHours(10);

        var day = Sun.GetSolarDay(Sydney, date, offset);

        AssertNearMinute(date, offset, 4, 11, day.CivilTwilight.Rising);
        AssertNearMinute(date, offset, 4, 41, day.Sunrise);
        AssertNearMinute(date, offset, 11, 53, day.SolarNoon);
        AssertNearMinute(date, offset, 19, 5, day.Sunset);
        AssertNearMinute(date, offset, 19, 35, day.CivilTwilight.Setting);

        // Southern hemisphere: the noon Sun stands to the north
        var azimuth = Sun.GetHorizontal(Sydney, day.SolarNoon.UtcDateTime).Azimuth;

        Assert.True(azimuth < 0.1 || azimuth > 359.9);
    }

    [Fact]
    public void SolarDay_Tromso_DecemberSolstice_IsPolarNightWithCivilTwilight()
    {
        var date = new DateTime(2026, 12, 21);

        var offset = TimeSpan.FromHours(1);

        var day = Sun.GetSolarDay(Tromso, date, offset);

        Assert.True(day.IsPolarNight);
        Assert.False(day.IsPolarDay);
        Assert.Null(day.Sunrise);
        Assert.Null(day.Sunset);
        Assert.Null(day.SunriseAzimuth);
        Assert.Equal(TimeSpan.Zero, day.DayLength);

        // USNO: "continuously below the horizon", civil twilight 09:31–13:53
        AssertNearMinute(date, offset, 9, 31, day.CivilTwilight.Rising);
        AssertNearMinute(date, offset, 13, 53, day.CivilTwilight.Setting);
    }

    [Fact]
    public void SolarDay_Tromso_JuneSolstice_IsPolarDay()
    {
        var date = new DateTime(2026, 6, 21);

        var offset = TimeSpan.FromHours(1);

        var day = Sun.GetSolarDay(Tromso, date, offset);

        // USNO: continuously above the horizon and above the twilight limit, upper transit 11:46
        Assert.True(day.IsPolarDay);
        Assert.False(day.IsPolarNight);
        Assert.Null(day.Sunrise);
        Assert.Null(day.Sunset);
        Assert.Equal(TimeSpan.FromDays(1), day.DayLength);
        Assert.True(day.CivilTwilight.IsAlwaysAbove);
        AssertNearMinute(date, offset, 11, 46, day.SolarNoon);
    }

    [Theory]
    // Tehran; Kiritimati, whose +14 h offset puts local noon on a different UTC date from the longitude's
    [InlineData(51.3890, 35.6892, 210)]
    [InlineData(-157.4, 1.87, 840)]
    [InlineData(172.6, -43.5, 780)]
    // An offset far west of the meridian: the first guess lands on the previous local date
    [InlineData(175.0, 0.0, -600)]
    public void SolarDay_SolarNoon_FallsOnTheRequestedLocalDate(double longitude, double latitude, int offsetMinutes)
    {
        var date = new DateTime(2026, 10, 28);

        var day = Sun.GetSolarDay(new Point(longitude, latitude), date, TimeSpan.FromMinutes(offsetMinutes));

        Assert.Equal(date, day.SolarNoon.Date);
        Assert.Equal(date, day.Date);
    }

    [Fact]
    public void SolarDay_WithTimeZone_UsesTheZonesOffset()
    {
        var zone = TimeZoneInfo.CreateCustomTimeZone("Test Iran", IranStandardTime, "Test Iran", "Test Iran");

        var date = new DateTime(2026, 6, 21);

        Assert.Equal(Sun.GetSolarDay(Tehran, date, IranStandardTime).Sunrise, Sun.GetSolarDay(Tehran, date, zone).Sunrise);
    }

    [Fact]
    public void AltitudeEvents_ForACustomElevation_AgreeWithTheSolarDay()
    {
        var date = new DateTime(2026, 6, 21);

        var civil = Sun.GetAltitudeEvents(Tehran, date, IranStandardTime, SunAltitudes.CivilTwilight);

        var day = Sun.GetSolarDay(Tehran, date, IranStandardTime);

        Assert.Equal(day.CivilTwilight.Rising, civil.Rising);
        Assert.Equal(day.CivilTwilight.Setting, civil.Setting);
    }

    [Theory]
    [InlineData(45.0, SolarPhase.Day)]
    [InlineData(-0.8333, SolarPhase.Day)]
    [InlineData(-0.84, SolarPhase.CivilTwilight)]
    [InlineData(-6.0, SolarPhase.CivilTwilight)]
    [InlineData(-6.01, SolarPhase.NauticalTwilight)]
    [InlineData(-12.0, SolarPhase.NauticalTwilight)]
    [InlineData(-12.01, SolarPhase.AstronomicalTwilight)]
    [InlineData(-18.0, SolarPhase.AstronomicalTwilight)]
    [InlineData(-18.01, SolarPhase.Night)]
    [InlineData(-90.0, SolarPhase.Night)]
    public void SolarPhase_FollowsTheStandardElevations(double elevation, SolarPhase expected)
    {
        Assert.Equal(expected, SolarPhases.FromElevation(elevation));
    }

    [Fact]
    public void SolarPhase_AtTehranAcrossTheSolstice_MatchesTheSolarDay()
    {
        var day = Sun.GetSolarDay(Tehran, new DateTime(2026, 6, 21), IranStandardTime);

        Assert.Equal(SolarPhase.Day, Sun.GetPhase(Tehran, day.SolarNoon.UtcDateTime));

        // Ten minutes after sunset: civil twilight; ten minutes after civil dusk: nautical
        Assert.Equal(SolarPhase.CivilTwilight, Sun.GetPhase(Tehran, day.Sunset!.Value.UtcDateTime.AddMinutes(10)));
        Assert.Equal(SolarPhase.NauticalTwilight, Sun.GetPhase(Tehran, day.CivilTwilight.Setting!.Value.UtcDateTime.AddMinutes(10)));
        Assert.Equal(SolarPhase.AstronomicalTwilight, Sun.GetPhase(Tehran, day.NauticalTwilight.Setting!.Value.UtcDateTime.AddMinutes(10)));
        Assert.Equal(SolarPhase.Night, Sun.GetPhase(Tehran, day.AstronomicalTwilight.Setting!.Value.UtcDateTime.AddMinutes(10)));
    }

    [Fact]
    public void SolarPhase_RejectsNaN()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SolarPhases.FromElevation(double.NaN));
    }

    [Fact]
    public void InvalidInput_Throws()
    {
        var date = new DateTime(2026, 6, 21);

        Assert.Throws<ArgumentOutOfRangeException>(() => Sun.GetSolarDay(new Point(51, 95), date, IranStandardTime));
        Assert.Throws<ArgumentOutOfRangeException>(() => Sun.GetSolarDay(Tehran, date, new TimeSpan(3, 30, 30)));
        Assert.Throws<ArgumentOutOfRangeException>(() => Sun.GetSolarDay(Tehran, date, TimeSpan.FromHours(15)));
        Assert.Throws<ArgumentNullException>(() => Sun.GetHorizontal(null!, date));
    }
}
