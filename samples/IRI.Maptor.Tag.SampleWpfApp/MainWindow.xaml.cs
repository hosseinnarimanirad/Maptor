using System;
using System.Globalization;
using System.Text;
using System.Windows;
using IRI.Maptor.Jab.Common;
using IRI.Maptor.Jab.Common.Localization;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Jab.Common.Models;
using IRI.Maptor.Jab.Core.Data;
using IRI.Maptor.Jab.Core.TileServices;

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

        var config = MapSettings.Default;
        config.InitialExtent = BoundingBoxes.WebMercator_Africa;

        var presenter = new ViewModel.AppViewModel();

        presenter.InitializeSettings(ProxySettings.Default, BaseMapSettings.Default, config, GeneralSettings.Default);

        await MapInitializationHelper.InitializeMapAsync(this.map, this, presenter);
        //new ViewModel.AppViewModel(), 
        //ProxySettings.Default,
        //BaseMapSettings.Default,
        //config,
        //GeneralSettings.Default);

        this.DataContext = presenter;

        presenter.SelectedMapProvider = TileMapProviderFactory.GoogleRoadMap;
        //LocalizationManager.Instance.SetCulture(CultureInfo.GetCultureInfo("fa-IR"));

    }
}
