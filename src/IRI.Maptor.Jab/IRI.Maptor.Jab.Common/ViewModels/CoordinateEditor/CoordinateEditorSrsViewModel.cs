using System;
using System.Collections.ObjectModel;

using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.SpatialReferenceSystem;

using Ellipsoid = IRI.Maptor.Sta.SpatialReferenceSystem.Ellipsoid<IRI.Maptor.Sta.Metrics.Meter, IRI.Maptor.Sta.Metrics.Degree>;

namespace IRI.Maptor.Jab.Common.ViewModels.CoordinateEditor;

public class CoordinateEditorSrsViewModel : Notifier
{
    private ObservableCollection<CoordinateDisplayMode> _availableSrsTypes;
    public ObservableCollection<CoordinateDisplayMode> AvailableSrsTypes
    {
        get
        {
            if (_availableSrsTypes == null)
            {
                _availableSrsTypes = new ObservableCollection<CoordinateDisplayMode>
                {
                    CoordinateDisplayMode.UTM,
                    CoordinateDisplayMode.WebMercator,
                    CoordinateDisplayMode.GeodeticDecimal,
                    CoordinateDisplayMode.GeodeticDms
                };
            }
            return _availableSrsTypes;
        }
    }

    private CoordinateDisplayMode _selectedSrsType = CoordinateDisplayMode.GeodeticDecimal;
    public CoordinateDisplayMode SelectedSrsType
    {
        get => _selectedSrsType;
        set
        {
            if (_selectedSrsType == value)
                return;

            _selectedSrsType = value;

            // Auto-set WGS84 for UTM (UTM always uses WGS84)
            if (_selectedSrsType == CoordinateDisplayMode.UTM)
            {
                _selectedEllipsoid = Ellipsoids.WGS84;
                RaisePropertyChanged(nameof(SelectedEllipsoid));
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsUtmZoneVisible));
            RaisePropertyChanged(nameof(IsEllipsoidVisible));
        }
    }

    private ObservableCollection<Ellipsoid> _availableEllipsoids;
    public ObservableCollection<Ellipsoid> AvailableEllipsoids
    {
        get => _availableEllipsoids;
        set
        {
            _availableEllipsoids = value;
            RaisePropertyChanged();
        }
    }

    private Ellipsoid _selectedEllipsoid;
    public Ellipsoid SelectedEllipsoid
    {
        get => _selectedEllipsoid;
        set
        {
            if (_selectedEllipsoid.Equals(value))
                return;

            _selectedEllipsoid = value;
            RaisePropertyChanged();
        }
    }

    private int _utmZone = 39; // Default zone
    public int UtmZone
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

    public bool IsUtmZoneVisible => SelectedSrsType == CoordinateDisplayMode.UTM;

    // Ellipsoid is only visible for Geodetic types
    // UTM always uses WGS84 (fixed)
    // WebMercator is sphere-based (no ellipsoid)
    public bool IsEllipsoidVisible => SelectedSrsType == CoordinateDisplayMode.GeodeticDecimal ||
                                      SelectedSrsType == CoordinateDisplayMode.GeodeticDms;

    private int _latLongPrecision = 5;
    public int LatLongPrecision
    {
        get => _latLongPrecision;
        set
        {
            if (_latLongPrecision == value)
                return;

            _latLongPrecision = Math.Max(0, Math.Min(10, value)); // Clamp between 0 and 10
            RaisePropertyChanged();
        }
    }

    private int _xyPrecision = 2;
    public int XYPrecision
    {
        get => _xyPrecision;
        set
        {
            if (_xyPrecision == value)
                return;

            _xyPrecision = Math.Max(0, Math.Min(10, value)); // Clamp between 0 and 10
            RaisePropertyChanged();
        }
    }

    public CoordinateEditorSrsViewModel()
    {
        // Initialize available ellipsoids
        _availableEllipsoids = new ObservableCollection<Ellipsoid>
        {
            Ellipsoids.WGS84,
            Ellipsoids.GRS80,
            Ellipsoids.Clarke1866,
            Ellipsoids.Clarke1880,
            Ellipsoids.Bessel1841,
            Ellipsoids.Airy1830,
            Ellipsoids.WGS72,
            Ellipsoids.Sphere
        };

        _selectedEllipsoid = _availableEllipsoids[0];
    }

    /// <summary>
    /// Converts coordinates from the selected SRS to Web Mercator
    /// </summary>
    public Point ConvertToWebMercator(double x, double y, int? utmZone = null)
    {
        switch (SelectedSrsType)
        {
            case CoordinateDisplayMode.UTM:
                var zone = utmZone ?? UtmZone;
                // Convert UTM to Geodetic, then to Web Mercator
                // UTM always uses WGS84 ellipsoid
                var utmPoint = new Point(x, y);
                var geodeticFromUtm = MapProjects.UTMToGeodetic(utmPoint, Ellipsoids.WGS84, zone);
                return MapProjects.GeodeticWgs84ToWebMercator(geodeticFromUtm);

            case CoordinateDisplayMode.WebMercator:
                return new Point(x, y);

            case CoordinateDisplayMode.GeodeticDecimal:
                var geodeticPoint = new Point(x, y);
                // If ellipsoid is WGS84, use direct conversion
                if (SelectedEllipsoid.AreTheSame(Ellipsoids.WGS84))
                {
                    return MapProjects.GeodeticWgs84ToWebMercator(geodeticPoint);
                }
                else
                {
                    // Convert from selected ellipsoid to WGS84, then to Web Mercator
                    var wgs84Point = Transformations.ChangeDatum(geodeticPoint, SelectedEllipsoid, Ellipsoids.WGS84);
                    return MapProjects.GeodeticWgs84ToWebMercator(wgs84Point);
                }

            case CoordinateDisplayMode.GeodeticDms:
                // When called from ApplyChanges, x and y are already decimal degrees (from DmsX.Value and DmsY.Value)
                // Treat as GeodeticDecimal for conversion
                var geodeticPointDms = new Point(x, y);
                // If ellipsoid is WGS84, use direct conversion
                if (SelectedEllipsoid.AreTheSame(Ellipsoids.WGS84))
                {
                    return MapProjects.GeodeticWgs84ToWebMercator(geodeticPointDms);
                }
                else
                {
                    // Convert from selected ellipsoid to WGS84, then to Web Mercator
                    var wgs84Point = Transformations.ChangeDatum(geodeticPointDms, SelectedEllipsoid, Ellipsoids.WGS84);
                    return MapProjects.GeodeticWgs84ToWebMercator(wgs84Point);
                }

            default:
                throw new NotSupportedException($"SRS type {SelectedSrsType} is not supported");
        }
    }

    /// <summary>
    /// Converts coordinates from Web Mercator to the selected SRS
    /// </summary>
    public (double x, double y) ConvertFromWebMercator(Point webMercatorPoint)
    {
        switch (SelectedSrsType)
        {
            case CoordinateDisplayMode.UTM:
                // Convert Web Mercator to Geodetic, then to UTM
                // UTM always uses WGS84 ellipsoid
                var geodeticFromWebMercator = MapProjects.WebMercatorToGeodeticWgs84(webMercatorPoint);
                var utmPoint = MapProjects.GeodeticToUTM(geodeticFromWebMercator, Ellipsoids.WGS84, UtmZone, geodeticFromWebMercator.Y > 0);
                return (utmPoint.X, utmPoint.Y);

            case CoordinateDisplayMode.WebMercator:
                return (webMercatorPoint.X, webMercatorPoint.Y);

            case CoordinateDisplayMode.GeodeticDecimal:
                var geodetic = MapProjects.WebMercatorToGeodeticWgs84(webMercatorPoint);
                // If ellipsoid is not WGS84, convert to selected ellipsoid
                if (!SelectedEllipsoid.AreTheSame(Ellipsoids.WGS84))
                {
                    var convertedGeodetic = Transformations.ChangeDatum(geodetic, Ellipsoids.WGS84, SelectedEllipsoid);
                    return (convertedGeodetic.X, convertedGeodetic.Y);
                }
                return (geodetic.X, geodetic.Y);

            case CoordinateDisplayMode.GeodeticDms:
                // Convert to decimal degrees first, then format as DMS string
                var geodeticDms = MapProjects.WebMercatorToGeodeticWgs84(webMercatorPoint);
                if (!SelectedEllipsoid.AreTheSame(Ellipsoids.WGS84))
                {
                    var convertedGeodeticDms = Transformations.ChangeDatum(geodeticDms, Ellipsoids.WGS84, SelectedEllipsoid);
                    return (convertedGeodeticDms.X, convertedGeodeticDms.Y);
                }
                return (geodeticDms.X, geodeticDms.Y);

            default:
                throw new NotSupportedException($"SRS type {SelectedSrsType} is not supported");
        }
    }

    /// <summary>
    /// Gets display string for a coordinate value based on selected SRS type
    /// </summary>
    public string GetDisplayString(double value, bool isLongitude = false)
    {
        switch (SelectedSrsType)
        {
            case CoordinateDisplayMode.UTM:
            case CoordinateDisplayMode.WebMercator:
                // Use XY precision for map projections
                return FormatWithPrecision(value, XYPrecision);

            case CoordinateDisplayMode.GeodeticDecimal:
                // Use LatLong precision for geodetic coordinates
                return FormatWithPrecision(value, LatLongPrecision);

            case CoordinateDisplayMode.GeodeticDms:
                return DegreeHelper.ToDms(value, true);

            default:
                return value.ToString();
        }
    }

    private string FormatWithPrecision(double value, int precision)
    {
        if (precision == 0)
            return value.ToString("#,#");

        string format = "#,#." + new string('0', precision);
        return value.ToString(format);
    }

    /// <summary>
    /// Converts DMS string to decimal degrees
    /// </summary>
    public static double ParseDmsToDecimal(string dmsString)
    {
        // Simple parser for DMS format like "35°30'45.123\"N" or "35 30 45.123"
        // This is a basic implementation - can be enhanced
        if (string.IsNullOrWhiteSpace(dmsString))
            return double.NaN;

        // Remove direction indicators (N/S/E/W) and parse
        dmsString = dmsString.Trim().ToUpper();
        bool isNegative = dmsString.Contains('S') || dmsString.Contains('W');
        dmsString = dmsString.Replace("N", "").Replace("S", "").Replace("E", "").Replace("W", "")
                             .Replace("°", " ").Replace("'", " ").Replace("\"", "").Trim();

        var parts = dmsString.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 1)
            return double.NaN;

        double degrees = 0;
        double minutes = 0;
        double seconds = 0;

        if (parts.Length >= 1 && double.TryParse(parts[0], out var deg))
            degrees = deg;
        if (parts.Length >= 2 && double.TryParse(parts[1], out var min))
            minutes = min;
        if (parts.Length >= 3 && double.TryParse(parts[2], out var sec))
            seconds = sec;

        double result = degrees + minutes / 60.0 + seconds / 3600.0;
        return isNegative ? -result : result;
    }
}

