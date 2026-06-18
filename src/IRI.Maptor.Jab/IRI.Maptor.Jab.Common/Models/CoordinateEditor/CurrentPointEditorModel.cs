using System;

using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Jab.Common.ViewModels.CoordinateEditor;
using IRI.Maptor.Jab.Core;
using IRI.Maptor.Jab.Core.Models;

namespace IRI.Maptor.Jab.Common.Models.CoordinateEditor;

public class CurrentPointEditorModel : Notifier
{
    private Locateable? _currentPoint;
    public Locateable? CurrentPoint
    {
        get => _currentPoint;
        set
        {
            if (_currentPoint == value)
                return;

            _currentPoint = value;
            RaisePropertyChanged();
            UpdateFromSelectedPoint();
        }
    }

    private string _coordinateX = string.Empty;
    public string CoordinateX
    {
        get => _coordinateX;
        set
        {
            if (_coordinateX == value)
                return;

            _coordinateX = value;
            RaisePropertyChanged();
        }
    }

    private string _coordinateY = string.Empty;
    public string CoordinateY
    {
        get => _coordinateY;
        set
        {
            if (_coordinateY == value)
                return;

            _coordinateY = value;
            RaisePropertyChanged();
        }
    }

    private DegreeMinuteSecondModel? _dmsX;
    public DegreeMinuteSecondModel? DmsX
    {
        get => _dmsX;
        set
        {
            if (_dmsX == value)
                return;

            // Unsubscribe from old model
            if (_dmsX != null)
                _dmsX.OnValueChanged -= DmsX_OnValueChanged;

            _dmsX = value;
            RaisePropertyChanged();

            // Subscribe to new model
            if (_dmsX != null)
                _dmsX.OnValueChanged += DmsX_OnValueChanged;
        }
    }

    private DegreeMinuteSecondModel? _dmsY;
    public DegreeMinuteSecondModel? DmsY
    {
        get => _dmsY;
        set
        {
            if (_dmsY == value)
                return;

            // Unsubscribe from old model
            if (_dmsY != null)
                _dmsY.OnValueChanged -= DmsY_OnValueChanged;

            _dmsY = value;
            RaisePropertyChanged();

            // Subscribe to new model
            if (_dmsY != null)
                _dmsY.OnValueChanged += DmsY_OnValueChanged;
        }
    }

    private void DmsX_OnValueChanged(object? sender, EventArgs e)
    {
        // User is editing DMS X - mark as user input
        RaisePropertyChanged(nameof(DmsX));
    }

    private void DmsY_OnValueChanged(object? sender, EventArgs e)
    {
        // User is editing DMS Y - mark as user input
        RaisePropertyChanged(nameof(DmsY));
    }

    public bool IsDmsMode => SrsViewModel?.SelectedSrsType == CoordinateDisplayMode.GeodeticDms;

    private int? _utmZone;
    public int? UtmZone
    {
        get => _utmZone;
        set
        {
            if (_utmZone == value)
                return;

            _utmZone = value;
            RaisePropertyChanged();
        }
    }


    private double? _z;
    public double? Z
    {
        get => _z;
        set
        {
            if (_z == value)
                return;

            _z = value;
            RaisePropertyChanged();
        }
    }

    private double? _m;
    public double? M
    {
        get => _m;
        set
        {
            if (_m == value)
                return;

            _m = value;
            RaisePropertyChanged();
        }
    }

    private bool _hasZ;
    public bool HasZ
    {
        get => _hasZ;
        set
        {
            if (_hasZ == value)
                return;

            _hasZ = value;
            RaisePropertyChanged();
        }
    }

    private bool _hasM;
    public bool HasM
    {
        get => _hasM;
        set
        {
            if (_hasM == value)
                return;

            _hasM = value;
            RaisePropertyChanged();
        }
    }


    private CoordinateEditorSrsViewModel? _srsViewModel;
    public CoordinateEditorSrsViewModel? SrsViewModel
    {
        get => _srsViewModel;
        set
        {
            if (_srsViewModel == value)
                return;

            // Unsubscribe from old ViewModel
            if (_srsViewModel != null)
                _srsViewModel.PropertyChanged -= SrsViewModel_PropertyChanged;

            _srsViewModel = value;
            RaisePropertyChanged();

            // Subscribe to new ViewModel
            if (_srsViewModel != null)
            {
                _srsViewModel.PropertyChanged += SrsViewModel_PropertyChanged;
                // Initialize DMS models if already in DMS mode
                if (IsDmsMode)
                {
                    if (DmsX == null)
                        DmsX = new DegreeMinuteSecondModel();

                    if (DmsY == null)
                        DmsY = new DegreeMinuteSecondModel();
                }
            }

            RaisePropertyChanged(nameof(IsDmsMode));
            UpdateFromSelectedPoint();
        }
    }

