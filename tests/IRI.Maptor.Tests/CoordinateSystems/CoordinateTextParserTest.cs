using IRI.Maptor.Core.Common.Helpers;

namespace IRI.Maptor.Tests.CoordinateSystems;

public class CoordinateTextParserTest
{

    private static void AssertClose(double expected, double actual, double tolerance)
        => Assert.InRange(actual, expected - tolerance, expected + tolerance);
    private const double Tolerance = 1e-6;

    [Theory]
    [InlineData("35.6892, 51.3890", 35.6892, 51.3890)]
    [InlineData("35.6892 51.3890", 35.6892, 51.3890)]
    [InlineData("35.6892;51.3890", 35.6892, 51.3890)]
    [InlineData("35.6892\t51.3890", 35.6892, 51.3890)]
    [InlineData("-33.8688, 151.2093", -33.8688, 151.2093)]
    [InlineData("35.6892N 51.3890E", 35.6892, 51.3890)]
    [InlineData("35.6892 N, 51.3890 E", 35.6892, 51.3890)]
    [InlineData("51.3890E 35.6892N", 35.6892, 51.3890)]          // hemisphere letters fix the order
    [InlineData("N35.6892 E51.3890", 35.6892, 51.3890)]
    [InlineData("151.2093, -33.8688", -33.8688, 151.2093)]        // |first| > 90 → must be the longitude
    [InlineData("lat: 35.6892, lon: 51.3890", 35.6892, 51.3890)]
    [InlineData("Latitude 35.6892 Longitude 51.3890", 35.6892, 51.3890)]
    public void ParsesDecimalPairs(string text, double lat, double lon)
    {
        Assert.True(CoordinateTextParser.TryParseLatLong(text, out var latitude, out var longitude), text);
        AssertClose(lat, latitude, Tolerance);
        AssertClose(lon, longitude, Tolerance);
    }

    [Theory]
    [InlineData("35°41'21.12\"N 51°23'20.4\"E", 35.6892, 51.389)]
    [InlineData("35°41′21.12″N, 51°23′20.4″E", 35.6892, 51.389)]      // typographic marks
    [InlineData("35° 41' 21.12\" N 51° 23' 20.4\" E", 35.6892, 51.389)]
    [InlineData("35 41 21.12 N 51 23 20.4 E", 35.6892, 51.389)]
    [InlineData("35 41 21.12 51 23 20.4", 35.6892, 51.389)]            // six bare numbers
    [InlineData("35 41.352 51 23.34", 35.6892, 51.389)]                // four bare numbers: deg + decimal min
    [InlineData("N35 41.352 E51 23.340", 35.6892, 51.389)]
    [InlineData("35°41'21.12\"S 51°23'20.4\"W", -35.6892, -51.389)]
    [InlineData("S 35 41 21.12, W 51 23 20.4", -35.6892, -51.389)]
    [InlineData("35°41'21.12\"N, 51°23'20.4\"E", 35.6892, 51.389)]
    public void ParsesDmsPairs(string text, double lat, double lon)
    {
        Assert.True(CoordinateTextParser.TryParseLatLong(text, out var latitude, out var longitude), text);
        AssertClose(lat, latitude, 1e-4);
        AssertClose(lon, longitude, 1e-4);
    }

    [Theory]
    [InlineData("۳۵٫۶۸۹۲، ۵۱٫۳۸۹۰", 35.6892, 51.3890)]             // Persian digits, Persian decimal and comma
    [InlineData("٣٥.٦٨٩٢, ٥١.٣٨٩٠", 35.6892, 51.3890)]             // Arabic-Indic digits
    [InlineData("−33.8688, 151.2093", -33.8688, 151.2093)]          // unicode minus
    public void ParsesNonLatinDigits(string text, double lat, double lon)
    {
        Assert.True(CoordinateTextParser.TryParseLatLong(text, out var latitude, out var longitude), text);
        AssertClose(lat, latitude, Tolerance);
        AssertClose(lon, longitude, Tolerance);
    }

