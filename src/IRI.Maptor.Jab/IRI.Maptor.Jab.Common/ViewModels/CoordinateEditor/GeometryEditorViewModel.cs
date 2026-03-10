using System;
using System.Linq;
using System.Windows.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Jab.Common.Assets.Commands;
using IRI.Maptor.Jab.Common.Models.CoordinateEditor;
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.Common.Enums;

namespace IRI.Maptor.Jab.Common.ViewModels.CoordinateEditor;

public class GeometryEditorViewModel : Notifier
{
    private EditableFeatureLayer _featureLayer;
    public EditableFeatureLayer FeatureLayer
    {
        get { return _featureLayer; }
        set
        {
            // Unsubscribe from old feature layer's event
            if (_featureLayer != null)
            {
                _featureLayer.LocateablesReconstructed -= FeatureLayer_LocateablesReconstructed;
            }

            _featureLayer = value;

            // Subscribe to new feature layer's event
            if (_featureLayer != null)
            {
                _featureLayer.LocateablesReconstructed += FeatureLayer_LocateablesReconstructed;
            }

            // Initialize SRS components if not already initialized
            if (_srsViewModel == null)
            {
                _srsViewModel = new CoordinateEditorSrsViewModel();
                // Subscribe to SRS changes to update CurrentPointEditor and DataGrid
                _srsViewModel.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(CoordinateEditorSrsViewModel.SelectedSrsType) ||
                        e.PropertyName == nameof(CoordinateEditorSrsViewModel.SelectedEllipsoid) ||
                        e.PropertyName == nameof(CoordinateEditorSrsViewModel.UtmZone))
                    {
                        _currentPointEditor?.UpdateFromSelectedPoint();
                        // Force DataGrid refresh by notifying that CurrentPagePoints has changed
                        // This will cause all MultiBindings in DataGrid cells to re-evaluate
                        RaisePropertyChanged(nameof(CurrentPagePoints));
                        // Also notify that Points collection changed to ensure bindings refresh
                        RaisePropertyChanged(nameof(Points));
                    }
                };
            }

