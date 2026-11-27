using System.Collections.Generic;
using System.Linq;
using System.Threading;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Core.Layers;
using IRI.Maptor.Presentation.Wpf;
using IRI.Maptor.Presentation.Wpf.Layers;
using IRI.Maptor.Presentation.Wpf.Snapping;
using IRI.Maptor.Tests.Common;

namespace IRI.Maptor.Tests.Mapping;

/// <summary>
/// Snapping while drawing, below the mouse: the layer choice behind the sketch bar, the per-extent cache and the
/// snapper. The mouse wiring in MapViewer is checked in the running application.
/// </summary>
[Collection(WpfCollection.Name)]
public class SnappingTest
{
    private static readonly BoundingBox View = new(0, 0, 1000, 1000);

    private static VectorLayer Layer(string name, params Geometry<Point>[] geometries)
        => new(name, geometries.ToList(), VisualParameters.CreateNew(1), LayerType.VectorLayer, RenderMode.Default, RasterizationMethod.GdiPlus);

    private static Geometry<Point> Line(int srid, params (double X, double Y)[] points)
        => Geometry<Point>.Create(points.Select(p => new Point(p.X, p.Y)).ToList(), Core.Common.Enums.GeometryType.LineString, srid);

    /// <summary>Starts a build and waits for it (the cache reads layers asynchronously).</summary>
    private static void Build(MapSnapCache cache, BoundingBox extent, IReadOnlyList<VectorLayer> layers)
    {
        using var built = new ManualResetEventSlim();

        void OnBuilt() => built.Set();

        cache.Built += OnBuilt;
        cache.EnsureFor(extent, layers);

        Assert.True(built.Wait(5000), "the snap index was not built");

        cache.Built -= OnBuilt;
    }

    #region SnapLayerChoice

    [Fact]
    public void Choice_IsOnAndEveryLayerChecked_ByDefault()
    {
        WpfTestHost.Run(() =>
        {
            var a = Layer("a", Line(SridHelper.WebMercator, (0, 0), (10, 0)));
            var b = Layer("b", Line(SridHelper.WebMercator, (0, 5), (10, 5)));

            var choice = new SnapLayerChoice();
            choice.Update(new[] { a, b });

            Assert.True(choice.IsEnabled);
            Assert.False(choice.HasNoLayers);
            Assert.Equal(new[] { a, b }, choice.CheckedLayers);
        });
    }

    [Fact]
    public void Choice_AnUncheckedLayer_StaysUncheckedWhenItLeavesAndComesBack()
    {
        WpfTestHost.Run(() =>
        {
            var a = Layer("a", Line(SridHelper.WebMercator, (0, 0), (10, 0)));
            var b = Layer("b", Line(SridHelper.WebMercator, (0, 5), (10, 5)));

            var choice = new SnapLayerChoice();
            choice.Update(new[] { a, b });

            choice.Layers[0].IsChecked = false;
            Assert.Equal(new[] { b }, choice.CheckedLayers);

            // a is turned off, then on again
            choice.Update(new[] { b });
            choice.Update(new[] { a, b });

            Assert.False(choice.Layers.Single(i => i.Layer == a).IsChecked);
            Assert.Equal(new[] { b }, choice.CheckedLayers);
        });
    }

    [Fact]
    public void Choice_Changed_OnToggleAndCheckButNotOnTheSameList()
    {
        WpfTestHost.Run(() =>
        {
            var a = Layer("a", Line(SridHelper.WebMercator, (0, 0), (10, 0)));

            var choice = new SnapLayerChoice();
            choice.Update(new[] { a });

            var changes = 0;
            choice.Changed += () => changes++;

            choice.Update(new[] { a });
            Assert.Equal(0, changes);

            choice.IsEnabled = false;
            choice.IsEnabled = false;
            Assert.Equal(1, changes);

            choice.Layers[0].IsChecked = false;
            Assert.Equal(2, changes);
        });
    }

    [Fact]
    public void Choice_NoLayers_SaysSo()
    {
        WpfTestHost.Run(() =>
        {
            var choice = new SnapLayerChoice();
            choice.Update(Enumerable.Empty<VectorLayer>());

            Assert.True(choice.HasNoLayers);
            Assert.Empty(choice.CheckedLayers);
        });
    }

