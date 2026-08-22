using System.Windows.Media;
using System.Windows.Controls;


namespace IRI.Maptor.Presentation.Wpf.Controls.MapMarkers;
 
public partial class CountableImageMarker : MapMarker
{
    public CountableImageMarker(ImageSource imageSource, string count)
    {
        InitializeComponent();

        this.image.Source = imageSource;

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
