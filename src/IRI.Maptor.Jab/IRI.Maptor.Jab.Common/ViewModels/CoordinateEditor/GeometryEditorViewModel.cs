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


    private bool _isEditable = false;
    public bool IsEditable
    {
        get
        {
            //// If in multi-line mode, use CurrentPart's IsEditable
            //if (Parts != null && Parts.Count > 0 && CurrentPart != null)
            //{
            //    return CurrentPart.IsEditable;
            //}
            return _isEditable;
        }
        set
        {
            //// If in multi-line mode, set CurrentPart's IsEditable
            //if (Parts != null && Parts.Count > 0 && CurrentPart != null)
            //{
            //    CurrentPart.IsEditable = value;
            //}
            //else
            //{
            _isEditable = value;
            //}
            RaisePropertyChanged();
        }
    }


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

    public bool IsEmptyGeometry => this.Points is null || this.Points.Count == 0;

    public GeometryEditorViewModel(EditableFeatureLayer editableFeatureLayer)
    {
        this.FeatureLayer = editableFeatureLayer;

        // Initialize CurrentPartIndex to 0 (first part)
        // This will trigger RefreshPointsFromCurrentPart() which initializes Points collection
        // Setting it explicitly ensures initialization happens
        CurrentPartIndex = 0;

        this.IsEditable = true;

        // Points are already initialized by RefreshPointsFromCurrentPart() called in CurrentPartIndex setter
        // which also subscribes to PropertyChanged events
        // UpdateValidationState is called by RefreshPointsFromCurrentPart via UpdatePagingProperties
        UpdateValidationState();
    }

    #region SRS Support

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

        var geometry = FeatureLayer.GetFinalGeometry();
        return geometry?.HasZ() ?? false;
    }

    /// <summary>
    /// Checks if the geometry has M coordinates
    /// </summary>
    public bool HasM()
    {
        if (FeatureLayer == null)
            return false;

        var geometry = FeatureLayer.GetFinalGeometry();
        return geometry?.HasM() ?? false;
    }

    #endregion

    #region Parts

    private List<IGeometry> Parts => this.FeatureLayer?.GetFinalGeometry()?.Geometries?.Cast<IGeometry>().ToList();

    public IGeometry? CurrentPart
    {
        get
        {
            if (Parts == null || Parts.Count == 0 || CurrentPartIndex < 0 || CurrentPartIndex >= Parts.Count)
                return null;

            var currentPart = Parts[CurrentPartIndex];

            //// Subscribe to property changes when CurrentPart changes
            //if (currentPart != _previousCurrentPart)
            //{
            //    if (_previousCurrentPart != null)
            //    {
            //        _previousCurrentPart.PropertyChanged -= CurrentPart_PropertyChanged;
            //    }
            //    if (currentPart != null)
            //    {
            //        currentPart.PropertyChanged += CurrentPart_PropertyChanged;
            //    }
            //    _previousCurrentPart = currentPart;
            //}

            return currentPart;
        }
    }

    private int _currentPartIndex = 0;
    public int CurrentPartIndex
    {
        get => _currentPartIndex;
        set
        {
            if (value < 0 || (FeatureLayer.GetFinalGeometry()?.Geometries != null && Parts.Count > 0 && value >= Parts.Count))
                return;

            _currentPartIndex = value;

            // Refresh Points collection from the current part
            RefreshPointsFromCurrentPart();

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(CurrentPart));
            RaisePropertyChanged(nameof(CurrentPartNumber));
            RaisePropertyChanged(nameof(CurrentPartIsValid));
            RaisePropertyChanged(nameof(IsNextPartAvailable));
            RaisePropertyChanged(nameof(IsPreviousPartAvailable));

            // Notify that all CurrentPart-dependent properties have changed
            RaisePropertyChanged(nameof(CurrentPagePoints));
            RaisePropertyChanged(nameof(SelectedPoint));
            RaisePropertyChanged(nameof(TotalPages));
            RaisePropertyChanged(nameof(CurrentPageNumber));
            RaisePropertyChanged(nameof(TotalPointCount));
            RaisePropertyChanged(nameof(CurrentPointIndex));
            RaisePropertyChanged(nameof(CurrentPointNumber));
            RaisePropertyChanged(nameof(IsPreviousPointAvailable));
            RaisePropertyChanged(nameof(IsNextPointAvailable));
            RaisePropertyChanged(nameof(IsLastPage));
            RaisePropertyChanged(nameof(IsEditable));
            RaisePropertyChanged(nameof(CurrentPageIndex));
        }
    }

    public int CurrentPartNumber => CurrentPartIndex + 1;

    public int TotalPartCount => Parts?.Count ?? 0;

    public bool IsNextPartAvailable => Parts != null && Parts.Count > 0;

    public bool IsPreviousPartAvailable => Parts != null && Parts.Count > 0;

    public bool CurrentPartIsValid
    {
        get
        {
            if (CurrentPart == null)
                return false;

            // For LineString, need at least 2 points
            if (CurrentPart is IGeometry geometry)
            {
                return geometry.IsValid();
            }

            return false;
        }
    }

    protected void Parts_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        //GeometryChanged?.Invoke(Parts);
        RaisePropertyChanged(nameof(TotalPartCount));
        RaisePropertyChanged(nameof(CurrentPart));
        RaisePropertyChanged(nameof(CurrentPartNumber));
        RaisePropertyChanged(nameof(CurrentPartIsValid));
        RaisePropertyChanged(nameof(IsNextPartAvailable));
        RaisePropertyChanged(nameof(IsPreviousPartAvailable));
        AdjustCurrentPartIndex();
    }

    private void AdjustCurrentPartIndex()
    {
        if (Parts == null || Parts.Count == 0)
        {
            if (_currentPartIndex != 0)
            {
                _currentPartIndex = 0;
                RaisePropertyChanged(nameof(CurrentPartIndex));
            }
        }
        else if (_currentPartIndex >= Parts.Count)
        {
            _currentPartIndex = Parts.Count - 1;
            RaisePropertyChanged(nameof(CurrentPartIndex));
        }
        RaisePropertyChanged(nameof(CurrentPart));
        RaisePropertyChanged(nameof(CurrentPartIsValid));
    }

    //public event Action<ObservableCollection<LineStringEditorPresenter>>? GeometryChanged;

    //public event Action<LineStringEditorPresenter>? RequestZoomToPart;


    #endregion


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

            var startIndex = CurrentPageIndex * MaxPointsPerPage;

            var count = Math.Min(MaxPointsPerPage, Points.Count - startIndex);

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

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(CurrentPointIndex));
            RaisePropertyChanged(nameof(CurrentPointNumber));
            RaisePropertyChanged(nameof(IsPreviousPointAvailable));
            RaisePropertyChanged(nameof(IsNextPointAvailable));
            // Update command states
            CommandManager.InvalidateRequerySuggested();

            // Calculate global index from current part index and local point index
            if (_selectedPoint != null && CurrentPointIndex >= 0)
            {
                int globalIndex = GetGlobalIndex(CurrentPartIndex, CurrentPointIndex);
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

    private int _maxPointsPerPage = 10;
    public int MaxPointsPerPage
    {
        get => _maxPointsPerPage;
        set
        {
            _maxPointsPerPage = value;
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

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(CurrentPageNumber));
            RaisePropertyChanged(nameof(CurrentPagePoints));
            RaisePropertyChanged(nameof(PointsOnCurrentPage));
            RaisePropertyChanged(nameof(IsLastPage));
        }
    }

    public int TotalPages
    {
        get
        {
            //// If in multi-line mode, use CurrentPart's TotalPages
            //if (Parts != null && Parts.Count > 0 && CurrentPart != null)
            //{
            //    return CurrentPart.TotalPages;
            //}
            return IsEmptyGeometry ? 0 : (int)Math.Ceiling(Points.Count / (double)MaxPointsPerPage);
        }
    }

    public int CurrentPointIndex
    {
        get
        {
            //// If in multi-line mode, use CurrentPart's CurrentPointIndex
            //if (Parts != null && Parts.Count > 0 && CurrentPart != null)
            //{
            //    return CurrentPart.CurrentPointIndex;
            //}
            if (SelectedPoint == null || Points == null)
                return -1;

            return Points.IndexOf(SelectedPoint);
        }
    }

    public int CurrentPointNumber => CurrentPointIndex >= 0 ? CurrentPointIndex + 1 : 0;

    public int CurrentPageNumber
    {
        get
        {
            //// If in multi-line mode, use CurrentPart's CurrentPageNumber
            //if (Parts != null && Parts.Count > 0 && CurrentPart != null)
            //{
            //    return CurrentPart.CurrentPageNumber;
            //}
            return CurrentPageIndex + 1;
        }
    }

    public int TotalPointCount
    {
        get
        {
            //// If in multi-line mode, use CurrentPart's TotalPointCount
            //if (Parts != null && Parts.Count > 0 && CurrentPart != null)
            //{
            //    return CurrentPart.TotalPointCount;
            //}
            return Points?.Count ?? 0;
        }
    }

    public int PointsOnCurrentPage => CurrentPagePoints.Count;

    public bool IsLastPage
    {
        get
        {
            //// If in multi-line mode, use CurrentPart's IsLastPage
            //if (Parts != null && Parts.Count > 0 && CurrentPart != null)
            //{
            //    return CurrentPart.IsLastPage;
            //}
            return TotalPages > 0 && CurrentPageIndex >= TotalPages - 1;
        }
    }

    public bool IsPreviousPointAvailable
    {
        get
        {
            //// If in multi-line mode, use CurrentPart's IsPreviousPointAvailable
            //if (Parts != null && Parts.Count > 0 && CurrentPart != null)
            //{
            //    return CurrentPart.IsPreviousPointAvailable;
            //}
            if (SelectedPoint == null || IsEmptyGeometry)
                return false;

            return CurrentPointIndex > 0;
        }
    }

    public bool IsNextPointAvailable
    {
        get
        {
            //// If in multi-line mode, use CurrentPart's IsNextPointAvailable
            //if (Parts != null && Parts.Count > 0 && CurrentPart != null)
            //{
            //    return CurrentPart.IsNextPointAvailable;
            //}
            if (SelectedPoint == null || IsEmptyGeometry)
                return false;

            return CurrentPointIndex >= 0 && CurrentPointIndex < Points.Count - 1;
        }
    }

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

        if (Parts.IsNullOrEmpty())
        {
            // Single-part geometry: index maps directly to Points collection
            if (index < 0 || index >= Points.Count)
                return;

            // Navigate to correct page
            int pageIndex = index / MaxPointsPerPage;
            if (pageIndex != CurrentPageIndex)
            {
                CurrentPageIndex = pageIndex;
            }

            // Update the selected point and its coordinates

            this.SelectedPoint = Points[index];

            if (this.SelectedPoint != null)
            {
                this.SelectedPoint.X = l.X;
                this.SelectedPoint.Y = l.Y;

                // Update CurrentPointEditor when coordinates change
                //_currentPointEditor?.UpdateFromSelectedPoint();
                _currentPointEditor.CurrentPoint = this.SelectedPoint;
            }
        }
        else
        {
            // Multi-part geometry: find which part contains this global index
            var (partIndex, localIndex) = GetPartAndLocalIndex(index);

            if (partIndex < 0 && localIndex < 0)
                return;

            // Switch to the correct part if needed
            if (partIndex != CurrentPartIndex)
                CurrentPartIndex = partIndex;

            // Ensure Points collection is up to date
            if (localIndex < 0 || localIndex >= Points.Count)
                return;

            // Navigate to correct page
            int pageIndex = localIndex / MaxPointsPerPage;
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
    }

    public event Action<ObservableCollection<Locateable>>? PointsChanged;

    /// <summary>
    /// Refreshes the Points collection from the current part's Locateable objects
    /// Uses the same Locateable instances from EditableFeatureLayer so changes on the map update the DataGrid
    /// </summary>
    /// <param name="preservePageAndSelection">If true, preserves the current page index and selection when possible</param>
    private void RefreshPointsFromCurrentPart(bool preservePageAndSelection = false)
    {
        if (FeatureLayer == null)
            return;

        var geometry = FeatureLayer.GetFinalGeometry();
        if (geometry == null)
            return;

        // Preserve current selection and page before refreshing
        Locateable? previousSelectedPoint = SelectedPoint;
        int? previousGlobalIndex = null;
        int previousPageIndex = CurrentPageIndex;

        if (previousSelectedPoint != null && Points != null)
        {
            int localIndex = Points.IndexOf(previousSelectedPoint);
            if (localIndex >= 0)
            {
                previousGlobalIndex = GetGlobalIndex(CurrentPartIndex, localIndex);
            }
        }

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
        List<Locateable> newPoints = FeatureLayer.GetLocateablesForPart(CurrentPartIndex);

        // Subscribe to new points' PropertyChanged events
        foreach (var point in newPoints)
        {
            point.PropertyChanged += Point_PropertyChanged;
        }

        // Update Points collection (this will trigger CollectionChanged)
        Points = new ObservableCollection<Locateable>(newPoints);

        if (preservePageAndSelection && previousGlobalIndex.HasValue)
        {
            // Restore selection and page if possible
            var (partIndex, localIndex) = GetPartAndLocalIndex(previousGlobalIndex.Value);
            if (partIndex == CurrentPartIndex && localIndex >= 0 && localIndex < newPoints.Count)
            {
                SelectedPoint = newPoints[localIndex];
                // Adjust page to show the selected point
                int pageOfSelectedPoint = localIndex / MaxPointsPerPage;
                CurrentPageIndex = pageOfSelectedPoint;
            }
            else
            {
                SelectedPoint = null;
                // Try to preserve page index if valid
                if (previousPageIndex >= 0 && previousPageIndex < TotalPages)
                {
                    CurrentPageIndex = previousPageIndex;
                }
                else
                {
                    CurrentPageIndex = 0;
                }
            }
        }
        else
        {
            // Reset page to first page when switching parts
            CurrentPageIndex = 0;
            SelectedPoint = null;
        }
    }

    /// <summary>
    /// Handles the LocateablesReconstructed event from FeatureLayer
    /// Refreshes the Points collection when Locateables are reconstructed (e.g., after add/remove operations)
    /// </summary>
    private void FeatureLayer_LocateablesReconstructed()
    {
        // Validate and adjust CurrentPartIndex if needed
        int partCount = Parts?.Count ?? 0;
        if (partCount == 0)
        {
            // No parts left, reset to 0
            if (_currentPartIndex != 0)
            {
                _currentPartIndex = 0;
                RaisePropertyChanged(nameof(CurrentPartIndex));
            }
        }
        else if (_currentPartIndex >= partCount)
        {
            // CurrentPartIndex is out of bounds, adjust to last valid index
            _currentPartIndex = partCount - 1;
            RaisePropertyChanged(nameof(CurrentPartIndex));
            RaisePropertyChanged(nameof(CurrentPart));
            RaisePropertyChanged(nameof(CurrentPartIsValid));
        }

        // Preserve page and selection when refreshing due to reconstruction
        RefreshPointsFromCurrentPart(preservePageAndSelection: true);
    }

    /// <summary>
    /// Calculates the global index from a part index and local index within that part
    /// </summary>
    private int GetGlobalIndex(int partIndex, int localIndex)
    {
        if (Parts.IsNullOrEmpty())
        {
            // Single-part geometry: local index is the global index
            return localIndex;
        }

        if (partIndex < 0 || partIndex >= Parts.Count)
            return -1;

        // Sum points from all parts before the target part
        int globalIndex = 0;
        for (int i = 0; i < partIndex; i++)
        {
            if (Parts[i] is IGeometry part && part.NumberOfPoints > 0)
            {
                globalIndex += part.NumberOfPoints;
            }
        }

        // Add the local index within the target part
        return globalIndex + localIndex;
    }

    /// <summary>
    /// Finds which part contains the global index and returns the part index and local index within that part
    /// </summary>
    private (int partIndex, int localIndex) GetPartAndLocalIndex(int globalIndex)
    {
        if (globalIndex < 0)
            return (-1, -1);

        if (Parts.IsNullOrEmpty())
        {
            // Single-part geometry: global index is the local index, part index is 0
            return (0, globalIndex);
        }

        int accumulatedPoints = 0;
        for (int i = 0; i < Parts.Count; i++)
        {
            if (Parts[i] is IGeometry part && part.NumberOfPoints > 0)
            {
                int partPointCount = part.NumberOfPoints;

                if (globalIndex < accumulatedPoints + partPointCount)
                {
                    // Found the part containing this global index
                    int localIndex = globalIndex - accumulatedPoints;
                    return (i, localIndex);
                }

                accumulatedPoints += partPointCount;
            }
        }

        // Global index is out of range
        return (-1, -1);
    }

    #endregion


    public event Action<Locateable>? RequestPanToPoint;

    public event Action<Locateable>? RequestZoomToPoint;

    public event Action<Locateable>? RequestCopyCoordinate;


    #region Point Commands

    private RelayCommand? _addPointCommand;
    public RelayCommand AddPointCommand =>
        _addPointCommand ??= new RelayCommand(param =>
        {
            if (FeatureLayer == null)
                return;

            // Add point to the current part being viewed, not necessarily the last part
            var geometry = FeatureLayer.GetFinalGeometry();
            if (geometry == null)
                return;

            // Use AddVertexToPart to add to the current part
            var locatable = FeatureLayer.AddVertexToPart(new Sta.Common.Primitives.Point(0, 0), CurrentPartIndex);

            if (locatable is null)
                return;

            // AddVertexToPart calls ReconstructLocateables() which triggers LocateablesReconstructed event
            // The event handler will refresh Points collection
            // After refresh, select the newly added point
            var refreshedPoints = FeatureLayer.GetLocateablesForPart(CurrentPartIndex);
            if (refreshedPoints.Count > 0)
            {
                SelectedPoint = refreshedPoints[refreshedPoints.Count - 1]; // Last point (newly added)
            }

            // Move to last page if new point is added
            if (TotalPages > 0)
            {
                CurrentPageIndex = TotalPages - 1;
            }

        }, param => (IsLastPage || Points.Count == 0) && !HasInvalidPoints);


    private RelayCommand? _deletePointCommand;
    public RelayCommand DeletePointCommand =>
        _deletePointCommand ??= new RelayCommand(param =>
        {
            if (param is Locateable point && FeatureLayer != null)
            {
                int indexToDelete = Points.IndexOf(point);
                if (indexToDelete < 0)
                    return;

                // Calculate global index to select the point on the map before deleting
                int globalIndex = GetGlobalIndex(CurrentPartIndex, indexToDelete);

                // Select the point on the map so TryDeleteCurrentPoint can find it
                FeatureLayer.SelectPoint(globalIndex);

                // Delete the point from geometry
                // This will call ReconstructLocateables() which triggers LocateablesReconstructed event
                // The event handler will refresh Points collection
                FeatureLayer.TryDeleteCurrentPoint();

                // After reconstruction, Points collection is refreshed by event handler
                // Clear selection or select nearest remaining point
                if (Points != null && Points.Count > 0)
                {
                    // Select the point at the same index, or the previous one if at end
                    int newIndex = indexToDelete < Points.Count ? indexToDelete : Points.Count - 1;
                    if (newIndex >= 0)
                    {
                        SelectedPoint = Points[newIndex];

                        // Adjust page if needed
                        int pageOfNewPoint = newIndex / MaxPointsPerPage;
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
        });


    private RelayCommand? _deleteCurrentPointCommand;
    public RelayCommand DeleteCurrentPointCommand =>
        _deleteCurrentPointCommand ??= new RelayCommand(param =>
        {
            if (SelectedPoint != null && FeatureLayer != null)
            {
                // Delete the selected point
                // This will call ReconstructLocateables() which triggers LocateablesReconstructed event
                // The event handler will refresh Points collection
                DeletePointCommand.Execute(SelectedPoint);
            }
        }, param => SelectedPoint != null && FeatureLayer != null);


    private RelayCommand? _goToNextPageCommand;
    public RelayCommand GoToNextPageCommand =>
        _goToNextPageCommand ??= new RelayCommand(param =>
        {
            if (CurrentPageIndex < TotalPages - 1)
            {
                CurrentPageIndex++;
            }
        }, param => CurrentPageIndex < TotalPages - 1 && !HasInvalidPoints);


    private RelayCommand? _goToPreviousPageCommand;
    public RelayCommand GoToPreviousPageCommand =>
        _goToPreviousPageCommand ??= new RelayCommand(param =>
        {
            if (CurrentPageIndex > 0)
            {
                CurrentPageIndex--;
            }
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
            if (currentIndex <= 0)
                return;

            // Navigate to previous point
            Locateable? previousPoint = Points[currentIndex - 1];
            if (previousPoint == null)
                return;

            SelectedPoint = previousPoint;

            // Adjust page if needed
            int pageOfPreviousPoint = (currentIndex - 1) / MaxPointsPerPage;
            if (pageOfPreviousPoint != CurrentPageIndex)
            {
                CurrentPageIndex = pageOfPreviousPoint;
            }
        }, param => IsPreviousPointAvailable && !HasInvalidPoints);


    private RelayCommand? _goToNextPointCommand;
    public RelayCommand GoToNextPointCommand =>
        _goToNextPointCommand ??= new RelayCommand(param =>
        {
            if (SelectedPoint == null || IsEmptyGeometry)
                return;

            int currentIndex = Points.IndexOf(SelectedPoint);
            if (currentIndex < 0 || currentIndex >= Points.Count - 1)
                return;

            // Navigate to next point
            Locateable? nextPoint = Points[currentIndex + 1];
            if (nextPoint == null)
                return;

            SelectedPoint = nextPoint;

            // Adjust page if needed
            int pageOfNextPoint = (currentIndex + 1) / MaxPointsPerPage;
            if (pageOfNextPoint != CurrentPageIndex)
            {
                CurrentPageIndex = pageOfNextPoint;
            }
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


    private RelayCommand? _insertPointBeforeSelectedCommand;
    public RelayCommand InsertPointBeforeSelectedCommand =>
        _insertPointBeforeSelectedCommand ??= new RelayCommand(param =>
        {
            if (SelectedPoint == null || Points == null || FeatureLayer == null)
                return;

            int selectedIndex = Points.IndexOf(SelectedPoint);
            if (selectedIndex < 0)
                return;

            // Calculate global index from current part and local index
            int globalIndex = GetGlobalIndex(CurrentPartIndex, selectedIndex);

            // Insert point into geometry at the calculated global index
            // This will call ReconstructLocateables() which will trigger LocateablesReconstructed event
            // The event handler will refresh Points collection and preserve selection
            var newLocateable = FeatureLayer.InsertVertexAt(new Sta.Common.Primitives.Point(0, 0), globalIndex);

            if (newLocateable != null)
            {
                // After reconstruction, Points collection is refreshed by event handler
                // Find the newly inserted point and select it
                var refreshedPoints = FeatureLayer.GetLocateablesForPart(CurrentPartIndex);
                int newLocalIndex = selectedIndex; // Inserted before selected, so same index
                if (newLocalIndex >= 0 && newLocalIndex < refreshedPoints.Count)
                {
                    SelectedPoint = refreshedPoints[newLocalIndex];

                    // Calculate which page the new point is on
                    int pageOfNewPoint = newLocalIndex / MaxPointsPerPage;
                    if (pageOfNewPoint != CurrentPageIndex)
                    {
                        CurrentPageIndex = pageOfNewPoint;
                    }
                }
            }

            PointsChanged?.Invoke(Points);
        }, param => SelectedPoint != null && !HasInvalidPoints);

    #endregion


    #region Multi-part commands

    private RelayCommand? _addPartCommand;
    public RelayCommand AddPartCommand =>
        _addPartCommand ??= new RelayCommand(param =>
        {
            //// If this is the first part (converting from single-line to multi-line)
            //if (Parts.Count == 0 && Points != null && Points.Count > 0)
            //{
            //    // Move current Points to a new part
            //    var existingPoints = new ObservableCollection<Locateable>(Points);
            //    var firstPart = new LineStringEditorPresenter(existingPoints);
            //    Parts.Add(firstPart);

            //    // Clear the main Points collection
            //    Points.Clear();
            //}

            // Add a new empty part
            //var newPart = new IGeometry();
            //Parts.Add(newPart);
            if (FeatureLayer == null)
                return;

            // Store the current part count before adding
            int oldPartCount = Parts?.Count ?? 0;

            // Add a new empty part - this will trigger ReconstructLocateables()
            bool success = FeatureLayer.TryAddNewPart();

            if (success)
            {
                // Wait for the geometry structure to be updated
                // The LocateablesReconstructed event will fire and refresh Points
                // After that, we can safely add a point to the new part
                int newPartCount = Parts?.Count ?? 0;
                if (newPartCount > oldPartCount)
                {
                    int newPartIndex = newPartCount - 1;

                    // Verify the new part exists in the geometry before adding point
                    var geometry = FeatureLayer.GetFinalGeometry();
                    if (geometry?.Geometries != null && newPartIndex < geometry.Geometries.Count)
                    {
                        // Add a point (0,0) to the new part
                        // AddVertexToPart will directly manipulate the geometry and trigger ReconstructLocateables()
                        var newPointLocatable = FeatureLayer.AddVertexToPart(new Point(0, 0), newPartIndex);

                        if (newPointLocatable != null)
                        {
                            // Navigate to the new part to show it in the DataGrid
                            // (Required to select the new point in the DataGrid)
                            CurrentPartIndex = newPartIndex;

                            // After CurrentPartIndex changes, RefreshPointsFromCurrentPart() is called
                            // Wait a moment for the Points collection to refresh, then select the new point
                            var refreshedPoints = FeatureLayer.GetLocateablesForPart(newPartIndex);
                            if (refreshedPoints.Count > 0)
                            {
                                // Select the newly added point (first point in the new part)
                                SelectedPoint = refreshedPoints[0];

                                // Calculate which page the new point is on and navigate to it
                                int pageOfNewPoint = 0 / MaxPointsPerPage; // First point is on page 0
                                if (pageOfNewPoint != CurrentPageIndex)
                                {
                                    CurrentPageIndex = pageOfNewPoint;
                                }
                            }
                        }
                    }
                }

                // Notify that CurrentPart has changed
                RaisePropertyChanged(nameof(CurrentPart));
                RaisePropertyChanged(nameof(CurrentPartNumber));
                RaisePropertyChanged(nameof(CurrentPartIsValid));
                RaisePropertyChanged(nameof(TotalPartCount));
                RaisePropertyChanged(nameof(CurrentPagePoints));
                RaisePropertyChanged(nameof(SelectedPoint));
                RaisePropertyChanged(nameof(TotalPages));
                RaisePropertyChanged(nameof(CurrentPageNumber));
                RaisePropertyChanged(nameof(TotalPointCount));
                RaisePropertyChanged(nameof(IsNextPartAvailable));
                RaisePropertyChanged(nameof(IsPreviousPartAvailable));
            }

            //GeometryChanged?.Invoke(Parts);
        });

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

            if (indexToDelete < 0 || Parts == null || indexToDelete >= Parts.Count)
                return;

            // Store state before deletion
            int oldPartCount = Parts.Count;
            bool wasCurrentPart = indexToDelete == CurrentPartIndex;
            bool wasLastPart = indexToDelete == Parts.Count - 1;

            // Delete the part from the geometry - this will trigger ReconstructLocateables()
            bool success = FeatureLayer.TryDeletePartByIndex(indexToDelete);

            if (success)
            {
                // After LocateablesReconstructed event fires and refreshes Points,
                // update CurrentPartIndex appropriately
                int newPartCount = Parts?.Count ?? 0;

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
                RaisePropertyChanged(nameof(CurrentPart));
                RaisePropertyChanged(nameof(CurrentPartNumber));
                RaisePropertyChanged(nameof(CurrentPartIsValid));
                RaisePropertyChanged(nameof(TotalPartCount));
                RaisePropertyChanged(nameof(CurrentPagePoints));
                RaisePropertyChanged(nameof(SelectedPoint));
                RaisePropertyChanged(nameof(TotalPages));
                RaisePropertyChanged(nameof(CurrentPageNumber));
                RaisePropertyChanged(nameof(TotalPointCount));
                RaisePropertyChanged(nameof(IsNextPartAvailable));
                RaisePropertyChanged(nameof(IsPreviousPartAvailable));
            }
        });

    private RelayCommand? _goToNextPartCommand;
    public RelayCommand GoToNextPartCommand =>
        _goToNextPartCommand ??= new RelayCommand(param =>
        {
            if (Parts == null || Parts.Count == 0)
                return;

            if (CurrentPartIndex < Parts.Count - 1)
            {
                CurrentPartIndex++;
            }
            else
            {
                CurrentPartIndex = 0;
            }
        }, param => IsNextPartAvailable);

    private RelayCommand? _goToPreviousPartCommand;
    public RelayCommand GoToPreviousPartCommand =>
        _goToPreviousPartCommand ??= new RelayCommand(param =>
        {
            if (Parts == null || Parts.Count == 0)
                return;

            if (CurrentPartIndex > 0)
            {
                CurrentPartIndex--;
            }
            else
            {
                CurrentPartIndex = Parts.Count - 1;
            }
        }, param => IsPreviousPartAvailable);

    private RelayCommand? _zoomToCurrentPartCommand;
    public RelayCommand ZoomToCurrentPartCommand =>
        _zoomToCurrentPartCommand ??= new RelayCommand(param =>
        {
            if (CurrentPart != null)
            {
                //RequestZoomToPart?.Invoke(CurrentPart);
            }
        }, param => CurrentPart != null);

    #endregion
}