    [Fact]
    public void Choice_UncheckedNames_RoundTripThroughAProject()
    {
        WpfTestHost.Run(() =>
        {
            var a = Layer("towers", Line(SridHelper.WebMercator, (0, 0), (10, 0)));
            var b = Layer("lines", Line(SridHelper.WebMercator, (0, 5), (10, 5)));

            var saved = new SnapLayerChoice();
            saved.Update(new[] { a, b });
            saved.Layers[0].IsChecked = false;

            Assert.Equal(new[] { "towers" }, saved.UncheckedLayerNames);

            // next session: new layer objects (new ids) with the same names, and one that loads later
            var restored = new SnapLayerChoice();
            restored.RestoreUncheckedLayerNames(saved.UncheckedLayerNames);

            var a2 = Layer("towers", Line(SridHelper.WebMercator, (0, 0), (10, 0)));
            var b2 = Layer("lines", Line(SridHelper.WebMercator, (0, 5), (10, 5)));

            restored.Update(new[] { b2 });
            Assert.Equal(new[] { b2 }, restored.CheckedLayers);

            restored.Update(new[] { a2, b2 });
            Assert.Equal(new[] { b2 }, restored.CheckedLayers);

            // a name not seen yet is still saved
            Assert.Equal(new[] { "towers" }, restored.UncheckedLayerNames);

            // checking it again drops the name
            restored.Layers.Single(i => i.Layer == a2).IsChecked = true;
            Assert.Empty(restored.UncheckedLayerNames);
        });
    }

    [Fact]
    public void Choice_Restore_UpdatesTheListedLayersAndChecksTheRest()
    {
        WpfTestHost.Run(() =>
        {
            var a = Layer("a", Line(SridHelper.WebMercator, (0, 0), (10, 0)));
            var b = Layer("b", Line(SridHelper.WebMercator, (0, 5), (10, 5)));

            var choice = new SnapLayerChoice();
            choice.Update(new[] { a, b });
            choice.Layers[1].IsChecked = false;

            var changes = 0;
            choice.Changed += () => changes++;

            choice.RestoreUncheckedLayerNames(new[] { "a" });

            Assert.True(changes > 0);
            Assert.Equal(new[] { b }, choice.CheckedLayers);
            Assert.False(choice.Layers.Single(i => i.Layer == a).IsChecked);

            // an old project file has no list: everything is checked
            choice.RestoreUncheckedLayerNames(null);
            Assert.Equal(new[] { a, b }, choice.CheckedLayers);
        });
    }

    [Fact]
    public void ProjectFile_KeepsTheSnapExcludedLayers_AndOldFilesHaveNone()
    {
        var folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "maptor-snapping-" + System.Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(folder);

        try
        {
            var path = System.IO.Path.Combine(folder, "p.mtproj");

            var project = new IRI.Maptor.Presentation.Core.Models.Project.MapProject { SnapExcludedLayerNames = ["towers", "lines"] };
            IRI.Maptor.Presentation.Core.Models.Project.MapProjectFile.Save(path, project);

            var loaded = IRI.Maptor.Presentation.Core.Models.Project.MapProjectFile.Load(path);
            Assert.Equal(new[] { "towers", "lines" }, loaded!.SnapExcludedLayerNames);

            // a file written before the list existed
            System.IO.File.WriteAllText(path, "{ \"formatVersion\": 1, \"name\": \"old\" }");

            var old = IRI.Maptor.Presentation.Core.Models.Project.MapProjectFile.Load(path);
            Assert.NotNull(old!.SnapExcludedLayerNames);
            Assert.Empty(old.SnapExcludedLayerNames);
        }
        finally
        {
            System.IO.Directory.Delete(folder, recursive: true);
        }
    }

    #endregion

    #region MapSnapCache

