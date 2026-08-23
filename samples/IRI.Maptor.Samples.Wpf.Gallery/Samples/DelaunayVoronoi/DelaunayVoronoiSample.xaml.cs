using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Persistence.DataSources;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Core.Data;
using IRI.Maptor.Presentation.Core.Layers;
using IRI.Maptor.Presentation.Core.Models;
using IRI.Maptor.Presentation.Core.TileServices;
using IRI.Maptor.Presentation.Wpf;
using IRI.Maptor.Presentation.Wpf.Layers;
using IRI.Maptor.Presentation.Wpf.Models;
using IRI.Maptor.Presentation.Wpf.ViewModels;
using IRI.Maptor.Samples.Wpf.Gallery.Shell;
using Point = IRI.Maptor.Core.Common.Primitives.Point;

namespace IRI.Maptor.Samples.Wpf.Gallery.Samples.DelaunayVoronoi;

/// <summary>
/// Scatters random points over Europe and draws the two structures Maptor derives from them.
/// The geometry is all in <see cref="DelaunayVoronoiBuilder"/>; this file only turns its three
/// feature lists into map layers and wires the two check boxes to their visibility.
/// </summary>
public partial class DelaunayVoronoiSample : UserControl
{
    private const string CellsLayerName = "Voronoi cells";

    private const string TrianglesLayerName = "Delaunay triangulation";

    private const string SitesLayerName = "Points";

    /// <summary>The area points are scattered over, and the rectangle the Voronoi cells are clipped to.</summary>
    private static readonly BoundingBox Extent = BoundingBoxes.WebMercator_Europe;

    private bool _initialized;

    public DelaunayVoronoiSample()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_initialized)
            return; // Loaded fires again each time the gallery re-attaches this view

        _initialized = true;

        var presenter = new GalleryMapViewModel();

        presenter.InitializeSettings(
            ProxySettings.Default,
            BaseMapSettings.Default,
            new MapSettings { InitialExtent = Extent },
            GeneralSettings.Default);

        await MapInitializationHelper.InitializeMapAsync(map, Window.GetWindow(this)!, presenter);

        DataContext = presenter;

        presenter.SelectedMapProvider = TileMapProviderFactory.GoogleRoadMap;

        Generate();
    }

    private void OnGenerate(object sender, RoutedEventArgs e) => Generate();

    private void Generate()
    {
        if (DataContext is not MapViewModelBase presenter)
            return;

        int count = (int)(pointCount.Value ?? DelaunayVoronoiBuilder.MinimumPointCount);

        var result = DelaunayVoronoiBuilder.Build(count, Extent);

        // the previous run goes before the new one is added, so Generate is repeatable
        presenter.ClearLayer(CellsLayerName);
        presenter.ClearLayer(TrianglesLayerName);
        presenter.ClearLayer(SitesLayerName);

        // added bottom-up: filled cells, the triangulation over them, the points on top
        AddLayer(presenter, CellsLayerName, result.Cells,
            new VisualParameters(fill: Colors.SteelBlue, stroke: Colors.White, strokeThickness: 1, opacity: 0.35));

        AddLayer(presenter, TrianglesLayerName, result.Triangles,
            new VisualParameters(fill: null, stroke: new SolidColorBrush(Colors.OrangeRed), strokeThickness: 1, opacity: 0.9));

        var sites = new VisualParameters(fill: Colors.Black, stroke: Colors.White, strokeThickness: 1, opacity: 1);

        // markers shrink as the set grows, otherwise a few hundred of them hide the structure they sit on
        double symbolSize = count <= 150 ? 7 : count <= 600 ? 5 : 3;

        sites.PointSymbol.SymbolWidth = symbolSize;

        sites.PointSymbol.SymbolHeight = symbolSize;

        AddLayer(presenter, SitesLayerName, result.Sites, sites);

        ApplyLayerVisibility();

        presenter.ZoomToExtent(Extent, isExactExtent: false, isNewExtent: true);

        status.Text =
            $"{result.Sites.Count} points, {result.Triangles.Count} triangles, {result.Cells.Count} cells " +
            $"({result.UnboundedCellCount} unbounded, clipped to the extent) in {result.Elapsed.TotalMilliseconds:F0} ms";
    }

    private static void AddLayer(MapViewModelBase presenter, string layerName, IReadOnlyList<Feature<Point>> features, VisualParameters parameters)
    {
        // the builder works in the map's own reference system, so the features go straight in
        var layer = new VectorLayer(
            layerName: layerName,
            dataSource: new MemoryDataSource(features),
            parameters: parameters,
            type: LayerType.VectorLayer,
            renderMode: RenderMode.Default,
            rasterizationMethod: RasterizationMethod.DrawingVisual,
            visibleRange: ScaleInterval.All);

        presenter.AddLayer(layer);
    }

    private void OnLayerVisibilityChanged(object sender, RoutedEventArgs e) => ApplyLayerVisibility();

    /// <summary>
    /// The check boxes only flip <see cref="ILayer.IsVisible"/>; nothing is recomputed, which is why
    /// switching between the two views is instant even with a few thousand points.
    /// </summary>
    private void ApplyLayerVisibility()
    {
        if (DataContext is not MapViewModelBase presenter)
            return;

        foreach (var layer in presenter.AllNonGroupLayers)
        {
            if (layer.LayerName == TrianglesLayerName)
                layer.IsVisible = showDelaunay.IsChecked == true;
            else if (layer.LayerName == CellsLayerName)
                layer.IsVisible = showVoronoi.IsChecked == true;
        }
    }
}
