using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using IRI.Maptor.Extensions;
using IRI.Maptor.Jab.Common;
using IRI.Maptor.Jab.Common.Abstractions;
using IRI.Maptor.Jab.Common.Assets.Commands;
using IRI.Maptor.Jab.Common.Localization;
using IRI.Maptor.Jab.Controls.Models.GeometryDetails;
using IRI.Maptor.Jab.Controls.Presenters.CoordinateEditors;
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.IO.TopoJson;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;

namespace IRI.Maptor.Jab.Controls.Presenters;

public class GeometryDetailsPresenter : Notifier
{
    private readonly IDialogService _dialogService;

    public Action<Point>? RequestZoomToPoint { get; set; }

    public GeometryDetailsPresenter(IDialogService dialogService)
    {
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        LocalizationManager.Instance.LanguageChanged += OnLanguageChanged;
    }

    private void OnLanguageChanged()
    {
        RaisePropertyChanged(nameof(LGeometryType));
        RaisePropertyChanged(nameof(LDimension));
        RaisePropertyChanged(nameof(LNumberOfPoints));
        RaisePropertyChanged(nameof(LNumberOfGeometries));
        RaisePropertyChanged(nameof(LUtmZone));
        RaisePropertyChanged(nameof(LPoints));
        RaisePropertyChanged(nameof(LExteriorRing));
        RaisePropertyChanged(nameof(LInteriorRing));
        RaisePropertyChanged(nameof(LAddPart));
        RaisePropertyChanged(nameof(LDeletePart));
        RaisePropertyChanged(nameof(LSave));
        RaisePropertyChanged(nameof(LCancel));
        RaisePropertyChanged(nameof(LExport));
        RaisePropertyChanged(nameof(LZoomToPoint));
        RaisePropertyChanged(nameof(LFormats));
        RaisePropertyChanged(nameof(LEditCoordinates));
    }

    // Localization properties
    public string LGeometryType => LocalizationManager.Instance["GeometryDetailsView_GeometryType"] ?? "Geometry Type";
    public string LDimension => LocalizationManager.Instance["GeometryDetailsView_Dimension"] ?? "Dimension";
    public string LNumberOfPoints => LocalizationManager.Instance["GeometryDetailsView_NumberOfPoints"] ?? "Number of Points";
    public string LNumberOfGeometries => LocalizationManager.Instance["GeometryDetailsView_NumberOfGeometries"] ?? "Number of Geometries";
    public string LUtmZone => LocalizationManager.Instance["GeometryDetailsView_UtmZone"] ?? "UTM Zone";
    public string LPoints => LocalizationManager.Instance["GeometryDetailsView_Points"] ?? "Points";
    public string LExteriorRing => LocalizationManager.Instance["GeometryDetailsView_ExteriorRing"] ?? "Exterior Ring";
    public string LInteriorRing => LocalizationManager.Instance["GeometryDetailsView_InteriorRing"] ?? "Interior Ring";
    public string LAddPart => LocalizationManager.Instance["GeometryDetailsView_AddPart"] ?? "Add Part";
    public string LDeletePart => LocalizationManager.Instance["GeometryDetailsView_DeletePart"] ?? "Delete Part";
    public string LSave => LocalizationManager.Instance["GeometryDetailsView_Save"] ?? "Save";
    public string LCancel => LocalizationManager.Instance["GeometryDetailsView_Cancel"] ?? "Cancel";
    public string LExport => LocalizationManager.Instance["GeometryDetailsView_Export"] ?? "Export";
    public string LZoomToPoint => LocalizationManager.Instance["GeometryDetailsView_ZoomToPoint"] ?? "Zoom to Point";
    public string LFormats => LocalizationManager.Instance["GeometryDetailsView_Formats"] ?? "Geometry Formats";
    public string LEditCoordinates => LocalizationManager.Instance["GeometryDetailsView_EditCoordinates"] ?? "Edit Coordinates";
    public string LBasicInfo => LocalizationManager.Instance["GeometryDetailsView_BasicInfo"] ?? "Basic Information";
    private IGeometry _geometry;
    public IGeometry Geometry
    {
        get => _geometry;
        set
        {
            _geometry = value;
            UpdateAllProperties();
        }
    }

    private CoordinateDimension _dimension;
    public CoordinateDimension Dimension
    {
        get => _dimension;
        set
        {
            _dimension = value;
            RaisePropertyChanged();
        }
    }