    [Theory]
    [InlineData("https://www.google.com/maps/@35.6892,51.389,15z", 35.6892, 51.389)]
    [InlineData("https://www.google.com/maps/place/Tehran/@35.6892523,51.3890004,12z/data=!3m1", 35.6892523, 51.3890004)]
    [InlineData("https://maps.google.com/?q=35.6892,51.389", 35.6892, 51.389)]
    [InlineData("https://www.google.com/maps?ll=35.6892,51.389&z=12", 35.6892, 51.389)]
    [InlineData("https://www.openstreetmap.org/#map=15/35.6892/51.3890", 35.6892, 51.389)]
    [InlineData("https://www.openstreetmap.org/?mlat=35.6892&mlon=51.389#map=15/35.6892/51.389", 35.6892, 51.389)]
    public void ParsesMapLinks(string text, double lat, double lon)
    {
        Assert.True(CoordinateTextParser.TryParseLatLong(text, out var latitude, out var longitude), text);
        AssertClose(lat, latitude, Tolerance);
        AssertClose(lon, longitude, Tolerance);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("hello")]
    [InlineData("35.6892")]                  // one number is not a pair
    [InlineData("35.6892, 51.3890, 12")]     // three numbers
    [InlineData("95, 51")]                   // latitude out of range, and 51 cannot be the latitude either way? (51 can) → swapped reading: lat 51, lon 95 is valid
    [InlineData("35N 51N")]                  // two latitudes
    [InlineData("35 61 10 N 51 23 20 E")]    // minutes ≥ 60
    [InlineData("35.5 30 N 51 23 E")]        // fractional degrees followed by minutes
    [InlineData("200, 200")]
    public void RejectsNonsense(string text)
    {
        var ok = CoordinateTextParser.TryParseLatLong(text, out var latitude, out var longitude);

        if (text == "95, 51")
        {
            // documented behaviour: an impossible "lat, lon" reading is tried the other way round
            Assert.True(ok);
            AssertClose(51, latitude, Tolerance);
            AssertClose(95, longitude, Tolerance);
            return;
        }

        Assert.False(ok, text);
    }

    [Theory]
    [InlineData("35.5", 35.5)]
    [InlineData("-35.5", -35.5)]
    [InlineData("35°30'", 35.5)]
    [InlineData("35 30 00 S", -35.5)]
    [InlineData("W 51 15", -51.25)]
    [InlineData("۵۱٫۲۵", 51.25)]
    [InlineData("35°41′21.12″ N", 35.6892)]
    public void ParsesSingleAngles(string text, double expected)
    {
        Assert.True(CoordinateTextParser.TryParseAngle(text, out var degrees), text);
        AssertClose(expected, degrees, 1e-4);
    }

    [Theory]
    [InlineData("39N 534123 3950123", 39, true, 534123, 3950123)]
    [InlineData("39 N 534123.5, 3950123.25", 39, true, 534123.5, 3950123.25)]
    [InlineData("UTM 39N 534123E 3950123N", 39, true, 534123, 3950123)]
    [InlineData("56S 334000 6250000", 56, false, 334000, 6250000)]
    [InlineData("39n 534123 3950123", 39, true, 534123, 3950123)]
    public void ParsesUtm(string text, int zone, bool north, double x, double y)
    {
        Assert.True(CoordinateTextParser.TryParseUtm(text, out var z, out var n, out var e, out var nn), text);
        Assert.Equal(zone, z);
        Assert.Equal(north, n);
        AssertClose(x, e, Tolerance);
        AssertClose(y, nn, Tolerance);
    }

    [Theory]
    [InlineData("35.6892, 51.3890")]
    [InlineData("61N 534123 3950123")]     // zone out of range
    [InlineData("39N 534123")]
    public void RejectsNonUtm(string text)
    {
        Assert.False(CoordinateTextParser.TryParseUtm(text, out _, out _, out _, out _), text);
    }

    [Theory]
    [InlineData("500000", 500000)]
    [InlineData("500,000.5", 500000.5)]
    [InlineData("۵۰۰۰۰۰", 500000)]
    [InlineData("1e3", 1000)]
    [InlineData("-0.5", -0.5)]
    public void ParsesPlainNumbers(string text, double expected)
    {
        Assert.True(CoordinateTextParser.TryParseNumber(text, out var value));
        AssertClose(expected, value, Tolerance);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("1.2.3")]
    public void RejectsNonNumbers(string text)
    {
        Assert.False(CoordinateTextParser.TryParseNumber(text, out _));
    }

    [Theory]
    [InlineData(35.6892, true, "35°41′21.12″ N")]
    [InlineData(-35.6892, true, "35°41′21.12″ S")]
    [InlineData(51.389, false, "51°23′20.40″ E")]
    [InlineData(-0.5, false, "0°30′00.00″ W")]
    [InlineData(35.999999999, true, "36°00′00.00″ N")]    // seconds round up and carry
    [InlineData(0, true, "0°00′00.00″ N")]
    public void FormatsDmsWithHemisphere(double value, bool isLatitude, string expected)
    {
        Assert.Equal(expected, DegreeHelper.ToDmsWithHemisphere(value, isLatitude));
    }

    [Fact]
    public void DmsRoundTripsThroughText()
    {
        foreach (var value in new[] { 35.6892, -35.6892, 0.0, 89.999, -179.99999, 12.345678 })
        {
            var text = DegreeHelper.ToDmsWithHemisphere(value, isLatitude: false, secondDecimals: 4);

            Assert.True(CoordinateTextParser.TryParseAngle(text, out var back), text);
            AssertClose(value, back, 1e-6);
        }
    }
}
