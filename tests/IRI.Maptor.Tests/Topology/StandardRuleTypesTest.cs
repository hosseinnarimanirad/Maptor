using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Analysis.PointsOnLines;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Topology.Checking;
using IRI.Maptor.Core.Topology.Findings;
using IRI.Maptor.Core.Topology.Rules;
using IRI.Maptor.Core.Topology.RuleTypes;

using Xunit;

namespace IRI.Maptor.Tests.Topology;

public class StandardRuleTypesTest
{
    private const string Tower = "Tower";
    private const string Route = "Route";
    private const string Number = "num";

    #region Synthetic network (projected, metres)

    private static Feature<Point> Line(int id, string wkt, int srid = 0) => new(Geometry<Point>.FromWkt(wkt, srid)) { Id = id };

    private static Feature<Point> TowerAt(int id, double x, double y, long? number = null, int srid = 0)
    {
        var wkt = string.Format(CultureInfo.InvariantCulture, "POINT({0} {1})", x, y);
        var attributes = new Dictionary<string, object>();

        if (number is not null)
            attributes[Number] = number.Value;

        return new Feature<Point>(Geometry<Point>.FromWkt(wkt, srid), attributes) { Id = id };
    }

    /// <summary>A network with at least one case of every points-on-lines finding.</summary>
    private static (List<Feature<Point>> Routes, List<Feature<Point>> Towers) Network()
    {
        var routes = new List<Feature<Point>>
        {
            Line(10, "LINESTRING(0 0, 1000 0)"),          // numbers 1 2 4 3 6: one out of order, a gap
            Line(20, "LINESTRING(1000 0, 1000 1000)"),    // shares its first tower with route 10; a missing and a repeated number
            Line(30, "LINESTRING(0 500, 300 500)"),       // one tower only: too few points, both ends open
            Line(40, "LINESTRING(0 2000, 300 2000)"),     // numbers 2 1 2: direction undecided
            Line(50, "LINESTRING(0 3000, 300 3000)"),     // two towers, the end at 300 open
        };

        var towers = new List<Feature<Point>>
        {
            TowerAt(1, 0, 0, 1), TowerAt(2, 250, 0.2, 2), TowerAt(3, 500, 0, 4), TowerAt(4, 750, 0, 3), TowerAt(5, 900, 0, 6),
            TowerAt(6, 1000, 0, 7),                                                       // junction of 10 and 20
            TowerAt(7, 1000, 300), TowerAt(8, 1000, 600, 9), TowerAt(9, 1000, 1000, 9),   // missing, repeated
            TowerAt(10, 150, 500, 1),                                                    // alone on route 30
            TowerAt(11, 0, 2000, 2), TowerAt(12, 150, 2000, 1), TowerAt(13, 300, 2000, 2),
            TowerAt(14, 500, 20, 5),                                                     // 20 m off route 10
            TowerAt(15, 5000, 5000, 5),                                                  // far from everything
            TowerAt(16, 0, 3000, 1), TowerAt(17, 150, 3000, 2),
        };

        return (routes, towers);
    }

    private static TopologyRuleSet NetworkRules(params TopologyRule[] extra)
    {
        var rules = new List<TopologyRule>
        {
            new("t.onRoute", PointOnLineRuleType.TypeId, Tower, Route),
            new("t.routeEnds", LineEndCoveredRuleType.TypeId, Route, Tower),
            new("t.routeTowers", LineCarriesPointsRuleType.TypeId, Route, Tower),
            new("t.junction", PointOnOneLineRuleType.TypeId, Tower, Route),
            new("t.numbers", SequenceAlongLineRuleType.TypeId, Tower, Route,
                new RuleParameters(new Dictionary<string, object> { ["sequenceField"] = Number })),
        };

        rules.AddRange(extra);

        return new TopologyRuleSet("t", 1, new[] { new TopologyLayer(Tower, GeometryType.Point), new TopologyLayer(Route, GeometryType.LineString) }, rules);
    }

    private static Task<TopologyCheckResult> CheckAsync(TopologyRuleSet ruleSet, List<Feature<Point>> routes, List<Feature<Point>> towers, int srid = 0)
        => new ValidationRunner(StandardRuleTypes.CreateCatalog()).RunAsync(ruleSet, TopologyCheckScope.WholeData, new ITopologyFeatureSource[]
        {
            new FeatureListSource(Route, srid, routes),
            new FeatureListSource(Tower, srid, towers),
        });

    #endregion

