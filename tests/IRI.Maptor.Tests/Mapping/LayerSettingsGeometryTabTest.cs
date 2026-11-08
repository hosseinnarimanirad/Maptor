using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Persistence.Abstractions;
using IRI.Maptor.Core.Persistence.DataSources;
using IRI.Maptor.Core.Spatial.Dtos;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Spatial.Validation;
using IRI.Maptor.Presentation.Wpf.Layers;
using IRI.Maptor.Presentation.Wpf.ViewModels.LayerSettings;

namespace IRI.Maptor.Tests.Mapping;

/// <summary>
/// Layer Settings › Geometry (LayerSettings_GeometryViewModel): check, fix as tracked edits, delete, permissions,
/// and Live layers checked on their loaded features only. Synthetic Web Mercator geometries.
/// </summary>
public class LayerSettingsGeometryTabTest
{
    private const int WebMercator = 3857;

    private static Geometry<Point> Ring(params (double x, double y)[] points)
        => Geometry<Point>.Create(points.Select(p => new Point(p.x, p.y)).ToList(), GeometryType.LineString, WebMercator);

    private static Feature<Point> Polygon(string name, params (double x, double y)[] ring)
        => new(Geometry<Point>.Create(new List<Geometry<Point>> { Ring(ring) }, GeometryType.Polygon, WebMercator),
               new Dictionary<string, object> { ["name"] = name }) { LabelAttribute = "name" };

    private static Feature<Point> PointFeature(string name, double x, double y)
        => new(Geometry<Point>.Create(new List<Point> { new(x, y) }, GeometryType.Point, WebMercator),
               new Dictionary<string, object> { ["name"] = name }) { LabelAttribute = "name" };

    // valid square, closed ring (fixable), clockwise square (orientation only), NaN point (delete only)
    private static List<Feature<Point>> Features() =>
    [
        Polygon("valid", (0, 0), (10, 0), (10, 10), (0, 10)),
        Polygon("closed", (20, 0), (30, 0), (30, 10), (20, 10), (20, 0)),
        Polygon("clockwise", (40, 0), (40, 10), (50, 10), (50, 0)),
        PointFeature("nan", double.NaN, 5),
    ];

    private static VectorLayer Layer(IVectorDataSource source)
        => new("parcels", source,
               new IRI.Maptor.Presentation.Wpf.VisualParameters(null, System.Windows.Media.Brushes.Red, 1, 1),
               IRI.Maptor.Presentation.Core.LayerType.VectorLayer, IRI.Maptor.Presentation.Core.RenderMode.Default,
               IRI.Maptor.Presentation.Core.RasterizationMethod.DrawingVisual, IRI.Maptor.Presentation.Core.Models.ScaleInterval.All);

    [Fact]
    public async Task Check_ListsInvalidFeatures_AndNotesOrientation()
    {
        var source = new MemoryDataSource(Features());

        var tab = new LayerSettings_GeometryViewModel(Layer(source), canEditGeometry: true);

        await tab.CheckAsync();

        Assert.Null(tab.ErrorMessage);
        Assert.True(tab.HasResult);
        Assert.Equal(new[] { "closed", "nan" }, tab.Problems.Select(p => p.FeatureLabel));
        Assert.Equal(new[] { GeometryIssueKind.ClosedRing, GeometryIssueKind.InvalidCoordinate }, tab.Groups.Select(g => g.Kind));
        Assert.NotNull(tab.OrientationNote);
        Assert.True(tab.Problems.Single(p => p.FeatureLabel == "closed").CanRepair);
        Assert.False(tab.Problems.Single(p => p.FeatureLabel == "nan").CanRepair);
        Assert.False(tab.IsLiveLayer);
        Assert.True(tab.IsEditable);
    }

