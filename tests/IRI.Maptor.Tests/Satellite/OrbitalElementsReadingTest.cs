using System.Collections.Generic;
using System.Linq;
using IRI.Maptor.Core.Satellite.Elements;

namespace IRI.Maptor.Tests.Satellite;

public class OrbitalElementsReadingTest
{
    // The ISS on 2008-09-20: the worked example on Wikipedia's and CelesTrak's TLE format pages
    internal const string IssLine1 = "1 25544U 98067A   08264.51782528 -.00002182  00000-0 -11606-4 0  2927";
    internal const string IssLine2 = "2 25544  51.6416 247.4627 0006703 130.5360 325.0288 15.72125391563537";

    /// <summary>Replaces a line's checksum digit with the right one, for lines the test edits.</summary>
    internal static string WithChecksum(string line) => line.Substring(0, 68) + TwoLineElement.Checksum(line);

    [Fact]
    public void Parse_ReadsEveryFieldOfTheIssExample()
    {
        var iss = TwoLineElement.Parse(IssLine1, IssLine2, "ISS (ZARYA)");

        Assert.Equal("ISS (ZARYA)", iss.Name);
        Assert.Equal("25544", iss.CatalogNumber);
        Assert.Equal(25544, iss.CatalogNumberValue);
        Assert.Equal('U', iss.Classification);
        Assert.Equal("98067A", iss.InternationalDesignator);

        // day 264.51782528 of 2008 = 20 September, 12:25:40.104 UTC
        Assert.Equal(new DateTime(2008, 9, 20, 12, 25, 40, 104, DateTimeKind.Utc), iss.Epoch, TimeSpan.FromMilliseconds(1));
        Assert.Equal(DateTimeKind.Utc, iss.Epoch.Kind);

        Assert.Equal(-0.00002182, iss.MeanMotionDot, 12);
        Assert.Equal(0.0, iss.MeanMotionDdot);
        Assert.Equal(-0.11606e-4, iss.BStar, 12);
        Assert.Equal(51.6416, iss.Inclination, 10);
        Assert.Equal(247.4627, iss.RightAscension, 10);
        Assert.Equal(0.0006703, iss.Eccentricity, 12);
        Assert.Equal(130.5360, iss.ArgumentOfPerigee, 10);
        Assert.Equal(325.0288, iss.MeanAnomaly, 10);
        Assert.Equal(15.72125391, iss.MeanMotion, 10);
        Assert.Equal(56353, iss.RevolutionNumber);
        Assert.Equal(292, iss.ElementSetNumber);
        Assert.Equal(1440 / 15.72125391, iss.PeriodMinutes, 9);
    }

    [Fact]
    public void Parse_IgnoresTextAfterColumn69()
    {
        var iss = TwoLineElement.Parse(IssLine1 + "   extra", IssLine2 + "     0.00      4320.0        360.00");

        Assert.Equal(15.72125391, iss.MeanMotion, 10);
    }

    [Fact]
    public void Parse_RejectsAWrongChecksum()
    {
        // one digit of the mean anomaly changed, checksum left as it was
        var corrupted = IssLine2.Replace("325.0288", "325.0289");

        var ok = TwoLineElement.TryParse(IssLine1, corrupted, null, out var elements, out var error);

        Assert.False(ok);
        Assert.Null(elements);
        Assert.Contains("checksum", error);
    }

    [Fact]
    public void Parse_RejectsLinesOfDifferentSatellites()
    {
        var other = WithChecksum(IssLine2.Replace("2 25544", "2 25545"));

        Assert.False(TwoLineElement.TryParse(IssLine1, other, null, out _, out var error));
        Assert.Contains("25545", error);
    }

    [Theory]
    [InlineData("1 25544U 98067A   08264.51782528", "line1")]
    [InlineData(null, "line1")]
    public void Parse_RejectsMalformedLines(string? line1, string expectedInMessage)
    {
        Assert.False(TwoLineElement.TryParse(line1!, IssLine2, null, out _, out var error));
        Assert.Contains(expectedInMessage, error);
    }

    [Fact]
    public void Parse_SwappedLines_AreRejected()
    {
        Assert.Throws<FormatException>(() => TwoLineElement.Parse(IssLine2, IssLine1));
    }

    [Theory]
    [InlineData(57, 1957)]
    [InlineData(99, 1999)]
    [InlineData(0, 2000)]
    [InlineData(56, 2056)]
    public void Epoch_UsesThe1957Pivot(int twoDigitYear, int expectedYear)
    {
        Assert.Equal(new DateTime(expectedYear, 1, 1, 12, 0, 0, DateTimeKind.Utc), TwoLineElement.EpochToUtc(twoDigitYear, 1.5));
    }

    [Theory]
    [InlineData("25544", 25544)]
    [InlineData("00005", 5)]
    [InlineData("A0001", 100001)]
    [InlineData("H9999", 179999)]
    [InlineData("J0000", 180000)]
    [InlineData("Z9999", 339999)]
    public void CatalogNumbers_DecodeAndEncodeAlpha5(string text, int number)
    {
        Assert.Equal(number, CatalogNumbers.Decode(text));
        Assert.Equal(text, CatalogNumbers.Encode(number));
    }

