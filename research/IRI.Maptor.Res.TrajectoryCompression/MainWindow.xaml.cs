using IRI.Maptor.Jab.Common;
using IRI.Maptor.Jab.Common.Data;
using IRI.Maptor.Jab.Controls.Common;
using System;
using System.Text;
using System.Windows;


namespace IRI.Maptor.Res.TrajectoryCompression;

public partial class MainWindow : Window
{
    ApplicationPresenter? Presenter { get { return this.DataContext as ApplicationPresenter; } }

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

        //new ApplicationPresenter(),
        //ProxySettings.Default,
        //BaseMapSettings.Default,
        //MapSettings.Default,
        //GeneralSettings.Default);

        this.DataContext = presenter;

        presenter.RemoveAllProviders();
    }


    private async void Button_Click(object sender, RoutedEventArgs e)
    {
        // TestAlgo();
        // TestThresholdChangesAlgo();
        // await SimplificationHelper.GeneralTest();
        // SimplificationHelper.TestAPSC();

        //await LRHelper.GeneralTest();
        //await LRHelper.InvestigateVisualDiff();
    }

}
