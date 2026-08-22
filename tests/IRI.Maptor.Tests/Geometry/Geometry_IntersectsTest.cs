using IRI.Maptor.Core.Spatial.Primitives;
using System.Collections.Generic;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;

namespace IRI.Maptor.Tests.TheGeometry;

public class Geometry_IntersectsTest
{
    const string _orthogonalLine = "LINESTRING (0 0, 0 100, 100 100)";
    const string _compoundLine = "LINESTRING (-392 194, -227 194, -227 305, -70 65)";
    const string _slantingLine = "LINESTRING (0 0, 100 0)";
    const string _rectangle = "POLYGON ((500 500, 500 1000, 1000 1000, 1000 500, 500 500))";
    const string _triangle = "POLYGON ((-444 251, -261 608, -86 257, -444 251))";
    const string _multiPolygon = "MULTIPOLYGON (((41362 46766, 50500 47500, 44191 54677, 39375 51582, 41362 46766)), ((69033 33428, 83365 39352, 74421 55442, 66090 47722, 50500 47500, 50500 35262, 69033 33428), (76753 38282, 61733 40001, 74421 49977, 76753 38282)))";

    #region Orthogonal line

    // 1401.12.10
    [Theory]
    //[MemberData(nameof(IntersectsOrthogonalLineData))]
    [InlineData(true, _orthogonalLine, "LINESTRING (100 100, 114 99)")]
    [InlineData(true, _orthogonalLine, "LINESTRING (41 127, 100 98)")]
    [InlineData(true, _orthogonalLine, "LINESTRING (0 100, -36 130)")]
    [InlineData(true, _orthogonalLine, "LINESTRING (-15 6, 8 138, -60 157)")]
    [InlineData(true, _orthogonalLine, "LINESTRING (99 156, 136 34, 96 104)")]
    [InlineData(true, _orthogonalLine, "LINESTRING (-51 89, 0 100, -53 122)")]
    [InlineData(true, _orthogonalLine, "LINESTRING (36 96, 35 104)")]
    [InlineData(true, _orthogonalLine, "LINESTRING (-2 94, 9 102)")]
    [InlineData(true, _orthogonalLine, "LINESTRING (-3 0, -3 107, 100 100)")]
    [InlineData(true, _orthogonalLine, "LINESTRING (-161 225, 99 119, 0 100)")]

    [InlineData(false, _orthogonalLine, "LINESTRING(51 109, 117 113)")]
    [InlineData(false, _orthogonalLine, "LINESTRING(-140 166, -56 212)")]
    [InlineData(false, _orthogonalLine, "LINESTRING(10 101, 15 101)")]
    [InlineData(false, _orthogonalLine, "LINESTRING(1 98, 7 98)")]
    [InlineData(false, _orthogonalLine, "LINESTRING(-1 97, -1 101, 2 101)")]
    [InlineData(false, _orthogonalLine, "LINESTRING(-162 198, -4 -107, 19 94)")]
    [InlineData(false, _orthogonalLine, "LINESTRING(97 95, 5 4, 4 96)")]
    [InlineData(false, _orthogonalLine, "LINESTRING(5 77, 28 95, 7 96)")]
    public void IntersectsOrthogonalLine(bool intersects, string wktFirstGeometry, string wktSecondGeometry)
    {
        // Arrange
        var firstSut = Geometry<Point>.FromWkt(wktFirstGeometry, SridHelper.WebMercator);
        var secondSut = Geometry<Point>.FromWkt(wktSecondGeometry, SridHelper.WebMercator);

        // Act
        var intersects1 = firstSut.Intersects(secondSut);
        var intersects2 = secondSut.Intersects(firstSut);

        // Asserts
        Assert.Equal(intersects, intersects1);
        Assert.Equal(intersects, intersects2);
    }

    #endregion

    #region CompoundLine

