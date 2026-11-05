using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Presentation.Wpf.Layers;
using IRI.Maptor.Presentation.Wpf.Models.Identify;
using IRI.Maptor.Presentation.Wpf.ViewModels.Identify;
using IRI.Maptor.Tests.Common;

using Xunit;

namespace IRI.Maptor.Tests.Mapping;

/// <summary>
/// Layer actions in the identify window (<see cref="VectorLayer.IdentifyFeatureActions"/>): shown for one
/// selected feature of their layer only, labelled from the action's state, never relabelled by a late
/// answer for an earlier selection, and refreshed after they run.
/// </summary>
[Collection(WpfCollection.Name)]
public class IdentifyFeatureActionsTest
{
    // any VectorLayer serves as the data layer; a drawing layer is the simplest one to build
    private static VectorLayer CreateLayer(string name)
    {
        var feature = Geometry<Point>.Create(10, 20, SridHelper.WebMercator).AsFeature();

        return DrawingItemLayer.Create(name, feature, [])!;
    }

    private static FeatureSet<Point> Hits(string name, params Feature<Point>[] features) => FeatureSet<Point>.Create(name, features);

    private static Feature<Point> NewFeature(int id, double x = 10, double y = 20)
    {
        var feature = Geometry<Point>.Create(x, y, SridHelper.WebMercator).AsFeature();
        feature.Id = id;
        return feature;
    }

    [Fact]
    public void FeatureOfALayerWithActionsShowsThemLabelledByTheirState()
    {
        WpfTestHost.Run(() =>
        {
            var layer = CreateLayer("towers");

            layer.IdentifyFeatureActions.Add(new IdentifyFeatureAction
            {
                Text = "Images",
                GetStateAsync = (_, feature, _) => Task.FromResult<IdentifyFeatureActionState?>(new($"Images ({feature.Id})", "tip", Badge: feature.Id.ToString())),
                ExecuteAsync = (_, _) => Task.CompletedTask,
            });

            var viewModel = new IdentifyResultsViewModel();

            viewModel.Update(new Point(10, 20), [(layer, Hits("towers", NewFeature(3), NewFeature(7)))]);

            // Update selects the first feature
            Assert.True(viewModel.HasFeatureActions);
            var item = Assert.Single(viewModel.FeatureActions);
            Assert.Equal("Images (3)", item.Text);
            Assert.Equal("tip", item.ToolTip);
            Assert.Equal("3", item.Badge);
            Assert.True(item.HasBadge);
            Assert.Same(layer, item.Layer);

            viewModel.SelectedNode = viewModel.Layers[0].Features[1];

            Assert.Equal("Images (7)", Assert.Single(viewModel.FeatureActions).Text);
        });
    }

    [Fact]
    public void LayerRowsAndLayersWithoutActionsShowNone()
    {
        WpfTestHost.Run(() =>
        {
            var towers = CreateLayer("towers");

            towers.IdentifyFeatureActions.Add(new IdentifyFeatureAction { Text = "Images", ExecuteAsync = (_, _) => Task.CompletedTask });

            var roads = CreateLayer("roads");

            var viewModel = new IdentifyResultsViewModel();

            viewModel.Update(new Point(10, 20), [(roads, Hits("roads", NewFeature(1))), (towers, Hits("towers", NewFeature(2)))]);

            // first feature belongs to roads, which has no actions
            Assert.False(viewModel.HasFeatureActions);
            Assert.Empty(viewModel.FeatureActions);

            // the towers layer row: an action is for one feature, not a whole layer
            viewModel.SelectedNode = viewModel.Layers[1];
            Assert.Empty(viewModel.FeatureActions);

            // a tower feature: shown, with its default label (no state delegate)
            viewModel.SelectedNode = viewModel.Layers[1].Features[0];
            Assert.Equal("Images", Assert.Single(viewModel.FeatureActions).Text);

            // nothing selected
            viewModel.Clear();
            Assert.Empty(viewModel.FeatureActions);
            Assert.False(viewModel.HasFeatureActions);
        });
    }

