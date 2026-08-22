
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace IRI.Maptor.Presentation.Wpf.Controls.MapMarkers;
 
public partial class ShapeWithLabelMarker : MapMarker
{
    //private bool _isExpanded;

    //public bool IsExpanded
    //{
    //    get { return _isExpanded; }
    //    set
    //    {
    //        this._isExpanded = value;
    //        RaisePropertyChanged();
    //    }
    //}

    public ShapeWithLabelMarker(Geometry shape, string count)
    {
        InitializeComponent();

        this.shape.Data = shape;

        this.labelBox.Text = count;
    }

    //public event PropertyChangedEventHandler PropertyChanged;
    //protected void RaisePropertyChanged([CallerMemberName] string propertyName = "")
    //{
    //    if (PropertyChanged != null)
    //    {
    //        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
    //    }
    //}

    private void Border_MouseDown(object sender, MouseButtonEventArgs e)
    {
        //this.IsExpanded = !this.IsExpanded;
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
