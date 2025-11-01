using System.Windows; 

namespace IRI.Maptor.Jab.Common.Views.MapMarkers;
 
public partial class TextboxMarker : MapMarker
{
    public TextboxMarker()
    {
        InitializeComponent();
    }

    public string LabelValue
    {
        get { return (string)GetValue(LabelValueProperty); }
        set { SetValue(LabelValueProperty, value); }
    }
     
    public static readonly DependencyProperty LabelValueProperty =
        DependencyProperty.Register(nameof(LabelValue), typeof(string), typeof(TextboxMarker), new PropertyMetadata(string.Empty));

     
    public string TooltipValue
    {
        get { return (string)GetValue(TooltipValueProperty); }
        set { SetValue(TooltipValueProperty, value); }
    }
     
    public static readonly DependencyProperty TooltipValueProperty =
        DependencyProperty.Register("TooltipValue", typeof(string), typeof(TextboxMarker), new PropertyMetadata(string.Empty));

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