    [Fact]
    public void ALateAnswerForAnEarlierSelectionIsIgnored()
    {
        WpfTestHost.Run(() =>
        {
            var layer = CreateLayer("towers");

            var pending = new Dictionary<int, TaskCompletionSource<IdentifyFeatureActionState?>>();
            var tokens = new Dictionary<int, CancellationToken>();

            layer.IdentifyFeatureActions.Add(new IdentifyFeatureAction
            {
                Text = "Images",
                GetStateAsync = (_, feature, token) =>
                {
                    var source = new TaskCompletionSource<IdentifyFeatureActionState?>();
                    pending[feature.Id] = source;
                    tokens[feature.Id] = token;
                    return source.Task;
                },
                ExecuteAsync = (_, _) => Task.CompletedTask,
            });

            var viewModel = new IdentifyResultsViewModel();

            viewModel.Update(new Point(10, 20), [(layer, Hits("towers", NewFeature(1), NewFeature(2)))]);

            var first = Assert.Single(viewModel.FeatureActions);
            Assert.True(first.IsLoading);

            viewModel.SelectedNode = viewModel.Layers[0].Features[1];

            var second = Assert.Single(viewModel.FeatureActions);

            Assert.True(tokens[1].IsCancellationRequested);
            Assert.False(tokens[2].IsCancellationRequested);

            // tower 1 answers after the user moved on: nothing changes
            pending[1].SetResult(new IdentifyFeatureActionState("Images (5)"));

            Assert.Equal("Images", first.Text);
            Assert.Equal("Images", second.Text);
            Assert.True(second.IsLoading);

            pending[2].SetResult(new IdentifyFeatureActionState("No images", IsEnabled: false));

            Assert.Equal("No images", second.Text);
            Assert.False(second.IsEnabled);
            Assert.False(second.IsLoading);
        });
    }

    [Fact]
    public void RunningAnActionPassesTheFeatureAndRefreshesTheLabel()
    {
        WpfTestHost.Run(() =>
        {
            var layer = CreateLayer("towers");

            var stateCalls = 0;
            var count = 0;
            (VectorLayer Layer, Feature<Point> Feature)? executed = null;

            layer.IdentifyFeatureActions.Add(new IdentifyFeatureAction
            {
                Text = "Images",
                GetStateAsync = (_, _, _) =>
                {
                    stateCalls++;
                    return Task.FromResult<IdentifyFeatureActionState?>(new($"Images ({count})"));
                },
                ExecuteAsync = (l, f) =>
                {
                    executed = (l, f);
                    count = 4; // e.g. photos uploaded in the gallery the action opened
                    return Task.CompletedTask;
                },
            });

            var viewModel = new IdentifyResultsViewModel();

            var feature = NewFeature(9);

            viewModel.Update(new Point(10, 20), [(layer, Hits("towers", feature))]);

            var item = Assert.Single(viewModel.FeatureActions);
            Assert.Equal("Images (0)", item.Text);
            Assert.Equal(1, stateCalls);

            Assert.True(item.Command.CanExecute(null));
            item.Command.Execute(null);

            Assert.NotNull(executed);
            Assert.Same(layer, executed!.Value.Layer);
            Assert.Same(feature, executed.Value.Feature);

            Assert.Equal(2, stateCalls);
            Assert.Equal("Images (4)", item.Text);
        });
    }

    [Fact]
    public void AFailingStateRequestKeepsTheDefaultButton()
    {
        WpfTestHost.Run(() =>
        {
            var layer = CreateLayer("towers");

            layer.IdentifyFeatureActions.Add(new IdentifyFeatureAction
            {
                Text = "Images",
                ToolTip = "Tower images",
                GetStateAsync = (_, _, _) => throw new InvalidOperationException("server down"),
                ExecuteAsync = (_, _) => Task.CompletedTask,
            });

            var viewModel = new IdentifyResultsViewModel();

            viewModel.Update(new Point(10, 20), [(layer, Hits("towers", NewFeature(1)))]);

            var item = Assert.Single(viewModel.FeatureActions);

            Assert.Equal("Images", item.Text);
            Assert.Equal("Tower images", item.ToolTip);
            Assert.True(item.IsEnabled);
            Assert.True(item.IsVisible);
            Assert.False(item.IsLoading);
            Assert.True(item.Command.CanExecute(null));
        });
    }
}
