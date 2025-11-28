using System.Collections.ObjectModel;
using IRI.Maptor.Jab.Common;

namespace IRI.Maptor.Jab.Common.Models.CoordinateEditor;

public class RingInfo : Notifier
{
    private bool _isExterior;
    public bool IsExterior
    {
        get => _isExterior;
        set
        {
            _isExterior = value;
            RaisePropertyChanged();
        }
    }

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
}




