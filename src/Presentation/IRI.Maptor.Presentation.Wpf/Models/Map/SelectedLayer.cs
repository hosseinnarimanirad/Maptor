using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Presentation.Wpf.Layers;
using IRI.Maptor.Core.Common.Exceptions;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Persistence.Abstractions;
using IRI.Maptor.Core.Persistence.DataSources;
using IRI.Maptor.Presentation.Wpf.Services;
using System.Windows.Input;
using IRI.Maptor.Presentation.Core.Layers;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Wpf.Models.Filters;

namespace IRI.Maptor.Presentation.Wpf.Models;

public class SelectedLayer : Notifier
{
    private IDialogService _dialogService;

    public Guid Id => AssociatedLayer?.LayerId ?? Guid.Empty;

    public VectorLayer AssociatedLayer { get; set; }

    public string LayerName => AssociatedLayer?.LayerName ?? string.Empty;

    public List<Field>? Fields { get; set; }

    private ObservableCollection<Feature<Point>> _features;
    public ObservableCollection<Feature<Point>> Features
    {
        get { return _features; }
        set
        {
            _features = value;
            RaisePropertyChanged();
            RefreshPagingView();
        }
    }


    private ObservableCollection<Feature<Point>> _highlightedFeatures = new ObservableCollection<Feature<Point>>();
    public ObservableCollection<Feature<Point>> HighlightedFeatures
    {
        get { return _highlightedFeatures; }
        set
        {
            if (_highlightedFeatures != null)
                _highlightedFeatures.CollectionChanged -= highlightedFeatures_CollectionChanged;

            _highlightedFeatures = value;
            RaisePropertyChanged();

            if (_highlightedFeatures != null)
                _highlightedFeatures.CollectionChanged += highlightedFeatures_CollectionChanged;

            RefreshHighlightedFeaturesOnMap(HighlightedFeatures);

            NotifyAll();
        }
    }

    public bool ShowSelectedOnMap { get; set; } = false;

    public bool IsSingleValueHighlighted => HighlightedFeatures?.Count == 1;

    public bool CanUndo
    {
        get
        {
            if (!IsSingleValueHighlighted || HighlightedFeatures?.FirstOrDefault() is not Feature<Point> feature)
                return false;

            return feature.Status == FeatureStatus.Updated && feature.OldVersion != null ||
                   feature.Status == FeatureStatus.New ||
                   feature.Status == FeatureStatus.Removed;
        }
    }

    public bool CanViewChanges
    {
        get
        {
            if (!IsSingleValueHighlighted || HighlightedFeatures?.FirstOrDefault() is not Feature<Point> feature)
                return false;

            return feature.Status == FeatureStatus.Updated && feature.OldVersion != null;
        }
    }

    public bool HasPendingChanges => AssociatedLayer.HasPendingChanges;

    public int CountOfSelectedFeatures => Features?.Count ?? 0;

    #region Actions and Funcs


    public Func<IEnumerable<Feature<Point>>, double?, Task>? RequestFeaturesChangedAsync { get; set; }

    public Func<IEnumerable<Feature<Point>>, double?, Task>? RequestHighlightFeaturesChangedAsync { get; set; }

    public Action<Feature<Point>>? RequestFlashSinglePoint { get; set; }

    public Action<IEnumerable<Feature<Point>>, Action>? RequestZoomTo { get; set; }

    public Func<Feature<Point>, Task>? RequestEditAsync { get; set; }

    public Action<Feature<Point>>? RequestViewChanges { get; set; }

    public Func<GeometryType, Task<Geometry<Point>?>>? RequestDrawAsync { get; set; }

    public Action? RequestRemoveSelectedLayer { get; set; }

    // wired by FeatureTable so the view can drop the DataGrid row selection
    public Action? RequestClearRowSelection { get; set; }

    public Action<ILayer>? RequestRefreshLayer { get; set; }

    public Func<DomainException, Task>? RequestShowErrorMessageAsync { get; set; }

    #endregion


    public SelectedLayer(IDialogService dialogService, VectorLayer layer, List<Field>? fields)
    {
        _dialogService = dialogService;

        AssociatedLayer = layer;

        Fields = fields;

        // the field initializer bypasses the property setter, so subscribe here
        _highlightedFeatures.CollectionChanged += highlightedFeatures_CollectionChanged;

        FilterManager = new FeatureTableFilterManager(fields, () => Features ?? Enumerable.Empty<Feature<Point>>());

        FilterManager.FilterChanged += OnFilterOrSortChanged;

        FilterManager.SortChanged += OnFilterOrSortChanged;
    }

