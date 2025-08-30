using System;
using System.Linq;
using System.Collections.Generic;

using IRI.Maptor.Extensions;
using IRI.Maptor.Jab.Common;
using IRI.Maptor.Jab.Controls.View;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Jab.Controls.Model.GoTo;
using IRI.Maptor.Jab.Common.Assets.Commands;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;
using IRI.Maptor.Jab.Common.Presenters;

namespace IRI.Maptor.Jab.Controls.Presenter;

public class GoToPresenter : Notifier
{
    private Action<Point> RequestZoomTo;

    private Action<Point> RequestPanTo;

    private delegate void updateDelegate(object sender, EventArgs e);

    private EventHandler<updateDelegate> OnUpdateRequired;

    private double _x;

    public double X
    {
        get { return _x; }
        set
        {
            if (_x == value)
                return;

            _x = value;
            RaisePropertyChanged();

            UpdateXY();
        }
    }

    private double _y;

    public double Y
    {
        get { return _y; }
        set
        {
            if (_y == value)
                return;

            _y = value;
            RaisePropertyChanged();

            UpdateXY();
        }
    }

    private int _utmZone = 39;

    public int UtmZone
    {
        get { return _utmZone; }
        set
        {
            _utmZone = value;
            RaisePropertyChanged();

            UpdateXY();
        }
    }

    private bool _isPaneOpen;

    public bool IsPaneOpen
    {
        get { return _isPaneOpen; }
        set
        {
            _isPaneOpen = value;
            RaisePropertyChanged();
        }
    }


    private readonly Model.DegreeMinuteSecondModel _longitudeDms;

    public Model.DegreeMinuteSecondModel LongitudeDms
    {
        get { return _longitudeDms; } 
    }

    private readonly Model.DegreeMinuteSecondModel _latitudeDms;

    public Model.DegreeMinuteSecondModel LatitudeDms
    {
        get { return _latitudeDms; } 
    }


    private List<HamburgerGoToMenuItem> _menuItems;

    public List<HamburgerGoToMenuItem> MenuItems
    {
        get { return _menuItems; }
        set
        {
            _menuItems = value;
            RaisePropertyChanged();
        }
    }

    private HamburgerGoToMenuItem _selectedItem;

    public HamburgerGoToMenuItem SelectedItem
    {
        get { return _selectedItem; }
        set
        {
            if (_selectedItem == value)
            {
                return;
            }

            _selectedItem = value;
            RaisePropertyChanged(); 
        }
    }


    public GoToPresenter(Action<Point> requestPanTo, Action<Point> requestZoomTo, List<HamburgerGoToMenuItem> items = null)
    {        
        this.RequestZoomTo = requestZoomTo;

        this.RequestPanTo = requestPanTo;

        if (items.IsNullOrEmpty())
        {
            this.MenuItems = GetDefaultItems();
        }
        else
        {
            this.MenuItems = items;
        }
         
        this._longitudeDms = new Model.DegreeMinuteSecondModel();

        this._longitudeDms.OnValueChanged -= OnValueChangedHandler;
        this._longitudeDms.OnValueChanged += OnValueChangedHandler;

        this._latitudeDms = new Model.DegreeMinuteSecondModel();

        //this._latitudeDms.OnValueChanged += (sender, e) => { UpdateXY(); };

        this._latitudeDms.OnValueChanged -= OnValueChangedHandler;
        this._latitudeDms.OnValueChanged += OnValueChangedHandler;

        this.IsPaneOpen = false;
    }

    private void OnValueChangedHandler(object? sender, EventArgs e)
    {
        UpdateXY();
    }

    public void ZoomTo()
    {
        this.RequestZoomTo?.Invoke(GetWgs84Point());
    }

    public void PanTo()
    {
        this.RequestPanTo?.Invoke(GetWgs84Point());
    }

    public Point GetWgs84Point()
    {
        var point = new Point(X, Y);

        switch (this.SelectedItem?.MenuType)
        {
            case SpatialReferenceType.Geodetic:
                return new Point(LongitudeDms.GetDegreeValue(), LatitudeDms.GetDegreeValue());

            case SpatialReferenceType.UTM:
                return point.Project(UTM.CreateForZone(UtmZone), new NoProjection());

            case SpatialReferenceType.Mercator:
            case SpatialReferenceType.TransverseMercator:
            case SpatialReferenceType.CylindricalEqualArea:
            case SpatialReferenceType.LambertConformalConic:
            case SpatialReferenceType.WebMercator:
            case SpatialReferenceType.AlbersEqualAreaConic:
            default:
                throw new NotImplementedException();
        }
    }

