using System;

using IRI.Maptor.Jab.Common.Models;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;

namespace IRI.Maptor.Jab.Common.ViewModels.Map;

public class MapPanelViewModel : Notifier
{
    private bool _isDetailsVisible;
    public bool IsDetailsVisible
    {
        get { return _isDetailsVisible; }
        set
        {
            _isDetailsVisible = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsDetailsNotVisible));
        }
    }

    public bool IsDetailsNotVisible
    {
        get { return !IsDetailsVisible; }
        set
        {
            _isDetailsVisible = !value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsDetailsVisible));
        }
    }

    private EditableFeatureLayerOptions _options;
    public EditableFeatureLayerOptions Options
    {
        get { return _options; }
        set
        {
            _options = value;
            RaisePropertyChanged();
        }
    }


    private CoordinateDisplayMode _spatialReference = CoordinateDisplayMode.GeodeticDecimal;
    public CoordinateDisplayMode SpatialReference
    {
        get { return _spatialReference; }
        set
        {
            var currentPoint = CurrentWebMercatorEditingPoint;

            _spatialReference = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsUTMEditingMode));
            RaisePropertyChanged(nameof(IsGeodeticWgs84EditingMode));

            UpdateCurrentEditingPoint(currentPoint);
        }
    }


    private bool _isUTMEditingMode;
    public bool IsUTMEditingMode
    {
        get { return _isUTMEditingMode; }
        set
        {
            if (_isUTMEditingMode == value)
            {
                return;
            }

            if (CurrentEditingPoint != null && value)
            {
                SpatialReference =  CoordinateDisplayMode.UTM;
            }

            _isUTMEditingMode = value;
            RaisePropertyChanged();

        }
    }


    private bool _isGeodeticWgs84EditingMode = true;
    public bool IsGeodeticWgs84EditingMode
    {
        get { return _isGeodeticWgs84EditingMode; }
        set
        {
            if (_isGeodeticWgs84EditingMode == value)
            {
                return;
            }

            if (CurrentEditingPoint != null && value)
            {
                SpatialReference = CoordinateDisplayMode.GeodeticDecimal;
            }

            _isGeodeticWgs84EditingMode = value;
            RaisePropertyChanged();

        }
    }

    private bool _isWebMercatorEditingMode;
    public bool IsWebMercatorEditingMode
    {
        get { return _isWebMercatorEditingMode; }
        set
        {
            if (_isWebMercatorEditingMode == value)
            {
                return;
            }

            if (CurrentEditingPoint != null && value)
            {
                SpatialReference = CoordinateDisplayMode.WebMercator;
            }

            _isWebMercatorEditingMode = value;
            RaisePropertyChanged();

        }
    }


    private NotifiablePoint _currentEditingPoint;

    public NotifiablePoint CurrentEditingPoint
    {
        get { return _currentEditingPoint; }
        set
        {
            _currentEditingPoint = value;
            RaisePropertyChanged();
        }
    }

    private int _currentEditingZone;

    public int CurrentEditingZone
    {
        get { return _currentEditingZone; }
        set
        {
            _currentEditingZone = value;
            RaisePropertyChanged();

            if (CurrentEditingPoint?.IsRaiseCoordinateChangeEnabled == true)
            {
                CurrentEditingPoint?.RaiseCoordinateChangedAction?.Invoke(CurrentEditingPoint);
            }

        }
    }


    public Point CurrentWebMercatorEditingPoint
    {
        get
        {
            if (SpatialReference == CoordinateDisplayMode.UTM)
            {
                var geodetic = MapProjects.UTMToGeodetic(CurrentEditingPoint, CurrentEditingZone);

                var webMercator = MapProjects.GeodeticWgs84ToWebMercator(geodetic);

                return new Point(webMercator.X, webMercator.Y);
            }
            else if (SpatialReference == CoordinateDisplayMode.GeodeticDecimal)
            {
                var webMercator = MapProjects.GeodeticWgs84ToWebMercator(CurrentEditingPoint);

                return new Point(webMercator.X, webMercator.Y);
            }
            else if (SpatialReference == CoordinateDisplayMode.WebMercator)
            {
                return new Point(CurrentEditingPoint.X, CurrentEditingPoint.Y);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }

    public MapPanelViewModel()
    {
        //RaisePropertyChanged(nameof(IsGeodeticWgs84EditingMode));
    }

    private Point FromWebMercator(Point webMercatorPoint)
    {
        if (SpatialReference == CoordinateDisplayMode.WebMercator)
            return webMercatorPoint;

        var geodetic = MapProjects.WebMercatorToGeodeticWgs84(webMercatorPoint);

        if (SpatialReference == CoordinateDisplayMode.UTM)
        {
            CurrentEditingZone = MapProjects.FindUtmZone(geodetic.X);

            return MapProjects.GeodeticToUTM(geodetic);
        }
        else if (SpatialReference == CoordinateDisplayMode.GeodeticDecimal)
        {
            return geodetic;
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    private RelayCommand _toggleAdvancedVisibilityCommand;

    public RelayCommand ToggleAdvancedVisibilityCommand
    {
        get
        {
            if (_toggleAdvancedVisibilityCommand == null)
            {
                _toggleAdvancedVisibilityCommand = new RelayCommand(param => { IsDetailsVisible = !IsDetailsVisible; });
            }

            return _toggleAdvancedVisibilityCommand;
        }
    }

    public void UpdateCurrentEditingPoint(Point webMercatorPoint)
    {
        lock (CurrentEditingPoint)
        {
            CurrentEditingPoint.IsRaiseCoordinateChangeEnabled = false;

            var point = FromWebMercator(webMercatorPoint);

            CurrentEditingPoint.X = point.X;

            CurrentEditingPoint.Y = point.Y;

            CurrentEditingPoint.IsRaiseCoordinateChangeEnabled = true;
        }

    }
}