    [Theory]
    [InlineData(false, _compoundLine, "LINESTRING(-834 254, -129 1083)")]
    [InlineData(false, _compoundLine, "LINESTRING(-340 196, -231 196)")]
    [InlineData(false, _compoundLine, "LINESTRING(-413 170, -161 196)")]
    [InlineData(false, _compoundLine, "LINESTRING(-223 292, -222 176, -400 184)")]
    [InlineData(false, _compoundLine, "LINESTRING(-231 258, -231 196)")]
    [InlineData(false, _compoundLine, "LINESTRING(-71 75, -51 31, -222 288, -221 41)")]
    [InlineData(false, _compoundLine, "MULTILINESTRING((-253 150, -188 239), (-232 301, -459 156))")]
    [InlineData(false, _compoundLine, "MULTILINESTRING((-268 164, -267 192), (-267 195, -267 223))")]
    [InlineData(false, _compoundLine, "MULTILINESTRING((-541 418, -404 417), (-372 196, -232 197), (-76 63, -222 291))")]
    [InlineData(false, _compoundLine, "MULTILINESTRING((-90 368, -84 94, -51 43), (-187 188, -192 248), (-189 258, -163 315))")]
    [InlineData(false, _compoundLine, "MULTILINESTRING((-430 104, -301 189), (-292 198, -231 248), (-220 251, -197 249), (-217 305, -233 319, -261 200))")]
    [InlineData(false, _compoundLine, "MULTILINESTRING((-207 295, -182 238), (-183 233, -149 104, -350 179), (-306 122, -163 203), (-222 259, -223 296, -224 188))")]

    [InlineData(true, _compoundLine, "LINESTRING (-392 194, -458 145)")]
    [InlineData(true, _compoundLine, "LINESTRING (-431 191, -229 231, -458 249, -129 316)")]
    [InlineData(true, _compoundLine, "LINESTRING (-424 217, -418 508, -5 53)")]
    [InlineData(true, _compoundLine, "LINESTRING (89 -184, -19 70, -70 65)")]
    [InlineData(true, _compoundLine, "LINESTRING (-227 305, -203 313)")]
    [InlineData(true, _compoundLine, "LINESTRING (160 591, 159 486, -82 74)")]
    [InlineData(true, _compoundLine, "LINESTRING (-1109 1649, 1186 1252, 1004 -627, -70 65)")]
    [InlineData(true, _compoundLine, "MULTILINESTRING ((-1307 912, 478 580), (-280 -587, -283 229))")]
    [InlineData(true, _compoundLine, "MULTILINESTRING ((-221 308, -62 69), (-229 195, -229 305), (369 764, -55 -197, -392 202))")]
    [InlineData(true, _compoundLine, "MULTILINESTRING ((-689 -418, -13 124), (-101 255, -22 -969), (-553 38, 115 525))")]
    [InlineData(true, _compoundLine, "MULTILINESTRING ((-3151 2308, 192 2356, -647 -620), (-407 230, -215 308))")]
    [InlineData(true, _compoundLine, "POLYGON ((-103 39, -47 160, -42 31, -103 39))")]
    [InlineData(true, _compoundLine, "POLYGON ((-166 -159, 184 251, 148 445, -556 220, -195 128, -166 -159))")]
    public void IntersectsCompoundLine(bool intersects, string wktFirstGeometry, string wktSecondGeometry)
    {
        // Arrange

        var firstSut = Geometry<Point>.FromWkt(wktFirstGeometry, SridHelper.WebMercator);
        var secondSut = Geometry<Point>.FromWkt(wktSecondGeometry, SridHelper.WebMercator);

        // Act
        var intersects1 = firstSut.Intersects(secondSut);
        var intersects2 = secondSut.Intersects(firstSut);

        // Asserts
        Assert.Equal(intersects, intersects1);
        Assert.Equal(intersects, intersects2);
    }

    #endregion

    #region Slanting line

