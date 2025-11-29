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

    private ObservableCollection<NotifiablePoint> _points = new ObservableCollection<NotifiablePoint>();
    public ObservableCollection<NotifiablePoint> Points
    {
        get => _points;
        set
        {
            _points = value ?? new ObservableCollection<NotifiablePoint>();
            RaisePropertyChanged();
        }
    }
}




