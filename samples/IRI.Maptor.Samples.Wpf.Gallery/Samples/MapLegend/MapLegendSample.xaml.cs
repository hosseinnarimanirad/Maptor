using System.Windows;
using System.Windows.Controls;
using IRI.Maptor.Presentation.Core.Data;
using IRI.Maptor.Presentation.Core.TileServices;
using IRI.Maptor.Presentation.Wpf.Models;
using IRI.Maptor.Samples.Wpf.Gallery.Shell;
using IRI.Maptor.Core.Common.Primitives;
using Point = IRI.Maptor.Core.Common.Primitives.Point;
using System.Collections.Generic;
using System.Windows.Media;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Wpf;
using IRI.Maptor.Presentation.Core.Models;
using IRI.Maptor.Presentation.Wpf.Layers;
using IRI.Maptor.Presentation.Wpf.ViewModels;
using IRI.Maptor.Core.Persistence.DataSources;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Core.SpatialReferenceSystem.MapProjections;

namespace IRI.Maptor.Samples.Wpf.Gallery.Samples.MapLegend;

public partial class MapLegendSample : UserControl
{
    private bool _initialized;
    private int _layerCount;

    public MapLegendSample()
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
            new MapSettings { InitialExtent = BoundingBoxes.WebMercator_Europe },
            GeneralSettings.Default);

        // Connects the MapViewer to the view model and wires the default dialogs to the owner window.
        await MapInitializationHelper.InitializeMapAsync(map, Window.GetWindow(this)!, presenter);

        DataContext = presenter;

        presenter.SelectedMapProvider = TileMapProviderFactory.GoogleRoadMap;

        AddSampleLayer(presenter);
    }

    private void OnAddSampleLayer(object sender, RoutedEventArgs e)
    {
        if (DataContext is MapViewModelBase presenter)
            AddSampleLayer(presenter);
    }

    /// <summary>An in-memory polygon layer: a ring of squares around Paris, each one a feature with attributes.</summary>
    private void AddSampleLayer(MapViewModelBase presenter)
    {
        _layerCount++;

        var features = new List<Feature<Point>>();

        for (int i = 0; i < 8; i++)
        {
            double angle = i * System.Math.PI / 4;
            double cx = 2.35 + _layerCount * 1.2 * System.Math.Cos(angle);
            double cy = 48.86 + _layerCount * 1.2 * System.Math.Sin(angle);

            var ring = new List<Point> { new(cx - 0.3, cy - 0.3), new(cx + 0.3, cy - 0.3), new(cx + 0.3, cy + 0.3), new(cx - 0.3, cy + 0.3), new(cx - 0.3, cy - 0.3) };

            // Geometries are built in WGS 84 and projected to Web Mercator, the map's internal system.
            var polygon = Geometry<Point>.CreatePolygon(ring, SridHelper.GeodeticWGS84).Project(SrsBases.WebMercator);

            features.Add(new Feature<Point>(polygon, new Dictionary<string, object> { ["Name"] = $"Square {i + 1}", ["Ring"] = _layerCount }));
        }

        var colors = new[] { Colors.SteelBlue, Colors.DarkOrange, Colors.SeaGreen, Colors.MediumPurple };
        var color = colors[(_layerCount - 1) % colors.Length];

        var layer = new VectorLayer(
            layerName: $"Sample squares {_layerCount}",
            dataSource: new MemoryDataSource(features),
            parameters: new VisualParameters(fill: color, stroke: Colors.Black, strokeThickness: 1, opacity: 0.6),
            type: LayerType.VectorLayer,
            renderMode: RenderMode.Default,
            rasterizationMethod: RasterizationMethod.DrawingVisual,
            visibleRange: ScaleInterval.All);

        presenter.AddLayer(layer);

        if (_layerCount == 1)
            presenter.ZoomToExtent(layer.Extent, isExactExtent: false, isNewExtent: true);
    }
}
