using System;
using System.Collections.ObjectModel;
using IRI.Maptor.Jab.Common;
using IRI.Maptor.Jab.Common.Assets.Commands;

namespace IRI.Maptor.Jab.Common.Presenters.CoordinateEditor;

public class MultiPolygonEditorPresenter : Notifier
{
    private ObservableCollection<PolygonEditorPresenter> _polygons = new ObservableCollection<PolygonEditorPresenter>();
    public ObservableCollection<PolygonEditorPresenter> Polygons
    {
        get => _polygons;
        set
        {
            _polygons = value ?? new ObservableCollection<PolygonEditorPresenter>();
            RaisePropertyChanged();
        }
    }

    public event Action<ObservableCollection<PolygonEditorPresenter>>? GeometryChanged;

    private RelayCommand _addPolygonCommand;
    public RelayCommand AddPolygonCommand =>
        _addPolygonCommand ??= new RelayCommand(param =>
        {
            var newPolygon = new PolygonEditorPresenter();
            Polygons.Add(newPolygon);
            GeometryChanged?.Invoke(Polygons);
        });

    private RelayCommand _deletePolygonCommand;
    public RelayCommand DeletePolygonCommand =>
        _deletePolygonCommand ??= new RelayCommand(param =>
        {
            if (param is PolygonEditorPresenter polygon)
            {
                Polygons.Remove(polygon);
                GeometryChanged?.Invoke(Polygons);
            }
        });

    public MultiPolygonEditorPresenter()
    {
        Polygons = new ObservableCollection<PolygonEditorPresenter>();
        Polygons.CollectionChanged += (s, e) => GeometryChanged?.Invoke(Polygons);
    }
}