    private int? _utmZone;
    public int? UtmZone
    {
        get => _utmZone;
        set
        {
            _utmZone = value;
            RaisePropertyChanged();
        }
    }

    private List<int> _utmZones = new List<int>();
    public List<int> UtmZones
    {
        get => _utmZones;
        set
        {
            _utmZones = value ?? new List<int>();
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(SpansMultipleUtmZones));
        }
    }

    public bool SpansMultipleUtmZones => UtmZones.Count > 1;

    private int _numberOfPoints;
    public int NumberOfPoints
    {
        get => _numberOfPoints;
        set
        {
            _numberOfPoints = value;
            RaisePropertyChanged();
        }
    }

    private int _numberOfGeometries;
    public int NumberOfGeometries
    {
        get => _numberOfGeometries;
        set
        {
            _numberOfGeometries = value;
            RaisePropertyChanged();
        }
    }

    private string _geometryType = string.Empty;
    public string GeometryType
    {
        get => _geometryType;
        set
        {
            _geometryType = value;
            RaisePropertyChanged();
        }
    }

    private ObservableCollection<PointInfo> _points = new ObservableCollection<PointInfo>();
    public ObservableCollection<PointInfo> Points
    {
        get => _points;
        set
        {
            _points = value ?? new ObservableCollection<PointInfo>();
            RaisePropertyChanged();
        }
    }

    // Format strings
    private string _wkt = string.Empty;
    public string Wkt
    {
        get => _wkt;
        set
        {
            _wkt = value;
            RaisePropertyChanged();
        }
    }

    private string _wkb = string.Empty;
    public string Wkb
    {
        get => _wkb;
        set
        {
            _wkb = value;
            RaisePropertyChanged();
        }
    }

    private string _sqlServerWkt = string.Empty;
    public string SqlServerWkt
    {
        get => _sqlServerWkt;
        set
        {
            _sqlServerWkt = value;
            RaisePropertyChanged();
        }
    }

    private string _sqlServerNativeBinary = string.Empty;
    public string SqlServerNativeBinary
    {
        get => _sqlServerNativeBinary;
        set
        {
            _sqlServerNativeBinary = value;
            RaisePropertyChanged();
        }
    }

    private string _geoJson = string.Empty;
    public string GeoJson
    {
        get => _geoJson;
        set
        {
            _geoJson = value;
            RaisePropertyChanged();
        }
    }

    private string _gml2 = string.Empty;
    public string Gml2
    {
        get => _gml2;
        set
        {
            _gml2 = value;
            RaisePropertyChanged();
        }
    }

    private string _gml3 = string.Empty;
    public string Gml3
    {
        get => _gml3;
        set
        {
            _gml3 = value;
            RaisePropertyChanged();
        }
    }

    private string _kml = string.Empty;
    public string Kml
    {
        get => _kml;
        set
        {
            _kml = value;
            RaisePropertyChanged();
        }
    }

    private string _esriJsonGeometry = string.Empty;
    public string EsriJsonGeometry
    {
        get => _esriJsonGeometry;
        set
        {
            _esriJsonGeometry = value;
            RaisePropertyChanged();
        }
    }

    private string _topoJson = string.Empty;
    public string TopoJson
    {
        get => _topoJson;
        set
        {
            _topoJson = value;
            RaisePropertyChanged();
        }
    }

    private string _dxf = string.Empty;
    public string Dxf
    {
        get => _dxf;
        set
        {
            _dxf = value;
            RaisePropertyChanged();
        }
    }

    // Export commands
    private RelayCommand _exportWktCommand;
    public RelayCommand ExportWktCommand =>
        _exportWktCommand ??= new RelayCommand(param => ExportFormat("WKT", ".wkt", Wkt));

    private RelayCommand _exportWkbCommand;
    public RelayCommand ExportWkbCommand =>
        _exportWkbCommand ??= new RelayCommand(param => ExportFormat("WKB", ".wkb", Wkb));

    private RelayCommand _exportSqlServerWktCommand;
    public RelayCommand ExportSqlServerWktCommand =>
        _exportSqlServerWktCommand ??= new RelayCommand(param => ExportFormat("SQL Server WKT", ".wkt", SqlServerWkt));

    private RelayCommand _exportSqlServerNativeBinaryCommand;
    public RelayCommand ExportSqlServerNativeBinaryCommand =>
        _exportSqlServerNativeBinaryCommand ??= new RelayCommand(param => ExportFormat("SQL Server Native Binary", ".bin", SqlServerNativeBinary));

    private RelayCommand _exportGeoJsonCommand;
    public RelayCommand ExportGeoJsonCommand =>
        _exportGeoJsonCommand ??= new RelayCommand(param => ExportFormat("GeoJSON", ".geojson", GeoJson));

    private RelayCommand _exportGml2Command;
    public RelayCommand ExportGml2Command =>
        _exportGml2Command ??= new RelayCommand(param => ExportFormat("GML 2", ".gml", Gml2));

    private RelayCommand _exportGml3Command;
    public RelayCommand ExportGml3Command =>
        _exportGml3Command ??= new RelayCommand(param => ExportFormat("GML 3", ".gml", Gml3));

    private RelayCommand _exportKmlCommand;
    public RelayCommand ExportKmlCommand =>
        _exportKmlCommand ??= new RelayCommand(param => ExportFormat("KML", ".kml", Kml));

    private RelayCommand _exportEsriJsonGeometryCommand;
    public RelayCommand ExportEsriJsonGeometryCommand =>
        _exportEsriJsonGeometryCommand ??= new RelayCommand(param => ExportFormat("Esri JSON Geometry", ".json", EsriJsonGeometry));

    private RelayCommand _exportTopoJsonCommand;
    public RelayCommand ExportTopoJsonCommand =>
        _exportTopoJsonCommand ??= new RelayCommand(param => ExportFormat("TopoJSON", ".topojson", TopoJson));

    private RelayCommand _exportDxfCommand;
    public RelayCommand ExportDxfCommand =>
        _exportDxfCommand ??= new RelayCommand(param => ExportFormat("DXF", ".dxf", Dxf));

    // Editor properties
    private object _geometryEditor;
    public object GeometryEditor
    {
        get => _geometryEditor;
        set
        {
            _geometryEditor = value;
            RaisePropertyChanged();
        }
    }

    private IGeometry _originalGeometry;
    private bool _isEditing;
    public bool IsEditing
    {
        get => _isEditing;
        set
        {
            _isEditing = value;
            RaisePropertyChanged();
        }
    }

    private RelayCommand _saveGeometryCommand;
    public RelayCommand SaveGeometryCommand =>
        _saveGeometryCommand ??= new RelayCommand(param => SaveGeometry(), param => IsEditing);

    private RelayCommand _cancelEditingCommand;
    public RelayCommand CancelEditingCommand =>
        _cancelEditingCommand ??= new RelayCommand(param => CancelEditing(), param => IsEditing);

    private void SaveGeometry()
    {
        // TODO: Convert editor data back to geometry
        // For now, just clear editing state
        IsEditing = false;
        _originalGeometry = null;
    }

    private void CancelEditing()
    {
        if (_originalGeometry != null)
        {
            Geometry = _originalGeometry;
        }
        IsEditing = false;
        _originalGeometry = null;
    }

    private void UpdateAllProperties()
    {
        if (_geometry == null || IsGeometryNullOrInvalid())
        {
            ClearAllProperties();
            return;
        }

        // Calculate dimension
        Dimension = CalculateDimension();

        // Get geometry in WGS84 geodetic for calculations
        Geometry<Point>? geodeticGeometry = GetGeodeticGeometry();
        if (geodeticGeometry == null)
        {
            ClearAllProperties();
            return;
        }

        // Update counts
        NumberOfPoints = _geometry.TotalNumberOfPoints;
        NumberOfGeometries = _geometry.NumberOfGeometries;
        GeometryType = _geometry.Type.ToString();

        // Calculate UTM zones
        CalculateUtmZones(geodeticGeometry);

        // Extract points in WGS84 geodetic
        ExtractPoints(geodeticGeometry);

        // Generate format strings
        GenerateFormatStrings();

        // Create geometry editor
        CreateGeometryEditor(geodeticGeometry);
    }

    private void CreateGeometryEditor(Geometry<Point>? geodeticGeometry)
    {
        if (_geometry == null)
        {
            GeometryEditor = null;
            return;
        }

        // Create appropriate editor based on geometry type
        switch (_geometry.Type)
        {
            case IRI.Maptor.Sta.Common.Primitives.GeometryType.Point:
                if (Points.Count > 0)
                {
                    var pointPresenter = new PointEditorPresenter(Points[0], canDelete: false);
                    if (RequestZoomToPoint != null)
                    {
                        pointPresenter.RequestZoomToPoint += RequestZoomToPoint;
                    }
                    GeometryEditor = pointPresenter;
                }
                break;

            case IRI.Maptor.Sta.Common.Primitives.GeometryType.LineString:
                var lineStringPresenter = new LineStringEditorPresenter(Points);
                GeometryEditor = lineStringPresenter;
                break;

            case IRI.Maptor.Sta.Common.Primitives.GeometryType.Polygon:
                if (geodeticGeometry != null)
                {
                    var polygonRings = CreatePolygonRings(geodeticGeometry);
                    var polygonPresenter = new PolygonEditorPresenter(polygonRings);
                    GeometryEditor = polygonPresenter;
                }
                break;

            case IRI.Maptor.Sta.Common.Primitives.GeometryType.MultiPoint:
                var multiPointPresenter = new MultiPointEditorPresenter(Points);
                GeometryEditor = multiPointPresenter;
                break;

            case IRI.Maptor.Sta.Common.Primitives.GeometryType.MultiLineString:
                var multiLineStringPresenter = new MultiLineStringEditorPresenter();
                // TODO: Populate with parts from geometry
                GeometryEditor = multiLineStringPresenter;
                break;

            case IRI.Maptor.Sta.Common.Primitives.GeometryType.MultiPolygon:
                var multiPolygonPresenter = new MultiPolygonEditorPresenter();
                // TODO: Populate with polygons from geometry
                GeometryEditor = multiPolygonPresenter;
                break;

            default:
                GeometryEditor = null;
                break;
        }
    }

    private ObservableCollection<RingInfo> CreatePolygonRings(Geometry<Point> geodeticGeometry)
    {
        var rings = new ObservableCollection<RingInfo>();
        
        if (geodeticGeometry?.Geometries != null)
        {
            for (int i = 0; i < geodeticGeometry.Geometries.Count; i++)
            {
                var ringGeometry = geodeticGeometry.Geometries[i];
                var ringPoints = new ObservableCollection<PointInfo>();
                
                if (ringGeometry?.Points != null)
                {
                    foreach (var point in ringGeometry.Points)
                    {
                        ringPoints.Add(new PointInfo { X = point.X, Y = point.Y });
                    }
                }
                
                rings.Add(new RingInfo 
                { 
                    IsExterior = i == 0, 
                    Points = ringPoints 
                });
            }
        }
        
        return rings;
    }

    private bool IsGeometryNullOrInvalid()
    {
        if (_geometry == null)
            return true;

        // Check if geometry has any points
        if (_geometry.TotalNumberOfPoints == 0)
            return true;

        // Try to cast to Geometry<Point> to check if invalid
        if (_geometry is Geometry<Point> geom)
        {
            return geom.IsNullOrEmpty() || !geom.IsValid();
        }
        else if (_geometry is Geometry<PointZ> geomZ)
        {
            return geomZ.IsNullOrEmpty() || !geomZ.IsValid();
        }
        else if (_geometry is Geometry<PointM> geomM)
        {
            return geomM.IsNullOrEmpty() || !geomM.IsValid();
        }
        else if (_geometry is Geometry<PointZM> geomZM)
        {
            return geomZM.IsNullOrEmpty() || !geomZM.IsValid();
        }

        // For other types, just check if it has points
        return false;
    }

    private CoordinateDimension CalculateDimension()
    {
        if (_geometry == null)
            return CoordinateDimension.TwoD;

        bool hasZ = _geometry.HasZ();
        bool hasM = _geometry.HasM();

        if (hasZ && hasM)
            return CoordinateDimension.ZM;
        if (hasZ)
            return CoordinateDimension.Z;
        if (hasM)
            return CoordinateDimension.M;
        return CoordinateDimension.TwoD;
    }

    private Geometry<Point>? GetGeodeticGeometry()
    {
        if (_geometry == null)
            return null;

        // Cast to Geometry<Point> for operations
        Geometry<Point>? geom = _geometry as Geometry<Point>;
        if (geom == null)
        {
            // Try to convert other point types
            if (_geometry is Geometry<PointZ> geomZ)
            {
                // Convert PointZ to Point (lose Z)
                var points = geomZ.GetAllPoints()?.Select(p => new Point(p.X, p.Y)).ToList();
                if (points != null && points.Count > 0)
                {
                    geom = Geometry<Point>.Create(points, geomZ.Type, geomZ.Srid);
                }
            }
            else if (_geometry is Geometry<PointM> geomM)
            {
                var points = geomM.GetAllPoints()?.Select(p => new Point(p.X, p.Y)).ToList();
                if (points != null && points.Count > 0)
                {
                    geom = Geometry<Point>.Create(points, geomM.Type, geomM.Srid);
                }
            }
            else if (_geometry is Geometry<PointZM> geomZM)
            {
                var points = geomZM.GetAllPoints()?.Select(p => new Point(p.X, p.Y)).ToList();
                if (points != null && points.Count > 0)
                {
                    geom = Geometry<Point>.Create(points, geomZM.Type, geomZM.Srid);
                }
            }
        }

        if (geom == null || geom.IsNullOrEmpty())
            return null;

        // Transform to WGS84 geodetic if needed
        if (geom.Srid != SridHelper.GeodeticWGS84)
        {
            try
            {
                // Try common transformations
                if (geom.Srid == SridHelper.WebMercator)
                {
                    geom = geom.Transform(MapProjects.WebMercatorToGeodeticWgs84, SridHelper.GeodeticWGS84);
                }
                else
                {
                    // For other SRIDs, we might need a more sophisticated transformation
                    // For now, assume it's already geodetic or can't transform
                    // In a real implementation, you'd check SRID and apply appropriate transformation
                }
            }
            catch
            {
                // Transformation failed, return null
                return null;
            }
        }

        return geom;
    }

    private void CalculateUtmZones(Geometry<Point> geodeticGeometry)
    {
        if (geodeticGeometry == null || geodeticGeometry.IsNullOrEmpty())
        {
            UtmZone = null;
            UtmZones = new List<int>();
            return;
        }

        var allPoints = geodeticGeometry.GetAllPoints();
        if (allPoints == null || allPoints.Count == 0)
        {
            UtmZone = null;
            UtmZones = new List<int>();
            return;
        }

        // Calculate UTM zone for each point
        var zones = new HashSet<int>();
        foreach (var point in allPoints)
        {
            try
            {
                int zone = MapProjects.FindUtmZone(point.X);
                zones.Add(zone);
            }
            catch
            {
                // Skip invalid points
            }
        }

        UtmZones = zones.OrderBy(z => z).ToList();

        // Primary UTM zone is from centroid
        try
        {
            var centroid = geodeticGeometry.GetCentroidPlusPoint();
            UtmZone = MapProjects.FindUtmZone(centroid.X);
        }
        catch
        {
            // If centroid fails, use first zone found
            UtmZone = UtmZones.FirstOrDefault();
        }
    }

    private void ExtractPoints(Geometry<Point> geodeticGeometry)
    {
        var points = new ObservableCollection<PointInfo>();

        if (geodeticGeometry == null || geodeticGeometry.IsNullOrEmpty())
        {
            Points = points;
            return;
        }

        var allGeodeticPoints = geodeticGeometry.GetAllPoints();
        if (allGeodeticPoints == null || allGeodeticPoints.Count == 0)
        {
            Points = points;
            return;
        }

        // Get original points with Z/M if available
        List<IPoint>? originalPoints = null;
        if (_geometry is Geometry<PointZ> geomZ)
        {
            originalPoints = geomZ.GetAllPoints()?.Cast<IPoint>().ToList();
        }
        else if (_geometry is Geometry<PointM> geomM)
        {
            originalPoints = geomM.GetAllPoints()?.Cast<IPoint>().ToList();
        }
        else if (_geometry is Geometry<PointZM> geomZM)
        {
            originalPoints = geomZM.GetAllPoints()?.Cast<IPoint>().ToList();
        }

        // Match geodetic points with original points to get Z/M
        for (int i = 0; i < allGeodeticPoints.Count; i++)
        {
            var geodeticPoint = allGeodeticPoints[i];
            var pointInfo = new PointInfo
            {
                X = geodeticPoint.X, // Longitude
                Y = geodeticPoint.Y  // Latitude
            };

            // Try to get Z and M from original points if available
            if (originalPoints != null && i < originalPoints.Count)
            {
                var originalPoint = originalPoints[i];
                if (originalPoint is IHasZ hasZ)
                {
                    pointInfo.Z = hasZ.Z;
                }
                if (originalPoint is IHasM hasM)
                {
                    pointInfo.M = hasM.M;
                }
            }

            points.Add(pointInfo);
        }

        Points = points;
    }

    private void GenerateFormatStrings()
    {
        if (_geometry == null)
        {
            ClearFormatStrings();
            return;
        }

        try
        {
            Wkt = _geometry.AsWkt() ?? string.Empty;
        }
        catch
        {
            Wkt = string.Empty;
        }

        try
        {
            var wkbBytes = _geometry.AsWkb();
            Wkb = wkbBytes != null ? HexStringHelper.ToHexStringUsingBitFiddle(wkbBytes, true) : string.Empty;
        }
        catch
        {
            Wkb = string.Empty;
        }

        try
        {
            SqlServerWkt = _geometry.AsSqlServerWkt() ?? string.Empty;
        }
        catch
        {
            SqlServerWkt = string.Empty;
        }

        try
        {
            var nativeBytes = _geometry.AsSqlServerNativeBinary();
            SqlServerNativeBinary = nativeBytes != null ? HexStringHelper.ToHexStringUsingBitFiddle(nativeBytes, true) : string.Empty;
        }
        catch
        {
            SqlServerNativeBinary = string.Empty;
        }

        try
        {
            var geoJson = _geometry.AsGeoJson();
            if (geoJson != null)
            {
                GeoJson = IRI.Maptor.Sta.Spatial.GeoJsonFormat.GeoJson.Serialize(geoJson, indented: true, removeSpaces: false);
            }
            else
            {
                GeoJson = string.Empty;
            }
        }
        catch
        {
            GeoJson = string.Empty;
        }

        // GML2 - not directly available, leave empty for now
        Gml2 = string.Empty;

        // GML3 - requires SqlGeometry conversion, leave empty for now
        Gml3 = string.Empty;

        try
        {
            if (_geometry is Geometry<Point> geom)
            {
                Kml = geom.AsKml() ?? string.Empty;
            }
            else
            {
                Kml = string.Empty;
            }
        }
        catch
        {
            Kml = string.Empty;
        }

        // EsriJsonGeometry - not directly available, leave empty for now
        EsriJsonGeometry = string.Empty;

        try
        {
            if (_geometry is Geometry<Point> geom && !geom.IsNullOrEmpty())
            {
                var topoJson = TopoJsonConverter.FromGeometry(geom);
                TopoJson = JsonSerializer.Serialize(topoJson);
            }
            else
            {
                TopoJson = string.Empty;
            }
        }
        catch
        {
            TopoJson = string.Empty;
        }

        // DXF - not directly available, leave empty for now
        Dxf = string.Empty;
    }

    private void ClearAllProperties()
    {
        Dimension = CoordinateDimension.TwoD;
        UtmZone = null;
        UtmZones = new List<int>();
        NumberOfPoints = 0;
        NumberOfGeometries = 0;
        GeometryType = string.Empty;
        Points = new ObservableCollection<PointInfo>();
        ClearFormatStrings();
    }

    private void ClearFormatStrings()
    {
        Wkt = string.Empty;
        Wkb = string.Empty;
        SqlServerWkt = string.Empty;
        SqlServerNativeBinary = string.Empty;
        GeoJson = string.Empty;
        Gml2 = string.Empty;
        Gml3 = string.Empty;
        Kml = string.Empty;
        EsriJsonGeometry = string.Empty;
        TopoJson = string.Empty;
        Dxf = string.Empty;
    }

    private async void ExportFormat(string formatName, string extension, string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            await _dialogService.ShowMessageAsync(
                $"No {formatName} content available to export.",
                "Export Failed",
                null);
            return;
        }

        var fileName = _dialogService.ShowSaveFileDialog(
            $"{formatName} files (*{extension})|*{extension}|All files (*.*)|*.*",
            null,
            $"geometry{extension}");

        if (!string.IsNullOrEmpty(fileName))
        {
            try
            {
                File.WriteAllText(fileName, content);
                await _dialogService.ShowMessageAsync(
                    $"{formatName} exported successfully.",
                    "Export Success",
                    null);
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageAsync(
                    $"Failed to export {formatName}: {ex.Message}",
                    "Export Failed",
                    null);
            }
        }
    }
}

