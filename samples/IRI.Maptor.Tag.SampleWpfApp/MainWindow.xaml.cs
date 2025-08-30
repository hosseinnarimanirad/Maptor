using System;
using System.Globalization;
using System.Text;
using System.Windows;
using IRI.Maptor.Jab.Common;
using IRI.Maptor.Jab.Common.TileServices;
using IRI.Maptor.Sta.Common.Primitives;

namespace IRI.Maptor.Tag.SampleWpfApp;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        System.Text.Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        //try
        //{
        //    SqlServerTypes.Utilities.LoadNativeAssembliesv14(Environment.CurrentDirectory);
        //}
        //catch
        //{
        //    MessageBox.Show("error!");
        //}

        var presenter = new ViewModel.AppViewModel();

        await this.map.Register(presenter);

        presenter.Initialize(this);

        //this.DataContext = presenter;
         
        presenter.ZoomToExtent(BoundingBoxes.WebMercator_Africa, false, isNewExtent: true);

        presenter.SelectedMapProvider = TileMapProviderFactory.GoogleRoadMap;
    }
}
