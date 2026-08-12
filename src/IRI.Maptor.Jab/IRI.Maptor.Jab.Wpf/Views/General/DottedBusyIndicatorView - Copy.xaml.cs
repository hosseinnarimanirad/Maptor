using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace IRI.Maptor.Jab.Controls;

/// <summary>
/// Interaction logic for DottedBusyIndicator.xaml
/// </summary>
public partial class DottedBusyIndicatorView2 : UserControl
{
    public DottedBusyIndicatorView2()
    {
        InitializeComponent();
    }
     
    public Brush DotBrush
    {
        get { return (Brush)GetValue(DotBrushProperty); }
        set { SetValue(DotBrushProperty, value); }
    }

    // Using a DependencyProperty as the backing store for DotBrush.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty DotBrushProperty =
        DependencyProperty.Register(nameof(DotBrush), typeof(Brush), typeof(DottedBusyIndicatorView), new PropertyMetadata(Brushes.White));




    public int DotSize
    {
        get { return (int)GetValue(DotSizeProperty); }
        set { SetValue(DotSizeProperty, value); }
    }

    // Using a DependencyProperty as the backing store for DotSize.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty DotSizeProperty =
        DependencyProperty.Register(nameof(DotSize), typeof(int), typeof(DottedBusyIndicatorView), new PropertyMetadata(8));




    public bool ShowLoadingText
    {
        get { return (bool)GetValue(ShowLoadingTextProperty); }
        set { SetValue(ShowLoadingTextProperty, value); }
    }

    // Using a DependencyProperty as the backing store for ShowLoadingText.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ShowLoadingTextProperty =
        DependencyProperty.Register("ShowLoadingText", typeof(bool), typeof(DottedBusyIndicatorView), new PropertyMetadata(false));


}
