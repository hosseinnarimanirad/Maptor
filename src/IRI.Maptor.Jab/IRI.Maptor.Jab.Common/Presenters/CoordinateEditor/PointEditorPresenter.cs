using System;
using IRI.Maptor.Jab.Common;
using IRI.Maptor.Jab.Common.Assets.Commands;
using IRI.Maptor.Jab.Common.Models;
using IRI.Maptor.Jab.Common.Models.CoordinateEditor;
using IRI.Maptor.Sta.Common.Primitives;

namespace IRI.Maptor.Jab.Common.Presenters.CoordinateEditor;

public class PointEditorPresenter : Notifier
{
    private NotifiablePoint _point;
    public NotifiablePoint Point
    {
        get => _point;
        set
        {
            _point = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(CanDelete));
        }
    }

    public bool CanDelete { get; set; }

    public event Action<IRI.Maptor.Sta.Common.Primitives.Point>? RequestZoomToPoint;
    public event Action<NotifiablePoint>? PointChanged;

    private RelayCommand _zoomToPointCommand;
    public RelayCommand ZoomToPointCommand =>
        _zoomToPointCommand ??= new RelayCommand(param =>
        {
            if (Point != null)
            {
                var point = new Point(Point.X, Point.Y);
                RequestZoomToPoint?.Invoke(point);
            }
        });

    private RelayCommand _deletePointCommand;
    public RelayCommand DeletePointCommand =>
        _deletePointCommand ??= new RelayCommand(param =>
        {
            // Delete handled by parent
        }, param => CanDelete);

    public PointEditorPresenter(NotifiablePoint point, bool canDelete = false)
    {
        Point = point ?? throw new ArgumentNullException(nameof(point));
        CanDelete = canDelete;

        if (Point != null)
        {
            Point.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(NotifiablePoint.X) || 
                    e.PropertyName == nameof(NotifiablePoint.Y) ||
                    e.PropertyName == nameof(NotifiablePoint.Z) ||
                    e.PropertyName == nameof(NotifiablePoint.M))
                {
                    PointChanged?.Invoke(Point);
                }
            };
        }
    }
}

