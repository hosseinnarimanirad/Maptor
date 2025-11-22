using System.Windows;

using IRI.Maptor.Sta.MachineLearning;
using IRI.Maptor.Res.LRSimplification.Common;
using IRI.Maptor.Jab.Common;
using IRI.Maptor.Jab.Controls.Common;

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
        var presenter = await MapInitializationHelper.InitializeMapAsync(
            this.map,
            this,
            new ViewModel.ApplicationPresenter());

        this.DataContext = presenter;

        presenter.RemoveAllProviders();
         

        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance); 

    }
}