    [Theory]
    [InlineData(_slantingLine, "LINESTRING (0 -0, -24 -26)", true)]
    [InlineData(_slantingLine, "LINESTRING (0 0, -31 0)", true)]
    [InlineData(_slantingLine, "LINESTRING (0 -0, -0 -37)", true)]
    [InlineData(_slantingLine, "LINESTRING (2 -6, 8 6)", true)]
    [InlineData(_slantingLine, "LINESTRING (26 -24, -13 6, 0 -0)", true)]
    [InlineData(_slantingLine, "LINESTRING (30 -1, 59 -44, 54 1)", true)]
    [InlineData(_slantingLine, "LINESTRING (-376 400, 184 -180)", true)]
    [InlineData(_slantingLine, "LINESTRING (65 -0, 65 10)", true)]
    public void IntersectsSlantingLine(string wktFirstGeometry, string wktSecondGeometry, bool intersects)
    {
        // Arrange
        var firstSut = Geometry<Point>.FromWkt(wktFirstGeometry, SridHelper.WebMercator);
        var secondSut = Geometry<Point>.FromWkt(wktSecondGeometry, SridHelper.WebMercator);

        // Act
        var intersects1 = firstSut.Intersects(secondSut);
        var intersects2 = secondSut.Intersects(firstSut);

        // Asserts
        Assert.Equal(intersects, intersects1);
        Assert.Equal(intersects, intersects2);
    }

    #endregion

    #region Rectangle

    [Theory]
    [InlineData(false, _rectangle, "LINESTRING(0 1459, 1864 633)")]
    [InlineData(false, _rectangle, "LINESTRING(1986 0, 1012 482, 1019 1209, 276 932)")]
    [InlineData(false, _rectangle, "MULTILINESTRING((1776 0, 1019 896), (699 1008, 381 1008), (407 645, 613 253))")]
    [InlineData(false, _rectangle, "MULTILINESTRING((1550 1517, 1001 1001, 1008 1199), (239 420, 494 666))")]
    [InlineData(false, _rectangle, "MULTIPOLYGON(((1384 440, 816 1340, 1365 815, 1384 440)), ((981 1015, 288 1013, 749 1371, 981 1015)))")]
    [InlineData(false, _rectangle, "MULTIPOLYGON(((529 1015, 983 1015, 978 1080, 536 1077, 529 1015)), ((371 55, 1434 193, 1368 1373, 367 1364, 371 55), (462 444, 467 1104, 1045 1106, 1062 449, 462 444)))")]
    [InlineData(false, _rectangle, "MULTIPOLYGON(((491 468, 486 1010, 718 1013, 704 1024, 692 1025, 699 1029, 178 1474, 491 468)), ((1012 459, 1267 1390, 699 1029, 704 1024, 1004 1018, 1012 459)))")]

