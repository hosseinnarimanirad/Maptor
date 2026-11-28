using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace IRI.Maptor.Presentation.Wpf.Controls.MapMarkers;

/// <summary>
/// The handle of a vertex of a geometry being drawn or edited (decision D1 of the drawing and editing plan, look A):
/// an 8 px white square with a thin dark line; under the pointer the line turns the accent colour; selected, the
/// square is filled with the accent colour. The first vertex of a part is a larger square, so the direction of a
/// line can be seen. The control is larger than the square and transparent around it, so the square is easy to hit.
/// No animation, no hand cursor (the map sets the move cursor).
/// </summary>
public sealed class VertexHandle : MapMarker
{
    private const double HitSize = 14;

    private const double Size = 8;

    private const double FirstSize = 10;

    private const string AccentBrush = "MahApps.Brushes.Highlight";
    private const string LineBrush = "IRI.Maptor.Brushes.OnMap.Text";
    private const string FillBrush = "IRI.Maptor.Brushes.OnMap.HandleFill";

    private readonly Rectangle _square = new()
    {
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        SnapsToDevicePixels = true,
    };

    private bool _isFirst;

    private bool _isSelected;

    private bool _isUnderPointer;

    public VertexHandle(bool isFirst = false)
    {
        Width = HitSize;
        Height = HitSize;

        // the whole control takes the mouse, not only the square
        Background = Brushes.Transparent;

        UseLayoutRounding = true;

        // the map puts its scale first in this group and adds its own transforms after it
        RenderTransform = new TransformGroup { Children = { new ScaleTransform() } };

        Content = _square;

        MouseEnter += (_, _) => { _isUnderPointer = true; UpdateLook(); };
        MouseLeave += (_, _) => { _isUnderPointer = false; UpdateLook(); };

        _isFirst = isFirst;

        UpdateLook();
    }

    /// <summary>True for the first vertex of a part: drawn as a larger square.</summary>
    public bool IsFirst
    {
        get => _isFirst;
        set
        {
            _isFirst = value;
            UpdateLook();
        }
    }

    public override bool IsSelected
    {
        get => _isSelected;
        set
        {
            _isSelected = value;
            UpdateLook();
        }
    }

    private void UpdateLook()
    {
        var size = _isFirst ? FirstSize : Size;

        _square.Width = size;
        _square.Height = size;

        if (_isSelected)
        {
            _square.SetResourceReference(Shape.FillProperty, AccentBrush);
            _square.SetResourceReference(Shape.StrokeProperty, FillBrush);
            _square.StrokeThickness = 1;
        }
        else
        {
            _square.SetResourceReference(Shape.FillProperty, FillBrush);
            _square.SetResourceReference(Shape.StrokeProperty, _isUnderPointer ? AccentBrush : LineBrush);
            _square.StrokeThickness = _isUnderPointer ? 2 : (_isFirst ? 1.5 : 1);
        }
    }
}
