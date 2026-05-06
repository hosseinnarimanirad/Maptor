using System.Text;
using System.Windows;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Jab.Common.TileServices;
using IRI.Maptor.Tag.SampleWpfApp.ViewModel;
using IRI.Maptor.Jab.Common.Data;
using IRI.Maptor.Jab.Common.Models;

namespace IRI.Maptor.Tag.SampleWpfApp.SampleWindows;
/// <summary>
/// Interaction logic for DrawingLegendWindow.xaml
/// </summary>
public partial class DrawingLegendWindow : Window
{
    public DrawingLegendWindow()
    {
        InitializeComponent();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // initial setup
        System.Text.Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        // Initialize map presenter (viewmodel)
        var config = new MapSettings()
        {
            InitialExtent = BoundingBoxes.WebMercator_Africa
        };
         
        var presenter = new ViewModel.AppViewModel();

        presenter.InitializeSettings(ProxySettings.Default, BaseMapSettings.Default, config, GeneralSettings.Default);

        await MapInitializationHelper.InitializeMapAsync(this.map, this, presenter);


        this.DataContext = presenter;

        // Configure initial view
        presenter.SelectedMapProvider = TileMapProviderFactory.GoogleRoadMap;
    }
}
