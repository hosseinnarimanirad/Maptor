using System;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Core.SpatialReferenceSystem.MapProjections.Mgrs;

using Xunit;

namespace IRI.Maptor.Tests.CoordinateSystems;

/// <summary>
/// MGRS encoding and decoding.
/// </summary>
/// <remarks>
/// The suite is deliberately split by how deterministic each path is. Decoding a reference and
/// encoding from UTM are pure integer grid arithmetic — those get exact assertions. Encoding from
/// a latitude/longitude runs through the transverse Mercator series first, so those assert the
/// grid zone and the 100 km square exactly (that is what the band tables and letter sets decide)
/// and leave the digits to the round-trip tests, which bound the whole chain instead.
/// </remarks>
public class MgrsTest
{
    /// <summary>
    /// A 1 m reference names the square's south-west corner, so decoding it lands up to 1 m south
    /// and 1 m west of the original — √2 m at worst — before any projection error.
    /// </summary>
    private const double OneMetreSquareTolerance = 1.5;

    #region Decoding — pure grid arithmetic, exact

    /// <summary>
    /// Two published references, one in an odd zone and one in an even zone, so both halves of
    /// the row-letter rule are covered (odd zones start the row sequence at A on the equator,
    /// even zones at F).
    /// </summary>
    [Theory]
    [InlineData("31U DQ 48251 11932", 31, true, 448251.0, 5411932.0)]   // Eiffel Tower, odd zone
    [InlineData("18S UJ 23383 06479", 18, true, 323383.0, 4306479.0)]   // Washington Monument, even zone
    public void ToUtm_PublishedReferences_AreExact(string mgrs, int expectedZone, bool expectedIsNorth, double expectedEasting, double expectedNorthing)
    {
        Assert.True(MgrsConverter.TryToUtm(mgrs, out var zone, out var isNorth, out var easting, out var northing));

        Assert.Equal(expectedZone, zone);
        Assert.Equal(expectedIsNorth, isNorth);
        Assert.Equal(expectedEasting, easting);
        Assert.Equal(expectedNorthing, northing);
    }

    /// <summary>
    /// Southern-hemisphere northings carry the 10 000 km false northing, and the band letter is
    /// what says which hemisphere the reference is in.
    /// </summary>
    [Fact]
    public void ToUtm_SouthernHemisphere_KeepsTheFalseNorthing()
    {
        Assert.True(MgrsConverter.TryToUtm("56H LH 12345 67890", out var zone, out var isNorth, out var easting, out var northing));

        Assert.Equal(56, zone);
        Assert.False(isNorth);
        Assert.Equal(312345.0, easting);
        Assert.Equal(6267890.0, northing);
    }

    /// <summary>Fewer digits means a coarser square, and the reference names its south-west corner.</summary>
    [Theory]
    [InlineData("39S WV", 500000.0, 3900000.0)]
    [InlineData("39S WV 5 3", 550000.0, 3930000.0)]
    [InlineData("39S WV 53 39", 553000.0, 3939000.0)]
    [InlineData("39S WV 535 395", 553500.0, 3939500.0)]
    [InlineData("39S WV 5351 3950", 553510.0, 3939500.0)]
    [InlineData("39S WV 53516 39501", 553516.0, 3939501.0)]
    public void ToUtm_EveryPrecision_LandsOnTheSquareCorner(string mgrs, double expectedEasting, double expectedNorthing)
    {
        Assert.True(MgrsConverter.TryToUtm(mgrs, out _, out _, out var easting, out var northing));

        Assert.Equal(expectedEasting, easting);
        Assert.Equal(expectedNorthing, northing);
    }

    /// <summary>The centre of a 100 km square is 50 km north-east of its corner.</summary>
    [Fact]
    public void ToUtm_SquareCentre_IsHalfASquareNorthEast()
    {
        Assert.True(MgrsConverter.TryToUtm("39S WV", useSquareCentre: true, out _, out _, out var easting, out var northing));

        Assert.Equal(550000.0, easting);
        Assert.Equal(3950000.0, northing);
    }

    #endregion

    #region Encoding from UTM — pure grid arithmetic, exact

    /// <summary>The exact inverse of <see cref="ToUtm_PublishedReferences_AreExact"/>.</summary>
    [Theory]
    [InlineData(31, true, 448251.0, 5411932.0, "31U DQ 48251 11932")]
    [InlineData(18, true, 323383.0, 4306479.0, "18S UJ 23383 06479")]
    [InlineData(56, false, 312345.0, 6267890.0, "56H LH 12345 67890")]
    public void FromUtm_PublishedReferences_AreExact(int zone, bool isNorth, double easting, double northing, string expected)
    {
        Assert.Equal(expected, MgrsConverter.FromUtm(zone, isNorth, easting, northing, MgrsPrecision.M1));
    }

