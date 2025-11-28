using IRI.Maptor.Jab.Common.Assets.Commands;
using IRI.Maptor.Jab.Common.Models;
using IRI.Maptor.Jab.Common.Models.CoordinateEditor;
using IRI.Maptor.Sta.Spatial.Primitives;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace IRI.Maptor.Jab.Common.Presenters.CoordinateEditor;

public abstract class GeometryEditorViewModel : Notifier
{
    private EditableFeatureLayer _featureLayer;
    public EditableFeatureLayer FeatureLayer
    {
        get { return _featureLayer; }
        set
        {
            _featureLayer = value;
            RaisePropertyChanged();
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
            if (Points == null || Points.Count == 0)
                return new ObservableCollection<Locateable>();

            var startIndex = CurrentPageIndex * MaxPointsPerPage;

            var count = Math.Min(MaxPointsPerPage, Points.Count - startIndex);

            return new ObservableCollection<Locateable>(Points.Skip(startIndex).Take(count));
        }
    }

    private Locateable? _selectedPoint;
    public Locateable? SelectedPoint
    {
        get
        {
            return _selectedPoint;
        }
        set
        {
            _selectedPoint = value;

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(CurrentPointIndex));
            RaisePropertyChanged(nameof(CurrentPointNumber));
            RaisePropertyChanged(nameof(IsPreviousPointAvailable));
            RaisePropertyChanged(nameof(IsNextPointAvailable));
            // Update command states
            CommandManager.InvalidateRequerySuggested();
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
            return Points == null || Points.Count == 0 ? 0 : (int)Math.Ceiling(Points.Count / (double)MaxPointsPerPage);
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
            if (SelectedPoint == null || Points == null || Points.Count == 0)
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
            if (SelectedPoint == null || Points == null || Points.Count == 0)
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
        if (Points == null || Points.Count == 0)
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

    public event Action<ObservableCollection<Locateable>>? PointsChanged;

    #endregion


    #region Parts

    private List<IGeometry> Parts => this.FeatureLayer?.GetFinalGeometry()?.Geometries?.Cast<IGeometry>().ToList();
    // Multi-line string functionality (merged from MultiLineStringEditorPresenter)
    //private ObservableCollection<LineStringEditorPresenter> _parts = new ObservableCollection<LineStringEditorPresenter>();
    //public ObservableCollection<LineStringEditorPresenter> Parts
    //{
    //    get => _parts;
    //    set
    //    {
    //        if (_parts != null)
    //        {
    //            _parts.CollectionChanged -= Parts_CollectionChanged;
    //        }
    //        _parts = value ?? new ObservableCollection<LineStringEditorPresenter>();
    //        if (_parts != null)
    //        {
    //            _parts.CollectionChanged += Parts_CollectionChanged;
    //        }
    //        RaisePropertyChanged();
    //        RaisePropertyChanged(nameof(TotalPartCount));
    //        RaisePropertyChanged(nameof(CurrentPart));
    //        RaisePropertyChanged(nameof(CurrentPartNumber));
    //        RaisePropertyChanged(nameof(IsNextPartAvailable));
    //        RaisePropertyChanged(nameof(IsPreviousPartAvailable));
    //        AdjustCurrentPartIndex();
    //    }
    //}

    private int _currentPartIndex = 0;
    public int CurrentPartIndex
    {
        get => _currentPartIndex;
        set
        {
            if (value < 0 || (FeatureLayer.GetFinalGeometry()?.Geometries != null && Parts.Count > 0 && value >= Parts.Count))
                return;

            _currentPartIndex = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(CurrentPart));
            RaisePropertyChanged(nameof(CurrentPartNumber));
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

    //private IGeometry? _previousCurrentPart;

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

    //private void CurrentPart_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    //{
    //    // Forward property change notifications from CurrentPart
    //    if (e.PropertyName == nameof(CurrentPagePoints) ||
    //        e.PropertyName == nameof(SelectedPoint) ||
    //        e.PropertyName == nameof(TotalPages) ||
    //        e.PropertyName == nameof(CurrentPageNumber) ||
    //        e.PropertyName == nameof(TotalPointCount) ||
    //        e.PropertyName == nameof(CurrentPointIndex) ||
    //        e.PropertyName == nameof(CurrentPointNumber) ||
    //        e.PropertyName == nameof(IsPreviousPointAvailable) ||
    //        e.PropertyName == nameof(IsNextPointAvailable) ||
    //        e.PropertyName == nameof(IsLastPage) ||
    //        e.PropertyName == nameof(IsEditable) ||
    //        e.PropertyName == nameof(CurrentPageIndex))
    //    {
    //        RaisePropertyChanged(e.PropertyName);
    //    }
    //}

    public int CurrentPartNumber => CurrentPartIndex + 1;

    public int TotalPartCount => Parts?.Count ?? 0;

    public bool IsNextPartAvailable => Parts != null && Parts.Count > 0;

    public bool IsPreviousPartAvailable => Parts != null && Parts.Count > 0;

    protected void Parts_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        //GeometryChanged?.Invoke(Parts);
        RaisePropertyChanged(nameof(TotalPartCount));
        RaisePropertyChanged(nameof(CurrentPart));
        RaisePropertyChanged(nameof(CurrentPartNumber));
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
    }

    //public event Action<ObservableCollection<LineStringEditorPresenter>>? GeometryChanged;

    //public event Action<LineStringEditorPresenter>? RequestZoomToPart;


    #endregion


    private RelayCommand _addPointCommand;
    public RelayCommand AddPointCommand =>
        _addPointCommand ??= new RelayCommand(param =>
        {
            //// If in multi-line mode, delegate to CurrentPart
            //if (Parts != null && Parts.Count > 0 && CurrentPart != null)
            //{
            //    CurrentPart.AddPointCommand.Execute(param);
            //    return;
            //}

            //var newPoint = new Locateable { X = 0, Y = 0 };
            //Points.Add(newPoint);

            //// Move to last page if new point is added
            //if (TotalPages > 0)
            //{
            //    CurrentPageIndex = TotalPages - 1;
            //}

            //// Select the newly added point
            //SelectedPoint = newPoint;
             
            var locatable = FeatureLayer.AddVertex(new Sta.Common.Primitives.Point(0, 0));

            if (locatable is null)
                return;

            Points.Add(locatable);

            // Move to last page if new point is added
            if (TotalPages > 0)
            {
                CurrentPageIndex = TotalPages - 1;
            }

            SelectedPoint = locatable;

        }, param => IsLastPage && !HasInvalidPoints);


    private RelayCommand _deletePointCommand;
    public RelayCommand DeletePointCommand =>
        _deletePointCommand ??= new RelayCommand(param =>
        {
            //// If in multi-line mode, delegate to CurrentPart
            //if (Parts != null && Parts.Count > 0 && CurrentPart != null)
            //{
            //    CurrentPart.DeletePointCommand.Execute(param);
            //    return;
            //}

            if (param is Locateable point)
            {
                int indexToDelete = Points.IndexOf(point);
                if (indexToDelete < 0)
                    return;

                int pageOfDeletedPoint = indexToDelete / MaxPointsPerPage;
                bool wasOnCurrentPage = pageOfDeletedPoint == CurrentPageIndex;

                Points.Remove(point);

                // Adjust page if needed
                if (Points.Count == 0)
                {
                    CurrentPageIndex = 0;
                }
                else if (wasOnCurrentPage && CurrentPagePoints.Count == 0 && CurrentPageIndex > 0)
                {
                    CurrentPageIndex--;
                }
                else if (TotalPages > 0 && CurrentPageIndex >= TotalPages)
                {
                    CurrentPageIndex = TotalPages - 1;
                }

                PointsChanged?.Invoke(Points);

                FeatureLayer.TryDeleteCurrentPoint();

                SelectedPoint = null;
            }
        });

    //private RelayCommand _deleteCurrentPointCommand;
    //public RelayCommand DeleteCurrentPointCommand =>
    //    _deleteCurrentPointCommand ??= new RelayCommand(param =>
    //    {
    //        if (SelectedPoint != null)
    //        {
    //            DeletePointCommand.Execute(SelectedPoint);
    //            SelectedPoint = null;
    //        }
    //    }, param => SelectedPoint != null);


    private RelayCommand _goToNextPageCommand;
    public RelayCommand GoToNextPageCommand =>
        _goToNextPageCommand ??= new RelayCommand(param =>
        {
            if (CurrentPageIndex < TotalPages - 1)
            {
                CurrentPageIndex++;
            }
        }, param => CurrentPageIndex < TotalPages - 1 && !HasInvalidPoints);


    private RelayCommand _goToPreviousPageCommand;
    public RelayCommand GoToPreviousPageCommand =>
        _goToPreviousPageCommand ??= new RelayCommand(param =>
        {
            if (CurrentPageIndex > 0)
            {
                CurrentPageIndex--;
            }
        }, param => CurrentPageIndex > 0 && !HasInvalidPoints);


    public event Action<Locateable>? RequestPanToPoint;

    public event Action<Locateable>? RequestZoomToPoint;

    public event Action<Locateable>? RequestCopyCoordinate;

    private RelayCommand _panToCurrentPointCommand;
    public RelayCommand PanToCurrentPointCommand =>
        _panToCurrentPointCommand ??= new RelayCommand(param =>
        {
            if (SelectedPoint != null)
            {
                RequestPanToPoint?.Invoke(SelectedPoint);
            }
        }, param => SelectedPoint != null && !HasInvalidPoints);

    private RelayCommand _goToPreviousPointCommand;
    public RelayCommand GoToPreviousPointCommand =>
        _goToPreviousPointCommand ??= new RelayCommand(param =>
        {
            if (SelectedPoint == null || Points == null || Points.Count == 0)
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

    private RelayCommand _goToNextPointCommand;
    public RelayCommand GoToNextPointCommand =>
        _goToNextPointCommand ??= new RelayCommand(param =>
        {
            if (SelectedPoint == null || Points == null || Points.Count == 0)
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

    private RelayCommand _zoomToCurrentPointCommand;
    public RelayCommand ZoomToCurrentPointCommand =>
        _zoomToCurrentPointCommand ??= new RelayCommand(param =>
        {
            if (SelectedPoint != null && IsPointValid(SelectedPoint))
            {
                RequestZoomToPoint?.Invoke(SelectedPoint);
            }
        }, param => SelectedPoint != null && IsPointValid(SelectedPoint) && !HasInvalidPoints);

    private RelayCommand _copyCurrentPointCommand;
    public RelayCommand CopyCurrentPointCommand =>
        _copyCurrentPointCommand ??= new RelayCommand(param =>
        {
            if (SelectedPoint != null)
            {
                RequestCopyCoordinate?.Invoke(SelectedPoint);
            }
        }, param => SelectedPoint != null);


    private RelayCommand _insertPointBeforeSelectedCommand;
    public RelayCommand InsertPointBeforeSelectedCommand =>
        _insertPointBeforeSelectedCommand ??= new RelayCommand(param =>
        {
            //// If in multi-line mode, delegate to CurrentPart
            //if (Parts != null && Parts.Count > 0 && CurrentPart != null)
            //{
            //    CurrentPart.InsertPointBeforeSelectedCommand.Execute(param);
            //    return;
            //}

            if (SelectedPoint == null || Points == null)
                return;

            int selectedIndex = Points.IndexOf(SelectedPoint);
            if (selectedIndex < 0)
                return;

            var newPoint = new Locateable { X = 0, Y = 0 };
            Points.Insert(selectedIndex, newPoint);

            // Calculate which page the new point is on
            int pageOfNewPoint = selectedIndex / MaxPointsPerPage;
            if (pageOfNewPoint != CurrentPageIndex)
            {
                CurrentPageIndex = pageOfNewPoint;
            }

            // Select the newly inserted point
            SelectedPoint = newPoint;

            PointsChanged?.Invoke(Points);
        }, param => SelectedPoint != null && !HasInvalidPoints);

    // Multi-line string commands (merged from MultiLineStringEditorPresenter)
    private RelayCommand _addPartCommand;
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
            FeatureLayer.StartNewPart(new Sta.Common.Primitives.Point(0, 0));

            CurrentPartIndex = Parts.Count - 1;

            // Notify that CurrentPart has changed
            RaisePropertyChanged(nameof(CurrentPart));
            RaisePropertyChanged(nameof(CurrentPagePoints));
            RaisePropertyChanged(nameof(SelectedPoint));
            RaisePropertyChanged(nameof(TotalPages));
            RaisePropertyChanged(nameof(CurrentPageNumber));
            RaisePropertyChanged(nameof(TotalPointCount));

            //GeometryChanged?.Invoke(Parts);
        });

    private RelayCommand _deletePartCommand;
    public RelayCommand DeletePartCommand =>
        _deletePartCommand ??= new RelayCommand(param =>
        {
            if (param is IGeometry part)
            {
                int indexToDelete = Parts.IndexOf(part);
                if (indexToDelete < 0)
                    return;

                bool wasCurrentPart = indexToDelete == CurrentPartIndex;
                bool wasLastPart = indexToDelete == Parts.Count - 1;

                Parts.Remove(part);

                if (Parts.Count == 0)
                {
                    CurrentPartIndex = 0;
                }
                else if (wasCurrentPart)
                {
                    if (wasLastPart && CurrentPartIndex > 0)
                    {
                        CurrentPartIndex = CurrentPartIndex - 1;
                    }
                    else if (!wasLastPart)
                    {
                        CurrentPartIndex = CurrentPartIndex;
                    }
                }
                else if (indexToDelete < CurrentPartIndex)
                {
                    CurrentPartIndex = CurrentPartIndex - 1;
                }

                //GeometryChanged?.Invoke(Parts);
            }
        });

    private RelayCommand _goToNextPartCommand;
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

    private RelayCommand _goToPreviousPartCommand;
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

    private RelayCommand _zoomToCurrentPartCommand;
    public RelayCommand ZoomToCurrentPartCommand =>
        _zoomToCurrentPartCommand ??= new RelayCommand(param =>
        {
            if (CurrentPart != null)
            {
                //RequestZoomToPart?.Invoke(CurrentPart);
            }
        }, param => CurrentPart != null);

}
