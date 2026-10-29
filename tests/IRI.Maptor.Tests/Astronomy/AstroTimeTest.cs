using IRI.Maptor.Core.Astronomy.Events;
using IRI.Maptor.Core.Astronomy.Time;

namespace IRI.Maptor.Tests.Astronomy;

/// <summary>
/// Time scales and sidereal time, checked against the worked examples in Meeus, <i>Astronomical Algorithms</i> (2nd ed.).
/// </summary>
public class AstroTimeTest
{
    private static double HoursMinutesSecondsToDegrees(int hours, int minutes, double seconds) => (hours + minutes / 60.0 + seconds / 3600.0) * 15.0;

    [Fact]
    public void JulianDate_OfJ2000Epoch_IsExact()
    {
        Assert.Equal(AstroTime.J2000, AstroTime.ToJulianDate(new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc)));
    }

    [Fact]
    public void JulianDate_MatchesMeeusExample7a()
    {
        // Sputnik 1 launch, 1957 October 4.81
        var time = new DateTime(1957, 10, 4, 0, 0, 0, DateTimeKind.Utc).AddDays(0.81);

        Assert.Equal(2436116.31, AstroTime.ToJulianDate(time), 6);
    }

    [Fact]
    public void JulianDate_RoundTripsThroughDateTime()
    {
        var time = new DateTime(2026, 10, 28, 13, 47, 12, 345, DateTimeKind.Utc);

        Assert.Equal(time, AstroTime.FromJulianDate(AstroTime.ToJulianDate(time)), TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public void JulianDate_TreatsUnspecifiedKindAsUtc()
    {
        var utc = new DateTime(2026, 3, 20, 14, 46, 0, DateTimeKind.Utc);

        Assert.Equal(AstroTime.ToJulianDate(utc), AstroTime.ToJulianDate(DateTime.SpecifyKind(utc, DateTimeKind.Unspecified)));
    }

    [Fact]
    public void DeltaT_AtYear2000_IsAboutSixtyFourSeconds()
    {
        // Observed value for 2000.0 is 63.83 s
        Assert.InRange(AstroTime.DeltaT(new DateTime(2000, 1, 1)), 63.3, 64.3);
    }

    [Fact]
    public void GreenwichMeanSiderealTime_MatchesMeeusExample12a()
    {
        // 1987 April 10, 0h UT → 13h10m46.3668s
        var sidereal = SiderealTime.GreenwichMean(new DateTime(1987, 4, 10, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(HoursMinutesSecondsToDegrees(13, 10, 46.3668), sidereal, 5);
    }

    [Fact]
    public void GreenwichMeanSiderealTime_MatchesMeeusExample12b()
    {
        // 1987 April 10, 19h21m00s UT → 8h34m57.0896s
        var sidereal = SiderealTime.GreenwichMean(new DateTime(1987, 4, 10, 19, 21, 0, DateTimeKind.Utc));

        Assert.Equal(HoursMinutesSecondsToDegrees(8, 34, 57.0896), sidereal, 5);
    }

    [Fact]
    public void GreenwichApparentSiderealTime_MatchesMeeusExample12a()
    {
        // 13h10m46.1351s; the low-precision nutation is good to about 0.05 s of time here
        var sidereal = SiderealTime.GreenwichApparent(new DateTime(1987, 4, 10, 0, 0, 0, DateTimeKind.Utc));

        Assert.InRange(sidereal - HoursMinutesSecondsToDegrees(13, 10, 46.1351), -0.0003, 0.0003);
    }

    [Fact]
    public void CrossingSolver_FindsTheCrossingOfAMonotonicValue()
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        double Value(DateTime t) => (t - start).TotalHours;

        var crossing = CrossingSolver.FindCrossing(Value, start, start.AddHours(10), 7.25, TimeSpan.FromSeconds(1));

        Assert.NotNull(crossing);
        Assert.Equal(start.AddHours(7.25), crossing!.Value, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void CrossingSolver_ReturnsNullWhenTheThresholdIsNotCrossed()
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        double Value(DateTime t) => (t - start).TotalHours;

        Assert.Null(CrossingSolver.FindCrossing(Value, start, start.AddHours(10), 12, TimeSpan.FromSeconds(1)));
    }
}