    [Fact]
    public async Task FixAll_AppliesTrackedEdits_ThenOnlyTheUnrepairableIsLeft()
    {
        var features = Features();

        var source = new MemoryDataSource(features);

        var refreshes = 0;

        var tab = new LayerSettings_GeometryViewModel(Layer(source), canEditGeometry: true) { RequestRefreshMap = () => refreshes++ };

        await tab.CheckAsync();
        await tab.FixAllAsync();

        // closed ring and clockwise ring repaired, as edits waiting for Save
        Assert.True(source.HasPendingChanges);
        Assert.Equal(2, source.NumberOfUpdatedFeatures);
        Assert.Equal(1, refreshes);
        Assert.True(tab.HasPendingChanges);
        Assert.Equal(new[] { "nan" }, tab.Problems.Select(p => p.FeatureLabel));
        Assert.Null(tab.OrientationNote);

        // every feature but the NaN point now passes the check
        var current = (await source.GetAsFeatureSetAsync()).Features.Where(f => !Equals(f.Attributes["name"], "nan")).ToList();

        Assert.Equal(3, current.Count);
        Assert.True(GeometryValidator.Validate(current).IsClean);
    }

    [Fact]
    public async Task DeleteSelected_RemovesTheUnrepairableFeature_AfterConfirming()
    {
        var source = new MemoryDataSource(Features());

        var questions = new List<string>();

        var tab = new LayerSettings_GeometryViewModel(Layer(source), canEditGeometry: true)
        {
            RequestConfirmAsync = q => { questions.Add(q); return Task.FromResult(true); },
        };

        await tab.CheckAsync();

        tab.SelectedProblem = tab.Problems.Single(p => p.FeatureLabel == "nan");

        Assert.False(tab.FixSelectedCommand.CanExecute(null));
        Assert.True(tab.DeleteSelectedCommand.CanExecute(null));

        await tab.DeleteSelectedAsync();

        Assert.Single(questions);
        Assert.Equal(1, source.NumberOfDeletedFeatures);
        Assert.Equal(new[] { "closed" }, tab.Problems.Select(p => p.FeatureLabel));
    }

    [Fact]
    public async Task WithoutPermission_ReportsOnly()
    {
        var source = new MemoryDataSource(Features());

        var tab = new LayerSettings_GeometryViewModel(Layer(source), canEditGeometry: false);

        await tab.CheckAsync();

        Assert.False(tab.IsEditable);
        Assert.True(tab.HasNotEditableReason);
        Assert.False(tab.FixAllCommand.CanExecute(null));

        await tab.FixAllAsync();

        Assert.False(source.HasPendingChanges);
    }

    [Fact]
    public async Task LiveLayer_ChecksOnlyTheLoadedFeatures_WithoutAskingTheServer()
    {
        var source = new LoadedOnlyLiveSource(Features());

        var tab = new LayerSettings_GeometryViewModel(Layer(source), canEditGeometry: true);

        Assert.True(tab.IsLiveLayer);

        await tab.CheckAsync();

        Assert.Equal(1, source.LoadedReads);
        Assert.Equal(0, source.FullReads);
        Assert.Equal(2, tab.Problems.Count);
    }

    /// <summary>A Live source whose whole-layer read would download everything: the tab must not use it.</summary>
    private sealed class LoadedOnlyLiveSource(IEnumerable<Feature<Point>> features) : MemoryDataSource(features), IServerQueryableVectorDataSource
    {
        public int LoadedReads { get; private set; }

        public int FullReads { get; private set; }

        public bool CanQueryServer => true;

        public bool IsViewTruncated => false;

        public event EventHandler<bool>? ViewTruncatedChanged { add { } remove { } }

        public FeatureSet<Point> GetLoadedFeatures()
        {
            LoadedReads++;
            return _webMercatorFeatureSet;
        }

        public override Task<FeatureSet<Point>> GetAsFeatureSetAsync(Geometry<Point>? geometry)
        {
            FullReads++;
            return base.GetAsFeatureSetAsync(geometry);
        }

        public Task<FeatureQueryPage> QueryPageAsync(FeatureQueryDto query, System.Threading.CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<DistinctValuesDto> GetDistinctValuesAsync(string fieldName, System.Threading.CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<FieldStatisticsDto> GetFieldStatisticsAsync(string fieldName, System.Threading.CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<GroupStatisticsDto> GetGroupStatisticsAsync(string fieldName, string? sumFieldName, string? measureKind, System.Threading.CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<GeometryStatisticsDto> GetGeometryStatisticsAsync(string? measureKind, System.Threading.CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
