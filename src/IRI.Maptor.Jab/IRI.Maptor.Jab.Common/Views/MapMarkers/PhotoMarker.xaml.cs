using System.Windows.Media;
using System.Windows.Controls;
using IRI.Maptor.Jab.Common.Abstractions;

namespace IRI.Maptor.Jab.Common.Views.MapMarkers;
 
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
