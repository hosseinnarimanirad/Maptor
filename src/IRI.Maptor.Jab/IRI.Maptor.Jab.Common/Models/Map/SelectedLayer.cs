using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Jab.Common.Assets.Commands;
using IRI.Maptor.Sta.Persistence.Abstractions;
using System.Collections.Specialized;
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

            _highlightedFeatures.CollectionChanged += highlightedFeatures_CollectionChanged;

            this.UpdateHighlightedFeaturesOnMap(HighlightedFeatures);

            RaisePropertyChanged(nameof(IsSingleValueHighlighted));
        }
    }

    public bool ShowSelectedOnMap { get; set; } = false;

    public bool IsSingleValueHighlighted => HighlightedFeatures?.Count == 1;

    public int CountOfSelectedFeatures => Features?.Count ?? 0;


    public Action<IEnumerable<Feature<Point>>, double?>? RequestFeaturesChanged { get; set; }

    public Action<IEnumerable<Feature<Point>>, double?>? RequestHighlightFeaturesChanged { get; set; }

    public Action<Feature<Point>>? RequestFlashSinglePoint { get; set; }

    public Action<IEnumerable<Feature<Point>>, Action>? RequestZoomTo { get; set; }

    public Action<Feature<Point>>? RequestEdit { get; set; }

    public Action? RequestRemove { get; set; }


    public SelectedLayer(VectorLayer layer, List<Field>? fields)
    {
        this.AssociatedLayer = layer;

        this.Fields = fields;
    }

    private void highlightedFeatures_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        this.UpdateHighlightedFeaturesOnMap(HighlightedFeatures);

        RaisePropertyChanged(nameof(IsSingleValueHighlighted));
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

    public void UpdateSelectedFeaturesOnMap(IEnumerable<Feature<Point>> enumerable, double? strokeThickness)
    {
        RequestFeaturesChanged?.Invoke(enumerable, strokeThickness);
    }

    public void UpdateHighlightedFeaturesOnMap(IEnumerable<Feature<Point>> enumerable)
    {
        RequestHighlightFeaturesChanged?.Invoke(enumerable, this.AssociatedLayer.DefaultSymbology?.StrokeThickness);
    }

    public IEnumerable<Feature<Point>> GetSelectedFeatures()
    {
        return Features;
    }

    public void Update(Feature<Point> oldGeometry, Feature<Point> newGeometry)
    {
        var dataSource = AssociatedLayer?.DataSource as IEditableVectorDataSource;

        if (dataSource is null)
            return;

        dataSource.Update(newGeometry);

        var feature = this.Features.Single(f => f.Id == oldGeometry.Id);

        feature.TheGeometry = newGeometry.TheGeometry;
    }

    public void UpdateFeature(Feature<Point> item) => (AssociatedLayer?.DataSource as IEditableVectorDataSource)?.Update(item);

    public void SaveChanges() => (AssociatedLayer.DataSource as IEditableVectorDataSource)?.SaveChanges();






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
                    if (HighlightedFeatures?.Count == 1)
                        this.RequestEdit(HighlightedFeatures.First());
                });
            }

            return _editCommand;
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
