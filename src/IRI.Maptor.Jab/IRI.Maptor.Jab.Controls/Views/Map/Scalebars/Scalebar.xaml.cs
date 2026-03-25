using System.Linq;
using System.Windows;
using System.Collections.Generic;

using IRI.Maptor.Jab.Common;
using IRI.Maptor.Sta.Common.Helpers;

namespace IRI.Maptor.Jab.Controls.Views;

/// <summary>
/// Interaction logic for UserControl1.xaml
/// </summary>
public partial class Scalebar : NotifiableUserControl
{

    private static readonly List<double> _roundLengths =
        new List<double>()
        {
            5,          10,         20,         // meters
            50,         100,        200,        // meters
            500,        1_000,      2_000,      // meters (2 km)
            5_000,      10_000,     20_000,     // 5k, 10k, 20k
            50_000,     100_000,    200_000,    // 50k, 100k, 200k
            500_000,    1_000_000,  2_000_000   // 500k, 1000k, 2000k
        };

    public Scalebar()
    {
        InitializeComponent();

        GroundLength = string.Empty;
    }

    public void SetScale(double mapScale)
    {
        PresentationSource source = PresentationSource.FromVisual(this);

        if (source == null)
            return;

        if (double.IsInfinity(mapScale) || double.IsNaN(mapScale))
            return;

        double dpiX = 96.0 * source.CompositionTarget.TransformToDevice.M11;
        double unitDistance = ConversionHelper.InchToMeterFactor / dpiX; //(1.0 / dpiX) * 1200.0 / (3937.0 * 12.0);

        var minScalebarWidth = 100;
        var maxScalebarWidth = 250;

        var minScreenLengthInMeter = minScalebarWidth * unitDistance;
        var maxScreenLengthInMeter = maxScalebarWidth * unitDistance;

        var minGroundLengthInMeter = minScreenLengthInMeter * mapScale;
        var maxGroundLengthInMeter = maxScreenLengthInMeter * mapScale;

        //this.scale.Content = "1/" + string.Format("{0:0,0}", 1.0 / mapScale);

        //double screenLength = this.scalebarLine.ActualWidth;

        //double screenLengthInMeter = screenLength * unitDistance;

        var selectedLength = _roundLengths.FirstOrDefault(l => l >= minGroundLengthInMeter && l <= maxGroundLengthInMeter);

        if (selectedLength == 0)
            return;

        this.ScaleBarLength = (selectedLength / mapScale) / unitDistance;
        RaisePropertyChanged(nameof(ScaleBarLength));


        //double groundLengthInMeter = /*screenLengthInMeter*/minScreenLengthInMeter * mapScale;
        var groundLengthInMeter = selectedLength;

        this.GroundLength = (groundLengthInMeter / 1000.0 >= 1) ?
            string.Format("{0:f0} km", groundLengthInMeter / 1000) :
            string.Format("{0} m", groundLengthInMeter);

        //this.Min = (groundLengthInMeter / 1000.0 > 1) ? "0 km" : "0 m";

        RaisePropertyChanged(nameof(GroundLength));

        this.CurrentScaleText = IRI.Maptor.Jab.Common.Localization.LocalizationManager.GetLocalizedNumberString($"1:{mapScale:N0}");
    }

    private string _currentScaleText;
    public string CurrentScaleText
    {
        get { return _currentScaleText; }
        set
        {
            _currentScaleText = value;
            RaisePropertyChanged();
        }
    }


    public double CurrentScale
    {
        get { return (double)GetValue(CurrentScaleProperty); }
        set { SetValue(CurrentScaleProperty, value); }
    }

    public static readonly DependencyProperty CurrentScaleProperty =
        DependencyProperty.Register("CurrentScale", typeof(double), typeof(Scalebar), new PropertyMetadata(
            new PropertyChangedCallback((d, dp) => { ((Scalebar)d).SetScale((double)dp.NewValue); })));


    public bool ShowScaleValue
    {
        get { return (bool)GetValue(ShowScaleValueProperty); }
        set { SetValue(ShowScaleValueProperty, value); }
    }

    public static readonly DependencyProperty ShowScaleValueProperty =
        DependencyProperty.Register("ShowScaleValue", typeof(bool), typeof(Scalebar), new PropertyMetadata(false));



    public bool ShowZoomLevel
    {
        get { return (bool)GetValue(ShowZoomLevelProperty); }
        set { SetValue(ShowZoomLevelProperty, value); }
    }

    // Using a DependencyProperty as the backing store for ShowZoomLevel.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ShowZoomLevelProperty =
        DependencyProperty.Register("ShowZoomLevel", typeof(bool), typeof(Scalebar), new PropertyMetadata(false));




    public bool ShowOptions
    {
        get { return (bool)GetValue(ShowOptionsProperty); }
        set { SetValue(ShowOptionsProperty, value); }
    }

    // Using a DependencyProperty as the backing store for ShowOptions.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ShowOptionsProperty =
        DependencyProperty.Register("ShowOptions", typeof(bool), typeof(Scalebar), new PropertyMetadata(false));




    public int ZoomLevel
    {
        get { return (int)GetValue(ZoomLevelProperty); }
        set { SetValue(ZoomLevelProperty, value); }
    }

    // Using a DependencyProperty as the backing store for ZoomLevel.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ZoomLevelProperty =
        DependencyProperty.Register("ZoomLevel", typeof(int), typeof(Scalebar), new PropertyMetadata(-1));



    public bool IsGoogleStyle
    {
        get { return (bool)GetValue(IsGoogleStyleProperty); }
        set { SetValue(IsGoogleStyleProperty, value); }
    }

    public static readonly DependencyProperty IsGoogleStyleProperty =
        DependencyProperty.Register("IsGoogleStyle", typeof(bool), typeof(Scalebar), new PropertyMetadata(false));


    public string GroundLength { get; set; }

    public double ScaleBarLength { get; set; } = 150;



}
