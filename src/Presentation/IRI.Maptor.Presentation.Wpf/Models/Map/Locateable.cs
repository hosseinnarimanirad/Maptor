using System;
using System.Windows.Media.Animation;

using IRI.Maptor.Extensions;
using IRI.Maptor.Presentation.Wpf.Events;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Presentation.Wpf.Controls.MapMarkers;
using IRI.Maptor.Core.SpatialReferenceSystem;

using WpfPoint = System.Windows.Point;
using IRI.Maptor.Presentation.Core;

namespace IRI.Maptor.Presentation.Wpf.Models;

public class Locateable : Notifier
{
    public event EventHandler OnRequestHandleMouseDown;

    public event EventHandler OnRequestHandleMouseUp;

    public event EventHandler<ChangeEventArgs<WpfPoint>> OnPositionChanged;

    public Action<bool> RequestChangeIsSelected;

    /// <summary>
    /// Cleanup callback. External code that subscribes handlers directly to <see cref="Element"/>
    /// should append the matching unsubscribe logic here so <see cref="Detach"/> can remove them.
    /// </summary>
    internal Action? OnDetach;

    public AnchorFunctionHandler AnchorFunction;


    public Guid Id { get; set; }

    public bool CanBeUsedAsEditingPoint { get; set; } = false;

    public bool CanTriggerPositionChange { get; set; } = true;

    private double _x;

    /// <summary>
    /// Web Mercator X coordinate
    /// </summary>
    public double X
    {
        get { return _x; }
        set
        {
            if (_x == value)
                return;

            var oldValue = new WpfPoint(_x, _y);

            _x = value;
            RaisePropertyChanged();

            _location.X = value;

            if (CanTriggerPositionChange)
                OnPositionChanged?.Invoke(this, new ChangeEventArgs<WpfPoint>(oldValue, new WpfPoint(_x, _y)));
        }
    }


    private double _y;

    /// <summary>
    /// Web Mercator Y coordinate
    /// </summary>
    public double Y
    {
        get { return _y; }
        set
        {
            if (_y == value)
                return;

            var oldValue = new WpfPoint(_x, _y);

            _y = value;
            RaisePropertyChanged();

            _location.Y = value;

            if (CanTriggerPositionChange)
                OnPositionChanged?.Invoke(this, new ChangeEventArgs<WpfPoint>(oldValue, new WpfPoint(_x, _y)));
        }
    }

    private WpfPoint _location;
    /// <summary>
    /// Web Mercator System.Windows.Point
    /// </summary>
    public WpfPoint Location => _location;

    private bool _isSelected;
    public bool IsSelected
    {
        get { return _isSelected; }
        set
        {
            _isSelected = value;
            RaisePropertyChanged();
            RequestChangeIsSelected?.Invoke(value);
        }
    }

    protected System.Windows.FrameworkElement _element;
    public virtual System.Windows.FrameworkElement Element
    {
        get { return _element; }
        set
        {
            if (_element != null)
            {
                _element.MouseDown -= Element_MouseDown;
                _element.MouseUp -= Element_MouseUp;
            }

            _element = value;
            _element.MouseDown -= Element_MouseDown;
            _element.MouseDown += Element_MouseDown;

            _element.MouseUp -= Element_MouseUp;
            _element.MouseUp += Element_MouseUp;
        }
    }

    private Locateable()
    {

    }

    public Locateable(AnchorFunctionHandler? anchorFunction = null)
    {
        if (anchorFunction == null)
        {
            AnchorFunction = AnchorFunctionHandlers.CenterCenter;
        }
        else
        {
            AnchorFunction = anchorFunction;
        }

        _location = new WpfPoint(0, 0);
    }

    public Locateable(Point wgs84GeodeticPosition, AnchorFunctionHandler? anchorFunction = null) : this(anchorFunction)
    {
        var webMercator = MapProjects.GeodeticWgs84ToWebMercator(wgs84GeodeticPosition);

        X = webMercator.X;

        Y = webMercator.Y;

        _location = webMercator.AsWpfPoint();
    }


    private void Element_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) => OnRequestHandleMouseDown?.Invoke(null, EventArgs.Empty);

    private void Element_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e) => RaiseMouseUpEvent();

    public void RaiseMouseUpEvent() => OnRequestHandleMouseUp?.Invoke(null, EventArgs.Empty);


    public void Select()
    {
        if (Element is null)
            return;

        var element = (LocationMarker)Element;

        element.BeginAnimation(System.Windows.FrameworkElement.HeightProperty, new DoubleAnimation(250, new System.Windows.Duration(new TimeSpan(0, 0, 1))) { FillBehavior = FillBehavior.HoldEnd });
    }

    public void Unselect()
    {
        if (Element == null)
            return;

        var storyBoard = Element.FindResource("mapMarkerResetOnMouseLeave") as Storyboard;

        if (storyBoard == null)
            return;

        storyBoard.Begin(Element);
    }

    /// <summary>
    /// Unsubscribes all handlers wired to this locateable and its <see cref="Element"/> so the
    /// instance (and its WPF element) can be garbage collected. Safe to call multiple times.
    /// </summary>
    public void Detach()
    {
        if (_element != null)
        {
            _element.MouseDown -= Element_MouseDown;
            _element.MouseUp -= Element_MouseUp;
        }

        OnPositionChanged = null;
        OnRequestHandleMouseDown = null;
        OnRequestHandleMouseUp = null;

        RequestChangeIsSelected = null;

        OnDetach?.Invoke();
        OnDetach = null;
    }

    public static Locateable CreateFromWebMercatorPoint(Point webMercatorPoint, AnchorFunctionHandler? anchorFunctionHandler = null)
    {
        return new Locateable()
        {
            X = webMercatorPoint.X,
            Y = webMercatorPoint.Y,

            _location = webMercatorPoint.AsWpfPoint()
        };
    }
}