using System.Windows;
using IRI.Maptor.Presentation.Core.Data;
using IRI.Maptor.Presentation.Core.TileServices;
using IRI.Maptor.Presentation.Wpf.Models;
using IRI.Maptor.Presentation.Wpf.ViewModels;
using IRI.Maptor.Core.Common.Primitives;

namespace IRI.Maptor.Samples.Wpf.HelloMap;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        // 1. The view model. MapViewModelBase is abstract; an empty subclass is enough to start.
        var presenter = new HelloMapViewModel();

        // 2. Settings: proxy, base-map credentials/cache, map behaviour (initial extent, zoom limits), general.
        presenter.InitializeSettings(
            ProxySettings.Default,
            BaseMapSettings.Default,
            new MapSettings { InitialExtent = BoundingBoxes.WebMercator_Europe },
            GeneralSettings.Default);

        // 3. Connect the MapViewer control to the view model. This also wires the default dialogs
        //    (go to, symbology, layer settings) to this window.
        await MapInitializationHelper.InitializeMapAsync(map, this, presenter);

        DataContext = presenter;

        // 4. Choose a base map. Tiles are downloaded on demand.
        presenter.SelectedMapProvider = TileMapProviderFactory.GoogleRoadMap;
    }
}

/// <summary>Your application's map view model. Add commands and state here as the app grows.</summary>
public class HelloMapViewModel : MapViewModelBase
{
}
