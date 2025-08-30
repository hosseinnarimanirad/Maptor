using System;
using System.Windows.Media.Animation;

using IRI.Maptor.Extensions;
using IRI.Maptor.Extensions;
using IRI.Maptor.Jab.Common.Events;
using IRI.Maptor.Jab.Common.Models;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;

using WpfPoint = System.Windows.Point;

namespace IRI.Maptor.Jab.Common;

public class Locateable : Notifier
{
    public event EventHandler OnRequestHandleMouseDown;

    public event EventHandler<ChangeEventArgs<WpfPoint>> OnPositionChanged;

    public Action<bool> RequestChangeIsSelected;

    public AncherFunctionHandler AncherFunction;

    public Guid Id { get; set; }

    public bool CanBeUsedAsEditingPoint { get; set; } = false;

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

            this._location.X = value;

            this.OnPositionChanged?.Invoke(this, new ChangeEventArgs<WpfPoint>(oldValue, new WpfPoint(_x, _y)));
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

            this._location.Y = value;

            this.OnPositionChanged?.Invoke(this, new ChangeEventArgs<WpfPoint>(oldValue, new WpfPoint(_x, _y)));
        }
    }

    private System.Windows.Point _location;
    /// <summary>
    /// Web Mercator System.Windows.Point
    /// </summary>
    public System.Windows.Point Location
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
            this.RequestChangeIsSelected?.Invoke(value);
        }
    }

    protected System.Windows.FrameworkElement _element;
    public virtual System.Windows.FrameworkElement Element
    {
        get { return _element; }
        set
        {
            this._element = value;
            this._element.MouseDown -= Element_MouseDown;
            this._element.MouseDown += Element_MouseDown;
        }
    }


    public Locateable(AncherFunctionHandler? ancherFunction = null)
    {
        if (ancherFunction == null)
        {
            this.AncherFunction = AncherFunctionHandlers.CenterCenter;
        }
        else
        {
            this.AncherFunction = ancherFunction;
        }

        this._location = new System.Windows.Point(0, 0);
    }

    public Locateable(Point wgs84GeodeticPosition, AncherFunctionHandler ancherFunction = null) : this(ancherFunction)
    {
        var webMercator = MapProjects.GeodeticWgs84ToWebMercator(wgs84GeodeticPosition);

        this.X = webMercator.X;

        this.Y = webMercator.Y;

        this._location = webMercator.AsWpfPoint();
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
        //if (Popup != null)
        //{
        //    this.Popup.Focus();

        //    this.Popup.IsOpen = !this.Popup.IsOpen;
        //}

        this.OnRequestHandleMouseDown?.Invoke(null, EventArgs.Empty);
    }

    public void Select()
    {
        if (this.Element == null)
            return;

        //var storyBoard = this.Element.FindResource("mapMarkerExpandOnMouseEnter") as Storyboard;

        //if (storyBoard == null)
        //    return;

        //storyBoard.Begin(this.Element);
        var element = ((View.MapMarkers.MapMarker)(this.Element));

        element.BeginAnimation(System.Windows.FrameworkElement.HeightProperty, new DoubleAnimation(250, new System.Windows.Duration(new TimeSpan(0, 0, 1))) { FillBehavior = FillBehavior.HoldEnd });
    }

    public void Unselect()
    {
        if (this.Element == null)
            return;

        var storyBoard = this.Element.FindResource("mapMarkerResetOnMouseLeave") as Storyboard;

        if (storyBoard == null)
            return;

        storyBoard.Begin(this.Element);
    }
}