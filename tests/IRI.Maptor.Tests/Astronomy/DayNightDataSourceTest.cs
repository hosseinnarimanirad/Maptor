using System.Linq;
using IRI.Maptor.Core.Astronomy.Solar;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Persistence.DataSources;
using IRI.Maptor.Core.SpatialReferenceSystem;

namespace IRI.Maptor.Tests.Astronomy;

public class DayNightDataSourceTest
{
    private const double MaxWebMercatorLatitude = 85.05;

    private static bool IsInBand(DayNightDataSource source, DayNightBand band, Point geographic)
    {
        var webMercator = MapProjects.GeodeticWgs84ToWebMercator(geographic);

        return source.GetFeatures()
            .Where(f => (string)f.Attributes[DayNightDataSource.KindFieldName] == band.ToString())
            .Any(f => SphericalCapHelperTest.IsInside(f.TheGeometry.Geometries[0].Points, webMercator.X, webMercator.Y));
    }

    [Theory]
    [InlineData(2026, 3, 20, 14, 46)]
    [InlineData(2026, 6, 21, 8, 24)]
    [InlineData(2026, 10, 28, 20, 0)]
    [InlineData(2026, 12, 21, 3, 30)]
    public void EveryBand_HoldsExactlyThePlacesWhereTheSunIsBelowItsLimit(int year, int month, int day, int hour, int minute)
    {
        var time = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Utc);

        var source = DayNightDataSource.Create(time);

        var features = source.GetFeatures();

        var random = new Random(42);

        foreach (var band in DayNightDataSource.AllBands)
        {
            var limit = DayNightDataSource.GetSunElevationLimit(band);

            var rings = features
                .Where(f => (string)f.Attributes[DayNightDataSource.KindFieldName] == band.ToString())
                .Select(f => f.TheGeometry.Geometries[0].Points)
                .ToList();

            int checkedPoints = 0;

            for (int i = 0; i < 1500; i++)
            {
                var place = new Point(random.NextDouble() * 360 - 180, (random.NextDouble() * 2 - 1) * MaxWebMercatorLatitude);

                var elevation = Sun.GetHorizontal(place, time).Elevation;

                if (Math.Abs(elevation - limit) < 0.5)
                    continue;

                checkedPoints++;

                var webMercator = MapProjects.GeodeticWgs84ToWebMercator(place);

                Assert.True(elevation < limit == rings.Any(r => SphericalCapHelperTest.IsInside(r, webMercator.X, webMercator.Y)),
                    FormattableString.Invariant($"{band} at ({place.X:F2}, {place.Y:F2}): Sun at {elevation:F2}°"));
            }

            Assert.True(checkedPoints > 1300);
        }
    }

    [Fact]
    public void Tehran_IsInDaylightAtNoonAndInFullNightAtMidnight()
    {
        var tehran = new Point(51.389, 35.689);

        var noon = DayNightDataSource.Create(new DateTime(2026, 6, 21, 8, 36, 0, DateTimeKind.Utc));

        Assert.All(DayNightDataSource.AllBands, band => Assert.False(IsInBand(noon, band, tehran)));

        var midnight = DayNightDataSource.Create(new DateTime(2026, 6, 21, 20, 36, 0, DateTimeKind.Utc));

        Assert.All(DayNightDataSource.AllBands, band => Assert.True(IsInBand(midnight, band, tehran)));
    }

    [Fact]
    public void Features_AreWidestBandFirst_AllPolygonsInWebMercator()
    {
        var features = DayNightDataSource.Create(new DateTime(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc)).GetFeatures();

        var kinds = features.Select(f => (string)f.Attributes[DayNightDataSource.KindFieldName]).Distinct().ToList();

        Assert.Equal(DayNightDataSource.AllBands.Select(b => b.ToString()), kinds);

        Assert.All(features, f =>
        {
            Assert.Equal(GeometryType.Polygon, f.TheGeometry.Type);
            Assert.Equal(SridHelper.WebMercator, f.TheGeometry.Srid);
        });
    }

    [Fact]
    public void SubSolarPoint_IsAddedOnlyWhenAsked()
    {
        var time = new DateTime(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc);

        var source = DayNightDataSource.Create(time);

        Assert.DoesNotContain(source.GetFeatures(), IsSubSolar);

        source.IncludeSubSolarPoint = true;

        var point = Assert.Single(source.GetFeatures(), IsSubSolar);

        Assert.Equal(GeometryType.Point, point.TheGeometry.Type);

        var expected = MapProjects.GeodeticWgs84ToWebMercator(Sun.GetSubSolarPoint(time));

        Assert.Equal(expected.X, point.TheGeometry.Points[0].X, 3);
        Assert.Equal(expected.Y, point.TheGeometry.Points[0].Y, 3);

        static bool IsSubSolar(Core.Spatial.Primitives.Feature<Point> f)
            => (string)f.Attributes[DayNightDataSource.KindFieldName] == DayNightDataSource.SubSolarPointKind;
    }

    [Fact]
    public void Bands_LimitWhatIsDrawn()
    {
        var source = DayNightDataSource.Create(new DateTime(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc));

        source.Bands = new[] { DayNightBand.BelowAstronomicalTwilight };

        Assert.All(source.GetFeatures(), f =>
            Assert.Equal(nameof(DayNightBand.BelowAstronomicalTwilight), f.Attributes[DayNightDataSource.KindFieldName]));
    }

    [Fact]
    public void Time_IsStoredAsUtc()
    {
        var source = new DayNightDataSource { Time = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Unspecified) };

        Assert.Equal(DateTimeKind.Utc, source.Time.Kind);
        Assert.Equal(12, source.Time.Hour);
    }
}
