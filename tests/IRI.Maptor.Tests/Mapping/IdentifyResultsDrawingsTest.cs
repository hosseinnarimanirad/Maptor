using System.Collections.Generic;
using System.Linq;

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
/// Drawing items in the identify results: one drawings group after the data layers, and a
/// selected drawing row handed to the drawing legend instead of the highlight overlay.
/// </summary>
[Collection(WpfCollection.Name)]
public class IdentifyResultsDrawingsTest
{
    private static DrawingItemLayer CreateDrawing(string name, double x = 10, double y = 20)
    {
        var feature = Geometry<Point>.Create(x, y, SridHelper.WebMercator).AsFeature();

        return DrawingItemLayer.Create(name, feature, [])!;
    }

    // any VectorLayer serves as the data layer; a drawing layer is the simplest one to build
    private static (VectorLayer Layer, FeatureSet<Point> Hits) CreateDataHit(string name)
    {
        var layer = CreateDrawing(name);

        return (layer, FeatureSet<Point>.Create(name, [layer.Feature!]));
    }

    private sealed class Recorder
    {
        public List<DrawingItemLayer?> SelectedDrawings { get; } = [];

        public int HighlightCalls { get; set; }

        public IdentifyResultsViewModel Attach(IdentifyResultsViewModel viewModel)
        {
            viewModel.RequestSelectDrawing = d => SelectedDrawings.Add(d);

            viewModel.RequestHighlightAsync = (_, _) =>
            {
                HighlightCalls++;
                return System.Threading.Tasks.Task.CompletedTask;
            };

            return viewModel;
        }

        public void Reset()
        {
            SelectedDrawings.Clear();
            HighlightCalls = 0;
        }
    }

    [Fact]
    public void DrawingsGroupIsListedAfterTheDataLayers()
    {
        WpfTestHost.Run(() =>
        {
            var viewModel = new IdentifyResultsViewModel();

            var first = CreateDrawing("first drawing");
            var second = CreateDrawing("second drawing");

            viewModel.Update(new Point(10, 20), [CreateDataHit("roads"), CreateDataHit("parcels")], [first, second]);

            Assert.Equal(3, viewModel.Layers.Count);

            Assert.False(viewModel.Layers[0].IsDrawingGroup);
            Assert.False(viewModel.Layers[1].IsDrawingGroup);

            var group = viewModel.Layers[2];

            Assert.True(group.IsDrawingGroup);
            Assert.Null(group.Layer);
            Assert.Equal(2, group.Count);
            Assert.Equal(["first drawing", "second drawing"], group.Features.Select(f => f.Title));
            Assert.Same(first, group.Features[0].Drawing);

            // the drawings count like any other result
            Assert.Equal(4, viewModel.TotalFeatureCount);
        });
    }

    [Fact]
    public void NoDrawingsMeansNoGroup()
    {
        WpfTestHost.Run(() =>
        {
            var viewModel = new IdentifyResultsViewModel();

            viewModel.Update(new Point(10, 20), [CreateDataHit("roads")], []);

            Assert.Single(viewModel.Layers);
            Assert.DoesNotContain(viewModel.Layers, l => l.IsDrawingGroup);
        });
    }

    [Fact]
    public void OnlyDrawingsStillGivesResults()
    {
        WpfTestHost.Run(() =>
        {
            var viewModel = new IdentifyResultsViewModel();

            var drawing = CreateDrawing("only drawing");

            viewModel.Update(new Point(10, 20), [], [drawing]);

            Assert.True(viewModel.HasResults);
            Assert.True(viewModel.Layers.Single().IsDrawingGroup);

            // the first result is selected as usual, here the drawing
            Assert.Same(drawing, viewModel.SelectedFeature?.Drawing);
        });
    }

    [Fact]
    public void SelectingADrawingRowSelectsItInTheLegendWithoutTheOverlay()
    {
        WpfTestHost.Run(() =>
        {
            var recorder = new Recorder();
            var viewModel = recorder.Attach(new IdentifyResultsViewModel());

            var drawing = CreateDrawing("drawing");

            viewModel.Update(new Point(10, 20), [CreateDataHit("roads")], [drawing]);

            recorder.Reset();

            viewModel.SelectedNode = viewModel.Layers[1].Features[0];

            Assert.Equal([drawing], recorder.SelectedDrawings);
            Assert.Equal(0, recorder.HighlightCalls);
        });
    }

