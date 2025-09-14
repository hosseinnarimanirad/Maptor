using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace IRI.Maptor.Jab.Controls.View;

/// <summary>
/// Interaction logic for UserControl1.xaml
/// </summary>
public partial class Scalebar : UserControl
{

    public Scalebar()
    {
        InitializeComponent();
    }

    public void SetScale(double mapScale)
    {
        PresentationSource source = PresentationSource.FromVisual(this);

        if (source == null)
        {
            return ;
        }

        double dpiX = 96.0 * source.CompositionTarget.TransformToDevice.M11;

        double unitDistance = (1.0 / dpiX) * 1200.0 / (3937.0 * 12.0);

        this.scale.Content = "مقیاس: " + "1/" + string.Format("{0:0,0}", 1.0 / mapScale);

        double screenLength = this.scalebarLine.ActualWidth;

        double screenLengthInMeter = screenLength * unitDistance;

        double groundLengthInMeter = screenLengthInMeter / mapScale;

        this.one.Content = (groundLengthInMeter / 1000.0 > 1) ?
            string.Format("{0:f0} km", groundLengthInMeter / 1000) :
            string.Format("{0} m", groundLengthInMeter);

        this.zero.Content = (groundLengthInMeter / 1000.0 > 1) ? "0 km" : "0 m";
    }



    public double CurrentScale
    {
        get { return (double)GetValue(CurrentScaleProperty); }
        set { SetValue(CurrentScaleProperty, value); }
    }

    // Using a DependencyProperty as the backing store for CurrentScale.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty CurrentScaleProperty =
        DependencyProperty.Register("CurrentScale", typeof(double), typeof(Scalebar), new PropertyMetadata(
            new PropertyChangedCallback((d, dp) => { ((Scalebar)d).SetScale((double)dp.NewValue); })));


}