    /// <summary>
    /// The row letter sequence runs continuously across the equator: 10 000 000 / 100 000 is 100,
    /// a whole number of 20-letter cycles, so the southern false northing does not disturb it.
    /// </summary>
    [Fact]
    public void FromUtm_AcrossTheEquator_KeepsTheRowLetterSequenceContinuous()
    {
        // 1 km either side of the equator in the same zone, same 100 km column.
        var justNorth = MgrsConverter.FromUtm(39, true, 500000, 1000, MgrsPrecision.Km1);

        var justSouth = MgrsConverter.FromUtm(39, false, 500000, 9999000, MgrsPrecision.Km1);

        Assert.Equal("39N WA 00 01", justNorth);
        Assert.Equal("39M WV 00 99", justSouth);
    }

    /// <summary>
    /// The column letter set rotates every three zones and the row offset flips every two, so the
    /// full pattern repeats every six. Same easting and northing, six consecutive zones.
    /// </summary>
    [Fact]
    public void FromUtm_SquareLetters_RepeatEverySixZones()
    {
        for (var zone = 31; zone <= 36; zone++)
        {
            var first = MgrsConverter.FromUtm(zone, true, 500000, 4300000, MgrsPrecision.Km100);

            var sixLater = MgrsConverter.FromUtm(zone + 6, true, 500000, 4300000, MgrsPrecision.Km100);

            // Same square letters, different zone number.
            Assert.Equal(first.Substring(first.Length - 2), sixLater.Substring(sixLater.Length - 2));
        }

        // ...and three zones apart the columns match but the rows do not, because of the offset.
        var zone31 = MgrsConverter.FromUtm(31, true, 500000, 4300000, MgrsPrecision.Km100);

        var zone34 = MgrsConverter.FromUtm(34, true, 500000, 4300000, MgrsPrecision.Km100);

        Assert.Equal(zone31[zone31.Length - 2], zone34[zone34.Length - 2]);
        Assert.NotEqual(zone31[zone31.Length - 1], zone34[zone34.Length - 1]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(61)]
    [InlineData(-1)]
    public void TryFromUtm_ImpossibleZone_Fails(int zone)
    {
        Assert.False(MgrsConverter.TryFromUtm(zone, true, 500000, 4300000, MgrsPrecision.M1, out _));
    }

    #endregion

    #region Encoding from geodetic

    /// <summary>
    /// The grid zone and 100 km square are what the band table and the letter sets decide, and
    /// they are stable against the last metre of projection accuracy. Both references are far
    /// enough from a 100 km boundary for that to hold.
    /// </summary>
    [Theory]
    [InlineData(2.2945, 48.8584, "31U DQ")]      // Eiffel Tower — odd zone
    [InlineData(-77.0353, 38.8895, "18S UJ")]    // Washington Monument — even zone
    [InlineData(51.3380, 35.6997, "39S WV")]     // Azadi Tower, Tehran
    [InlineData(151.2153, -33.8568, "56H LH")]   // Sydney Opera House — southern hemisphere
    public void FromGeodetic_GridZoneAndSquare_MatchPublishedReferences(double longitude, double latitude, string expected)
    {
        Assert.Equal(expected, MgrsConverter.FromGeodetic(longitude, latitude, MgrsPrecision.Km100));
    }

    /// <summary>Longitude 0 / latitude 0 sits in the origin square of the whole grid.</summary>
    [Fact]
    public void FromGeodetic_TheOrigin_IsZone31NorthSquareAa()
    {
        Assert.Equal("31N AA", MgrsConverter.FromGeodetic(0.0, 0.0, MgrsPrecision.Km100));
    }

    /// <summary>Each step of precision adds one digit per axis and shortens nothing else.</summary>
    [Theory]
    [InlineData(MgrsPrecision.Km100, "31U DQ")]
    [InlineData(MgrsPrecision.Km10, "31U DQ 4 1")]
    [InlineData(MgrsPrecision.Km1, "31U DQ 48 11")]
    [InlineData(MgrsPrecision.M100, "31U DQ 482 119")]
    public void FromGeodetic_CoarsePrecisions_TruncateRatherThanRound(MgrsPrecision precision, string expected)
    {
        Assert.Equal(expected, MgrsConverter.FromGeodetic(2.2945, 48.8584, precision));
    }

    /// <summary>Every precision produces the documented number of digits per axis.</summary>
    [Theory]
    [InlineData(MgrsPrecision.Km100, 0)]
    [InlineData(MgrsPrecision.Km10, 1)]
    [InlineData(MgrsPrecision.Km1, 2)]
    [InlineData(MgrsPrecision.M100, 3)]
    [InlineData(MgrsPrecision.M10, 4)]
    [InlineData(MgrsPrecision.M1, 5)]
    public void FromGeodetic_DigitCount_MatchesThePrecision(MgrsPrecision precision, int expectedDigits)
    {
        var mgrs = MgrsConverter.FromGeodetic(51.3380, 35.6997, precision);

        var parsed = MgrsConverter.Parse(mgrs);

        Assert.Equal(precision, parsed.Precision);
        Assert.Equal(expectedDigits * 2, mgrs.Replace(" ", string.Empty).Length - 5); // "39S" + two square letters
    }

    [Theory]
    [InlineData(84.1)]
    [InlineData(-80.1)]
    [InlineData(90.0)]
    [InlineData(-90.0)]
    public void TryFromGeodetic_OutsideTheUtmBand_Fails(double latitude)
    {
        // The polar caps need UPS, which this library has no projection for.
        Assert.False(MgrsConverter.TryFromGeodetic(51.0, latitude, MgrsPrecision.M1, out _));

        Assert.Throws<ArgumentOutOfRangeException>(() => MgrsConverter.FromGeodetic(51.0, latitude, MgrsPrecision.M1));
    }

    /// <summary>Both ends of the covered range are inside it, not outside.</summary>
    [Theory]
    [InlineData(-80.0, 'C')]
    [InlineData(84.0, 'X')]
    public void FromGeodetic_TheExactLimits_AreIncluded(double latitude, char expectedBand)
    {
        Assert.True(MgrsConverter.TryFromGeodetic(51.0, latitude, MgrsPrecision.Km100, out var mgrs));

        Assert.Equal(expectedBand, MgrsConverter.Parse(mgrs).Band);
    }

    #endregion

    #region Latitude bands

    /// <summary>
    /// Bands run 8° each from 80°S, skipping I and O, except X which is 12° because the polar
    /// bands start only at 84°.
    /// </summary>
    [Theory]
    [InlineData(-80.0, 'C')]
    [InlineData(-72.0, 'D')]
    [InlineData(-8.0, 'M')]
    [InlineData(-0.001, 'M')]
    [InlineData(0.0, 'N')]
    [InlineData(8.0, 'P')]     // I is skipped, so P follows N
    [InlineData(16.0, 'Q')]
    [InlineData(32.0, 'S')]
    [InlineData(40.0, 'T')]
    [InlineData(56.0, 'V')]
    [InlineData(64.0, 'W')]    // O is skipped
    [InlineData(72.0, 'X')]
    [InlineData(83.9, 'X')]    // X is the 12-degree one
    public void FromGeodetic_BandLetter_FollowsTheEightDegreeBands(double latitude, char expectedBand)
    {
        var mgrs = MgrsConverter.FromGeodetic(51.0, latitude, MgrsPrecision.Km100);

        Assert.Equal(expectedBand, MgrsConverter.Parse(mgrs).Band);
    }

    /// <summary>The band letter is what decides the hemisphere when a reference is decoded.</summary>
    [Theory]
    [InlineData('M', false)]
    [InlineData('N', true)]
    [InlineData('C', false)]
    [InlineData('X', true)]
    public void ToUtm_Hemisphere_ComesFromTheBandLetter(char band, bool expectedIsNorth)
    {
        Assert.True(MgrsConverter.TryToUtm($"39{band} WV 50000 50000", out _, out var isNorth, out _, out _));

        Assert.Equal(expectedIsNorth, isNorth);
    }

    #endregion

    #region Zone boundaries and the irregular zones

    /// <summary>
    /// The zone boundaries themselves. Needing these exact is what drove the fix to
    /// <see cref="MapProjects.FindUtmZone(double)"/> that this now delegates to — it used to
    /// compute <c>30 + ceil(lon / 6)</c> and answer the zone to the west at every multiple of six.
    /// </summary>
    [Theory]
    [InlineData(0.0, 31)]
    [InlineData(6.0, 32)]
    [InlineData(12.0, 33)]
    [InlineData(48.0, 39)]
    [InlineData(-6.0, 30)]
    [InlineData(-180.0, 1)]
    [InlineData(179.999, 60)]
    public void FromGeodetic_ExactZoneBoundaries_UseTheEasternZone(double longitude, int expectedZone)
    {
        // Latitude 45 keeps every case clear of the Norway and Svalbard exceptions.
        var mgrs = MgrsConverter.FromGeodetic(longitude, 45.0, MgrsPrecision.Km100);

        Assert.Equal(expectedZone, MgrsConverter.Parse(mgrs).Zone);
    }

    /// <summary>
    /// Norway: over band V (56°–64°N) zone 32 is widened westward to 3°E, so the western coast
    /// stays in one zone instead of being split.
    /// </summary>
    [Theory]
    [InlineData(5.3221, 60.3913, 32)]   // Bergen — would be zone 31 without the exception
    [InlineData(3.0, 56.0, 32)]         // the south-west corner of the widened area
    [InlineData(2.999, 60.0, 31)]       // just west of it
    [InlineData(12.0, 60.0, 33)]        // just east of it
    [InlineData(5.3221, 55.999, 31)]    // just south of band V
    [InlineData(5.3221, 64.0, 31)]      // just north of band V
    public void FromGeodetic_Norway_WidensZone32OverBandV(double longitude, double latitude, int expectedZone)
    {
        var mgrs = MgrsConverter.FromGeodetic(longitude, latitude, MgrsPrecision.Km100);

        Assert.Equal(expectedZone, MgrsConverter.Parse(mgrs).Zone);
    }

    /// <summary>
    /// Svalbard: over band X (72°–84°N) zones 31, 33, 35 and 37 are widened and 32, 34 and 36
    /// do not exist at all.
    /// </summary>
    [Theory]
    [InlineData(15.6469, 78.2232, 33)]  // Longyearbyen — would be zone 33 anyway, but via the exception
    [InlineData(8.999, 78.0, 31)]
    [InlineData(9.0, 78.0, 33)]
    [InlineData(20.999, 78.0, 33)]
    [InlineData(21.0, 78.0, 35)]
    [InlineData(32.999, 78.0, 35)]
    [InlineData(33.0, 78.0, 37)]
    [InlineData(41.999, 78.0, 37)]
    [InlineData(42.0, 78.0, 38)]        // east of the exception, the grid is regular again
    public void FromGeodetic_Svalbard_SkipsTheEvenZonesOverBandX(double longitude, double latitude, int expectedZone)
    {
        var mgrs = MgrsConverter.FromGeodetic(longitude, latitude, MgrsPrecision.Km100);

        Assert.Equal(expectedZone, MgrsConverter.Parse(mgrs).Zone);
    }

    /// <summary>
    /// The central meridians of the outermost zones are 177°W and 177°E.
    /// <see cref="MapProjects.CalculateCentralMeridian(int)"/> used to report the western ones as
    /// 183°–357°, which is equivalent modulo 360 but would leave the projection with a
    /// 360-degree longitude difference. Fixed there; this guards the result.
    /// </summary>
    [Theory]
    [InlineData(-177.0, 0.5)]
    [InlineData(177.0, -0.5)]
    public void FromGeodetic_OnTheOutermostCentralMeridians_LandsOnTheFalseEasting(double longitude, double latitude)
    {
        var mgrs = MgrsConverter.FromGeodetic(longitude, latitude, MgrsPrecision.M1);

        Assert.True(MgrsConverter.TryToUtm(mgrs, out _, out _, out var easting, out _));

        Assert.Equal(500000.0, easting);
    }

    #endregion

    #region Round trips

    /// <summary>
    /// The whole chain, worldwide. At 1 m precision the reference names the square's south-west
    /// corner, so the corner-to-point offset dominates and bounds everything else.
    /// </summary>
    [Fact]
    public void RoundTrip_AcrossTheWholeGrid_StaysWithinTheSquare()
    {
        var worst = 0.0;

        var worstAt = string.Empty;

        for (var latitude = -79.5; latitude <= 83.5; latitude += 3.7)
        {
            for (var longitude = -179.0; longitude <= 179.0; longitude += 17.3)
            {
                Assert.True(MgrsConverter.TryFromGeodetic(longitude, latitude, MgrsPrecision.M1, out var mgrs),
                    $"encoding failed at ({longitude}, {latitude})");

                Assert.True(MgrsConverter.TryToGeodetic(mgrs, out var back), $"decoding failed for '{mgrs}'");

                var metresEast = (back.X - longitude) * 111320.0 * Math.Cos(latitude * Math.PI / 180.0);

                var metresNorth = (back.Y - latitude) * 110574.0;

                var distance = Math.Sqrt(metresEast * metresEast + metresNorth * metresNorth);

                if (distance > worst)
                {
                    worst = distance;
                    worstAt = $"({longitude}, {latitude}) -> {mgrs}";
                }
            }
        }

        Assert.True(worst < OneMetreSquareTolerance, $"worst round-trip error was {worst:F3} m at {worstAt}");
    }

    /// <summary>A coarser reference names a bigger square, and the error grows with it — no more.</summary>
    [Theory]
    [InlineData(MgrsPrecision.M1, 1.5)]
    [InlineData(MgrsPrecision.M10, 15.0)]
    [InlineData(MgrsPrecision.M100, 150.0)]
    [InlineData(MgrsPrecision.Km1, 1500.0)]
    [InlineData(MgrsPrecision.Km10, 15000.0)]
    [InlineData(MgrsPrecision.Km100, 150000.0)]
    public void RoundTrip_EveryPrecision_StaysWithinItsSquare(MgrsPrecision precision, double toleranceInMetres)
    {
        const double longitude = 51.3380;
        const double latitude = 35.6997;

        var mgrs = MgrsConverter.FromGeodetic(longitude, latitude, precision);

        Assert.True(MgrsConverter.TryToGeodetic(mgrs, out var back));

        var metresEast = (back.X - longitude) * 111320.0 * Math.Cos(latitude * Math.PI / 180.0);

        var metresNorth = (back.Y - latitude) * 110574.0;

        Assert.True(Math.Sqrt(metresEast * metresEast + metresNorth * metresNorth) < toleranceInMetres);
    }

    /// <summary>The southern hemisphere round-trips as well as the northern one.</summary>
    [Theory]
    [InlineData(151.2153, -33.8568)]   // Sydney
    [InlineData(-58.3816, -34.6037)]   // Buenos Aires
    [InlineData(18.4241, -33.9249)]    // Cape Town
    [InlineData(51.0, -0.001)]         // one metre south of the equator
    public void RoundTrip_SouthernHemisphere_IsExact(double longitude, double latitude)
    {
        var mgrs = MgrsConverter.FromGeodetic(longitude, latitude, MgrsPrecision.M1);

        Assert.True(MgrsConverter.TryToGeodetic(mgrs, out var back));

        var metresEast = (back.X - longitude) * 111320.0 * Math.Cos(latitude * Math.PI / 180.0);

        var metresNorth = (back.Y - latitude) * 110574.0;

        Assert.True(Math.Sqrt(metresEast * metresEast + metresNorth * metresNorth) < OneMetreSquareTolerance);
    }

    #endregion

    #region Extent — a reference names a region, not a point

    /// <summary>A bare zone number is the whole six-degree strip, pole band to pole band.</summary>
    [Fact]
    public void GetBoundingBox_BareZone_IsTheWholeStrip()
    {
        var box = MgrsConverter.GetBoundingBox("39");

        Assert.Equal(48.0, box.XMin, 9);
        Assert.Equal(54.0, box.XMax, 9);
        Assert.Equal(-80.0, box.YMin, 9);
        Assert.Equal(84.0, box.YMax, 9);
    }

    /// <summary>A grid zone designator is one cell of the zone/band lattice: six degrees by eight.</summary>
    [Fact]
    public void GetBoundingBox_GridZone_IsOneCell()
    {
        var box = MgrsConverter.GetBoundingBox("39S");

        Assert.Equal(48.0, box.XMin, 9);
        Assert.Equal(54.0, box.XMax, 9);
        Assert.Equal(32.0, box.YMin, 9);
        Assert.Equal(40.0, box.YMax, 9);
    }

    /// <summary>Band X is the tall one — twelve degrees, not eight.</summary>
    [Fact]
    public void GetBoundingBox_BandX_IsTwelveDegreesTall()
    {
        Assert.Equal(12.0, MgrsConverter.GetBoundingBox("37X").Height, 9);
    }

    /// <summary>
    /// A 100 km square really is about 100 km on a side. The box is slightly larger than the
    /// square because a straight line in UTM bows in latitude/longitude, and the edges are
    /// sampled rather than just the corners so that bulge is included.
    /// </summary>
    [Fact]
    public void GetBoundingBox_HundredKilometreSquare_IsAboutAHundredKilometres()
    {
        var box = MgrsConverter.GetBoundingBox("39S WV");

        var metresTall = box.Height * 110574.0;

        var metresWide = box.Width * 111320.0 * Math.Cos(box.Center.Y * Math.PI / 180.0);

        Assert.InRange(metresTall, 100000.0, 101500.0);
        Assert.InRange(metresWide, 100000.0, 101500.0);
    }

    /// <summary>Each extra pair of digits names a square a tenth of the size, nested in the last.</summary>
    [Fact]
    public void GetBoundingBox_EachPrecision_NestsInsideTheCoarserOne()
    {
        var references = new[] { "39S WV", "39S WV 5 3", "39S WV 53 39", "39S WV 535 395", "39S WV 5351 3950", "39S WV 53516 39501" };

        BoundingBox? previous = null;

        foreach (var reference in references)
        {
            var box = MgrsConverter.GetBoundingBox(reference);

            if (previous is BoundingBox outer)
            {
                Assert.True(box.Width < outer.Width, $"{reference} is not narrower than its parent");

                // a hair of tolerance: the parent's box is the bulged one, the child's is not
                Assert.True(box.XMin >= outer.XMin - 1e-6 && box.XMax <= outer.XMax + 1e-6, $"{reference} escapes its parent in longitude");
                Assert.True(box.YMin >= outer.YMin - 1e-6 && box.YMax <= outer.YMax + 1e-6, $"{reference} escapes its parent in latitude");
            }

            previous = box;
        }
    }

    /// <summary>The corner a reference decodes to has to sit inside the region it names.</summary>
    [Theory]
    [InlineData("39S WV")]
    [InlineData("39S WV 53516 39501")]
    [InlineData("18S UJ 23383 06479")]
    [InlineData("56H LH 12345 67890")]
    [InlineData("31U DQ 48251 11932")]
    public void GetBoundingBox_ContainsThePositionTheReferenceDecodesTo(string reference)
    {
        var box = MgrsConverter.GetBoundingBox(reference);

        var point = MgrsConverter.ToGeodetic(reference);

        Assert.InRange(point.X, box.XMin, box.XMax);
        Assert.InRange(point.Y, box.YMin, box.YMax);
    }

    /// <summary>
    /// Norway and Svalbard redraw the cells, so the grid zone box is not always the zone's
    /// nominal six degrees.
    /// </summary>
    [Theory]
    [InlineData("31V", 0.0, 3.0)]     // narrowed so 32V can take the coast
    [InlineData("32V", 3.0, 12.0)]    // widened west over Norway
    [InlineData("33V", 12.0, 18.0)]   // unaffected
    [InlineData("31X", 0.0, 9.0)]     // Svalbard
    [InlineData("33X", 9.0, 21.0)]
    [InlineData("35X", 21.0, 33.0)]
    [InlineData("37X", 33.0, 42.0)]
    [InlineData("39X", 48.0, 54.0)]   // east of the exception, back to nominal
    public void GetBoundingBox_IrregularCells_HaveTheirRealWidth(string reference, double expectedWest, double expectedEast)
    {
        var box = MgrsConverter.GetBoundingBox(reference);

        Assert.Equal(expectedWest, box.XMin, 9);
        Assert.Equal(expectedEast, box.XMax, 9);
    }

    /// <summary>Over band X the even zones 32, 34 and 36 do not exist at all.</summary>
    [Theory]
    [InlineData("32X")]
    [InlineData("34X")]
    [InlineData("36X")]
    public void TryGetBoundingBox_CellsThatDoNotExist_AreRejected(string reference)
    {
        Assert.False(MgrsConverter.TryGetBoundingBox(reference, out _));
    }

    /// <summary>A zone whose cells spill sideways reports the widest longitude span it ever reaches.</summary>
    [Fact]
    public void GetBoundingBox_BareZone32_IsWidenedByNorway()
    {
        var box = MgrsConverter.GetBoundingBox("32");

        Assert.Equal(3.0, box.XMin, 9);
        Assert.Equal(12.0, box.XMax, 9);
    }

    /// <summary>
    /// A square letter pair with no band in front of it is not a legal reference. The regex can
    /// only reach that reading by backtracking past the optional band, so it is rejected
    /// explicitly rather than by the pattern.
    /// </summary>
    [Theory]
    [InlineData("39WV")]
    [InlineData("39 WV 53516 39501")]
    public void TryGetBoundingBox_SquareWithoutABand_IsRejected(string reference)
    {
        Assert.False(MgrsConverter.TryGetBoundingBox(reference, out _));
        Assert.False(MgrsConverter.TryParse(reference, out _));
    }

    /// <summary>
    /// <see cref="MgrsConverter.TryParse"/> keeps its old contract: it yields a coordinate, so it
    /// needs at least a 100 km square. The coarser levels are regions and only the extent API
    /// takes them.
    /// </summary>
    [Theory]
    [InlineData("39")]
    [InlineData("39S")]
    public void TryParse_ReferencesCoarserThanASquare_AreStillRejected(string reference)
    {
        Assert.False(MgrsConverter.TryParse(reference, out _));

        Assert.True(MgrsConverter.TryGetBoundingBox(reference, out _));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("61")]
    [InlineData("39I")]
    [InlineData("39S W")]
    [InlineData("hello")]
    public void TryGetBoundingBox_Malformed_Fails(string? reference)
    {
        Assert.False(MgrsConverter.TryGetBoundingBox(reference, out _));
    }

    [Fact]
    public void GetBoundingBox_Malformed_Throws()
    {
        Assert.Throws<FormatException>(() => MgrsConverter.GetBoundingBox("39WV"));
    }

    #endregion

    #region Parsing

    /// <summary>Spacing and case are presentation; the parsed value is the same either way.</summary>
    [Theory]
    [InlineData("31UDQ4825111932")]
    [InlineData("31U DQ 48251 11932")]
    [InlineData("31UDQ 4825111932")]
    [InlineData("31U DQ 4825111932")]
    [InlineData("31u dq 48251 11932")]
    [InlineData("  31U   DQ   48251   11932  ")]
    public void TryParse_AnySpacingOrCase_GivesTheSameCoordinate(string text)
    {
        Assert.True(MgrsConverter.TryParse(text, out var parsed));

        Assert.Equal(31, parsed.Zone);
        Assert.Equal('U', parsed.Band);
        Assert.Equal('D', parsed.Column);
        Assert.Equal('Q', parsed.Row);
        Assert.Equal(48251, parsed.Easting);
        Assert.Equal(11932, parsed.Northing);
        Assert.Equal(MgrsPrecision.M1, parsed.Precision);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("hello")]
    [InlineData("31U")]                     // no square
    [InlineData("31U D")]                   // half a square
    [InlineData("61U DQ 48251 11932")]      // zone out of range
    [InlineData("0U DQ 48251 11932")]       // zone out of range
    [InlineData("31I DQ 48251 11932")]      // I is not a band letter
    [InlineData("31O DQ 48251 11932")]      // O is not a band letter
    [InlineData("31A DQ 48251 11932")]      // A is a polar band, not a UTM one
    [InlineData("31U IQ 48251 11932")]      // I is not a square letter
    [InlineData("31U DO 48251 11932")]      // O is not a square letter
    [InlineData("31U DW 48251 11932")]      // W is past V, the last row letter
    [InlineData("31U SQ 48251 11932")]      // S is not in zone 31's column set
    [InlineData("31U DQ 4825 11932")]       // odd number of digits
    [InlineData("31U DQ 482511 119321")]    // more than five digits per axis
    [InlineData("31U DQ 48251 11932 7")]    // stray digit
    public void TryParse_Malformed_Fails(string? text)
    {
        Assert.False(MgrsConverter.TryParse(text, out _));

        Assert.False(MgrsConverter.TryToGeodetic(text, out _));
    }

    /// <summary>
    /// The column letters rotate in sets of eight every three zones, so the same letter is legal
    /// in one zone and not in the next.
    /// </summary>
    [Theory]
    [InlineData(31, 'A', true)]     // zones 1,4,7,... use A-H
    [InlineData(31, 'J', false)]
    [InlineData(32, 'J', true)]     // zones 2,5,8,... use J-R
    [InlineData(32, 'A', false)]
    [InlineData(33, 'S', true)]     // zones 3,6,9,... use S-Z
    [InlineData(33, 'A', false)]
    public void TryParse_ColumnLetter_MustBelongToTheZonesSet(int zone, char column, bool expected)
    {
        Assert.Equal(expected, MgrsConverter.TryParse($"{zone}U {column}Q", out _));
    }

    [Fact]
    public void Parse_Malformed_Throws()
    {
        Assert.Throws<FormatException>(() => MgrsConverter.Parse("not an mgrs reference"));

        Assert.Throws<FormatException>(() => MgrsConverter.ToGeodetic("31U IQ 48251 11932"));
    }

    #endregion

    #region Normalizing

    /// <summary>
    /// However it was typed, the canonical form is upper case with the conventional spacing and
    /// leading zeros kept. Every level normalizes, not just the ones that name a square.
    /// </summary>
    [Theory]
    [InlineData("39", "39")]
    [InlineData("39s", "39S")]
    [InlineData("39 S", "39S")]
    [InlineData("39swv", "39S WV")]
    [InlineData("39s wv 5 3", "39S WV 5 3")]
    [InlineData("39swv5351639501", "39S WV 53516 39501")]
    [InlineData("  31u   dq   48251   11932  ", "31U DQ 48251 11932")]
    [InlineData("39SWV0012300045", "39S WV 00123 00045")]
    public void TryNormalize_GivesTheConventionalForm(string typed, string expected)
    {
        Assert.True(MgrsConverter.TryNormalize(typed, out var canonical));

        Assert.Equal(expected, canonical);
    }

    /// <summary>Normalizing is idempotent — the canonical form is already canonical.</summary>
    [Theory]
    [InlineData("39")]
    [InlineData("39S")]
    [InlineData("39S WV")]
    [InlineData("39S WV 53516 39501")]
    public void TryNormalize_IsIdempotent(string reference)
    {
        Assert.True(MgrsConverter.TryNormalize(reference, out var once));
        Assert.True(MgrsConverter.TryNormalize(once, out var twice));

        Assert.Equal(reference, once);
        Assert.Equal(once, twice);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("hello")]
    [InlineData("39WV")]
    [InlineData("61S")]
    public void TryNormalize_Malformed_Fails(string? reference)
    {
        Assert.False(MgrsConverter.TryNormalize(reference, out var canonical));

        Assert.Equal(string.Empty, canonical);
    }

    #endregion

    #region Formatting

    [Theory]
    [InlineData(MgrsPrecision.M1, 48251, 11932, "31U DQ 48251 11932", "31UDQ4825111932")]
    [InlineData(MgrsPrecision.Km1, 48, 11, "31U DQ 48 11", "31UDQ4811")]
    [InlineData(MgrsPrecision.Km100, 0, 0, "31U DQ", "31UDQ")]
    public void ToString_WithAndWithoutSpaces(MgrsPrecision precision, int easting, int northing, string spaced, string compact)
    {
        var coordinate = new MgrsCoordinate(31, 'U', 'D', 'Q', easting, northing, precision);

        Assert.Equal(spaced, coordinate.ToString());
        Assert.Equal(compact, coordinate.ToString(withSpaces: false));
    }

    /// <summary>Leading zeros are part of the reference — dropping them would change the position.</summary>
    [Fact]
    public void ToString_LeadingZeros_ArePadded()
    {
        var coordinate = new MgrsCoordinate(39, 'S', 'W', 'V', 123, 45, MgrsPrecision.M1);

        Assert.Equal("39S WV 00123 00045", coordinate.ToString());
    }

    [Fact]
    public void GridZoneDesignatorAndSquareId_AreTheTwoHalvesOfThePrefix()
    {
        var coordinate = MgrsConverter.Parse("39S WV 53516 39501");

        Assert.Equal("39S", coordinate.GridZoneDesignator);
        Assert.Equal("WV", coordinate.SquareId);
    }

    /// <summary>Everything a coordinate is written as parses back to the same coordinate.</summary>
    [Theory]
    [InlineData("31U DQ 48251 11932")]
    [InlineData("56H LH 12345 67890")]
    [InlineData("39S WV")]
    [InlineData("1N EA 00000 55265")]
    public void ToString_ThenParse_IsTheIdentity(string text)
    {
        var coordinate = MgrsConverter.Parse(text);

        Assert.Equal(coordinate, MgrsConverter.Parse(coordinate.ToString()));
        Assert.Equal(coordinate, MgrsConverter.Parse(coordinate.ToString(withSpaces: false)));
    }

    [Theory]
    [InlineData(MgrsPrecision.Km100, 100000.0)]
    [InlineData(MgrsPrecision.Km10, 10000.0)]
    [InlineData(MgrsPrecision.Km1, 1000.0)]
    [InlineData(MgrsPrecision.M100, 100.0)]
    [InlineData(MgrsPrecision.M10, 10.0)]
    [InlineData(MgrsPrecision.M1, 1.0)]
    public void GetSquareSize_MatchesThePrecisionName(MgrsPrecision precision, double expected)
    {
        Assert.Equal(expected, MgrsConverter.GetSquareSize(precision), 6);
    }

    #endregion
}
