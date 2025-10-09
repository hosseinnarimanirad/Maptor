using System.Windows.Controls;
using IRI.Maptor.Jab.Common.Abstractions;

namespace IRI.Maptor.Jab.Common.View.MapMarkers;
 
public partial class PointMarker : MapMarker
{
    public PointMarker(string label)
    {
        InitializeComponent();

        this.labelBox.Text = label;

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
