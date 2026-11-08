using System.Collections.Generic;
using System.Linq;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Spatial.Validation;

namespace IRI.Maptor.Tests.Spatial;

/// <summary>
/// GeometryValidator / GeometryRepair: problems found in features read from files, and the features each user
/// choice ("fix", "leave out invalid", "add as is") produces. Synthetic coordinates only.
/// </summary>
public class GeometryValidatorTest
{
    private const int Geodetic = 4326;

    private const int WebMercator = 3857;

    // rings are built as plain point lists so a test can keep a repeated closing point
    private static Geometry<Point> Ring(params (double x, double y)[] points)
        => Geometry<Point>.Create(points.Select(p => new Point(p.x, p.y)).ToList(), GeometryType.LineString, Geodetic);

    private static Geometry<Point> Polygon(params Geometry<Point>[] rings)
        => Geometry<Point>.Create(rings.ToList(), GeometryType.Polygon, Geodetic);

    private static Feature<Point> Feature(Geometry<Point> geometry, int id = 0)
        => new(geometry, new Dictionary<string, object> { ["name"] = $"f{id}" }) { Id = id };

    // counterclockwise 10 x 10 square
    private static Geometry<Point> Square() => Ring((0, 0), (10, 0), (10, 10), (0, 10));

    // clockwise 2 x 2 hole inside Square()
    private static Geometry<Point> Hole() => Ring((4, 4), (4, 6), (6, 6), (6, 4));

    private static List<GeometryIssueKind> Kinds(GeometryValidationReport<Point> report) => report.Issues.Select(i => i.Kind).ToList();

    [Fact]
    public void ValidPolygon_IsClean_AndEveryChoiceReturnsTheSameFeature()
    {
        var feature = Feature(Polygon(Square(), Hole()));

        var report = GeometryValidator.Validate(new[] { feature });

        Assert.True(report.IsClean);
        Assert.False(report.NeedsUserDecision);
        Assert.Same(feature, GeometryRepair.Fix(report).Single());
        Assert.Same(feature, GeometryRepair.LeaveOutInvalid(report).Single());
        Assert.Same(feature, GeometryRepair.KeepAsIs(report).Single());
    }

    [Fact]
    public void ClosingPointRepeatedTwice_IsReportedAndRemoved_InputUnchanged()
    {
        var feature = Feature(Polygon(Ring((0, 0), (10, 0), (10, 10), (0, 10), (0, 0), (0, 0))));

        var report = GeometryValidator.Validate(new[] { feature });

        Assert.Equal(new[] { GeometryIssueKind.ClosedRing }, Kinds(report));
        Assert.True(report.NeedsUserDecision);
        Assert.Equal(GeometryRepairOutcome.Repaired, report.ProblemFeatures.Single().Outcome);
        Assert.Equal(0, report.Issues.Single().RingIndex);
        Assert.Null(report.Issues.Single().PartIndex);

        var fixedFeature = GeometryRepair.Fix(report).Single();

        Assert.NotSame(feature, fixedFeature);
        Assert.Equal(feature.Id, fixedFeature.Id);
        Assert.Equal(feature.Key, fixedFeature.Key);
        Assert.Same(feature.Attributes, fixedFeature.Attributes);
        Assert.Equal(4, fixedFeature.TheGeometry.Geometries[0].Points.Count);
        Assert.Equal(100, fixedFeature.TheGeometry.EuclideanArea, 6);

        // the read geometry is not touched
        Assert.Equal(6, feature.TheGeometry.Geometries[0].Points.Count);
    }

    [Fact]
    public void RepeatedPointInLine_KeepsOneCopy()
    {
        var line = Geometry<Point>.Create(
            new List<Point> { new(0, 0), new(1, 1), new(1, 1), new(1, 1), new(2, 0) }, GeometryType.LineString, Geodetic);

        var report = GeometryValidator.Validate(new[] { Feature(line) });

        Assert.Equal(new[] { GeometryIssueKind.RepeatedPoint }, Kinds(report));

        var points = GeometryRepair.Fix(report).Single().TheGeometry.Points;

        Assert.Equal(3, points.Count);
        Assert.Equal(new[] { 0d, 1, 2 }, points.Select(p => p.X));
    }

