using IRI.Maptor.Jab.Common.TileServices;
using IRI.Maptor.Jab.Controls.Presenter;
using IRI.Maptor.Sta.Common.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace IRI.Maptor.Tag.SampleWpfApp.LegendSamples;
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