    [Fact]
    public void LeavingADrawingRowReleasesTheLegendSelection()
    {
        WpfTestHost.Run(() =>
        {
            var recorder = new Recorder();
            var viewModel = recorder.Attach(new IdentifyResultsViewModel());

            var drawing = CreateDrawing("drawing");

            viewModel.Update(new Point(10, 20), [CreateDataHit("roads")], [drawing]);

            viewModel.SelectedNode = viewModel.Layers[1].Features[0];

            recorder.Reset();

            // a data feature: release, then the overlay
            viewModel.SelectedNode = viewModel.Layers[0].Features[0];

            Assert.Equal([(DrawingItemLayer?)null], recorder.SelectedDrawings);
            Assert.Equal(1, recorder.HighlightCalls);

            recorder.Reset();

            // the group row stands for all its drawings: overlay, no legend selection
            viewModel.SelectedNode = viewModel.Layers[1];

            Assert.Equal([(DrawingItemLayer?)null], recorder.SelectedDrawings);
            Assert.Equal(1, recorder.HighlightCalls);
        });
    }

    [Fact]
    public void ClosingTheWindowReleasesTheLegendSelection()
    {
        WpfTestHost.Run(() =>
        {
            var recorder = new Recorder();
            var viewModel = recorder.Attach(new IdentifyResultsViewModel());

            viewModel.Update(new Point(10, 20), [], [CreateDrawing("drawing")]);

            recorder.Reset();

            viewModel.Clear();

            Assert.Equal([(DrawingItemLayer?)null], recorder.SelectedDrawings);
        });
    }

    [Fact]
    public void AHiddenDrawingKeepsTheOverlay()
    {
        WpfTestHost.Run(() =>
        {
            var recorder = new Recorder();
            var viewModel = recorder.Attach(new IdentifyResultsViewModel());

            var drawing = CreateDrawing("hidden drawing");

            drawing.Visibility = System.Windows.Visibility.Collapsed;

            viewModel.Update(new Point(10, 20), [CreateDataHit("roads")], [drawing]);

            recorder.Reset();

            viewModel.SelectedNode = viewModel.Layers[1].Features[0];

            // the legend draws no highlight for a hidden drawing, so the overlay stays
            Assert.Equal([(DrawingItemLayer?)null], recorder.SelectedDrawings);
            Assert.Equal(1, recorder.HighlightCalls);
        });
    }

    /// <summary>
    /// The view's templates (pencil icon, rule above the group, hidden fields row) load and lay
    /// out with a drawings group both as a tree row and as the selected layer.
    /// </summary>
    [Fact]
    public void TheViewRendersADrawingsGroup()
    {
        WpfTestHost.Run(() =>
        {
            var viewModel = new IdentifyResultsViewModel();

            viewModel.Update(new Point(10, 20), [CreateDataHit("roads")], [CreateDrawing("drawing")]);

            viewModel.SelectedNode = viewModel.Layers[1];

            // what a host app merges into its Application resources; removed again below so the
            // shared test Application stays as other WPF tests expect it
            var appDictionaries = new[]
            {
                "pack://application:,,,/MahApps.Metro;component/Styles/Controls.xaml",
                "pack://application:,,,/MahApps.Metro;component/Styles/Fonts.xaml",
                "pack://application:,,,/MahApps.Metro;component/Styles/Themes/Light.Blue.xaml",
                "pack://application:,,,/IRI.Maptor.Presentation.Wpf;component/Assets/IRI.Maptor.Colors.xaml",
            }.Select(u => new System.Windows.ResourceDictionary { Source = new System.Uri(u, System.UriKind.Absolute) }).ToList();

            var merged = System.Windows.Application.Current.Resources.MergedDictionaries;

            appDictionaries.ForEach(merged.Add);

            System.Windows.Window? window = null;

            try
            {
                var view = new IRI.Maptor.Presentation.Wpf.Controls.Identify.IdentifyResultsView { DataContext = viewModel };

                window = new System.Windows.Window
                {
                    Content = view,
                    Width = 700,
                    Height = 400,
                    Left = -10000,
                    Top = -10000,
                    ShowActivated = false,
                    ShowInTaskbar = false,
                    WindowStyle = System.Windows.WindowStyle.None
                };

                window.Show();

                window.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);

                Assert.True(view.ActualWidth > 0);
            }
            finally
            {
                window?.Close();

                appDictionaries.ForEach(d => merged.Remove(d));
            }
        });
    }

    [Fact]
    public void TheFilterMatchesDrawingNames()
    {
        WpfTestHost.Run(() =>
        {
            var viewModel = new IdentifyResultsViewModel();

            viewModel.Update(new Point(10, 20), [], [CreateDrawing("north fence"), CreateDrawing("south gate")]);

            viewModel.FilterText = "fence";

            var group = viewModel.Layers.Single();

            Assert.Equal(["north fence"], group.VisibleFeatures.Select(f => f.Title));
        });
    }
}
