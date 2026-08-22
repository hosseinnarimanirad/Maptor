using System.Windows.Media;
using System.Windows.Controls;


namespace IRI.Maptor.Presentation.Wpf.Controls.MapMarkers;

public partial class ImageMarker : MapMarker
{
    public ImageMarker(ImageSource symbol, double width = 16, double height = 16)
    {
        InitializeComponent();

        this.image.Source = symbol;

        this.root.Width = width;

        this.root.Height = height;

        //this.viewbox.Width = width;

        //this.viewbox.Height = height;
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
