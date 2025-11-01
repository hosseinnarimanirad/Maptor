using System.Text;
using System.Windows;

using IRI.Maptor.Jab.Common.TileServices;
using IRI.Maptor.Jab.Controls.Presenters;
using IRI.Maptor.Sta.Common.Primitives;

namespace IRI.Maptor.Tag.SampleWpfApp.SampleWindows;
/// <summary>
/// Interaction logic for AllLegendsWindow.xaml
/// </summary>
public partial class AllLegendsWindow : Window
{
    public AllLegendsWindow()
    {
        InitializeComponent();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // initial setup
        System.Text.Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        // Initialize map presenter (viewmodel)
        var presenter = new MapApplicationPresenter();
        await this.map.Register(presenter);
        presenter.Initialize(this);

        // Configure initial view
        presenter.SelectedMapProvider = TileMapProviderFactory.GoogleRoadMap;
        presenter.ZoomToExtent(BoundingBoxes.WebMercator_Africa, false, isNewExtent: true);
    }
}
