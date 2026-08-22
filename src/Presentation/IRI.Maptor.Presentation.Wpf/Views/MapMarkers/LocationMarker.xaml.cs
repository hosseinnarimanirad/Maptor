using System.Windows;
using System.Windows.Controls;


namespace IRI.Maptor.Presentation.Wpf.Controls.MapMarkers;
 
public partial class LocationMarker : MapMarker
{
    public LocationMarker(int number)
    {
        InitializeComponent();

        this.Value = number.ToString();

        //this.numberBox.Text = number.ToString();
    }

    public LocationMarker(string content)
    {
        InitializeComponent();

        this.Value = content;

        //this.numberBox.Text = content;
    }



    public string Value
    {
        get { return (string)GetValue(ValueProperty); }
        set { SetValue(ValueProperty, value); }
    }

    // Using a DependencyProperty as the backing store for Value.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(string), typeof(LocationMarker), new PropertyMetadata(string.Empty));


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
