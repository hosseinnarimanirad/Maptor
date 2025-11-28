using System;
using System.Collections.ObjectModel;
using System.Linq;
using IRI.Maptor.Jab.Common;
using IRI.Maptor.Jab.Common.Assets.Commands;
using IRI.Maptor.Jab.Common.Models.CoordinateEditor;

namespace IRI.Maptor.Jab.Common.Presenters.CoordinateEditor;

public class MultiPointEditorPresenter : Notifier
{
    private ObservableCollection<PointEditorPresenter> _points = new ObservableCollection<PointEditorPresenter>();
    public ObservableCollection<PointEditorPresenter> Points
    {
        get => _points;
        set
        {
            _points = value ?? new ObservableCollection<PointEditorPresenter>();
            RaisePropertyChanged();
        }
    }

    public event Action<ObservableCollection<PointEditorPresenter>>? GeometryChanged;

    private RelayCommand _addPointCommand;
    public RelayCommand AddPointCommand =>
        _addPointCommand ??= new RelayCommand(param =>
        {
            var newPointInfo = new PointInfo { X = 0, Y = 0 };
            var newPointPresenter = new PointEditorPresenter(newPointInfo, canDelete: true);
            Points.Add(newPointPresenter);
            GeometryChanged?.Invoke(Points);
        });

    private RelayCommand _deletePointCommand;
    public RelayCommand DeletePointCommand =>
        _deletePointCommand ??= new RelayCommand(param =>
        {
            if (param is PointEditorPresenter pointPresenter)
            {
                Points.Remove(pointPresenter);
                GeometryChanged?.Invoke(Points);
            }
        });

    public MultiPointEditorPresenter(ObservableCollection<PointInfo>? points = null)
    {
        if (points != null)
        {
            Points = new ObservableCollection<PointEditorPresenter>(
                points.Select(p => new PointEditorPresenter(p, canDelete: true)));
        }
        else
        {
            Points = new ObservableCollection<PointEditorPresenter>();
        }

        Points.CollectionChanged += (s, e) => GeometryChanged?.Invoke(Points);
    }
}




