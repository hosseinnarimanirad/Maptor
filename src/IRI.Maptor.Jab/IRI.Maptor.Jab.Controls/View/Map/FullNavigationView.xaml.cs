using IRI.Maptor.Jab.Common.Events;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace IRI.Maptor.Jab.Controls.View;

/// <summary>
/// Interaction logic for FullNavigationView.xaml
/// </summary>
public partial class FullNavigationView : UserControl, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private static readonly double panStep = 40;

    //Dictionary<double, double> zoomLevels;

    //IRI.MapViewer.MapViewer mapViewer;

    public event MouseButtonEventHandler OnMoveLeft;

    public event MouseButtonEventHandler OnMoveRight;

    public event MouseButtonEventHandler OnMoveUp;

    public event MouseButtonEventHandler OnMoveDown;

    public event MouseButtonEventHandler OnZoomIn;

    public event MouseButtonEventHandler OnZoomOut;


    //public event IRI.Maptor.Jab.Common.ZoomEventHandler OnZoomChanged;

    //public event IRI.Maptor.Jab.Common.PanEventHandler OnPanTriggered;

    public event EventHandler<ZoomEventArgs> OnZoomChanged;

    public event EventHandler<PanEventArgs> OnPanTriggered;



    public FullNavigationView()
    {
        InitializeComponent(); 
    }

    private void moveRight_MouseDown(object sender, MouseButtonEventArgs e)
    {
        PanMap(-panStep, 0);

        this.OnMoveRight?.Invoke(sender, e);
    }

    private void moveDown_MouseDown(object sender, MouseButtonEventArgs e)
    {
        PanMap(0, -panStep);

        this.OnMoveDown?.Invoke(sender, e);
    }

    private void moveLeft_MouseDown(object sender, MouseButtonEventArgs e)
    {
        PanMap(panStep, 0);

        this.OnMoveLeft?.Invoke(sender, e);
    }

    private void moveUp_MouseDown(object sender, MouseButtonEventArgs e)
    {
        PanMap(0, panStep);

        this.OnMoveUp?.Invoke(sender, e);
    }

    private void zoomIn_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (this.zoom.Value + 1 <= this.zoom.Maximum)
        {
            this.ZoomLevel += 1;
        }

        OnZoomIn?.Invoke(sender, e);
    }

    private void zoomOut_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (this.zoom.Value - 1 >= this.zoom.Minimum)
        {
            //this.zoom.Value -= 1;
            this.ZoomLevel -= 1;
        }

        this.OnZoomOut?.Invoke(sender, e);
    }

    private void zoom_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        this.ZoomLevel = this.zoom.Value;

        this.OnZoomChanged?.Invoke(sender, ZoomEventArgs.EmptyArg);
    }

    private void PanMap(double xOffset, double yOffset)
    {
        this.OnPanTriggered?.Invoke(this, new PanEventArgs(new Point(xOffset, yOffset)));
    }

    public void UpdateZoomLevel(double zoomLevel)
    {
        this.zoom.ValueChanged -= zoom_ValueChanged;

        this.zoom.Value = zoomLevel;

        this.zoom.ValueChanged += zoom_ValueChanged;
    }

    public void ZoomIn()
    {
        //this.zoom.Value++;
        this.ZoomLevel = this.ZoomLevel + 1;
    }

    public void ZoomOut()
    {
        //this.zoom.Value--;
        this.ZoomLevel = this.ZoomLevel - 1;
    }

    //public double ZoomLevel
    //{
    //    get { return this.zoom.Value; }
    //}

    public double ZoomLevel
    {
        get { return (double)GetValue(ZoomLevelProperty); }
        set
        {
            if (this.zoom.Maximum < value || this.zoom.Minimum > value)
                return;

            UpdateZoomLevel(value);

            SetValue(ZoomLevelProperty, value);

            OnPropertyChanged(nameof(ZoomLevel));
            //this.zoom.Value = value;
        }
    }

    // Using a DependencyProperty as the backing store for Fill.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ZoomLevelProperty =
        DependencyProperty.Register(nameof(ZoomLevel),
            typeof(double),
            typeof(FullNavigationView),
            new PropertyMetadata(new PropertyChangedCallback((d, dp) =>
            {
                ((FullNavigationView)d).UpdateZoomLevel((double)dp.NewValue);
            })));
}
