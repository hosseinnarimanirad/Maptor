using System;
using System.Linq;
using System.Collections.Generic;

using IRI.Maptor.Extensions;
using IRI.Maptor.Jab.Common;
using IRI.Maptor.Jab.Controls.Views;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Jab.Common.Assets.Commands;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;
using IRI.Maptor.Jab.Common.ViewModels;
using IRI.Maptor.Jab.Common.Models.GoTo;

namespace IRI.Maptor.Jab.Common.ViewModels;

public class GoToViewModel : Notifier
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


    private readonly Models.DegreeMinuteSecondModel _longitudeDms;

    public Models.DegreeMinuteSecondModel LongitudeDms
    {
        get { return _longitudeDms; }
    }

    private readonly Models.DegreeMinuteSecondModel _latitudeDms;

    public Models.DegreeMinuteSecondModel LatitudeDms
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


    public GoToViewModel(Action<Point> requestPanTo, Action<Point> requestZoomTo, List<HamburgerGoToMenuItem> items = null)
    {
        RequestZoomTo = requestZoomTo;

        RequestPanTo = requestPanTo;

        if (items.IsNullOrEmpty())
        {
            MenuItems = GetDefaultItems();
        }
        else
        {
            MenuItems = items;
        }

        _longitudeDms = new Models.DegreeMinuteSecondModel();

        _longitudeDms.OnValueChanged -= OnValueChangedHandler;
        _longitudeDms.OnValueChanged += OnValueChangedHandler;

        _latitudeDms = new Models.DegreeMinuteSecondModel();

        //this._latitudeDms.OnValueChanged += (sender, e) => { UpdateXY(); };

        _latitudeDms.OnValueChanged -= OnValueChangedHandler;
        _latitudeDms.OnValueChanged += OnValueChangedHandler;

        IsPaneOpen = false;
    }

    private void OnValueChangedHandler(object? sender, EventArgs e)
    {
        UpdateXY();
    }

    public void ZoomTo()
    {
        RequestZoomTo?.Invoke(GetWgs84Point());
    }

    public void PanTo()
    {
        RequestPanTo?.Invoke(GetWgs84Point());
    }

    public Point GetWgs84Point()
    {
        var point = new Point(X, Y);

        switch (SelectedItem?.MenuType)
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
        var globeMarkup = new MahApps.Metro.IconPacks.PackIconMaterial() { Kind = MahApps.Metro.IconPacks.PackIconMaterialKind.Earth }.Data;

        var mapTreasureMarkup = new MahApps.Metro.IconPacks.PackIconMaterial() { Kind = MahApps.Metro.IconPacks.PackIconMaterialKind.EarthBox }.Data;

        return new List<HamburgerGoToMenuItem>()
        {
            new HamburgerGoToMenuItem(new GoToGeodeticView(), SpatialReferenceType.Geodetic){
                Title = "Geodetic",
                SubTitle ="WGS84",
                Tooltip ="Geodetic",
                Icon = globeMarkup,
            },
            new HamburgerGoToMenuItem(new GoToMapProjectView(), SpatialReferenceType.UTM){
                Title = "Uiversal Transverse Mercator",
                SubTitle ="UTM",
                Tooltip ="UTM",
                Icon = mapTreasureMarkup,
            }
        };
    }

    private void UpdateXY()
    {
        var geodeticPoint = GetWgs84Point();

        if (SelectedItem?.MenuType != SpatialReferenceType.Geodetic)
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
        else if (SelectedItem.MenuType != SpatialReferenceType.UTM)
        {
            var zone = MapProjects.FindUtmZone(geodeticPoint.X);

            var utmPoint = geodeticPoint.Project(new NoProjection(), UTM.CreateForZone(UtmZone));

            _x = utmPoint.X;

            _y = utmPoint.Y;

            _utmZone = zone;

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
        if (!MenuItems.IsNullOrEmpty())
        {
            SelectedItem = MenuItems.First();
            IsPaneOpen = false;
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
                _zoomToCommand = new RelayCommand(param => { ZoomTo(); });
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
                _panToCommand = new RelayCommand(param => { PanTo(); });
            }

            return _panToCommand;
        }
    }


    #endregion

    public static GoToViewModel Create(MapViewModelBase mapPresenter)
    {
        var gotoPresenter = new GoToViewModel(
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

        LongitudeDms.Value = geodeticPoint.X;

        LatitudeDms.Value = geodeticPoint.Y;
    }
}
