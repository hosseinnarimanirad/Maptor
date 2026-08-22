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
using IRI.Maptor.Core.Persistence.DataSources;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Core.SpatialReferenceSystem.MapProjections;

namespace IRI.Maptor.Samples.Wpf.Gallery.Samples.Identify;

public partial class IdentifySample : UserControl
{
    private bool _initialized;

    public IdentifySample()
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

        // Two overlapping polygon layers, so one click can hit features in both.
        var regions = CreateSquaresLayer("Regions", size: 1.6, offset: 0.0, color: Colors.SteelBlue);
        var zones = CreateSquaresLayer("Zones", size: 0.7, offset: 0.5, color: Colors.DarkOrange);

        presenter.AddLayer(regions);
        presenter.AddLayer(zones);
        presenter.ZoomToExtent(regions.Extent, isExactExtent: false, isNewExtent: true);
    }

    /// <summary>A 3 × 3 grid of squares over western Europe; every square carries a few attributes.</summary>
    private static VectorLayer CreateSquaresLayer(string name, double size, double offset, Color color)
    {
        var features = new List<Feature<Point>>();

        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                double x0 = -2.0 + offset + col * 2.0, y0 = 44.0 + offset + row * 2.0;

                var ring = new List<Point> { new(x0, y0), new(x0 + size, y0), new(x0 + size, y0 + size), new(x0, y0 + size), new(x0, y0) };

                var polygon = Geometry<Point>.CreatePolygon(ring, SridHelper.GeodeticWGS84).Project(SrsBases.WebMercator);

                features.Add(new Feature<Point>(polygon, new Dictionary<string, object>
                {
                    ["Name"] = $"{name} {row * 3 + col + 1}",
                    ["Row"] = row,
                    ["Column"] = col,
                    ["Layer"] = name,
                }));
            }
        }

        var parameters = new VisualParameters(fill: color, stroke: Colors.Black, strokeThickness: 1, opacity: 0.5);

        return new VectorLayer(name, new MemoryDataSource(features), parameters,
            LayerType.VectorLayer, RenderMode.Default, RasterizationMethod.DrawingVisual, ScaleInterval.All);
    }
}