            if (_currentPointEditor == null)
            {
                _currentPointEditor = new CurrentPointEditorModel
                {
                    SrsViewModel = _srsViewModel
                };
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(SrsViewModel));
            RaisePropertyChanged(nameof(CurrentPointEditor));
        }
    }


    public Geometry<Point>? Geometry => _featureLayer?.GetFinalGeometry();

    public bool IsEmptyGeometry => this.Geometry.IsNullOrEmpty() || this.HasNoPoints;

    public bool HasNoPoints => this.TotalPointCount < 1;

    public bool HasNoParts => this.Parts is null || this.Parts.Count < 1;

    public bool IsRingBase =>
            GeometryType == Sta.Common.Enums.GeometryType.Polygon ||
            GeometryType == Sta.Common.Enums.GeometryType.MultiPolygon;

    //public bool IsMultiGeometry => GeometryType == Sta.Common.Enums.GeometryType.MultiPoint ||
    //                                GeometryType == Sta.Common.Enums.GeometryType.MultiLineString ||
    //                                GeometryType == Sta.Common.Enums.GeometryType.MultiPolygon;

    public GeometryType? GeometryType => Geometry?.Type;

    private bool _isEditable = false;
    public bool IsEditable
    {
        get => _isEditable;
        set
        {
            _isEditable = value;
            RaisePropertyChanged();
        }
    }

    private bool _showInnerAngle = true;
    public bool ShowInnerAngle
    {
        get => _showInnerAngle;
        set
        {
            _showInnerAngle = value;
            RaisePropertyChanged();
            // Refresh angle columns when this changes
            RaisePropertyChanged(nameof(CurrentPagePoints));
        }
    }

    private bool _useDecimalDegreesForAngles = false;
    public bool UseDecimalDegreesForAngles
    {
        get => _useDecimalDegreesForAngles;
        set
        {
            _useDecimalDegreesForAngles = value;
            RaisePropertyChanged();
            // Refresh angle columns when this changes
            RaisePropertyChanged(nameof(CurrentPagePoints));
        }
    }

    private bool _showZColumn = true;
    public bool ShowZColumn
    {
        get => _showZColumn;
        set
        {
            _showZColumn = value;
            RaisePropertyChanged();
        }
    }

    public GeometryEditorViewModel(EditableFeatureLayer editableFeatureLayer)
    {
        this.FeatureLayer = editableFeatureLayer;

        RefreshPoints();

        this.IsEditable = true;

        // Points are already initialized by RefreshPointsFromCurrentPart() called in CurrentPartIndex setter
        // which also subscribes to PropertyChanged events
        // UpdateValidationState is called by RefreshPointsFromCurrentPart via UpdatePagingProperties
        UpdateValidationState();

        RaisePropertyChangedForMultiPartSection();
        RaisePropertyChangedForMultiPolygonSection();
    }


    #region Data Grid Paging

    private ObservableCollection<int> _availablePageSizes = new ObservableCollection<int> { 10, 20, 50, 100 };
    public ObservableCollection<int> AvailablePageSizes
    {
        get => _availablePageSizes;
        set
        {
            _availablePageSizes = value ?? new ObservableCollection<int> { 10, 20, 50 };
            RaisePropertyChanged();
        }
    }

    private int _selectedPageSize = 10;
    public int SelectedPageSize
    {
        get => _selectedPageSize;
        set
        {
            _selectedPageSize = value;
            RaisePropertyChanged();
            UpdatePagingProperties();
        }
    }


    private int _currentPageIndex = 0;
    public int CurrentPageIndex
    {
        get
        {
            return _currentPageIndex;
        }
        set
        {
            if (value < 0 || (TotalPages > 0 && value >= TotalPages))
                return;

            _currentPageIndex = value;

            //RaisePropertyChanged();
            //RaisePropertyChanged(nameof(CurrentPageNumber));
            //RaisePropertyChanged(nameof(CurrentPagePoints));
            //RaisePropertyChanged(nameof(PointsOnCurrentPage));
            //RaisePropertyChanged(nameof(IsLastPage));
            RaisePropertyChangedForDataGridSection();
        }
    }


    public int CurrentPageNumber => CurrentPageIndex + 1;

    public int TotalPages => IsEmptyGeometry ? 0 : (int)Math.Ceiling(TotalPointCount / (double)SelectedPageSize);

    public bool IsLastPage => TotalPages > 0 && CurrentPageIndex >= TotalPages - 1;

    #endregion


    #region Current Point Navigation

    public int CurrentPointIndex
    {
        get
        {
            if (SelectedPoint is null || Points is null)
                return -1;

            return Points.IndexOf(SelectedPoint);
        }
    }

    public int CurrentPointNumber => Math.Max(CurrentPointIndex + 1, 0);

    public int TotalPointCount => Points?.Count ?? 0;

    public int PointsOnCurrentPage => CurrentPagePoints.Count;

    public bool IsPreviousPointAvailable
    {
        get
        {
            if (SelectedPoint is null || IsEmptyGeometry)
                return false;

            return CurrentPointIndex > 0;
        }
    }

    public bool IsNextPointAvailable
    {
        get
        {
            if (SelectedPoint is null || IsEmptyGeometry)
                return false;

            return CurrentPointIndex >= 0 && CurrentPointIndex < TotalPointCount - 1;
        }
    }

    #endregion


    #region Editing Point Section

    private CoordinateEditorSrsViewModel? _srsViewModel;
    public CoordinateEditorSrsViewModel? SrsViewModel
    {
        get => _srsViewModel;
        set
        {
            if (_srsViewModel == value)
                return;

            _srsViewModel = value;
            RaisePropertyChanged();

            // Update CurrentPointEditor's SRS ViewModel reference
            if (_currentPointEditor != null)
            {
                _currentPointEditor.SrsViewModel = value;
            }
        }
    }

    private CurrentPointEditorModel? _currentPointEditor;
    public CurrentPointEditorModel? CurrentPointEditor
    {
        get => _currentPointEditor;
        set
        {
            if (_currentPointEditor == value)
                return;

            _currentPointEditor = value;
            RaisePropertyChanged();

            // Wire up SRS ViewModel
            if (_currentPointEditor != null && _srsViewModel != null)
            {
                _currentPointEditor.SrsViewModel = _srsViewModel;
            }
        }
    }

    /// <summary>
    /// Checks if the geometry has Z coordinates
    /// </summary>
    public bool HasZ()
    {
        if (FeatureLayer == null)
            return false;

        return Geometry?.HasZ() ?? false;
    }

    /// <summary>
    /// Checks if the geometry has M coordinates
    /// </summary>
    public bool HasM()
    {
        if (FeatureLayer == null)
            return false;

        return Geometry?.HasM() ?? false;
    }

    #endregion


    #region Parts

    private List<IGeometry>? Parts => GeometryType == Sta.Common.Enums.GeometryType.MultiPolygon ?
                                           CurrentPolygon?.GetGeometries() :
                                            Geometry?.GetGeometries();

    public IGeometry? CurrentPart
    {
        get
        {
            if (HasNoParts || CurrentPartIndex < 0 || CurrentPartIndex >= TotalPartCount)
                return null;

            return Parts![CurrentPartIndex];
        }
    }

    private int _currentPartIndex = 0;
    public int CurrentPartIndex
    {
        get => _currentPartIndex;
        set
        {
            if (value < 0 || value > TotalPartCount)
                return;

            _currentPartIndex = value;

            // Refresh Points collection from the current part
            RefreshPoints();

            RaisePropertyChangedForDataGridSection();

            RaisePropertyChangedForMultiPartSection();
        }
    }

    public int CurrentPartNumber => CurrentPartIndex + 1;

    public int TotalPartCount => Parts?.Count ?? 0;

    public bool IsNextPartAvailable => CurrentPart != null && CurrentPartIndex < TotalPartCount - 1;

    public bool IsPreviousPartAvailable => CurrentPart != null && CurrentPartIndex > 0;

    public bool CurrentPartIsValid => CurrentPart?.IsValid() == true;

    #endregion


    #region Multi Polygons

    private List<IGeometry>? Polygons => GeometryType == Sta.Common.Enums.GeometryType.MultiPolygon ?
                                            Geometry?.GetGeometries() : null;

    public IGeometry? CurrentPolygon
    {
        get
        {
            if (Polygons.IsNullOrEmpty() || CurrentPolygonIndex is null || CurrentPolygonIndex < 0 || CurrentPolygonIndex >= TotalPolygonCount)
                return null;

            return Polygons[CurrentPolygonIndex.Value];
        }
    }

    private int? _currentPolygonIndex = null;
    public int? CurrentPolygonIndex
    {
        get => _currentPolygonIndex;
        set
        {
            if (Polygons is null || value < 0 || value >= TotalPolygonCount)
                return;

            if (_currentPolygonIndex == value)
                return;

            _currentPolygonIndex = value;

            // Refresh the parts
            CurrentPartIndex = 0;

            // Refresh Points collection from the current part
            //RefreshPointsFromCurrentPart();

            RaisePropertyChanged();

            RaisePropertyChangedForDataGridSection();
            RaisePropertyChangedForMultiPartSection();
            RaisePropertyChangedForMultiPolygonSection();
        }
    }

    public int? CurrentPolygonNumber => CurrentPolygonIndex + 1;

    public int TotalPolygonCount => Polygons?.Count ?? 0;

    public bool IsNextPolygonAvailable => Polygons != null && CurrentPolygonIndex < TotalPolygonCount - 1;

    public bool IsPreviousPolygonAvailable => Polygons != null && CurrentPolygonIndex > 0;

    public bool CurrentPolygonIsValid => CurrentPolygon?.IsValid() == true;

    #endregion


    private void RaisePropertyChangedForDataGridSection()
    {
        // Notify that all CurrentPart-dependent properties have changed
        RaisePropertyChanged(nameof(CurrentPagePoints));

        RaisePropertyChanged(nameof(SelectedPoint));

        RaisePropertyChanged(nameof(CurrentPageIndex));
        RaisePropertyChanged(nameof(CurrentPageNumber));
        RaisePropertyChanged(nameof(TotalPages));
        RaisePropertyChanged(nameof(IsLastPage));

        RaisePropertyChanged(nameof(CurrentPointIndex));
        RaisePropertyChanged(nameof(CurrentPointNumber));
        RaisePropertyChanged(nameof(TotalPointCount));
        RaisePropertyChanged(nameof(PointsOnCurrentPage));
        RaisePropertyChanged(nameof(IsPreviousPointAvailable));
        RaisePropertyChanged(nameof(IsNextPointAvailable));

        RaisePropertyChanged(nameof(IsEditable));
    }

    private void RaisePropertyChangedForMultiPartSection()
    {
        RaisePropertyChanged(nameof(CurrentPart));
        RaisePropertyChanged(nameof(CurrentPartIndex));
        RaisePropertyChanged(nameof(CurrentPartNumber));
        RaisePropertyChanged(nameof(TotalPartCount));
        RaisePropertyChanged(nameof(IsNextPartAvailable));
        RaisePropertyChanged(nameof(IsPreviousPartAvailable));
        RaisePropertyChanged(nameof(CurrentPartIsValid));
    }

    private void RaisePropertyChangedForMultiPolygonSection()
    {
        RaisePropertyChanged(nameof(CurrentPolygon));
        RaisePropertyChanged(nameof(CurrentPolygonIndex));
        RaisePropertyChanged(nameof(CurrentPolygonNumber));
        RaisePropertyChanged(nameof(TotalPolygonCount));
        RaisePropertyChanged(nameof(IsNextPolygonAvailable));
        RaisePropertyChanged(nameof(IsPreviousPolygonAvailable));
        RaisePropertyChanged(nameof(CurrentPolygonIsValid));
    }

    #region Current Geometry or Geometry Part

    // all points of the current geometry part
    private ObservableCollection<Locateable> _points = new ObservableCollection<Locateable>();
    public ObservableCollection<Locateable> Points
    {
        get => _points;
        set
        {
            if (_points != null)
                _points.CollectionChanged -= Points_CollectionChanged;

            _points = value ?? new ObservableCollection<Locateable>();

            if (_points != null)
                _points.CollectionChanged += Points_CollectionChanged;

            RaisePropertyChanged();
            UpdatePagingProperties();
        }
    }

    public ObservableCollection<Locateable> CurrentPagePoints
    {
        get
        {
            if (IsEmptyGeometry)
                return new ObservableCollection<Locateable>();

            var startIndex = CurrentPageIndex * SelectedPageSize;

            var count = Math.Min(SelectedPageSize, Points.Count - startIndex);

            return new ObservableCollection<Locateable>(Points.Skip(startIndex).Take(count));
        }
    }

    private Locateable? _selectedPoint;

    private bool _isUpdatingFromChangeCurrentEditingPoint = false;

    public Locateable? SelectedPoint
    {
        get
        {
            return _selectedPoint;
        }
        set
        {
            if (_selectedPoint == value)
                return;

            // Unsubscribe from old point's PropertyChanged
            if (_selectedPoint != null)
            {
                _selectedPoint.PropertyChanged -= SelectedPoint_PropertyChanged;
            }

            _selectedPoint = value;

            // Subscribe to new point's PropertyChanged
            if (_selectedPoint != null)
            {
                _selectedPoint.PropertyChanged += SelectedPoint_PropertyChanged;
            }

            //RaisePropertyChanged();
            //RaisePropertyChanged(nameof(CurrentPointIndex));
            //RaisePropertyChanged(nameof(CurrentPointNumber));
            //RaisePropertyChanged(nameof(IsPreviousPointAvailable));
            //RaisePropertyChanged(nameof(IsNextPointAvailable));
            RaisePropertyChangedForDataGridSection();

            // Update command states
            CommandManager.InvalidateRequerySuggested();

            // Calculate global index from current part index and local point index
            if (_selectedPoint != null && CurrentPointIndex >= 0)
            {
                int globalIndex = GetGlobalIndex(CurrentPointIndex);
                FeatureLayer.SelectPoint(globalIndex);
            }

            // Update CurrentPointEditor when SelectedPoint changes
            if (_currentPointEditor != null)
            {
                _currentPointEditor.CurrentPoint = _selectedPoint;
                _currentPointEditor.HasZ = HasZ();
                _currentPointEditor.HasM = HasM();
            }
        }
    }

    /// <summary>
    /// Event fired when SelectedPoint coordinates change (from DataGrid editing)
    /// </summary>
    public event Action<Point>? RequestUpdateCurrentEditingPoint;

    private bool _hasInvalidPoints = false;
    public bool HasInvalidPoints
    {
        get => _hasInvalidPoints;
        private set
        {
            if (_hasInvalidPoints != value)
            {
                _hasInvalidPoints = value;
                RaisePropertyChanged();
                // Update command states
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public bool IsPointValid(Locateable point)
    {
        if (point == null)
            return false;
        return !double.IsNaN(point.X) && !double.IsNaN(point.Y);
    }

    protected void UpdateValidationState()
    {
        if (IsEmptyGeometry)
        {
            HasInvalidPoints = false;
            return;
        }

        HasInvalidPoints = Points.Any(p => !IsPointValid(p));
    }

    private void SelectedPoint_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_isUpdatingFromChangeCurrentEditingPoint)
            return; // Prevent infinite loop when updating from ChangeCurrentEditingPoint

        if (e.PropertyName == nameof(Locateable.X) || e.PropertyName == nameof(Locateable.Y))
        {
            if (_selectedPoint != null)
            {
                // Fire event with Web Mercator coordinates (SelectedPoint already uses Web Mercator)
                RequestUpdateCurrentEditingPoint?.Invoke(new Point(_selectedPoint.X, _selectedPoint.Y));

                // Update CurrentPointEditor when coordinates change (e.g., from map drag)
                _currentPointEditor?.UpdateFromSelectedPoint();

                // Refresh DataGrid display to show updated coordinates in selected SRS
                RaisePropertyChanged(nameof(CurrentPagePoints));
            }
        }
    }

    protected void Point_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Locateable.X) || e.PropertyName == nameof(Locateable.Y))
        {
            UpdateValidationState();
            PointsChanged?.Invoke(Points);
        }
    }

    public int GetPointNumber(Locateable point)
    {
        if (point == null || Points == null)
            return 0;

        int index = Points.IndexOf(point);
        return index >= 0 ? index + 1 : 0;
    }

    protected void Points_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Subscribe/unsubscribe to point property changes
        if (e.NewItems != null)
        {
            foreach (Locateable point in e.NewItems)
            {
                point.PropertyChanged += Point_PropertyChanged;
            }
        }
        if (e.OldItems != null)
        {
            foreach (Locateable point in e.OldItems)
            {
                point.PropertyChanged -= Point_PropertyChanged;
            }
        }

        UpdateValidationState();
        PointsChanged?.Invoke(Points);
        UpdatePagingProperties();

        // Refresh DataGrid when Points collection changes to update coordinate display
        RaisePropertyChanged(nameof(CurrentPagePoints));
    }

    private void UpdatePagingProperties()
    {
        RaisePropertyChanged(nameof(TotalPages));
        RaisePropertyChanged(nameof(TotalPointCount));
        RaisePropertyChanged(nameof(CurrentPagePoints));
        RaisePropertyChanged(nameof(PointsOnCurrentPage));
        RaisePropertyChanged(nameof(IsLastPage));

        // Adjust current page if needed
        if (TotalPages > 0 && CurrentPageIndex >= TotalPages)
        {
            CurrentPageIndex = TotalPages - 1;
        }

        UpdateValidationState();
    }

    internal void ChangeCurrentEditingPoint(Point currentWebMercatorEditingPoint)
    {
        if (SelectedPoint is null)
            return;

        // Set flag to prevent infinite loop when updating SelectedPoint coordinates
        _isUpdatingFromChangeCurrentEditingPoint = true;
        try
        {
            SelectedPoint.X = currentWebMercatorEditingPoint.X;
            SelectedPoint.Y = currentWebMercatorEditingPoint.Y;

            // Update CurrentPointEditor when coordinates change from map
            _currentPointEditor?.UpdateFromSelectedPoint();

            // Refresh DataGrid to show updated coordinates in selected SRS
            RaisePropertyChanged(nameof(CurrentPagePoints));
        }
        finally
        {
            _isUpdatingFromChangeCurrentEditingPoint = false;
        }
    }

    // in the case the selected point is changed outside
    // and even its coordinates may have been changed
    // index is the index of the point across all parts of a geometry
    // so it does not depend on the part nor on the 
    internal void UpdateSelectedPoint(Locateable l, int index)
    {
        if (l == null)
            return;

        // Multi-part geometry: find which part contains this global index
        //var (partIndex, localIndex) = GetPartAndLocalIndex(index);
        var pointAddress = Geometry.FindPointAddress(index);
        var partIndex = pointAddress.PartIndex;
        var localIndex = pointAddress.LocalPointIndex;

        if (partIndex < 0 && localIndex < 0)
            return;

        // Switch to the correct part if needed
        if (partIndex != CurrentPartIndex)
            CurrentPartIndex = partIndex;

        // Ensure Points collection is up to date
        if (localIndex < 0 || localIndex >= TotalPointCount)
            return;

        // Navigate to correct page
        int pageIndex = localIndex / SelectedPageSize;
        if (pageIndex != CurrentPageIndex)
        {
            CurrentPageIndex = pageIndex;
        }

        // Update the selected point and its coordinates
        this.SelectedPoint = Points[localIndex];

        if (this.SelectedPoint != null)
        {
            this.SelectedPoint.X = l.X;
            this.SelectedPoint.Y = l.Y;

            // Update CurrentPointEditor when coordinates change
            if (_currentPointEditor is not null)
            {
                //_currentPointEditor?.UpdateFromSelectedPoint();
                _currentPointEditor.CurrentPoint = this.SelectedPoint;
            }
        }

    }

    public event Action<ObservableCollection<Locateable>>? PointsChanged;

    /// <summary>
    /// Refreshes the Points collection from the current part's Locateable objects
    /// Uses the same Locateable instances from EditableFeatureLayer so changes on the map update the DataGrid
    /// </summary>
    /// <param name="preservePageAndSelection">If true, preserves the current page index and selection when possible</param>
    private void RefreshPoints()
    {
        if (FeatureLayer == null)
            return;

        if (Geometry == null)
            return;

        // Preserve current selection and page before refreshing
        Locateable? previousSelectedPoint = SelectedPoint;

        // Unsubscribe from old points
        if (_points != null)
        {
            foreach (var point in _points)
            {
                point.PropertyChanged -= Point_PropertyChanged;
            }
        }

        // Get Locateable objects from EditableFeatureLayer for the current part
        // These are the same instances used on the map, so changes will sync automatically
        List<Locateable> newPoints = FeatureLayer.GetLocateablesForPart(CurrentPolygonIndex, CurrentPartIndex);

        // Subscribe to new points' PropertyChanged events
        foreach (var point in newPoints)
        {
            point.PropertyChanged += Point_PropertyChanged;
        }

        // Update Points collection (this will trigger CollectionChanged)
        Points = new ObservableCollection<Locateable>(newPoints);

        // Reset page to first page when switching parts
        CurrentPageIndex = 0;
        SelectedPoint = null;
    }

    /// <summary>
    /// Handles the LocateablesReconstructed event from FeatureLayer
    /// Refreshes the Points collection when Locateables are reconstructed (e.g., after add/remove operations)
    /// </summary>
    private void FeatureLayer_LocateablesReconstructed()
    {
        // Validate and adjust CurrentPartIndex if needed
        if (TotalPartCount == 0 && _currentPartIndex != 0)
        {
            CurrentPartIndex = 0;
        }
        else if (TotalPartCount > 0 && CurrentPartIndex >= TotalPartCount)
        {
            // CurrentPartIndex is out of bounds, adjust to last valid index
            CurrentPartIndex = TotalPartCount - 1;
        }
        else
        {
            RefreshPoints();
        }


        //// Preserve page and selection when refreshing due to reconstruction
        //RefreshPointsFromCurrentPart();
    }

    /// <summary>
    /// Calculates the global index from a part index and local index within that part
    /// </summary>
    private int GetGlobalIndex(int localIndex)
    {
        if (Geometry is null)
            return -1;

        if (HasNoParts)
        {
            // Single-part geometry: local index is the global index
            return localIndex;
        }

        if (CurrentPartIndex < 0 || CurrentPartIndex >= TotalPartCount)
            return -1;

        if (GeometryType == Sta.Common.Enums.GeometryType.MultiPolygon)
            return Geometry.ToGlobalPointIndex(new PointAddress(CurrentPolygonIndex, CurrentPartIndex, localIndex));

        else
            return Geometry.ToGlobalPointIndex(new PointAddress(null, CurrentPartIndex, localIndex));         
    }

    #endregion


    public event Action<Locateable>? RequestPanToPoint;

    public event Action<Locateable>? RequestZoomToPoint;

    public event Action<Locateable>? RequestCopyCoordinate;

    public event Action<IGeometry?>? RequestZoomToGeometry;

    #region Methods

    private void AddNewPoint()
    {
        if (FeatureLayer == null)
            return;

        // Add point to the current part being viewed, not necessarily the last part 
        if (Geometry == null)
            return;

        //if (this.CurrentPointEditor?.CurrentPoint != null)
        //    this.CurrentPointEditor.CurrentPoint = null;

        // Use AddVertexToPart to add to the current part
        var newPoint = GetNewPoint();

        if (newPoint.IsNaN())
            return;

        Locateable? locatable = FeatureLayer.AddVertexToPart(newPoint, CurrentPolygonIndex, CurrentPartIndex);

        if (locatable is null)
            return;

        this.Points.Add(locatable);

        this.SelectedPoint = locatable;

        RaisePropertyChangedForDataGridSection();
        RaisePropertyChangedForMultiPartSection();
        RaisePropertyChangedForMultiPolygonSection();
    }

    //private void InsertNewPointBeforeSelectedPoint()
    //{
    //    if (SelectedPoint == null || HasNoPoints || FeatureLayer == null)
    //        return;

    //    int selectedIndex = Points.IndexOf(SelectedPoint);

    //    if (selectedIndex < 0)
    //        return;

    //    // Calculate global index from current part and local index
    //    int globalIndex = GetGlobalIndex(selectedIndex);

    //    // Insert point into geometry at the calculated global index
    //    // This will call ReconstructLocateables() which will trigger LocateablesReconstructed event
    //    // The event handler will refresh Points collection and preserve selection
    //    var newLocateable = FeatureLayer.InsertVertexAt(new Sta.Common.Primitives.Point(0, 0), globalIndex);

    //    if (newLocateable is null)
    //        return;

    //    // After reconstruction, Points collection is refreshed by event handler
    //    // Find the newly inserted point and select it
    //    var refreshedPoints = FeatureLayer.GetLocateablesForPart(CurrentPartIndex);
    //    int newLocalIndex = selectedIndex; // Inserted before selected, so same index
    //    if (newLocalIndex >= 0 && newLocalIndex < refreshedPoints.Count)
    //    {
    //        SelectedPoint = refreshedPoints[newLocalIndex];

    //        // Calculate which page the new point is on
    //        int pageOfNewPoint = newLocalIndex / SelectedPageSize;
    //        if (pageOfNewPoint != CurrentPageIndex)
    //        {
    //            CurrentPageIndex = pageOfNewPoint;
    //        }
    //    }


    //    PointsChanged?.Invoke(Points);
    //}

    private void DeletePoint(Locateable? point)
    {
        if (FeatureLayer is null || point is null)
            return;

        int indexToDelete = Points.IndexOf(point);

        if (indexToDelete < 0)
            return;

        // Calculate global index to select the point on the map before deleting
        int globalIndex = GetGlobalIndex(indexToDelete);

        // Select the point on the map so TryDeleteCurrentPoint can find it
        FeatureLayer.SelectPoint(globalIndex);

        // Delete the point from geometry
        // This will call ReconstructLocateables() which triggers LocateablesReconstructed event
        // The event handler will refresh Points collection
        FeatureLayer.TryDeleteCurrentPoint();

        SelectNearestPoint(indexToDelete);

    }

    private void SelectNearestPoint(int index)
    {
        // After reconstruction, Points collection is refreshed by event handler
        // Clear selection or select nearest remaining point
        if (TotalPointCount > 0)
        {
            // Select the point at the same index, or the previous one if at end
            int newIndex = index < TotalPointCount ? index : TotalPointCount - 1;

            if (newIndex >= 0)
            {
                SelectedPoint = Points[newIndex];

                // Adjust page if needed
                int pageOfNewPoint = newIndex / SelectedPageSize;

                if (pageOfNewPoint != CurrentPageIndex)
                {
                    CurrentPageIndex = pageOfNewPoint;
                }
            }
        }
        else
        {
            SelectedPoint = null;
            CurrentPageIndex = 0;
        }
    }

    private void GoToPoint(int index)
    {
        if (index < 0 || index >= TotalPointCount)
            return;

        // Navigate to previous point
        Locateable? point = Points[index];

        if (point == null)
            return;

        // Adjust page if needed
        int pageOfPoint = (index) / SelectedPageSize;

        if (pageOfPoint != CurrentPageIndex)
        {
            CurrentPageIndex = pageOfPoint;
        }

        SelectedPoint = point;
    }

    private Point GetNewPoint()
    {
        if (SrsViewModel is null || CurrentPointEditor is null)
            return Point.NaN;

        if (SrsViewModel.SelectedSrsType == Sta.SpatialReferenceSystem.CoordinateDisplayMode.GeodeticDms)
        {
            var point = CurrentPointEditor.GetNewLatLong();

            return SrsViewModel.ConvertToWebMercator(point.X, point.Y);
        }
        else
        {
            var point = CurrentPointEditor.GetNewXY();

            return SrsViewModel.ConvertToWebMercator(point.X, point.Y, SrsViewModel.UtmZone);
        }
    }

    #endregion


    #region Point Commands

    private RelayCommand? _addPointCommand;
    public RelayCommand AddPointCommand =>
        _addPointCommand ??= new RelayCommand(param => AddNewPoint(), param => !HasInvalidPoints);


    //private RelayCommand? _insertPointBeforeSelectedCommand;
    //public RelayCommand InsertPointBeforeSelectedCommand =>
    //    _insertPointBeforeSelectedCommand ??= new RelayCommand(param => InsertNewPointBeforeSelectedPoint(), param => SelectedPoint != null && !HasInvalidPoints);


    private RelayCommand? _deleteCurrentPointCommand;
    public RelayCommand DeleteCurrentPointCommand =>
        _deleteCurrentPointCommand ??= new RelayCommand(param => DeletePoint(SelectedPoint), param => SelectedPoint != null && FeatureLayer != null);


    private RelayCommand? _goToNextPageCommand;
    public RelayCommand GoToNextPageCommand =>
        _goToNextPageCommand ??= new RelayCommand(param =>
        {
            if (CurrentPageIndex < TotalPages - 1)
                CurrentPageIndex++;

        }, param => CurrentPageIndex < TotalPages - 1 && !HasInvalidPoints);


    private RelayCommand? _goToPreviousPageCommand;
    public RelayCommand GoToPreviousPageCommand =>
        _goToPreviousPageCommand ??= new RelayCommand(param =>
        {
            if (CurrentPageIndex > 0)
                CurrentPageIndex--;

        }, param => CurrentPageIndex > 0 && !HasInvalidPoints);


    private RelayCommand? _panToCurrentPointCommand;
    public RelayCommand PanToCurrentPointCommand =>
        _panToCurrentPointCommand ??= new RelayCommand(param =>
        {
            if (SelectedPoint != null)
            {
                RequestPanToPoint?.Invoke(SelectedPoint);
            }
        }, param => SelectedPoint != null && !HasInvalidPoints);


    private RelayCommand? _goToPreviousPointCommand;
    public RelayCommand GoToPreviousPointCommand =>
        _goToPreviousPointCommand ??= new RelayCommand(param =>
        {
            if (SelectedPoint == null || IsEmptyGeometry)
                return;

            int currentIndex = Points.IndexOf(SelectedPoint);

            GoToPoint(currentIndex - 1);

        }, param => IsPreviousPointAvailable && !HasInvalidPoints);


    private RelayCommand? _goToNextPointCommand;
    public RelayCommand GoToNextPointCommand =>
        _goToNextPointCommand ??= new RelayCommand(param =>
        {
            if (SelectedPoint == null || IsEmptyGeometry)
                return;

            int currentIndex = Points.IndexOf(SelectedPoint);

            GoToPoint(currentIndex + 1);

            //if (currentIndex < 0 || currentIndex >= TotalPointCount - 1)
            //    return;

            //// Navigate to next point
            //Locateable? nextPoint = Points[currentIndex + 1];
            //if (nextPoint == null)
            //    return;

            //SelectedPoint = nextPoint;

            //// Adjust page if needed
            //int pageOfNextPoint = (currentIndex + 1) / SelectedPageSize;
            //if (pageOfNextPoint != CurrentPageIndex)
            //{
            //    CurrentPageIndex = pageOfNextPoint;
            //}
        }, param => IsNextPointAvailable && !HasInvalidPoints);


    private RelayCommand? _zoomToCurrentPointCommand;
    public RelayCommand ZoomToCurrentPointCommand =>
        _zoomToCurrentPointCommand ??= new RelayCommand(param =>
        {
            if (SelectedPoint != null && IsPointValid(SelectedPoint))
            {
                RequestZoomToPoint?.Invoke(SelectedPoint);
            }
        }, param => SelectedPoint != null && IsPointValid(SelectedPoint) && !HasInvalidPoints);


    private RelayCommand? _copyCurrentPointCommand;
    public RelayCommand CopyCurrentPointCommand =>
        _copyCurrentPointCommand ??= new RelayCommand(param =>
        {
            if (SelectedPoint != null)
            {
                RequestCopyCoordinate?.Invoke(SelectedPoint);
            }
        }, param => SelectedPoint != null);



    private RelayCommand? _applyCurrentPointChangesCommand;
    public RelayCommand ApplyCurrentPointChangesCommand =>
        _applyCurrentPointChangesCommand ??= new RelayCommand(param =>
        {
            if (CurrentPointEditor == null)
                return;

            bool success = CurrentPointEditor.ApplyChanges();
            if (success)
            {
                // Changes applied successfully, the Locateable has been updated
                // The DataGrid will refresh automatically through property change notifications
                RaisePropertyChanged(nameof(CurrentPagePoints));
            }
        }, param => CurrentPointEditor != null && CurrentPointEditor.ValidateInput());

    #endregion


    #region Multi-part commands

    private RelayCommand? _addPartCommand;
    public RelayCommand AddPartCommand =>
        _addPartCommand ??= new RelayCommand(param =>
        {
            if (FeatureLayer == null)
                return;

            // Add a new empty part - this will trigger ReconstructLocateables()
            bool success = false;

            if (IsRingBase)
            {
                success = FeatureLayer.TryAddNewRing(CurrentPolygonIndex);
            }
            else
            {
                success = FeatureLayer.TryAddNewPart();
            }


            if (!success) return;

            // Wait for the geometry structure to be updated
            // The LocateablesReconstructed event will fire and refresh Points
            // After that, we can safely add a point to the new part

            int newPartIndex = TotalPartCount - 1;

            // Verify the new part exists in the geometry before adding point 
            if (Geometry?.Geometries != null && newPartIndex < Geometry.Geometries.Count)
            {
                // Add a point (0,0) to the new part
                // AddVertexToPart will directly manipulate the geometry and trigger ReconstructLocateables()
                var newPointLocatable = FeatureLayer.AddVertexToPart(GetNewPoint(), CurrentPolygonIndex, newPartIndex);

                if (newPointLocatable != null)
                {
                    // Navigate to the new part to show it in the DataGrid
                    // (Required to select the new point in the DataGrid)
                    CurrentPartIndex = newPartIndex;

                    // After CurrentPartIndex changes, RefreshPointsFromCurrentPart() is called
                    // Wait a moment for the Points collection to refresh, then select the new point
                    var refreshedPoints = FeatureLayer.GetLocateablesForPart(CurrentPolygonIndex, newPartIndex);
                    if (refreshedPoints.Count > 0)
                    {
                        // Select the newly added point (first point in the new part)
                        SelectedPoint = refreshedPoints[0];

                        // Calculate which page the new point is on and navigate to it
                        int pageOfNewPoint = 0 / SelectedPageSize; // First point is on page 0
                        if (pageOfNewPoint != CurrentPageIndex)
                        {
                            CurrentPageIndex = pageOfNewPoint;
                        }
                    }
                }
            }


            RaisePropertyChangedForDataGridSection();
            RaisePropertyChangedForMultiPartSection();
            RaisePropertyChangedForMultiPolygonSection();


            //GeometryChanged?.Invoke(Parts);
        });

    private RelayCommand? _deletePartCommand;
    public RelayCommand DeletePartCommand =>
        _deletePartCommand ??= new RelayCommand(param =>
        {
            if (FeatureLayer == null)
                return;

            int indexToDelete = -1;

            // Extract part index from parameter
            if (param is IGeometry part)
            {
                indexToDelete = Parts?.IndexOf(part) ?? -1;
            }
            else if (param is int partIndex)
            {
                indexToDelete = partIndex;
            }

            if (HasNoParts || indexToDelete < 0 || indexToDelete >= TotalPartCount)
                return;

            // Store state before deletion
            int oldPartCount = TotalPartCount;
            bool wasCurrentPart = indexToDelete == CurrentPartIndex;
            bool wasLastPart = indexToDelete == TotalPartCount - 1;

            // Delete the part from the geometry - this will trigger ReconstructLocateables()
            bool success = FeatureLayer.TryDeletePartByIndex(indexToDelete);

            if (success)
            {
                // After LocateablesReconstructed event fires and refreshes Points,
                // update CurrentPartIndex appropriately
                int newPartCount = TotalPartCount;

                if (newPartCount == 0)
                {
                    // All parts deleted
                    CurrentPartIndex = 0;
                }
                else if (wasCurrentPart)
                {
                    // Deleted the current part
                    if (wasLastPart && CurrentPartIndex > 0)
                    {
                        // Was last part, move to previous part
                        CurrentPartIndex = CurrentPartIndex - 1;
                    }
                    else if (!wasLastPart)
                    {
                        // Was not last part, stay at same index (which now points to next part)
                        CurrentPartIndex = CurrentPartIndex;
                    }
                    else
                    {
                        // Was last part but it was the only part, set to 0
                        CurrentPartIndex = 0;
                    }
                }
                else if (indexToDelete < CurrentPartIndex)
                {
                    // Deleted part was before current part, decrement index
                    CurrentPartIndex = CurrentPartIndex - 1;
                }
                // If deleted part was after current part, CurrentPartIndex stays the same

                // Notify that CurrentPart has changed
                RaisePropertyChangedForDataGridSection();
                RaisePropertyChangedForMultiPartSection();
                RaisePropertyChangedForMultiPolygonSection();
            }
        });

    private RelayCommand? _goToNextPartCommand;
    public RelayCommand GoToNextPartCommand =>
        _goToNextPartCommand ??= new RelayCommand(param =>
        {
            if (TotalPartCount == 0)
                return;

            CurrentPartIndex = (CurrentPartIndex < TotalPartCount - 1) ? CurrentPartIndex + 1 : 0;

            GoToPoint(0);

        }, param => IsNextPartAvailable);

    private RelayCommand? _goToPreviousPartCommand;
    public RelayCommand GoToPreviousPartCommand =>
        _goToPreviousPartCommand ??= new RelayCommand(param =>
        {
            if (TotalPartCount == 0)
                return;

            CurrentPartIndex = (CurrentPartIndex > 0) ? CurrentPartIndex - 1 : TotalPartCount - 1;

            GoToPoint(0);

        }, param => IsPreviousPartAvailable);

    private RelayCommand? _zoomToCurrentPartCommand;
    public RelayCommand ZoomToCurrentPartCommand =>
        _zoomToCurrentPartCommand ??= new RelayCommand(param =>
        {
            if (CurrentPart != null)
            {
                RequestZoomToGeometry?.Invoke(CurrentPart);
            }
        }, param => CurrentPart != null);

    #endregion


    #region Polygon Commands

    private RelayCommand? _addPolygonCommand;
    public RelayCommand AddPolygonCommand =>
        _addPolygonCommand ??= new RelayCommand(param =>
        {
            if (FeatureLayer == null)
                return;

            if (!IsRingBase)
                return;

            // Add a new empty part - this will trigger ReconstructLocateables()
            bool success = FeatureLayer.TryAddNewPart();

            if (success)
            {
                // Wait for the geometry structure to be updated
                // The LocateablesReconstructed event will fire and refresh Points
                // After that, we can safely add a point to the new part

                int newPolygonIndex = TotalPolygonCount - 1;
                // Add a point (0,0) to the new part
                // AddVertexToPart will directly manipulate the geometry and trigger ReconstructLocateables()
                var newPointLocatable = FeatureLayer.AddVertexToPart(GetNewPoint(), newPolygonIndex, CurrentPartIndex);

                if (newPointLocatable != null)
                {
                    // Navigate to the new part to show it in the DataGrid
                    // (Required to select the new point in the DataGrid)
                    CurrentPolygonIndex = newPolygonIndex;

                    // After CurrentPartIndex changes, RefreshPointsFromCurrentPart() is called
                    // Wait a moment for the Points collection to refresh, then select the new point
                    var refreshedPoints = FeatureLayer.GetLocateablesForPart(newPolygonIndex, CurrentPartIndex);
                    if (refreshedPoints.Count > 0)
                    {
                        // Select the newly added point (first point in the new part)
                        SelectedPoint = refreshedPoints[0];
                    }
                }

                RaisePropertyChangedForDataGridSection();
                RaisePropertyChangedForMultiPartSection();
                RaisePropertyChangedForMultiPolygonSection();
            }

            //GeometryChanged?.Invoke(Parts);
        });


    private RelayCommand? _deletePolygonCommand;
    public RelayCommand DeletePolygonCommand =>
        _deletePolygonCommand ??= new RelayCommand(param =>
        {
            if (FeatureLayer == null)
                return;

            int indexToDelete = -1;

            // Extract part index from parameter
            if (param is IGeometry part)
            {
                indexToDelete = Parts?.IndexOf(part) ?? -1;
            }
            else if (param is int partIndex)
            {
                indexToDelete = partIndex;
            }

            if (HasNoParts || indexToDelete < 0 || indexToDelete >= TotalPartCount)
                return;

            // Store state before deletion
            int oldPartCount = TotalPartCount;
            bool wasCurrentPart = indexToDelete == CurrentPartIndex;
            bool wasLastPart = indexToDelete == TotalPartCount - 1;

            // Delete the part from the geometry - this will trigger ReconstructLocateables()
            bool success = FeatureLayer.TryDeletePartByIndex(indexToDelete);

            if (success)
            {
                // After LocateablesReconstructed event fires and refreshes Points,
                // update CurrentPartIndex appropriately
                int newPartCount = TotalPartCount;

                if (newPartCount == 0)
                {
                    // All parts deleted
                    CurrentPartIndex = 0;
                }
                else if (wasCurrentPart)
                {
                    // Deleted the current part
                    if (wasLastPart && CurrentPartIndex > 0)
                    {
                        // Was last part, move to previous part
                        CurrentPartIndex = CurrentPartIndex - 1;
                    }
                    else if (!wasLastPart)
                    {
                        // Was not last part, stay at same index (which now points to next part)
                        CurrentPartIndex = CurrentPartIndex;
                    }
                    else
                    {
                        // Was last part but it was the only part, set to 0
                        CurrentPartIndex = 0;
                    }
                }
                else if (indexToDelete < CurrentPartIndex)
                {
                    // Deleted part was before current part, decrement index
                    CurrentPartIndex = CurrentPartIndex - 1;
                }
                // If deleted part was after current part, CurrentPartIndex stays the same

                // Notify that CurrentPart has changed
                RaisePropertyChangedForDataGridSection();
                RaisePropertyChangedForMultiPartSection();
            }
        });


    private RelayCommand? _goToNextPolygonCommand;
    public RelayCommand GoToNextPolygonCommand =>
        _goToNextPolygonCommand ??= new RelayCommand(param =>
        {
            if (Polygons.IsNullOrEmpty())
                return;

            CurrentPolygonIndex = (CurrentPolygonIndex < TotalPolygonCount - 1) ? CurrentPolygonIndex + 1 : 0;

        }, param => IsNextPolygonAvailable);


    private RelayCommand? _goToPreviousPolygonCommand;
    public RelayCommand GoToPreviousPolygonCommand =>
        _goToPreviousPolygonCommand ??= new RelayCommand(param =>
        {
            if (Polygons.IsNullOrEmpty())
                return;

            CurrentPolygonIndex = (CurrentPolygonIndex > 0) ? CurrentPolygonIndex - 1 : TotalPolygonCount - 1;

        }, param => IsPreviousPolygonAvailable);


    private RelayCommand? _zoomToCurrentPolygonCommand;
    public RelayCommand ZoomToCurrentPolygonCommand =>
        _zoomToCurrentPolygonCommand ??= new RelayCommand(param =>
        {
            if (CurrentPolygon != null)
            {
                RequestZoomToGeometry?.Invoke(CurrentPolygon);
            }
        }, param => CurrentPolygon != null);



    #endregion
}
