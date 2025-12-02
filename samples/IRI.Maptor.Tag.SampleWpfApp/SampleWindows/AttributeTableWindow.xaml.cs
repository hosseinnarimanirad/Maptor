using System.Text;
using System.Windows;

using IRI.Maptor.Jab.Controls.Common; 
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Jab.Common.TileServices;
using IRI.Maptor.Tag.SampleWpfApp.ViewModel;

namespace IRI.Maptor.Tag.SampleWpfApp.SampleWindows;
/// <summary>
/// Interaction logic for TableOfContentWindow.xaml
/// </summary>
public partial class TableOfContentWindow : Window
{
    public TableOfContentWindow()
    {
        InitializeComponent();
    }


    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // initial setup
        System.Text.Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        // Initialize map presenter (viewmodel)
        var config = new MapViewerConfiguration
        {
            InitialExtent = BoundingBoxes.WebMercator_Africa
        };

        var presenter = await MapInitializationHelper.InitializeMapAsync(
            this.map,
            this,
            new AppViewModel(),
            config);

        this.DataContext = presenter;

        // Configure initial view
        presenter.SelectedMapProvider = TileMapProviderFactory.GoogleRoadMap;
    }
     
}