    [Fact]
    public void Cache_IndexesTheLayersFeatures()
    {
        WpfTestHost.Run(() =>
        {
            var layer = Layer("lines", Line(SridHelper.WebMercator, (100, 100), (200, 100)));
            var cache = new MapSnapCache();

            Build(cache, View, new[] { layer });

            var snap = SnapEngine.Snap(cache.Index!, new Point(198, 103), 5);

            Assert.Equal(SnapKind.Vertex, snap!.Kind);
            Assert.Equal(200, snap.Location.X);
            Assert.False(cache.IsOverLimit);
        });
    }

    [Fact]
    public void Cache_SameExtent_IsNotRebuilt_AndInvalidateDropsTheIndex()
    {
        WpfTestHost.Run(() =>
        {
            var layer = Layer("lines", Line(SridHelper.WebMercator, (100, 100), (200, 100)));
            var cache = new MapSnapCache();

            Build(cache, View, new[] { layer });

            var builds = 0;
            cache.Built += () => builds++;

            cache.EnsureFor(View, new[] { layer });
            Assert.Equal(0, builds);

            cache.Invalidate();
            Assert.Null(cache.Index);
        });
    }

    [Fact]
    public void Cache_TooManyVertices_IndexesNothingAndSaysSo()
    {
        WpfTestHost.Run(() =>
        {
            var points = Enumerable.Range(0, MapSnapCache.MaxVertices + 1).Select(i => (i * 0.01, 1.0)).ToArray();
            var layer = Layer("dense", Line(SridHelper.WebMercator, points));
            var cache = new MapSnapCache();

            Build(cache, View, new[] { layer });

            Assert.True(cache.IsOverLimit);
            Assert.Null(cache.Index);
        });
    }

    [Fact]
    public void Cache_LiveLayer_IsReadByBox_NeverByGeometry()
    {
        WpfTestHost.Run(() =>
        {
            var source = new LiveSource(new[] { new Feature<Point>(Line(SridHelper.WebMercator, (100, 100), (200, 100))) });

            var layer = new VectorLayer("live", source, VisualParameters.CreateNew(1), LayerType.VectorLayer,
                                        RenderMode.Default, RasterizationMethod.GdiPlus, IRI.Maptor.Presentation.Core.Models.ScaleInterval.All);

            var choice = new SnapLayerChoice();
            choice.Update(new[] { layer });

            // listed and checked like any other layer
            Assert.Equal(new[] { layer }, choice.CheckedLayers);

            var cache = new MapSnapCache();
            Build(cache, View, choice.CheckedLayers);

            Assert.Equal(1, source.BoxReads);
            Assert.Equal(0, source.GeometryReads);
            Assert.Equal(SnapKind.Vertex, SnapEngine.Snap(cache.Index!, new Point(199, 102), 5)!.Kind);
        });
    }

    /// <summary>A Live source: asked with a geometry it would fetch the whole area from the server.</summary>
    private sealed class LiveSource(IEnumerable<Feature<Point>> features)
        : IRI.Maptor.Core.Persistence.DataSources.MemoryDataSource(features), IRI.Maptor.Core.Persistence.Abstractions.IServerQueryableVectorDataSource
    {
        public int BoxReads { get; private set; }

        public int GeometryReads { get; private set; }

        public bool CanQueryServer => true;

        public bool IsViewTruncated => false;

        public event System.EventHandler<bool>? ViewTruncatedChanged { add { } remove { } }

        public FeatureSet<Point> GetLoadedFeatures() => _webMercatorFeatureSet;

        public override System.Threading.Tasks.Task<FeatureSet<Point>> GetAsFeatureSetAsync(BoundingBox boundingBox)
        {
            BoxReads++;
            return base.GetAsFeatureSetAsync(boundingBox);
        }

        public override System.Threading.Tasks.Task<FeatureSet<Point>> GetAsFeatureSetAsync(Geometry<Point>? geometry)
        {
            GeometryReads++;
            return base.GetAsFeatureSetAsync(geometry);
        }

        public System.Threading.Tasks.Task<IRI.Maptor.Core.Persistence.Abstractions.FeatureQueryPage> QueryPageAsync(IRI.Maptor.Core.Spatial.Dtos.FeatureQueryDto query, CancellationToken cancellationToken = default) => throw new System.NotSupportedException();

        public System.Threading.Tasks.Task<IRI.Maptor.Core.Spatial.Dtos.DistinctValuesDto> GetDistinctValuesAsync(string fieldName, CancellationToken cancellationToken = default) => throw new System.NotSupportedException();

        public System.Threading.Tasks.Task<IRI.Maptor.Core.Spatial.Dtos.FieldStatisticsDto> GetFieldStatisticsAsync(string fieldName, CancellationToken cancellationToken = default) => throw new System.NotSupportedException();

        public System.Threading.Tasks.Task<IRI.Maptor.Core.Spatial.Dtos.GroupStatisticsDto> GetGroupStatisticsAsync(string fieldName, string? sumFieldName, string? measureKind, CancellationToken cancellationToken = default) => throw new System.NotSupportedException();

        public System.Threading.Tasks.Task<IRI.Maptor.Core.Spatial.Dtos.GeometryStatisticsDto> GetGeometryStatisticsAsync(string? measureKind, CancellationToken cancellationToken = default) => throw new System.NotSupportedException();
    }