    [InlineData(true, _rectangle, "LINESTRING(320 762, 1438 1551)")]
    [InlineData(true, _rectangle, "LINESTRING(500 1000, 2011 1579)")]
    [InlineData(true, _rectangle, "LINESTRING(525 512, 965 513)")]
    [InlineData(true, _rectangle, "LINESTRING(720 734, 740 763)")]
    [InlineData(true, _rectangle, "MULTILINESTRING((116 410, 123 1000), (1000 791, 1301 429))")]
    [InlineData(true, _rectangle, "MULTILINESTRING((2441 677, 2181 1511), (630 911, 690 907))")]
    [InlineData(true, _rectangle, "LINESTRING(649 1000, 904 1006)")]
    [InlineData(true, _rectangle, "LINESTRING(500 1000, 500 1175)")]
    [InlineData(true, _rectangle, "LINESTRING(500 1000, 500 788)")]
    [InlineData(true, _rectangle, "LINESTRING(500 854, 480 1027, 1154 997, 986 421, 461 498)")]
    [InlineData(true, _rectangle, "POLYGON((500 1000, 500 1138, 353 1138, 353 1000, 500 1000))")]
    [InlineData(true, _rectangle, "POLYGON((500 1000, 711 1000, 500 843, 500 1000))")]
    [InlineData(true, _rectangle, "POLYGON((475 1026, 1053 1025, 1051 398, 479 477, 475 1026))")]
    [InlineData(true, _rectangle, "POLYGON((725 846, 801 851, 926 722, 725 846))")]
    [InlineData(true, _rectangle, "POLYGON((1293 247, 976 724, 1950 1577, 1293 247))")]
    [InlineData(true, _rectangle, "MULTIPOLYGON(((803 1233, 925 1553, 326 1632, 237 1622, 139 1517, 803 1233), (703 1374, 383 1505, 405 1558, 567 1527, 703 1374)), ((1519 931, 1413 1411, 1280 1242, 1077 1247, 1519 931)), ((1000 699, 1311 813, 1000 858, 1000 699)))")]
    [InlineData(true, _rectangle, "POLYGON((1000 770, 1353 914, 1391 565, 1000 770))")]
    [InlineData(true, _rectangle, "LINESTRING(953 453, 1008 509, 1037 417)")]
    [InlineData(true, _rectangle, "POLYGON((947 430, 1134 683, 923 0, 947 430))")]
    [InlineData(true, _rectangle, "POLYGON((458 -182, 1559 -39, 1413 1503, 238 1479, -153 715, 458 -182), (1000 500, 500 500, 500 1000, 1000 1000, 1000 500))")]
    [InlineData(true, _rectangle, "POLYGON((741 1000, 1000 1000, 1000 768, 1306 1216, 741 1000))")]
    public void IntersectsRectangle(bool intersects, string wktFirstGeometry, string wktSecondGeometry)
    {
        // Arrange

        var firstSut = Geometry<Point>.FromWkt(wktFirstGeometry, SridHelper.WebMercator);
        var secondSut = Geometry<Point>.FromWkt(wktSecondGeometry, SridHelper.WebMercator);

        // Act
        var intersects1 = firstSut.Intersects(secondSut);
        var intersects2 = secondSut.Intersects(firstSut);

        // Asserts
        Assert.Equal(intersects, intersects1);
        Assert.Equal(intersects, intersects2);
    }

    #endregion

    #region Triangle

    [Theory]
    [InlineData(false, _triangle, "LINESTRING(-631 480, 104 781)")]
    [InlineData(false, _triangle, "LINESTRING(-413 -184, -71 265, -189 477)")]
    [InlineData(false, _triangle, "MULTILINESTRING((-612 661, -340 469), (-168 437, 46 138))")]
    [InlineData(false, _triangle, "MULTIPOLYGON(((-239 601, -29 652, 108 503, -36 613, -239 601)), ((-154 1, 140 271, -147 517, -70 251, -154 1)))")]
    [InlineData(false, _triangle, "POLYGON((-750 117, 317 134, -315 874, -750 117), (-504 220, -258 681, -36 234, -504 220))")]
    [InlineData(false, _triangle, "LINESTRING(-269 662, 54 82, -563 195, -197 742)")]

