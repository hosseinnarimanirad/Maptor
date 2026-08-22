using System.Windows;

using IRI.Maptor.Core.MachineLearning;
using IRI.Maptor.Res.LRSimplification.Common;
using IRI.Maptor.Presentation.Wpf;
using IRI.Maptor.Presentation.Wpf.Models;
using IRI.Maptor.Presentation.Core.Data;

namespace IRI.Maptor.Res.LRSimplification;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    ViewModel.ApplicationPresenter Presenter { get { return this.DataContext as ViewModel.ApplicationPresenter; } }

    public MainWindow()
    {
        InitializeComponent();
    }


    private async void createData_Click(object sender, RoutedEventArgs e)
    {
        var outputFileName = await Presenter.DialogService.ShowSaveFileDialogAsync("*.sdjson|*.sdjson", null, $"{DateTime.Now.ToLongTimeString().Replace(':', '-')}.sdjson");

        if (string.IsNullOrWhiteSpace(outputFileName))
            return;

        SyntheticDataHelper.PrintAll(outputFileName);

        var data = SyntheticDataFactory.CreateAll();

        SyntheticDataFile file = new SyntheticDataFile()
        {
            LineSamples = data,
            Features = new List<LRSimplificationFeatures>()
            {
                LRSimplificationFeatures.Area,
                LRSimplificationFeatures.BaseLength,
                LRSimplificationFeatures.CosineOfAngle,
                LRSimplificationFeatures.DistanceToNext,
                LRSimplificationFeatures.DistanceToPrevious,
                LRSimplificationFeatures.SquareCosineOfAngle,
                LRSimplificationFeatures.VerticalDistance,
                LRSimplificationFeatures.dX12,
                LRSimplificationFeatures.dX13,
                LRSimplificationFeatures.dX23,
                LRSimplificationFeatures.dY12,
                LRSimplificationFeatures.dY13,
                LRSimplificationFeatures.dY23

            }
        };

        file.Save(outputFileName);
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var presenter = new ViewModel.ApplicationPresenter();

        var mapSettings = MapSettings.Default ;
        mapSettings.AllowLargeDataLoading = true;
        mapSettings.ShowTileBorder = false;

        presenter.InitializeSettings(ProxySettings.Default, BaseMapSettings.Default, mapSettings, GeneralSettings.Default);

        await MapInitializationHelper.InitializeMapAsync(this.map, this, presenter);

        //new ViewModel.ApplicationPresenter(),
        //ProxySettings.Default,
        //BaseMapSettings.Default,
        //MapSettings.Default,
        //GeneralSettings.Default);

        this.DataContext = presenter;

        presenter.RemoveAllProviders();


        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

    }
}