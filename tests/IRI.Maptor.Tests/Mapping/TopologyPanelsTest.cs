using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Persistence.DataSources;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Core.Topology.Checking;
using IRI.Maptor.Core.Topology.Findings;
using IRI.Maptor.Core.Topology.FixTypes;
using IRI.Maptor.Core.Topology.Rules;
using IRI.Maptor.Core.Topology.RuleTypes;
using IRI.Maptor.Presentation.Wpf.Controls;
using IRI.Maptor.Presentation.Wpf.Layers;
using IRI.Maptor.Presentation.Wpf.Topology;
using IRI.Maptor.Presentation.Wpf.ViewModels;
using IRI.Maptor.Tests.Common;

using Point = IRI.Maptor.Core.Common.Primitives.Point;

using Xunit;

namespace IRI.Maptor.Tests.Mapping;

/// <summary>The Topology tab's workspace, panels and overlay on synthetic map layers near 51° E, 35.7° N.</summary>
[Collection(WpfCollection.Name)]
public class TopologyPanelsTest
{
    private sealed class TestMap : MapViewModelBase
    {
    }

    /// <summary>A map whose user may not edit geometries (D34).</summary>
    private sealed class ReadOnlyMap : MapViewModelBase
    {
        public override bool CanEditGeometry => false;
    }

    private const double MetreOfLatitude = 1 / 110_950.0;

    private static Point WebMercator(double longitude, double latitude) => MapProjects.GeodeticWgs84ToWebMercator(new Point(longitude, latitude));

    private static Feature<Point> Tower(int id, double longitude, double latitude)
        => new(Geometry<Point>.Create(new List<Point> { WebMercator(longitude, latitude) }, GeometryType.Point, SridHelper.WebMercator)) { Id = id };

    private static Feature<Point> Route(int id, params (double Longitude, double Latitude)[] points)
        => new(Geometry<Point>.Create(points.Select(p => WebMercator(p.Longitude, p.Latitude)).ToList(), GeometryType.LineString, SridHelper.WebMercator)) { Id = id };

    private static VectorLayer Layer(string name, MemoryDataSource source)
        => new(name, source,
               new IRI.Maptor.Presentation.Wpf.VisualParameters(null, System.Windows.Media.Brushes.Red, 1, 1),
               IRI.Maptor.Presentation.Core.LayerType.VectorLayer, IRI.Maptor.Presentation.Core.RenderMode.Default,
               IRI.Maptor.Presentation.Core.RasterizationMethod.DrawingVisual, IRI.Maptor.Presentation.Core.Models.ScaleInterval.All);

    private static TopologyRuleSet Rules() => new("test", 1,
        new[] { new TopologyLayer("Tower", GeometryType.Point), new TopologyLayer("Route", GeometryType.LineString), new TopologyLayer("Pole", GeometryType.Point) },
        new[]
        {
            new TopologyRule("t.onRoute", PointOnLineRuleType.TypeId, "Tower", "Route"),
            new TopologyRule("t.ends", LineEndCoveredRuleType.TypeId, "Route", "Tower"),
            new TopologyRule("t.poles", DistinctPointsRuleType.TypeId, "Pole"),
        });

    private sealed class Fixture
    {
        public Fixture(MapViewModelBase? map = null)
        {
            Towers = new MemoryDataSource(new[]
            {
                Tower(1, 51.0, 35.7),
                Tower(2, 51.001, 35.7),
                Tower(3, 51.0005, 35.7 + 0.8 * MetreOfLatitude),     // 0.8 m off the route: near line
            }, resetIds: false);

            Routes = new MemoryDataSource(new[] { Route(10, (51.0, 35.7), (51.001, 35.7)) }, resetIds: false);

            var layers = new Dictionary<string, VectorLayer> { ["Tower"] = Layer("towers", Towers), ["Route"] = Layer("routes", Routes) };

            Map = map ?? new TestMap();
            Workspace = new TopologyWorkspace(Map, Rules(), StandardRuleTypes.CreateCatalog(),
                entity => layers.TryGetValue(entity, out var layer) ? new[] { layer } : Array.Empty<VectorLayer>());
            Overlay = new TopologyOverlay(Map);
        }

        public MemoryDataSource Towers { get; }

        public MemoryDataSource Routes { get; }