    public FeatureTableFilterManager FilterManager { get; }

    private void OnFilterOrSortChanged()
    {
        _currentPageIndex = 0;

        RefreshPageDeferred();
    }

    private async void highlightedFeatures_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        await RefreshHighlightedFeaturesOnMap(HighlightedFeatures);

        NotifyAll();
    }

    private void TryFlashPoint(IEnumerable<Feature<Point>> point)
    {
        if (point?.Count() == 1 && point.First().GeometryType/*TheGeometry.Type*/ == GeometryType.Point)
        {
            RequestFlashSinglePoint?.Invoke(point.First());
        }
    }


    public void UpdateSelectedFeatures(IEnumerable<Feature<Point>> items)
    {
        _currentPageIndex = 0;

        // a new selection is a new dataset; stale filters would silently blank the grid
        FilterManager.ClearAll(raiseEvent: false);
        FilterManager.ClearSort();

        Features = new ObservableCollection<Feature<Point>>(items);
    }

    public void UpdateHighlightedFeatures(IEnumerable<Feature<Point>> items)
    {
        HighlightedFeatures = new ObservableCollection<Feature<Point>>(items);
    }

    public async Task RefreshSelectedFeaturesOnMap(IEnumerable<Feature<Point>> enumerable, double? strokeThickness)
    {
        if (AssociatedLayer != null)
        {
            AssociatedLayer.NumberOfSelectedFeatures = enumerable.Count();
        }

        if (RequestFeaturesChangedAsync is null)
            return;

        await RequestFeaturesChangedAsync.Invoke(enumerable.Where(i => i.Status != FeatureStatus.Removed && i.Status != FeatureStatus.CanceledNew), strokeThickness);
    }

    public async Task RefreshHighlightedFeaturesOnMap(IEnumerable<Feature<Point>> enumerable)
    {
        if (RequestHighlightFeaturesChangedAsync is null)
            return;

        await RequestHighlightFeaturesChangedAsync.Invoke(enumerable, AssociatedLayer.DefaultSymbology?.StrokeThickness);
    }

    public void RefreshFeatureInView(Feature<Point> feature)
    {
        RefreshPageDeferred();
    }

    #region Data Grid Paging

    private ObservableCollection<int> _availablePageSizes = new ObservableCollection<int> { 50, 100, 200, 500 };
    public ObservableCollection<int> AvailablePageSizes
    {
        get => _availablePageSizes;
        set
        {
            _availablePageSizes = value ?? new ObservableCollection<int> { 50, 100, 200, 500 };
            RaisePropertyChanged();
        }
    }

    private int _selectedPageSize = 100;
    public int SelectedPageSize
    {
        get => _selectedPageSize;
        set
        {
            if (value < 1)
                return;

            _selectedPageSize = value;
            RaisePropertyChanged();
            RefreshPagingView();
        }
    }

    private int _currentPageIndex = 0;
    public int CurrentPageIndex
    {
        get => _currentPageIndex;
        set
        {
            if (value < 0 || (TotalPages > 0 && value >= TotalPages))
                return;

            if (_currentPageIndex == value)
                return;

            _currentPageIndex = value;

            // navigating to another page drops the row selection so that
            // delete/edit never target rows that are no longer visible
            RefreshPagingView(preserveSelection: false);
        }
    }

    public int CurrentPageNumber => TotalPages == 0 ? 0 : CurrentPageIndex + 1;

    public int TotalPages => ViewFeatures.Count == 0
                                ? 0
                                : (int)Math.Ceiling(ViewFeatures.Count / (double)SelectedPageSize);

    private IReadOnlyList<Feature<Point>>? _viewFeatures;

    /// <summary>Features after applying the column filters and sort; the paging source.</summary>
    public IReadOnlyList<Feature<Point>> ViewFeatures => _viewFeatures ??= ComputeViewFeatures();

    private IReadOnlyList<Feature<Point>> ComputeViewFeatures()
    {
        if (Features is null || Features.Count == 0)
            return Array.Empty<Feature<Point>>();

        IEnumerable<Feature<Point>> result = Features;

        if (FilterManager.HasActiveFilter)
            result = result.Where(FilterManager.Matches);

        result = FilterManager.ApplySort(result);

        return ReferenceEquals(result, Features) ? Features : result.ToList();
    }

    public int CountOfFilteredFeatures => ViewFeatures.Count;

    public ObservableCollection<Feature<Point>> CurrentPageFeatures
    {
        get
        {
            if (ViewFeatures.Count == 0)
                return new ObservableCollection<Feature<Point>>();

            return new ObservableCollection<Feature<Point>>(ViewFeatures.Skip(CurrentPageIndex * SelectedPageSize).Take(SelectedPageSize));
        }
    }

    private bool _pageRefreshPending;

    // Coalesces refresh requests and defers them past any in-progress DataGrid edit
    // transaction; rebinding the page mid-transaction throws InvalidOperationException.
    private void RefreshPageDeferred()
    {
        if (_pageRefreshPending)
            return;

        _pageRefreshPending = true;

        var app = System.Windows.Application.Current;

        if (app?.Dispatcher == null || app.Dispatcher.HasShutdownStarted || app.Dispatcher.HasShutdownFinished)
        {
            _pageRefreshPending = false;
            RefreshPagingView();
            return;
        }

        app.Dispatcher.BeginInvoke(new Action(() =>
        {
            _pageRefreshPending = false;
            RefreshPagingView();
        }), System.Windows.Threading.DispatcherPriority.Background);
    }

    private void RefreshPagingView(bool preserveSelection = true)
    {
        _viewFeatures = null;

        // snapshot before rebinding: replacing the grid's ItemsSource clears its
        // selection, which flows back into HighlightedFeatures
        var snapshot = preserveSelection ? HighlightedFeatures?.ToList() : null;

        if (TotalPages > 0 && _currentPageIndex >= TotalPages)
            _currentPageIndex = TotalPages - 1;

        if (_currentPageIndex < 0)
            _currentPageIndex = 0;

        RaisePropertyChanged(nameof(CurrentPageFeatures));
        RaisePropertyChanged(nameof(CurrentPageIndex));
        RaisePropertyChanged(nameof(CurrentPageNumber));
        RaisePropertyChanged(nameof(TotalPages));
        RaisePropertyChanged(nameof(CountOfSelectedFeatures));
        RaisePropertyChanged(nameof(CountOfFilteredFeatures));

        var page = CurrentPageFeatures;

        var restored = snapshot?.Where(f => page.Contains(f)).ToList() ?? new List<Feature<Point>>();

        var current = HighlightedFeatures;

        var isSame = current != null && current.Count == restored.Count && restored.All(current.Contains);

        if (!isSame)
            UpdateHighlightedFeatures(restored);

        CommandManager.InvalidateRequerySuggested();
    }

    #endregion

    public IEnumerable<Feature<Point>> GetSelectedFeatures(bool includeRemoved = false)
    {
        return Features.Where(i => includeRemoved || i.Status != FeatureStatus.Removed && i.Status != FeatureStatus.CanceledNew);
    }

    public bool UpdateGeometry(Feature<Point> feature, Geometry<Point> newGeometry)
    {
        var dataSource = AssociatedLayer?.DataSource as IEditableVectorDataSource;

        if (dataSource is null)
            return false;

        if (Features is null)
            return false;

        if (!dataSource.UpdateGeometry(feature, newGeometry))
            return false;

        // in order to update the RowHeader icon
        RefreshFeatureInView(feature);

        NotifyAll();

        return true;
    }

    public void UpdateAttributes(Feature<Point> feature, Dictionary<string, object> oldAttributes)
    {
        var dataSource = AssociatedLayer?.DataSource as IEditableVectorDataSource;

        if (dataSource is null)
            return;

        if (Features is null)
            return;

        if (!dataSource.UpdateAttributes(feature, oldAttributes))
            return;

        // in order to update the RowHeader icon
        RefreshFeatureInView(feature);

        NotifyAll();
    }


    public async Task UndoAllChangesAsync()
    {
        var dataSource = AssociatedLayer?.DataSource as IEditableVectorDataSource;

        if (dataSource is null || !dataSource.HasPendingChanges)
            return;

        var features = dataSource.GetCurrentChanges();

        foreach (var feature in features)
        {
            UndoFeatureChanges(dataSource, feature);
        }

        NotifyAll();

        RequestRefreshLayer?.Invoke(AssociatedLayer);

        await RefreshSelectedFeaturesOnMap(GetSelectedFeatures(), AssociatedLayer?.DefaultSymbology?.StrokeThickness);
    }

    private void UndoFeatureChanges(IEditableVectorDataSource dataSource, Feature<Point> feature)
    {
        if (feature is null)
            return;

        if (feature.Status == FeatureStatus.Updated && feature.OldVersion != null)
        {
            dataSource.UndoChanges(feature);
        }
        else if (feature.Status == FeatureStatus.Removed)
        {
            dataSource.UndoChanges(feature);
        }
        else if (feature.Status == FeatureStatus.New)
        {
            dataSource.Remove(feature);

            Features?.Remove(feature);

            HighlightedFeatures?.Remove(feature);
        }

        RefreshFeatureInView(feature);
    }

    private void NotifyAll()
    {
        RaisePropertyChanged(nameof(IsSingleValueHighlighted));
        RaisePropertyChanged(nameof(CountOfSelectedFeatures));
        RaisePropertyChanged(nameof(CanUndo));
        RaisePropertyChanged(nameof(CanViewChanges));
        RaisePropertyChanged(nameof(HasPendingChanges));

        CommandManager.InvalidateRequerySuggested();
    }


    private bool CanExecuteAddFeature()
    {
        if (this.AssociatedLayer is null)
            return false;

        if (this.AssociatedLayer.IsBusy)
            return false;

        var dataSource = AssociatedLayer?.DataSource as IEditableVectorDataSource;

        if (dataSource is null)
            return false;

        var vectorDataSource = AssociatedLayer?.DataSource as VectorDataSource;

        var geometryType = vectorDataSource?.GeometryType;

        return geometryType != null && geometryType != GeometryType.None;
    }

    private bool CanExecuteDeleteFeature()
    {
        if (this.AssociatedLayer is null)
            return false;

        if (this.AssociatedLayer.IsBusy)
            return false;

        return HighlightedFeatures?.Any(f => f.Status != FeatureStatus.Removed && f.Status != FeatureStatus.CanceledNew) == true;
    }

    private bool CanExecuteEditFeature()
    {
        if (this.AssociatedLayer is null)
            return false;

        if (this.AssociatedLayer.IsBusy)
            return false;

        return IsSingleValueHighlighted;
    }

    private bool CanExecuteUndoEditFeature()
    {
        if (this.AssociatedLayer is null)
            return false;

        if (this.AssociatedLayer.IsBusy)
            return false;

        return CanUndo;
    }

    private bool CanExecuteSaveChanges()
    {

        if (this.AssociatedLayer is null)
            return false;

        if (this.AssociatedLayer.IsBusy)
            return false;

        return HasPendingChanges;
    }


    private async Task AddAsync()
    {
        try
        {
            var dataSource = AssociatedLayer?.DataSource as IEditableVectorDataSource;

            if (dataSource is null || RequestDrawAsync is null)
                return;

            var vectorDataSource = AssociatedLayer?.DataSource as VectorDataSource;
            var geometryType = vectorDataSource?.GeometryType ?? GeometryType.Point;
            //var srid = AssociatedLayer?.DataSource?.Srid ?? 0;

            // todo
            // this should be changed to draw geometry using the layers type
            //var emptyGeometry = Geometry<Point>.CreateEmpty(geometryType, srid);
            var geometry = await RequestDrawAsync(geometryType);

            if (geometry is null)
                return;

            this.AssociatedLayer!.IsBusy = true;

            var attributes = new Dictionary<string, object>();

            if (Fields != null)
            {
                foreach (var field in Fields)
                    attributes[field.Name] = field.GetDefaultValue();
            }

            var newId = dataSource.GetNewId();// (Features?.Select(f => f.Id).DefaultIfEmpty(0).Max() ?? 0) + 1;

            var newFeature = new Feature<Point>(geometry, attributes)
            {
                Id = newId
            };

            newFeature.MarkAsNew();

            dataSource.Add(newFeature);

            Features ??= new ObservableCollection<Feature<Point>>();

            Features.Add(newFeature);

            // an active filter could hide the just-added row, and an active sort would not
            // place it on the last page; clear both so it is guaranteed visible
            if (FilterManager.HasActiveFilter)
                FilterManager.ClearAll(raiseEvent: false);

            FilterManager.ClearSort();

            // TotalPages below must see the added feature
            _viewFeatures = null;

            // make the new row visible: it is appended at the end, on the last page
            if (TotalPages > 0 && CurrentPageIndex != TotalPages - 1)
                CurrentPageIndex = TotalPages - 1;
            else
                RefreshPagingView();

            NotifyAll();

            if (ShowSelectedOnMap)
                await RefreshSelectedFeaturesOnMap(GetSelectedFeatures(), AssociatedLayer?.DefaultSymbology?.StrokeThickness);

            RequestRefreshLayer?.Invoke(AssociatedLayer);

            //RequestEdit?.Invoke(newFeature);
        }
        catch (DomainException ex)
        {
            await _dialogService.ShowErrorMessage(ex);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorMessage(new MaptorUnknownException(ex.Message));
        }
        finally
        {
            if (this.AssociatedLayer != null)
                this.AssociatedLayer.IsBusy = false;
        }
    }

    private async Task DeleteAsync()
    {
        try
        {
            var message = IRI.Maptor.Presentation.Core.Properties.Resources.dialog_msg_confirmDeleteFeatures;

            if (await _dialogService.ShowYesNoDialogAsync(message) == false)
                return;

            this.AssociatedLayer.IsBusy = true;

            var dataSource = AssociatedLayer?.DataSource as IEditableVectorDataSource;

            if (dataSource is null || HighlightedFeatures is null || HighlightedFeatures.Count < 1)
                return;

            var toRemove = HighlightedFeatures.Where(f => f.Status != FeatureStatus.Removed && f.Status != FeatureStatus.CanceledNew).ToList();

            foreach (var feature in toRemove)
            {
                dataSource.Remove(feature);

                // deleting a not-yet-saved feature cancels it entirely; drop its row too
                if (feature.Status == FeatureStatus.CanceledNew)
                    Features?.Remove(feature);
            }

            if (RequestClearRowSelection != null)
                RequestClearRowSelection.Invoke();
            else
                HighlightedFeatures.Clear();

            RefreshPagingView();

            NotifyAll();

            RequestRefreshLayer?.Invoke(AssociatedLayer);

            await RefreshSelectedFeaturesOnMap(GetSelectedFeatures(), AssociatedLayer?.DefaultSymbology?.StrokeThickness);
        }
        catch (DomainException ex)
        {
            await _dialogService.ShowErrorMessage(ex);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorMessage(new MaptorUnknownException(ex.Message));
        }
        finally
        {
            this.AssociatedLayer.IsBusy = false;
        }
    }

    private async Task EditAsync()
    {
        var feature = IsSingleValueHighlighted ? HighlightedFeatures!.First() : null;

        if (feature != null && RequestEditAsync != null)
            await RequestEditAsync.Invoke(feature);

        NotifyAll();
    }

    public async Task UndoCurrentRowChangesAsync()
    {
        var dataSource = AssociatedLayer?.DataSource as IEditableVectorDataSource;

        if (dataSource is null || !CanUndo || dataSource.IsInitializing || dataSource.IsProcessing)
            return;

        var feature = IsSingleValueHighlighted ? HighlightedFeatures!.First() : null;

        UndoFeatureChanges(dataSource, feature);

        NotifyAll();

        RequestRefreshLayer?.Invoke(AssociatedLayer);

        await RefreshSelectedFeaturesOnMap(GetSelectedFeatures(), AssociatedLayer?.DefaultSymbology?.StrokeThickness);
    }

    private async Task UndoChangesAsync()
    {
        var message = IRI.Maptor.Presentation.Core.Properties.Resources.dialog_msg_discardPendingChanges;

        if (await _dialogService.ShowYesNoDialogAsync(message) == false)
            return;

        await UndoCurrentRowChangesAsync();
    }


    public async Task SaveChangesAsync()
    {
        try
        {
            var editableSource = AssociatedLayer.DataSource as IEditableVectorDataSource;

            if (editableSource is null)
                return;

            await editableSource.SaveChangesAsync();

            // Marshal grid refresh to UI thread; HTTP completion may have resumed on a background thread
            await DispatcherInvokeAsync(() =>
            {
                if (Features != null)
                {
                    var toBeRemoved = Features.Where(f => f.Status == FeatureStatus.Removed || f.Status == FeatureStatus.CanceledNew).ToList();

                    foreach (var item in toBeRemoved)
                        Features.Remove(item);
                }

                // rebind the visible page; features now have Status=Unchanged
                RefreshPagingView();

                NotifyAll();
            });

            await this._dialogService?.ShowMessage_DoneSuccessfully();
        }
        catch (DomainException ex)
        {
            await RequestShowErrorMessageAsync(ex);
        }
        catch (Exception ex)
        {
            await RequestShowErrorMessageAsync(new MaptorUnknownException(ex.Message));
        }
    }


    #region Commands

    private RelayCommand? _addCommand;
    public RelayCommand AddCommand
    {
        get
        {
            if (_addCommand is null)
            {
                _addCommand = new RelayCommand(
                    async param => await this.AddAsync(),
                    param => this.CanExecuteAddFeature());
            }

            return _addCommand;
        }
    }


    private RelayCommand? _deleteCommand;
    public RelayCommand DeleteCommand
    {
        get
        {
            if (_deleteCommand is null)
            {
                _deleteCommand = new RelayCommand(async param => await this.DeleteAsync(), param => CanExecuteDeleteFeature());
            }

            return _deleteCommand;
        }
    }


    private RelayCommand? _editCommand;
    public RelayCommand EditCommand
    {
        get
        {
            if (_editCommand is null)
            {
                _editCommand = new RelayCommand(async param => await EditAsync(), param => CanExecuteEditFeature());
            }

            return _editCommand;
        }
    }


    private RelayCommand? _undoChangesCommand;
    public RelayCommand UndoChangesCommand
    {
        get
        {
            if (_undoChangesCommand is null)
            {
                _undoChangesCommand = new RelayCommand(async param => await UndoChangesAsync(), param => CanExecuteUndoEditFeature());
            }

            return _undoChangesCommand;
        }
    }


    private RelayCommand? _saveChangesCommand;
    public RelayCommand SaveChangesCommand
    {
        get
        {
            if (_saveChangesCommand is null)
                _saveChangesCommand = new RelayCommand(async param => await SaveChangesAsync(), _ => CanExecuteSaveChanges());

            return _saveChangesCommand;
        }
    }


    private RelayCommand? _removeSelectedLayerCommand;
    public RelayCommand RemoveSelectedLayerCommand
    {
        get
        {
            if (_removeSelectedLayerCommand is null)
                _removeSelectedLayerCommand = new RelayCommand(param => RequestRemoveSelectedLayer?.Invoke());

            return _removeSelectedLayerCommand;
        }
    }


    private RelayCommand? _viewChangesCommand;
    public RelayCommand ViewChangesCommand
    {
        get
        {
            if (_viewChangesCommand is null)
            {
                _viewChangesCommand = new RelayCommand(param =>
                {
                    if (IsSingleValueHighlighted && HighlightedFeatures?.FirstOrDefault() is Feature<Point> feature)
                        RequestViewChanges?.Invoke(feature);
                }, param => CanViewChanges);
            }
            return _viewChangesCommand;
        }
    }


    private RelayCommand? _zoomToCommand;
    public RelayCommand ZoomToCommand
    {
        get
        {
            if (_zoomToCommand is null)
                _zoomToCommand = new RelayCommand(param => RequestZoomTo?.Invoke(HighlightedFeatures, () => { TryFlashPoint(HighlightedFeatures); }));

            return _zoomToCommand;
        }
    }


    private RelayCommand? _goToNextPageCommand;
    public RelayCommand GoToNextPageCommand =>
        _goToNextPageCommand ??= new RelayCommand(
            param => CurrentPageIndex++,
            param => CurrentPageIndex < TotalPages - 1);


    private RelayCommand? _goToPreviousPageCommand;
    public RelayCommand GoToPreviousPageCommand =>
        _goToPreviousPageCommand ??= new RelayCommand(
            param => CurrentPageIndex--,
            param => CurrentPageIndex > 0);


    private RelayCommand? _goToFirstPageCommand;
    public RelayCommand GoToFirstPageCommand =>
        _goToFirstPageCommand ??= new RelayCommand(
            param => CurrentPageIndex = 0,
            param => CurrentPageIndex > 0);


    private RelayCommand? _goToLastPageCommand;
    public RelayCommand GoToLastPageCommand =>
        _goToLastPageCommand ??= new RelayCommand(
            param => CurrentPageIndex = TotalPages - 1,
            param => CurrentPageIndex < TotalPages - 1);


    #endregion

    /// <summary>
    /// Marshals the action to the UI thread. WPF requires collection/property updates on the UI thread.
    /// If no dispatcher is available (e.g. unit tests), runs the action directly.
    /// </summary>
    private static Task DispatcherInvokeAsync(Action action)
    {
        var app = System.Windows.Application.Current;
        if (app?.Dispatcher == null || app.Dispatcher.HasShutdownStarted || app.Dispatcher.HasShutdownFinished)
        {
            action();
            return Task.CompletedTask;
        }
        if (app.Dispatcher.CheckAccess())
        {
            action();
            return Task.CompletedTask;
        }
        return app.Dispatcher.InvokeAsync(action).Task;
    }
}
