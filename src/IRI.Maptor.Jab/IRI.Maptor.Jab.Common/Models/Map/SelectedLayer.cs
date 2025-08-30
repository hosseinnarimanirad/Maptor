using System; 
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Jab.Common.Assets.Commands;
using IRI.Maptor.Sta.Persistence.Abstractions;


namespace IRI.Maptor.Jab.Common.Models.Map;

public class SelectedLayer/*<TGeometryAware> */: Notifier/*, ISelectedLayer *//*where TGeometryAware : class, IGeometryAware<Point>*/
{
    public Guid Id { get { return AssociatedLayer?.LayerId ?? Guid.Empty; } }

    public VectorLayer AssociatedLayer { get; set; }

    public string LayerName { get { return AssociatedLayer?.LayerName; } }

    public bool ShowSelectedOnMap { get; set; } = false;

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

    public List<Field>? Fields { get; set; }

    private ObservableCollection<Feature<Point>> _highlightedFeatures = new ObservableCollection<Feature<Point>>();
    public ObservableCollection<Feature<Point>> HighlightedFeatures
    {
        get { return _highlightedFeatures; }
        set
        {
            _highlightedFeatures = value;
            RaisePropertyChanged();

            _highlightedFeatures.CollectionChanged += (sender, e) =>
            {
                this.UpdateHighlightedFeaturesOnMap(HighlightedFeatures/*.Cast<Feature<Point>>()*/);

                RaisePropertyChanged(nameof(IsSingleValueHighlighted));
                //Update();
            };

            //Update();
            this.UpdateHighlightedFeaturesOnMap(HighlightedFeatures/*.Cast<Feature<Point>>()*/);

            RaisePropertyChanged(nameof(IsSingleValueHighlighted));
        }
    }

    public bool IsSingleValueHighlighted
    {
        get
        {
            return HighlightedFeatures?.Count == 1;
        }
    }

    //private void Update()
    //{
    //    this.UpdateHighlightedFeaturesOnMap(HighlightedFeatures.Cast<Feature<Point>>());
    //    RaisePropertyChanged(nameof(IsSingleValueHighlighted));
    //}


    public SelectedLayer(VectorLayer layer, List<Field>? fields)
    {
        //this.Id = id;

        this.AssociatedLayer = layer;

        this.Fields = fields;
    }

    public void UpdateSelectedFeatures(IEnumerable<Feature<Point>> items)
    {
        Features = new ObservableCollection<Feature<Point>>(items/*?.Cast<TGeometryAware>()*/);
    }

    public void UpdateHighlightedFeatures(IEnumerable<Feature<Point>> items)
    {
        HighlightedFeatures = new ObservableCollection<Feature<Point>>(items/*.Cast<TGeometryAware>()*/);
    }

    public void UpdateSelectedFeaturesOnMap(IEnumerable<Feature<Point>> enumerable)
    {
        FeaturesChangedAction?.Invoke(enumerable);
    }

    public void UpdateHighlightedFeaturesOnMap(IEnumerable<Feature<Point>> enumerable)
    {
        HighlightFeaturesChangedAction?.Invoke(enumerable);
    }

    public IEnumerable<Feature<Point>> GetSelectedFeatures()
    {
        return Features/*?.Cast<Feature<Point>>().ToList()*/;

        //AssociatedLayer.
    }

    public int CountOfSelectedFeatures()
    {
        return Features.Count;
    }

    //public IEnumerable<Feature<Point>> GetHighlightedFeatures()
    //{
    //    return HighlightedFeatures/*?.Cast<Feature<Point>>().ToList()*/;
    //}

    private void TryFlashPoint(IEnumerable<Feature<Point>> point)
    {
        if (point?.Count() == 1 && point.First().TheGeometry.Type == GeometryType.Point)
        {
            RequestFlashSinglePoint?.Invoke(point.First());
        }
    }

    public void Update(Feature<Point> oldGeometry, Feature<Point> newGeometry)
    {
        var dataSource = (this?.AssociatedLayer as VectorLayer)?.DataSource as IEditableVectorDataSource/*<Feature<Point>, Point>*/;

        dataSource.Update(newGeometry /*as TGeometryAware*/);

        var feature = this.Features.Single(f => f.Id == oldGeometry.Id);

        feature.TheGeometry = newGeometry.TheGeometry;

        //this.UpdateHighlightedFeatures(new List<Feature<Point>>() { feature });
        //var highlight = HighlightedFeatures.Single(h => h.Id == oldGeometry.Id)
    }

    public void UpdateFeature(Feature<Point> item)
    {
        //var itemValue = item as Feature<Point>;

        var dataSource = (this?.AssociatedLayer as VectorLayer)?.DataSource as IEditableVectorDataSource/*<TGeometryAware, Point>*/;

        //dataSource.UpdateFeature(itemValue);
        dataSource.Update(item);
    }

    public Action<IEnumerable<Feature<Point>>> FeaturesChangedAction { get; set; }

    public Action<IEnumerable<Feature<Point>>> HighlightFeaturesChangedAction { get; set; }

    public Action<Feature<Point>> RequestFlashSinglePoint { get; set; }

    public Action<IEnumerable<Feature<Point>>, Action> RequestZoomTo { get; set; }

    public Action<Feature<Point>> RequestEdit { get; set; }


    public void SaveChanges()
    {
        ((AssociatedLayer as VectorLayer).DataSource as IEditableVectorDataSource/*<TGeometryAware, Point>*/).SaveChanges();
    }



    public Action RequestRemove { get; set; }


    private RelayCommand _zoomToCommand;
    public RelayCommand ZoomToCommand
    {
        get
        {
            if (_zoomToCommand == null)
            {
                _zoomToCommand = new RelayCommand(param =>
                { 
                    this.RequestZoomTo?.Invoke(HighlightedFeatures, () => { TryFlashPoint(HighlightedFeatures); });
                });
            }

            return _zoomToCommand;
        }
    }


    private RelayCommand _editCommand;
    public RelayCommand EditCommand
    {
        get
        {
            if (_editCommand == null)
            {
                _editCommand = new RelayCommand(param =>
                {
                    //var highlightedFeatures = GetHighlightedFeatures();

                    if (HighlightedFeatures?.Count == 1)
                    {
                        this.RequestEdit(HighlightedFeatures.First());
                    }
                });
            }

            return _editCommand;
        }
    }


    private RelayCommand _saveCommand;
    public RelayCommand SaveCommand
    {
        get
        {
            if (_saveCommand == null)
            {
                _saveCommand = new RelayCommand(param =>
                {
                    this.SaveChanges();
                });
            }

            return _saveCommand;
        }
    }


    private RelayCommand _removeCommand;
    public RelayCommand RemoveCommand
    {
        get
        {
            if (_removeCommand == null)
            {
                _removeCommand = new RelayCommand(param =>
                {
                    this.RequestRemove?.Invoke();
                });
            }

            return _removeCommand;
        }
    }
}
