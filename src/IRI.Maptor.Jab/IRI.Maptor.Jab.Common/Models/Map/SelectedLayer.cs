using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Jab.Common.Assets.Commands;
using IRI.Maptor.Sta.Persistence.Abstractions;
using IRI.Maptor.Sta.Persistence.DataSources;
using IRI.Maptor.Sta.Common.Enums;

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
            {
                _highlightedFeatures.CollectionChanged -= highlightedFeatures_CollectionChanged;
                _highlightedFeatures.CollectionChanged += highlightedFeatures_CollectionChanged;
            }

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
            return feature.Status == FeatureStatus.Updated && feature.OldFeature != null ||
                   feature.Status == FeatureStatus.New;
        }
    }

    public int CountOfSelectedFeatures => Features?.Count ?? 0;


    public Action<IEnumerable<Feature<Point>>, double?>? RequestFeaturesChanged { get; set; }

    public Action<IEnumerable<Feature<Point>>, double?>? RequestHighlightFeaturesChanged { get; set; }

    public Action<Feature<Point>>? RequestFlashSinglePoint { get; set; }

    public Action<IEnumerable<Feature<Point>>, Action>? RequestZoomTo { get; set; }

    public Action<Feature<Point>>? RequestEdit { get; set; }

    public Func<GeometryType, Task<Geometry<Point>?>> RequestDraw { get; set; }

    public Action? RequestRemove { get; set; }

    public Action<ILayer>? RequestRefreshLayer { get; set; }


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
        RequestFeaturesChanged?.Invoke(enumerable, strokeThickness);
    }

    public void RefreshHighlightedFeaturesOnMap(IEnumerable<Feature<Point>> enumerable)
    {
        RequestHighlightFeaturesChanged?.Invoke(enumerable, this.AssociatedLayer.DefaultSymbology?.StrokeThickness);
    }

    public IEnumerable<Feature<Point>> GetSelectedFeatures()
    {
        return Features;
    }

    public void Update(Feature<Point> oldFeature, Feature<Point> newFeature)
    {
        var dataSource = AssociatedLayer?.DataSource as IEditableVectorDataSource;

        if (dataSource is null)
            return;

        if (Features is null)
            return;

        if (!dataSource.Update(oldFeature, newFeature))
            return;

        var feature = Features.SingleOrDefault(f => f.Id == oldFeature.Id);

        if (feature is null)
            return;

        //feature.MarkAsUpdated(newFeature);

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

    public void SaveChanges()
    {
        (AssociatedLayer.DataSource as IEditableVectorDataSource)?.SaveChanges();

        // Replace collection to force DataGrid to re-bind; features now have Status=Unchanged
        if (Features != null)
            Features = new ObservableCollection<Feature<Point>>(Features);

        NotifyAll();
    }

    private void NotifyAll()
    {
        RaisePropertyChanged(nameof(IsSingleValueHighlighted));
        RaisePropertyChanged(nameof(CountOfSelectedFeatures));
        RaisePropertyChanged(nameof(CanDelete));
        RaisePropertyChanged(nameof(CanUndo));
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

                    var newId = (Features?.Select(f => f.Id).DefaultIfEmpty(0).Max() ?? 0) + 1;

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
                        RefreshSelectedFeaturesOnMap(Features, AssociatedLayer?.DefaultSymbology?.StrokeThickness);

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
                        Features?.Remove(feature);
                    }
                    foreach (var feature in toRemove)
                        HighlightedFeatures.Remove(feature);

                    NotifyAll();

                    RequestRefreshLayer?.Invoke(AssociatedLayer);

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
                    var feature = HighlightedFeatures?.Count == 1 ? HighlightedFeatures!.First() : null;
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
                    var dataSource = AssociatedLayer?.DataSource as IEditableVectorDataSource;
                    if (dataSource is null || !CanUndo)
                        return;

                    var feature = HighlightedFeatures?.Count == 1 ? HighlightedFeatures!.First() : null;
                    if (feature is null)
                        return;

                    if (feature.Status == FeatureStatus.Updated && feature.OldFeature != null)
                    {
                        dataSource.Update(feature, feature.OldFeature);
                        feature.MarkAsSaved();
                        RefreshFeatureInView(feature);
                    }
                    else if (feature.Status == FeatureStatus.New)
                    {
                        dataSource.Remove(feature);
                        Features?.Remove(feature);
                        HighlightedFeatures?.Remove(feature);
                    }

                    NotifyAll();
                    RequestRefreshLayer?.Invoke(AssociatedLayer);
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
                _saveCommand = new RelayCommand(param => this.SaveChanges());

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
}
