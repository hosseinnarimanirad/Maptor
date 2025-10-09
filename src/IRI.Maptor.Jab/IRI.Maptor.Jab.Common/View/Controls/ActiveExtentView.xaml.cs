using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using IRI.Maptor.Jab.Common.Events;
using IRI.Maptor.Jab.Common.Presenters;

namespace IRI.Maptor.Jab.Common.View.Controls;

public partial class ActiveExtentView : MapMarker
{
    public event EventHandler/*<ScreenExtentChangedEventArgs>*/ OnExtentChanged;

    public ActiveExtentViewModel? Presenter => this.DataContext as ActiveExtentViewModel;
     
    public ActiveExtentView()
    {
        InitializeComponent();
    }


    private Ellipse? _activeEllipse;

    private Point _previousMousePosition;
     
    private const double MinSize = 20;
     
    public double GetWidth() => double.IsNaN(Width) ? ActualWidth : Width;

    public double GetHeight() => double.IsNaN(Height) ? ActualHeight : Height;

    private double GetLeft()
    {
        double left = Canvas.GetLeft(this);

        if (double.IsNaN(left)) { left = 0; Canvas.SetLeft(this, left); }

        return left;
    }

    private double GetTop()
    {
        double top = Canvas.GetTop(this);

        if (double.IsNaN(top)) { top = 0; Canvas.SetTop(this, top); }

        return top;
    }

    private void Ellipse_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;

        _activeEllipse = sender as Ellipse;

        if (_activeEllipse is null)
            return;

        if (!(Parent is Canvas canvas)) return;

        _activeEllipse.CaptureMouse();

        _previousMousePosition = e.GetPosition(canvas);

        _activeEllipse.MouseMove -= Ellipse_MouseMove;
        _activeEllipse.MouseMove += Ellipse_MouseMove;

        _activeEllipse.MouseLeftButtonUp -= Ellipse_MouseLeftButtonUp;
        _activeEllipse.MouseLeftButtonUp += Ellipse_MouseLeftButtonUp;
    }

    private void Ellipse_MouseMove(object sender, MouseEventArgs e)
    {
        if (_activeEllipse == null || !(Parent is Canvas canvas)) return;

        Point pos = e.GetPosition(canvas);
        double dx = pos.X - _previousMousePosition.X;
        double dy = pos.Y - _previousMousePosition.Y;

        if (Math.Abs(dx) + Math.Abs(dy) < 1) return;

        double newLeft = GetLeft();
        double newTop = GetTop();
        double newWidth = GetWidth();
        double newHeight = GetHeight();

        //double leftChange = 0, topChange = 0, rightChange = 0, bottomChange = 0;

        if (_activeEllipse == rightEllipse)
        {
            newWidth = Math.Max(MinSize, newWidth + dx);

            //rightChange = dx;
        }
        else if (_activeEllipse == leftEllipse)
        {
            newWidth = Math.Max(MinSize, newWidth - dx);

            //leftChange = dx;

            newLeft += dx;

            //// if we hit min-size, clamp left so we don't "flip"
            //if (Width == MinSize)
            //    newLeft = _startLeft + _startWidth - newWidth;
        }
        else if (_activeEllipse == bottomEllipse)
        {
            newHeight = Math.Max(MinSize, newHeight + dy);

            //bottomChange = -dy;
        }
        else if (_activeEllipse == topEllipse)
        {
            newHeight = Math.Max(MinSize, newHeight - dy);

            //topChange = -dy;

            newTop += dy;

            //newTop = _startTop + dy;
            //if (newHeight == MinSize)
            //    newTop = _startTop + _startHeight - newHeight;
        }


        // apply changes
        Canvas.SetLeft(this, newLeft);
        Canvas.SetTop(this, newTop);
        Width = newWidth;
        Height = newHeight;

        _previousMousePosition = pos;

        //this.OnExtentChanged?.UpdateExtent(leftChange: leftChange, rightChange: rightChange, topChange: topChange, bottomChange: bottomChange);

        this.OnExtentChanged?.Invoke(this, EventArgs.Empty);

        //this.OnExtentChanged?.Invoke(
        //    this, 
        //    new ScreenExtentChangedEventArgs(
        //        leftChange: leftChange, 
        //        rightChange: rightChange, 
        //        topChange: topChange, 
        //        bottomChange: bottomChange));
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
    }
}