        public MapViewModelBase Map { get; }

        public TopologyWorkspace Workspace { get; }

        public TopologyOverlay Overlay { get; }

        public Task<TopologyCheckResult> CheckAsync() => Workspace.CheckAsync(TopologyCheckScope.WholeData, null, CancellationToken.None);
    }

    /// <summary>Runs the body on an STA thread with a pumping dispatcher, as the map's UI thread has.</summary>
    private static void OnUiThread(Func<Task> body)
    {
        Exception? failure = null;

        WpfTestHost.Run(() =>
        {
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));

            var frame = new DispatcherFrame();
            var task = body();

            task.ContinueWith(_ => frame.Continue = false, TaskScheduler.Default);

            Dispatcher.PushFrame(frame);

            failure = task.Exception?.GetBaseException();
        });

        if (failure is not null)
            throw new InvalidOperationException($"Test body threw: {failure}", failure);
    }

    [Fact]
    public void Workspace_ChecksTheLoadedLayers_AndSkipsRulesOfMissingOnes() => OnUiThread(async () =>
    {
        var f = new Fixture();

        var result = await f.CheckAsync();

        var issue = Assert.Single(result.Issues);
        Assert.Equal(PointOnLineRuleType.NearLine, issue.Kind);

        Assert.Equal(new[] { "t.poles" }, f.Workspace.SkippedRules.Select(r => r.Id).ToArray());
        Assert.True(f.Workspace.HasResult);
        Assert.Null(f.Workspace.Comparison);

        Assert.Equal(TopologyLayerState.Loaded, f.Workspace.StateOf("Tower"));
        Assert.Equal(TopologyLayerState.NotLoaded, f.Workspace.StateOf("Pole"));
    });

    [Fact]
    public void Workspace_SecondCheckOfTheSameScope_TellsNewAndGone() => OnUiThread(async () =>
    {
        var f = new Fixture();

        await f.CheckAsync();

        // The near tower is deleted (its issue goes) and a tower far from every route is added (a new issue).
        var near = (await f.Workspace.MapFeaturesAsync("Tower", new long[] { 3 })).Single().Feature;
        f.Towers.Remove(near);
        f.Towers.Add(Tower(4, 51.0005, 35.71));

        await f.CheckAsync();

        Assert.NotNull(f.Workspace.Comparison);
        Assert.Equal(PointOnLineRuleType.FarFromLines, Assert.Single(f.Workspace.Comparison!.New).Kind);
        Assert.Equal(PointOnLineRuleType.NearLine, Assert.Single(f.Workspace.Comparison.Gone).Kind);
    });

    [Fact]
    public void Workspace_SessionSettings_ChangeTheCheck_AndReset() => OnUiThread(async () =>
    {
        var f = new Fixture();

        var rule = f.Workspace.RuleSet.FindRule("t.onRoute")!;
        f.Workspace.UpdateRule(rule.WithParameters(rule.Parameters.With("distance", 1.0)));

        Assert.True(f.Workspace.HasSessionChanges);
        Assert.Empty((await f.CheckAsync()).Issues);
        Assert.True(f.Workspace.CheckedWithSessionChanges);

        f.Workspace.ResetSettings();

        Assert.False(f.Workspace.HasSessionChanges);
        Assert.Single((await f.CheckAsync()).Issues);
    });

    [Fact]
    public void IssuesPanel_GroupsFiltersAndDescribesTheIssues() => OnUiThread(async () =>
    {
        var f = new Fixture();
        f.Towers.Add(Tower(4, 51.0005, 35.71));   // far from every route

        var panel = new TopologyIssuesPanelViewModel(f.Workspace, f.Overlay);

        await f.CheckAsync();

        Assert.Equal(2, panel.Rows.Count);
        Assert.Equal(2, panel.ErrorCount);
        Assert.Equal(2, panel.VisibleIssues.Count);
        Assert.Single(panel.View.Groups);

        panel.Search = "4";
        Assert.Single(panel.VisibleIssues);

        panel.Search = string.Empty;
        panel.ShowErrors = false;
        Assert.Empty(panel.VisibleIssues);

        panel.ShowErrors = true;
        panel.Selected = panel.View.Cast<TopologyIssueRow>().First(r => r.Issue.Kind == PointOnLineRuleType.NearLine);

        Assert.True(panel.HasSelection);
        Assert.Equal(2, panel.SelectedFeatures.Count);
        Assert.DoesNotContain("{0}", panel.Selected.Sentence);
        Assert.Contains("[3]", panel.Selected.FeaturesText);

        var row = TopologyIssueExport.Row(panel.Selected);
        Assert.Equal(TopologyIssueExport.Headers().Count, row.Count);
    });

    [Fact]
    public void RulesPanel_ChangesLastTheSession_AndResetAllUndoesThem() => OnUiThread(() =>
    {
        var f = new Fixture();
        var panel = new TopologyRulesPanelViewModel(f.Workspace);

        var onRoute = panel.Rules.Single(r => r.Rule.Id == "t.onRoute");

        // Only the parameters a rule set gives are listed; this rule relies on its rule type's defaults.
        Assert.Empty(onRoute.Parameters);

        var near = onRoute.Kinds.Single(k => k.Kind == PointOnLineRuleType.NearLine);
        near.Setting = TopologyKindRow.Options.Single(o => o.Value == TopologyKindSetting.Hidden);

        Assert.True(f.Workspace.RuleSet.FindRule("t.onRoute")!.IsHidden(PointOnLineRuleType.NearLine));
        Assert.True(onRoute.IsChanged);
        Assert.True(panel.HasSessionChanges);

        panel.ResetAllCommand.Execute(null);

        Assert.False(f.Workspace.HasSessionChanges);
        Assert.False(f.Workspace.RuleSet.FindRule("t.onRoute")!.IsHidden(PointOnLineRuleType.NearLine));

        return Task.CompletedTask;
    });

    [Fact]
    public void RulesPanel_ReadsPersianDigits_AndRefusesBadNumbers() => OnUiThread(() =>
    {
        var map = new TestMap();
        var ruleSet = new TopologyRuleSet("t", 1, new[] { new TopologyLayer("Tower", GeometryType.Point), new TopologyLayer("Route", GeometryType.LineString) },
            new[] { new TopologyRule("t.onRoute", PointOnLineRuleType.TypeId, "Tower", "Route", new RuleParameters(new Dictionary<string, object> { ["distance"] = 0.5, ["nearDistance"] = 50 })) });
        var workspace = new TopologyWorkspace(map, ruleSet, StandardRuleTypes.CreateCatalog(), _ => Array.Empty<VectorLayer>());
        var panel = new TopologyRulesPanelViewModel(workspace);

        var distance = panel.Rules[0].Parameters.Single(p => p.Name == "distance");

        distance.Text = "۱٫۵";
        Assert.True(distance.IsValid);
        Assert.Equal(1.5, workspace.RuleSet.Rules[0].Parameters.GetNumber("distance", 0), 9);

        distance.Text = "abc";
        Assert.False(distance.IsValid);
        Assert.Equal(1.5, workspace.RuleSet.Rules[0].Parameters.GetNumber("distance", 0), 9);

        // A near distance below the distance is a setting that cannot be used.
        panel.Rules[0].Parameters.Single(p => p.Name == "nearDistance").Text = "1";
        Assert.True(panel.HasProblems);

        return Task.CompletedTask;
    });

    [Fact]
    public void CheckPanel_ListsTheLayers_AndRunsTheCheck() => OnUiThread(async () =>
    {
        var f = new Fixture();
        var opened = 0;
        var panel = new TopologyCheckPanelViewModel(f.Workspace, () => opened++);

        await panel.RefreshAsync();

        Assert.Equal(new[] { "Tower", "Route", "Pole" }, panel.Layers.Select(l => l.EntityName).ToArray());
        Assert.True(panel.Layers.Single(l => l.EntityName == "Pole").IsMissing);
        Assert.True(panel.CanCheck);
        Assert.True(panel.HasWarning);   // the "not loaded" note

        panel.CheckCommand.Execute(null);

        for (int i = 0; i < 200 && opened == 0; i++)
            await Task.Delay(10);

        Assert.Equal(1, opened);
        Assert.True(f.Workspace.HasResult);
    });

    [Fact]
    public void Overlay_Plan_SplitsBySeverity_AndLimitsToErrors()
    {
        static TopologyIssue Issue(IssueSeverity severity, int id)
        {
            var finding = new TopologyFinding("r", "k", severity, new[] { new FindingFeature("P", id, FeatureRole.A) },
                Geometry<Point>.Create(51, 35.7, SridHelper.GeodeticWGS84));
            return new TopologyIssue(finding, IssueIdentity.Hash(finding), severity);
        }

        var issues = new[] { Issue(IssueSeverity.Error, 1), Issue(IssueSeverity.Warning, 2), Issue(IssueSeverity.Info, 3) };

        var plan = TopologyOverlay.Plan(issues, issues[1], out var limited);

        Assert.False(limited);
        Assert.Equal(new[] { 1, 1, 1, 1 }, plan.Select(p => p.Geometries.Count).ToArray());
        Assert.All(plan.SelectMany(p => p.Geometries), g => Assert.Equal(SridHelper.WebMercator, g.Srid));

        var many = Enumerable.Range(0, TopologyOverlay.Limit + 1).Select(i => Issue(i % 2 == 0 ? IssueSeverity.Error : IssueSeverity.Warning, i)).ToList();

        var limitedPlan = TopologyOverlay.Plan(many, null, out limited);

        Assert.True(limited);
        Assert.Equal(0, limitedPlan[1].Geometries.Count);
        Assert.Equal(many.Count(i => i.Severity == IssueSeverity.Error), limitedPlan[2].Geometries.Count);
    }

    [Fact]
    public void Views_Load() => OnUiThread(async () =>
    {
        var cache = typeof(IRI.Maptor.Presentation.Wpf.Helpers.SharedResourceDictionary)
            .GetField("_cache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .GetValue(null) as System.Collections.IDictionary;
        lock (cache!)
            cache.Clear();

        var merged = Application.Current.Resources.MergedDictionaries;
        if (!merged.Any(d => d.Source?.OriginalString.EndsWith("Maptor.All.xaml") == true))
        {
            WpfTestHost.EnsureMahAppsResources();
            merged.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/IRI.Maptor.Presentation.Wpf;component/Assets/Maptor.All.xaml") });
        }

        var f = new Fixture();
        var issues = new TopologyIssuesPanelViewModel(f.Workspace, f.Overlay);

        await f.CheckAsync();

        _ = new TopologyCheckPanelView { DataContext = new TopologyCheckPanelViewModel(f.Workspace, () => { }) };
        _ = new TopologyIssuesPanelView { DataContext = issues };
        _ = new TopologyRulesPanelView { DataContext = new TopologyRulesPanelViewModel(f.Workspace) };

        lock (cache)
            cache.Clear();
    });

    #region Fixes (step 5.1)

    [Fact]
    public void FixSession_AppliesTheFix_AsPendingEdits_AndTheNextCheckFindsNothing() => OnUiThread(async () =>
    {
        var f = new Fixture();
        var fixes = new TopologyFixSession(f.Workspace);

        var issue = Assert.Single((await f.CheckAsync()).Issues);
        Assert.True(fixes.HasFixFor(issue));

        var evaluation = Assert.Single(await fixes.EvaluateAsync(issue, CancellationToken.None));

        // Equal ranks in this rule set: the tower moves onto the route.
        Assert.True(evaluation.CanApply);
        Assert.Equal(PointOntoLineFix.MovePoint, evaluation.Offer.Proposal!.Variant);

        await fixes.ApplyAsync(evaluation);

        Assert.Equal(1, f.Towers.NumberOfUpdatedFeatures);
        Assert.Equal(0, f.Routes.NumberOfUpdatedFeatures);
        Assert.True(fixes.IsPending(issue));
        Assert.Equal(1, fixes.PendingCount);

        // The tower is still in Web Mercator, now on the route.
        var tower = (await f.Workspace.MapFeaturesAsync("Tower", new long[] { 3 })).Single().Feature;
        Assert.Equal(SridHelper.WebMercator, tower.TheGeometry.Srid);
        Assert.Equal(WebMercator(51.0005, 35.7).Y, tower.TheGeometry.Points![0].Y, 3);

        Assert.Empty((await f.CheckAsync()).Issues);
        Assert.Equal(0, fixes.PendingCount);
    });

    [Fact]
    public void FixSession_WithoutEditPermission_OffersButCannotApply() => OnUiThread(async () =>
    {
        var f = new Fixture(new ReadOnlyMap());
        var fixes = new TopologyFixSession(f.Workspace);

        var issue = Assert.Single((await f.CheckAsync()).Issues);
        var evaluation = Assert.Single(await fixes.EvaluateAsync(issue, CancellationToken.None));

        Assert.True(evaluation.Offer.IsOffered);
        Assert.False(evaluation.CanApply);
        Assert.Equal(FixApplyReasons.NoPermission, evaluation.CannotApply!.Value.Reason);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixes.ApplyAsync(evaluation));
    });

    [Fact]
    public void IssuesPanel_PreviewThenApply_MarksTheRowPending() => OnUiThread(async () =>
    {
        var f = new Fixture();
        var fixes = new TopologyFixSession(f.Workspace);
        var panel = new TopologyIssuesPanelViewModel(f.Workspace, f.Overlay, fixes);

        await f.CheckAsync();

        var row = panel.View.Cast<TopologyIssueRow>().Single();
        panel.Selected = row;

        var fix = panel.Fix!;

        for (int i = 0; i < 200 && fix.IsEvaluating; i++)
            await Task.Delay(10);

        Assert.True(fix.IsVisible);
        Assert.True(fix.CanPreview, fix.Note);
        Assert.Contains("[3]", fix.FixText);
        Assert.DoesNotContain("{", fix.FixText);
        Assert.False(fix.ApplyCommand.CanExecute(null));

        fix.PreviewCommand.Execute(null);
        for (int i = 0; i < 100 && !fix.IsPreviewing; i++)
            await Task.Delay(10);

        Assert.True(fix.IsPreviewing);
        Assert.NotNull(f.Overlay.Preview);

        fix.ApplyCommand.Execute(null);
        for (int i = 0; i < 200 && !row.IsFixPending; i++)
            await Task.Delay(10);

        Assert.True(row.IsFixPending);
        Assert.True(panel.HasPendingFixes);
        Assert.DoesNotContain("{", panel.PendingFixText);
        Assert.Null(f.Overlay.Preview);
        Assert.True(fix.IsPending);
        Assert.False(fix.CanPreview);
    });

    [Fact]
    public void Overlay_PreviewPlan_DrawsBeforeAndAfter_OnTheMap()
    {
        var before = Geometry<Point>.Create(51.0005, 35.70001, SridHelper.GeodeticWGS84);
        var after = Geometry<Point>.Create(51.0005, 35.7, SridHelper.GeodeticWGS84);
        var finding = new TopologyFinding("r", PointOnLineRuleType.NearLine, IssueSeverity.Error, new[] { new FindingFeature("Tower", 3, FeatureRole.A) }, before);

        var proposal = new IRI.Maptor.Core.Topology.Fixes.TopologyFixProposal("t", "v", finding,
            new[] { new IRI.Maptor.Core.Topology.Fixes.FeatureEdit("Tower", 3, before, after), new IRI.Maptor.Core.Topology.Fixes.FeatureEdit("Tower", 4, before, null) },
            new BoundingBox(51, 35.6, 51.1, 35.8));

        var (drawnBefore, drawnAfter) = TopologyOverlay.PreviewPlan(proposal);

        Assert.Equal(2, drawnBefore.Count);
        Assert.Single(drawnAfter);   // a removed feature has no "after"
        Assert.All(drawnBefore.Concat(drawnAfter), g => Assert.Equal(SridHelper.WebMercator, g.Srid));
    }

    [Fact]
    public void FixApplier_ToLayer_KeepsTheLayersSrid()
    {
        var geodetic = Geometry<Point>.Create(51, 35.7, SridHelper.GeodeticWGS84);

        var mercator = TopologyFixApplier.ToLayer(geodetic, SridHelper.WebMercator);
        var zero = TopologyFixApplier.ToLayer(geodetic, 0);
        var same = TopologyFixApplier.ToLayer(geodetic, SridHelper.GeodeticWGS84);

        Assert.Equal(SridHelper.WebMercator, mercator.Srid);
        Assert.Equal(WebMercator(51, 35.7).X, mercator.Points![0].X, 3);
        Assert.Equal(0, zero.Srid);
        Assert.Equal(mercator.Points[0].Y, zero.Points![0].Y, 6);
        Assert.Same(geodetic, same);
    }

    #endregion
}