    [InlineData(true, _triangle, "LINESTRING(-258 586, -102 277)")]
    [InlineData(true, _triangle, "LINESTRING(-624 26, -281 634, -243 540)")]
    [InlineData(true, _triangle, "MULTILINESTRING((-549 -101, 378 56), (-136 297, -285 418))")]
    [InlineData(true, _triangle, "LINESTRING(-501 195, -264 610, -164 398)")]
    [InlineData(true, _triangle, "POLYGON((-469 549, -194 588, -223 647, -472 629, -469 549))")]
    [InlineData(true, _triangle, "POLYGON((-292 507, -251 501, -233 450, -285 427, -292 507))")]
    [InlineData(true, _triangle, "MULTIPOLYGON(((166 290, 258 385, 430 365, 533 207, 166 290)), ((-368 268, -379 323, -306 323, -368 268)))")]
    [InlineData(true, _triangle, "POLYGON((-528 396, -282 664, 298 419, -34 22, -554 187, -528 396))")]
    [InlineData(true, _triangle, "MULTILINESTRING((-694 654, -509 -15, -214 244), (-86 257, -41 223))")]
    [InlineData(true, _triangle, "MULTILINESTRING((-103 474, -176 436), (-269 607, -425 377, -409 308))")]
    [InlineData(true, _triangle, "POLYGON((-86 257, -1 261, -39 188, -86 257))")]
    public void IntersectsTriangle(bool intersects, string wktFirstGeometry, string wktSecondGeometry)
    {
        // Arrange

        var firstSut = Geometry<Point>.FromWkt(wktFirstGeometry, SridHelper.WebMercator);
        var secondSut = Geometry<Point>.FromWkt(wktSecondGeometry, SridHelper.WebMercator);

        // Act
        var intersects1 = firstSut.Intersects(secondSut);
        var intersects2 = secondSut.Intersects(firstSut);

        // Asserts
        Assert.Equal(intersects, intersects1);
        Assert.Equal(intersects, intersects2);
    }

    #endregion

    #region MultiPolygon

    [Theory]
    [InlineData(false, _multiPolygon, "LINESTRING(51250 47936, 72347 55045)")]
    [InlineData(false, _multiPolygon, "LINESTRING(64550 40446, 73723 44191)")]
    [InlineData(false, _multiPolygon, "MULTILINESTRING((31988 35171, 49645 46561), (65773 41439, 72117 44344))")]
    [InlineData(false, _multiPolygon, "MULTILINESTRING((75557 39146, 73952 48242, 63709 40598, 74640 39375), (72423 31502, 93520 44191, 72270 57873, 51250 48624))")]
    [InlineData(false, _multiPolygon, "MULTILINESTRING((18917 65746, 39632 52370), (65850 41439, 70206 39605), (75098 34942, 102922 28216))")]
    [InlineData(false, _multiPolygon, "POLYGON((64932 40981, 73417 47172, 75557 39528, 64932 40981))")]
    [InlineData(false, _multiPolygon, "MULTIPOLYGON(((64168 40675, 68831 44115, 68754 40446, 64168 40675)), ((73417 46713, 75634 39146, 69977 43962, 73417 46713)))")]
    [InlineData(false, _multiPolygon, "MULTIPOLYGON(((75320 39681, 73646 47936, 63404 40446, 75320 39681)), ((29848 13005, 102998 22406, 94666 65288, 26026 66816, 29848 13005), (49492 31579, 34587 55733, 78997 60396, 89010 32496, 49492 31579)))")]
    [InlineData(false, _multiPolygon, "MULTIPOLYGON(((51327 47936, 65162 48166, 58588 50612, 51327 47936)), ((49721 46561, 49951 36089, 45670 43274, 49721 46561)), ((67531 42433, 74105 40446, 73952 44115, 72576 46255, 69671 45490, 67531 42433)))")]

