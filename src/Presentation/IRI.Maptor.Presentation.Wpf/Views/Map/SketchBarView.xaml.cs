using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace IRI.Maptor.Presentation.Wpf.Controls;

/// <summary>
/// The sketch bar over the map while drawing, measuring or editing. Its place is the top centre of the map; its header is
/// a handle to drag it anywhere on the map (kept inside the map's edges). A moved bar stays where it was left for as long
/// as this view lives, and goes back with the header's button or a double click on the header. Moving is a matter of
/// the view only, so it is kept here, not in a view model.
/// </summary>
public partial class SketchBarView : UserControl
{
    private Point? _dragStart;

    private Vector _offsetAtDragStart;

    public SketchBarView()
    {
        InitializeComponent();

        // the map or the bar changed size (a window resized, the details opened): keep the moved bar on the map
        SizeChanged += (_, _) => MoveBarTo(CurrentOffset);
        bar.SizeChanged += (_, _) => MoveBarTo(CurrentOffset);
    }

    private Vector CurrentOffset => new Vector(barOffset.X, barOffset.Y);

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // a button in the header does its own job
        if (IsInButton(e.OriginalSource as DependencyObject))
            return;

        if (e.ClickCount == 2)
        {
            MoveBarTo(new Vector());
            e.Handled = true;
            return;
        }

        _dragStart = e.GetPosition(this);
        _offsetAtDragStart = CurrentOffset;
        header.CaptureMouse();
        e.Handled = true;
    }

    private void Header_MouseMove(object sender, MouseEventArgs e)
    {
        if (_dragStart is null || !header.IsMouseCaptured)
            return;

        MoveBarTo(_offsetAtDragStart + (e.GetPosition(this) - _dragStart.Value));
        e.Handled = true;
    }

    private void Header_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_dragStart is null)
            return;

        _dragStart = null;
        header.ReleaseMouseCapture();
        e.Handled = true;
    }

    // Alt+Tab, a dialog: the drag ends where it is
    private void Header_LostMouseCapture(object sender, MouseEventArgs e) => _dragStart = null;

    private void ResetPosition_Click(object sender, RoutedEventArgs e) => MoveBarTo(new Vector());

    /// <summary>Moves the bar by <paramref name="offset"/> from its place, as far as the map's edges allow.</summary>
    private void MoveBarTo(Vector offset)
    {
        if (bar.ActualWidth > 0 && ActualWidth > 0)
        {
            // where the bar sits with no offset, in this view's coordinates
            var place = bar.TranslatePoint(new Point(), this) - CurrentOffset;

            offset.X = Clamp(offset.X, -place.X, ActualWidth - bar.ActualWidth - place.X);
            offset.Y = Clamp(offset.Y, -place.Y, ActualHeight - bar.ActualHeight - place.Y);
        }

        barOffset.X = offset.X;
        barOffset.Y = offset.Y;

        var isMoved = offset.Length > 0.5;
        resetPositionButton.Visibility = isMoved ? Visibility.Visible : Visibility.Collapsed;
    }

    // the low end wins when the bar is bigger than the map, so its header stays reachable
    private static double Clamp(double value, double min, double max) => value > max ? (max < min ? min : max) : value < min ? min : value;

    private bool IsInButton(DependencyObject? element)
    {
        for (var e = element; e is not null && e != header; e = e is Visual or System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(e) : LogicalTreeHelper.GetParent(e))
        {
            if (e is ButtonBase)
                return true;
        }

        return false;
    }
}