    private List<HamburgerGoToMenuItem> GetDefaultItems()
    {
        return new List<HamburgerGoToMenuItem>()
        {
            new HamburgerGoToMenuItem(new GoToGeodeticView(), SpatialReferenceType.Geodetic){
                Title = "Geodetic",
                SubTitle ="WGS84",
                Tooltip ="Geodetic",
                Icon = IRI.Maptor.Jab.Common.Assets.ShapeStrings.Appbar.appbarGlobe,
            },
            new HamburgerGoToMenuItem(new GoToMapProjectView(), SpatialReferenceType.UTM){
                Title = "Uiversal Transverse Mercator",
                SubTitle ="UTM",
                Tooltip ="UTM",
                Icon = IRI.Maptor.Jab.Common.Assets.ShapeStrings.Appbar.appbarMapTreasure,
            }
        };
    }

    private void UpdateXY()
    {
        var geodeticPoint = GetWgs84Point();

        if (this.SelectedItem?.MenuType != SpatialReferenceType.Geodetic)
        {
            LongitudeDms.OnValueChanged -= OnValueChangedHandler;
            LatitudeDms.OnValueChanged -= OnValueChangedHandler;

            LongitudeDms.Value = geodeticPoint.X;
            LatitudeDms.Value = geodeticPoint.Y;

            //RaisePropertyChanged(nameof(LongitudeDms));
            //RaisePropertyChanged(nameof(LatitudeDms));
            
            LongitudeDms.OnValueChanged += OnValueChangedHandler;
            LatitudeDms.OnValueChanged += OnValueChangedHandler;

        }
        else if (this.SelectedItem.MenuType != SpatialReferenceType.UTM)
        {
            var zone = MapProjects.FindUtmZone(geodeticPoint.X);

            var utmPoint = geodeticPoint.Project(new NoProjection(), UTM.CreateForZone(UtmZone));

            this._x = utmPoint.X;

            this._y = utmPoint.Y;

            this._utmZone = zone;

            RaisePropertyChanged(nameof(X));
            RaisePropertyChanged(nameof(Y));
            RaisePropertyChanged(nameof(UtmZone));
        }
    }

    //private void UpdateLatLong()
    //{
    //    //if (this.SelectedItem?.MenuType == SpatialReferenceType.Geodetic)
    //    //{

    //    //}
    //}

    public void SelectDefaultMenu()
    {
        if (!this.MenuItems.IsNullOrEmpty())
        {
            this.SelectedItem = this.MenuItems.First();
            this.IsPaneOpen = false;
        }

    }

    #region Commands


    private RelayCommand _zoomToCommand;

    public RelayCommand ZoomToCommand
    {
        get
        {
            if (_zoomToCommand == null)
            {
                _zoomToCommand = new RelayCommand(param => { this.ZoomTo(); });
            }

            return _zoomToCommand;
        }
    }


    private RelayCommand _panToCommand;

    public RelayCommand PanToCommand
    {
        get
        {
            if (_panToCommand == null)
            {
                _panToCommand = new RelayCommand(param => { this.PanTo(); });
            }

            return _panToCommand;
        }
    }


    #endregion

    public static GoToPresenter Create(MapPresenter mapPresenter)
    {
        var gotoPresenter = new GoToPresenter(
           p =>
           {
               var webMercatorPoint = MapProjects.GeodeticWgs84ToWebMercator(p);

               mapPresenter.PanTo(webMercatorPoint, () =>
               {
                   mapPresenter.FlashPoint(webMercatorPoint);
               });

           },
           p =>
           {
               var webMercatorPoint = MapProjects.GeodeticWgs84ToWebMercator(p);

               mapPresenter.ZoomAndCenterToGoogleZoomLevel(13, webMercatorPoint, () =>
               {
                   mapPresenter.FlashPoint(webMercatorPoint);
               });
           });

        return gotoPresenter;
    }

    internal void SetWebMercatorPoint(Point point)
    {
        var geodeticPoint = MapProjects.WebMercatorToGeodeticWgs84(point);

        this.LongitudeDms.Value = geodeticPoint.X;

        this.LatitudeDms.Value = geodeticPoint.Y;
    }
}
