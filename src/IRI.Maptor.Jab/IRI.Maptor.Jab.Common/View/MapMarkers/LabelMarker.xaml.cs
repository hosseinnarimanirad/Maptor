using System.Windows;
using System.Windows.Controls;
using IRI.Maptor.Jab.Common.Abstractions;

namespace IRI.Maptor.Jab.Common.View.MapMarkers;
 
public partial class LabelMarker : MapMarker
{

    public string LabelValue
    {
        get { return (string)GetValue(LabelValueProperty); }
        set { SetValue(LabelValueProperty, value); }
    }

    public static readonly DependencyProperty LabelValueProperty =
        DependencyProperty.Register(nameof(LabelValue), typeof(string), typeof(LabelMarker), new PropertyMetadata(string.Empty));
     

    public string ToolTipValue
    {
        get { return (string)GetValue(ToolTipValueProperty); }
        set { SetValue(ToolTipValueProperty, value); }
    }

    public static readonly DependencyProperty ToolTipValueProperty =
        DependencyProperty.Register("ToolTipValue", typeof(string), typeof(LabelMarker), new PropertyMetadata(string.Empty));

     
    public LabelMarker(string count, bool isExpandBringToFrontEnabled = false)
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