    #endregion

    #region MapSnapper

    [Fact]
    public void Snapper_SnapsOnceTheCacheIsBuilt_AndNotWhenOff()
    {
        WpfTestHost.Run(() =>
        {
            var layer = Layer("lines", Line(SridHelper.WebMercator, (100, 100), (200, 100)));
            var choice = new SnapLayerChoice();

            // one pixel = one map unit here
            var snapper = new MapSnapper(choice, () => View, pixels => pixels, () => new[] { layer });

            snapper.BeginSession();

            // the first request starts the build and finds no index yet
            Assert.Null(snapper.Snap(new Point(150, 104)));

            SpinWait.SpinUntil(() => snapper.Snap(new Point(150, 104.5)) is not null, 5000);

            var snap = snapper.Snap(new Point(150, 104));

            Assert.Equal(SnapKind.Edge, snap!.Kind);
            Assert.Equal(100, snap.Location.Y, 9);

            choice.IsEnabled = false;
            Assert.Null(snapper.Snap(new Point(150, 103)));
        });
    }

    [Fact]
    public void Snapper_UncheckedLayer_IsNotSnappedTo()
    {
        WpfTestHost.Run(() =>
        {
            var layer = Layer("lines", Line(SridHelper.WebMercator, (100, 100), (200, 100)));
            var choice = new SnapLayerChoice();
            var snapper = new MapSnapper(choice, () => View, pixels => pixels, () => new[] { layer });

            snapper.BeginSession();
            choice.Layers[0].IsChecked = false;

            snapper.Snap(new Point(150, 104));
            Thread.Sleep(200);

            Assert.Null(snapper.Snap(new Point(150, 103)));
        });
    }

    [Fact]
    public void Snapper_Editing_LeavesTheStoredCopyOfTheEditedGeometryOut()
    {
        WpfTestHost.Run(() =>
        {
            var edited = Line(SridHelper.WebMercator, (100, 100), (200, 100));
            // 30 away: outside the 10-unit tolerance of the edited line's end
            var other = Line(SridHelper.WebMercator, (100, 130), (200, 130));

            var layer = Layer("lines", edited, other);
            var snapper = new MapSnapper(new SnapLayerChoice(), () => View, pixels => pixels, () => new[] { layer });

            // the edit is handed an equal copy, as an edit of a layer feature is (repaired or re-read)
            snapper.BeginSession(edited: Line(SridHelper.WebMercator, (100, 100), (200, 100)));

            snapper.Snap(new Point(0, 0));
            SpinWait.SpinUntil(() => snapper.Snap(new Point(150, 128)) is not null, 5000);

            // next to the edited line's old end: its own copy does not pull the vertex back
            Assert.Null(snapper.Snap(new Point(200, 101)));

            // the other line is still a target
            var snap = snapper.Snap(new Point(199, 128));
            Assert.Equal(SnapKind.Vertex, snap!.Kind);
            Assert.Equal(130, snap.Location.Y);
        });
    }

