using System.Windows;

using IRI.Maptor.Jab.Controls;
using IRI.Maptor.Jab.Wpf.Helpers;

namespace IRI.Maptor.Jab.Controls;

public partial class Scalebar : NotifiableUserControl
{

    public Scalebar()
    {
        InitializeComponent();

        // the value may have been set before the control had a PresentationSource
        this.Loaded += (sender, e) =>
        {
            if (CurrentScale > 0)
                SetScale(CurrentScale);
        };
    }

    public void SetScale(double mapScale)
    {
        PresentationSource source = PresentationSource.FromVisual(this);

        if (source == null)
            return;

        if (mapScale <= 0 || double.IsInfinity(mapScale) || double.IsNaN(mapScale))
            return;

        double dpiX = 96.0 * source.CompositionTarget.TransformToDevice.M11;
        double unitDistance = ScalebarHelper.GetUnitDistance(dpiX);

        this.CurrentScaleText = IRI.Maptor.Jab.Core.Localization.LocalizationManager.GetLocalizedNumberString($"1:{(1 / mapScale):N0}");

        var selectedLength = ScalebarHelper.ChooseRoundScale(mapScale, unitDistance);

        if (selectedLength == 0)
            return;

        this.ScaleBarLength = ScalebarHelper.GetScalebarLength(selectedLength, mapScale, unitDistance);

        this.GroundLength = ScalebarHelper.GetGroundLengthLabel(selectedLength);
    }


    public double CurrentScale
    {
        get { return (double)GetValue(CurrentScaleProperty); }
        set { SetValue(CurrentScaleProperty, value); }
    }

    public static readonly DependencyProperty CurrentScaleProperty =
        DependencyProperty.Register(nameof(CurrentScale), typeof(double), typeof(Scalebar), new PropertyMetadata(
            new PropertyChangedCallback((d, dp) => { ((Scalebar)d).SetScale((double)dp.NewValue); })));


    public bool ShowScaleValue
    {
        get { return (bool)GetValue(ShowScaleValueProperty); }
        set { SetValue(ShowScaleValueProperty, value); }
    }

    public static readonly DependencyProperty ShowScaleValueProperty =
        DependencyProperty.Register(nameof(ShowScaleValue), typeof(bool), typeof(Scalebar), new PropertyMetadata(false));


    public bool ShowZoomLevel
    {
        get { return (bool)GetValue(ShowZoomLevelProperty); }
        set { SetValue(ShowZoomLevelProperty, value); }
    }

    public static readonly DependencyProperty ShowZoomLevelProperty =
        DependencyProperty.Register(nameof(ShowZoomLevel), typeof(bool), typeof(Scalebar), new PropertyMetadata(false));


    public bool ShowOptions
    {
        get { return (bool)GetValue(ShowOptionsProperty); }
        set { SetValue(ShowOptionsProperty, value); }
    }

    public static readonly DependencyProperty ShowOptionsProperty =
        DependencyProperty.Register(nameof(ShowOptions), typeof(bool), typeof(Scalebar), new PropertyMetadata(false));


    public int ZoomLevel
    {
        get { return (int)GetValue(ZoomLevelProperty); }
        set { SetValue(ZoomLevelProperty, value); }
    }

    public static readonly DependencyProperty ZoomLevelProperty =
        DependencyProperty.Register(nameof(ZoomLevel), typeof(int), typeof(Scalebar), new PropertyMetadata(-1));


    public bool IsGoogleStyle
    {
        get { return (bool)GetValue(IsGoogleStyleProperty); }
        set { SetValue(IsGoogleStyleProperty, value); }
    }

    public static readonly DependencyProperty IsGoogleStyleProperty =
        DependencyProperty.Register(nameof(IsGoogleStyle), typeof(bool), typeof(Scalebar), new PropertyMetadata(false));


    private string _currentScaleText = string.Empty;
    public string CurrentScaleText
    {
        get { return _currentScaleText; }
        set
        {
            _currentScaleText = value;
            RaisePropertyChanged();
        }
    }

    //public string GroundLength { get; set; }
    private string _groundLength = string.Empty;
    public string GroundLength
    {
        get { return _groundLength; }
        set
        {
            _groundLength = value;
            RaisePropertyChanged();
        }
    }

    //public double ScaleBarLength { get; set; } = 150;
    private double _scaleBarLength = 150;
    public double ScaleBarLength
    {
        get { return _scaleBarLength; }
        set
        {
            _scaleBarLength = value;
            RaisePropertyChanged();
        }
    }


}
