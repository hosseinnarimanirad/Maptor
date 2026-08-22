using System.Windows.Media;
using System.Windows.Controls;


namespace IRI.Maptor.Presentation.Wpf.Controls.MapMarkers;
 
public partial class PhotoMarker : MapMarker
{
    public PhotoMarker()
    {
        InitializeComponent();
    }

    public PhotoMarker(ImageSource imageSource)
    {
        InitializeComponent();

        this.image.Source = imageSource;
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
