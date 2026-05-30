using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Jab.Common.Layers;
using IRI.Maptor.Sta.Common.Exceptions;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Persistence.Abstractions;
using IRI.Maptor.Sta.Persistence.DataSources;
using IRI.Maptor.Jab.Common.Services;
using System.Windows.Input;

namespace IRI.Maptor.Jab.Common.Models;

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

    public Action<ILayer>? RequestRefreshLayer { get; set; }

    public Func<DomainException, Task>? RequestShowErrorMessageAsync { get; set; }

    #endregion


    public SelectedLayer(IDialogService dialogService, VectorLayer layer, List<Field>? fields)
    {
        _dialogService = dialogService;

        AssociatedLayer = layer;

        Fields = fields;
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

        await RequestFeaturesChangedAsync?.Invoke(enumerable.Where(i => i.Status != FeatureStatus.Removed && i.Status != FeatureStatus.CanceledNew), strokeThickness);
    }

    public async Task RefreshHighlightedFeaturesOnMap(IEnumerable<Feature<Point>> enumerable)
    {
        if (RequestHighlightFeaturesChangedAsync is null)
            return;

        await RequestHighlightFeaturesChangedAsync.Invoke(enumerable, AssociatedLayer.DefaultSymbology?.StrokeThickness);
    }

    public void RefreshFeatureInView(Feature<Point> feature)
    {
        if (Features is null)
            return;

        var idx = Features?.IndexOf(feature) ?? -1;

        if (idx >= 0)
        {
            Features!.RemoveAt(idx);

            Features.Insert(idx, feature);
        }
    }

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

            RefreshFeatureInView(feature);
        }
        else if (feature.Status == FeatureStatus.Removed)
        {
            dataSource.UndoChanges(feature);

            RefreshFeatureInView(feature);
        }
        else if (feature.Status == FeatureStatus.New)
        {
            dataSource.Remove(feature);

            Features?.Remove(feature);

            HighlightedFeatures?.Remove(feature);
        }
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

        return HighlightedFeatures?.Count >= 1;
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
        var dataSource = AssociatedLayer?.DataSource as IEditableVectorDataSource;

        if (dataSource is null)
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

        NotifyAll();

        if (ShowSelectedOnMap)
            await RefreshSelectedFeaturesOnMap(GetSelectedFeatures(), AssociatedLayer?.DefaultSymbology?.StrokeThickness);

        RequestRefreshLayer?.Invoke(AssociatedLayer);

        //RequestEdit?.Invoke(newFeature);
    }

    private async Task DeleteAsync()
    {
        var dataSource = AssociatedLayer?.DataSource as IEditableVectorDataSource;

        if (dataSource is null || HighlightedFeatures?.Count < 1)
            return;

        var toRemove = HighlightedFeatures.ToList();

        foreach (var feature in toRemove)
        {
            dataSource.Remove(feature);
            RefreshFeatureInView(feature);
        }

        foreach (var feature in toRemove)
            HighlightedFeatures.Remove(feature);

        NotifyAll();

        RequestRefreshLayer?.Invoke(AssociatedLayer);

        await RefreshSelectedFeaturesOnMap(GetSelectedFeatures(), AssociatedLayer?.DefaultSymbology?.StrokeThickness);

    }

    private async Task EditAsync()
    {
        var feature = IsSingleValueHighlighted ? HighlightedFeatures!.First() : null;

        if (feature != null)
            await RequestEditAsync?.Invoke(feature);

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
        var message = IRI.Maptor.Jab.Common.Properties.Resources.dialog_msg_discardPendingChanges;

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
                var toBeRemoved = Features.Where(f => f.Status == FeatureStatus.Removed).ToList();

                foreach (var item in toBeRemoved)
                    Features.Remove(item);

                // Replace collection to force DataGrid to re-bind; features now have Status=Unchanged
                if (Features != null)
                    Features = new ObservableCollection<Feature<Point>>(Features);

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
                });
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