    [InlineData(true, _multiPolygon, "LINESTRING(65348 47952, 78113 44894)")]
    [InlineData(true, _multiPolygon, "LINESTRING(76049 38932, 60838 42066)")]
    [InlineData(true, _multiPolygon, "LINESTRING(52124 50015, 46162 43213)")]
    [InlineData(true, _multiPolygon, "LINESTRING(40000 40000, 57551 40000)")]
    [InlineData(true, _multiPolygon, "LINESTRING(40735 51009, 43945 49175)")]
    [InlineData(true, _multiPolygon, "LINESTRING(56328 37174, 73679 44436)")]
    [InlineData(true, _multiPolygon, "MULTILINESTRING((50825 48716, 68787 63316), (40735 51086, 41805 65838))")]
    [InlineData(true, _multiPolygon, "MULTILINESTRING((27358 63698, 39741 52997), (55564 33658, 82852 23110), (68099 46270, 71692 49098))")]
    [InlineData(true, _multiPolygon, "MULTILINESTRING((64660 40690, 73297 43748), (41805 50398, 45092 48792))")]
    [InlineData(true, _multiPolygon, "LINESTRING(52468 48315, 77463 48391)")]
    [InlineData(true, _multiPolygon, "LINESTRING(50500 47500, 55869 52519)")]
    [InlineData(true, _multiPolygon, "LINESTRING(61733 40001, 72570 41890)")]
    [InlineData(true, _multiPolygon, "MULTILINESTRING((61733 40001, 64983 39603), (50500 35262, 61235 30645))")]
    [InlineData(true, _multiPolygon, "POLYGON((66628 41487, 70182 47029, 74042 46608, 66628 41487))")]
    [InlineData(true, _multiPolygon, "POLYGON((65328 40646, 59251 43704, 60436 36939, 65328 40646))")]
    [InlineData(true, _multiPolygon, "POLYGON((36129 57500, 42168 51615, 45760 62087, 36129 57500))")]
    [InlineData(true, _multiPolygon, "POLYGON((98502 10262, 48894 11256, 50500 35262, 98502 10262))")]
    [InlineData(true, _multiPolygon, "MULTIPOLYGON(((96897 32582, 82297 48252, 85813 50698, 96897 32582)), ((65634 40837, 61430 42442, 61200 38085, 65634 40837)))")]
    [InlineData(true, _multiPolygon, "MULTIPOLYGON(((41633 51385, 41250 48099, 44690 49627, 41633 51385)), ((73736 47334, 59901 51691, 52334 44965, 73736 47334)))")]
    [InlineData(true, _multiPolygon, "POLYGON((50500 47500, 51739 48649, 52723 48191, 50500 47500))")]
    [InlineData(true, _multiPolygon, "LINESTRING(50500 35262, 50500 32339)")]
    [InlineData(true, _multiPolygon, "POLYGON((83272 39363, 86034 56456, 74492 55462, 83272 39363))")]
    [InlineData(true, _multiPolygon, "MULTIPOLYGON(((53836 70397, 41147 74142, 36637 71926, 53836 70397)), ((99468 23464, 74550 58090, 50549 48689, 99468 23464), (96946 25681, 78983 37605, 53377 48230, 74015 56409, 96946 25681)))")]
    [InlineData(true, _multiPolygon, "MULTIPOLYGON(((50503 47950, 57122 61224, 55692 61033, 50503 47950)), ((42064 26675, 50503 47950, 42523 31949, 34803 58243, 55692 61033, 56434 62906, 31592 60613, 42064 26675)))")]
    [InlineData(true, _multiPolygon, "POLYGON((36484 18725, 107494 44561, 103367 65428, 33350 62753, 36484 18725), (47185 25681, 43134 57402, 83417 59161, 95494 41351, 47185 25681))")]
    [InlineData(true, _multiPolygon, "POLYGON((74421 49977, 70930 46814, 74704 46738, 74421 49977))")]
    public void IntersectsMultiPolygon(bool intersects, string wktFirstGeometry, string wktSecondGeometry)
    {
        // Arrange

        var firstSut = Geometry<Point>.FromWkt(wktFirstGeometry, SridHelper.WebMercator);
        var secondSut = Geometry<Point>.FromWkt(wktSecondGeometry, SridHelper.WebMercator);

        // Act
        var intersects1 = firstSut.Intersects(secondSut);
        var intersects2 = secondSut.Intersects(firstSut);

        // Asserts
        Assert.Equal(intersects, intersects1);
        Assert.Equal(intersects, intersects2);
    }

    #endregion

    public static List<object[]> IntersectsOrthogonalLineData()
    {
        return new List<object[]> { };
    }

}
