using System.Linq;
using System.Windows;
using System.Collections.Generic;

using IRI.Maptor.Jab.Common;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Jab.Common.Helpers;
using System;

namespace IRI.Maptor.Jab.Controls.Views;

public partial class Scalebar : NotifiableUserControl
{

    public Scalebar()
    {
        InitializeComponent();
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

        //var minScalebarWidth = 100;
        //var maxScalebarWidth = 250;

        //var minScreenLengthInMeter = minScalebarWidth * unitDistance;
        //var maxScreenLengthInMeter = maxScalebarWidth * unitDistance;

        //var minGroundLengthInMeter = minScreenLengthInMeter * mapScale;
        //var maxGroundLengthInMeter = maxScreenLengthInMeter * mapScale;

        //var selectedLength = _roundLengths.FirstOrDefault(l => l >= minGroundLengthInMeter && l <= maxGroundLengthInMeter);

        var selectedLength = ScalebarHelper.ChooseRoundScale(mapScale, unitDistance);

        if (selectedLength == 0)
            return;

        //this.ScaleBarLength = (selectedLength / mapScale) / unitDistance;
        this.ScaleBarLength = ScalebarHelper.GetScalebarLength(selectedLength, mapScale, unitDistance);

        //RaisePropertyChanged(nameof(ScaleBarLength));


        //double groundLengthInMeter = /*screenLengthInMeter*/minScreenLengthInMeter * mapScale;
        var groundLengthInMeter = selectedLength;

        this.GroundLength = (groundLengthInMeter / 1000.0 >= 1) ?
            string.Format("{0:f0} km", groundLengthInMeter / 1000) :
            string.Format("{0} m", groundLengthInMeter);

        //this.Min = (groundLengthInMeter / 1000.0 > 1) ? "0 km" : "0 m";

        //RaisePropertyChanged(nameof(GroundLength)); 
        this.CurrentScaleText = IRI.Maptor.Jab.Common.Localization.LocalizationManager.GetLocalizedNumberString($"1:{(1 / mapScale):N0}");
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