    [Fact]
    public void Snapper_Editing_TheSameObjectIsLeftOutToo()
    {
        WpfTestHost.Run(() =>
        {
            var edited = Line(SridHelper.WebMercator, (100, 100), (200, 100));
            var layer = Layer("lines", edited);
            var snapper = new MapSnapper(new SnapLayerChoice(), () => View, pixels => pixels, () => new[] { layer });

            snapper.BeginSession(edited: edited);

            snapper.Snap(new Point(0, 0));
            Thread.Sleep(200);

            Assert.Null(snapper.Snap(new Point(200, 101)));

            // a drawing session afterwards snaps to it again
            snapper.BeginSession();
            snapper.Snap(new Point(0, 0));
            SpinWait.SpinUntil(() => snapper.Snap(new Point(200, 101.5)) is not null, 5000);

            Assert.NotNull(snapper.Snap(new Point(200, 101)));
        });
    }

    [Fact]
    public void Snapper_FollowsTheDistanceAndKindsSettings()
    {
        WpfTestHost.Run(() =>
        {
            var layer = Layer("lines", Line(SridHelper.WebMercator, (100, 100), (200, 100)));

            var tolerance = 10.0;
            var options = new SnapOptions();

            var snapper = new MapSnapper(new SnapLayerChoice(), () => View, pixels => pixels, () => new[] { layer }, () => tolerance, () => options);

            snapper.BeginSession();
            snapper.Snap(new Point(0, 0));
            SpinWait.SpinUntil(() => snapper.Snap(new Point(150, 103.5)) is not null, 5000);

            // 4 away from the line: in at 10 px, out at 2 px, and a new value applies to the same point at once
            Assert.NotNull(snapper.Snap(new Point(150, 104)));

            tolerance = 2;
            Assert.Null(snapper.Snap(new Point(150, 104)));

            tolerance = 10;
            Assert.NotNull(snapper.Snap(new Point(150, 104)));

            // near the line's end: a vertex by default, the edge when vertices are off, nothing when both are off
            Assert.Equal(SnapKind.Vertex, snapper.Snap(new Point(198, 102))!.Kind);

            options = new SnapOptions { SnapToVertices = false };
            Assert.Equal(SnapKind.Edge, snapper.Snap(new Point(198, 102))!.Kind);

            options = new SnapOptions { SnapToVertices = false, SnapToEdges = false };
            Assert.Null(snapper.Snap(new Point(198, 102)));
        });
    }

    [Fact]
    public void MapSettings_SnappingDefaults_AndTheDistanceIsClamped()
    {
        var settings = new IRI.Maptor.Presentation.Core.Data.MapSettings();

        Assert.Equal(10, settings.Snapping_TolerancePixels);
        Assert.True(settings.Snapping_ToVertices);
        Assert.True(settings.Snapping_ToEdges);

        var model = new IRI.Maptor.Presentation.Wpf.Models.Settings.MapSettingsModel(settings);

        model.Snapping_TolerancePixels = 3;
        Assert.Equal(5, settings.Snapping_TolerancePixels);

        model.Snapping_TolerancePixels = 40;
        Assert.Equal(25, settings.Snapping_TolerancePixels);

        model.Snapping_TolerancePixels = 12;
        Assert.Equal(12, model.Snapping_TolerancePixels);
    }

    #endregion

    [Fact]
    public void Views_WithTheSnappingControls_Load()
    {
        WpfTestHost.Run(() =>
        {
            WpfTestHost.EnsureMahAppsResources();

            // converters, shapes and fonts come from the application, as in the apps
            foreach (var dictionary in new[] { "Assets/IRI.Maptor.Converters.xaml", "Assets/Shapes/Others.xaml", "Assets/IRI.Maptor.Fonts.xaml" })
            {
                System.Windows.Application.Current.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary
                {
                    Source = new System.Uri($"pack://application:,,,/IRI.Maptor.Presentation.Wpf;component/{dictionary}", System.UriKind.Absolute)
                });
            }

            // resolves every StaticResource and icon name of the sketch bar, its snapping popup included
            var sketchBar = new IRI.Maptor.Presentation.Wpf.Controls.SketchBarView();
            Assert.NotNull(sketchBar);

            // the map with its overlay canvas for the snap marker
            var map = new IRI.Maptor.Presentation.Wpf.Controls.MapViewer();
            Assert.NotNull(map);
        });
    }
}