    private void SrsViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(CoordinateEditorSrsViewModel.SelectedSrsType))
            return;
        //{
        //// Initialize DMS models when switching to DMS mode (must happen before RaisePropertyChanged)
        //if (IsDmsMode)
        //{
        //    if (DmsX == null)
        //    {
        //        if (CurrentPoint != null && SrsViewModel != null)
        //        {
        //            var (x, _) = SrsViewModel.ConvertFromWebMercator(new Point(CurrentPoint.X, CurrentPoint.Y));
        //            DmsX = new DegreeMinuteSecondModel(x);
        //        }
        //        else
        //        {
        //            DmsX = new DegreeMinuteSecondModel();
        //        }
        //    }
        //    if (DmsY == null)
        //    {
        //        if (CurrentPoint != null && SrsViewModel != null)
        //        {
        //            var (_, y) = SrsViewModel.ConvertFromWebMercator(new Point(CurrentPoint.X, CurrentPoint.Y));
        //            DmsY = new DegreeMinuteSecondModel(y);
        //        }
        //        else
        //        {
        //            DmsY = new DegreeMinuteSecondModel();
        //        }
        //    }
        //}
        //RaisePropertyChanged(nameof(IsDmsMode));
        UpdateFromSelectedPoint();
        //}
    }

    /// <summary>
    /// Updates editor fields when SelectedPoint changes
    /// </summary>
    public void UpdateFromSelectedPoint()
    {
        if (CurrentPoint == null || SrsViewModel == null)
        {
            CoordinateX = string.Empty;
            CoordinateY = string.Empty;
            if (DmsX != null)
                DmsX.Value = 0;
            if (DmsY != null)
                DmsY.Value = 0;
            Z = null;
            M = null;
            HasZ = false;
            HasM = false;
            return;
        }

        // Convert Web Mercator to selected SRS
        var webMercatorPoint = new Point(CurrentPoint.X, CurrentPoint.Y);
        var xy = SrsViewModel.ConvertFromWebMercator(webMercatorPoint);

        // Format based on SRS type
        if (SrsViewModel.SelectedSrsType == CoordinateDisplayMode.GeodeticDms)
        {
            // Update DMS models with decimal degrees
            // Always initialize if null
            if (DmsX == null)
                DmsX = new DegreeMinuteSecondModel(xy.X);

            else
                DmsX.Value = xy.X;

            if (DmsY == null)
                DmsY = new DegreeMinuteSecondModel(xy.Y);

            else
                DmsY.Value = xy.Y;

            RaisePropertyChanged(nameof(IsDmsMode));
        }
        else
        {
            //// Only update if fields are empty (user hasn't started editing)
            //// This prevents overwriting user input when point changes on map
            //if (string.IsNullOrWhiteSpace(CoordinateX) && string.IsNullOrWhiteSpace(CoordinateY))
            //{
            CoordinateX = SrsViewModel.GetDisplayString(xy.X);
            CoordinateY = SrsViewModel.GetDisplayString(xy.Y);
            //}
        }

        // Update UTM zone if applicable
        if (SrsViewModel.SelectedSrsType == CoordinateDisplayMode.UTM)
        {
            var geodetic = MapProjects.WebMercatorToGeodeticWgs84(webMercatorPoint);
            UtmZone = MapProjects.FindUtmZone(geodetic.X);
        }
        else
        {
            UtmZone = null;
        }

        // Check for Z and M values
        // Note: Locateable doesn't have Z/M, but we need to check the underlying geometry
        // This will be set by GeometryEditorViewModelBase when it checks the geometry
        // For now, set to false - will be updated by parent ViewModel
    }

    /// <summary>
    /// Applies changes by converting edited coordinates to Web Mercator and updating Locateable
    /// </summary>
    public bool ApplyChanges()
    {
        if (CurrentPoint == null || SrsViewModel == null)
            return false;

        if (!ValidateInput())
            return false;

        try
        {
            double x, y;

            // Parse input based on SRS type
            if (SrsViewModel.SelectedSrsType == CoordinateDisplayMode.GeodeticDms)
            {
                if (DmsX == null || DmsY == null)
                    return false;

                // Get decimal degrees from DMS models
                x = DmsX.Value;
                y = DmsY.Value;
            }
            else
            {
                if (!double.TryParse(CoordinateX, out x) || !double.TryParse(CoordinateY, out y))
                    return false;
            }

            // Convert to Web Mercator
            Point webMercatorPoint = SrsViewModel.ConvertToWebMercator(x, y, UtmZone);

            // Update Locateable
            CurrentPoint.X = webMercatorPoint.X;
            CurrentPoint.Y = webMercatorPoint.Y;

            // Note: Z and M values are stored in the underlying geometry, not in Locateable
            // Updating Z/M would require modifying the geometry directly through EditableFeatureLayer
            // This is a future enhancement - for now, Z/M are read-only in the editor

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates coordinate input based on SRS type
    /// </summary>
    public bool ValidateInput()
    {
        if (SrsViewModel == null)
            return false;

        try
        {
            if (SrsViewModel.SelectedSrsType == CoordinateDisplayMode.GeodeticDms)
            {
                // Validate DMS models
                if (DmsX == null || DmsY == null)
                    return false;

                // DMS models always have valid values (they're NumericUpDown controls)
                return true;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(CoordinateX) || string.IsNullOrWhiteSpace(CoordinateY))
                    return false;

                return double.TryParse(CoordinateX, out _) && double.TryParse(CoordinateY, out _);
            }
        }
        catch
        {
            return false;
        }
    }


    public Point GetNewXY()
    {
        bool hasValidX = double.TryParse(CoordinateX, out double x);
        bool hasValidY = double.TryParse(CoordinateY, out double y);

        return hasValidX && hasValidY ? new Point(x, y) : Point.NaN;
    }

    public Point GetNewLatLong() => DmsX != null && DmsY != null ? new Point(DmsX.Value, DmsY.Value) : Point.NaN;

}

