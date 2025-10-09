using System.Windows.Media;
using System.Windows.Controls;
using IRI.Maptor.Jab.Common.Abstractions;

namespace IRI.Maptor.Jab.Common.View.MapMarkers;
 
public partial class CircleImageMarker : MapMarker
{
    public CircleImageMarker()
    {
        InitializeComponent();
    }

    public CircleImageMarker(ImageSource image, string? tooltip = null)
    {
        InitializeComponent();

        this.image.Source = image;

        if (tooltip != null)
        {
            this.image.ToolTip = tooltip;
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