    [Theory]
    [InlineData("I0001")]
    [InlineData("O0001")]
    [InlineData("A001")]
    [InlineData("12a45")]
    public void CatalogNumbers_RejectInvalidText(string text)
    {
        Assert.Throws<FormatException>(() => CatalogNumbers.Decode(text));
    }

    [Fact]
    public void Parse_ReadsAnAlpha5CatalogNumber()
    {
        var line1 = WithChecksum(IssLine1.Replace("1 25544U", "1 A5544U"));
        var line2 = WithChecksum(IssLine2.Replace("2 25544", "2 A5544"));

        var elements = TwoLineElement.Parse(line1, line2);

        Assert.Equal("A5544", elements.CatalogNumber);
        Assert.Equal(105544, elements.CatalogNumberValue);
    }

    [Fact]
    public void TleReader_ReadsTwoAndThreeLineSets_AndReportsBadOnes()
    {
        var bad = IssLine2.Replace("325.0288", "325.0289");

        var text = string.Join("\n",
            "# a comment",
            "0 ISS (ZARYA)",
            IssLine1,
            IssLine2,
            "",
            IssLine1,
            IssLine2,
            "BROKEN",
            IssLine1,
            bad,
            "LAST ONE",
            IssLine1,
            IssLine2);

        var errors = new List<TleReadError>();

        var sets = TleReader.Read(text, errors.Add);

        Assert.Equal(3, sets.Count);
        Assert.Equal("ISS (ZARYA)", sets[0].Name);
        Assert.Null(sets[1].Name);
        Assert.Equal("LAST ONE", sets[2].Name);

        var error = Assert.Single(errors);
        Assert.Equal(9, error.LineNumber);
        Assert.Contains("checksum", error.Message);

        Assert.Throws<FormatException>(() => TleReader.Read(text));
    }

    [Fact]
    public void TleReader_ReportsALineOneWithoutLineTwo()
    {
        var errors = new List<TleReadError>();

        var sets = TleReader.Read(IssLine1 + "\n" + IssLine1 + "\n" + IssLine2 + "\n" + IssLine1, errors.Add);

        Assert.Single(sets);
        Assert.Equal(2, errors.Count);
    }

    [Fact]
    public void Omm_ReadsCelesTrakJson_ToTheSameElementsAsTheTle()
    {
        const string json = @"[{
            ""OBJECT_NAME"": ""ISS (ZARYA)"", ""OBJECT_ID"": ""1998-067A"",
            ""EPOCH"": ""2008-09-20T12:25:40.104192"",
            ""MEAN_MOTION"": 15.72125391, ""ECCENTRICITY"": 0.0006703, ""INCLINATION"": 51.6416,
            ""RA_OF_ASC_NODE"": 247.4627, ""ARG_OF_PERICENTER"": 130.536, ""MEAN_ANOMALY"": 325.0288,
            ""EPHEMERIS_TYPE"": 0, ""CLASSIFICATION_TYPE"": ""U"", ""NORAD_CAT_ID"": 25544,
            ""ELEMENT_SET_NO"": 292, ""REV_AT_EPOCH"": 56353, ""BSTAR"": -1.1606e-5,
            ""MEAN_MOTION_DOT"": -2.182e-5, ""MEAN_MOTION_DDOT"": 0 }]";

        var omm = Assert.Single(OmmJsonReader.Read(json));

        var tle = TwoLineElement.Parse(IssLine1, IssLine2);

        Assert.Equal("ISS (ZARYA)", omm.Name);
        Assert.Equal("1998-067A", omm.InternationalDesignator);
        Assert.Equal(tle.CatalogNumberValue, omm.CatalogNumberValue);
        Assert.Equal(tle.Epoch, omm.Epoch, TimeSpan.FromMilliseconds(1));
        Assert.Equal(DateTimeKind.Utc, omm.Epoch.Kind);
        Assert.Equal(tle.MeanMotion, omm.MeanMotion, 12);
        Assert.Equal(tle.Eccentricity, omm.Eccentricity, 12);
        Assert.Equal(tle.BStar, omm.BStar, 12);
        Assert.Equal(tle.ElementSetNumber, omm.ElementSetNumber);
        Assert.Equal(tle.RevolutionNumber, omm.RevolutionNumber);
    }

    [Fact]
    public void Omm_ReportsAnItemWithAMissingField()
    {
        const string json = @"[{ ""OBJECT_NAME"": ""NO EPOCH"", ""NORAD_CAT_ID"": 1 }]";

        var errors = new List<string>();

        Assert.Empty(OmmJsonReader.Read(json, errors.Add));
        Assert.Contains("EPOCH", Assert.Single(errors));
        Assert.Throws<FormatException>(() => OmmJsonReader.Read(json));
    }
}
