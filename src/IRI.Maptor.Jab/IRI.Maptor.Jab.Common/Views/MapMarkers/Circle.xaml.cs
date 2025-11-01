using System.Windows.Media;
using System.Windows.Controls;
using IRI.Maptor.Jab.Common.Abstractions;

namespace IRI.Maptor.Jab.Common.Views.MapMarkers;
 
public partial class Circle : MapMarker
{
    public Circle(double opacity = 1, Brush? stroke = null, Brush? fill = null, double strokeThickness = 1, int size = 12)
    {
        InitializeComponent();

        this.Opacity = opacity;

        this.ellipse.Opacity = opacity;

        if (stroke != null)
        {
            this.ellipse.Stroke = stroke;
        }

        if (fill != null)
        {
            this.ellipse.Fill = fill;
        }

        this.ellipse.StrokeThickness = strokeThickness;

        this.Width = size;

        this.Height = size;

        this._unSelectedFill = this.ellipse.Fill;

        this._unSelectedStroke = this.ellipse.Stroke;

        this._unSelectedStrokeThickness = this.ellipse.StrokeThickness;
    }

    Brush _unSelectedFill;

    Brush _unSelectedStroke;

    double _unSelectedStrokeThickness;

    private bool _isSelected;

    public bool IsSelected
    {
        get { return _isSelected; }
        set
        {
            _isSelected = value;

            this.ellipse.Fill = value ? Brushes.Red : _unSelectedFill;

            this.ellipse.Stroke = value ? Brushes.White : _unSelectedStroke;

            this.ellipse.StrokeThickness = value ? _unSelectedStrokeThickness + 1 : _unSelectedStrokeThickness;
        }
    }
}
