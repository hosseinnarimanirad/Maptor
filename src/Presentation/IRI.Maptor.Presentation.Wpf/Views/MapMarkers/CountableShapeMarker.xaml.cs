using System.Windows.Media;
using System.Windows.Controls;


namespace IRI.Maptor.Presentation.Wpf.Controls.MapMarkers;

/// <summary>
/// Interaction logic for CountableShapeMarker.xaml
/// </summary>
public partial class CountableShapeMarker : MapMarker
{
    public CountableShapeMarker(Geometry shape, string count)
    {
        InitializeComponent();

        //this.image.Data = Geometry.Parse(shape);
        this.image.Data = shape;

        this.labelBox.Text = count;
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

