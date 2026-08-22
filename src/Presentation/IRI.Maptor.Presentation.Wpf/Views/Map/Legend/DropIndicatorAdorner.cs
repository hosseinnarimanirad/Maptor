using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace IRI.Maptor.Presentation.Wpf.Controls;

/// <summary>
/// Thin insertion line drawn at the top or bottom edge of a legend row while a layer
/// drag hovers over it.
/// </summary>
internal sealed class DropIndicatorAdorner : Adorner
{
    private readonly Brush _brush;

    public bool IsTop { get; }

    public DropIndicatorAdorner(FrameworkElement adornedElement, bool top) : base(adornedElement)
    {
        IsTop = top;

        _brush = adornedElement.TryFindResource("MahApps.Brushes.Highlight") as Brush ?? SystemColors.HighlightBrush;

        IsHitTestVisible = false;
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        if (AdornedElement is not FrameworkElement element)
            return;

        var y = IsTop ? 0.0 : element.ActualHeight;

        drawingContext.DrawLine(new Pen(_brush, 2.0), new Point(0, y), new Point(element.ActualWidth, y));
    }
}
