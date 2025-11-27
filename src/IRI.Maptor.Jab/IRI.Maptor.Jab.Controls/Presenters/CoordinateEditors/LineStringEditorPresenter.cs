using System;
using System.Collections.ObjectModel;
using System.Linq;
using IRI.Maptor.Jab.Common;
using IRI.Maptor.Jab.Common.Assets.Commands;
using IRI.Maptor.Jab.Controls.Models.GeometryDetails;
using IRI.Maptor.Sta.Common.Primitives;

namespace IRI.Maptor.Jab.Controls.Presenters.CoordinateEditors;

public class LineStringEditorPresenter : Notifier
{
    private ObservableCollection<PointInfo> _points = new ObservableCollection<PointInfo>();
    public ObservableCollection<PointInfo> Points
    {
        get => _points;
        set
        {
            _points = value ?? new ObservableCollection<PointInfo>();
            RaisePropertyChanged();
        }
    }

    public event Action<ObservableCollection<PointInfo>>? PointsChanged;

    private RelayCommand _addPointCommand;
    public RelayCommand AddPointCommand =>
        _addPointCommand ??= new RelayCommand(param =>
        {
            var newPoint = new PointInfo { X = 0, Y = 0 };
            Points.Add(newPoint);
            PointsChanged?.Invoke(Points);
        });

    private RelayCommand _deletePointCommand;
    public RelayCommand DeletePointCommand =>
        _deletePointCommand ??= new RelayCommand(param =>
        {
            if (param is PointInfo point)
            {
                Points.Remove(point);
                PointsChanged?.Invoke(Points);
            }
        });

    public LineStringEditorPresenter(ObservableCollection<PointInfo>? points = null)
    {
        Points = points ?? new ObservableCollection<PointInfo>();
        Points.CollectionChanged += (s, e) => PointsChanged?.Invoke(Points);
        
        foreach (var point in Points)
        {
            point.PropertyChanged += (s, e) => PointsChanged?.Invoke(Points);
        }
    }
}




