using System.Windows;
using System.Windows.Controls;



namespace IRI.Maptor.Presentation.Wpf.Controls.MapMarkers;
 
public partial class RectangleLabelMarker : MapMarker
{

    public string LabelValue
    {
        get { return (string)GetValue(LabelValueProperty); }
        set { SetValue(LabelValueProperty, value); }
    }

    // Using a DependencyProperty as the backing store for LabelValue.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty LabelValueProperty =
        DependencyProperty.Register(nameof(LabelValue), typeof(string), typeof(RectangleLabelMarker), new PropertyMetadata(string.Empty));


    public string TooltipValue
    {
        get { return (string)GetValue(TooltipValueProperty); }
        set { SetValue(TooltipValueProperty, value); }
    }

    // Using a DependencyProperty as the backing store for TooltipValue.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty TooltipValueProperty =
        DependencyProperty.Register(nameof(TooltipValue), typeof(string), typeof(RectangleLabelMarker), new PropertyMetadata(string.Empty));


    public RectangleLabelMarker(string count, bool isExpandBringToFrontEnabled = false)
    {
        InitializeComponent();

        LabelValue = count;

        if (isExpandBringToFrontEnabled)
        {
            this.Style = (Style)this.FindResource("expandableStyle");
        }
    }
    //private bool _isSelected;

    //public bool IsSelected
    //{
    //    get { return _isSelected; }
    //    set
    //    {
    //        _isSelected = value;
    //    }
    //}
}
