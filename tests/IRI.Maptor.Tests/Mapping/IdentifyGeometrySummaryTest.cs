using System.Collections.Generic;
using System.Linq;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Helpers;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Presentation.Wpf.Models.Identify;

using Xunit;

namespace IRI.Maptor.Tests.Mapping;

/// <summary>The geometry strip above an identified feature's attributes: measures and counts per geometry type.</summary>
public class IdentifyGeometrySummaryTest
{
    private const int Wgs84 = SridHelper.GeodeticWGS84;

    // rings are never closed: the first point is not repeated
    private static List<Point> Square(double x, double y, double size) =>
        [new Point(x, y), new Point(x + size, y), new Point(x + size, y + size), new Point(x, y + size)];

    private static Geometry<Point> Ring(List<Point> points) => Geometry<Point>.CreatePolygonRing(points, Wgs84);

    private static Geometry<Point> Polygon(params List<Point>[] rings) =>
        Geometry<Point>.Create(rings.Select(Ring).ToList(), GeometryType.Polygon, Wgs84);

    [Fact]
    public void Point_ShowsNothing()
    {
        var summary = IdentifyGeometrySummary.Create(Geometry<Point>.Create(51.4, 35.7, Wgs84));

        Assert.NotNull(summary);
        Assert.False(summary.HasAny);
    }

    [Fact]
    public void NullOrEmpty_IsNull()
    {
        Assert.Null(IdentifyGeometrySummary.Create(null));
        Assert.Null(IdentifyGeometrySummary.Create(Geometry<Point>.CreateEmpty(GeometryType.Polygon, Wgs84)));
    }

    [Fact]
    public void MultiPoint_ShowsPointsOnly()
    {
        var summary = IdentifyGeometrySummary.Create(Geometry<Point>.Create([new Point(1, 1), new Point(2, 2), new Point(3, 3)], GeometryType.MultiPoint, Wgs84))!;

        Assert.True(summary.ShowPoints);
        Assert.Equal(3, summary.NumberOfPoints);
        Assert.False(summary.ShowParts);
        Assert.False(summary.ShowLength);
        Assert.False(summary.ShowArea);
    }

    [Fact]
    public void LineString_HasLengthAndPoints_NoParts()
    {
        // 0.01° of longitude on the equator ≈ 1113.2 m on WGS84
        var line = Geometry<Point>.Create([new Point(0, 0), new Point(0.005, 0), new Point(0.01, 0)], GeometryType.LineString, Wgs84);

        var summary = IdentifyGeometrySummary.Create(line)!;

        Assert.Equal(1113.19, summary.Length!.Value, 0);
        Assert.Equal(3, summary.NumberOfPoints);
        Assert.Equal(1, summary.NumberOfParts);
        Assert.False(summary.ShowParts);
        Assert.False(summary.ShowArea);
        Assert.False(summary.ShowPerimeter);
    }

    [Fact]
    public void MultiLineString_CountsParts_AndSumsLength()
    {
        var a = Geometry<Point>.Create([new Point(0, 0), new Point(0.01, 0)], GeometryType.LineString, Wgs84);
        var b = Geometry<Point>.Create([new Point(0, 1), new Point(0.01, 1), new Point(0.02, 1)], GeometryType.LineString, Wgs84);

        var multi = Geometry<Point>.Create([a, b], GeometryType.MultiLineString, Wgs84);

        var summary = IdentifyGeometrySummary.Create(multi)!;

        Assert.Equal(2, summary.NumberOfParts);
        Assert.True(summary.ShowParts);
        Assert.Equal(5, summary.NumberOfPoints);
        Assert.Equal(IdentifyGeometrySummary.Create(a)!.Length!.Value + IdentifyGeometrySummary.Create(b)!.Length!.Value, summary.Length!.Value, 6);
    }

