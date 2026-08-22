using System.Windows;
using System.Windows.Controls;
using IRI.Maptor.Presentation.Core.Data;
using IRI.Maptor.Presentation.Core.TileServices;
using IRI.Maptor.Presentation.Wpf.Models;
using IRI.Maptor.Samples.Wpf.Gallery.Shell;
using IRI.Maptor.Core.Common.Primitives;
using Point = IRI.Maptor.Core.Common.Primitives.Point;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace IRI.Maptor.Samples.Wpf.Gallery.Samples.AttributeTable;

public partial class AttributeTableSample : UserControl
{
    private bool _initialized;

    public AttributeTableSample()
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

        var layer = CreateCitiesLayer();

        presenter.AddLayer(layer);
        presenter.ZoomToExtent(layer.Extent, isExactExtent: false, isNewExtent: true);

        // Open the layer's attribute table right away — this is what the legend's
        // "show attributes" command does when the user clicks it.
        var features = await layer.GetFeaturesAsync();
        var selected = new SelectedLayer(presenter.DialogService, layer, layer.GetFields());

        if (features != null)
            selected.Features = new ObservableCollection<Feature<Point>>(features.Features);

        await presenter.AddSelectedLayer(selected);
    }

    /// <summary>A point layer with a few attributes per feature, built in memory.</summary>
    private static VectorLayer CreateCitiesLayer()
    {
        (string name, string country, int population, double lon, double lat)[] cities =
        [
            ("London", "United Kingdom", 8_900_000, -0.128, 51.507),
            ("Paris", "France", 2_100_000, 2.352, 48.857),
            ("Berlin", "Germany", 3_700_000, 13.405, 52.520),
            ("Madrid", "Spain", 3_300_000, -3.704, 40.417),
            ("Rome", "Italy", 2_800_000, 12.496, 41.903),
            ("Amsterdam", "Netherlands", 900_000, 4.904, 52.368),
            ("Vienna", "Austria", 1_900_000, 16.373, 48.208),
            ("Prague", "Czech Republic", 1_300_000, 14.438, 50.076),
        ];

        var features = new List<Feature<Point>>();

        foreach (var (name, country, population, lon, lat) in cities)
        {
            var point = Geometry<Point>.Create(lon, lat, SridHelper.GeodeticWGS84).Project(SrsBases.WebMercator);

            features.Add(new Feature<Point>(point, new Dictionary<string, object>
            {
                ["Name"] = name,
                ["Country"] = country,
                ["Population"] = population,
            }));
        }

        var parameters = new VisualParameters(fill: Colors.Crimson, stroke: Colors.White, strokeThickness: 1.5, opacity: 1)
        {
            PointSymbol = new IRI.Maptor.Presentation.Wpf.Cartography.Symbologies.SimplePointSymbolizer(pointSize: 12),
        };

        return new VectorLayer("Cities", new MemoryDataSource(features), parameters,
            LayerType.VectorLayer, RenderMode.Default, RasterizationMethod.DrawingVisual, ScaleInterval.All);
    }
}
