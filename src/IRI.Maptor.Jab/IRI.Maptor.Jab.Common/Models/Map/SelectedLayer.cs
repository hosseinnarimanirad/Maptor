using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Jab.Common.Assets.Commands;
using IRI.Maptor.Sta.Persistence.Abstractions;
using IRI.Maptor.Sta.Persistence.DataSources;
using IRI.Maptor.Sta.Common.Enums;
using System.Windows.Forms;

namespace IRI.Maptor.Jab.Common.Models.Map;

public class SelectedLayer : Notifier
{
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

            this.RefreshHighlightedFeaturesOnMap(HighlightedFeatures);

            NotifyAll();
        }
    }

    public bool ShowSelectedOnMap { get; set; } = false;

    public bool IsSingleValueHighlighted => HighlightedFeatures?.Count == 1;

    public bool CanAdd => AssociatedLayer?.DataSource is IEditableVectorDataSource;

    public bool CanDelete => HighlightedFeatures?.Count >= 1;

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


    public Action<IEnumerable<Feature<Point>>, double?>? RequestFeaturesChanged { get; set; }

    public Action<IEnumerable<Feature<Point>>, double?>? RequestHighlightFeaturesChanged { get; set; }

    public Action<Feature<Point>>? RequestFlashSinglePoint { get; set; }

    public Action<IEnumerable<Feature<Point>>, Action>? RequestZoomTo { get; set; }

    public Action<Feature<Point>>? RequestEdit { get; set; }

    public Action<Feature<Point>>? RequestViewChanges { get; set; }

    public Func<GeometryType, Task<Geometry<Point>?>> RequestDraw { get; set; }

    public Action? RequestRemove { get; set; }

    public Action<ILayer>? RequestRefreshLayer { get; set; }

    public Func<string, Task> RequestShowErrorMessage { get; set; }


    public SelectedLayer(VectorLayer layer, List<Field>? fields)
    {
        this.AssociatedLayer = layer;

        this.Fields = fields;
    }

    private void highlightedFeatures_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        this.RefreshHighlightedFeaturesOnMap(HighlightedFeatures);

        NotifyAll();
    }

    private void TryFlashPoint(IEnumerable<Feature<Point>> point)
    {
        if (point?.Count() == 1 && point.First().TheGeometry.Type == GeometryType.Point)
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

    public void RefreshSelectedFeaturesOnMap(IEnumerable<Feature<Point>> enumerable, double? strokeThickness)
    {
        if (this.AssociatedLayer != null)
        {
            this.AssociatedLayer.NumberOfSelectedFeatures = enumerable.Count();
        }

        RequestFeaturesChanged?.Invoke(enumerable.Where(i => i.Status != FeatureStatus.Removed && i.Status != FeatureStatus.CanceledNew), strokeThickness);
    }

    public void RefreshHighlightedFeaturesOnMap(IEnumerable<Feature<Point>> enumerable)
    {
        RequestHighlightFeaturesChanged?.Invoke(enumerable, this.AssociatedLayer.DefaultSymbology?.StrokeThickness);
    }

    public IEnumerable<Feature<Point>> GetSelectedFeatures(bool includeRemoved = false)
    {
        return Features.Where(i => includeRemoved || (i.Status != FeatureStatus.Removed && i.Status != FeatureStatus.CanceledNew));
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

    //public void Update(Feature<Point> oldFeature, Feature<Point> newFeature)
    //{
    //    var dataSource = AssociatedLayer?.DataSource as IEditableVectorDataSource;

    //    if (dataSource is null)
    //        return;

    //    if (Features is null)
    //        return;

    //    if (!dataSource.update(oldFeature, newFeature))
    //        return;

    //    var feature = Features.SingleOrDefault(f => f.Id == oldFeature.Id);

    //    if (feature is null)
    //        return;

    //    //feature.MarkAsUpdated(newFeature);

    //    RefreshFeatureInView(feature);
    //}

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

    ///// <summary>
    ///// Updates the feature in the data source. Returns true if the feature was updated.
    ///// Call with (oldFeature, newFeature) - for cell edits, capture oldFeature in BeginningEdit.
    ///// </summary>
    //public bool UpdateFeature(Feature<Point> oldFeature, Feature<Point> newFeature)
    //{
    //    var dataSource = AssociatedLayer?.DataSource as IEditableVectorDataSource;
    //    if (dataSource is null)
    //        return false;

    //    if (!dataSource.Update(oldFeature, newFeature))
    //        return false;

    //    if (Features is not null)
    //        RefreshFeatureInView(newFeature);

    //    return true;
    //}

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

    public async Task SaveChangesAsync()
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
    }

    public void UndoCurrentRowChanges()
    {
        var dataSource = AssociatedLayer?.DataSource as IEditableVectorDataSource;
        if (dataSource is null || !CanUndo)
            return;

        var feature = IsSingleValueHighlighted ? HighlightedFeatures!.First() : null;

        UndoFeatureChanges(dataSource, feature);

        NotifyAll();

        RequestRefreshLayer?.Invoke(AssociatedLayer);
        RefreshSelectedFeaturesOnMap(GetSelectedFeatures(), AssociatedLayer?.DefaultSymbology?.StrokeThickness);
    }

    public void UndoAllChanges()
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
        RefreshSelectedFeaturesOnMap(GetSelectedFeatures(), AssociatedLayer?.DefaultSymbology?.StrokeThickness);
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
        else if (feature.Status == FeatureStatus.New)
        {
            dataSource.Remove(feature);

            Features?.Remove(feature);

            HighlightedFeatures?.Remove(feature);
        }
        else if (feature.Status == FeatureStatus.Removed)
        {
            dataSource.UndoChanges(feature);
            RefreshFeatureInView(feature);
        }
    }

    //public void UndoAllChanges()
    //{
    //    if (true)
    //    {

    //    }
    //}

    private void NotifyAll()
    {
        RaisePropertyChanged(nameof(IsSingleValueHighlighted));
        RaisePropertyChanged(nameof(CountOfSelectedFeatures));
        RaisePropertyChanged(nameof(CanDelete));
        RaisePropertyChanged(nameof(CanUndo));
        RaisePropertyChanged(nameof(CanViewChanges));
        RaisePropertyChanged(nameof(HasPendingChanges));
    }

    private RelayCommand? _addCommand;
    public RelayCommand AddCommand
    {
        get
        {
            if (_addCommand is null)
            {
                _addCommand = new RelayCommand(async param =>
                {
                    var dataSource = AssociatedLayer?.DataSource as IEditableVectorDataSource;
                    if (dataSource is null)
                        return;

                    var vectorDataSource = AssociatedLayer?.DataSource as VectorDataSource;
                    var geometryType = vectorDataSource?.GeometryType ?? Sta.Common.Enums.GeometryType.Point;
                    //var srid = AssociatedLayer?.DataSource?.Srid ?? 0;

                    // todo
                    // this should be changed to draw geometry using the layers type
                    //var emptyGeometry = Geometry<Point>.CreateEmpty(geometryType, srid);
                    var geometry = await RequestDraw(geometryType);

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
                        RefreshSelectedFeaturesOnMap(GetSelectedFeatures(), AssociatedLayer?.DefaultSymbology?.StrokeThickness);

                    RequestRefreshLayer?.Invoke(AssociatedLayer);

                    //RequestEdit?.Invoke(newFeature);
                });
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
                _deleteCommand = new RelayCommand(param =>
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

                    RefreshSelectedFeaturesOnMap(GetSelectedFeatures(), AssociatedLayer?.DefaultSymbology?.StrokeThickness);

                });
            }
            return _deleteCommand;
        }
    }




    private RelayCommand? _zoomToCommand;
    public RelayCommand ZoomToCommand
    {
        get
        {
            if (_zoomToCommand is null)
                _zoomToCommand = new RelayCommand(param => this.RequestZoomTo?.Invoke(HighlightedFeatures, () => { TryFlashPoint(HighlightedFeatures); }));

            return _zoomToCommand;
        }
    }


    private RelayCommand? _editCommand;
    public RelayCommand EditCommand
    {
        get
        {
            if (_editCommand is null)
            {
                _editCommand = new RelayCommand(param =>
                {
                    var feature = IsSingleValueHighlighted ? HighlightedFeatures!.First() : null;
                    if (feature != null)
                        RequestEdit?.Invoke(feature);
                });
            }

            return _editCommand;
        }
    }


    private RelayCommand? _undoCommand;
    public RelayCommand UndoCommand
    {
        get
        {
            if (_undoCommand is null)
            {
                _undoCommand = new RelayCommand(param =>
                {
                    UndoCurrentRowChanges();
                });
            }
            return _undoCommand;
        }
    }


    private RelayCommand? _saveCommand;
    public RelayCommand SaveCommand
    {
        get
        {
            if (_saveCommand is null)
                _saveCommand = new RelayCommand(async param =>
                {
                    try
                    {
                        await this.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        await RequestShowErrorMessage(ex.Message);
                    }

                }, _ => HasPendingChanges);

            return _saveCommand;
        }
    }


    private RelayCommand? _removeCommand;
    public RelayCommand RemoveCommand
    {
        get
        {
            if (_removeCommand is null)
                _removeCommand = new RelayCommand(param => this.RequestRemove?.Invoke());

            return _removeCommand;
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