    [Fact]
    public async Task Engine_GivesTheSameCountsAsTheBuilderDirectly()
    {
        var (routes, towers) = Network();

        var direct = new PointLineTopologyBuilder<Point>(new PointLineTopologyOptions { Tolerance = 0.5, CoincidentTolerance = 0.01 })
            .Build(
                routes.Select(r => r.TheGeometry).ToList(),
                towers.Select(t => t.TheGeometry.Points![0]).ToList(),
                towers.Select(t => t.Attributes!.TryGetValue(Number, out var n) ? (long?)n : null).ToList());

        var result = await CheckAsync(NetworkRules(), routes, towers);

        int Engine(params string[] kinds) => result.Issues.Count(i => kinds.Contains(i.Kind));
        int Builder(PointLineIssueCode code) => direct.Issues.Count(i => i.Code == code);

        var pairs = new (PointLineIssueCode Code, int Engine)[]
        {
            (PointLineIssueCode.OrphanPoint, Engine(PointOnLineRuleType.NearLine, PointOnLineRuleType.FarFromLines)),
            (PointLineIssueCode.OpenLineEnd, Engine(LineEndCoveredRuleType.Start, LineEndCoveredRuleType.End)),
            (PointLineIssueCode.LineWithoutPoints, Engine(LineCarriesPointsRuleType.TooFewPoints)),
            (PointLineIssueCode.SharedPoint, Engine(PointOnOneLineRuleType.OnSeveralLines)),
            (PointLineIssueCode.MissingSequence, Engine(SequenceAlongLineRuleType.Missing)),
            (PointLineIssueCode.DuplicateSequence, Engine(SequenceAlongLineRuleType.Duplicate)),
            (PointLineIssueCode.SequenceOutOfOrder, Engine(SequenceAlongLineRuleType.OutOfOrder)),
            (PointLineIssueCode.SequenceGap, Engine(SequenceAlongLineRuleType.Gap)),
            (PointLineIssueCode.DirectionUndecided, Engine(SequenceAlongLineRuleType.DirectionUndecided)),
        };

        foreach (var (code, engine) in pairs)
        {
            Assert.True(Builder(code) > 0, $"the network has no {code}");
            Assert.Equal(Builder(code), engine);
        }
    }

    [Fact]
    public async Task PointOnLine_NamesTheNearestLine_AndItsDistance()
    {
        var (routes, towers) = Network();

        var result = await CheckAsync(NetworkRules(), routes, towers);

        var near = Assert.Single(result.Issues, i => i.Kind == PointOnLineRuleType.NearLine);

        Assert.Equal(new[] { "Tower:14", "Route:10" }, near.Finding.Features.Select(f => f.ToString()).ToArray());
        Assert.Equal(20, near.Finding.Value!.Value, 6);
        Assert.Equal(ValueUnit.Metres, near.Finding.ValueUnit);

        var far = Assert.Single(result.Issues, i => i.Kind == PointOnLineRuleType.FarFromLines);

        Assert.Equal("Tower:15", Assert.Single(far.Finding.Features).ToString());
    }

    [Fact]
    public async Task Junction_CanBeHiddenByTheRuleSet()
    {
        var (routes, towers) = Network();

        var ruleSet = NetworkRules();
        ruleSet = ruleSet.WithRule(ruleSet.FindRule("t.junction")!.WithHidden(PointOnOneLineRuleType.OnSeveralLines, true));

        var result = await CheckAsync(ruleSet, routes, towers);

        Assert.DoesNotContain(result.Issues, i => i.Kind == PointOnOneLineRuleType.OnSeveralLines);
        Assert.Contains(result.Issues, i => i.Kind == PointOnLineRuleType.NearLine);
    }

    [Fact]
    public async Task RuleTypeSeverities_FollowTheDesign()
    {
        var (routes, towers) = Network();

        var result = await CheckAsync(NetworkRules(), routes, towers);

        IssueSeverity SeverityOf(string kind) => result.Issues.First(i => i.Kind == kind).Severity;

        Assert.Equal(IssueSeverity.Error, SeverityOf(PointOnLineRuleType.NearLine));
        Assert.Equal(IssueSeverity.Warning, SeverityOf(LineEndCoveredRuleType.End));
        Assert.Equal(IssueSeverity.Info, SeverityOf(PointOnOneLineRuleType.OnSeveralLines));
        Assert.Equal(IssueSeverity.Error, SeverityOf(SequenceAlongLineRuleType.OutOfOrder));
        Assert.Equal(IssueSeverity.Info, SeverityOf(SequenceAlongLineRuleType.Gap));
    }

    [Fact]
    public async Task LineEnds_WherePartsMeet_AreNotChecked_UnlessAskedFor()
    {
        var routes = new List<Feature<Point>> { Line(1, "MULTILINESTRING((0 0, 100 0), (100 0, 200 0))") };
        var towers = new List<Feature<Point>> { TowerAt(1, 0, 0), TowerAt(2, 200, 0) };

        var layers = new[] { new TopologyLayer(Tower, GeometryType.Point), new TopologyLayer(Route, GeometryType.MultiLineString) };

        var plain = new TopologyRuleSet("t", 1, layers, new[] { new TopologyRule("ends", LineEndCoveredRuleType.TypeId, Route, Tower) });
        var interior = new TopologyRuleSet("t", 1, layers, new[]
        {
            new TopologyRule("ends", LineEndCoveredRuleType.TypeId, Route, Tower,
                new RuleParameters(new Dictionary<string, object> { [LineEndCoveredRuleType.InteriorPartEndsParameter] = 1 })),
        });

        Assert.Empty((await CheckAsync(plain, routes, towers)).Issues);
        Assert.Equal(2, (await CheckAsync(interior, routes, towers)).Issues.Count);
    }