    [Fact]
    public void Polygon_WithHole_SubtractsHoleArea_AndPerimeterIncludesHole()
    {
        var outer = Square(0, 0, 0.01);
        var hole = Square(0.003, 0.003, 0.004);

        var whole = IdentifyGeometrySummary.Create(Polygon(outer))!;
        var holeOnly = IdentifyGeometrySummary.Create(Polygon(hole))!;
        var withHole = IdentifyGeometrySummary.Create(Polygon(outer, hole))!;

        Assert.Equal(whole.Area!.Value - holeOnly.Area!.Value, withHole.Area!.Value, 3);
        Assert.Equal(whole.Perimeter!.Value + holeOnly.Perimeter!.Value, withHole.Perimeter!.Value, 6);

        Assert.Equal(8, withHole.NumberOfPoints);
        Assert.Equal(1, withHole.NumberOfParts);
        Assert.False(withHole.ShowParts);
        Assert.Equal(1, withHole.NumberOfHoles);
        Assert.True(withHole.ShowHoles);
        Assert.False(withHole.ShowLength);
    }

    [Fact]
    public void MultiPolygon_CountsPolygonsAsParts_AndHolesSeparately()
    {
        var first = Polygon(Square(0, 0, 0.01), Square(0.003, 0.003, 0.004));
        var second = Polygon(Square(1, 1, 0.01));

        var summary = IdentifyGeometrySummary.Create(Geometry<Point>.Create([first, second], GeometryType.MultiPolygon, Wgs84))!;

        Assert.Equal(2, summary.NumberOfParts);
        Assert.Equal(1, summary.NumberOfHoles);
        Assert.Equal(12, summary.NumberOfPoints);
        Assert.True(summary.Area > 0);
        Assert.True(summary.Perimeter > 0);
    }

    [Fact]
    public void WebMercatorPolygon_MeasuresLikeItsWgs84Copy()
    {
        var wgs84 = Polygon(Square(51.40, 35.70, 0.01));
        var webMercator = wgs84.Transform(MapProjects.GeodeticWgs84ToWebMercator, SridHelper.WebMercator);

        var a = IdentifyGeometrySummary.Create(wgs84)!;
        var b = IdentifyGeometrySummary.Create(webMercator)!;

        Assert.Equal(a.Area!.Value, b.Area!.Value, 0);
        Assert.Equal(a.Perimeter!.Value, b.Perimeter!.Value, 2);
    }

    [Theory]
    [InlineData(MeasureDisplayUnit.Meter, "1,500 m", "2,000,000 m\xB2")]
    [InlineData(MeasureDisplayUnit.Kilometer, "1.5 km", "2 km\xB2")]
    // feet and miles through UnitConversion, i.e. the US survey foot (1200/3937 m)
    [InlineData(MeasureDisplayUnit.Foot, "4,921.25 ft", "21,527,734.72 ft\xB2")]
    [InlineData(MeasureDisplayUnit.Mile, "0.9321 mi", "0.7722 mi\xB2")]
    public void OneUnit_DrivesLengthAndItsSquareForArea(MeasureDisplayUnit unit, string length, string area)
    {
        Assert.Equal(length, MeasureDisplayFormatter.FormatLength(1500, unit));
        Assert.Equal(area, MeasureDisplayFormatter.FormatArea(2e6, unit));
    }

    [Fact]
    public void AutoUnit_IsTheSizeBasedLabel()
    {
        Assert.Equal(UnitHelper.GetLengthLabel(1500), MeasureDisplayFormatter.FormatLength(1500, MeasureDisplayUnit.Auto));
        Assert.Equal(UnitHelper.GetAreaLabel(350), MeasureDisplayFormatter.FormatArea(350, MeasureDisplayUnit.Auto));
    }

    [Fact]
    public void SmallValueInALargeUnit_DoesNotReadZero()
    {
        // a 350 m² parcel in mi²
        Assert.Equal("0.00013514 mi\xB2", MeasureDisplayFormatter.FormatArea(350, MeasureDisplayUnit.Mile));
    }

    [Fact]
    public void UnitLabels_UseLowerCaseKm()
    {
        Assert.Equal("1.5 km", UnitHelper.GetLengthLabel(1500));
        Assert.Equal("2.0 km\xB2", UnitHelper.GetAreaLabel(2e6));
    }
}
