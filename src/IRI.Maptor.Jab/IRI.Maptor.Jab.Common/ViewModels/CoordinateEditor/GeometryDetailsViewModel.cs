using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Jab.Common.Layers;
using IRI.Maptor.Jab.Common.Models;
using IRI.Maptor.Jab.Common.Helpers;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Jab.Common.Services;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Spatial.IO.TopoJson;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;

namespace IRI.Maptor.Jab.Common.ViewModels.CoordinateEditor;

public class GeometryDetailsViewModel : Notifier
{
    private readonly IDialogService _dialogService;

    private readonly EditableFeatureLayer _editableFeatureLayer;

    public GeometryDetailsViewModel(EditableFeatureLayer editableFeatureLayer, IDialogService dialogService)
    {
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        _editableFeatureLayer = editableFeatureLayer;

        // Subscribe to LocateablesReconstructed event to refresh geometry when parts change
        _editableFeatureLayer.LocateablesReconstructed += EditableFeatureLayer_LocateablesReconstructed;

        // Initialize available formats
        AvailableFormats = new ObservableCollection<string>
        {
            "WKT",
            "WKB",
            "SQL Server WKT",
            "SQL Server Native Binary",
            "GeoJSON",
            //"GML 2",
            //"GML 3",
            "KML",
            "Esri JSON Geometry",
            "TopoJSON",
            "DXF"
        };

        SelectedFormat = "WKT"; // Default format

        this.Geometry = editableFeatureLayer.GetFinalFixedGeometry();

        this.GeometryEditor = new GeometryEditorViewModel(_editableFeatureLayer);
    }

    private void EditableFeatureLayer_LocateablesReconstructed()
    {
        // Refresh geometry when parts are added/deleted
        // This will trigger UpdateAllProperties() which updates all dependent properties
        Geometry = _editableFeatureLayer.GetFinalFixedGeometry();
    }

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

