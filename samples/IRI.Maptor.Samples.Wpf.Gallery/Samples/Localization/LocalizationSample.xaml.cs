using System.Windows;
using System.Windows.Controls;
using IRI.Maptor.Presentation.Core.Data;
using IRI.Maptor.Presentation.Core.TileServices;
using IRI.Maptor.Presentation.Wpf.Models;
using IRI.Maptor.Samples.Wpf.Gallery.Shell;
using IRI.Maptor.Core.Common.Primitives;
using Point = IRI.Maptor.Core.Common.Primitives.Point;
using IRI.Maptor.Presentation.Wpf.Localization;

namespace IRI.Maptor.Samples.Wpf.Gallery.Samples.Localization;

public partial class LocalizationSample : UserControl
{
    private bool _initialized;

    public LocalizationSample()
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

        // LanguageCombo lists the cultures Maptor ships and calls LocalizationManager.SetCulture on change.
        languageCombo.DataContext = new LanguageSelectorViewModel();
    }
}