    [Fact]
    public async Task LineCarriesPoints_UsesTheMinimum()
    {
        var routes = new List<Feature<Point>> { Line(1, "LINESTRING(0 0, 100 0)") };
        var towers = new List<Feature<Point>> { TowerAt(1, 0, 0), TowerAt(2, 100, 0) };

        var ruleSet = new TopologyRuleSet("t", 1,
            new[] { new TopologyLayer(Tower, GeometryType.Point), new TopologyLayer(Route, GeometryType.LineString) },
            new[]
            {
                new TopologyRule("two", LineCarriesPointsRuleType.TypeId, Route, Tower),
                new TopologyRule("three", LineCarriesPointsRuleType.TypeId, Route, Tower,
                    new RuleParameters(new Dictionary<string, object> { [LineCarriesPointsRuleType.MinimumPointsParameter] = 3 })),
            });

        var issue = Assert.Single((await CheckAsync(ruleSet, routes, towers)).Issues);

        Assert.Equal("three", issue.RuleId);
        Assert.Equal(2, issue.Finding.Value);
    }

    [Fact]
    public async Task PointOnLine_OnGeodeticData_MeasuresInMetres()
    {
        // 1 m of latitude is about 1 / 110 950 degrees at 35.7° N.
        const double degreesPerMetre = 1 / 110_950.0;

        var routes = new List<Feature<Point>> { Line(1, "LINESTRING(51.0 35.7, 51.01 35.7)", 4326) };
        var towers = new List<Feature<Point>>
        {
            TowerAt(1, 51.002, 35.7 + 0.3 * degreesPerMetre, srid: 4326),
            TowerAt(2, 51.004, 35.7 + 0.8 * degreesPerMetre, srid: 4326),
        };

        var ruleSet = new TopologyRuleSet("t", 1,
            new[] { new TopologyLayer(Tower, GeometryType.Point), new TopologyLayer(Route, GeometryType.LineString) },
            new[] { new TopologyRule("onRoute", PointOnLineRuleType.TypeId, Tower, Route) });

        var issue = Assert.Single((await CheckAsync(ruleSet, routes, towers, 4326)).Issues);

        Assert.Equal("Tower:2", issue.Finding.Features[0].ToString());
        Assert.InRange(issue.Finding.Value!.Value, 0.79, 0.81);
    }

    [Fact]
    public async Task GeometryValid_ReportsTheValidatorsProblems()
    {
        var features = new List<Feature<Point>>
        {
            new() { Id = 1 },
            Line(2, "LINESTRING(0 0, 0 0, 10 0)"),
            Line(3, "LINESTRING(0 0, 10 0)"),
        };

        var ruleSet = new TopologyRuleSet("t", 1, new[] { new TopologyLayer(Route, GeometryType.LineString) },
            new[] { new TopologyRule("valid", GeometryValidRuleType.TypeId, Route) });

        var result = await new ValidationRunner(StandardRuleTypes.CreateCatalog())
            .RunAsync(ruleSet, TopologyCheckScope.WholeData, new[] { new FeatureListSource(Route, 0, features) });

        var noGeometry = Assert.Single(result.Issues, i => i.Kind == "NoGeometry");
        Assert.Equal(IssueSeverity.Error, noGeometry.Severity);
        Assert.Null(noGeometry.Finding.Location);

        var repeated = Assert.Single(result.Issues, i => i.Kind == "RepeatedPoint");
        Assert.Equal("Route:2", repeated.Finding.Features[0].ToString());

        Assert.DoesNotContain(result.Issues, i => i.Finding.IsAbout(Route, 3));
    }

    [Fact]
    public void GeometryValid_WrongRingOrientation_IsInformation()
    {
        var ruleType = new GeometryValidRuleType();

        Assert.Equal(IssueSeverity.Info, ruleType.DefaultSeverityOf("WrongRingOrientation"));
        Assert.Equal(IssueSeverity.Error, ruleType.DefaultSeverityOf("InvalidCoordinate"));
    }

    [Fact]
    public void SequenceRule_NeedsItsField()
    {
        var ruleSet = new TopologyRuleSet("t", 1,
            new[] { new TopologyLayer(Tower, GeometryType.Point), new TopologyLayer(Route, GeometryType.LineString) },
            new[] { new TopologyRule("numbers", SequenceAlongLineRuleType.TypeId, Tower, Route) });

        Assert.Single(RuleSetValidator.Validate(ruleSet, StandardRuleTypes.CreateCatalog()));
    }

    [Fact]
    public void StandardCatalog_HasEveryRuleType()
    {
        var ids = StandardRuleTypes.CreateCatalog().RuleTypes.Select(t => t.Id).OrderBy(i => i).ToArray();

        Assert.Equal(new[] { "geometryValid", "lineCarriesPoints", "lineEndCovered", "pointOnLine", "pointOnOneLine", "sequenceAlongLine" }, ids);
    }
}
