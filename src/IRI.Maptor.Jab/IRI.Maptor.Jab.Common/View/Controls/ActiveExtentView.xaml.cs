using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using IRI.Maptor.Jab.Common.Abstractions;
using IRI.Maptor.Jab.Common.Events;

namespace IRI.Maptor.Jab.Common.View.Controls;
 
public partial class ActiveExtentView : UserControl, IMapMarker
{
    public event EventHandler<ScreenExtentChangedEventArgs> OnActiveExtentChanged;

    public ActiveExtentView()
    {
        InitializeComponent();
    }

    private bool _isSelected;

    public bool IsSelected
    {
        get { return _isSelected; }
        set
        {
            _isSelected = value;
        }
    }


    private Ellipse? _activeEllipse;

    private Point _startMousePos;

    private double _startLeft, _startTop, _startWidth, _startHeight;

    private const double MinSize = 20;

    //public IRI.Maptor.Sta.Common.Primitives.Point WebMercatorCenter { get; set; }


    private void Ellipse_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;

        _activeEllipse = sender as Ellipse;

        if (_activeEllipse is null)
            return;

        if (!(Parent is Canvas canvas)) return;

        _activeEllipse.CaptureMouse();

        //ensure canvas coordinates are defined(fall back to 0 if NaN)
        double left = Canvas.GetLeft(this);
        if (double.IsNaN(left)) { left = 0; Canvas.SetLeft(this, left); }

        double top = Canvas.GetTop(this);
        if (double.IsNaN(top)) { top = 0; Canvas.SetTop(this, top); }

        //store start geometry
        _startLeft = left;
        _startTop = top;
        _startWidth = double.IsNaN(Width) ? ActualWidth : Width;
        _startHeight = double.IsNaN(Height) ? ActualHeight : Height;

        _startMousePos = e.GetPosition(canvas);


        _activeEllipse.MouseMove -= Ellipse_MouseMove;
        _activeEllipse.MouseMove += Ellipse_MouseMove;

        _activeEllipse.MouseLeftButtonUp -= Ellipse_MouseLeftButtonUp;
        _activeEllipse.MouseLeftButtonUp += Ellipse_MouseLeftButtonUp;
    }

    private void Ellipse_MouseMove(object sender, MouseEventArgs e)
    {
        if (/*!_isResizing || */_activeEllipse == null || !(Parent is Canvas canvas)) return;

        Point pos = e.GetPosition(canvas);
        double dx = pos.X - _startMousePos.X;
        double dy = pos.Y - _startMousePos.Y;

        // jitter filter
        if (Math.Abs(dx) + Math.Abs(dy) < 0.5)
            return;

        double newLeft = _startLeft;
        double newTop = _startTop;
        double newWidth = _startWidth;
        double newHeight = _startHeight;

        double leftChange = 0, topChange = 0, rightChange = 0, bottomChange = 0;

        if (_activeEllipse == rightEllipse)
        {
            newWidth = Math.Max(MinSize, _startWidth + dx);

            rightChange = dx;
        }
        else if (_activeEllipse == leftEllipse)
        {
            newWidth = Math.Max(MinSize, _startWidth - dx);
            newLeft = _startLeft + dx;

            leftChange = -dx;

            // if we hit min-size, clamp left so we don't "flip"
            if (newWidth == MinSize)
                newLeft = _startLeft + _startWidth - newWidth;
        }
        else if (_activeEllipse == bottomEllipse)
        {
            newHeight = Math.Max(MinSize, _startHeight + dy);

            bottomChange = dy;
        }
        else if (_activeEllipse == topEllipse)
        {
            newHeight = Math.Max(MinSize, _startHeight - dy);
            newTop = _startTop + dy;

            topChange = -dy;

            if (newHeight == MinSize)
                newTop = _startTop + _startHeight - newHeight;
        }

        //---new corner combos-- -
        else if (_activeEllipse == topLeftEllipse)
        {
            newWidth = Math.Max(MinSize, _startWidth - dx);
            newLeft = _startLeft + dx;
            newHeight = Math.Max(MinSize, _startHeight - dy);
            newTop = _startTop + dy;

            if (newWidth == MinSize)
                newLeft = _startLeft + _startWidth - newWidth;
            if (newHeight == MinSize)
                newTop = _startTop + _startHeight - newHeight;
        }
        else if (_activeEllipse == topRightEllipse)
        {
            newWidth = Math.Max(MinSize, _startWidth + dx);
            newHeight = Math.Max(MinSize, _startHeight - dy);
            newTop = _startTop + dy;

            if (newHeight == MinSize)
                newTop = _startTop + _startHeight - newHeight;
        }
        else if (_activeEllipse == bottomRightEllipse)
        {
            newWidth = Math.Max(MinSize, _startWidth + dx);
            newHeight = Math.Max(MinSize, _startHeight + dy);
        }
        else if (_activeEllipse == bottomLeftEllipse)
        {
            newWidth = Math.Max(MinSize, _startWidth - dx);
            newLeft = _startLeft + dx;
            newHeight = Math.Max(MinSize, _startHeight + dy);

            if (newWidth == MinSize)
                newLeft = _startLeft + _startWidth - newWidth;
        }

        // apply changes
        Canvas.SetLeft(this, newLeft);
        Canvas.SetTop(this, newTop);
        Width = newWidth;
        Height = newHeight;

    }

    private void Ellipse_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _activeEllipse = sender as Ellipse;

        if (_activeEllipse is null)
            return;

        _activeEllipse.MouseMove -= Ellipse_MouseMove;
        _activeEllipse.MouseLeftButtonUp -= Ellipse_MouseLeftButtonUp;

        _activeEllipse.ReleaseMouseCapture();

        _activeEllipse = null;

        //this.OnActiveExtentChanged?.Invoke(this, new ScreenExtentChangedEventArgs(leftChange, rightChange, topChange, bottomChange));
    }

    //private IRI.Maptor.Sta.Common.Primitives.BoundingBox GetCurrentScreenExtent()
    //{
    //    return new Sta.Common.Primitives.BoundingBox(_startLeft, _startTop, _startLeft + Width, _startTop + Height);
    //}
}
