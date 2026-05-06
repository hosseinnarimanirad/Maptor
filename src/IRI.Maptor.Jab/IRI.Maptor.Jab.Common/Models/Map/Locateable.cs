using System;
using System.Windows.Media.Animation;

using IRI.Maptor.Extensions;
using IRI.Maptor.Jab.Common.Events;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Jab.Controls.MapMarkers;
using IRI.Maptor.Sta.SpatialReferenceSystem;

using WpfPoint = System.Windows.Point;

namespace IRI.Maptor.Jab.Common.Models;

public class Locateable : Notifier
{
    public event EventHandler OnRequestHandleMouseDown;

    public event EventHandler OnRequestHandleMouseUp;

    public event EventHandler<ChangeEventArgs<WpfPoint>> OnPositionChanged;

    public Action<bool> RequestChangeIsSelected;

    public AncherFunctionHandler AncherFunction;

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
    public WpfPoint Location
    {
        get { return _location; }
    }

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
            _element = value;
            _element.MouseDown -= Element_MouseDown;
            _element.MouseDown += Element_MouseDown;

            _element.MouseUp -= _element_MouseUp;
            _element.MouseUp += _element_MouseUp;
        }
    }

    private Locateable()
    {

    }

    public Locateable(AncherFunctionHandler? ancherFunction = null)
    {
        if (ancherFunction == null)
        {
            AncherFunction = AncherFunctionHandlers.CenterCenter;
        }
        else
        {
            AncherFunction = ancherFunction;
        }

        _location = new WpfPoint(0, 0);
    }

    public Locateable(Point wgs84GeodeticPosition, AncherFunctionHandler? ancherFunction = null) : this(ancherFunction)
    {
        var webMercator = MapProjects.GeodeticWgs84ToWebMercator(wgs84GeodeticPosition);

        X = webMercator.X;

        Y = webMercator.Y;

        _location = webMercator.AsWpfPoint();
    }

    //public Locateable(FrameworkElement element, Popup infoWindow, SpecialPointLayer.AncherFunctionHandler ancherFunction = null)
    //{
    //    this.Popup = infoWindow;

    //    if (ancherFunction == null)
    //    {
    //        this.AncherFunction = SpecialPointLayer.CenterCenter;
    //    }
    //    else
    //    {
    //        this.AncherFunction = ancherFunction;
    //    }

    //    this.Element = element;

    //    if (infoWindow != null)
    //    {
    //        infoWindow.AllowsTransparency = true;

    //        infoWindow.Child = new IRI.Maptor.Jab.Common.UserControls.SimpleInfoControl();

    //        infoWindow.PopupAnimation = PopupAnimation.Slide;

    //        infoWindow.PlacementTarget = element;

    //        infoWindow.Placement = PlacementMode.Left;

    //        infoWindow.Focus();

    //        infoWindow.StaysOpen = false;

    //        //this.Element.MouseDown += Element_MouseDown;
    //    }
    //}

    void Element_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        OnRequestHandleMouseDown?.Invoke(null, EventArgs.Empty);
    }

    private void _element_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        RaiseMouseUpEvent();
    }

    public void RaiseMouseUpEvent()
    {
        OnRequestHandleMouseUp?.Invoke(null, EventArgs.Empty);
    }


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

    public static Locateable CreateFromWebMercatorPoint(Point webMercatorPoint, AncherFunctionHandler? ancherFunctionHandler = null)
    {
        return new Locateable()
        {
            X = webMercatorPoint.X,
            Y = webMercatorPoint.Y,

            _location = webMercatorPoint.AsWpfPoint()
        };
    }
}