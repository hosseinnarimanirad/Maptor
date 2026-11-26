using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Persistence.Abstractions;
using IRI.Maptor.Core.Persistence.DataSources;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Core.Topology.Checking;
using IRI.Maptor.Core.Topology.Rules;
using IRI.Maptor.Core.Topology.RuleTypes;
using IRI.Maptor.Presentation.Wpf.Layers;
using IRI.Maptor.Presentation.Wpf.Topology;

using Xunit;

namespace IRI.Maptor.Tests.Mapping;

/// <summary>Map layers (Web Mercator) as topology feature sources (WGS84). Synthetic features near 51° E, 35.7° N.</summary>
public class MapLayerFeatureSourceTest
{
    private static Point WebMercator(double longitude, double latitude) => MapProjects.GeodeticWgs84ToWebMercator(new Point(longitude, latitude));

    private static Feature<Point> PointAt(int id, double longitude, double latitude)
    {
        var p = WebMercator(longitude, latitude);

        return new Feature<Point>(Geometry<Point>.Create(new List<Point> { p }, GeometryType.Point, SridHelper.WebMercator), new Dictionary<string, object> { ["name"] = $"p{id}" }) { Id = id };
    }

    private static Feature<Point> LineAt(int id, params (double Longitude, double Latitude)[] points)
        => new(Geometry<Point>.Create(points.Select(p => WebMercator(p.Longitude, p.Latitude)).ToList(), GeometryType.LineString, SridHelper.WebMercator)) { Id = id };

    private static VectorLayer Layer(string name, IVectorDataSource source)
        => new(name, source,
               new IRI.Maptor.Presentation.Wpf.VisualParameters(null, System.Windows.Media.Brushes.Red, 1, 1),
               IRI.Maptor.Presentation.Core.LayerType.VectorLayer, IRI.Maptor.Presentation.Core.RenderMode.Default,
               IRI.Maptor.Presentation.Core.RasterizationMethod.DrawingVisual, IRI.Maptor.Presentation.Core.Models.ScaleInterval.All);

    /// <summary>Runs the body on a thread with a pumping dispatcher, as a map layer's UI thread has.</summary>
    private static void OnUiThread(Func<Task> body)
    {
        Exception? failure = null;

        var thread = new Thread(() =>
        {
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));

            var frame = new DispatcherFrame();

            var task = body();

            task.ContinueWith(_ => frame.Continue = false, TaskScheduler.Default);

            Dispatcher.PushFrame(frame);

            failure = task.Exception?.GetBaseException();
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
            throw new InvalidOperationException($"Test body threw: {failure}", failure);
    }

    [Fact]
    public void Features_ComeBackInWgs84_AndTheLayerIsUnchanged() => OnUiThread(async () =>
    {
        var data = new MemoryDataSource(new[] { PointAt(7, 51.0, 35.7) }, resetIds: false);
        var source = new MapLayerFeatureSource(Layer("towers", data), "Tower");

        var feature = Assert.Single(await source.GetFeaturesAsync(null, Array.Empty<string>(), CancellationToken.None));

        Assert.Equal(7, feature.Id);
        Assert.Equal(SridHelper.GeodeticWGS84, feature.TheGeometry.Srid);
        Assert.Equal(51.0, feature.TheGeometry.Points![0].X, 9);
        Assert.Equal(35.7, feature.TheGeometry.Points[0].Y, 9);
        Assert.Equal("p7", feature.Attributes!["name"]);

        var original = Assert.Single((await Layer("towers", data).GetFeaturesAsync())!.Features);
        Assert.Equal(SridHelper.WebMercator, original.TheGeometry.Srid);

        Assert.Equal(SridHelper.GeodeticWGS84, source.Srid);
        Assert.Equal("Tower", source.EntityName);
        Assert.False(source.IsLive);
    });

    [Fact]
    public void BoxAndIdQueries_SelectTheRightFeatures() => OnUiThread(async () =>
    {
        var data = new MemoryDataSource(new[] { PointAt(1, 51.0, 35.7), PointAt(2, 51.5, 35.7), PointAt(3, 52.0, 36.0) }, resetIds: false);
        var source = new MapLayerFeatureSource(Layer("towers", data), "Tower");

        var inBox = await source.GetFeaturesAsync(new BoundingBox(50.9, 35.6, 51.6, 35.8), Array.Empty<string>(), CancellationToken.None);
        var byId = await source.GetFeaturesByIdsAsync(new long[] { 3, 42 }, Array.Empty<string>(), CancellationToken.None);

        Assert.Equal(new[] { 1, 2 }, inBox.Select(f => f.Id).OrderBy(i => i).ToArray());
        Assert.Equal(new[] { 3 }, byId.Select(f => f.Id).ToArray());
    });

    [Fact]
    public void ACheckOverMapLayers_MeasuresGroundMetres_NotWebMercatorUnits() => OnUiThread(async () =>
    {
        // A tower 0.8 m north of the route on the ground. In Web Mercator units it is about 0.99 "metres" off
        // (scale 1 / cos 35.7°), which would wrongly pass a 0.9 m tolerance or fail a 0.85 m one the other way.
        var metreOfLatitude = 1 / 110_950.0;

        var towers = new MemoryDataSource(new[] { PointAt(1, 51.0005, 35.7 + 0.8 * metreOfLatitude) }, resetIds: false);
        var routes = new MemoryDataSource(new[] { LineAt(10, (51.0, 35.7), (51.001, 35.7)) }, resetIds: false);

        var ruleSet = new TopologyRuleSet("t", 1,
            new[] { new TopologyLayer("Tower", GeometryType.Point), new TopologyLayer("Route", GeometryType.LineString) },
            new[] { new TopologyRule("onRoute", PointOnLineRuleType.TypeId, "Tower", "Route") });

        var result = await new ValidationRunner(StandardRuleTypes.CreateCatalog()).RunAsync(ruleSet, TopologyCheckScope.WholeData, new ITopologyFeatureSource[]
        {
            new MapLayerFeatureSource(Layer("towers", towers), "Tower"),
            new MapLayerFeatureSource(Layer("routes", routes), "Route"),
        });

        var issue = Assert.Single(result.Issues);

        Assert.Equal(PointOnLineRuleType.NearLine, issue.Kind);
        Assert.InRange(issue.Finding.Value!.Value, 0.79, 0.81);
        Assert.Equal(new[] { "Tower:1", "Route:10" }, issue.Finding.Features.Select(f => f.ToString()).ToArray());
    });

    [Fact]
    public void WatchChanges_ReportsReloadsAndEdits_UntilDisposed() => OnUiThread(() =>
    {
        var data = new MemoryDataSource(new[] { PointAt(1, 51.0, 35.7) }, resetIds: false);
        var source = new MapLayerFeatureSource(Layer("towers", data), "Tower");

        var calls = 0;

        using (source.WatchChanges(() => calls++))
        {
            data.Add(PointAt(2, 51.1, 35.7));
        }

        var afterDispose = calls;

        data.Add(PointAt(3, 51.2, 35.7));

        Assert.True(afterDispose > 0, "an edit was not reported");
        Assert.Equal(afterDispose, calls);

        return Task.CompletedTask;
    });
}
