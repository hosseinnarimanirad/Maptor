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

    private readonly TaskCompletionSource _ready = new();

    /// <summary>Completes when the map and the presenter are initialized (end of <see cref="Window_Loaded"/>).</summary>
    public Task Ready => _ready.Task;

    /// <summary>
    /// `--snapshot` mode (2026-10-08): loads a feature into the comparison panel, runs it at <paramref name="level"/> (or the
    /// feature's natural LoD for a 256-px screen), waits for the map to draw and saves the window as a PNG at twice the screen
    /// resolution. Used for the manuscript's figure of the application.
    /// </summary>
    public async Task SnapshotAsync(string geoJsonFile, string pngFile, int? level)
    {
        await Ready;
        Width = 1450; Height = 820;
        await Presenter.Comparison.LoadAsync(geoJsonFile);
        if (level.HasValue)
            Presenter.Comparison.Level = level.Value;
        await Presenter.Comparison.RunAsync();
        Presenter.Comparison.ShowOnly("LRS", "RW");
        drawingsExpander.IsExpanded = true;
        await Task.Delay(8000);
        UpdateLayout();

        var dpi = 192.0;
        var visual = (System.Windows.Media.Visual)Content;
        var element = (FrameworkElement)Content;
        var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap((int)(element.ActualWidth * dpi / 96), (int)(element.ActualHeight * dpi / 96),
            dpi, dpi, System.Windows.Media.PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
        using var stream = System.IO.File.Create(pngFile);
        encoder.Save(stream);
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
                LRSimplificationFeatures.Overhang,
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

        _ready.TrySetResult();
    }
}