    [Fact]
    public void LineOfOneRepeatedPoint_IsRemoved()
    {
        var line = Geometry<Point>.Create(new List<Point> { new(5, 5), new(5, 5) }, GeometryType.LineString, Geodetic);

        var report = GeometryValidator.Validate(new[] { Feature(line) });

        Assert.Equal(new[] { GeometryIssueKind.RepeatedPoint, GeometryIssueKind.TooFewPoints }, Kinds(report));
        Assert.Equal(GeometryRepairOutcome.Removed, report.ProblemFeatures.Single().Outcome);
        Assert.Empty(GeometryRepair.Fix(report));
    }

    [Fact]
    public void OuterRingWithTooFewPoints_RemovesTheFeature_ButAddAsIsKeepsIt()
    {
        var feature = Feature(Polygon(Ring((0, 0), (10, 0), (0, 0))));

        var report = GeometryValidator.Validate(new[] { feature });

        Assert.Equal(new[] { GeometryIssueKind.ClosedRing, GeometryIssueKind.TooFewPoints }, Kinds(report));
        Assert.Equal(GeometryRepairOutcome.Removed, report.ProblemFeatures.Single().Outcome);
        Assert.Empty(GeometryRepair.Fix(report));
        Assert.Empty(GeometryRepair.LeaveOutInvalid(report));
        Assert.Same(feature, GeometryRepair.KeepAsIs(report).Single());
    }

    [Fact]
    public void ZeroAreaRing_IsRemoved()
    {
        var report = GeometryValidator.Validate(new[] { Feature(Polygon(Ring((0, 0), (5, 5), (10, 10)))) });

        Assert.Equal(new[] { GeometryIssueKind.ZeroAreaRing }, Kinds(report));
        Assert.Empty(GeometryRepair.Fix(report));
    }

    [Fact]
    public void BrokenHole_IsDropped_PolygonKept()
    {
        var report = GeometryValidator.Validate(new[] { Feature(Polygon(Square(), Ring((4, 4), (6, 6)))) });

        var issue = report.Issues.Single();

        Assert.Equal(GeometryIssueKind.TooFewPoints, issue.Kind);
        Assert.Equal(1, issue.RingIndex);

        var polygon = GeometryRepair.Fix(report).Single().TheGeometry;

        Assert.Single(polygon.Geometries);
        Assert.Equal(100, polygon.EuclideanArea, 6);
    }

    [Fact]
    public void NaNCoordinate_CannotBeRepaired()
    {
        var feature = Feature(Polygon(Ring((0, 0), (10, double.NaN), (10, 10), (0, 10))));

        var report = GeometryValidator.Validate(new[] { feature, Feature(Polygon(Square()), 1) });

        var problem = report.ProblemFeatures.Single();

        Assert.Equal(GeometryIssueKind.InvalidCoordinate, problem.Issues.Single().Kind);
        Assert.False(problem.Issues.Single().CanFix);
        Assert.Equal(GeometryRepairOutcome.Unrepairable, problem.Outcome);

        Assert.Equal(new[] { 1 }, GeometryRepair.Fix(report).Select(f => f.Id));
        Assert.Equal(new[] { 1 }, GeometryRepair.LeaveOutInvalid(report).Select(f => f.Id));
        Assert.Equal(new[] { 0, 1 }, GeometryRepair.KeepAsIs(report).Select(f => f.Id));
    }

    [Fact]
    public void OutOfRangeLongitude_IsReportedOnlyForGeodeticGeometries()
    {
        var points = new List<Point> { new(200, 10) };

        var geodetic = GeometryValidator.Validate(new[] { Feature(Geometry<Point>.Create(points, GeometryType.Point, Geodetic)) });

        var mercator = GeometryValidator.Validate(new[] { Feature(Geometry<Point>.Create(points, GeometryType.Point, WebMercator)) });

        Assert.Equal(new[] { GeometryIssueKind.CoordinateOutOfRange }, Kinds(geodetic));
        Assert.Equal(GeometryRepairOutcome.Unrepairable, geodetic.ProblemFeatures.Single().Outcome);
        Assert.True(mercator.IsClean);
    }

