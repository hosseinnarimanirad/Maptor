using System;
using IRI.Maptor.Jab.Common;
using IRI.Maptor.Jab.Common.Assets.Commands;
using IRI.Maptor.Jab.Controls.Models.GeometryDetails;
using IRI.Maptor.Sta.Common.Primitives;

namespace IRI.Maptor.Jab.Controls.Presenters.CoordinateEditors;

public class PointEditorPresenter : Notifier
{
    private PointInfo _point;
    public PointInfo Point
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
    public event Action<PointInfo>? PointChanged;

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

    public PointEditorPresenter(PointInfo point, bool canDelete = false)
    {
        Point = point ?? throw new ArgumentNullException(nameof(point));
        CanDelete = canDelete;

        if (Point != null)
        {
            Point.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(PointInfo.X) || 
                    e.PropertyName == nameof(PointInfo.Y) ||
                    e.PropertyName == nameof(PointInfo.Z) ||
                    e.PropertyName == nameof(PointInfo.M))
                {
                    PointChanged?.Invoke(Point);
                }
            };
        }
    }
}

