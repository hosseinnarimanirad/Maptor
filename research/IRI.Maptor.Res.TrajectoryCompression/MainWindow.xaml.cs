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

        //try
        //{
        //    SqlServerTypes.Utilities.LoadNativeAssembliesv14(Environment.CurrentDirectory);
        //}
        //catch (Exception)
        //{
        //    throw;
        //}

        var presenter = new ApplicationPresenter();

        await this.map.Register(presenter);

        presenter.Initialize(this);

        //this.DataContext = presenter;

        presenter.RemoveAllProviders();
    }
      

    private async void Button_Click(object sender, RoutedEventArgs e)
    {
        // TestAlgo();
        // TestThresholdChangesAlgo();
        // await SimplificationHelper.GeneralTest();
        // SimplificationHelper.TestAPSC();

        await LRHelper.GeneralTest();
        //await LRHelper.InvestigateVisualDiff();
    }
      
}
