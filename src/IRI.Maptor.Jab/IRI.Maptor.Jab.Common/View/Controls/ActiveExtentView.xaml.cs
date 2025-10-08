using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using IRI.Maptor.Jab.Common.Abstractions;
using IRI.Maptor.Jab.Common.Events;

namespace IRI.Maptor.Jab.Common.View.Controls;
/// <summary>
/// Interaction logic for ActiveExtentView.xaml
/// </summary>
public partial class ActiveExtentView : UserControl, IMapMarker
{
    public event EventHandler<CustomEventArgs<IRI.Maptor.Sta.Common.Primitives.BoundingBox>> ActiveExtentChanged;

    public ActiveExtentView()
    {
        InitializeComponent();
    }

    //private bool _isDragging = false;
    //private bool _isResizing = false;
    private Point _clickPosition;
    private Ellipse? _activeHandle;

    private IRI.Maptor.Sta.Common.Primitives.BoundingBox _currentExtent;

    private bool _isSelected;

    public bool IsSelected
    {
        get { return _isSelected; }
        set
        {
            _isSelected = value;
        }
    }

    //#region Move

    //private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    //{
    //    _isDragging = true;

    //    _clickPosition = e.GetPosition(this);

    //    CaptureMouse();
    //}

    //private void Border_MouseMove(object sender, MouseEventArgs e)
    //{
    //    if (_isDragging && Parent is Canvas canvas)
    //    {
    //        var position = e.GetPosition(canvas);

    //        var dx = position.X - _clickPosition.X;

    //        var dy = position.Y - _clickPosition.Y;

    //        var newExtent = this._currentExtent.Transform(p => new Sta.Common.Primitives.Point(p.X + dy, p.Y + dy));

    //        _currentExtent = newExtent;

    //        this.ActiveExtentChanged?.Invoke(this, new CustomEventArgs<Sta.Common.Primitives.BoundingBox>(newExtent));
    //    }
    //}

    //private void Border_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    //{
    //    _isDragging = false;

    //    ReleaseMouseCapture();
    //}

    //#endregion


    private void Ellipse_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
         
        //_isResizing = true;

        _activeHandle = sender as Ellipse;

        if (_activeHandle is null)
            return;

        _activeHandle.CaptureMouse();

        _clickPosition = e.GetPosition(Parent as Canvas);

        _activeHandle.MouseMove -= Ellipse_MouseMove;
        _activeHandle.MouseMove += Ellipse_MouseMove;

        _activeHandle.MouseLeftButtonUp -= Ellipse_MouseLeftButtonUp;
        _activeHandle.MouseLeftButtonUp += Ellipse_MouseLeftButtonUp;
    }

    private void Ellipse_MouseMove(object sender, MouseEventArgs e)
    {
        if (/*!_isResizing || */_activeHandle == null || !(Parent is Canvas canvas)) return;

        Point pos = e.GetPosition(canvas);
        double deltaX = pos.X - _clickPosition.X;
        double deltaY = pos.Y - _clickPosition.Y;

        if (Math.Abs(deltaX) + Math.Abs(deltaY) < 0.5)
            return;

        double newWidth = Width;
        double newHeight = Height;
        double newLeft = Canvas.GetLeft(this);
        double newTop = Canvas.GetTop(this);

        if (_activeHandle == rightEllipse)
        {
            newWidth = Math.Max(20, Width + deltaX);
        }
        else if (_activeHandle == leftEllipse)
        {
            newWidth = Math.Max(20, Width - deltaX);
            newLeft += deltaX; // shift left boundary
        }
        else if (_activeHandle == bottomEllipse)
        {
            newHeight = Math.Max(20, Height + deltaY);
        }
        else if (_activeHandle == topEllipse)
        {
            newHeight = Math.Max(20, Height - deltaY);
            newTop += deltaY; // shift top boundary
        }

        // Apply new values
        Width = newWidth;
        Height = newHeight;
        Canvas.SetLeft(this, newLeft);
        Canvas.SetTop(this, newTop);

        _clickPosition = pos; 
    }

    private void Ellipse_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        //_isResizing = false;

        _activeHandle = sender as Ellipse;

        if (_activeHandle is null)
            return;

        _activeHandle.MouseMove -= Ellipse_MouseMove;
        _activeHandle.MouseLeftButtonUp -= Ellipse_MouseLeftButtonUp;

        _activeHandle.ReleaseMouseCapture();

        _activeHandle = null;

         
    }

}
