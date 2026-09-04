using System;
using System.Windows;
using System.Windows.Input;
using IRI.Maptor.Extensions;
using IRI.Maptor.Core.SpatialReferenceSystem.MapProjections.Mgrs;
using IRI.Maptor.Presentation.Wpf;
using IRI.Maptor.Presentation.Wpf.Controls;
using IRI.Maptor.Presentation.Wpf.ViewModels.Map;

namespace IRI.Maptor.Presentation.Wpf.Controls;

/// <summary>
/// Interaction logic for CoordinatePanelView.xaml
/// </summary>
public partial class CoordinatePanelView : NotifiableUserControl
{
    public CoordinatePanelViewModel? Presenter { get { return this.DataContext as CoordinatePanelViewModel; } }
    
    public CoordinatePanelView()
    {
        InitializeComponent();
    }
     
    private void options_MouseDown(object sender, MouseButtonEventArgs e)
    {
        this.optionsRow.Height = new GridLength(1, GridUnitType.Auto);

        this.optionsRow2.Height = new GridLength(1, GridUnitType.Auto); 
    }

    private void UserControl_MouseLeave(object sender, MouseEventArgs e)
    {
        this.optionsRow.Height = new GridLength(0, GridUnitType.Pixel);

        this.optionsRow2.Height = new GridLength(0, GridUnitType.Pixel); 
    }


    /// <summary>
    /// Sets the x,y coordinates from ellipsoidal mercator. ellipsoid: WGS84
    /// </summary>
    /// <param name="mercatorX"></param>
    /// <param name="mercatorY"></param>
    public void SetCoordinates(Point geodeticPoint)
    {
        if (Presenter is null)
            return;
         
        Presenter.SelectedItem?.Update(geodeticPoint.AsPoint());

        UpdateMgrs(geodeticPoint);
    }

    /// <summary>
    /// Refreshes <see cref="CurrentMgrs"/> from the position the panel was just handed. Skipped
    /// entirely while the option is off: this runs on every mouse move.
    /// </summary>
    private void UpdateMgrs(Point geodeticPoint)
    {
        if (!ShowMgrs)
            return;

        // MGRS covers 80 S to 84 N only; past that there is simply nothing to show, which is not
        // an error worth surfacing on a mouse move.
        CurrentMgrs = MgrsConverter.TryFromGeodetic(geodeticPoint.X, geodeticPoint.Y, MgrsPrecision.M1, out var mgrs)
            ? mgrs
            : string.Empty;
    }


    public Point Position
    {
        get { return (Point)GetValue(PositionProperty); }
        set { SetValue(PositionProperty, value); }
    }

    // Using a DependencyProperty as the backing store for Position.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty PositionProperty =
        DependencyProperty.Register(nameof(Position), typeof(Point), typeof(CoordinatePanelView), new PropertyMetadata(new PropertyChangedCallback((d, dp) =>
        {
            try
            {
                ((CoordinatePanelView)d).SetCoordinates((Point)dp.NewValue);
            }
            catch (Exception ex)
            {
                return;
            }
        })));
     
    public double? CurrentHeight
    {
        get { return (double?)GetValue(CurrentHeightProperty); }
        set { SetValue(CurrentHeightProperty, value); }
    }

    public static readonly DependencyProperty CurrentHeightProperty =
        DependencyProperty.Register(nameof(CurrentHeight), typeof(double?), typeof(CoordinatePanelView), new PropertyMetadata(null));
    
    
    public bool IsHeightAvailable
    {
        get { return (bool)GetValue(IsHeightAvailableProperty); }
        set { SetValue(IsHeightAvailableProperty, value); }
    }

    public static readonly DependencyProperty IsHeightAvailableProperty =
        DependencyProperty.Register(nameof(IsHeightAvailable), typeof(bool), typeof(CoordinatePanelView), new PropertyMetadata(false));


    /// <summary>
    /// Whether the MGRS reference is shown alongside the coordinates. Hosts bind this to
    /// <c>GeneralSettings.CoordinatePanel_ShowMgrs</c>; it defaults to false, so a host that does
    /// not bind it keeps the panel exactly as it was.
    /// </summary>
    public bool ShowMgrs
    {
        get { return (bool)GetValue(ShowMgrsProperty); }
        set { SetValue(ShowMgrsProperty, value); }
    }

    public static readonly DependencyProperty ShowMgrsProperty =
        DependencyProperty.Register(nameof(ShowMgrs), typeof(bool), typeof(CoordinatePanelView), new PropertyMetadata(false));


    /// <summary>
    /// The MGRS reference for the current position. Unlike <see cref="CurrentHeight"/>, which only
    /// the host can supply, this is derived from the position the panel already receives, so it is
    /// filled in by <see cref="SetCoordinates"/> rather than bound in from outside.
    /// </summary>
    public string CurrentMgrs
    {
        get { return (string)GetValue(CurrentMgrsProperty); }
        set { SetValue(CurrentMgrsProperty, value); }
    }

    public static readonly DependencyProperty CurrentMgrsProperty =
        DependencyProperty.Register(nameof(CurrentMgrs), typeof(string), typeof(CoordinatePanelView), new PropertyMetadata(string.Empty));



}
