using System;
using System.Windows;
using System.Windows.Input;
using IRI.Maptor.Extensions;
using IRI.Maptor.Jab.Common;
using IRI.Maptor.Jab.Common.ViewModels.Map;

namespace IRI.Maptor.Jab.Controls.Views;

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
    /// Sets the x,y coordinates from elipsoidal mercatar. elipsoid: WGS84
    /// </summary>
    /// <param name="mecatorX"></param>
    /// <param name="mercatorY"></param>
    public void SetCoordinates(Point geodeticPoint)
    {
        if (Presenter is null)
            return;
         
        Presenter.SelectedItem?.Update(geodeticPoint.AsPoint());
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
        DependencyProperty.Register("CurrentHeight", typeof(double?), typeof(CoordinatePanelView), new PropertyMetadata(null));
    
    
    public bool IsHeightAvailable
    {
        get { return (bool)GetValue(IsHeightAvailableProperty); }
        set { SetValue(IsHeightAvailableProperty, value); }
    }

    public static readonly DependencyProperty IsHeightAvailableProperty =
        DependencyProperty.Register("IsHeightAvailable", typeof(bool), typeof(CoordinatePanelView), new PropertyMetadata(false));



}