    [Fact]
    public void ClockwiseOuterRing_IsFixedAutomatically_InEveryChoice()
    {
        var feature = Feature(Polygon(Ring((0, 0), (0, 10), (10, 10), (10, 0))));

        var report = GeometryValidator.Validate(new[] { feature });

        Assert.Equal(new[] { GeometryIssueKind.WrongRingOrientation }, Kinds(report));
        Assert.True(report.HasAutomaticFixesOnly);
        Assert.False(report.NeedsUserDecision);
        Assert.Equal(0, report.InvalidFeatureCount);
        Assert.Equal(1, report.AutomaticallyFixedFeatureCount);

        foreach (var features in new[] { GeometryRepair.Fix(report), GeometryRepair.LeaveOutInvalid(report), GeometryRepair.KeepAsIs(report) })
        {
            var ring = features.Single().TheGeometry.Geometries[0].Points;

            Assert.False(SpatialUtility.IsClockwise(ring));
        }

        Assert.True(SpatialUtility.IsClockwise(feature.TheGeometry.Geometries[0].Points));
    }

    [Fact]
    public void CounterclockwiseHole_IsReversed()
    {
        var hole = Ring((4, 4), (6, 4), (6, 6), (4, 6));

        var report = GeometryValidator.Validate(new[] { Feature(Polygon(Square(), hole)) });

        var issue = report.Issues.Single();

        Assert.Equal(GeometryIssueKind.WrongRingOrientation, issue.Kind);
        Assert.Equal(1, issue.RingIndex);
        Assert.True(SpatialUtility.IsClockwise(GeometryRepair.Fix(report).Single().TheGeometry.Geometries[1].Points));
    }

    [Fact]
    public void MultiPolygon_BrokenPartIsDropped_AndReportedByPart()
    {
        var multiPolygon = Geometry<Point>.Create(
            new List<Geometry<Point>> { Polygon(Square()), Polygon(Ring((20, 20), (30, 20), (20, 20))) },
            GeometryType.MultiPolygon,
            Geodetic);

        var report = GeometryValidator.Validate(new[] { Feature(multiPolygon) });

        Assert.All(report.Issues, i => Assert.Equal(1, i.PartIndex));
        Assert.All(report.Issues, i => Assert.Equal(0, i.RingIndex));

        var repaired = GeometryRepair.Fix(report).Single().TheGeometry;

        Assert.Equal(GeometryType.MultiPolygon, repaired.Type);
        Assert.Single(repaired.Geometries);
        Assert.Equal(100, repaired.EuclideanArea, 6);
    }

    [Fact]
    public void FeatureWithoutGeometry_CannotBeRepaired()
    {
        var report = GeometryValidator.Validate(new[] { new Feature<Point> { Id = 7 } });

        Assert.Equal(new[] { GeometryIssueKind.NoGeometry }, Kinds(report));
        Assert.Equal(GeometryRepairOutcome.Unrepairable, report.ProblemFeatures.Single().Outcome);
        Assert.Empty(GeometryRepair.Fix(report));
    }

    [Fact]
    public void Report_CountsByKindAndFeature_AndKeepsTheOrder()
    {
        var features = new[]
        {
            Feature(Polygon(Square()), 0),
            Feature(Polygon(Ring((0, 0), (10, 0), (10, 10), (0, 10), (0, 0))), 1),
            Feature(Polygon(Ring((0, 0), (0, 10), (10, 10), (10, 0))), 2),
            Feature(Polygon(Ring((0, 0), (10, 0), (10, 10), (0, 10), (0, 0))), 3),
        };

        var report = GeometryValidator.Validate(features);

        Assert.Equal(2, report.CountByKind[GeometryIssueKind.ClosedRing]);
        Assert.Equal(1, report.CountByKind[GeometryIssueKind.WrongRingOrientation]);
        Assert.Equal(2, report.InvalidFeatureCount);
        Assert.Equal(1, report.AutomaticallyFixedFeatureCount);
        Assert.Equal(new[] { 1, 2, 3 }, report.ProblemFeatures.Select(p => p.FeatureIndex));

        Assert.Equal(new[] { 0, 1, 2, 3 }, GeometryRepair.Fix(report).Select(f => f.Id));
        Assert.Equal(new[] { 0, 2 }, GeometryRepair.LeaveOutInvalid(report).Select(f => f.Id));
    }
}
