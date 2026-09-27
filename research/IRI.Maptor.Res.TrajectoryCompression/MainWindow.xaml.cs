using System.Text;
using System.Windows;

using IRI.Maptor.Presentation.Wpf;
using IRI.Maptor.Presentation.Wpf.Models;
using IRI.Maptor.Presentation.Core.Data;


namespace IRI.Maptor.Res.TrajectoryCompression;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var presenter = new ApplicationPresenter();

        presenter.InitializeSettings(ProxySettings.Default, BaseMapSettings.Default, MapSettings.Default, GeneralSettings.Default);

        await MapInitializationHelper.InitializeMapAsync(this.map, this, presenter);

        this.DataContext = presenter;

        presenter.RemoveAllProviders();
    }

    private async void Button_Click(object sender, RoutedEventArgs e)
    {
        // the published assessment; other experiments: SimplificationHelper.GeneralTest(SimplificationHelper.Experiment.X)
        await SimplificationHelper.GeneralTest();
    }
}