    public CoordinateDimension Dimension => _geometry.GetDimension();

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
        private set
        {
            _utmZones = value ?? new List<int>();
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(SpansMultipleUtmZones));
        }
    }

    public bool SpansMultipleUtmZones => UtmZones.Count > 1;

    public int NumberOfPoints => Geometry.TotalNumberOfPoints;

    public int NumberOfGeometries => Geometry.NumberOfGeometries;

    public string GeometryType => Geometry.Type.ToString();


    #region Format selection properties

    private ObservableCollection<string> _availableFormats;
    public ObservableCollection<string> AvailableFormats
    {
        get => _availableFormats;
        set
        {
            _availableFormats = value ?? new ObservableCollection<string>();
            RaisePropertyChanged();
        }
    }

    private string _selectedFormat = "WKT";
    public string SelectedFormat
    {
        get => _selectedFormat;
        set
        {
            if (_selectedFormat != value)
            {
                _selectedFormat = value;
                RaisePropertyChanged();
                UpdateStringRepresentation();
            }
        }
    }

    private string _stringRepresentation = string.Empty;
    public string StringRepresentation
    {
        get => _stringRepresentation;
        set
        {
            _stringRepresentation = value ?? string.Empty;
            RaisePropertyChanged();
        }
    }

    // Export command
    private RelayCommand _exportCommand;
    public RelayCommand ExportCommand =>
        _exportCommand ??= new RelayCommand(param => ExportCurrentFormat());

    private async Task ExportCurrentFormat()
    {
        if (string.IsNullOrEmpty(StringRepresentation))
        {
            await _dialogService.ShowMessageAsync(
                $"No {SelectedFormat} content available to export.",
                "Export Failed",
                null);
            return;
        }

        // Determine file extension based on format
        string extension = SelectedFormat switch
        {
            "WKT" => ".wkt",
            "WKB" => ".wkb",
            "SQL Server WKT" => ".wkt",
            "SQL Server Native Binary" => ".bin",
            "GeoJSON" => ".geojson",
            "GML 2" => ".gml",
            "GML 3" => ".gml",
            "KML" => ".kml",
            "Esri JSON Geometry" => ".json",
            "TopoJSON" => ".topojson",
            "DXF" => ".dxf",
            _ => ".txt"
        };

        var fileName = await _dialogService.ShowSaveFileDialogAsync(
            $"{SelectedFormat} files (*{extension})|*{extension}|All files (*.*)|*.*",
            null,
            $"geometry{extension}");

        if (!string.IsNullOrEmpty(fileName))
        {
            try
            {
                File.WriteAllText(fileName, StringRepresentation);
                await _dialogService.ShowMessageAsync(
                    $"{SelectedFormat} exported successfully.",
                    "Export Success",
                    null);
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageAsync(
                    $"Failed to export {SelectedFormat}: {ex.Message}",
                    "Export Failed",
                    null);
            }
        }
    }


    private void UpdateStringRepresentation()
    {
        if (_geometry == null)
        {
            StringRepresentation = string.Empty;
            return;
        }

        try
        {
            switch (SelectedFormat)
            {
                case "WKT":
                    StringRepresentation = _geometry.AsWkt() ?? string.Empty;
                    break;

                case "WKB":
                    var wkbBytes = _geometry.AsWkb();
                    StringRepresentation = wkbBytes != null ? HexStringHelper.ToHexStringUsingBitFiddle(wkbBytes, true) : string.Empty;
                    break;

                case "SQL Server WKT":
                    StringRepresentation = _geometry.AsSqlServerWkt() ?? string.Empty;
                    break;

                case "SQL Server Native Binary":
                    var nativeBytes = _geometry.AsSqlServerNativeBinary();
                    StringRepresentation = nativeBytes != null ? HexStringHelper.ToHexStringUsingBitFiddle(nativeBytes, true) : string.Empty;
                    break;

                case "GeoJSON":
                    var geoJson = _geometry.AsGeoJson()?.Serialize(indented: true, removeSpaces: false);

                    StringRepresentation = geoJson ?? string.Empty;
                    break;

                case "GML 2":
                    // Not directly available, leave empty for now
                    StringRepresentation = string.Empty;
                    break;

                case "GML 3":
                    // Requires SqlGeometry conversion, leave empty for now
                    StringRepresentation = string.Empty;
                    break;

                case "KML":
                    if (_geometry is Geometry<Point> geom)
                    {
                        var kmlGeometry = geom.Srid == SridHelper.GeodeticWGS84
                            ? geom
                            : geom.Project(SrsBases.GeodeticWgs84);
                        StringRepresentation = kmlGeometry.AsKml() ?? string.Empty;
                    }
                    else
                    {
                        StringRepresentation = string.Empty;
                    }
                    break;

                case "Esri JSON Geometry":
                    if (_geometry is Geometry<Point> egeom)
                    {
                        StringRepresentation = egeom.AsEsriJsonGeometry().ToString() ?? string.Empty;
                    }
                    else
                    {
                        StringRepresentation = string.Empty;
                    }
                    break;
                      
                case "TopoJSON":
                    if (_geometry is Geometry<Point> topoGeom && !topoGeom.IsNullOrEmpty())
                    {
                        var topoJson = TopoJsonConverter.FromFeatures([topoGeom.AsFeature()]);
                        StringRepresentation = JsonSerializer.Serialize(topoJson);
                    }
                    else
                    {
                        StringRepresentation = string.Empty;
                    }
                    break;

                case "DXF":
                    // Not directly available, leave empty for now
                    StringRepresentation = string.Empty;
                    break;

                default:
                    StringRepresentation = string.Empty;
                    break;
            }
        }
        catch
        {
            StringRepresentation = string.Empty;
        }
    }

    #endregion


    // Editor properties
    private GeometryEditorViewModel _geometryEditor;
    public GeometryEditorViewModel GeometryEditor
    {
        get => _geometryEditor;
        set
        {
            // Unsubscribe from old editor's events
            if (_geometryEditor != null)
            {
                _geometryEditor.RequestUpdateCurrentEditingPoint -= GeometryEditor_RequestUpdateCurrentEditingPoint;
                _geometryEditor.RequestZoomToPoint -= GeometryEditor_RequestZoomToPoint;
                _geometryEditor.RequestZoomToGeometry -= GeometryEditor_RequestZoomToGeometry;
                _geometryEditor.RequestPanToPoint -= GeometryEditor_RequestPanToPoint;
                _geometryEditor.RequestCopyCoordinate -= GeometryEditor_RequestCopyCoordinate;
            }

            _geometryEditor = value;

            // Subscribe to new editor's events
            if (_geometryEditor != null)
            {
                _geometryEditor.RequestUpdateCurrentEditingPoint += GeometryEditor_RequestUpdateCurrentEditingPoint;
                _geometryEditor.RequestZoomToPoint += GeometryEditor_RequestZoomToPoint;
                _geometryEditor.RequestZoomToGeometry += GeometryEditor_RequestZoomToGeometry;
                _geometryEditor.RequestPanToPoint += GeometryEditor_RequestPanToPoint;
                _geometryEditor.RequestCopyCoordinate += GeometryEditor_RequestCopyCoordinate;
            }

            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// Action to update CurrentEditingPoint in MapViewModelBase when coordinates change in DataGrid
    /// </summary>
    public Action<Point>? RequestUpdateCurrentEditingPoint { get; set; }

    /// <summary>
    /// Action to zoom to a point on the map
    /// </summary>
    public Action<Point>? RequestZoomToPoint { get; set; }

    public Action<IGeometry?>? RequestZoomToGeometry { get; set; }

    /// <summary>
    /// Action to pan to a point on the map
    /// </summary>
    public Action<Point>? RequestPanToPoint { get; set; }

    /// <summary>
    /// Action to copy coordinate to clipboard
    /// </summary>
    public Action<Locateable, CoordinateDisplayMode>? RequestCopyCoordinate { get; set; }

    private void GeometryEditor_RequestUpdateCurrentEditingPoint(Point webMercatorPoint)
    {
        RequestUpdateCurrentEditingPoint?.Invoke(webMercatorPoint);
    }

    private void GeometryEditor_RequestZoomToPoint(Locateable locateable)
    {
        if (locateable != null)
        {
            var point = new Point(locateable.X, locateable.Y);
            RequestZoomToPoint?.Invoke(point);
        }
    }

    private void GeometryEditor_RequestZoomToGeometry(IGeometry? geometry)
    {
        if (geometry != null)
        {
            RequestZoomToGeometry?.Invoke(geometry);
        }
    }


    private void GeometryEditor_RequestPanToPoint(Locateable locateable)
    {
        if (locateable != null)
        {
            var point = new Point(locateable.X, locateable.Y);
            RequestPanToPoint?.Invoke(point);
        }
    }

    private void GeometryEditor_RequestCopyCoordinate(Locateable locateable, CoordinateDisplayMode mode)
    {
        if (locateable is null)
            return;

        //var point = new Point(locateable.X, locateable.Y);

        RequestCopyCoordinate?.Invoke(locateable, mode);
    }

    public event Action? RequestClose;

    private RelayCommand _saveGeometryCommand;
    public RelayCommand SaveGeometryCommand =>
        _saveGeometryCommand ??= new RelayCommand(param =>
        {
            _editableFeatureLayer.FinishEditing();
            RequestClose?.Invoke();
        }, param => true);

    private RelayCommand _cancelEditingCommand;
    public RelayCommand CancelEditingCommand =>
        _cancelEditingCommand ??= new RelayCommand(param =>
        {
            _editableFeatureLayer.CancelDrawing();
            RequestClose?.Invoke();
        }, param => true);


    private RelayCommand _copySelectedFormat;

    public RelayCommand CopySelectedFormat
    {
        get
        {
            if (_copySelectedFormat == null)
            {
                _copySelectedFormat = new RelayCommand(param =>
                {
                    ClipboardHelper.CopyText(StringRepresentation);
                });
            }

            return _copySelectedFormat;
        }
    }


    private void UpdateAllProperties()
    {
        if (_geometry == null || _geometry.IsEmpty())
        {
            ClearAllProperties();
            return;
        }

        // Calculate dimension
        //Dimension = _geometry.GetDimension();
        RaisePropertyChanged(nameof(Dimension));

        // Get geometry in WGS84 geodetic for calculations
        Geometry<Point>? geodeticGeometry = _editableFeatureLayer.GetGeodeticWgs84Geometery();
        if (geodeticGeometry == null)
        {
            ClearAllProperties();
            return;
        }

        // Update counts
        RaisePropertyChanged(nameof(NumberOfPoints));
        RaisePropertyChanged(nameof(NumberOfGeometries));
        RaisePropertyChanged(nameof(GeometryType));
        //NumberOfPoints = _geometry.TotalNumberOfPoints;
        //NumberOfGeometries = _geometry.NumberOfGeometries;
        //GeometryType = _geometry.Type.ToString();

        // Calculate UTM zones
        CalculateUtmZones(geodeticGeometry);

        // Extract points in WGS84 geodetic
        //ExtractPoints(geodeticGeometry);

        // Update string representation for selected format
        UpdateStringRepresentation();

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

        UtmZone = UtmZones.FirstOrDefault();

    }

    //private void ExtractPoints(Geometry<Point> geodeticGeometry)
    //{
    //    var points = new ObservableCollection<NotifiablePoint>();

    //    if (geodeticGeometry == null || geodeticGeometry.IsNullOrEmpty())
    //    {
    //        //Points = points;
    //        return;
    //    }

    //    var allGeodeticPoints = geodeticGeometry.GetAllPoints();
    //    if (allGeodeticPoints == null || allGeodeticPoints.Count == 0)
    //    {
    //        //Points = points;
    //        return;
    //    }

    //    // Get original points with Z/M if available
    //    List<IPoint>? originalPoints = null;
    //    if (_geometry is Geometry<PointZ> geomZ)
    //    {
    //        originalPoints = geomZ.GetAllPoints()?.Cast<IPoint>().ToList();
    //    }
    //    else if (_geometry is Geometry<PointM> geomM)
    //    {
    //        originalPoints = geomM.GetAllPoints()?.Cast<IPoint>().ToList();
    //    }
    //    else if (_geometry is Geometry<PointZM> geomZM)
    //    {
    //        originalPoints = geomZM.GetAllPoints()?.Cast<IPoint>().ToList();
    //    }

    //    // Match geodetic points with original points to get Z/M
    //    for (int i = 0; i < allGeodeticPoints.Count; i++)
    //    {
    //        var geodeticPoint = allGeodeticPoints[i];
    //        var pointInfo = new NotifiablePoint
    //        {
    //            X = geodeticPoint.X, // Longitude
    //            Y = geodeticPoint.Y  // Latitude
    //        };

    //        // Try to get Z and M from original points if available
    //        if (originalPoints != null && i < originalPoints.Count)
    //        {
    //            var originalPoint = originalPoints[i];
    //            if (originalPoint is IHasZ hasZ)
    //            {
    //                pointInfo.Z = hasZ.Z;
    //            }
    //            if (originalPoint is IHasM hasM)
    //            {
    //                pointInfo.M = hasM.M;
    //            }
    //        }

    //        points.Add(pointInfo);
    //    }

    //    //Points = points;
    //}

    private void ClearAllProperties()
    {
        //Dimension = CoordinateDimension.TwoD;
        UtmZone = null;
        UtmZones = new List<int>();
        //NumberOfPoints = 0;
        //NumberOfGeometries = 0;
        //GeometryType = string.Empty;
        //Points = new ObservableCollection<PointInfo>();
        StringRepresentation = string.Empty;
    }

    internal void ChangeCurrentEditingPoint(Point currentWebMercatorEditingPoint)
    {
        GeometryEditor.ChangeCurrentEditingPoint(currentWebMercatorEditingPoint);
    }
}

